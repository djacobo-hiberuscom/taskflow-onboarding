using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Infrastructure.Persistence;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using DomainTask = TaskFlow.Domain.Task;
using Project = TaskFlow.Domain.Project;
using TaskPriority = TaskFlow.Domain.TaskPriority;
using TaskState = TaskFlow.Domain.TaskState;

namespace TaskFlow.Api.IntegrationTests;

public class TaskStatusApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(TaskState.ToDo, "ToDo")]
    [InlineData(TaskState.ToDo, "InProgress")]
    [InlineData(TaskState.ToDo, "Done")]
    [InlineData(TaskState.InProgress, "ToDo")]
    [InlineData(TaskState.InProgress, "InProgress")]
    [InlineData(TaskState.InProgress, "Done")]
    [InlineData(TaskState.Done, "ToDo")]
    [InlineData(TaskState.Done, "InProgress")]
    [InlineData(TaskState.Done, "Done")]
    public async Task ChangeStatus_AllTransitions_UpdateStatusAndPreserveOtherTaskFields(TaskState initial, string status)
    {
        var sample = await SeedAsync(initial: initial);
        using var factory = CreateFactory();
        // Un miembro puede cambiar el estado aunque la tarea esté asignada a otra persona.
        using var client = CreateClient(factory, sample.Member);
        var before = DateTimeOffset.UtcNow.AddTicks(-10);

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), new { status });

        var detail = await ReadUpdatedAsync(response);
        Assert.Equal(status, detail.Status);
        Assert.Equal(sample.Task.Id, detail.Id);
        Assert.Equal(sample.Project.Id, detail.ProjectId);
        Assert.Equal(sample.Project.Name, detail.ProjectName);
        Assert.Equal(sample.Task.Title, detail.Title);
        Assert.Equal(sample.Task.Description, detail.Description);
        Assert.Equal(sample.Task.Priority.ToString(), detail.Priority);
        Assert.Equal(sample.Task.DueDate, detail.DueDate);
        Assert.Equal(sample.Task.CreatedAt, detail.CreatedAt);
        Assert.NotNull(detail.Assignee);
        Assert.Equal(sample.Assignee.Id, detail.Assignee.Id);
        Assert.Equal(sample.Assignee.DisplayName, detail.Assignee.DisplayName);
        Assert.InRange(detail.UpdatedAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(TimeSpan.Zero, detail.UpdatedAt.Offset);
        Assert.Null(response.Headers.Location);
        await using var context = CreateContext();
        var stored = await context.Tasks.SingleAsync(task => task.Id == sample.Task.Id);
        Assert.Equal(status, stored.Status.ToString());
        Assert.Equal(sample.Task.Title, stored.Title);
        Assert.Equal(sample.Task.Description, stored.Description);
        Assert.Equal(sample.Task.Priority, stored.Priority);
        Assert.Equal(sample.Task.DueDate, stored.DueDate);
        Assert.Equal(sample.Task.ProjectId, stored.ProjectId);
        Assert.Equal(sample.Task.AssignedId, stored.AssignedId);
        Assert.Equal(sample.Task.CreatedAt, stored.CreatedAt);
        Assert.InRange((stored.UpdatedAt - detail.UpdatedAt).Duration(), TimeSpan.Zero, TimeSpan.FromTicks(9));
        Assert.Equal(2, await context.ProjectMemberships.CountAsync(item => item.ProjectId == sample.Project.Id));
        using var getResponse = await client.GetAsync($"/api/v1/tasks/{sample.Task.Id}");
        var fetched = await ReadUpdatedAsync(getResponse);
        Assert.Equal(status, fetched.Status);
        Assert.Equal(stored.UpdatedAt, fetched.UpdatedAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangeStatus_AdminOutsideProject_CanChangeActiveOrArchivedTaskWithoutBecomingMember(bool archived)
    {
        var sample = await SeedAsync(archived);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Outsider, "Admin");

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), new { status = "Done" });

        var detail = await ReadUpdatedAsync(response);
        Assert.Equal("Done", detail.Status);
        await using var context = CreateContext();
        Assert.False(await context.ProjectMemberships.AnyAsync(item =>
            item.ProjectId == sample.Project.Id && item.UserId == sample.Outsider.Id));
        var project = await context.Projects.SingleAsync(item => item.Id == sample.Project.Id);
        Assert.Equal(sample.Project.ArchivedAt, project.ArchivedAt);
        Assert.Equal(sample.Project.UpdatedAt, project.UpdatedAt);
    }

    [Fact]
    public async Task ChangeStatus_UnassignedTask_RemainsUnassigned()
    {
        var sample = await SeedAsync(unassigned: true);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), new { status = "Done" });

        var detail = await ReadUpdatedAsync(response);
        Assert.Null(detail.Assignee);
        await using var context = CreateContext();
        var task = await context.Tasks.SingleAsync(item => item.Id == sample.Task.Id);
        Assert.Null(task.AssignedId);
        Assert.Equal(TaskState.Done, task.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    [InlineData("Unknown")]
    [InlineData("todo")]
    [InlineData("InProress")]
    [InlineData("1")]
    public async Task ChangeStatus_InvalidText_Returns400WithoutChangingTask(string status)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), new { status });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"status\":")]
    [InlineData("{\"status\":null}")]
    [InlineData("{\"status\":1}")]
    [InlineData("{\"status\":true}")]
    [InlineData("{\"status\":[]}")]
    public async Task ChangeStatus_InvalidJsonOrMissingStatus_Returns400WithoutChangingTask(string body)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.PutAsync(Route(sample.Task.Id), content);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("projectId")]
    [InlineData("assigneeId")]
    [InlineData("unknownProperty")]
    public async Task ChangeStatus_AdditionalField_Returns400WithoutChangingTask(string property)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = new Dictionary<string, object?> { ["status"] = "Done", [property] = "No permitido" };

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), body);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("archived")]
    [InlineData("missing-user")]
    [InlineData("missing-admin")]
    public async Task ChangeStatus_InvisibleOrMissingTask_Returns404WithoutChangingTask(string scenario)
    {
        var sample = await SeedAsync(archived: scenario == "archived");
        using var factory = CreateFactory();
        using var client = CreateClient(factory, scenario == "foreign" ? sample.Outsider : sample.Member,
            scenario == "missing-admin" ? "Admin" : "User");
        var id = scenario.StartsWith("missing-", StringComparison.Ordinal) ? Guid.NewGuid() : sample.Task.Id;

        using var response = await client.PutAsJsonAsync(Route(id), new { status = "Done" });

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("123")]
    public async Task ChangeStatus_InvalidIdentifier_Returns400(string id)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.PutAsJsonAsync($"/api/v1/tasks/{id}/status", new { status = "Done" });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task ChangeStatus_InvalidSession_ReturnsProblemWithoutChangingTask(string session, HttpStatusCode status)
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

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), new { status = "Done" });

        await AssertProblemAsync(response, status);
        await AssertUnchangedAsync(sample);
    }

    private async Task<Sample> SeedAsync(bool archived = false, bool unassigned = false, TaskState initial = TaskState.InProgress)
    {
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro del proyecto", null);
        var assignee = new AppUser(Guid.NewGuid().ToString(), "Responsable de tarea", null);
        var outsider = new AppUser(Guid.NewGuid().ToString(), "Fuera del proyecto", null);
        var project = new Project($"Proyecto {Guid.NewGuid():N}", null, member.Id);
        if (archived) { project.Archive(); }
        var task = new DomainTask(project.Id, "Título original", TaskPriority.High,
            "Descripción original", unassigned ? null : assignee.Id, initial, new DateOnly(2030, 1, 1));
        await using var context = CreateContext();
        context.Users.AddRange(member, assignee, outsider);
        context.Projects.Add(project);
        context.ProjectMemberships.AddRange(new AppUserProject(project.Id, member.Id),
            new AppUserProject(project.Id, assignee.Id));
        context.Tasks.Add(task);
        var createdAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        context.Entry(task).Property(item => item.CreatedAt).CurrentValue = createdAt;
        context.Entry(task).Property(item => item.UpdatedAt).CurrentValue = createdAt.AddDays(1);
        await context.SaveChangesAsync();
        await context.Entry(project).ReloadAsync();
        return new Sample(project, task, member, assignee, outsider);
    }

    private async Task AssertUnchangedAsync(Sample sample)
    {
        await using var context = CreateContext();
        var stored = await context.Tasks.SingleAsync(task => task.Id == sample.Task.Id);
        Assert.Equal(JsonSerializer.Serialize(sample.Task), JsonSerializer.Serialize(stored));
        Assert.Equal(2, await context.ProjectMemberships.CountAsync(item => item.ProjectId == sample.Project.Id));
    }

    private static string Route(Guid taskId) => $"/api/v1/tasks/{taskId}/status";
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

    private static async Task<TaskResponse> ReadUpdatedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        return Assert.IsType<TaskResponse>(await response.Content.ReadFromJsonAsync<TaskResponse>());
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
        if (status == HttpStatusCode.BadRequest)
        {
            Assert.NotEmpty(json.RootElement.GetProperty("errors").EnumerateObject());
        }
    }

    private sealed record Sample(Project Project, DomainTask Task, AppUser Member, AppUser Assignee, AppUser Outsider);
}
