using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Infrastructure.Persistence;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using DomainTask = TaskFlow.Domain.Task;
using Project = TaskFlow.Domain.Project;
using TaskPriority = TaskFlow.Domain.TaskPriority;
using TaskState = TaskFlow.Domain.TaskState;

namespace TaskFlow.Api.IntegrationTests;

public class ProjectArchiveApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Archive_Admin_Returns204AndPreservesProjectTasksAndMembers()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Admin, "Admin");
        var before = DateTimeOffset.UtcNow;

        using var response = await client.PostAsync($"/api/v1/projects/{sample.Project.Id}/archive", null);
        var after = DateTimeOffset.UtcNow;

        await AssertNoContentAsync(response);
        await using var context = CreateContext();
        var stored = await context.Projects.SingleAsync(project => project.Id == sample.Project.Id);
        Assert.NotNull(stored.ArchivedAt);
        // PostgreSQL conserva microsegundos; redondeamos el límite inferior a esa precisión.
        var lowerBound = new DateTimeOffset(before.Ticks - before.Ticks % 10, TimeSpan.Zero);
        Assert.InRange(stored.ArchivedAt.Value, lowerBound, after);
        Assert.Equal(stored.ArchivedAt.Value, stored.UpdatedAt);
        await AssertPreservedDataAsync(sample, context);
    }

    [Fact]
    public async Task Archive_AlreadyArchived_IsIdempotentAndPreservesTimestamps()
    {
        var sample = await SeedAsync(archived: true);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Admin, "Admin");

        using var first = await client.PostAsync($"/api/v1/projects/{sample.Project.Id}/archive", null);
        using var second = await client.PostAsync($"/api/v1/projects/{sample.Project.Id}/archive", null);

        await AssertNoContentAsync(first);
        await AssertNoContentAsync(second);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("creator", false, false, HttpStatusCode.Forbidden)]
    [InlineData("member", false, false, HttpStatusCode.Forbidden)]
    [InlineData("outsider", false, false, HttpStatusCode.NotFound)]
    [InlineData("member", true, false, HttpStatusCode.NotFound)]
    [InlineData("member", false, true, HttpStatusCode.NotFound)]
    [InlineData("admin", false, true, HttpStatusCode.NotFound)]
    public async Task Archive_VisibilityPrecedesAdministrativePermission(
        string actor, bool archived, bool missing, HttpStatusCode expected)
    {
        var sample = await SeedAsync(archived);
        using var factory = CreateFactory();
        var user = actor switch
        {
            "creator" => sample.Creator,
            "member" => sample.Member,
            _ => sample.Admin
        };
        using var client = CreateClient(factory, user, actor == "admin" ? "Admin" : "User");
        var id = missing ? Guid.NewGuid() : sample.Project.Id;

        using var response = await client.PostAsync($"/api/v1/projects/{id}/archive", null);

        await AssertProblemAsync(response, expected);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task Archive_InvalidIdentifier_Returns400()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Admin, "Admin");

        using var response = await client.PostAsync("/api/v1/projects/not-a-guid/archive", null);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task Archive_ProjectDisappearsFromUserReadsAndAppearsInAdminArchivedList()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var member = CreateClient(factory, sample.Member);
        using var admin = CreateClient(factory, sample.Admin, "Admin");
        using var visibleBefore = await member.GetAsync($"/api/v1/projects/{sample.Project.Id}");
        Assert.Equal(HttpStatusCode.OK, visibleBefore.StatusCode);

        using var archive = await admin.PostAsync($"/api/v1/projects/{sample.Project.Id}/archive", null);
        await AssertNoContentAsync(archive);

        using var hiddenAfter = await member.GetAsync($"/api/v1/projects/{sample.Project.Id}");
        await AssertProblemAsync(hiddenAfter, HttpStatusCode.NotFound);
        using var memberList = await member.GetAsync("/api/v1/projects");
        Assert.Equal(HttpStatusCode.OK, memberList.StatusCode);
        var visiblePage = Assert.IsType<ProjectPageResponse>(await memberList.Content.ReadFromJsonAsync<ProjectPageResponse>());
        Assert.Empty(visiblePage.Items);
        Assert.Equal(0, visiblePage.TotalCount);

        using var adminDetail = await admin.GetAsync($"/api/v1/projects/{sample.Project.Id}");
        Assert.Equal(HttpStatusCode.OK, adminDetail.StatusCode);
        using var archivedList = await admin.GetAsync("/api/v1/projects?archived=true&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, archivedList.StatusCode);
        var archivedPage = Assert.IsType<ProjectPageResponse>(await archivedList.Content.ReadFromJsonAsync<ProjectPageResponse>());
        Assert.Contains(archivedPage.Items, project => project.Id == sample.Project.Id);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task Archive_InvalidSession_DoesNotChangeData(string session, HttpStatusCode expected)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        if (session != "missing-token")
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                session == "invalid-token" ? "invalid-token" :
                    factory.CreateToken(sample.Admin.KeycloakSub, session == "missing-role" ? ["Other"] : ["Admin"]));
        }
        if (session is "inactive-token" or "introspection-unavailable")
        {
            factory.Introspection.Respond = _ => Task.FromResult(new HttpResponseMessage(
                session == "inactive-token" ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("{\"active\":false}")
            });
        }

        using var response = await client.PostAsync($"/api/v1/projects/{sample.Project.Id}/archive", null);

        await AssertProblemAsync(response, expected);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task Archive_ConcurrentRequests_PerformOneUpdateAndBothReturn204()
    {
        var sample = await SeedAsync();
        var counter = new ProjectUpdateCounter();
        using var factory = CreateFactory();
        using var observedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddDbContext<TaskFlowDbContext>(options =>
                options.AddInterceptors(counter))));
        using var client = observedFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(sample.Admin.KeycloakSub, ["Admin"]));

        var responses = await Task.WhenAll(
            client.PostAsync($"/api/v1/projects/{sample.Project.Id}/archive", null),
            client.PostAsync($"/api/v1/projects/{sample.Project.Id}/archive", null));
        try
        {
            foreach (var response in responses)
            {
                await AssertNoContentAsync(response);
            }
            Assert.Equal(1, counter.Count);
            await using var context = CreateContext();
            var stored = await context.Projects.SingleAsync(project => project.Id == sample.Project.Id);
            Assert.NotNull(stored.ArchivedAt);
            Assert.Equal(stored.ArchivedAt.Value, stored.UpdatedAt);
            await AssertPreservedDataAsync(sample, context);
        }
        finally
        {
            foreach (var response in responses) { response.Dispose(); }
        }
    }

    private async Task<Sample> SeedAsync(bool archived = false)
    {
        var creator = new AppUser(Guid.NewGuid().ToString(), "Creadora", null);
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro", null);
        var admin = new AppUser(Guid.NewGuid().ToString(), "Administrador sin pertenencia", null);
        var project = new Project($"Archivado {Guid.NewGuid():N}", "Descripción conservada", creator.Id);
        if (archived) { project.Archive(); }
        var tasks = new[]
        {
            new DomainTask(project.Id, "Tarea asignada", TaskPriority.High, assignedId: member.Id,
                status: TaskState.InProgress, dueDate: new DateOnly(2026, 1, 1)),
            new DomainTask(project.Id, "Tarea sin asignar", TaskPriority.Low)
        };
        await using var context = CreateContext();
        context.Users.AddRange(creator, member, admin);
        context.Projects.Add(project);
        var oldDate = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        context.Entry(project).Property(item => item.CreatedAt).CurrentValue = oldDate;
        context.Entry(project).Property(item => item.UpdatedAt).CurrentValue = oldDate.AddDays(1);
        if (archived)
        {
            context.Entry(project).Property(item => item.ArchivedAt).CurrentValue = oldDate.AddDays(1);
        }
        context.ProjectMemberships.AddRange(
            new AppUserProject(project.Id, creator.Id), new AppUserProject(project.Id, member.Id));
        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync();
        return new Sample(project, creator, member, admin, tasks);
    }

    private async Task AssertUnchangedAsync(Sample sample)
    {
        await using var context = CreateContext();
        var stored = await context.Projects.SingleAsync(project => project.Id == sample.Project.Id);
        Assert.Equal(sample.Project.ArchivedAt, stored.ArchivedAt);
        Assert.Equal(sample.Project.UpdatedAt, stored.UpdatedAt);
        await AssertPreservedDataAsync(sample, context);
    }

    private static async Task AssertPreservedDataAsync(Sample sample, TaskFlowDbContext context)
    {
        var stored = await context.Projects.SingleAsync(project => project.Id == sample.Project.Id);
        Assert.Equal(sample.Project.Name, stored.Name);
        Assert.Equal(sample.Project.Description, stored.Description);
        Assert.Equal(sample.Project.CreatedBy, stored.CreatedBy);
        Assert.Equal(sample.Project.CreatedAt, stored.CreatedAt);
        Assert.Equal(new[] { sample.Creator.Id, sample.Member.Id }.OrderBy(id => id),
            await context.ProjectMemberships.Where(membership => membership.ProjectId == stored.Id)
                .OrderBy(membership => membership.UserId).Select(membership => membership.UserId).ToArrayAsync());
        var storedTasks = await context.Tasks.Where(task => task.ProjectId == stored.Id).ToArrayAsync();
        Assert.Equal(sample.Tasks.Length, storedTasks.Length);
        foreach (var task in sample.Tasks)
        {
            var storedTask = Assert.Single(storedTasks, item => item.Id == task.Id);
            Assert.Equal(task.Title, storedTask.Title);
            Assert.Equal(task.AssignedId, storedTask.AssignedId);
            Assert.Equal(task.Status, storedTask.Status);
            Assert.Equal(task.Priority, storedTask.Priority);
            Assert.Equal(task.DueDate, storedTask.DueDate);
        }
    }

    private AuthenticationTestFactory CreateFactory() => new(postgres.Database.GetConnectionString());
    private TaskFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<TaskFlowDbContext>()
        .UseNpgsql(postgres.Database.GetConnectionString()).Options);

    private static HttpClient CreateClient(AuthenticationTestFactory factory, AppUser user, string role = "User")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(user.KeycloakSub, [role]));
        return client;
    }

    private static async Task AssertNoContentAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, document.RootElement.GetProperty("status").GetInt32());
    }

    private sealed record Sample(Project Project, AppUser Creator, AppUser Member, AppUser Admin, DomainTask[] Tasks);

    private sealed class ProjectUpdateCounter : DbCommandInterceptor
    {
        private int _count;
        public int Count => Volatile.Read(ref _count);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CountUpdate(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CountUpdate(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void CountUpdate(DbCommand command)
        {
            if (command.CommandText.Contains("UPDATE project SET", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _count);
            }
        }
    }
}
