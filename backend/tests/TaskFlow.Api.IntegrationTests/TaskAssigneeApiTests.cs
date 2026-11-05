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
using DomainTask = TaskFlow.Domain.Task;
using Project = TaskFlow.Domain.Project;
using TaskPriority = TaskFlow.Domain.TaskPriority;
using TaskState = TaskFlow.Domain.TaskState;

namespace TaskFlow.Api.IntegrationTests;

public class TaskAssigneeApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("User", false, false, false)]
    [InlineData("User", false, false, true)]
    [InlineData("User", false, true, false)]
    [InlineData("Admin", false, false, false)]
    [InlineData("Admin", true, false, false)]
    public async Task ChangeAssignee_AssignsOrReassignsAndPreservesPreviousMembershipAndOtherFields(
        string role, bool archived, bool alreadyMember, bool initiallyUnassigned)
    {
        var sample = await SeedAsync(archived, alreadyMember, initiallyUnassigned);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, role == "Admin" ? sample.Outsider : sample.Member, role);
        var before = DateTimeOffset.UtcNow.AddTicks(-10);

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), new { assigneeId = sample.Candidate.Id });

        var detail = await ReadUpdatedAsync(response);
        AssertPreservedFields(sample, detail);
        Assert.NotNull(detail.Assignee);
        Assert.Equal(sample.Candidate.Id, detail.Assignee.Id);
        Assert.Equal(sample.Candidate.DisplayName, detail.Assignee.DisplayName);
        Assert.InRange(detail.UpdatedAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(TimeSpan.Zero, detail.UpdatedAt.Offset);
        Assert.Null(response.Headers.Location);
        await using var context = CreateContext();
        var stored = await context.Tasks.SingleAsync(task => task.Id == sample.Task.Id);
        Assert.Equal(sample.Candidate.Id, stored.AssignedId);
        Assert.Equal(sample.Task.Title, stored.Title);
        Assert.Equal(sample.Task.Description, stored.Description);
        Assert.Equal(sample.Task.Priority, stored.Priority);
        Assert.Equal(sample.Task.Status, stored.Status);
        Assert.Equal(sample.Task.ProjectId, stored.ProjectId);
        Assert.Equal(sample.Task.DueDate, stored.DueDate);
        Assert.Equal(sample.Task.CreatedAt, stored.CreatedAt);
        Assert.InRange((stored.UpdatedAt - detail.UpdatedAt).Duration(), TimeSpan.Zero, TimeSpan.FromTicks(9));
        Assert.Equal(3, await context.ProjectMemberships.CountAsync(item => item.ProjectId == sample.Project.Id));
        Assert.Equal(1, await context.ProjectMemberships.CountAsync(item =>
            item.ProjectId == sample.Project.Id && item.UserId == sample.Candidate.Id));
        Assert.True(await context.ProjectMemberships.AnyAsync(item =>
            item.ProjectId == sample.Project.Id && item.UserId == sample.PreviousAssignee.Id));
        Assert.False(await context.ProjectMemberships.AnyAsync(item =>
            item.ProjectId == sample.Project.Id && item.UserId == sample.Outsider.Id));
        var project = await context.Projects.SingleAsync(item => item.Id == sample.Project.Id);
        Assert.Equal(sample.Project.ArchivedAt, project.ArchivedAt);
        Assert.Equal(sample.Project.UpdatedAt, project.UpdatedAt);

        using var getResponse = await client.GetAsync($"/api/v1/tasks/{sample.Task.Id}");
        var fetched = await ReadUpdatedAsync(getResponse);
        Assert.Equal(sample.Candidate.Id, fetched.Assignee?.Id);
        if (!archived)
        {
            using var assignedClient = CreateClient(factory, sample.Candidate);
            using var assignedResponse = await assignedClient.GetAsync($"/api/v1/projects/{sample.Project.Id}");
            Assert.Equal(HttpStatusCode.OK, assignedResponse.StatusCode);
            using var previousClient = CreateClient(factory, sample.PreviousAssignee);
            using var previousResponse = await previousClient.GetAsync($"/api/v1/projects/{sample.Project.Id}");
            Assert.Equal(HttpStatusCode.OK, previousResponse.StatusCode);
        }
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "displayName", "id" }, json.RootElement.GetProperty("assignee")
            .EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangeAssignee_ExplicitNull_UnassignsAndKeepsAllMemberships(bool initiallyUnassigned)
    {
        var sample = await SeedAsync(initiallyUnassigned: initiallyUnassigned);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), new { assigneeId = (Guid?)null });

        var detail = await ReadUpdatedAsync(response);
        AssertPreservedFields(sample, detail);
        Assert.Null(detail.Assignee);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("assignee").ValueKind);
        await using var context = CreateContext();
        var task = await context.Tasks.SingleAsync(item => item.Id == sample.Task.Id);
        Assert.Null(task.AssignedId);
        Assert.Equal(2, await context.ProjectMemberships.CountAsync(item => item.ProjectId == sample.Project.Id));
        Assert.True(await context.ProjectMemberships.AnyAsync(item =>
            item.ProjectId == sample.Project.Id && item.UserId == sample.PreviousAssignee.Id));
    }

    [Fact]
    public async Task ChangeAssignee_RepeatedSameAssignment_DoesNotDuplicateOrReplaceMembership()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        DateTimeOffset membershipCreatedAt;
        await using (var before = CreateContext())
        {
            membershipCreatedAt = (await before.ProjectMemberships.SingleAsync(item =>
                item.ProjectId == sample.Project.Id && item.UserId == sample.PreviousAssignee.Id)).CreatedAt;
        }
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var response = await client.PutAsJsonAsync(Route(sample.Task.Id),
                new { assigneeId = sample.PreviousAssignee.Id });
            var detail = await ReadUpdatedAsync(response);
            Assert.Equal(sample.PreviousAssignee.Id, detail.Assignee?.Id);
        }

        await using var context = CreateContext();
        var memberships = await context.ProjectMemberships.Where(item =>
            item.ProjectId == sample.Project.Id && item.UserId == sample.PreviousAssignee.Id).ToArrayAsync();
        Assert.Equal(membershipCreatedAt, Assert.Single(memberships).CreatedAt);
        Assert.Equal(2, await context.ProjectMemberships.CountAsync(item => item.ProjectId == sample.Project.Id));
    }

    [Fact]
    public async Task ChangeAssignee_UnknownUser_Returns400ForAssigneeIdWithoutChangingTaskOrMemberships()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), new { assigneeId = Guid.NewGuid() });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "assigneeId");
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task ChangeAssignee_EmptyGuid_Returns400ForAssigneeIdWithoutChangingTask()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), new { assigneeId = Guid.Empty });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "assigneeId");
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"assigneeId\":")]
    [InlineData("{\"assigneeId\":\"not-a-guid\"}")]
    [InlineData("{\"assigneeId\":\"\"}")]
    [InlineData("{\"assigneeId\":1}")]
    [InlineData("{\"assigneeId\":true}")]
    [InlineData("{\"assigneeId\":[]}")]
    public async Task ChangeAssignee_MissingPropertyOrInvalidJson_Returns400WithoutChangingTask(string body)
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
    [InlineData("projectId")]
    [InlineData("status")]
    [InlineData("assignedId")]
    [InlineData("unknownProperty")]
    public async Task ChangeAssignee_AdditionalField_Returns400WithoutAddingMembership(string property)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = new Dictionary<string, object?> { ["assigneeId"] = sample.Candidate.Id, [property] = "No permitido" };

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), body);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("foreign", false)]
    [InlineData("foreign", true)]
    [InlineData("archived", false)]
    [InlineData("archived", true)]
    [InlineData("missing-user", false)]
    [InlineData("missing-user", true)]
    [InlineData("missing-admin", false)]
    [InlineData("missing-admin", true)]
    public async Task ChangeAssignee_InvisibleOrMissingTask_Returns404BeforeUnknownAssigneeError(
        string scenario, bool unknownAssignee)
    {
        var sample = await SeedAsync(archived: scenario == "archived");
        using var factory = CreateFactory();
        using var client = CreateClient(factory, scenario == "foreign" ? sample.Outsider : sample.Member,
            scenario == "missing-admin" ? "Admin" : "User");
        var id = scenario.StartsWith("missing-", StringComparison.Ordinal) ? Guid.NewGuid() : sample.Task.Id;

        using var response = await client.PutAsJsonAsync(Route(id),
            new { assigneeId = unknownAssignee ? Guid.NewGuid() : sample.Candidate.Id });

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("123")]
    public async Task ChangeAssignee_InvalidTaskIdentifier_Returns400(string id)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.PutAsJsonAsync($"/api/v1/tasks/{id}/assignee",
            new { assigneeId = sample.Candidate.Id });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task ChangeAssignee_InvalidSession_RejectsRequestWithoutChangingDatabase(string session, HttpStatusCode status)
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

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), new { assigneeId = sample.Candidate.Id });

        await AssertProblemAsync(response, status);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task ChangeAssignee_ConcurrentAssignmentsOfSameUserToTwoTasks_CreateOneMembership()
    {
        var sample = await SeedAsync();
        var secondTask = new DomainTask(sample.Project.Id, "Segunda tarea", TaskPriority.Low);
        await using (var context = CreateContext())
        {
            context.Tasks.Add(secondTask);
            await context.SaveChangesAsync();
        }
        using var factory = CreateFactory();
        using var firstClient = CreateClient(factory, sample.Member);
        using var secondClient = CreateClient(factory, sample.Member);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var responses = await Task.WhenAll(
            firstClient.PutAsJsonAsync(Route(sample.Task.Id), new { assigneeId = sample.Candidate.Id }, timeout.Token),
            secondClient.PutAsJsonAsync(Route(secondTask.Id), new { assigneeId = sample.Candidate.Id }, timeout.Token));
        using var firstResponse = responses[0];
        using var secondResponse = responses[1];
        Assert.Equal(sample.Candidate.Id, (await ReadUpdatedAsync(firstResponse)).Assignee?.Id);
        Assert.Equal(sample.Candidate.Id, (await ReadUpdatedAsync(secondResponse)).Assignee?.Id);
        await using var verification = CreateContext();
        Assert.Equal(2, await verification.Tasks.CountAsync(task =>
            task.ProjectId == sample.Project.Id && task.AssignedId == sample.Candidate.Id));
        Assert.Equal(1, await verification.ProjectMemberships.CountAsync(item =>
            item.ProjectId == sample.Project.Id && item.UserId == sample.Candidate.Id));
    }

    [Fact]
    public async Task ChangeAssignee_ConcurrentDifferentAssignees_LastWriteWinsAndBothMembershipsRemain()
    {
        var sample = await SeedAsync();
        var otherCandidate = new AppUser(Guid.NewGuid().ToString(), "Otro asignado", null);
        await using (var context = CreateContext())
        {
            context.Users.Add(otherCandidate);
            await context.SaveChangesAsync();
        }
        using var factory = CreateFactory();
        using var firstClient = CreateClient(factory, sample.Member);
        using var secondClient = CreateClient(factory, sample.Member);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var responses = await Task.WhenAll(
            firstClient.PutAsJsonAsync(Route(sample.Task.Id), new { assigneeId = sample.Candidate.Id }, timeout.Token),
            secondClient.PutAsJsonAsync(Route(sample.Task.Id), new { assigneeId = otherCandidate.Id }, timeout.Token));
        using var firstResponse = responses[0];
        using var secondResponse = responses[1];
        Assert.Equal(sample.Candidate.Id, (await ReadUpdatedAsync(firstResponse)).Assignee?.Id);
        Assert.Equal(otherCandidate.Id, (await ReadUpdatedAsync(secondResponse)).Assignee?.Id);
        await using var verification = CreateContext();
        var stored = await verification.Tasks.SingleAsync(task => task.Id == sample.Task.Id);
        Assert.Contains(stored.AssignedId!.Value, new[] { sample.Candidate.Id, otherCandidate.Id });
        Assert.Equal(4, await verification.ProjectMemberships.CountAsync(item => item.ProjectId == sample.Project.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangeAssignee_FailureAfterSqlUpdate_RollsBackTaskAndNewMembership(bool unassign)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        var failure = new FailAfterAssignmentUpdateInterceptor();
        using var failingFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<TaskFlowDbContext>(options => options.AddInterceptors(failure))));
        using var client = failingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(sample.Member.KeycloakSub));

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id),
            new { assigneeId = unassign ? (Guid?)null : sample.Candidate.Id });

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.True(failure.UpdateExecuted);
        Assert.Equal(!unassign, failure.MembershipInserted);
        Assert.DoesNotContain("Fallo simulado", await response.Content.ReadAsStringAsync());
        await AssertUnchangedAsync(sample);
    }

    private async Task<Sample> SeedAsync(bool archived = false, bool alreadyMember = false, bool initiallyUnassigned = false)
    {
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro del proyecto", null);
        var previous = new AppUser(Guid.NewGuid().ToString(), "Asignado anterior", null);
        var candidate = new AppUser(Guid.NewGuid().ToString(), "Nuevo asignado", "candidate@example.com");
        var outsider = new AppUser(Guid.NewGuid().ToString(), "Otra persona", null);
        var project = new Project($"Proyecto {Guid.NewGuid():N}", null, member.Id);
        if (archived) { project.Archive(); }
        var task = new DomainTask(project.Id, "Título original", TaskPriority.High,
            "Descripción original", initiallyUnassigned ? null : previous.Id,
            TaskState.InProgress, new DateOnly(2030, 1, 1));
        await using var context = CreateContext();
        context.Users.AddRange(member, previous, candidate, outsider);
        context.Projects.Add(project);
        context.ProjectMemberships.AddRange(new AppUserProject(project.Id, member.Id),
            new AppUserProject(project.Id, previous.Id));
        if (alreadyMember) { context.ProjectMemberships.Add(new AppUserProject(project.Id, candidate.Id)); }
        context.Tasks.Add(task);
        var createdAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        context.Entry(task).Property(item => item.CreatedAt).CurrentValue = createdAt;
        context.Entry(task).Property(item => item.UpdatedAt).CurrentValue = createdAt.AddDays(1);
        await context.SaveChangesAsync();
        await context.Entry(project).ReloadAsync();
        return new Sample(project, task, member, previous, candidate, outsider);
    }

    private static void AssertPreservedFields(Sample sample, TaskResponse detail)
    {
        Assert.Equal(sample.Task.Id, detail.Id);
        Assert.Equal(sample.Project.Id, detail.ProjectId);
        Assert.Equal(sample.Project.Name, detail.ProjectName);
        Assert.Equal(sample.Task.Title, detail.Title);
        Assert.Equal(sample.Task.Description, detail.Description);
        Assert.Equal(sample.Task.Priority.ToString(), detail.Priority);
        Assert.Equal(sample.Task.Status.ToString(), detail.Status);
        Assert.Equal(sample.Task.DueDate, detail.DueDate);
        Assert.Equal(sample.Task.CreatedAt, detail.CreatedAt);
    }

    private async Task AssertUnchangedAsync(Sample sample)
    {
        await using var context = CreateContext();
        var stored = await context.Tasks.SingleAsync(task => task.Id == sample.Task.Id);
        Assert.Equal(JsonSerializer.Serialize(sample.Task), JsonSerializer.Serialize(stored));
        Assert.Equal(2, await context.ProjectMemberships.CountAsync(item => item.ProjectId == sample.Project.Id));
        Assert.False(await context.ProjectMemberships.AnyAsync(item =>
            item.ProjectId == sample.Project.Id && item.UserId == sample.Candidate.Id));
    }

    private static string Route(Guid taskId) => $"/api/v1/tasks/{taskId}/assignee";
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

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string? field = null)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
        if (status == HttpStatusCode.BadRequest)
        {
            var errors = json.RootElement.GetProperty("errors");
            Assert.NotEmpty(errors.EnumerateObject());
            if (field is not null)
            {
                Assert.True(errors.TryGetProperty(field, out var messages), $"Falta el error del campo {field}.");
                Assert.NotEmpty(messages.EnumerateArray());
            }
        }
    }

    private sealed class FailAfterAssignmentUpdateInterceptor : DbCommandInterceptor
    {
        internal bool MembershipInserted { get; private set; }
        internal bool UpdateExecuted { get; private set; }
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            CheckForUpdate(command);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO app_user_project", StringComparison.Ordinal))
            {
                MembershipInserted = true;
            }
            CheckForUpdate(command);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void CheckForUpdate(DbCommand command)
        {
            if (command.CommandText.Contains("UPDATE task SET", StringComparison.Ordinal) ||
                command.CommandText.Contains("UPDATE \"task\" SET", StringComparison.Ordinal))
            {
                UpdateExecuted = true;
                throw new InvalidOperationException("Fallo simulado después de modificar el asignado.");
            }
        }
    }

    private sealed record Sample(Project Project, DomainTask Task, AppUser Member,
        AppUser PreviousAssignee, AppUser Candidate, AppUser Outsider);
}
