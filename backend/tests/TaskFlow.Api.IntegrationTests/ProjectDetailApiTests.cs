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

public class ProjectDetailApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("User", false)]
    [InlineData("Admin", false)]
    [InlineData("Admin", true)]
    public async Task GetById_VisibleProject_ReturnsCreatorAndOnlyItsMembers(string role, bool archived)
    {
        var sample = await SeedAsync(archived);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, role == "Admin" ? sample.Outsider : sample.Member, role);

        using var response = await client.GetAsync($"/api/v1/projects/{sample.Project.Id}");

        var detail = await ReadDetailAsync(response);
        Assert.Equal(sample.Project.Id, detail.Id);
        Assert.Equal(sample.Project.Name, detail.Name);
        Assert.Equal(sample.Project.Description, detail.Description);
        Assert.Equal(sample.Project.CreatedAt, detail.CreatedAt);
        Assert.Equal(sample.Project.UpdatedAt, detail.UpdatedAt);
        Assert.Equal(sample.Project.ArchivedAt, detail.ArchivedAt);
        Assert.NotNull(detail.CreatedBy);
        // Quien consulta no es el creador: su identidad no debe sustituir a la autoría.
        Assert.Equal(sample.Creator.Id, detail.CreatedBy.Id);
        Assert.Equal(sample.Creator.DisplayName, detail.CreatedBy.DisplayName);
        Assert.Equal(new[] { sample.Creator.Id, sample.Member.Id }.OrderBy(id => id),
            detail.Members.Select(member => member.Id).OrderBy(id => id));
        Assert.Equal(sample.Creator.DisplayName,
            detail.Members.Single(member => member.Id == sample.Creator.Id).DisplayName);
        Assert.Equal(sample.Member.DisplayName,
            detail.Members.Single(member => member.Id == sample.Member.Id).DisplayName);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "archivedAt", "createdAt", "createdBy", "description", "id", "members", "name", "updatedAt" },
            json.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        AssertSummaryFields(json.RootElement.GetProperty("createdBy"));
        foreach (var member in json.RootElement.GetProperty("members").EnumerateArray())
        {
            AssertSummaryFields(member);
        }
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("archived")]
    [InlineData("missing")]
    public async Task GetById_UserCannotSeeProject_ReturnsSame404AsMissing(string scenario)
    {
        var sample = await SeedAsync(archived: scenario == "archived");
        using var factory = CreateFactory();
        using var client = CreateClient(factory, scenario == "foreign" ? sample.Outsider : sample.Member);
        var projectId = scenario == "missing" ? Guid.NewGuid() : sample.Project.Id;

        using var response = await client.GetAsync($"/api/v1/projects/{projectId}");
        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        using var missingResponse = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}");
        var missingProblem = await ReadProblemAsync(missingResponse, HttpStatusCode.NotFound);

        Assert.Equal(missingProblem.Title, problem.Title);
        Assert.Equal(missingProblem.Type, problem.Type);
        Assert.Equal(missingProblem.Detail, problem.Detail);
        Assert.DoesNotContain(sample.Project.Name, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetById_DeletedCreator_ReturnsNullCreatorAndRemainingMembers()
    {
        var sample = await SeedAsync();
        // Solo se modifica la base temporal del test; queda otro miembro en el proyecto.
        await using (var context = CreateContext())
        {
            context.Users.Remove(sample.Creator);
            await context.SaveChangesAsync();
        }
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.GetAsync($"/api/v1/projects/{sample.Project.Id}");

        var detail = await ReadDetailAsync(response);
        Assert.Null(detail.CreatedBy);
        Assert.Equal(sample.Member.Id, Assert.Single(detail.Members).Id);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("createdBy").ValueKind);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("123")]
    public async Task GetById_InvalidIdentifier_Returns400(string projectId)
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory, new AppUser(Guid.NewGuid().ToString(), "Lector", null));

        using var response = await client.GetAsync($"/api/v1/projects/{projectId}");

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task GetById_InvalidSession_RejectsRequestWithoutWriting(
        string session, HttpStatusCode expectedStatus)
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

        using var response = await client.GetAsync($"/api/v1/projects/{sample.Project.Id}");

        await ReadProblemAsync(response, expectedStatus);
        Assert.Equal(before, await ReadCountsAsync());
    }

    private async Task<Sample> SeedAsync(bool archived = false)
    {
        var creator = new AppUser(Guid.NewGuid().ToString(), "Creadora", "creator@example.com");
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro", "member@example.com");
        var outsider = new AppUser(Guid.NewGuid().ToString(), "Otro usuario", "other@example.com");
        var project = new Project($"Detalle {Guid.NewGuid():N}", "Descripción del proyecto", creator.Id);
        var otherProject = new Project("Otro proyecto", null, outsider.Id);
        if (archived)
        {
            project.Archive();
        }
        await using var context = CreateContext();
        context.Users.AddRange(creator, member, outsider);
        context.Projects.AddRange(project, otherProject);
        context.ProjectMemberships.AddRange(
            new AppUserProject(project.Id, creator.Id),
            new AppUserProject(project.Id, member.Id),
            new AppUserProject(otherProject.Id, outsider.Id));
        await context.SaveChangesAsync();
        // Comparar las fechas con la precisión persistida por PostgreSQL.
        await context.Entry(project).ReloadAsync();
        return new Sample(project, creator, member, outsider);
    }

    private AuthenticationTestFactory CreateFactory() => new(postgres.Database.GetConnectionString());

    private TaskFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<TaskFlowDbContext>()
        .UseNpgsql(postgres.Database.GetConnectionString()).Options);

    private async Task<(int Users, int Projects, int Memberships)> ReadCountsAsync()
    {
        await using var context = CreateContext();
        return (await context.Users.CountAsync(), await context.Projects.CountAsync(),
            await context.ProjectMemberships.CountAsync());
    }

    private static HttpClient CreateClient(AuthenticationTestFactory factory, AppUser user, string role = "User")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(user.KeycloakSub, [role]));
        return client;
    }

    private static async Task<ProjectDetailResponse> ReadDetailAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        return Assert.IsType<ProjectDetailResponse>(await response.Content.ReadFromJsonAsync<ProjectDetailResponse>());
    }

    private static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = Assert.IsType<ProblemDetails>(await response.Content.ReadFromJsonAsync<ProblemDetails>());
        Assert.Equal((int)status, problem.Status);
        return problem;
    }

    private static void AssertSummaryFields(JsonElement summary) =>
        Assert.Equal(new[] { "displayName", "id" },
            summary.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());

    private sealed record Sample(Project Project, AppUser Creator, AppUser Member, AppUser Outsider);
}
