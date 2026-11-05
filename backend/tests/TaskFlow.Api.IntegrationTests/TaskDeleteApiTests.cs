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
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Api.Contracts.Users;
using TaskFlow.Infrastructure.Persistence;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using DomainTask = TaskFlow.Domain.Task;
using Project = TaskFlow.Domain.Project;
using TaskPriority = TaskFlow.Domain.TaskPriority;

namespace TaskFlow.Api.IntegrationTests;

public class TaskDeleteApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("User", false, true)]
    [InlineData("User", false, false)]
    [InlineData("Admin", false, true)]
    [InlineData("Admin", true, true)]
    public async Task Delete_VisibleTask_RemovesOnlyThatTaskAndPreservesProjectsUsersAndMemberships(
        string role, bool archived, bool assigned)
    {
        var sample = await SeedAsync(archived, assigned);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, role == "Admin" ? sample.Outsider : sample.Member, role);
        var before = await ReadCountsAsync();

        using var response = await client.DeleteAsync(Route(sample.Task.Id));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        Assert.Null(response.Headers.Location);
        var after = await ReadCountsAsync();
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Projects, after.Projects);
        Assert.Equal(before.Memberships, after.Memberships);
        Assert.Equal(before.Tasks - 1, after.Tasks);
        await using var context = CreateContext();
        Assert.False(await context.Tasks.AnyAsync(task => task.Id == sample.Task.Id));
        Assert.Equal(JsonSerializer.Serialize(sample.Sibling), JsonSerializer.Serialize(
            await context.Tasks.SingleAsync(task => task.Id == sample.Sibling.Id)));
        Assert.Equal(JsonSerializer.Serialize(sample.OtherTask), JsonSerializer.Serialize(
            await context.Tasks.SingleAsync(task => task.Id == sample.OtherTask.Id)));
        Assert.Equal(2, await context.ProjectMemberships.CountAsync(item => item.ProjectId == sample.Project.Id));
        Assert.True(await context.ProjectMemberships.AnyAsync(item =>
            item.ProjectId == sample.Project.Id && item.UserId == sample.Assignee.Id));
        var project = await context.Projects.SingleAsync(item => item.Id == sample.Project.Id);
        Assert.Equal(JsonSerializer.Serialize(sample.Project), JsonSerializer.Serialize(project));
        using var deletedResponse = await client.GetAsync(Route(sample.Task.Id));
        await AssertProblemAsync(deletedResponse, HttpStatusCode.NotFound);
        if (!archived)
        {
            using var assignedClient = CreateClient(factory, sample.Assignee);
            using var projectResponse = await assignedClient.GetAsync($"/api/v1/projects/{sample.Project.Id}");
            Assert.Equal(HttpStatusCode.OK, projectResponse.StatusCode);
        }
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("archived")]
    [InlineData("missing-user")]
    [InlineData("missing-admin")]
    public async Task Delete_InvisibleOrMissingTask_Returns404WithoutDeletingAnything(string scenario)
    {
        var sample = await SeedAsync(archived: scenario == "archived");
        using var factory = CreateFactory();
        using var client = CreateClient(factory, scenario == "foreign" ? sample.Outsider : sample.Member,
            scenario == "missing-admin" ? "Admin" : "User");
        var id = scenario.StartsWith("missing-", StringComparison.Ordinal) ? Guid.NewGuid() : sample.Task.Id;
        var before = await ReadCountsAsync();

        using var response = await client.DeleteAsync(Route(id));

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
        await AssertUnchangedAsync(sample, before);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("123")]
    public async Task Delete_InvalidIdentifier_Returns400WithoutDeletingAnything(string id)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var before = await ReadCountsAsync();

        using var response = await client.DeleteAsync($"/api/v1/tasks/{id}");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample, before);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task Delete_InvalidSession_RejectsRequestWithoutDeletingAnything(string session, HttpStatusCode status)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        if (session != "missing-token")
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                session == "invalid-token" ? "invalid-token" :
                factory.CreateToken(sample.Member.KeycloakSub, session == "missing-role" ? ["Other"] : ["User"]));
        }
        if (session is "inactive-token" or "introspection-unavailable")
        {
            factory.Introspection.Respond = _ => Task.FromResult(new HttpResponseMessage(
                session == "inactive-token" ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("{\"active\":false}")
            });
        }
        var before = await ReadCountsAsync();

        using var response = await client.DeleteAsync(Route(sample.Task.Id));

        await AssertProblemAsync(response, status);
        await AssertUnchangedAsync(sample, before);
    }

    [Fact]
    public async Task Delete_Twice_SecondRequestReturns404WithoutAffectingOtherData()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        using var first = await client.DeleteAsync(Route(sample.Task.Id));
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        var afterFirst = await ReadCountsAsync();

        using var second = await client.DeleteAsync(Route(sample.Task.Id));

        await AssertProblemAsync(second, HttpStatusCode.NotFound);
        Assert.Equal(afterFirst, await ReadCountsAsync());
    }

    [Fact]
    public async Task Delete_TwoConcurrentRequests_OneDeletesAndTheOtherReturns404()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var firstClient = CreateClient(factory, sample.Member);
        using var secondClient = CreateClient(factory, sample.Member);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var before = await ReadCountsAsync();

        var responses = await Task.WhenAll(firstClient.DeleteAsync(Route(sample.Task.Id), timeout.Token),
            secondClient.DeleteAsync(Route(sample.Task.Id), timeout.Token));
        using var first = responses[0];
        using var second = responses[1];

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.NoContent));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.NotFound));
        var after = await ReadCountsAsync();
        Assert.Equal(before.Tasks - 1, after.Tasks);
        Assert.Equal(before.Memberships, after.Memberships);
        Assert.Equal(before.Projects, after.Projects);
        Assert.Equal(before.Users, after.Users);
    }

    [Fact]
    public async Task Delete_FailureAfterSqlDelete_RollsBackDeletion()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        var failure = new FailAfterTaskDeleteInterceptor();
        using var failingFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<TaskFlowDbContext>(options => options.AddInterceptors(failure))));
        using var client = failingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(sample.Member.KeycloakSub));
        var before = await ReadCountsAsync();

        using var response = await client.DeleteAsync(Route(sample.Task.Id));

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.True(failure.DeleteExecuted);
        Assert.DoesNotContain("Fallo simulado", await response.Content.ReadAsStringAsync());
        await AssertUnchangedAsync(sample, before);
    }

    [Fact]
    public async Task TaskLifecycle_AssignedUserCanCollaborateAndKeepsProjectAccessAfterUnassignmentAndDeletion()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var creator = CreateClient(factory, sample.Member);
        using var collaborator = CreateClient(factory, sample.Assignee);
        using var projectResponse = await creator.PostAsJsonAsync("/api/v1/projects", new { name = "Proyecto del recorrido" });
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        var project = Assert.IsType<ProjectResponse>(await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>());
        using var taskResponse = await creator.PostAsJsonAsync($"/api/v1/projects/{project.Id}/tasks",
            new { title = "Tarea del recorrido", priority = "High" });
        Assert.Equal(HttpStatusCode.Created, taskResponse.StatusCode);
        var task = Assert.IsType<TaskResponse>(await taskResponse.Content.ReadFromJsonAsync<TaskResponse>());

        using var assignableResponse = await creator.GetAsync("/api/v1/users/assignable");
        Assert.Equal(HttpStatusCode.OK, assignableResponse.StatusCode);
        var assignableUsers = Assert.IsType<UserSummaryResponse[]>(
            await assignableResponse.Content.ReadFromJsonAsync<UserSummaryResponse[]>());
        var selected = Assert.Single(assignableUsers, user => user.Id == sample.Assignee.Id);
        using var assigned = await creator.PutAsJsonAsync(Route(task.Id) + "/assignee", new { assigneeId = selected.Id });
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        using var visibleProject = await collaborator.GetAsync($"/api/v1/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.OK, visibleProject.StatusCode);
        using var edited = await collaborator.PutAsJsonAsync(Route(task.Id), new { title = "Tarea editada", priority = "Low" });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        using var completed = await collaborator.PutAsJsonAsync(Route(task.Id) + "/status", new { status = "Done" });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        using var unassigned = await collaborator.PutAsJsonAsync(Route(task.Id) + "/assignee", new { assigneeId = (Guid?)null });
        Assert.Equal(HttpStatusCode.OK, unassigned.StatusCode);

        // Ya no tiene tareas asignadas, pero conserva la pertenencia y puede eliminarla.
        using var deleted = await collaborator.DeleteAsync(Route(task.Id));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var missing = await collaborator.GetAsync(Route(task.Id));
        await AssertProblemAsync(missing, HttpStatusCode.NotFound);
        using var stillVisible = await collaborator.GetAsync($"/api/v1/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.OK, stillVisible.StatusCode);
        await using var context = CreateContext();
        Assert.Equal(2, await context.ProjectMemberships.CountAsync(item => item.ProjectId == project.Id));
        Assert.False(await context.Tasks.AnyAsync(item => item.ProjectId == project.Id));
        Assert.True(await context.Projects.AnyAsync(item => item.Id == project.Id));
    }

    private async Task<Sample> SeedAsync(bool archived = false, bool assigned = true)
    {
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro editor", null);
        var assignee = new AppUser(Guid.NewGuid().ToString(), "Responsable de tarea", null);
        var outsider = new AppUser(Guid.NewGuid().ToString(), "Otra persona", null);
        var project = new Project($"Proyecto {Guid.NewGuid():N}", null, member.Id);
        var otherProject = new Project($"Otro proyecto {Guid.NewGuid():N}", null, outsider.Id);
        if (archived) { project.Archive(); }
        var task = new DomainTask(project.Id, "Tarea a eliminar", TaskPriority.High, assignedId: assigned ? assignee.Id : null);
        var sibling = new DomainTask(project.Id, "Tarea que permanece", TaskPriority.Medium);
        var otherTask = new DomainTask(otherProject.Id, "Tarea de otro proyecto", TaskPriority.Low);
        await using var context = CreateContext();
        context.Users.AddRange(member, assignee, outsider);
        context.Projects.AddRange(project, otherProject);
        context.ProjectMemberships.AddRange(new AppUserProject(project.Id, member.Id),
            new AppUserProject(project.Id, assignee.Id), new AppUserProject(otherProject.Id, outsider.Id));
        context.Tasks.AddRange(task, sibling, otherTask);
        await context.SaveChangesAsync();
        await context.Entry(project).ReloadAsync();
        await context.Entry(task).ReloadAsync();
        await context.Entry(sibling).ReloadAsync();
        await context.Entry(otherTask).ReloadAsync();
        return new Sample(project, task, sibling, otherTask, member, assignee, outsider);
    }

    private async Task AssertUnchangedAsync(Sample sample, (int Users, int Projects, int Tasks, int Memberships) before)
    {
        Assert.Equal(before, await ReadCountsAsync());
        await using var context = CreateContext();
        var stored = await context.Tasks.SingleAsync(task => task.Id == sample.Task.Id);
        Assert.Equal(JsonSerializer.Serialize(sample.Task), JsonSerializer.Serialize(stored));
    }

    private async Task<(int Users, int Projects, int Tasks, int Memberships)> ReadCountsAsync()
    {
        await using var context = CreateContext();
        return (await context.Users.CountAsync(), await context.Projects.CountAsync(),
            await context.Tasks.CountAsync(), await context.ProjectMemberships.CountAsync());
    }

    private static string Route(Guid taskId) => $"/api/v1/tasks/{taskId}";
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

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
    }

    private sealed class FailAfterTaskDeleteInterceptor : DbCommandInterceptor
    {
        internal bool DeleteExecuted { get; private set; }
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            CheckForDelete(command);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            CheckForDelete(command);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void CheckForDelete(DbCommand command)
        {
            if (command.CommandText.Contains("DELETE FROM task", StringComparison.Ordinal) ||
                command.CommandText.Contains("DELETE FROM \"task\"", StringComparison.Ordinal))
            {
                DeleteExecuted = true;
                throw new InvalidOperationException("Fallo simulado después del DELETE de tarea.");
            }
        }
    }

    private sealed record Sample(Project Project, DomainTask Task, DomainTask Sibling, DomainTask OtherTask,
        AppUser Member, AppUser Assignee, AppUser Outsider);
}
