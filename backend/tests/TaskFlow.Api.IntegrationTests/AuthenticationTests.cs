using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Api.Authentication;
using TaskFlow.Api.Contracts.Users;

namespace TaskFlow.Api.IntegrationTests;

public class AuthenticationTests
{
    [Fact]
    public async Task Me_WithoutToken_Returns401WithoutCreatingUser()
    {
        using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, value => value.Scheme == "Bearer");
        Assert.Empty(factory.Users.Records);
        Assert.Equal(0, factory.Introspection.RequestCount);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("no-expiration")]
    [InlineData("no-subject")]
    [InlineData("empty-subject")]
    [InlineData("long-subject")]
    [InlineData("unsigned")]
    [InlineData("symmetric")]
    public async Task Me_InvalidToken_Returns401BeforeIntrospectionOrPersistence(string invalidPart)
    {
        using var factory = new AuthenticationTestFactory();
        using var otherRsa = RSA.Create(2048);
        var token = invalidPart switch
        {
            "malformed" => "not-a-jwt",
            "signature" => factory.CreateToken(key: new RsaSecurityKey(otherRsa)),
            "issuer" => factory.CreateToken(issuer: "http://other.test/realms/taskflow"),
            "audience" => factory.CreateToken(audience: "taskflow-spa"),
            "expired" => factory.CreateToken(expires: DateTime.UtcNow.AddMinutes(-2)),
            "future" => factory.CreateToken(notBefore: DateTime.UtcNow.AddMinutes(2)),
            "no-expiration" => factory.CreateToken(includeExpiration: false),
            "no-subject" => factory.CreateToken(includeSubject: false),
            "empty-subject" => factory.CreateToken(subject: " "),
            "long-subject" => factory.CreateToken(subject: new string('s', 256)),
            "unsigned" => factory.CreateToken(algorithm: SecurityAlgorithms.None),
            "symmetric" => factory.CreateToken(
                key: new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)),
                algorithm: SecurityAlgorithms.HmacSha256),
            _ => throw new ArgumentOutOfRangeException(nameof(invalidPart))
        };
        using var client = factory.CreateClient();
        using var response = await SendMeAsync(client, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(factory.Users.Records);
        Assert.Equal(0, factory.Introspection.RequestCount);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task Me_ValidToken_ReturnsLocalProfileAndMappedRole(string role)
    {
        using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient();
        var subject = Guid.NewGuid().ToString();
        var token = factory.CreateToken(subject, [role], additionalClaims:
        [
            new Claim("name", "  Usuario de prueba  "),
            new Claim("email", "test@example.com")
        ]);
        using var response = await SendMeAsync(client, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = Assert.IsType<CurrentUserResponse>(await response.Content.ReadFromJsonAsync<CurrentUserResponse>());
        Assert.Equal("Usuario de prueba", profile.DisplayName);
        Assert.Equal("test@example.com", profile.Email);
        Assert.Equal(new[] { role }, profile.Roles);
        Assert.NotEqual(Guid.Parse(subject), profile.Id);
        Assert.Equal(factory.Users.Records[subject].Id, profile.Id);
        Assert.Equal(1, factory.Introspection.RequestCount);
    }

    [Theory]
    [InlineData("{\"roles\":[\"Other\"]}")]
    [InlineData("{\"roles\":[\"user\"]}")]
    [InlineData("{\"roles\":[]}")]
    [InlineData("{\"roles\":\"Admin\"}")]
    [InlineData("[]")]
    public async Task Me_WithoutAcceptedRealmRole_Returns403WithoutCreatingUser(string realmAccess)
    {
        using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient();
        // Un rol plano no debe sustituir a realm_access.roles.
        var token = factory.CreateToken(realmAccess: realmAccess,
            additionalClaims: [new Claim(ClaimTypes.Role, "Admin")]);
        using var response = await SendMeAsync(client, token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Empty(factory.Users.Records);
        Assert.Equal(1, factory.Introspection.RequestCount);
    }

    [Theory]
    [InlineData("inactive", HttpStatusCode.Unauthorized)]
    [InlineData("http-error", HttpStatusCode.ServiceUnavailable)]
    [InlineData("client-error", HttpStatusCode.ServiceUnavailable)]
    [InlineData("invalid-json", HttpStatusCode.ServiceUnavailable)]
    [InlineData("missing-active", HttpStatusCode.ServiceUnavailable)]
    [InlineData("invalid-active", HttpStatusCode.ServiceUnavailable)]
    [InlineData("network", HttpStatusCode.ServiceUnavailable)]
    [InlineData("timeout", HttpStatusCode.ServiceUnavailable)]
    public async Task Me_IntrospectionFailure_DoesNotExecuteBusinessOperation(
        string failure, HttpStatusCode expectedStatus)
    {
        using var factory = new AuthenticationTestFactory();
        factory.Introspection.Respond = _ => failure switch
        {
            "network" => throw new HttpRequestException("Test connection failure"),
            "timeout" => throw new TaskCanceledException("Test timeout"),
            _ => Task.FromResult(new HttpResponseMessage(failure switch
            {
                "http-error" => HttpStatusCode.InternalServerError,
                "client-error" => HttpStatusCode.Unauthorized,
                _ => HttpStatusCode.OK
            })
            {
                Content = new StringContent(failure switch
                {
                    "inactive" => "{\"active\":false}",
                    "invalid-json" => "not-json",
                    "missing-active" => "{}",
                    "invalid-active" => "{\"active\":\"true\"}",
                    _ => "{}"
                })
            })
        };
        using var client = factory.CreateClient();
        using var response = await SendMeAsync(client, factory.CreateToken());

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Empty(factory.Users.Records);
        if (expectedStatus == HttpStatusCode.ServiceUnavailable)
        {
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Me_SameTokenRevokedBetweenRequests_IsNotAcceptedFromCache()
    {
        using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient();
        var token = factory.CreateToken();
        using var first = await SendMeAsync(client, token);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        factory.Introspection.Respond = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"active\":false}")
        });
        using var second = await SendMeAsync(client, token);

        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(2, factory.Introspection.RequestCount);
        Assert.Single(factory.Users.Records);
    }

    [Fact]
    public async Task Me_MissingIntrospectionSecret_Returns503WithoutCreatingUser()
    {
        using var factory = new AuthenticationTestFactory { IntrospectionSecret = "" };
        using var client = factory.CreateClient();
        using var response = await SendMeAsync(client, factory.CreateToken());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Empty(factory.Users.Records);
        Assert.Equal(0, factory.Introspection.RequestCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CurrentUser_ResolvesLocalIdentityAndAdminPolicy(bool isAdmin)
    {
        using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient();
        var subject = Guid.NewGuid().ToString();
        var token = factory.CreateToken(subject, isAdmin ? ["Admin", "User"] : ["User"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var currentUser = await client.GetAsync("/__tests/current-user");

        Assert.Equal(HttpStatusCode.OK, currentUser.StatusCode);
        using var document = JsonDocument.Parse(await currentUser.Content.ReadAsStringAsync());
        Assert.Equal(factory.Users.Records[subject].Id, document.RootElement.GetProperty("userId").GetGuid());
        Assert.Equal(isAdmin, document.RootElement.GetProperty("isAdmin").GetBoolean());

        using var adminAction = await client.GetAsync("/__tests/current-user/admin");
        Assert.Equal(isAdmin ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden, adminAction.StatusCode);
    }

    [Fact]
    public async Task RoleTransformation_RepeatedExecution_DoesNotDuplicateRoles()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("realm_access", "{\"roles\":[\"User\",\"User\",\"unknown\"]}")], "Bearer"));
        var transformation = new KeycloakRolesTransformation();

        var first = await transformation.TransformAsync(principal);
        var second = await transformation.TransformAsync(first);

        Assert.True(second.IsInRole("User"));
        Assert.False(second.IsInRole("unknown"));
        Assert.Single(second.FindAll(ClaimTypes.Role));
        Assert.Empty(principal.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public async Task Me_RefreshedClaims_UpdateSameLocalUserOnlyWhenProfileChanges()
    {
        using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient();
        var subject = Guid.NewGuid().ToString();
        var initialToken = factory.CreateToken(subject, additionalClaims:
            [new Claim("name", "Nombre inicial"), new Claim("email", "initial@example.com")]);
        using var initial = await SendMeAsync(client, initialToken);
        Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        var user = factory.Users.Records[subject];
        var createdAt = user.CreatedAt;
        Assert.Equal(0, factory.UnitOfWork.SaveCount);

        var refreshedToken = factory.CreateToken(subject, ["Admin"], additionalClaims:
            [new Claim("name", "Nombre actualizado"), new Claim("email", "updated@example.com")]);
        using var refreshed = await SendMeAsync(client, refreshedToken);
        var profile = Assert.IsType<CurrentUserResponse>(await refreshed.Content.ReadFromJsonAsync<CurrentUserResponse>());
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal(user.Id, profile.Id);
        Assert.Equal("Nombre actualizado", profile.DisplayName);
        Assert.Equal("updated@example.com", profile.Email);
        Assert.Equal(new[] { "Admin" }, profile.Roles);
        Assert.Equal(createdAt, user.CreatedAt);
        Assert.Equal(1, factory.UnitOfWork.SaveCount);

        using var repeated = await SendMeAsync(client, refreshedToken);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(1, factory.UnitOfWork.SaveCount);
        Assert.Single(factory.Users.Records);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-email")]
    [InlineData("too-long")]
    public async Task Me_InvalidOrMissingEmail_IsStoredAsNull(string? email)
    {
        using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient();
        var claims = email is null ? Array.Empty<Claim>() : new[]
        {
            new Claim("email", email == "too-long" ? new string('a', 310) + "@example.com" : email)
        };
        using var response = await SendMeAsync(client, factory.CreateToken(additionalClaims: claims));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = Assert.IsType<CurrentUserResponse>(await response.Content.ReadFromJsonAsync<CurrentUserResponse>());
        Assert.Null(profile.Email);
    }

    [Fact]
    public async Task Me_NameFallbackAndMaximumLength_FollowProfileRules()
    {
        using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient();
        var subject = Guid.NewGuid().ToString();
        using var fallback = await SendMeAsync(client, factory.CreateToken(subject));
        var fallbackProfile = Assert.IsType<CurrentUserResponse>(
            await fallback.Content.ReadFromJsonAsync<CurrentUserResponse>());
        Assert.Equal($"Usuario {fallbackProfile.Id.ToString("N")[..8]}", fallbackProfile.DisplayName);

        using var preferred = await SendMeAsync(client, factory.CreateToken(subject, additionalClaims:
            [new Claim("name", " "), new Claim("preferred_username", "  nombre.preferido  ")]));
        var preferredProfile = Assert.IsType<CurrentUserResponse>(
            await preferred.Content.ReadFromJsonAsync<CurrentUserResponse>());
        Assert.Equal("nombre.preferido", preferredProfile.DisplayName);

        using var longName = await SendMeAsync(client, factory.CreateToken(subject, additionalClaims:
            [new Claim("name", new string('n', 201))]));
        var longProfile = Assert.IsType<CurrentUserResponse>(
            await longName.Content.ReadFromJsonAsync<CurrentUserResponse>());
        Assert.Equal(new string('n', 200), longProfile.DisplayName);
    }

    [Fact]
    public async Task Cors_PreflightFromFrontend_DoesNotRequireToken()
    {
        using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/me");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal(0, factory.Introspection.RequestCount);
        Assert.Empty(factory.Users.Records);
    }

    private static async Task<HttpResponseMessage> SendMeAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }
}
