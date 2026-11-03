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

namespace TaskFlow.Api.IntegrationTests;

public class ProjectDeleteApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_Admin_RemovesProjectTasksAndMembershipsButKeepsUsersAndOtherProjects(bool archived)
    {
        var sample = await SeedAsync(archived);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Admin, "Admin");

        using var response = await client.DeleteAsync($"/api/v1/projects/{sample.Project.Id}");

        await AssertNoContentAsync(response);
        await AssertDeletedAsync(sample);
        using var repeated = await client.DeleteAsync($"/api/v1/projects/{sample.Project.Id}");
        await AssertProblemAsync(repeated, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ProjectWithOneMember_IsAllowedForAdmin()
    {
        var sample = await SeedAsync(singleMember: true);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Admin, "Admin");

        using var response = await client.DeleteAsync($"/api/v1/projects/{sample.Project.Id}");

        await AssertNoContentAsync(response);
        await AssertDeletedAsync(sample);
    }

    [Theory]
    [InlineData("creator", false, false, HttpStatusCode.Forbidden)]
    [InlineData("member", false, false, HttpStatusCode.Forbidden)]
    [InlineData("outsider", false, false, HttpStatusCode.NotFound)]
    [InlineData("member", true, false, HttpStatusCode.NotFound)]
    [InlineData("member", false, true, HttpStatusCode.NotFound)]
    [InlineData("admin", false, true, HttpStatusCode.NotFound)]
    public async Task Delete_VisibilityPrecedesAdministrativePermission(
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

        using var response = await client.DeleteAsync($"/api/v1/projects/{id}");

        await AssertProblemAsync(response, expected);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task Delete_InvalidIdentifier_Returns400WithoutDeletingData()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Admin, "Admin");

        using var response = await client.DeleteAsync("/api/v1/projects/not-a-guid");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task Delete_InvalidSession_DoesNotChangeData(string session, HttpStatusCode expected)
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

        using var response = await client.DeleteAsync($"/api/v1/projects/{sample.Project.Id}");

        await AssertProblemAsync(response, expected);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task Delete_FailureAfterSqlDelete_RollsBackProjectAndCascades()
    {
        var sample = await SeedAsync();
        var interceptor = new FailAfterProjectDeleteInterceptor();
        using var factory = CreateFactory();
        using var failingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddDbContext<TaskFlowDbContext>(options =>
                options.AddInterceptors(interceptor))));
        using var client = failingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(sample.Admin.KeycloakSub, ["Admin"]));

        using var response = await client.DeleteAsync($"/api/v1/projects/{sample.Project.Id}");

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.True(interceptor.DeleteExecuted);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task ProjectLifecycle_CreatorAndAdmin_CompleteProjectWorkflow()
    {
        using var factory = CreateFactory();
        using var creator = factory.CreateClient();
        using var outsider = factory.CreateClient();
        using var admin = factory.CreateClient();
        creator.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken());
        outsider.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken());
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(roles: ["Admin"]));

        using var created = await creator.PostAsJsonAsync("/api/v1/projects", new { name = "Recorrido completo" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var project = Assert.IsType<ProjectResponse>(await created.Content.ReadFromJsonAsync<ProjectResponse>());
        var uri = $"/api/v1/projects/{project.Id}";
        using var detail = await creator.GetAsync(uri);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var invisible = await outsider.GetAsync(uri);
        await AssertProblemAsync(invisible, HttpStatusCode.NotFound);
        using var updated = await creator.PutAsJsonAsync(uri, new { name = "Proyecto editado" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        using var forbidden = await creator.PostAsync(uri + "/archive", null);
        await AssertProblemAsync(forbidden, HttpStatusCode.Forbidden);
        using var archived = await admin.PostAsync(uri + "/archive", null);
        await AssertNoContentAsync(archived);
        using var hiddenArchived = await creator.GetAsync(uri);
        await AssertProblemAsync(hiddenArchived, HttpStatusCode.NotFound);
        using var restored = await admin.PostAsync(uri + "/restore", null);
        await AssertNoContentAsync(restored);
        using var visibleAgain = await creator.GetAsync(uri);
        Assert.Equal(HttpStatusCode.OK, visibleAgain.StatusCode);
        var restoredProject = Assert.IsType<ProjectDetailResponse>(await visibleAgain.Content.ReadFromJsonAsync<ProjectDetailResponse>());
        Assert.Equal("Proyecto editado", restoredProject.Name);
        Assert.Equal(project.CreatedBy!.Id, Assert.Single(restoredProject.Members).Id);

        using var deleted = await admin.DeleteAsync(uri);
        await AssertNoContentAsync(deleted);
        using var missing = await creator.GetAsync(uri);
        await AssertProblemAsync(missing, HttpStatusCode.NotFound);
        await using var context = CreateContext();
        Assert.False(await context.Projects.AnyAsync(item => item.Id == project.Id));
        Assert.False(await context.ProjectMemberships.AnyAsync(item => item.ProjectId == project.Id));
        Assert.True(await context.Users.AnyAsync(user => user.Id == project.CreatedBy.Id));
    }

    private async Task<Sample> SeedAsync(bool archived = false, bool singleMember = false)
    {
        var creator = new AppUser(Guid.NewGuid().ToString(), "Creadora", null);
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro", null);
        var admin = new AppUser(Guid.NewGuid().ToString(), "Administrador sin pertenencia", null);
        var project = new Project($"Borrado {Guid.NewGuid():N}", "Descripción inicial", creator.Id);
        var other = new Project($"Conservado {Guid.NewGuid():N}", "Otro proyecto", creator.Id);
        if (archived) { project.Archive(); }
        var tasks = new[]
        {
            new DomainTask(project.Id, "Asignada", TaskPriority.High,
                assignedId: singleMember ? creator.Id : member.Id),
            new DomainTask(project.Id, "Sin asignar", TaskPriority.Low)
        };
        var otherTask = new DomainTask(other.Id, "Tarea conservada", TaskPriority.Medium, assignedId: member.Id);
        await using var context = CreateContext();
        context.Users.AddRange(creator, member, admin);
        context.Projects.AddRange(project, other);
        context.ProjectMemberships.Add(new AppUserProject(project.Id, creator.Id));
        if (!singleMember)
        {
            context.ProjectMemberships.Add(new AppUserProject(project.Id, member.Id));
        }
        context.ProjectMemberships.AddRange(
            new AppUserProject(other.Id, creator.Id), new AppUserProject(other.Id, member.Id));
        context.Tasks.AddRange(tasks);
        context.Tasks.Add(otherTask);
        await context.SaveChangesAsync();
        await context.Entry(project).ReloadAsync();
        await context.Entry(other).ReloadAsync();
        return new Sample(project, other, creator, member, admin, tasks, otherTask, singleMember);
    }

    private async Task AssertDeletedAsync(Sample sample)
    {
        await using var context = CreateContext();
        Assert.False(await context.Projects.AnyAsync(project => project.Id == sample.Project.Id));
        Assert.False(await context.Tasks.AnyAsync(task => task.ProjectId == sample.Project.Id));
        Assert.False(await context.ProjectMemberships.AnyAsync(membership => membership.ProjectId == sample.Project.Id));
        await AssertUnrelatedDataAsync(sample, context);
    }

    private async Task AssertUnchangedAsync(Sample sample)
    {
        await using var context = CreateContext();
        var stored = await context.Projects.SingleAsync(project => project.Id == sample.Project.Id);
        Assert.Equal(sample.Project.Name, stored.Name);
        Assert.Equal(sample.Project.Description, stored.Description);
        Assert.Equal(sample.Project.CreatedBy, stored.CreatedBy);
        Assert.Equal(sample.Project.CreatedAt, stored.CreatedAt);
        Assert.Equal(sample.Project.UpdatedAt, stored.UpdatedAt);
        Assert.Equal(sample.Project.ArchivedAt, stored.ArchivedAt);
        Assert.Equal(sample.SingleMember ? 1 : 2, await context.ProjectMemberships.CountAsync(item => item.ProjectId == stored.Id));
        var storedTasks = await context.Tasks.Where(task => task.ProjectId == stored.Id).ToArrayAsync();
        Assert.Equal(sample.Tasks.Length, storedTasks.Length);
        foreach (var task in sample.Tasks)
        {
            var storedTask = Assert.Single(storedTasks, item => item.Id == task.Id);
            Assert.Equal(task.Title, storedTask.Title);
            Assert.Equal(task.AssignedId, storedTask.AssignedId);
        }
        await AssertUnrelatedDataAsync(sample, context);
    }

    private static async Task AssertUnrelatedDataAsync(Sample sample, TaskFlowDbContext context)
    {
        var users = new[] { sample.Creator.Id, sample.Member.Id, sample.Admin.Id };
        Assert.Equal(3, await context.Users.CountAsync(user => users.Contains(user.Id)));
        var other = await context.Projects.SingleAsync(project => project.Id == sample.OtherProject.Id);
        Assert.Equal(sample.OtherProject.Name, other.Name);
        Assert.Equal(sample.OtherProject.CreatedBy, other.CreatedBy);
        Assert.Equal(sample.OtherProject.CreatedAt, other.CreatedAt);
        Assert.Equal(sample.OtherProject.UpdatedAt, other.UpdatedAt);
        Assert.Equal(2, await context.ProjectMemberships.CountAsync(item => item.ProjectId == other.Id));
        var task = await context.Tasks.SingleAsync(item => item.Id == sample.OtherTask.Id);
        Assert.Equal(sample.OtherTask.Title, task.Title);
        Assert.Equal(sample.OtherTask.AssignedId, task.AssignedId);
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

    private sealed record Sample(Project Project, Project OtherProject, AppUser Creator, AppUser Member,
        AppUser Admin, DomainTask[] Tasks, DomainTask OtherTask, bool SingleMember);

    private sealed class FailAfterProjectDeleteInterceptor : DbCommandInterceptor
    {
        public bool DeleteExecuted { get; private set; }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            CheckDelete(command);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            CheckDelete(command);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void CheckDelete(DbCommand command)
        {
            if (command.CommandText.Contains("DELETE FROM project", StringComparison.Ordinal))
            {
                DeleteExecuted = true;
                throw new InvalidOperationException("Fallo simulado después del DELETE.");
            }
        }
    }
}
