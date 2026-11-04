using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Infrastructure.Persistence;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using Project = TaskFlow.Domain.Project;

namespace TaskFlow.Api.IntegrationTests;

public class ProjectListApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task List_FiltersBeforeCountingAndPaging_ReturnsOrderedProjectsAndCreators(string role)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, role == "Admin" ? sample.Observer : sample.Member, role);
        var visible = ActiveProjects(sample, role).ToArray();

        using var response = await client.GetAsync("/api/v1/projects?page=2&pageSize=3");

        var page = await ReadPageAsync(response);
        Assert.Equal(2, page.Page);
        Assert.Equal(3, page.PageSize);
        Assert.Equal(visible.Length, page.TotalCount);
        Assert.Equal(visible.Skip(3).Take(3).Select(project => project.Id), page.Items.Select(project => project.Id));
        foreach (var item in page.Items)
        {
            var stored = sample.Projects.Single(project => project.Id == item.Id);
            Assert.Equal(stored.Name, item.Name);
            Assert.Equal(stored.Description, item.Description);
            Assert.Equal(stored.CreatedAt, item.CreatedAt);
            Assert.Equal(stored.UpdatedAt, item.UpdatedAt);
            Assert.Null(item.ArchivedAt);
            var creator = stored.CreatedBy == sample.Creator.Id ? sample.Creator : sample.OtherCreator;
            Assert.NotNull(item.CreatedBy);
            Assert.Equal(creator.Id, item.CreatedBy.Id);
            Assert.Equal(creator.DisplayName, item.CreatedBy.DisplayName);
        }

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "items", "page", "pageSize", "totalCount" },
            json.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        foreach (var item in json.RootElement.GetProperty("items").EnumerateArray())
        {
            Assert.Equal(new[] { "archivedAt", "createdAt", "createdBy", "description", "id", "name", "updatedAt" },
                item.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
            Assert.Equal(new[] { "displayName", "id" }, item.GetProperty("createdBy")
                .EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        }
    }

    [Fact]
    public async Task List_DefaultParameters_ReturnsFirst20ActiveProjects()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Observer, "Admin");

        using var response = await client.GetAsync("/api/v1/projects");

        var page = await ReadPageAsync(response);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(30, page.TotalCount);
        Assert.Equal(ActiveProjects(sample, "Admin").Take(20).Select(project => project.Id),
            page.Items.Select(project => project.Id));
    }

    [Fact]
    public async Task List_AdminRequestsArchived_ReturnsOnlyArchivedProjects()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Observer, "Admin");

        using var response = await client.GetAsync("/api/v1/projects?archived=true");

        var page = await ReadPageAsync(response);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(sample.Projects.Where(project => project.ArchivedAt.HasValue)
            .OrderByDescending(project => project.CreatedAt).ThenBy(project => project.Id).Select(project => project.Id),
            page.Items.Select(project => project.Id));
        Assert.All(page.Items, project => Assert.NotNull(project.ArchivedAt));
    }

    [Fact]
    public async Task List_UserRequestsArchived_Returns403EvenWhenMember()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.GetAsync("/api/v1/projects?archived=true");

        await ReadProblemAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_UserWithoutMembership_Returns200AndEmptyPage()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Observer);

        using var response = await client.GetAsync("/api/v1/projects");

        var page = await ReadPageAsync(response);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public async Task List_PageBeyondLast_ReturnsEmptyPageWithRealTotal(int requestedPage)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Observer, "Admin");

        using var response = await client.GetAsync($"/api/v1/projects?page={requestedPage}&pageSize=100");

        var page = await ReadPageAsync(response);
        Assert.Empty(page.Items);
        Assert.Equal(30, page.TotalCount);
        Assert.Equal(requestedPage, page.Page);
        Assert.Equal(100, page.PageSize);
    }

    [Fact]
    public async Task List_MaximumPageSize_IsAccepted()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Observer, "Admin");

        using var response = await client.GetAsync("/api/v1/projects?pageSize=100");

        var page = await ReadPageAsync(response);
        Assert.Equal(100, page.PageSize);
        Assert.Equal(30, page.Items.Length);
        Assert.Equal(30, page.TotalCount);
    }

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("page=-1", "page")]
    [InlineData("pageSize=0", "pageSize")]
    [InlineData("pageSize=-1", "pageSize")]
    [InlineData("pageSize=101", "pageSize")]
    [InlineData("pageSize=2147483647", "pageSize")]
    [InlineData("page=abc", "page")]
    [InlineData("pageSize=abc", "pageSize")]
    [InlineData("page=2147483648", "page")]
    [InlineData("archived=invalid", "archived")]
    public async Task List_InvalidQuery_Returns400WithFieldErrors(string query, string field)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Observer, "Admin");

        using var response = await client.GetAsync($"/api/v1/projects?{query}");

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.NotNull(problem);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty(field, out var errors));
        Assert.NotEmpty(errors.EnumerateArray());
    }

    [Fact]
    public async Task List_DeletedCreator_ReturnsNullCreatorForTheirProjects()
    {
        var sample = await SeedAsync();
        var creatorProjectIds = sample.Projects.Where(project => project.CreatedBy == sample.Creator.Id)
            .Select(project => project.Id).ToHashSet();
        await using (var context = CreateContext())
        {
            context.Users.Remove(sample.Creator);
            await context.SaveChangesAsync();
        }
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Observer, "Admin");

        using var response = await client.GetAsync("/api/v1/projects?pageSize=100");

        var page = await ReadPageAsync(response);
        Assert.Equal(30, page.TotalCount);
        Assert.All(page.Items.Where(project => creatorProjectIds.Contains(project.Id)), project => Assert.Null(project.CreatedBy));
        Assert.All(page.Items.Where(project => !creatorProjectIds.Contains(project.Id)), project =>
        {
            Assert.NotNull(project.CreatedBy);
            Assert.Equal(sample.OtherCreator.Id, project.CreatedBy.Id);
        });
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task List_InvalidSession_RejectsRequestWithoutWriting(string session, HttpStatusCode expectedStatus)
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
        await using var context = CreateContext();
        var before = (await context.Users.CountAsync(), await context.Projects.CountAsync(),
            await context.ProjectMemberships.CountAsync());

        using var response = await client.GetAsync("/api/v1/projects");

        await ReadProblemAsync(response, expectedStatus);
        Assert.Equal(before, (await context.Users.CountAsync(), await context.Projects.CountAsync(),
            await context.ProjectMemberships.CountAsync()));
    }

    private async Task<Sample> SeedAsync()
    {
        await using var context = CreateContext();
        // Esta fixture pertenece solo a esta clase y utiliza una base de datos temporal.
        await context.Projects.ExecuteDeleteAsync();
        await context.Users.ExecuteDeleteAsync();
        var creator = new AppUser(Guid.NewGuid().ToString(), "Creadora", "creator@example.com");
        var otherCreator = new AppUser(Guid.NewGuid().ToString(), "Otro creador", "other@example.com");
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro", "member@example.com");
        var observer = new AppUser(Guid.NewGuid().ToString(), "Sin pertenencias", null);
        context.Users.AddRange(creator, otherCreator, member, observer);
        var projects = new Project[32];
        var memberProjectIds = new HashSet<Guid>();
        var firstDate = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
        for (var index = 0; index < projects.Length; index++)
        {
            var author = index % 2 == 0 ? creator : otherCreator;
            var project = new Project($"Proyecto {index}", index == 0 ? null : $"Descripción {index}", author.Id);
            // Fechas repetidas e identificadores conocidos para verificar también el desempate.
            context.Entry(project).Property(item => item.Id).CurrentValue =
                Guid.Parse($"00000000-0000-0000-0000-{index + 1:D12}");
            context.Entry(project).Property(item => item.CreatedAt).CurrentValue = firstDate.AddMinutes(index / 2);
            context.Entry(project).Property(item => item.UpdatedAt).CurrentValue = project.CreatedAt;
            if (index >= 30)
            {
                project.Archive();
            }
            projects[index] = project;
            context.ProjectMemberships.Add(new AppUserProject(project.Id, author.Id));
            // Conservamos un miembro incluso al eliminar a la creadora en su prueba.
            if (author.Id != otherCreator.Id)
            {
                context.ProjectMemberships.Add(new AppUserProject(project.Id, otherCreator.Id));
            }
            if (index % 3 == 0 || index >= 30)
            {
                context.ProjectMemberships.Add(new AppUserProject(project.Id, member.Id));
                memberProjectIds.Add(project.Id);
            }
        }
        context.Projects.AddRange(projects.Reverse());
        await context.SaveChangesAsync();
        return new Sample(projects, creator, otherCreator, member, observer, memberProjectIds);
    }

    private static IOrderedEnumerable<Project> ActiveProjects(Sample sample, string role) =>
        sample.Projects.Where(project => project.ArchivedAt is null &&
                (role == "Admin" || sample.MemberProjectIds.Contains(project.Id)))
            .OrderByDescending(project => project.CreatedAt).ThenBy(project => project.Id);

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

    private static async Task<ProjectPageResponse> ReadPageAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        return Assert.IsType<ProjectPageResponse>(await response.Content.ReadFromJsonAsync<ProjectPageResponse>());
    }

    private static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = Assert.IsType<ProblemDetails>(await response.Content.ReadFromJsonAsync<ProblemDetails>());
        Assert.Equal((int)status, problem.Status);
        return problem;
    }

    private sealed record Sample(Project[] Projects, AppUser Creator, AppUser OtherCreator, AppUser Member,
        AppUser Observer, HashSet<Guid> MemberProjectIds);
}
