using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Api.Authentication;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Users;
using AppUser = TaskFlow.Domain.AppUser;

namespace TaskFlow.Api.IntegrationTests;

internal sealed class AuthenticationTestFactory : TaskFlowApiFactory
{
    internal const string Issuer = "http://keycloak.test/realms/taskflow";
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly bool _usePostgres;

    internal RsaSecurityKey SigningKey { get; }
    internal IntrospectionTestHandler Introspection { get; } = new();
    internal TestUserStore Users { get; } = new();
    internal TestUnitOfWork UnitOfWork { get; } = new();
    internal string IntrospectionSecret { get; set; } = "test-only-secret";

    internal AuthenticationTestFactory(string? connectionString = null)
        : base(connectionString ?? "Host=127.0.0.1;Port=1;Database=auth_test;Username=postgres;Timeout=1")
    {
        _usePostgres = connectionString is not null;
        SigningKey = new RsaSecurityKey(_rsa) { KeyId = "taskflow-test-key" };
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = Issuer,
                ["Keycloak:Audience"] = "taskflow-api",
                ["Keycloak:Introspection:ClientSecret"] = IntrospectionSecret
            }));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(AuthenticationProbeController).Assembly);
            var discovery = new OpenIdConnectConfiguration { Issuer = Issuer };
            discovery.SigningKeys.Add(new RsaSecurityKey(_rsa.ExportParameters(false))
            {
                KeyId = SigningKey.KeyId
            });
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery));

            services.AddHttpClient<KeycloakIntrospectionClient>()
                .ConfigurePrimaryHttpMessageHandler(() => Introspection);

            if (!_usePostgres)
            {
                services.RemoveAll<IAppUserRepository>();
                services.AddSingleton<IAppUserRepository>(Users);
                services.RemoveAll<IUnitOfWork>();
                services.AddSingleton<IUnitOfWork>(UnitOfWork);
            }
        });
    }

    internal string CreateToken(
        string? subject = null, string[]? roles = null, string? issuer = null,
        string? audience = null, DateTime? expires = null, DateTime? notBefore = null,
        SecurityKey? key = null, string algorithm = SecurityAlgorithms.RsaSha256,
        bool includeSubject = true, bool includeExpiration = true,
        string? realmAccess = null, IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = new List<Claim>();
        if (includeSubject)
        {
            claims.Add(new Claim("sub", subject ?? Guid.NewGuid().ToString()));
        }
        claims.Add(new Claim("realm_access",
            realmAccess ?? JsonSerializer.Serialize(new { roles = roles ?? ["User"] }),
            JsonClaimValueTypes.Json));
        if (additionalClaims is not null)
        {
            claims.AddRange(additionalClaims);
        }

        var token = new JwtSecurityToken(
            issuer: issuer ?? Issuer,
            audience: audience ?? "taskflow-api",
            claims: claims,
            notBefore: notBefore ?? DateTime.UtcNow.AddMinutes(-10),
            expires: includeExpiration ? expires ?? DateTime.UtcNow.AddMinutes(5) : null,
            signingCredentials: algorithm == SecurityAlgorithms.None ? null :
                new SigningCredentials(key ?? SigningKey, algorithm));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _rsa.Dispose();
        }
    }
}

internal sealed class IntrospectionTestHandler : HttpMessageHandler
{
    private int _requestCount;
    internal int RequestCount => Volatile.Read(ref _requestCount);
    internal Func<CancellationToken, Task<HttpResponseMessage>> Respond { get; set; } = _ =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"active\":true}")
        });

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(AuthenticationTestFactory.Issuer + "/protocol/openid-connect/token/introspect",
            request.RequestUri?.ToString());
        Interlocked.Increment(ref _requestCount);
        return Respond(cancellationToken);
    }
}

internal sealed class TestUserStore : IAppUserRepository
{
    internal ConcurrentDictionary<string, AppUser> Records { get; } = new();

    public Task<AppUser> GetOrCreateAsync(AppUser candidate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Records.GetOrAdd(candidate.KeycloakSub, candidate));
    }

    public Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Records.Values.SingleOrDefault(user => user.Id == userId));
    }

    public Task<AppUser?> GetByIdForKeyShareAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        // Este almacén es en memoria; el bloqueo real se comprueba contra PostgreSQL.
        return GetByIdAsync(userId, cancellationToken);
    }

    public Task<AppUser[]> GetByIdsAsync(Guid[] userIds, CancellationToken cancellationToken)
    {
        return Task.FromResult(Records.Values.Where(user => userIds.Contains(user.Id)).ToArray());
    }

    public Task<AppUser[]> GetAssignableAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Records.Values.OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Id).ToArray());
    }
}

internal sealed class TestUnitOfWork : IUnitOfWork
{
    internal int SaveCount { get; private set; }

    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        // Este doble solo se usa con el almacén en memoria de las pruebas de autenticación.
        cancellationToken.ThrowIfCancellationRequested();
        return operation(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}
