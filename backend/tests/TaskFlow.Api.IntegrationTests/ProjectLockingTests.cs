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
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Repositories;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using Project = TaskFlow.Domain.Project;

namespace TaskFlow.Api.IntegrationTests;

public class ProjectLockingTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task GetByIdForUpdate_WithoutTransaction_Throws()
    {
        await using var context = CreateContext();
        var repository = new ProjectRepository(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.GetByIdForUpdateAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetByIdForUpdate_SecondTransactionWaitsUntilFirstFinishes(bool rollback)
    {
        var sample = await SeedAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var first = CreateContext();
        await using var firstTransaction = await first.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, timeout.Token);
        var project = await new ProjectRepository(first).GetByIdForUpdateAsync(sample.Project.Id, timeout.Token);
        Assert.NotNull(project);
        project.UpdateDetails("Cambio de la primera transacción", project.Description);
        await first.SaveChangesAsync(timeout.Token);

        await using var second = CreateContext();
        await using var secondTransaction = await second.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, timeout.Token);
        var secondPid = ((NpgsqlConnection)second.Database.GetDbConnection()).ProcessID;
        var secondRead = new ProjectRepository(second).GetByIdForUpdateAsync(sample.Project.Id, timeout.Token);

        try
        {
            await WaitUntilDatabaseLockAsync(secondPid, secondRead, timeout.Token);
            if (rollback)
            {
                await firstTransaction.RollbackAsync(timeout.Token);
            }
            else
            {
                await firstTransaction.CommitAsync(timeout.Token);
            }

            var secondProject = await secondRead;
            Assert.NotNull(secondProject);
            Assert.Equal(rollback ? sample.Project.Name : "Cambio de la primera transacción", secondProject.Name);
            await secondTransaction.CommitAsync(timeout.Token);
        }
        finally
        {
            if (first.Database.CurrentTransaction is not null)
            {
                await firstTransaction.RollbackAsync(CancellationToken.None);
            }
            timeout.Cancel();
            // Aseguramos que termine la consulta pendiente antes de liberar su contexto.
            try { await secondRead; } catch (Exception) { }
        }
    }

    [Theory]
    [InlineData("archive")]
    [InlineData("delete")]
    public async Task Update_AfterWaitingForConcurrentWrite_RechecksProjectAndReturns404(string operation)
    {
        var sample = await SeedAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var holder = CreateContext();
        await using var holderTransaction = await holder.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, timeout.Token);
        var lockedProject = await new ProjectRepository(holder).GetByIdForUpdateAsync(
            sample.Project.Id, timeout.Token);
        Assert.NotNull(lockedProject);

        var interceptor = new LockAttemptInterceptor();
        using var factory = new AuthenticationTestFactory(postgres.Database.GetConnectionString());
        using var observedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddDbContext<TaskFlowDbContext>(options =>
                options.AddInterceptors(interceptor))));
        using var client = observedFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(sample.User.KeycloakSub));

        var update = client.PutAsJsonAsync($"/api/v1/projects/{sample.Project.Id}",
            new { name = "Edición que debe rechazarse" }, timeout.Token);
        try
        {
            var updaterPid = await interceptor.Started.Task.WaitAsync(timeout.Token);
            await WaitUntilDatabaseLockAsync(updaterPid, update, timeout.Token);

            // Simula una escritura administrativa con otra conexión real.
            if (operation == "archive")
            {
                lockedProject.Archive();
            }
            else
            {
                holder.Projects.Remove(lockedProject);
            }
            await holder.SaveChangesAsync(timeout.Token);
            await holderTransaction.CommitAsync(timeout.Token);

            using var response = await update;
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            await using var verification = CreateContext();
            var stored = await verification.Projects.SingleOrDefaultAsync(item => item.Id == sample.Project.Id);
            if (operation == "archive")
            {
                Assert.NotNull(stored);
                Assert.Equal(sample.Project.Name, stored.Name);
                Assert.NotNull(stored.ArchivedAt);
                Assert.Equal(1, await verification.ProjectMemberships.CountAsync(item => item.ProjectId == stored.Id));
            }
            else
            {
                Assert.Null(stored);
                Assert.False(await verification.ProjectMemberships.AnyAsync(item => item.ProjectId == sample.Project.Id));
            }
        }
        finally
        {
            if (holder.Database.CurrentTransaction is not null)
            {
                await holderTransaction.RollbackAsync(CancellationToken.None);
            }
            timeout.Cancel();
            try
            {
                using var pendingResponse = await update;
            }
            catch (Exception) { }
        }
    }

    [Theory]
    [InlineData("archive", true)]
    [InlineData("archive", false)]
    [InlineData("restore", true)]
    [InlineData("restore", false)]
    [InlineData("delete", true)]
    [InlineData("delete", false)]
    public async Task StateChangeAndUpdate_ConcurrentHttpRequests_RecheckVisibilityAfterLock(
        string action, bool stateChangeFirst)
    {
        var sample = await SeedAsync(archived: action == "restore");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var pause = new PauseFirstLockInterceptor();
        using var factory = new AuthenticationTestFactory(postgres.Database.GetConnectionString());
        using var observedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddDbContext<TaskFlowDbContext>(options =>
                options.AddInterceptors(pause))));
        using var member = observedFactory.CreateClient();
        using var admin = observedFactory.CreateClient();
        member.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(sample.User.KeycloakSub, ["User"]));
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(sample.User.KeycloakSub, ["Admin"]));

        var projectUri = $"/api/v1/projects/{sample.Project.Id}";
        Task<HttpResponseMessage> ChangeStateAsync() => action == "delete"
            ? admin.DeleteAsync(projectUri, timeout.Token)
            : admin.PostAsync(projectUri + $"/{action}", null, timeout.Token);

        var first = stateChangeFirst
            ? ChangeStateAsync()
            : member.PutAsJsonAsync($"/api/v1/projects/{sample.Project.Id}",
                new { name = "Edición concurrente" }, timeout.Token);
        Task<HttpResponseMessage>? second = null;
        try
        {
            await pause.FirstLocked.Task.WaitAsync(timeout.Token);
            second = stateChangeFirst
                ? member.PutAsJsonAsync($"/api/v1/projects/{sample.Project.Id}",
                    new { name = "Edición concurrente" }, timeout.Token)
                : ChangeStateAsync();

            var secondPid = await pause.SecondStarted.Task.WaitAsync(timeout.Token);
            await WaitUntilDatabaseLockAsync(secondPid, second, timeout.Token);
            pause.ReleaseFirst.TrySetResult(true);

            using var firstResponse = await first;
            using var secondResponse = await second;
            var updateAllowed = action == "restore" ? stateChangeFirst : !stateChangeFirst;
            var updateStatus = updateAllowed ? HttpStatusCode.OK : HttpStatusCode.NotFound;
            Assert.Equal(stateChangeFirst ? HttpStatusCode.NoContent : updateStatus, firstResponse.StatusCode);
            Assert.Equal(stateChangeFirst ? updateStatus : HttpStatusCode.NoContent, secondResponse.StatusCode);
            if (updateAllowed)
            {
                var updatedResponse = stateChangeFirst ? secondResponse : firstResponse;
                var updatedProject = Assert.IsType<ProjectResponse>(
                    await updatedResponse.Content.ReadFromJsonAsync<ProjectResponse>(timeout.Token));
                Assert.Equal("Edición concurrente", updatedProject.Name);
            }

            await using var context = CreateContext();
            var stored = await context.Projects.SingleOrDefaultAsync(project => project.Id == sample.Project.Id);
            if (action == "delete")
            {
                Assert.Null(stored);
                Assert.False(await context.ProjectMemberships.AnyAsync(item => item.ProjectId == sample.Project.Id));
                Assert.True(await context.Users.AnyAsync(user => user.Id == sample.User.Id));
            }
            else
            {
                Assert.NotNull(stored);
                Assert.Equal(updateAllowed ? "Edición concurrente" : sample.Project.Name, stored.Name);
                if (action == "archive")
                {
                    Assert.NotNull(stored.ArchivedAt);
                    Assert.Equal(stored.ArchivedAt.Value, stored.UpdatedAt);
                }
                else
                {
                    Assert.Null(stored.ArchivedAt);
                }
                Assert.Equal(1, await context.ProjectMemberships.CountAsync(item => item.ProjectId == stored.Id));
            }
        }
        finally
        {
            pause.ReleaseFirst.TrySetResult(true);
            timeout.Cancel();
            await DrainRequestAsync(first);
            await DrainRequestAsync(second);
        }
    }

    private static async Task DrainRequestAsync(Task<HttpResponseMessage>? request)
    {
        if (request is not null)
        {
            try { using var response = await request; } catch (Exception) { }
        }
    }

    private async Task WaitUntilDatabaseLockAsync(int pid, Task operation, CancellationToken cancellationToken)
    {
        await using var inspection = CreateContext();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));

        while (true)
        {
            Assert.False(operation.IsCompleted, "La operación terminó sin esperar el bloqueo del proyecto.");
            var waiting = await inspection.Database.SqlQuery<int>($"""
                SELECT COUNT(*)::integer AS "Value"
                FROM pg_stat_activity
                WHERE pid = {pid} AND wait_event_type = 'Lock'
                """).SingleAsync(deadline.Token);

            if (waiting == 1)
            {
                return;
            }
            await Task.Delay(25, deadline.Token);
        }
    }

    private async Task<(AppUser User, Project Project)> SeedAsync(bool archived = false)
    {
        var user = new AppUser(Guid.NewGuid().ToString(), "Miembro", null);
        var project = new Project($"Proyecto bloqueado {Guid.NewGuid():N}", null, user.Id);
        if (archived) { project.Archive(); }
        await using var context = CreateContext();
        context.Users.Add(user);
        context.Projects.Add(project);
        context.ProjectMemberships.Add(new AppUserProject(project.Id, user.Id));
        await context.SaveChangesAsync();
        return (user, project);
    }

    private TaskFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<TaskFlowDbContext>()
        .UseNpgsql(postgres.Database.GetConnectionString()).Options);

    private sealed class PauseFirstLockInterceptor : DbCommandInterceptor
    {
        private int _firstSeen;
        public TaskCompletionSource<bool> FirstLocked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ReleaseFirst { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<int> SecondStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal) && Volatile.Read(ref _firstSeen) == 1)
            {
                SecondStarted.TrySetResult(((NpgsqlConnection)command.Connection!).ProcessID);
            }
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal) &&
                Interlocked.CompareExchange(ref _firstSeen, 1, 0) == 0)
            {
                FirstLocked.TrySetResult(true);
                await ReleaseFirst.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }

    private sealed class LockAttemptInterceptor : DbCommandInterceptor
    {
        public TaskCompletionSource<int> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal))
            {
                Started.TrySetResult(((NpgsqlConnection)command.Connection!).ProcessID);
            }
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
