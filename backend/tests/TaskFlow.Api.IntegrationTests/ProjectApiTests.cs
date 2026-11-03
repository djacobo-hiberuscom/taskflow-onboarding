using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Api.IntegrationTests;

public class ProjectApiTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public ProjectApiTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task Create_ValidRequest_PersistsProjectAndCreatorMembership(string role)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var subject = Guid.NewGuid().ToString();
        Authenticate(client, factory.CreateToken(subject, [role], additionalClaims:
            [new Claim("name", "Usuario creador"), new Claim("email", "creator@example.com")]));

        var beforeCreation = DateTimeOffset.UtcNow;
        using var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            name = "  Proyecto de prueba  ",
            description = "  Descripción de prueba  "
        });
        var afterCreation = DateTimeOffset.UtcNow;

        var project = await ReadCreatedProjectAsync(response);
        Assert.Equal("Proyecto de prueba", project.Name);
        Assert.Equal("Descripción de prueba", project.Description);
        Assert.NotNull(project.CreatedBy);
        Assert.Equal("Usuario creador", project.CreatedBy.DisplayName);
        Assert.NotEqual(Guid.Parse(subject), project.CreatedBy.Id);
        Assert.Null(project.ArchivedAt);
        Assert.InRange(project.CreatedAt, beforeCreation, afterCreation);
        Assert.Equal(project.CreatedAt, project.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, project.CreatedAt.Offset);

        // Un contexto distinto demuestra que los datos están en PostgreSQL, no solo en el tracker.
        await using var context = CreateContext();
        var stored = await context.Projects.SingleAsync(item => item.Id == project.Id);
        var creator = await context.Users.SingleAsync(user => user.KeycloakSub == subject);
        var membership = Assert.Single(await context.ProjectMemberships
            .Where(item => item.ProjectId == project.Id).ToListAsync());

        Assert.Equal(creator.Id, project.CreatedBy.Id);
        Assert.Equal(creator.Id, stored.CreatedBy);
        Assert.Equal(creator.Id, membership.UserId);
        Assert.Equal(project.Name, stored.Name);
        Assert.Equal(project.Description, stored.Description);
        Assert.Null(stored.ArchivedAt);
        // PostgreSQL almacena microsegundos y DateTimeOffset puede contener fracciones menores.
        Assert.InRange((stored.CreatedAt - project.CreatedAt).Duration(),
            TimeSpan.Zero, TimeSpan.FromTicks(9));
        Assert.Equal(stored.CreatedAt, stored.UpdatedAt);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "displayName", "id" }, json.RootElement.GetProperty("createdBy")
            .EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
    }

    [Theory]
    [InlineData("omitted")]
    [InlineData("null")]
    [InlineData("empty")]
    [InlineData("whitespace")]
    public async Task Create_OptionalDescription_IsStoredAndReturnedAsNull(string descriptionCase)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        Authenticate(client, factory.CreateToken());
        var body = new Dictionary<string, object?> { ["name"] = "Proyecto sin descripción" };
        if (descriptionCase != "omitted")
        {
            body["description"] = descriptionCase switch
            {
                "null" => null,
                "empty" => "",
                _ => " \t\r\n "
            };
        }

        using var response = await client.PostAsJsonAsync("/api/v1/projects", body);
        var project = await ReadCreatedProjectAsync(response);

        Assert.Null(project.Description);
        await using var context = CreateContext();
        var stored = await context.Projects.SingleAsync(item => item.Id == project.Id);
        Assert.Null(stored.Description);
    }

    [Fact]
    public async Task Create_TextAtMaximumLengthAfterTrimming_IsAccepted()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        Authenticate(client, factory.CreateToken());
        var name = new string('n', 150);
        var description = new string('d', 5000);

        using var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            name = $"  {name}  ",
            description = $" \t{description}\r\n "
        });
        var project = await ReadCreatedProjectAsync(response);

        Assert.Equal(name, project.Name);
        Assert.Equal(description, project.Description);
        await using var context = CreateContext();
        var stored = await context.Projects.SingleAsync(item => item.Id == project.Id);
        Assert.Equal(name, stored.Name);
        Assert.Equal(description, stored.Description);
    }

    [Fact]
    public async Task Create_RepeatedName_CreatesDifferentProjectsWithTheirMemberships()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        Authenticate(client, factory.CreateToken());
        var body = new { name = $"Proyecto repetido {Guid.NewGuid():N}" };

        using var firstResponse = await client.PostAsJsonAsync("/api/v1/projects", body);
        using var secondResponse = await client.PostAsJsonAsync("/api/v1/projects", body);
        var first = await ReadCreatedProjectAsync(firstResponse);
        var second = await ReadCreatedProjectAsync(secondResponse);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.CreatedBy!.Id, second.CreatedBy!.Id);
        var ids = new[] { first.Id, second.Id };
        await using var context = CreateContext();
        Assert.Equal(2, await context.Projects.CountAsync(project => ids.Contains(project.Id)));
        Assert.Equal(2, await context.ProjectMemberships.CountAsync(membership =>
            ids.Contains(membership.ProjectId) && membership.UserId == first.CreatedBy.Id));
    }

    [Theory]
    [InlineData("empty-name", "name")]
    [InlineData("whitespace-name", "name")]
    [InlineData("long-name", "name")]
    [InlineData("long-description", "description")]
    public async Task Create_InvalidField_Returns400WithFieldErrorWithoutSaving(
        string invalidField, string expectedField)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        Authenticate(client, factory.CreateToken());
        var before = await ReadProjectCountsAsync();
        var body = new
        {
            name = invalidField switch
            {
                "empty-name" => "",
                "whitespace-name" => " \t\r\n ",
                "long-name" => new string('n', 151),
                _ => "Proyecto válido"
            },
            description = invalidField == "long-description" ? new string('d', 5001) : null
        };

        using var response = await client.PostAsJsonAsync("/api/v1/projects", body);

        await AssertValidationProblemAsync(response, expectedField);
        Assert.Equal(before, await ReadProjectCountsAsync());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"name\":null}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"name\":42}")]
    [InlineData("{\"name\":\"Proyecto\",\"description\":42}")]
    [InlineData("{\"name\":")]
    public async Task Create_InvalidJsonOrMissingName_Returns400WithoutSaving(string body)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        Authenticate(client, factory.CreateToken());
        var before = await ReadProjectCountsAsync();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/v1/projects", content);

        await AssertValidationProblemAsync(response);
        Assert.Equal(before, await ReadProjectCountsAsync());
    }

    [Theory]
    [InlineData("createdBy")]
    [InlineData("archivedAt")]
    [InlineData("members")]
    [InlineData("id")]
    [InlineData("unknownProperty")]
    public async Task Create_UnknownOrProtectedProperty_Returns400WithoutSaving(string property)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        Authenticate(client, factory.CreateToken());
        var before = await ReadProjectCountsAsync();
        var body = new Dictionary<string, object?>
        {
            ["name"] = "Proyecto válido",
            [property] = Guid.NewGuid().ToString()
        };

        using var response = await client.PostAsJsonAsync("/api/v1/projects", body);

        await AssertValidationProblemAsync(response);
        Assert.Equal(before, await ReadProjectCountsAsync());
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task Create_UnauthorizedOrUnverifiableSession_DoesNotWriteToDatabase(
        string session, HttpStatusCode expectedStatus)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        if (session != "missing-token")
        {
            Authenticate(client, factory.CreateToken(roles: session == "missing-role" ? ["Other"] : ["User"]));
        }
        if (session is "inactive-token" or "introspection-unavailable")
        {
            factory.Introspection.Respond = _ => Task.FromResult(new HttpResponseMessage(
                session == "inactive-token" ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("{\"active\":false}")
            });
        }
        var before = await ReadProjectCountsAsync();
        await using var context = CreateContext();
        var usersBefore = await context.Users.CountAsync();

        using var response = await client.PostAsJsonAsync("/api/v1/projects", new { name = "Proyecto rechazado" });

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(before, await ReadProjectCountsAsync());
        Assert.Equal(usersBefore, await context.Users.CountAsync());
    }

    [Fact]
    public async Task Create_WhenMembershipInsertFails_RollsBackProjectInsert()
    {
        using var factory = CreateFactory();
        var interceptor = new RejectMembershipInsertInterceptor();
        using var failingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddDbContext<TaskFlowDbContext>(options =>
                options.UseNpgsql(_postgres.Database.GetConnectionString(), postgres => postgres.MaxBatchSize(1))
                    .AddInterceptors(interceptor))));
        using var client = failingFactory.CreateClient();
        Authenticate(client, factory.CreateToken());
        var before = await ReadProjectCountsAsync();

        using var response = await client.PostAsJsonAsync("/api/v1/projects", new { name = "Proyecto revertido" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("Fallo de prueba", await response.Content.ReadAsStringAsync());
        Assert.True(interceptor.ProjectInsertCompleted,
            "La prueba debe ejecutar primero el INSERT del proyecto antes de provocar el fallo de la pertenencia.");
        Assert.Equal(before, await ReadProjectCountsAsync());
    }

    private AuthenticationTestFactory CreateFactory() => new(_postgres.Database.GetConnectionString());

    private TaskFlowDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseNpgsql(_postgres.Database.GetConnectionString()).Options;
        return new TaskFlowDbContext(options);
    }

    private async Task<(int Projects, int Memberships)> ReadProjectCountsAsync()
    {
        await using var context = CreateContext();
        return (await context.Projects.CountAsync(), await context.ProjectMemberships.CountAsync());
    }

    private static void Authenticate(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<ProjectResponse> ReadCreatedProjectAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var project = Assert.IsType<ProjectResponse>(await response.Content.ReadFromJsonAsync<ProjectResponse>());
        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal($"/api/v1/projects/{project.Id}", response.Headers.Location?.OriginalString);
        return project;
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string? field = null)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        var errors = document.RootElement.GetProperty("errors");
        Assert.NotEmpty(errors.EnumerateObject());
        if (field is not null)
        {
            Assert.True(errors.TryGetProperty(field, out var messages), $"Falta el error del campo {field}.");
            Assert.NotEmpty(messages.EnumerateArray());
        }
    }

    private sealed class RejectMembershipInsertInterceptor : DbCommandInterceptor
    {
        public bool ProjectInsertCompleted { get; private set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            RejectMembershipInsert(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            RejectMembershipInsert(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            RecordProjectInsert(command);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            RecordProjectInsert(command);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void RecordProjectInsert(DbCommand command)
        {
            if (command.CommandText.Contains("INSERT INTO project ", StringComparison.Ordinal))
            {
                ProjectInsertCompleted = true;
            }
        }

        private static void RejectMembershipInsert(DbCommand command)
        {
            if (command.CommandText.Contains("INSERT INTO app_user_project", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Fallo de prueba al insertar la pertenencia.");
            }
        }
    }
}
