using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Infrastructure.Persistence;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using DomainTask = TaskFlow.Domain.Task;
using Project = TaskFlow.Domain.Project;
using TaskPriority = TaskFlow.Domain.TaskPriority;
using TaskState = TaskFlow.Domain.TaskState;

namespace TaskFlow.Api.IntegrationTests;

public class ProjectRestoreApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Restore_Admin_Returns204AndPreservesProjectTasksAndMembers()
    {
        var sample = await SeedAsync(archived: true);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Admin, "Admin");
        var before = DateTimeOffset.UtcNow;

        using var response = await client.PostAsync($"/api/v1/projects/{sample.Project.Id}/restore", null);
        var after = DateTimeOffset.UtcNow;

        await AssertNoContentAsync(response);
        await using var context = CreateContext();
        var stored = await context.Projects.SingleAsync(project => project.Id == sample.Project.Id);
        Assert.Null(stored.ArchivedAt);
        var lowerBound = new DateTimeOffset(before.Ticks - before.Ticks % 10, TimeSpan.Zero);
        Assert.InRange(stored.UpdatedAt, lowerBound, after);
        await AssertPreservedDataAsync(sample, context);
    }

    [Fact]
    public async Task Restore_AlreadyActive_IsIdempotentAndPreservesTimestamps()
    {
        var sample = await SeedAsync(archived: false);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Admin, "Admin");

        using var first = await client.PostAsync($"/api/v1/projects/{sample.Project.Id}/restore", null);
        using var second = await client.PostAsync($"/api/v1/projects/{sample.Project.Id}/restore", null);

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
    [InlineData("admin", true, true, HttpStatusCode.NotFound)]
    public async Task Restore_VisibilityPrecedesAdministrativePermission(
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

        using var response = await client.PostAsync($"/api/v1/projects/{id}/restore", null);

        await AssertProblemAsync(response, expected);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task Restore_InvalidIdentifier_Returns400()
    {
        var sample = await SeedAsync(archived: true);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Admin, "Admin");

        using var response = await client.PostAsync("/api/v1/projects/not-a-guid/restore", null);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task Restore_MemberCanReadProjectAgainAndItLeavesArchivedList()
    {
        var sample = await SeedAsync(archived: true);
        using var factory = CreateFactory();
        using var member = CreateClient(factory, sample.Member);
        using var admin = CreateClient(factory, sample.Admin, "Admin");
        using var hiddenBefore = await member.GetAsync($"/api/v1/projects/{sample.Project.Id}");
        await AssertProblemAsync(hiddenBefore, HttpStatusCode.NotFound);

        using var restore = await admin.PostAsync($"/api/v1/projects/{sample.Project.Id}/restore", null);
        await AssertNoContentAsync(restore);

        using var visibleAfter = await member.GetAsync($"/api/v1/projects/{sample.Project.Id}");
        Assert.Equal(HttpStatusCode.OK, visibleAfter.StatusCode);
        var detail = Assert.IsType<ProjectDetailResponse>(await visibleAfter.Content.ReadFromJsonAsync<ProjectDetailResponse>());
        Assert.Null(detail.ArchivedAt);
        Assert.Equal(2, detail.Members.Length);
        using var memberList = await member.GetAsync("/api/v1/projects");
        Assert.Equal(HttpStatusCode.OK, memberList.StatusCode);
        var visiblePage = Assert.IsType<ProjectPageResponse>(await memberList.Content.ReadFromJsonAsync<ProjectPageResponse>());
        Assert.Equal(1, visiblePage.TotalCount);
        Assert.Equal(sample.Project.Id, Assert.Single(visiblePage.Items).Id);

        using var archivedList = await admin.GetAsync("/api/v1/projects?archived=true&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, archivedList.StatusCode);
        var archivedPage = Assert.IsType<ProjectPageResponse>(await archivedList.Content.ReadFromJsonAsync<ProjectPageResponse>());
        Assert.DoesNotContain(archivedPage.Items, project => project.Id == sample.Project.Id);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task Restore_InvalidSession_DoesNotChangeData(string session, HttpStatusCode expected)
    {
        var sample = await SeedAsync(archived: true);
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

        using var response = await client.PostAsync($"/api/v1/projects/{sample.Project.Id}/restore", null);

        await AssertProblemAsync(response, expected);
        await AssertUnchangedAsync(sample);
    }

    private async Task<Sample> SeedAsync(bool archived)
    {
        var creator = new AppUser(Guid.NewGuid().ToString(), "Creadora", null);
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro", null);
        var admin = new AppUser(Guid.NewGuid().ToString(), "Administrador sin pertenencia", null);
        var project = new Project($"Restauración {Guid.NewGuid():N}", "Descripción conservada", creator.Id);
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
        context.Entry(project).Property(project => project.CreatedAt).CurrentValue = oldDate;
        context.Entry(project).Property(project => project.UpdatedAt).CurrentValue = oldDate.AddDays(1);
        if (archived)
        {
            context.Entry(project).Property(project => project.ArchivedAt).CurrentValue = oldDate.AddDays(1);
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
}
