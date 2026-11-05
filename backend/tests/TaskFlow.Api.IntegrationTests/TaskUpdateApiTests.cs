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
using Npgsql;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Repositories;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using DomainTask = TaskFlow.Domain.Task;
using Project = TaskFlow.Domain.Project;
using TaskPriority = TaskFlow.Domain.TaskPriority;
using TaskState = TaskFlow.Domain.TaskState;

namespace TaskFlow.Api.IntegrationTests;

public class TaskUpdateApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("User", false, "Low")]
    [InlineData("Admin", false, "Medium")]
    [InlineData("Admin", true, "High")]
    public async Task Update_VisibleTask_UpdatesDetailsAndPreservesProjectStatusAssignmentAndCreation(
        string role, bool archived, string priority)
    {
        var sample = await SeedAsync(archived);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, role == "Admin" ? sample.Outsider : sample.Member, role);
        var body = ValidBody();
        body["priority"] = priority;
        body["dueDate"] = "2000-01-01";
        var before = DateTimeOffset.UtcNow.AddTicks(-10);

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), body);

        var detail = await ReadUpdatedAsync(response);
        Assert.Equal(sample.Task.Id, detail.Id);
        Assert.Equal(sample.Project.Id, detail.ProjectId);
        Assert.Equal(sample.Project.Name, detail.ProjectName);
        Assert.Equal("Título actualizado", detail.Title);
        Assert.Equal("Descripción actualizada", detail.Description);
        Assert.Equal(priority, detail.Priority);
        Assert.Equal(new DateOnly(2000, 1, 1), detail.DueDate);
        Assert.Equal("InProgress", detail.Status);
        Assert.Equal(sample.Task.CreatedAt, detail.CreatedAt);
        Assert.InRange(detail.UpdatedAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(TimeSpan.Zero, detail.UpdatedAt.Offset);
        Assert.NotNull(detail.Assignee);
        Assert.Equal(sample.Assignee.Id, detail.Assignee.Id);
        Assert.Equal(sample.Assignee.DisplayName, detail.Assignee.DisplayName);
        Assert.Null(response.Headers.Location);

        await using var context = CreateContext();
        var stored = await context.Tasks.SingleAsync(task => task.Id == sample.Task.Id);
        Assert.Equal(detail.Title, stored.Title);
        Assert.Equal(detail.Description, stored.Description);
        Assert.Equal(detail.Priority, stored.Priority.ToString());
        Assert.Equal(detail.DueDate, stored.DueDate);
        Assert.Equal(sample.Task.ProjectId, stored.ProjectId);
        Assert.Equal(sample.Task.Status, stored.Status);
        Assert.Equal(sample.Task.AssignedId, stored.AssignedId);
        Assert.Equal(sample.Task.CreatedAt, stored.CreatedAt);
        Assert.InRange((stored.UpdatedAt - detail.UpdatedAt).Duration(), TimeSpan.Zero, TimeSpan.FromTicks(9));
        Assert.Equal(2, await context.ProjectMemberships.CountAsync(item => item.ProjectId == sample.Project.Id));
        var project = await context.Projects.SingleAsync(item => item.Id == sample.Project.Id);
        Assert.Equal(sample.Project.Name, project.Name);
        Assert.Equal(sample.Project.UpdatedAt, project.UpdatedAt);
        Assert.Equal(sample.Project.ArchivedAt, project.ArchivedAt);

        using var getResponse = await client.GetAsync(Route(sample.Task.Id));
        var fetched = await ReadUpdatedAsync(getResponse);
        Assert.Equal(detail.Title, fetched.Title);
        Assert.Equal(stored.UpdatedAt, fetched.UpdatedAt);
    }

    [Theory]
    [InlineData("omitted")]
    [InlineData("null")]
    [InlineData("whitespace")]
    public async Task Update_EmptyOptionalFields_ClearPreviousDescriptionAndDueDate(string scenario)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = new Dictionary<string, object?> { ["title"] = "Título válido", ["priority"] = "Medium" };
        if (scenario != "omitted")
        {
            body["description"] = scenario == "null" ? null : " \t\r\n ";
            body["dueDate"] = null;
        }

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), body);

        var detail = await ReadUpdatedAsync(response);
        Assert.Null(detail.Description);
        Assert.Null(detail.DueDate);
        Assert.Equal(sample.Assignee.Id, detail.Assignee?.Id);
        await using var context = CreateContext();
        var stored = await context.Tasks.SingleAsync(task => task.Id == sample.Task.Id);
        Assert.Null(stored.Description);
        Assert.Null(stored.DueDate);
    }

    [Fact]
    public async Task Update_TextAtMaximumLengthsAfterTrimming_IsAccepted()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = ValidBody();
        body["title"] = $"  {new string('t', 200)}  ";
        body["description"] = $" \t{new string('d', 5000)}\r\n ";

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), body);

        var detail = await ReadUpdatedAsync(response);
        Assert.Equal(new string('t', 200), detail.Title);
        Assert.Equal(new string('d', 5000), detail.Description);
    }

    [Theory]
    [InlineData("title", "empty")]
    [InlineData("title", "whitespace")]
    [InlineData("title", "too-long")]
    [InlineData("description", "too-long")]
    [InlineData("priority", "Unknown")]
    [InlineData("priority", "high")]
    [InlineData("priority", "1")]
    public async Task Update_InvalidField_Returns400AndPreservesAllStoredValues(string field, string scenario)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = ValidBody();
        body[field] = scenario switch
        {
            "empty" => "",
            "whitespace" => " \t\r\n ",
            "too-long" => new string('x', field == "title" ? 201 : 5001),
            _ => scenario
        };

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), body);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, field);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"title\":")]
    [InlineData("{\"title\":\"Título\"}")]
    [InlineData("{\"priority\":\"High\"}")]
    [InlineData("{\"title\":null,\"priority\":\"High\"}")]
    [InlineData("{\"title\":\"Título\",\"priority\":null}")]
    [InlineData("{\"title\":\"Título\",\"priority\":2}")]
    [InlineData("{\"title\":\"Título\",\"priority\":\"High\",\"dueDate\":\"2026-02-30\"}")]
    public async Task Update_InvalidJsonOrRequiredField_Returns400WithoutChangingTask(string body)
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
    [InlineData("assigneeId")]
    [InlineData("createdAt")]
    [InlineData("unknownProperty")]
    public async Task Update_UnknownOrProtectedField_Returns400WithoutChangingTask(string property)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = ValidBody();
        body[property] = property == "status" ? "Done" : Guid.NewGuid().ToString();

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), body);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("archived")]
    [InlineData("missing-user")]
    [InlineData("missing-admin")]
    public async Task Update_InvisibleOrMissingTask_Returns404WithoutChangingTask(string scenario)
    {
        var sample = await SeedAsync(archived: scenario == "archived");
        using var factory = CreateFactory();
        using var client = CreateClient(factory, scenario == "foreign" ? sample.Outsider : sample.Member,
            scenario == "missing-admin" ? "Admin" : "User");
        var id = scenario.StartsWith("missing-", StringComparison.Ordinal) ? Guid.NewGuid() : sample.Task.Id;

        using var response = await client.PutAsJsonAsync(Route(id), ValidBody());

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("123")]
    public async Task Update_InvalidTaskIdentifier_Returns400(string id)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.PutAsJsonAsync($"/api/v1/tasks/{id}", ValidBody());

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task Update_InvalidSession_RejectsRequestWithoutChanges(string session, HttpStatusCode status)
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

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), ValidBody());

        await AssertProblemAsync(response, status);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task Update_FailureAfterSqlUpdate_RollsBackTaskChanges()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        var failure = new FailAfterTaskUpdateInterceptor();
        using var failingFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<TaskFlowDbContext>(options => options.AddInterceptors(failure))));
        using var client = failingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(sample.Member.KeycloakSub));

        using var response = await client.PutAsJsonAsync(Route(sample.Task.Id), ValidBody());

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.True(failure.UpdateExecuted);
        Assert.DoesNotContain("Fallo simulado", await response.Content.ReadAsStringAsync());
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("archive", "details")]
    [InlineData("delete-project", "details")]
    [InlineData("delete-task", "details")]
    [InlineData("edit-task", "details")]
    [InlineData("archive", "status")]
    [InlineData("delete-project", "status")]
    [InlineData("delete-task", "status")]
    [InlineData("edit-task", "status")]
    [InlineData("archive", "assignee")]
    [InlineData("delete-project", "assignee")]
    [InlineData("delete-task", "assignee")]
    [InlineData("edit-task", "assignee")]
    public async Task Write_WaitsForProjectLockAndReadsCurrentTaskAndPermissions(string operation, string writeKind)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        var attempt = new LockAttemptInterceptor();
        using var observedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<TaskFlowDbContext>(options => options.AddInterceptors(attempt))));
        using var client = observedFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(sample.Member.KeycloakSub));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var holder = CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync(timeout.Token);
        var project = await new ProjectRepository(holder).GetByIdForUpdateAsync(sample.Project.Id, timeout.Token);
        Assert.NotNull(project);

        var route = Route(sample.Task.Id) + (writeKind == "details" ? "" : $"/{writeKind}");
        object body = writeKind switch
        {
            "status" => new { status = "ToDo" },
            "assignee" => new { assigneeId = sample.Assignee.Id },
            _ => ValidBody()
        };
        var update = client.PutAsJsonAsync(route, body, timeout.Token);
        try
        {
            var pid = await attempt.Started.Task.WaitAsync(timeout.Token);
            await WaitUntilBlockedAsync(pid, update, timeout.Token);
            if (operation == "archive")
            {
                project.Archive();
            }
            else if (operation == "delete-project")
            {
                holder.Projects.Remove(project);
            }
            else
            {
                var task = await holder.Tasks.SingleAsync(item => item.Id == sample.Task.Id, timeout.Token);
                if (operation == "delete-task")
                {
                    holder.Tasks.Remove(task);
                }
                else
                {
                    task.ChangeStatus(TaskState.Done);
                    task.ChangeAssignee(null);
                }
            }
            await holder.SaveChangesAsync(timeout.Token);
            await transaction.CommitAsync(timeout.Token);

            using var response = await update;
            if (operation == "edit-task")
            {
                var detail = await ReadUpdatedAsync(response);
                Assert.Equal(writeKind == "status" ? "ToDo" : "Done", detail.Status);
                Assert.Equal(writeKind == "assignee" ? (Guid?)sample.Assignee.Id : null, detail.Assignee?.Id);
                Assert.Equal(writeKind == "details" ? "Título actualizado" : sample.Task.Title, detail.Title);
                await using var verification = CreateContext();
                var stored = await verification.Tasks.SingleAsync(item => item.Id == sample.Task.Id);
                Assert.Equal(writeKind == "status" ? TaskState.ToDo : TaskState.Done, stored.Status);
                Assert.Equal(writeKind == "assignee" ? (Guid?)sample.Assignee.Id : null, stored.AssignedId);
                Assert.Equal(detail.Title, stored.Title);
            }
            else
            {
                await AssertProblemAsync(response, HttpStatusCode.NotFound);
                if (operation == "archive")
                {
                    await AssertUnchangedAsync(sample);
                }
                else
                {
                    await using var verification = CreateContext();
                    Assert.False(await verification.Tasks.AnyAsync(item => item.Id == sample.Task.Id));
                }
            }
        }
        finally
        {
            if (holder.Database.CurrentTransaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            timeout.Cancel();
            try { using var ignored = await update; } catch (Exception) { }
        }
    }

    private async Task WaitUntilBlockedAsync(int pid, Task operation, CancellationToken cancellationToken)
    {
        await using var inspection = CreateContext();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        while (true)
        {
            Assert.False(operation.IsCompleted, "La edición terminó sin esperar el bloqueo del proyecto.");
            var waiting = await inspection.Database.SqlQuery<int>($"""
                SELECT COUNT(*)::integer AS "Value" FROM pg_stat_activity
                WHERE pid = {pid} AND wait_event_type = 'Lock'
                """).SingleAsync(deadline.Token);
            if (waiting == 1) { return; }
            await Task.Delay(25, deadline.Token);
        }
    }

    private async Task<Sample> SeedAsync(bool archived = false)
    {
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro editor", null);
        var assignee = new AppUser(Guid.NewGuid().ToString(), "Responsable de tarea", null);
        var outsider = new AppUser(Guid.NewGuid().ToString(), "Otra persona", null);
        var project = new Project($"Proyecto {Guid.NewGuid():N}", null, member.Id);
        if (archived) { project.Archive(); }
        var task = new DomainTask(project.Id, "Título inicial", TaskPriority.Medium,
            "Descripción inicial", assignee.Id, TaskState.InProgress, new DateOnly(2030, 6, 1));
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

    private static Dictionary<string, object?> ValidBody() => new()
    {
        ["title"] = "  Título actualizado  ", ["description"] = "  Descripción actualizada  ",
        ["priority"] = "High", ["dueDate"] = "2031-01-01"
    };

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
        if (field is not null)
        {
            Assert.NotEmpty(json.RootElement.GetProperty("errors").GetProperty(field).EnumerateArray());
        }
    }

    private sealed class LockAttemptInterceptor : DbCommandInterceptor
    {
        internal TaskCompletionSource<int> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal))
            {
                Started.TrySetResult(((NpgsqlConnection)command.Connection!).ProcessID);
            }
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class FailAfterTaskUpdateInterceptor : DbCommandInterceptor
    {
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
            CheckForUpdate(command);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void CheckForUpdate(DbCommand command)
        {
            if (command.CommandText.Contains("UPDATE task SET", StringComparison.Ordinal) ||
                command.CommandText.Contains("UPDATE \"task\" SET", StringComparison.Ordinal))
            {
                UpdateExecuted = true;
                throw new InvalidOperationException("Fallo simulado después del UPDATE de tarea.");
            }
        }
    }

    private sealed record Sample(Project Project, DomainTask Task, AppUser Member, AppUser Assignee, AppUser Outsider);
}
