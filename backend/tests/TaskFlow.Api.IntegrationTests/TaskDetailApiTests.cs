using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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

public class TaskDetailApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("User", false)]
    [InlineData("Admin", false)]
    [InlineData("Admin", true)]
    public async Task GetById_VisibleTask_ReturnsContractWithoutChangingStoredDates(string role, bool archived)
    {
        var sample = await SeedAsync(archived);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, role == "Admin" ? sample.Outsider : sample.Member, role);

        using var response = await client.GetAsync($"/api/v1/tasks/{sample.Task.Id}");

        var detail = await ReadDetailAsync(response);
        Assert.Equal(sample.Task.Id, detail.Id);
        Assert.Equal(sample.Project.Id, detail.ProjectId);
        Assert.Equal(sample.Project.Name, detail.ProjectName);
        Assert.Equal(sample.Task.Title, detail.Title);
        Assert.Equal(sample.Task.Description, detail.Description);
        Assert.Equal("InProgress", detail.Status);
        Assert.Equal("High", detail.Priority);
        Assert.Equal(sample.Task.DueDate, detail.DueDate);
        Assert.Equal(sample.Task.CreatedAt, detail.CreatedAt);
        Assert.Equal(sample.Task.UpdatedAt, detail.UpdatedAt);
        Assert.NotNull(detail.Assignee);
        // El asignado es distinto del usuario que consulta y del creador del proyecto.
        Assert.Equal(sample.Assignee.Id, detail.Assignee.Id);
        Assert.Equal(sample.Assignee.DisplayName, detail.Assignee.DisplayName);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[]
        {
            "assignee", "createdAt", "description", "dueDate", "id", "priority",
            "projectId", "projectName", "status", "title", "updatedAt"
        }, json.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Equal("2026-01-01", json.RootElement.GetProperty("dueDate").GetString());
        Assert.Equal(new[] { "displayName", "id" }, json.RootElement.GetProperty("assignee")
            .EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());

        using var secondResponse = await client.GetAsync($"/api/v1/tasks/{sample.Task.Id}");
        var secondDetail = await ReadDetailAsync(secondResponse);
        Assert.Equal(detail.CreatedAt, secondDetail.CreatedAt);
        Assert.Equal(detail.UpdatedAt, secondDetail.UpdatedAt);
        await using var context = CreateContext();
        var stored = await context.Tasks.SingleAsync(task => task.Id == sample.Task.Id);
        Assert.Equal(sample.Task.CreatedAt, stored.CreatedAt);
        Assert.Equal(sample.Task.UpdatedAt, stored.UpdatedAt);
    }

    [Fact]
    public async Task GetById_UnassignedTaskWithOptionalFieldsEmpty_ReturnsExplicitNullsAndDefaultStatus()
    {
        var sample = await SeedAsync(optionalFields: false);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.GetAsync($"/api/v1/tasks/{sample.Task.Id}");

        var detail = await ReadDetailAsync(response);
        Assert.Null(detail.Assignee);
        Assert.Null(detail.Description);
        Assert.Null(detail.DueDate);
        Assert.Equal("ToDo", detail.Status);
        Assert.Equal("Low", detail.Priority);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var property in new[] { "assignee", "description", "dueDate" })
        {
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty(property).ValueKind);
        }
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("archived")]
    [InlineData("missing-user")]
    [InlineData("missing-admin")]
    public async Task GetById_InvisibleOrMissingTask_Returns404WithoutLeakingDetails(string scenario)
    {
        var sample = await SeedAsync(archived: scenario == "archived");
        using var factory = CreateFactory();
        var role = scenario == "missing-admin" ? "Admin" : "User";
        using var client = CreateClient(factory, scenario == "foreign" ? sample.Outsider : sample.Member, role);
        var taskId = scenario.StartsWith("missing-", StringComparison.Ordinal) ? Guid.NewGuid() : sample.Task.Id;

        using var response = await client.GetAsync($"/api/v1/tasks/{taskId}");
        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        using var missingResponse = await client.GetAsync($"/api/v1/tasks/{Guid.NewGuid()}");
        var missingProblem = await ReadProblemAsync(missingResponse, HttpStatusCode.NotFound);

        Assert.Equal(missingProblem.Title, problem.Title);
        Assert.Equal(missingProblem.Type, problem.Type);
        Assert.Equal(missingProblem.Detail, problem.Detail);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(sample.Task.Title, body);
        Assert.DoesNotContain(sample.Project.Name, body);
        Assert.DoesNotContain(sample.Assignee.DisplayName, body);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("123")]
    public async Task GetById_InvalidIdentifier_Returns400(string taskId)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.GetAsync($"/api/v1/tasks/{taskId}");

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task GetById_InvalidSession_RejectsRequestWithoutWriting(string session, HttpStatusCode status)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        if (session != "missing-token")
        {
            var token = session == "invalid-token" ? "invalid-token" :
                factory.CreateToken(sample.Member.KeycloakSub, session == "missing-role" ? ["Other"] : ["User"]);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

        using var response = await client.GetAsync($"/api/v1/tasks/{sample.Task.Id}");

        await ReadProblemAsync(response, status);
        Assert.Equal(before, await ReadCountsAsync());
    }

    private async Task<Sample> SeedAsync(bool archived = false, bool optionalFields = true)
    {
        var creator = new AppUser(Guid.NewGuid().ToString(), "Creadora", "creator@example.com");
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro lector", "member@example.com");
        var assignee = new AppUser(Guid.NewGuid().ToString(), "Persona asignada", "assignee@example.com");
        var outsider = new AppUser(Guid.NewGuid().ToString(), "Fuera del proyecto", null);
        var project = new Project($"Proyecto {Guid.NewGuid():N}", null, creator.Id);
        var task = optionalFields
            ? new DomainTask(project.Id, $"Tarea {Guid.NewGuid():N}", TaskPriority.High,
                "Descripción de la tarea", assignee.Id, TaskState.InProgress, new DateOnly(2026, 1, 1))
            : new DomainTask(project.Id, $"Tarea {Guid.NewGuid():N}", TaskPriority.Low);
        if (archived)
        {
            project.Archive();
        }
        await using var context = CreateContext();
        context.Users.AddRange(creator, member, assignee, outsider);
        context.Projects.Add(project);
        context.ProjectMemberships.AddRange(
            new AppUserProject(project.Id, creator.Id),
            new AppUserProject(project.Id, member.Id),
            new AppUserProject(project.Id, assignee.Id));
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        // Comparar con la precisión de las fechas persistidas por PostgreSQL.
        await context.Entry(task).ReloadAsync();
        return new Sample(project, task, member, assignee, outsider);
    }

    private AuthenticationTestFactory CreateFactory() => new(postgres.Database.GetConnectionString());

    private TaskFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<TaskFlowDbContext>()
        .UseNpgsql(postgres.Database.GetConnectionString()).Options);

    private async Task<(int Users, int Projects, int Memberships, int Tasks)> ReadCountsAsync()
    {
        await using var context = CreateContext();
        return (await context.Users.CountAsync(), await context.Projects.CountAsync(),
            await context.ProjectMemberships.CountAsync(), await context.Tasks.CountAsync());
    }

    private static HttpClient CreateClient(AuthenticationTestFactory factory, AppUser user, string role = "User")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(user.KeycloakSub, [role]));
        return client;
    }

    private static async Task<TaskResponse> ReadDetailAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        return Assert.IsType<TaskResponse>(await response.Content.ReadFromJsonAsync<TaskResponse>());
    }

    private static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = Assert.IsType<ProblemDetails>(await response.Content.ReadFromJsonAsync<ProblemDetails>());
        Assert.Equal((int)status, problem.Status);
        return problem;
    }

    private sealed record Sample(Project Project, DomainTask Task, AppUser Member, AppUser Assignee, AppUser Outsider);
}
