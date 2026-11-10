using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskFlow.Api.Contracts.Dashboard;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Infrastructure.Persistence;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using DomainTask = TaskFlow.Domain.Task;
using Project = TaskFlow.Domain.Project;
using TaskPriority = TaskFlow.Domain.TaskPriority;
using TaskState = TaskFlow.Domain.TaskState;

namespace TaskFlow.Api.IntegrationTests;

public class DashboardApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static readonly DateOnly Today = new(2026, 11, 10);

    [Theory]
    [InlineData("User", 2, 5, 1, 2)]
    [InlineData("Admin", 3, 6, 2, 3)]
    public async Task Get_CountsVisibleActiveProjectsAndAllTheirTasks(
        string role, int projects, int open, int completed, int overdue)
    {
        var sample = await SeedAsync();
        using var authentication = CreateFactory();
        using var host = WithClock(authentication, new FixedClock("2026-11-10T12:00:00Z"));
        using var client = CreateClient(host, authentication, sample.Current, role);
        var before = await SnapshotAsync();

        // La identidad y los permisos proceden del token, nunca de la query string.
        var result = await ReadDashboardAsync(client,
            $"/api/v1/dashboard?userId={sample.Other.Id}&isAdmin=true&businessDate=2099-01-01");

        AssertCounts(result, projects, open, completed, overdue);
        Assert.Equal(Today, result.BusinessDate);
        Assert.Equal("Europe/Madrid", result.BusinessTimeZone);
        Assert.Equal(before, await SnapshotAsync());
        Assert.Equal(1, authentication.Introspection.RequestCount);
    }

    [Fact]
    public async Task Get_UserWithNoMemberships_ReceivesZeroCounts()
    {
        var sample = await SeedAsync();
        using var authentication = CreateFactory();
        using var host = WithClock(authentication, new FixedClock("2026-11-10T12:00:00Z"));
        using var client = CreateClient(host, authentication, sample.Outsider);

        AssertCounts(await ReadDashboardAsync(client), 0, 0, 0, 0);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task Get_EmptyDatabase_ReturnsZeroCounts(string role)
    {
        await ResetAsync();
        using var authentication = CreateFactory();
        using var host = WithClock(authentication, new FixedClock("2026-11-10T12:00:00Z"));
        using var client = CreateClient(host, authentication,
            new AppUser(Guid.NewGuid().ToString(), "New user", null), role);

        AssertCounts(await ReadDashboardAsync(client), 0, 0, 0, 0);
    }

    [Theory]
    [InlineData("2026-01-10T22:59:59Z", "2026-01-10")]
    [InlineData("2026-01-10T23:00:00Z", "2026-01-11")]
    [InlineData("2026-07-10T21:59:59Z", "2026-07-10")]
    [InlineData("2026-07-10T22:00:00Z", "2026-07-11")]
    [InlineData("2026-03-28T22:30:00Z", "2026-03-28")]
    [InlineData("2026-03-29T22:30:00Z", "2026-03-30")]
    [InlineData("2026-10-24T22:30:00Z", "2026-10-25")]
    [InlineData("2026-10-25T22:30:00Z", "2026-10-25")]
    public async Task Get_UsesMadridCalendarDateAcrossMidnightAndDaylightSaving(
        string utcInstant, string expectedDate)
    {
        await ResetAsync();
        var date = DateOnly.ParseExact(expectedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var user = new AppUser(Guid.NewGuid().ToString(), "Calendar user", null);
        var project = new Project("Calendar project", null, user.Id);
        await using (var context = CreateContext())
        {
            context.Users.Add(user);
            context.Projects.Add(project);
            context.ProjectMemberships.Add(new AppUserProject(project.Id, user.Id));
            context.Tasks.AddRange(
                NewTask(project, "Yesterday", TaskState.ToDo, date.AddDays(-1)),
                NewTask(project, "Today", TaskState.InProgress, date),
                NewTask(project, "Tomorrow", TaskState.ToDo, date.AddDays(1)),
                NewTask(project, "Without date", TaskState.ToDo, null),
                NewTask(project, "Completed yesterday", TaskState.Done, date.AddDays(-1)));
            await context.SaveChangesAsync();
        }
        using var authentication = CreateFactory();
        using var host = WithClock(authentication, new FixedClock(utcInstant));
        using var client = CreateClient(host, authentication, user);

        var result = await ReadDashboardAsync(client);

        Assert.Equal(date, result.BusinessDate);
        Assert.Equal("Europe/Madrid", result.BusinessTimeZone);
        AssertCounts(result, 1, 4, 1, 1);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("expired-token", HttpStatusCode.Unauthorized)]
    [InlineData("wrong-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("provider-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task Get_InvalidSession_DoesNotExposeCountsOrWrite(
        string session, HttpStatusCode status)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        if (session != "missing-token")
        {
            var token = session == "invalid-token" ? "invalid-token" : factory.CreateToken(
                sample.Current.KeycloakSub, session == "wrong-role" ? ["Other"] : ["User"],
                expires: session == "expired-token" ? DateTime.UtcNow.AddMinutes(-1) : null);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        if (session is "inactive-token" or "provider-unavailable")
            factory.Introspection.Respond = _ => Task.FromResult(new HttpResponseMessage(
                session == "inactive-token" ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)
            { Content = new StringContent("{\"active\":false}") });
        var before = await SnapshotAsync();

        using var response = await client.GetAsync("/api/v1/dashboard");

        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = Assert.IsType<ProblemDetails>(await response.Content.ReadFromJsonAsync<ProblemDetails>());
        Assert.Equal((int)status, problem.Status);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Fact]
    public async Task Get_ReflectsHttpMutationsAndTheNextBusinessDayWithoutRestarting()
    {
        await ResetAsync();
        var admin = new AppUser(Guid.NewGuid().ToString(), "Admin", null);
        var clock = new FixedClock("2026-11-10T22:59:59Z");
        using var authentication = CreateFactory();
        using var host = WithClock(authentication, clock);
        using var client = CreateClient(host, authentication, admin, "Admin");
        AssertCounts(await ReadDashboardAsync(client), 0, 0, 0, 0);

        using var createProject = await client.PostAsJsonAsync("/api/v1/projects", new { name = "Live counts" });
        Assert.Equal(HttpStatusCode.Created, createProject.StatusCode);
        var project = Assert.IsType<ProjectResponse>(await createProject.Content.ReadFromJsonAsync<ProjectResponse>());
        AssertCounts(await ReadDashboardAsync(client), 1, 0, 0, 0);

        using var createTask = await client.PostAsJsonAsync($"/api/v1/projects/{project.Id}/tasks",
            new { title = "Due today", priority = "Medium", dueDate = Today });
        Assert.Equal(HttpStatusCode.Created, createTask.StatusCode);
        var task = Assert.IsType<TaskResponse>(await createTask.Content.ReadFromJsonAsync<TaskResponse>());
        AssertCounts(await ReadDashboardAsync(client), 1, 1, 0, 0);

        clock.UtcNow = DateTimeOffset.Parse("2026-11-10T23:00:00Z", CultureInfo.InvariantCulture);
        var nextDay = await ReadDashboardAsync(client);
        Assert.Equal(Today.AddDays(1), nextDay.BusinessDate);
        AssertCounts(nextDay, 1, 1, 0, 1);

        using var complete = await client.PutAsJsonAsync($"/api/v1/tasks/{task.Id}/status", new { status = "Done" });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        AssertCounts(await ReadDashboardAsync(client), 1, 0, 1, 0);

        using var reopen = await client.PutAsJsonAsync($"/api/v1/tasks/{task.Id}/status", new { status = "InProgress" });
        Assert.Equal(HttpStatusCode.OK, reopen.StatusCode);
        AssertCounts(await ReadDashboardAsync(client), 1, 1, 0, 1);

        using var update = await client.PutAsJsonAsync($"/api/v1/tasks/{task.Id}",
            new { title = "No longer overdue", priority = "High", dueDate = Today.AddDays(3) });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        AssertCounts(await ReadDashboardAsync(client), 1, 1, 0, 0);

        using var archive = await client.PostAsync($"/api/v1/projects/{project.Id}/archive", null);
        Assert.True(archive.IsSuccessStatusCode);
        AssertCounts(await ReadDashboardAsync(client), 0, 0, 0, 0);
        using var restore = await client.PostAsync($"/api/v1/projects/{project.Id}/restore", null);
        Assert.True(restore.IsSuccessStatusCode);
        AssertCounts(await ReadDashboardAsync(client), 1, 1, 0, 0);

        using var deleteTask = await client.DeleteAsync($"/api/v1/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteTask.StatusCode);
        AssertCounts(await ReadDashboardAsync(client), 1, 0, 0, 0);

        using var anotherTask = await client.PostAsJsonAsync($"/api/v1/projects/{project.Id}/tasks",
            new { title = "Cascade deletion", priority = "Low", dueDate = Today });
        Assert.Equal(HttpStatusCode.Created, anotherTask.StatusCode);
        AssertCounts(await ReadDashboardAsync(client), 1, 1, 0, 1);
        using var deleteProject = await client.DeleteAsync($"/api/v1/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteProject.StatusCode);
        AssertCounts(await ReadDashboardAsync(client), 0, 0, 0, 0);
    }

    [Fact]
    public async Task OpenApi_DescribesDashboardResponseAndErrorCodes()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var responses = root.GetProperty("paths").GetProperty("/api/v1/dashboard")
            .GetProperty("get").GetProperty("responses");
        foreach (var code in new[] { "200", "401", "403", "503" })
            Assert.True(responses.TryGetProperty(code, out _), $"Missing response {code}");
        var schema = responses.GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");
        if (schema.TryGetProperty("$ref", out var reference))
        {
            foreach (var segment in reference.GetString()![2..].Split('/'))
                root = root.GetProperty(segment.Replace("~1", "/").Replace("~0", "~"));
            schema = root;
        }
        Assert.Equal(new[] { "activeProjects", "businessDate", "businessTimeZone", "completedTasks", "openTasks", "overdueTasks" },
            schema.GetProperty("properties").EnumerateObject().Select(property => property.Name).Order());
    }

    private async Task<Sample> SeedAsync()
    {
        await ResetAsync();
        await using var context = CreateContext();
        var current = new AppUser(Guid.NewGuid().ToString(), "Current", null);
        var other = new AppUser(Guid.NewGuid().ToString(), "Other", null);
        var outsider = new AppUser(Guid.NewGuid().ToString(), "Outsider", null);
        context.Users.AddRange(current, other, outsider);
        var active = new Project("Visible active", null, current.Id);
        var empty = new Project("Visible empty", null, current.Id);
        var foreign = new Project("Foreign active", null, other.Id);
        var archived = new Project("Visible archived", null, current.Id);
        archived.Archive();
        context.Projects.AddRange(active, empty, foreign, archived);
        foreach (var project in new[] { active, empty, archived })
            context.ProjectMemberships.Add(new AppUserProject(project.Id, current.Id));
        foreach (var project in new[] { active, foreign, archived })
            context.ProjectMemberships.Add(new AppUserProject(project.Id, other.Id));
        context.Tasks.AddRange(
            NewTask(active, "Yesterday own", TaskState.ToDo, Today.AddDays(-1), current.Id),
            NewTask(active, "Yesterday other", TaskState.InProgress, Today.AddDays(-1), other.Id),
            NewTask(active, "Today", TaskState.ToDo, Today),
            NewTask(active, "Tomorrow", TaskState.InProgress, Today.AddDays(1)),
            NewTask(active, "No date", TaskState.ToDo, null),
            NewTask(active, "Completed past", TaskState.Done, Today.AddDays(-1), other.Id),
            NewTask(foreign, "Hidden overdue", TaskState.ToDo, Today.AddDays(-1)),
            NewTask(foreign, "Hidden completed", TaskState.Done, null),
            NewTask(archived, "Archived overdue", TaskState.ToDo, Today.AddDays(-1)),
            NewTask(archived, "Archived completed", TaskState.Done, Today.AddDays(-1)));
        await context.SaveChangesAsync();
        return new Sample(current, other, outsider);
    }

    private async Task ResetAsync()
    {
        // Solo se vacia la base del contenedor temporal exclusivo de esta clase.
        await using var context = CreateContext();
        await context.Projects.ExecuteDeleteAsync();
        await context.Users.ExecuteDeleteAsync();
    }

    private async Task<string> SnapshotAsync()
    {
        await using var context = CreateContext();
        return JsonSerializer.Serialize(new
        {
            Users = await context.Users.AsNoTracking().OrderBy(item => item.Id).ToArrayAsync(),
            Projects = await context.Projects.AsNoTracking().OrderBy(item => item.Id).ToArrayAsync(),
            Tasks = await context.Tasks.AsNoTracking().OrderBy(item => item.Id).ToArrayAsync(),
            Memberships = await context.ProjectMemberships.AsNoTracking().OrderBy(item => item.ProjectId)
                .ThenBy(item => item.UserId).ToArrayAsync()
        });
    }

    private AuthenticationTestFactory CreateFactory() => new(postgres.Database.GetConnectionString());

    private TaskFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<TaskFlowDbContext>()
        .UseNpgsql(postgres.Database.GetConnectionString()).Options);

    private static DomainTask NewTask(Project project, string title, TaskState state, DateOnly? due, Guid? assigned = null) =>
        new(project.Id, title, TaskPriority.Medium, assignedId: assigned, status: state, dueDate: due);

    private static WebApplicationFactory<Program> WithClock(AuthenticationTestFactory factory, TimeProvider clock) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(clock);
        }));

    private static HttpClient CreateClient(WebApplicationFactory<Program> host,
        AuthenticationTestFactory authentication, AppUser user, string role = "User")
    {
        var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            authentication.CreateToken(user.KeycloakSub, [role]));
        return client;
    }

    private static async Task<DashboardResponse> ReadDashboardAsync(HttpClient client, string path = "/api/v1/dashboard")
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        return Assert.IsType<DashboardResponse>(await response.Content.ReadFromJsonAsync<DashboardResponse>());
    }

    private static void AssertCounts(DashboardResponse result, int projects, int open, int completed, int overdue)
    {
        Assert.Equal(projects, result.ActiveProjects);
        Assert.Equal(open, result.OpenTasks);
        Assert.Equal(completed, result.CompletedTasks);
        Assert.Equal(overdue, result.OverdueTasks);
    }

    private sealed record Sample(AppUser Current, AppUser Other, AppUser Outsider);

    private sealed class FixedClock(string utcInstant) : TimeProvider
    {
        internal DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse(utcInstant, CultureInfo.InvariantCulture);
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
