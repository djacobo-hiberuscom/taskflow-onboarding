using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using System.Text.Json;

namespace TaskFlow.Api.IntegrationTests;

public class HealthTest : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public HealthTest(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Health_Returns200_WhenDatabaseIsAvailable()
    {
        using var factory = new TaskFlowApiFactory(_postgres.Database.GetConnectionString());
        
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("Healthy", body);

    }

    [Fact]
    public async Task Health_Returns503_WhenDatabaseIsUnavailable()
    {
        using var factory = new TaskFlowApiFactory(_postgres.Database.GetConnectionString());

        using var client = factory.CreateClient();

        try
        {
            await _postgres.Database.StopAsync();

            using var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            var body = await response.Content.ReadAsStringAsync();

            using var json = JsonDocument.Parse(body);

            Assert.Equal(503, json.RootElement.GetProperty("status").GetInt32());
        }
        finally
        {
            await _postgres.Database.StartAsync();
        }
    }
    
}
