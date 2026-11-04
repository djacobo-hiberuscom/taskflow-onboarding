using System.Data;
using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Application.Projects;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Repositories;
using AppUser = TaskFlow.Domain.AppUser;

namespace TaskFlow.Api.IntegrationTests;

public class CreatorLockingTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task GetByIdForKeyShare_WithoutTransaction_Throws()
    {
        await using var context = CreateContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new AppUserRepository(context).GetByIdForKeyShareAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdForKeyShare_MissingUser_ReturnsNull()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var user = await new AppUserRepository(context).GetByIdForKeyShareAsync(Guid.NewGuid());

        Assert.Null(user);
        await transaction.CommitAsync();
    }

    [Fact]
    public async Task Create_MissingCreator_AbortsWithoutInsertingProjectOrMembership()
    {
        await using var context = CreateContext();
        var before = (await context.Projects.CountAsync(), await context.ProjectMemberships.CountAsync());
        var service = new ProjectService(new ProjectRepository(context), context, new AppUserRepository(context));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateProjectAsync("Proyecto sin creador", null, Guid.NewGuid()));

        Assert.Equal("El usuario creador ya no existe.", exception.Message);
        Assert.Null(context.Database.CurrentTransaction);
        await using var verification = CreateContext();
        Assert.Equal(before, (await verification.Projects.CountAsync(), await verification.ProjectMemberships.CountAsync()));
    }

    [Fact]
    public async Task KeyShare_TwoTransactionsCanProtectTheSameCreator()
    {
        var user = await SeedUserAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var first = CreateContext();
        await using var firstTransaction = await first.Database.BeginTransactionAsync(timeout.Token);
        Assert.NotNull(await new AppUserRepository(first).GetByIdForKeyShareAsync(user.Id, timeout.Token));
        await using var second = CreateContext();
        await using var secondTransaction = await second.Database.BeginTransactionAsync(timeout.Token);
        var secondLock = new AppUserRepository(second).GetByIdForKeyShareAsync(user.Id, timeout.Token);

        try
        {
            var protectedUser = await secondLock.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotNull(protectedUser);
            Assert.Equal(user.Id, protectedUser.Id);
            await secondTransaction.CommitAsync(timeout.Token);
        }
        finally
        {
            await firstTransaction.RollbackAsync(CancellationToken.None);
            timeout.Cancel();
            try { await secondLock; } catch (Exception) { }
        }
    }

    [Fact]
    public async Task KeyShare_AllowsConcurrentProfileUpdate()
    {
        var user = await SeedUserAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var holder = CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync(timeout.Token);
        Assert.NotNull(await new AppUserRepository(holder).GetByIdForKeyShareAsync(user.Id, timeout.Token));
        await using var editor = CreateContext();
        var update = editor.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE app_user SET display_name = {"Perfil actualizado"} WHERE id = {user.Id}
            """, timeout.Token);

        try
        {
            Assert.Equal(1, await update.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            await transaction.RollbackAsync(CancellationToken.None);
            timeout.Cancel();
            try { await update; } catch (Exception) { }
        }

        await using var verification = CreateContext();
        Assert.Equal("Perfil actualizado", (await verification.Users.SingleAsync(item => item.Id == user.Id)).DisplayName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KeyShare_UserDeletionWaitsUntilTransactionEnds(bool rollback)
    {
        var user = await SeedUserAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var holder = CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync(timeout.Token);
        Assert.NotNull(await new AppUserRepository(holder).GetByIdForKeyShareAsync(user.Id, timeout.Token));
        await using var deleter = CreateContext();
        await deleter.Database.OpenConnectionAsync(timeout.Token);
        var deleterPid = ((NpgsqlConnection)deleter.Database.GetDbConnection()).ProcessID;
        var delete = deleter.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM app_user WHERE id = {user.Id}", timeout.Token);

        try
        {
            await WaitUntilDatabaseLockAsync(deleterPid, delete, timeout.Token);
            if (rollback) { await transaction.RollbackAsync(timeout.Token); }
            else { await transaction.CommitAsync(timeout.Token); }
            Assert.Equal(1, await delete);
            await using var verification = CreateContext();
            Assert.False(await verification.Users.AnyAsync(item => item.Id == user.Id));
        }
        finally
        {
            if (holder.Database.CurrentTransaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            timeout.Cancel();
            try { await delete; } catch (Exception) { }
        }
    }

    [Fact]
    public async Task Create_ProtectsCreatorBeforeAnyInsertUntilProjectAndMembershipCommit()
    {
        var user = await SeedUserAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var probe = new KeyShareProbe(pauseAfterAcquisition: true);
        using var factory = CreateFactory();
        using var observedFactory = ObserveKeyShare(factory, probe);
        using var client = observedFactory.CreateClient();
        Authenticate(client, factory.CreateToken(user.KeycloakSub));
        var create = client.PostAsJsonAsync("/api/v1/projects", new { name = "Creación protegida" }, timeout.Token);

        await using var deleter = CreateContext();
        await using var deleteTransaction = await deleter.Database.BeginTransactionAsync(timeout.Token);
        Task<int>? delete = null;
        try
        {
            await probe.Acquired.Task.WaitAsync(timeout.Token);
            Assert.True(probe.TransactionWasActive);
            await using (var inspection = CreateContext())
            {
                Assert.False(await inspection.Projects.AnyAsync(project => project.CreatedBy == user.Id));
                Assert.False(await inspection.ProjectMemberships.AnyAsync(membership => membership.UserId == user.Id));
            }

            var deleterPid = ((NpgsqlConnection)deleter.Database.GetDbConnection()).ProcessID;
            delete = deleter.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM app_user WHERE id = {user.Id}", timeout.Token);
            await WaitUntilDatabaseLockAsync(deleterPid, delete, timeout.Token);

            probe.Release.TrySetResult(true);
            using var response = await create;
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var project = Assert.IsType<ProjectResponse>(await response.Content.ReadFromJsonAsync<ProjectResponse>(timeout.Token));
            Assert.Equal(1, await delete);
            // El DELETE solo prueba la espera; se revierte para conservar la identidad y sus referencias.
            await deleteTransaction.RollbackAsync(timeout.Token);

            await using var verification = CreateContext();
            Assert.Equal(user.Id, (await verification.Projects.SingleAsync(item => item.Id == project.Id)).CreatedBy);
            Assert.True(await verification.ProjectMemberships.AnyAsync(item => item.ProjectId == project.Id && item.UserId == user.Id));
            Assert.True(await verification.Users.AnyAsync(item => item.Id == user.Id));
        }
        finally
        {
            probe.Release.TrySetResult(true);
            timeout.Cancel();
            await DrainRequestAsync(create);
            if (delete is not null) { try { await delete; } catch (Exception) { } }
            if (deleter.Database.CurrentTransaction is not null)
            {
                await deleteTransaction.RollbackAsync(CancellationToken.None);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_WhenUserDeletionLocksFirst_RechecksCreatorBeforeInserting(bool commitDeletion)
    {
        var user = await SeedUserAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var maintenance = CreateContext();
        await using var transaction = await maintenance.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, timeout.Token);
        var users = await maintenance.Users.FromSql(
            $"SELECT * FROM app_user WHERE id = {user.Id} FOR UPDATE").ToArrayAsync(timeout.Token);
        var lockedUser = Assert.Single(users);
        var probe = new KeyShareProbe();
        using var factory = CreateFactory();
        using var observedFactory = ObserveKeyShare(factory, probe);
        using var client = observedFactory.CreateClient();
        Authenticate(client, factory.CreateToken(user.KeycloakSub));
        var name = $"Creador concurrente {Guid.NewGuid():N}";
        var create = client.PostAsJsonAsync("/api/v1/projects", new { name }, timeout.Token);

        try
        {
            var creatorPid = await probe.Started.Task.WaitAsync(timeout.Token);
            await WaitUntilDatabaseLockAsync(creatorPid, create, timeout.Token);
            maintenance.Users.Remove(lockedUser);
            await maintenance.SaveChangesAsync(timeout.Token);
            if (commitDeletion) { await transaction.CommitAsync(timeout.Token); }
            else { await transaction.RollbackAsync(timeout.Token); }

            using var response = await create;
            Assert.Equal(commitDeletion ? HttpStatusCode.InternalServerError : HttpStatusCode.Created, response.StatusCode);
            await using var verification = CreateContext();
            Assert.Equal(!commitDeletion, await verification.Projects.AnyAsync(project => project.Name == name));
            Assert.Equal(!commitDeletion, await verification.ProjectMemberships.AnyAsync(membership => membership.UserId == user.Id));
            if (commitDeletion)
            {
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
                Assert.DoesNotContain("El usuario creador", await response.Content.ReadAsStringAsync(timeout.Token));
            }
        }
        finally
        {
            if (maintenance.Database.CurrentTransaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            timeout.Cancel();
            await DrainRequestAsync(create);
        }
    }

    private async Task WaitUntilDatabaseLockAsync(int pid, Task operation, CancellationToken cancellationToken)
    {
        await using var inspection = CreateContext();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        while (true)
        {
            Assert.False(operation.IsCompleted, "La operación terminó sin esperar el bloqueo del usuario.");
            var count = await inspection.Database.SqlQuery<int>($"""
                SELECT COUNT(*)::integer AS "Value" FROM pg_stat_activity
                WHERE pid = {pid} AND wait_event_type = 'Lock'
                """).SingleAsync(deadline.Token);
            if (count == 1) { return; }
            await Task.Delay(25, deadline.Token);
        }
    }

    private async Task<AppUser> SeedUserAsync()
    {
        var user = new AppUser(Guid.NewGuid().ToString(), "Creador", null);
        await using var context = CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private AuthenticationTestFactory CreateFactory() => new(postgres.Database.GetConnectionString());
    private TaskFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<TaskFlowDbContext>()
        .UseNpgsql(postgres.Database.GetConnectionString()).Options);

    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> ObserveKeyShare(
        AuthenticationTestFactory factory, KeyShareProbe probe) => factory.WithWebHostBuilder(builder =>
        builder.ConfigureTestServices(services => services.AddDbContext<TaskFlowDbContext>(options =>
            options.AddInterceptors(probe))));

    private static void Authenticate(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static async Task DrainRequestAsync(Task<HttpResponseMessage> request)
    {
        try { using var response = await request; } catch (Exception) { }
    }

    private sealed class KeyShareProbe(bool pauseAfterAcquisition = false) : DbCommandInterceptor
    {
        public TaskCompletionSource<int> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Acquired { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool TransactionWasActive { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FOR KEY SHARE", StringComparison.Ordinal))
            {
                TransactionWasActive = eventData.Context?.Database.CurrentTransaction is not null;
                Started.TrySetResult(((NpgsqlConnection)command.Connection!).ProcessID);
            }
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FOR KEY SHARE", StringComparison.Ordinal))
            {
                Acquired.TrySetResult(true);
                if (pauseAfterAcquisition) { await Release.Task.WaitAsync(cancellationToken); }
            }
            return result;
        }
    }
}
