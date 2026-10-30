using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Users;
using TaskFlow.Infrastructure.Persistence.Repositories;

namespace TaskFlow.Api.Authentication;

public static class AuthenticationExtensions
{
    private static readonly object IntrospectionUnavailable = new();

    public static IServiceCollection AddTaskFlowAuthentication(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection("Keycloak");
        var settings = section.Get<KeycloakOptions>() ?? new KeycloakOptions();

        services.AddOptions<KeycloakOptions>().Bind(section)
            .Validate(options => Uri.TryCreate(options.Authority, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps ||
                 (environment.IsDevelopment() && uri.Scheme == Uri.UriSchemeHttp)),
                "Keycloak:Authority debe ser una URL HTTPS; HTTP solo se admite en Development.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience),
                "Falta configurar Keycloak:Audience.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Introspection.ClientId),
                "Falta configurar Keycloak:Introspection:ClientId.")
            .Validate(options => options.Introspection.TimeoutSeconds is >= 1 and <= 30,
                "El tiempo de espera de introspección debe estar entre 1 y 30 segundos.")
            .ValidateOnStart();

        services.AddHttpClient<KeycloakIntrospectionClient>((provider, client) =>
        {
            client.Timeout = TimeSpan.FromSeconds(provider.GetRequiredService<IOptions<KeycloakOptions>>()
                .Value.Introspection.TimeoutSeconds);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = settings.Authority;
            options.Audience = settings.Audience;
            options.RequireHttpsMetadata = !environment.IsDevelopment();
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.SaveToken = false;
            options.BackchannelTimeout = TimeSpan.FromSeconds(5);
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = settings.Authority.TrimEnd('/'),
                ValidateAudience = true,
                ValidAudience = settings.Audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "name",
                RoleClaimType = ClaimTypes.Role
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
                    {
                        context.NoResult();
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var subject = context.Principal?.FindFirst("sub")?.Value;
                    if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255)
                    {
                        context.Fail("El token no contiene una identidad válida.");
                        return;
                    }

                    var token = context.SecurityToken switch
                    {
                        JsonWebToken jwt => jwt.EncodedToken,
                        JwtSecurityToken jwt => jwt.RawData,
                        _ => null
                    };

                    if (token is null)
                    {
                        context.Fail("El token no tiene un formato admitido.");
                        return;
                    }

                    var introspection = context.HttpContext.RequestServices
                        .GetRequiredService<KeycloakIntrospectionClient>();
                    var activity = await introspection.CheckAsync(token, context.HttpContext.RequestAborted);
                    if (activity == TokenActivity.Unavailable)
                    {
                        context.HttpContext.Items[IntrospectionUnavailable] = true;
                        context.Fail("No se pudo comprobar el estado del token.");
                    }
                    else if (activity == TokenActivity.Inactive)
                    {
                        context.Fail("El token está inactivo.");
                    }
                },
                OnChallenge = async context =>
                {
                    if (context.HttpContext.Items.ContainsKey(IntrospectionUnavailable))
                    {
                        context.HandleResponse();
                        await Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "No se puede comprobar la sesión en este momento.")
                            .ExecuteAsync(context.HttpContext);
                    }
                }
            };
        });

        services.AddTransient<IClaimsTransformation, KeycloakRolesTransformation>();
        services.AddAuthorization(options =>
        {
            var authenticatedUser = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().RequireRole("Admin", "User").Build();
            options.DefaultPolicy = authenticatedUser;
            options.FallbackPolicy = authenticatedUser;
            options.AddPolicy("AuthenticatedUser", authenticatedUser);
            options.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));
        });

        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<CurrentUser>());
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<LocalUserService>();

        return services;
    }
}
