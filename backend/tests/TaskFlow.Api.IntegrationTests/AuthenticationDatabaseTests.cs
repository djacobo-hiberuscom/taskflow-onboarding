using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts.Users;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Api.IntegrationTests;

public class AuthenticationDatabaseTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Me_ConcurrentFirstRequests_CreateOnePostgresUser()
    {
        using var factory = new AuthenticationTestFactory(postgres.Database.GetConnectionString());
        using var client = factory.CreateClient();
        var subject = Guid.NewGuid().ToString();
        var token = factory.CreateToken(subject, additionalClaims: [new Claim("name", "Usuario concurrente")]);

        var profiles = await Task.WhenAll(Enumerable.Range(0, 16).Select(async _ =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return Assert.IsType<CurrentUserResponse>(await response.Content.ReadFromJsonAsync<CurrentUserResponse>());
        }));

        Assert.Single(profiles.Select(profile => profile.Id).Distinct());
        Assert.Equal(16, factory.Introspection.RequestCount);

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseNpgsql(postgres.Database.GetConnectionString()).Options;
        await using var context = new TaskFlowDbContext(options);
        var user = Assert.Single(await context.Users.Where(user => user.KeycloakSub == subject).ToListAsync());
        Assert.Equal(profiles[0].Id, user.Id);
        Assert.Equal("Usuario concurrente", user.DisplayName);
    }

    [Fact]
    public async Task Health_WithInvalidToken_RemainsAnonymousAndSkipsIntrospection()
    {
        using var factory = new AuthenticationTestFactory(postgres.Database.GetConnectionString());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, factory.Introspection.RequestCount);
    }
}
