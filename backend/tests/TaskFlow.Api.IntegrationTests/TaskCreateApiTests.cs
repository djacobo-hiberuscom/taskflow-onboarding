using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Infrastructure.Persistence;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using Project = TaskFlow.Domain.Project;

namespace TaskFlow.Api.IntegrationTests;

public class TaskCreateApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("User", false, false, "ToDo", "Low")]
    [InlineData("User", false, true, "InProgress", "Medium")]
    [InlineData("Admin", false, false, "Done", "High")]
    [InlineData("Admin", true, false, "InProgress", "High")]
    public async Task Create_ValidAssignment_PersistsTaskAndOneMembershipWithWorkingLocation(
        string role, bool archived, bool alreadyMember, string status, string priority)
    {
        var sample = await SeedAsync(archived, alreadyMember);
        using var factory = CreateFactory();
        var caller = role == "Admin" ? sample.Outsider : sample.Member;
        using var client = CreateClient(factory, caller, role);
        var body = ValidBody(sample.Assignee.Id);
        body["title"] = "  Tarea con asignado  ";
        body["description"] = "  Descripción limpia  ";
        body["status"] = status;
        body["priority"] = priority;
        body["dueDate"] = "2000-01-01";
        var before = DateTimeOffset.UtcNow;

        using var response = await client.PostAsJsonAsync(Route(sample.Project.Id), body);

        var detail = await ReadCreatedAsync(response);
        Assert.Equal(sample.Project.Id, detail.ProjectId);
        Assert.Equal(sample.Project.Name, detail.ProjectName);
        Assert.Equal("Tarea con asignado", detail.Title);
        Assert.Equal("Descripción limpia", detail.Description);
        Assert.Equal(status, detail.Status);
        Assert.Equal(priority, detail.Priority);
        Assert.Equal(new DateOnly(2000, 1, 1), detail.DueDate);
        Assert.NotNull(detail.Assignee);
        Assert.Equal(sample.Assignee.Id, detail.Assignee.Id);
        Assert.Equal(sample.Assignee.DisplayName, detail.Assignee.DisplayName);
        Assert.InRange(detail.CreatedAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(detail.CreatedAt, detail.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, detail.CreatedAt.Offset);
        await using var context = CreateContext();
        var stored = await context.Tasks.SingleAsync(task => task.Id == detail.Id);
        Assert.Equal(detail.Title, stored.Title);
        Assert.Equal(detail.Description, stored.Description);
        Assert.Equal(sample.Assignee.Id, stored.AssignedId);
        Assert.Equal(sample.Project.Id, stored.ProjectId);
        Assert.Equal(status, stored.Status.ToString());
        Assert.Equal(priority, stored.Priority.ToString());
        Assert.Equal(detail.DueDate, stored.DueDate);
        Assert.Equal(stored.CreatedAt, stored.UpdatedAt);
        Assert.InRange((stored.CreatedAt - detail.CreatedAt).Duration(), TimeSpan.Zero, TimeSpan.FromTicks(9));
        Assert.Equal(1, await context.ProjectMemberships.CountAsync(membership =>
            membership.ProjectId == sample.Project.Id && membership.UserId == sample.Assignee.Id));
        Assert.False(await context.ProjectMemberships.AnyAsync(membership =>
            membership.ProjectId == sample.Project.Id && membership.UserId == sample.Outsider.Id));

        using var getResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = Assert.IsType<TaskResponse>(await getResponse.Content.ReadFromJsonAsync<TaskResponse>());
        Assert.Equal(detail.Id, fetched.Id);
        Assert.Equal(detail.Assignee.Id, fetched.Assignee?.Id);
        if (!archived)
        {
            // La asignación da visibilidad a alguien que antes no pertenecía al proyecto.
            using var assignedClient = CreateClient(factory, sample.Assignee);
            using var assignedResponse = await assignedClient.GetAsync($"/api/v1/tasks/{detail.Id}");
            Assert.Equal(HttpStatusCode.OK, assignedResponse.StatusCode);
        }
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "displayName", "id" }, json.RootElement.GetProperty("assignee")
            .EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
    }

    [Theory]
    [InlineData("omitted")]
    [InlineData("null")]
    [InlineData("whitespace")]
    public async Task Create_WithoutAssignment_DefaultsStatusAndNormalizesOptionalFields(string descriptionCase)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = ValidBody();
        if (descriptionCase != "omitted")
        {
            body["description"] = descriptionCase == "null" ? null : " \t\r\n ";
            body["assigneeId"] = null;
            body["dueDate"] = null;
        }

        using var response = await client.PostAsJsonAsync(Route(sample.Project.Id), body);

        var detail = await ReadCreatedAsync(response);
        Assert.Equal("ToDo", detail.Status);
        Assert.Null(detail.Assignee);
        Assert.Null(detail.Description);
        Assert.Null(detail.DueDate);
        await using var context = CreateContext();
        Assert.Equal(1, await context.ProjectMemberships.CountAsync(membership =>
            membership.ProjectId == sample.Project.Id));
        var stored = await context.Tasks.SingleAsync(task => task.Id == detail.Id);
        Assert.Null(stored.AssignedId);
        Assert.Null(stored.Description);
        Assert.Null(stored.DueDate);
    }

    [Fact]
    public async Task Create_MaximumTextLengthsAfterTrimming_AreAccepted()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = ValidBody();
        body["title"] = $"  {new string('t', 200)}  ";
        body["description"] = $" \t{new string('d', 5000)}\r\n ";

        using var response = await client.PostAsJsonAsync(Route(sample.Project.Id), body);

        var detail = await ReadCreatedAsync(response);
        Assert.Equal(new string('t', 200), detail.Title);
        Assert.Equal(new string('d', 5000), detail.Description);
    }

    [Theory]
    [InlineData("title", "empty")]
    [InlineData("title", "whitespace")]
    [InlineData("title", "too-long")]
    [InlineData("description", "too-long")]
    [InlineData("status", "Unknown")]
    [InlineData("status", "InProress")]
    [InlineData("status", "todo")]
    [InlineData("status", "1")]
    [InlineData("priority", "Unknown")]
    [InlineData("priority", "high")]
    [InlineData("priority", "1")]
    [InlineData("assigneeId", "empty-guid")]
    public async Task Create_InvalidField_ReturnsFieldErrorWithoutWriting(string field, string scenario)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = ValidBody(sample.Assignee.Id);
        body[field] = scenario switch
        {
            "empty" => "",
            "whitespace" => " \t\r\n ",
            "too-long" => new string('x', field == "title" ? 201 : 5001),
            "empty-guid" => Guid.Empty,
            _ => scenario
        };
        var before = await ReadCountsAsync();

        using var response = await client.PostAsJsonAsync(Route(sample.Project.Id), body);

        await AssertValidationAsync(response, field);
        Assert.Equal(before, await ReadCountsAsync());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"title\":")]
    [InlineData("{\"title\":\"Tarea\"}")]
    [InlineData("{\"priority\":\"High\"}")]
    [InlineData("{\"title\":null,\"priority\":\"High\"}")]
    [InlineData("{\"title\":42,\"priority\":\"High\"}")]
    [InlineData("{\"title\":\"Tarea\",\"priority\":null}")]
    [InlineData("{\"title\":\"Tarea\",\"priority\":2}")]
    [InlineData("{\"title\":\"Tarea\",\"priority\":\"High\",\"status\":null}")]
    [InlineData("{\"title\":\"Tarea\",\"priority\":\"High\",\"status\":1}")]
    [InlineData("{\"title\":\"Tarea\",\"priority\":\"High\",\"description\":42}")]
    [InlineData("{\"title\":\"Tarea\",\"priority\":\"High\",\"dueDate\":\"2026-02-30\"}")]
    [InlineData("{\"title\":\"Tarea\",\"priority\":\"High\",\"assigneeId\":\"not-a-guid\"}")]
    public async Task Create_InvalidJsonOrRequiredField_Returns400WithoutWriting(string body)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var before = await ReadCountsAsync();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(Route(sample.Project.Id), content);

        await AssertValidationAsync(response);
        Assert.Equal(before, await ReadCountsAsync());
    }

    [Theory]
    [InlineData("projectId")]
    [InlineData("assignedId")]
    [InlineData("createdAt")]
    [InlineData("unknownProperty")]
    public async Task Create_UnknownOrProtectedField_Returns400WithoutWriting(string property)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = ValidBody(sample.Assignee.Id);
        body[property] = Guid.NewGuid();
        var before = await ReadCountsAsync();

        using var response = await client.PostAsJsonAsync(Route(sample.Project.Id), body);

        await AssertValidationAsync(response);
        Assert.Equal(before, await ReadCountsAsync());
    }

    [Theory]
    [InlineData("foreign", false)]
    [InlineData("foreign", true)]
    [InlineData("archived", false)]
    [InlineData("archived", true)]
    [InlineData("missing", false)]
    [InlineData("missing", true)]
    public async Task Create_InvisibleProject_Returns404EvenWhenAssigneeDoesNotExist(string scenario, bool unknownAssignee)
    {
        var sample = await SeedAsync(archived: scenario == "archived");
        using var factory = CreateFactory();
        using var client = CreateClient(factory, scenario == "foreign" ? sample.Outsider : sample.Member);
        var projectId = scenario == "missing" ? Guid.NewGuid() : sample.Project.Id;
        var before = await ReadCountsAsync();

        using var response = await client.PostAsJsonAsync(Route(projectId),
            ValidBody(unknownAssignee ? Guid.NewGuid() : sample.Assignee.Id));

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Equal(before, await ReadCountsAsync());
    }

    [Fact]
    public async Task Create_UnknownAssigneeInVisibleProject_Returns400ForAssigneeWithoutWriting()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var before = await ReadCountsAsync();

        using var response = await client.PostAsJsonAsync(Route(sample.Project.Id), ValidBody(Guid.NewGuid()));

        await AssertValidationAsync(response, "assigneeId");
        Assert.Equal(before, await ReadCountsAsync());
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task Create_InvalidSession_ReturnsProblemWithoutWriting(string session, HttpStatusCode status)
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

        using var response = await client.PostAsJsonAsync(Route(sample.Project.Id), ValidBody(sample.Assignee.Id));

        await ReadProblemAsync(response, status);
        Assert.Equal(before, await ReadCountsAsync());
    }

    [Fact]
    public async Task Create_ConcurrentTasksWithSameTitleAndAssignee_CreateTwoTasksAndOneMembership()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var firstClient = CreateClient(factory, sample.Member);
        using var secondClient = CreateClient(factory, sample.Member);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var responses = await Task.WhenAll(
            firstClient.PostAsJsonAsync(Route(sample.Project.Id), ValidBody(sample.Assignee.Id), timeout.Token),
            secondClient.PostAsJsonAsync(Route(sample.Project.Id), ValidBody(sample.Assignee.Id), timeout.Token));
        using var firstResponse = responses[0];
        using var secondResponse = responses[1];
        var first = await ReadCreatedAsync(firstResponse);
        var second = await ReadCreatedAsync(secondResponse);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.Title, second.Title);
        await using var context = CreateContext();
        Assert.Equal(2, await context.Tasks.CountAsync(task => task.ProjectId == sample.Project.Id));
        Assert.Equal(1, await context.ProjectMemberships.CountAsync(membership =>
            membership.ProjectId == sample.Project.Id && membership.UserId == sample.Assignee.Id));
    }

    [Fact]
    public async Task Create_TaskInsertFails_RollsBackPreviouslyInsertedMembership()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        var interceptor = new RejectTaskInsertInterceptor();
        using var failingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddDbContext<TaskFlowDbContext>(options =>
                options.UseNpgsql(postgres.Database.GetConnectionString()).AddInterceptors(interceptor))));
        using var client = failingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(sample.Member.KeycloakSub, ["User"]));
        var before = await ReadCountsAsync();

        using var response = await client.PostAsJsonAsync(Route(sample.Project.Id), ValidBody(sample.Assignee.Id));

        await ReadProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.True(interceptor.MembershipInsertCompleted);
        Assert.True(interceptor.TaskInsertRejected);
        Assert.DoesNotContain("Fallo de prueba", await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await ReadCountsAsync());
    }

    private async Task<Sample> SeedAsync(bool archived = false, bool alreadyMember = false)
    {
        var member = new AppUser(Guid.NewGuid().ToString(), "Creadora del proyecto", null);
        var assignee = new AppUser(Guid.NewGuid().ToString(), "Persona asignada", "assignee@example.com");
        var outsider = new AppUser(Guid.NewGuid().ToString(), "Administrador ajeno", null);
        var project = new Project($"Proyecto {Guid.NewGuid():N}", null, member.Id);
        if (archived)
        {
            project.Archive();
        }
        await using var context = CreateContext();
        context.Users.AddRange(member, assignee, outsider);
        context.Projects.Add(project);
        context.ProjectMemberships.Add(new AppUserProject(project.Id, member.Id));
        if (alreadyMember)
        {
            context.ProjectMemberships.Add(new AppUserProject(project.Id, assignee.Id));
        }
        await context.SaveChangesAsync();
        return new Sample(project, member, assignee, outsider);
    }

    private static Dictionary<string, object?> ValidBody(Guid? assigneeId = null)
    {
        var body = new Dictionary<string, object?> { ["title"] = "Tarea de prueba", ["priority"] = "High" };
        if (assigneeId.HasValue)
        {
            body["assigneeId"] = assigneeId.Value;
        }
        return body;
    }

    private static string Route(Guid projectId) => $"/api/v1/projects/{projectId}/tasks";

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

    private async Task<(int Users, int Tasks, int Memberships)> ReadCountsAsync()
    {
        await using var context = CreateContext();
        return (await context.Users.CountAsync(), await context.Tasks.CountAsync(),
            await context.ProjectMemberships.CountAsync());
    }

    private static async Task<TaskResponse> ReadCreatedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var detail = Assert.IsType<TaskResponse>(await response.Content.ReadFromJsonAsync<TaskResponse>());
        Assert.NotEqual(Guid.Empty, detail.Id);
        Assert.Equal($"/api/v1/tasks/{detail.Id}", response.Headers.Location?.OriginalString);
        return detail;
    }

    private static async Task<Microsoft.AspNetCore.Mvc.ProblemDetails> ReadProblemAsync(
        HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(
            await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>());
        Assert.Equal((int)status, problem.Status);
        return problem;
    }

    private static async Task AssertValidationAsync(HttpResponseMessage response, string? field = null)
    {
        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = json.RootElement.GetProperty("errors");
        Assert.NotEmpty(errors.EnumerateObject());
        if (field is not null)
        {
            Assert.True(errors.TryGetProperty(field, out var messages), $"Falta el error del campo {field}.");
            Assert.NotEmpty(messages.EnumerateArray());
        }
    }

    private sealed class RejectTaskInsertInterceptor : DbCommandInterceptor
    {
        internal bool MembershipInsertCompleted { get; private set; }
        internal bool TaskInsertRejected { get; private set; }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO app_user_project", StringComparison.Ordinal))
            {
                MembershipInsertCompleted = true;
            }
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            RejectTaskInsert(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            RejectTaskInsert(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void RejectTaskInsert(DbCommand command)
        {
            if (command.CommandText.Contains("INSERT INTO task ", StringComparison.Ordinal) ||
                command.CommandText.Contains("INSERT INTO \"task\" ", StringComparison.Ordinal))
            {
                TaskInsertRejected = true;
                throw new InvalidOperationException("Fallo de prueba al insertar la tarea.");
            }
        }
    }

    private sealed record Sample(Project Project, AppUser Member, AppUser Assignee, AppUser Outsider);
}
