using Testcontainers.PostgreSql;
using Xunit;

namespace TaskFlow.Api.IntegrationTests;

public class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Database { get; } = 
        new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("taskflow_test")
        .Build();

    public Task InitializeAsync()
    {
        return Database.StartAsync();
    }

    public Task DisposeAsync()
    {
        return Database.DisposeAsync().AsTask();
    }
}