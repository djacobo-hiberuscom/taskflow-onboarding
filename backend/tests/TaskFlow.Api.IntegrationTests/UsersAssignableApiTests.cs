using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts.Users;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Repositories;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using Project = TaskFlow.Domain.Project;

namespace TaskFlow.Api.IntegrationTests;

public class UsersAssignableApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task GetAssignable_ReturnsAllLocalUsersInNameAndIdOrderWithOnlyPublicSummaryFields(string role)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Caller, role);
        int expectedCount;
        await using (var context = CreateContext()) { expectedCount = await context.Users.CountAsync(); }

        using var response = await client.GetAsync("/api/v1/users/assignable");

        var users = await ReadUsersAsync(response);
        Assert.Equal(expectedCount, users.Length);
        Assert.True(users.Length > 20, "Este endpoint no pagina ni limita la lista a los primeros 20 usuarios.");
        Assert.Equal(users.Length, users.Select(user => user.Id).Distinct().Count());
        Assert.Contains(users, user => user.Id == sample.Caller.Id);
        Assert.Contains(users, user => user.Id == sample.Candidate.Id && user.DisplayName == sample.Candidate.DisplayName);
        var selectedIds = new[] { sample.First.Id, sample.TwinLow.Id, sample.TwinHigh.Id, sample.Candidate.Id };
        Assert.Equal(selectedIds, users.Where(user => selectedIds.Contains(user.Id)).Select(user => user.Id).ToArray());
        await using var verification = CreateContext();
        Assert.False(await verification.ProjectMemberships.AnyAsync(item => item.UserId == sample.Candidate.Id));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        foreach (var user in json.RootElement.EnumerateArray())
        {
            Assert.Equal(new[] { "displayName", "id" }, user.EnumerateObject()
                .Select(property => property.Name).OrderBy(name => name).ToArray());
        }
        // Se comprueba la sesión del solicitante, no se consulta Keycloak por cada candidato.
        Assert.Equal(1, factory.Introspection.RequestCount);
    }

    [Fact]
    public async Task GetAssignable_RepeatedRequests_DoNotModifyProfilesOrCreateMemberships()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Caller);
        var before = await ReadSnapshotAsync();

        using var firstResponse = await client.GetAsync("/api/v1/users/assignable");
        using var secondResponse = await client.GetAsync("/api/v1/users/assignable");

        var first = await ReadUsersAsync(firstResponse);
        var second = await ReadUsersAsync(secondResponse);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Equal(before, await ReadSnapshotAsync());
    }

    [Fact]
    public async Task GetAssignable_FirstAccess_IncludesNewlyResolvedLocalUserWithoutCreatingProjectMemberships()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var subject = Guid.NewGuid().ToString();
        var name = $"Nuevo usuario {Guid.NewGuid():N}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(subject, ["User"], additionalClaims: [new Claim("name", name)]));
        int beforeUsers;
        int beforeMemberships;
        await using (var context = CreateContext())
        {
            beforeUsers = await context.Users.CountAsync();
            beforeMemberships = await context.ProjectMemberships.CountAsync();
        }

        using var response = await client.GetAsync("/api/v1/users/assignable");

        var users = await ReadUsersAsync(response);
        await using var verification = CreateContext();
        var local = await verification.Users.SingleAsync(user => user.KeycloakSub == subject);
        Assert.Contains(users, user => user.Id == local.Id && user.DisplayName == name);
        Assert.Equal(beforeUsers + 1, await verification.Users.CountAsync());
        Assert.Equal(beforeMemberships, await verification.ProjectMemberships.CountAsync());
    }

    [Fact]
    public async Task GetAssignable_RepositoryDoesNotTrackReadOnlyUsers()
    {
        var sample = await SeedAsync();
        await using var context = CreateContext();

        var users = await new AppUserRepository(context).GetAssignableAsync(CancellationToken.None);

        Assert.Contains(users, user => user.Id == sample.Candidate.Id);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Null(context.Database.CurrentTransaction);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task GetAssignable_InvalidSession_DoesNotExposeUsersOrModifyDatabase(string session, HttpStatusCode status)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        if (session != "missing-token")
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                session == "invalid-token" ? "invalid-token" :
                factory.CreateToken(sample.Caller.KeycloakSub, session == "missing-role" ? ["Other"] : ["User"]));
        }
        if (session is "inactive-token" or "introspection-unavailable")
        {
            factory.Introspection.Respond = _ => Task.FromResult(new HttpResponseMessage(
                session == "inactive-token" ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("{\"active\":false}")
            });
        }
        var before = await ReadSnapshotAsync();

        using var response = await client.GetAsync("/api/v1/users/assignable");

        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain(sample.Candidate.DisplayName, await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await ReadSnapshotAsync());
    }

    private async Task<Sample> SeedAsync()
    {
        var scope = Guid.NewGuid().ToString("N");
        var caller = new AppUser(Guid.NewGuid().ToString(), $"PPP {scope}", "caller@example.com");
        var candidate = new AppUser(Guid.NewGuid().ToString(), $"ZZZ {scope}", "candidate@example.com");
        var first = new AppUser(Guid.NewGuid().ToString(), $"AAA {scope}", "first@example.com");
        var twinLow = new AppUser(Guid.NewGuid().ToString(), $"MMM {scope}", null);
        var twinHigh = new AppUser(Guid.NewGuid().ToString(), $"MMM {scope}", null);
        var extra = Enumerable.Range(0, 25).Select(index => new AppUser(
            Guid.NewGuid().ToString(), $"QQQ {scope} {index:D2}", null)).ToArray();
        var project = new Project($"Proyecto {scope}", null, caller.Id);
        await using var context = CreateContext();
        // Misma etiqueta, IDs ordenados conocidos e inserción en orden contrario al esperado.
        context.Entry(twinLow).Property(user => user.Id).CurrentValue = Guid.ParseExact(scope[..24] + "00000001", "N");
        context.Entry(twinHigh).Property(user => user.Id).CurrentValue = Guid.ParseExact(scope[..24] + "00000002", "N");
        context.Users.AddRange(candidate, twinHigh, caller, twinLow, first);
        context.Users.AddRange(extra);
        context.Projects.Add(project);
        context.ProjectMemberships.Add(new AppUserProject(project.Id, caller.Id));
        await context.SaveChangesAsync();
        return new Sample(caller, candidate, first, twinLow, twinHigh);
    }

    private async Task<(string Users, int Projects, int Memberships, int Tasks)> ReadSnapshotAsync()
    {
        await using var context = CreateContext();
        var users = await context.Users.AsNoTracking().OrderBy(user => user.Id).ToArrayAsync();
        return (JsonSerializer.Serialize(users), await context.Projects.CountAsync(),
            await context.ProjectMemberships.CountAsync(), await context.Tasks.CountAsync());
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

    private static async Task<UserSummaryResponse[]> ReadUsersAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        return Assert.IsType<UserSummaryResponse[]>(await response.Content.ReadFromJsonAsync<UserSummaryResponse[]>());
    }

    private sealed record Sample(AppUser Caller, AppUser Candidate, AppUser First, AppUser TwinLow, AppUser TwinHigh);
}
