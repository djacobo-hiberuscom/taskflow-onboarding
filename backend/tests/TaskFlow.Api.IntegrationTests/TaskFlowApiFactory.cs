using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace TaskFlow.Api.IntegrationTests;

public class TaskFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public TaskFlowApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureHostConfiguration(configuration =>
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:TaskFlowDb"] = _connectionString
                }
            ));
        
        return base.CreateHost(builder);
    }
}