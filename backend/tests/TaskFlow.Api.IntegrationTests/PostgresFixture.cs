using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace TaskFlow.Api.IntegrationTests;

public class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Database { get; } = 
        new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("taskflow_test")
        .Build();

    public async Task InitializeAsync()
    {
        await Database.StartAsync();

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseNpgsql(Database.GetConnectionString())
            .Options;

        await using var context = new TaskFlowDbContext(options);

        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return Database.DisposeAsync().AsTask();
    }
}