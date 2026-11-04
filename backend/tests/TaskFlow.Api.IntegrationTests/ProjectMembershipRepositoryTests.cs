using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Repositories;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using Project = TaskFlow.Domain.Project;

namespace TaskFlow.Api.IntegrationTests;

public class ProjectMembershipRepositoryTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task EnsureMembership_WithoutTransaction_ThrowsWithoutInserting()
    {
        var sample = await SeedAsync();
        await using var context = CreateContext();
        var repository = new ProjectRepository(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.EnsureMembershipAsync(sample.ProjectId, sample.UserId, CancellationToken.None));

        Assert.False(await HasMembershipAsync(sample));
    }

    [Fact]
    public async Task EnsureMembership_NewMember_InsertsImmediatelyAndPersistsOnCommit()
    {
        var sample = await SeedAsync();
        await using var context = CreateContext();
        var repository = new ProjectRepository(context);
        var before = DateTimeOffset.UtcNow.AddTicks(-10);

        await context.ExecuteInTransactionAsync(async token =>
        {
            await repository.EnsureMembershipAsync(sample.ProjectId, sample.UserId, token);
            // No se necesita SaveChanges para ejecutar este INSERT de SQL directo.
            var inserted = await context.ProjectMemberships.SingleAsync(membership =>
                membership.ProjectId == sample.ProjectId && membership.UserId == sample.UserId, token);
            Assert.InRange(inserted.CreatedAt, before, DateTimeOffset.UtcNow);
            return true;
        });

        Assert.True(await HasMembershipAsync(sample));
    }

    [Fact]
    public async Task EnsureMembership_ExistingMember_DoesNotDuplicateOrChangeCreationDate()
    {
        var sample = await SeedAsync();
        DateTimeOffset createdAt;
        await using (var seedContext = CreateContext())
        {
            var membership = new AppUserProject(sample.ProjectId, sample.UserId);
            seedContext.ProjectMemberships.Add(membership);
            await seedContext.SaveChangesAsync();
            await seedContext.Entry(membership).ReloadAsync();
            createdAt = membership.CreatedAt;
        }
        await using (var context = CreateContext())
        {
            var repository = new ProjectRepository(context);
            await context.ExecuteInTransactionAsync(async token =>
            {
                await repository.EnsureMembershipAsync(sample.ProjectId, sample.UserId, token);
                await repository.EnsureMembershipAsync(sample.ProjectId, sample.UserId, token);
                return true;
            });
        }

        await using var verification = CreateContext();
        var memberships = await verification.ProjectMemberships.Where(membership =>
            membership.ProjectId == sample.ProjectId && membership.UserId == sample.UserId).ToArrayAsync();
        Assert.Equal(createdAt, Assert.Single(memberships).CreatedAt);
    }

    [Fact]
    public async Task EnsureMembership_LaterOperationFails_RollsBackInsertedMembership()
    {
        var sample = await SeedAsync();
        await using var context = CreateContext();
        var repository = new ProjectRepository(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.ExecuteInTransactionAsync<bool>(async token =>
            {
                await repository.EnsureMembershipAsync(sample.ProjectId, sample.UserId, token);
                Assert.True(await context.ProjectMemberships.AnyAsync(membership =>
                    membership.ProjectId == sample.ProjectId && membership.UserId == sample.UserId, token));
                throw new InvalidOperationException("Fallo posterior simulado por la prueba.");
            }));

        Assert.False(await HasMembershipAsync(sample));
    }

    [Fact]
    public async Task EnsureMembership_TwoConcurrentTransactions_BothSucceedWithOneMembership()
    {
        var sample = await SeedAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        async Task InsertAsync()
        {
            await using var context = CreateContext();
            var repository = new ProjectRepository(context);
            await context.ExecuteInTransactionAsync(async token =>
            {
                await repository.EnsureMembershipAsync(sample.ProjectId, sample.UserId, token);
                return true;
            }, timeout.Token);
        }

        await Task.WhenAll(InsertAsync(), InsertAsync());

        await using var verification = CreateContext();
        Assert.Equal(1, await verification.ProjectMemberships.CountAsync(membership =>
            membership.ProjectId == sample.ProjectId && membership.UserId == sample.UserId, timeout.Token));
    }

    private async Task<Sample> SeedAsync()
    {
        var creator = new AppUser(Guid.NewGuid().ToString(), "Creador", null);
        var candidate = new AppUser(Guid.NewGuid().ToString(), "Nuevo miembro", null);
        var project = new Project("Proyecto de prueba de pertenencia", null, creator.Id);
        await using var context = CreateContext();
        context.Users.AddRange(creator, candidate);
        context.Projects.Add(project);
        context.ProjectMemberships.Add(new AppUserProject(project.Id, creator.Id));
        await context.SaveChangesAsync();
        return new Sample(project.Id, candidate.Id);
    }

    private async Task<bool> HasMembershipAsync(Sample sample)
    {
        await using var context = CreateContext();
        return await context.ProjectMemberships.AnyAsync(membership =>
            membership.ProjectId == sample.ProjectId && membership.UserId == sample.UserId);
    }

    private TaskFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<TaskFlowDbContext>()
        .UseNpgsql(postgres.Database.GetConnectionString()).Options);

    private sealed record Sample(Guid ProjectId, Guid UserId);
}
