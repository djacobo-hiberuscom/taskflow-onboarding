using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Seeding;
using AppUser = TaskFlow.Domain.AppUser;

namespace TaskFlow.Api.IntegrationTests;

public class DemoUserSeederTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Seed_ActualRealmTwice_PreservesIdsDatesAndExactlyFourProfiles()
    {
        await ResetAsync();
        using var factory = CreateFactory();
        var realmPath = GetRealmPath();

        await SeedAsync(factory, realmPath);
        var first = await ReadUsersAsync();
        await SeedAsync(factory, realmPath);
        var second = await ReadUsersAsync();

        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Equal(4, second.Length);
        using var realm = JsonDocument.Parse(await File.ReadAllTextAsync(realmPath));
        foreach (var identity in realm.RootElement.GetProperty("users").EnumerateArray())
        {
            var user = Assert.Single(second, item => item.KeycloakSub == identity.GetProperty("id").GetString());
            var expectedName = $"{identity.GetProperty("firstName").GetString()} {identity.GetProperty("lastName").GetString()}".Trim();
            Assert.Equal(expectedName, user.DisplayName);
            Assert.Equal(identity.GetProperty("email").GetString(), user.Email);
            Assert.NotEqual(Guid.Empty, user.Id);
        }
        await using var context = CreateContext();
        Assert.Empty(await context.Projects.ToArrayAsync());
        Assert.Empty(await context.ProjectMemberships.ToArrayAsync());
        Assert.Empty(await context.Tasks.ToArrayAsync());
        Assert.Equal(0, factory.Introspection.RequestCount);
    }

    [Fact]
    public async Task Seed_ExistingProfileFromEarlierLogin_PreservesItAndUnrelatedUsers()
    {
        await ResetAsync();
        using var realm = JsonDocument.Parse(await File.ReadAllTextAsync(GetRealmPath()));
        var subject = realm.RootElement.GetProperty("users")[0].GetProperty("id").GetString()!;
        var existing = new AppUser(subject, "Nombre actualizado desde el token", "current@example.com");
        var unrelated = new AppUser(Guid.NewGuid().ToString(), "Ajeno a la demo", null);
        await using (var context = CreateContext())
        {
            context.Users.AddRange(existing, unrelated);
            await context.SaveChangesAsync();
            await context.Entry(existing).ReloadAsync();
            await context.Entry(unrelated).ReloadAsync();
        }
        using var factory = CreateFactory();

        await SeedAsync(factory, GetRealmPath());

        var users = await ReadUsersAsync();
        Assert.Equal(5, users.Length);
        Assert.Equal(JsonSerializer.Serialize(existing), JsonSerializer.Serialize(
            Assert.Single(users, user => user.KeycloakSub == subject)));
        Assert.Equal(JsonSerializer.Serialize(unrelated), JsonSerializer.Serialize(
            Assert.Single(users, user => user.Id == unrelated.Id)));
    }

    [Fact]
    public async Task Seed_ConcurrentExecutions_CreateOneProfilePerSubject()
    {
        await ResetAsync();
        using var factory = CreateFactory();

        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => SeedAsync(factory, GetRealmPath())));

        var users = await ReadUsersAsync();
        Assert.Equal(4, users.Length);
        Assert.Equal(4, users.Select(user => user.KeycloakSub).Distinct().Count());
        Assert.Equal(4, users.Select(user => user.Id).Distinct().Count());
    }

    [Theory]
    [InlineData("usuario.prueba", "usuario.prueba")]
    [InlineData(null, "fallback")]
    public async Task Seed_EmptyPersonalName_UsesUsernameOrLocalFallback(string? username, string expected)
    {
        await ResetAsync();
        var subject = Guid.NewGuid().ToString();
        using var file = await RealmFileAsync(subject, username, " ", null, "invalid-email");
        using var factory = CreateFactory();

        await SeedAsync(factory, file.Path);

        var user = Assert.Single(await ReadUsersAsync());
        Assert.Equal(subject, user.KeycloakSub);
        Assert.Equal(expected == "fallback" ? $"Usuario {user.Id.ToString("N")[..8]}" : expected, user.DisplayName);
        Assert.Null(user.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Seed_EmptySubject_IsRejectedWithoutCreatingAProfile(string? subject)
    {
        await ResetAsync();
        using var file = await RealmFileAsync(subject, "demo", "Demo", "User", null);
        using var factory = CreateFactory();

        await Assert.ThrowsAsync<InvalidDataException>(() => SeedAsync(factory, file.Path));

        Assert.Empty(await ReadUsersAsync());
    }

    [Fact]
    public async Task Seed_InvalidJson_IsRejectedWithoutWriting()
    {
        await ResetAsync();
        using var file = await TemporaryRealmFile.CreateAsync("{broken-json");
        using var factory = CreateFactory();

        await Assert.ThrowsAnyAsync<JsonException>(() => SeedAsync(factory, file.Path));

        Assert.Empty(await ReadUsersAsync());
    }

    [Fact]
    public async Task Seed_MissingFile_IsRejectedWithoutWriting()
    {
        await ResetAsync();
        using var factory = CreateFactory();
        var missingFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"taskflow-missing-{Guid.NewGuid():N}.json");

        await Assert.ThrowsAsync<FileNotFoundException>(() => SeedAsync(factory, missingFile));

        Assert.Empty(await ReadUsersAsync());
    }

    [Fact]
    public async Task Seed_AlreadyCancelled_DoesNotWrite()
    {
        await ResetAsync();
        using var factory = CreateFactory();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            SeedAsync(factory, GetRealmPath(), cancellation.Token));

        Assert.Empty(await ReadUsersAsync());
    }

    [Fact]
    public async Task Command_DevelopmentTwice_SeedsAndExitsWithoutStartingHttpServer()
    {
        await ResetAsync();

        var firstRun = await RunCommandAsync("Development");
        Assert.Equal(0, firstRun.ExitCode);
        Assert.Contains("Semilla de usuarios completada.", firstRun.Output);
        Assert.DoesNotContain("Now listening on", firstRun.Output);
        var firstUsers = await ReadUsersAsync();
        Assert.Equal(4, firstUsers.Length);

        var secondRun = await RunCommandAsync("Development");
        Assert.Equal(0, secondRun.ExitCode);
        Assert.Equal(JsonSerializer.Serialize(firstUsers), JsonSerializer.Serialize(await ReadUsersAsync()));
    }

    [Fact]
    public async Task Startup_Production_RejectsDemoSeedWithoutWriting()
    {
        await ResetAsync();
        using var factory = new ProductionSeedFactory(postgres.Database.GetConnectionString());

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("solo se permite en Development", exception.Message);
        Assert.Empty(await ReadUsersAsync());
    }

    private async Task SeedAsync(AuthenticationTestFactory factory, string realmPath,
        CancellationToken cancellationToken = default)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DemoUserSeeder>();
        await seeder.SeedAsync(realmPath, cancellationToken);
    }

    private async Task<(int ExitCode, string Output)> RunCommandAsync(string environment)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = System.IO.Path.Combine(GetRepositoryRoot(), "backend", "src", "TaskFlow.Api"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        start.ArgumentList.Add(typeof(Program).Assembly.Location);
        start.ArgumentList.Add("--SeedDemoUsers:RealmFile");
        start.ArgumentList.Add(GetRealmPath());
        start.Environment["DOTNET_ENVIRONMENT"] = environment;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        // Solo se suministra la conexión del Testcontainer de esta clase.
        start.Environment["ConnectionStrings__TaskFlowDb"] = postgres.Database.GetConnectionString();
        start.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
        using var process = Process.Start(start) ?? throw new InvalidOperationException("No se inició el comando de prueba.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            return (process.ExitCode, await output + await error);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    private async Task ResetAsync()
    {
        // Limpieza exclusiva de la base temporal de esta fixture.
        await using var context = CreateContext();
        await context.Projects.ExecuteDeleteAsync();
        await context.Users.ExecuteDeleteAsync();
    }

    private async Task<AppUser[]> ReadUsersAsync()
    {
        await using var context = CreateContext();
        return await context.Users.AsNoTracking().OrderBy(user => user.KeycloakSub).ToArrayAsync();
    }

    private AuthenticationTestFactory CreateFactory() => new(postgres.Database.GetConnectionString());

    private TaskFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<TaskFlowDbContext>()
        .UseNpgsql(postgres.Database.GetConnectionString()).Options);

    private static string GetRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "infrastructure", "keycloak", "realm", "realm-export.json")))
                return directory.FullName;
        throw new DirectoryNotFoundException("No se encontró el realm de la demo en el repositorio.");
    }

    private static string GetRealmPath() => System.IO.Path.Combine(GetRepositoryRoot(), "infrastructure", "keycloak", "realm", "realm-export.json");

    private static Task<TemporaryRealmFile> RealmFileAsync(string? subject, string? username,
        string? firstName, string? lastName, string? email) => TemporaryRealmFile.CreateAsync(JsonSerializer.Serialize(new
        {
            users = new[] { new { id = subject, username, firstName, lastName, email } }
        }));

    private sealed class ProductionSeedFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:TaskFlowDb"] = connectionString,
                    ["SeedDemoUsers:RealmFile"] = GetRealmPath()
                }));
            return base.CreateHost(builder);
        }
    }

    private sealed class TemporaryRealmFile(string path) : IDisposable
    {
        public string Path { get; } = path;

        public static async Task<TemporaryRealmFile> CreateAsync(string json)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"taskflow-seed-test-{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(path, json);
            return new TemporaryRealmFile(path);
        }

        public void Dispose() => File.Delete(Path);
    }
}
