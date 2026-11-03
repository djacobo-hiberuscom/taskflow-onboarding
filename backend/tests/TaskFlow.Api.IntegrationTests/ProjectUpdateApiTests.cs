using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Infrastructure.Persistence;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using Project = TaskFlow.Domain.Project;

namespace TaskFlow.Api.IntegrationTests;

public class ProjectUpdateApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("creator", false)]
    [InlineData("member", false)]
    [InlineData("admin", false)]
    [InlineData("admin", true)]
    public async Task Update_AllowedRequest_PersistsDetailsAndPreservesOriginalCreator(
        string actor, bool archived)
    {
        var sample = await SeedAsync(archived);
        using var factory = CreateFactory();
        var user = actor switch
        {
            "creator" => sample.Creator,
            "member" => sample.Member,
            _ => sample.Outsider
        };
        using var client = CreateClient(factory, user, actor == "admin" ? "Admin" : "User");
        var before = DateTimeOffset.UtcNow;

        using var response = await client.PutAsJsonAsync($"/api/v1/projects/{sample.Project.Id}", new
        {
            name = "  Nombre actualizado  ",
            description = "  Descripción actualizada  "
        });
        var after = DateTimeOffset.UtcNow;

        var project = await ReadUpdatedProjectAsync(response, sample.Project.Id);
        Assert.Equal("Nombre actualizado", project.Name);
        Assert.Equal("Descripción actualizada", project.Description);
        Assert.NotNull(project.CreatedBy);
        Assert.Equal(sample.Creator.Id, project.CreatedBy.Id);
        Assert.Equal(sample.Creator.DisplayName, project.CreatedBy.DisplayName);
        Assert.Equal(sample.Project.CreatedAt, project.CreatedAt);
        Assert.Equal(sample.Project.ArchivedAt, project.ArchivedAt);
        Assert.InRange(project.UpdatedAt, before, after);

        await using var context = CreateContext();
        var stored = await context.Projects.SingleAsync(item => item.Id == project.Id);
        Assert.Equal(project.Name, stored.Name);
        Assert.Equal(project.Description, stored.Description);
        Assert.Equal(sample.Creator.Id, stored.CreatedBy);
        Assert.Equal(sample.Project.CreatedAt, stored.CreatedAt);
        Assert.Equal(sample.Project.ArchivedAt, stored.ArchivedAt);
        Assert.InRange((stored.UpdatedAt - project.UpdatedAt).Duration(), TimeSpan.Zero, TimeSpan.FromTicks(9));
        Assert.Equal(new[] { sample.Creator.Id, sample.Member.Id }.OrderBy(id => id),
            await context.ProjectMemberships.Where(item => item.ProjectId == project.Id)
                .OrderBy(item => item.UserId).Select(item => item.UserId).ToArrayAsync());
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("archived")]
    [InlineData("missing")]
    public async Task Update_InvisibleOrMissingProject_Returns404WithoutChanges(string scenario)
    {
        var sample = await SeedAsync(scenario == "archived");
        using var factory = CreateFactory();
        using var client = CreateClient(factory, scenario == "foreign" ? sample.Outsider : sample.Member);
        var id = scenario == "missing" ? Guid.NewGuid() : sample.Project.Id;

        using var response = await client.PutAsJsonAsync($"/api/v1/projects/{id}", new { name = "Cambio rechazado" });

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("omitted")]
    [InlineData("null")]
    [InlineData("empty")]
    [InlineData("whitespace")]
    public async Task Update_OptionalDescription_ClearsExistingDescription(string descriptionCase)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
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

        using var response = await client.PutAsJsonAsync($"/api/v1/projects/{sample.Project.Id}", body);

        var project = await ReadUpdatedProjectAsync(response, sample.Project.Id);
        Assert.Null(project.Description);
        await using var context = CreateContext();
        Assert.Null((await context.Projects.SingleAsync(item => item.Id == sample.Project.Id)).Description);
    }

    [Fact]
    public async Task Update_DeletedCreator_ReturnsNullCreatorAndPreservesMemberships()
    {
        var sample = await SeedAsync();
        await using (var context = CreateContext())
        {
            await context.Users.Where(user => user.Id == sample.Creator.Id).ExecuteDeleteAsync();
        }
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.PutAsJsonAsync($"/api/v1/projects/{sample.Project.Id}",
            new { name = "Proyecto conservado" });

        var project = await ReadUpdatedProjectAsync(response, sample.Project.Id);
        Assert.Null(project.CreatedBy);
        await using var verification = CreateContext();
        Assert.Null((await verification.Projects.SingleAsync(item => item.Id == sample.Project.Id)).CreatedBy);
        Assert.Equal(sample.Member.Id, Assert.Single(await verification.ProjectMemberships
            .Where(item => item.ProjectId == sample.Project.Id).ToListAsync()).UserId);
    }

    [Fact]
    public async Task Update_TextLimitsAreAppliedAfterTrimming()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var name = new string('n', 150);
        var description = new string('d', 5000);

        using var response = await client.PutAsJsonAsync($"/api/v1/projects/{sample.Project.Id}",
            new { name = $"  {name}  ", description = $" \t{description}\r\n " });

        var project = await ReadUpdatedProjectAsync(response, sample.Project.Id);
        Assert.Equal(name, project.Name);
        Assert.Equal(description, project.Description);
        await using var context = CreateContext();
        var stored = await context.Projects.SingleAsync(item => item.Id == sample.Project.Id);
        Assert.Equal(name, stored.Name);
        Assert.Equal(description, stored.Description);
    }

    [Theory]
    [InlineData("empty-name", "name")]
    [InlineData("whitespace-name", "name")]
    [InlineData("long-name", "name")]
    [InlineData("long-description", "description")]
    public async Task Update_InvalidField_Returns400WithoutChanges(string scenario, string field)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = new
        {
            name = scenario switch
            {
                "empty-name" => "",
                "whitespace-name" => " \t\r\n ",
                "long-name" => new string('n', 151),
                _ => "Nombre válido"
            },
            description = scenario == "long-description" ? new string('d', 5001) : null
        };

        using var response = await client.PutAsJsonAsync($"/api/v1/projects/{sample.Project.Id}", body);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, field);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"name\":null}")]
    [InlineData("null")]
    [InlineData("{\"name\":42}")]
    [InlineData("{\"name\":")]
    public async Task Update_InvalidOrMissingBody_Returns400WithoutChanges(string body)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.PutAsync($"/api/v1/projects/{sample.Project.Id}", content);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("createdBy")]
    [InlineData("archivedAt")]
    [InlineData("members")]
    [InlineData("id")]
    public async Task Update_ProtectedProperty_Returns400WithoutChanges(string property)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var body = new Dictionary<string, object?> { ["name"] = "Nombre válido", [property] = Guid.NewGuid() };

        using var response = await client.PutAsJsonAsync($"/api/v1/projects/{sample.Project.Id}", body);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task Update_InvalidIdentifier_Returns400()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.PutAsJsonAsync("/api/v1/projects/not-a-guid", new { name = "Nombre válido" });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertUnchangedAsync(sample);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task Update_InvalidSession_DoesNotChangeProject(string session, HttpStatusCode expected)
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

        using var response = await client.PutAsJsonAsync($"/api/v1/projects/{sample.Project.Id}",
            new { name = "Cambio rechazado" });

        await AssertProblemAsync(response, expected);
        await AssertUnchangedAsync(sample);
    }

    [Fact]
    public async Task Update_FailureAfterSqlUpdate_RollsBackChanges()
    {
        var sample = await SeedAsync();
        var interceptor = new FailAfterProjectUpdateInterceptor();
        using var factory = CreateFactory();
        using var failingFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<TaskFlowDbContext>(options => options.AddInterceptors(interceptor))));
        using var client = failingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(sample.Member.KeycloakSub));

        using var response = await client.PutAsJsonAsync($"/api/v1/projects/{sample.Project.Id}",
            new { name = "Cambio revertido", description = "Cambio revertido" });

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.True(interceptor.UpdateExecuted);
        await AssertUnchangedAsync(sample);
    }

    private async Task<Sample> SeedAsync(bool archived = false)
    {
        var creator = new AppUser(Guid.NewGuid().ToString(), "Creadora original", null);
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro editor", null);
        var outsider = new AppUser(Guid.NewGuid().ToString(), "Otra persona", null);
        var project = new Project($"Proyecto {Guid.NewGuid():N}", "Descripción inicial", creator.Id);
        if (archived)
        {
            project.Archive();
        }
        await using var context = CreateContext();
        context.Users.AddRange(creator, member, outsider);
        context.Projects.Add(project);
        var createdAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        context.Entry(project).Property(item => item.CreatedAt).CurrentValue = createdAt;
        context.Entry(project).Property(item => item.UpdatedAt).CurrentValue = createdAt.AddDays(1);
        if (archived)
        {
            context.Entry(project).Property(item => item.ArchivedAt).CurrentValue = createdAt.AddDays(1);
        }
        context.ProjectMemberships.AddRange(
            new AppUserProject(project.Id, creator.Id), new AppUserProject(project.Id, member.Id));
        await context.SaveChangesAsync();
        return new Sample(project, creator, member, outsider);
    }

    private async Task AssertUnchangedAsync(Sample sample)
    {
        await using var context = CreateContext();
        var stored = await context.Projects.SingleAsync(item => item.Id == sample.Project.Id);
        Assert.Equal(sample.Project.Name, stored.Name);
        Assert.Equal(sample.Project.Description, stored.Description);
        Assert.Equal(sample.Project.CreatedBy, stored.CreatedBy);
        Assert.Equal(sample.Project.CreatedAt, stored.CreatedAt);
        Assert.Equal(sample.Project.UpdatedAt, stored.UpdatedAt);
        Assert.Equal(sample.Project.ArchivedAt, stored.ArchivedAt);
        Assert.Equal(2, await context.ProjectMemberships.CountAsync(item => item.ProjectId == stored.Id));
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

    private static async Task<ProjectResponse> ReadUpdatedProjectAsync(HttpResponseMessage response, Guid id)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var project = Assert.IsType<ProjectResponse>(await response.Content.ReadFromJsonAsync<ProjectResponse>());
        Assert.Equal(id, project.Id);
        return project;
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string? field = null)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, document.RootElement.GetProperty("status").GetInt32());
        if (field is not null)
        {
            Assert.NotEmpty(document.RootElement.GetProperty("errors").GetProperty(field).EnumerateArray());
        }
    }

    private sealed record Sample(Project Project, AppUser Creator, AppUser Member, AppUser Outsider);

    private sealed class FailAfterProjectUpdateInterceptor : DbCommandInterceptor
    {
        public bool UpdateExecuted { get; private set; }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            CheckForUpdate(command);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            CheckForUpdate(command);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void CheckForUpdate(DbCommand command)
        {
            if (command.CommandText.Contains("UPDATE project SET", StringComparison.Ordinal))
            {
                UpdateExecuted = true;
                throw new InvalidOperationException("Fallo simulado después del UPDATE.");
            }
        }
    }
}
