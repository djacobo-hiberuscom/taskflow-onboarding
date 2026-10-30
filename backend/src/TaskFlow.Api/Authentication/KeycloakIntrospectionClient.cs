using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TaskFlow.Api.Authentication;

public enum TokenActivity
{
    Active,
    Inactive,
    Unavailable
}

public sealed class KeycloakIntrospectionClient(
    HttpClient httpClient, IOptions<KeycloakOptions> options,
    ILogger<KeycloakIntrospectionClient> logger)
{
    public async Task<TokenActivity> CheckAsync(string token, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Introspection.ClientSecret))
        {
            logger.LogWarning("Falta configurar el secreto del cliente de introspección de Keycloak.");
            return TokenActivity.Unavailable;
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = token,
            ["token_type_hint"] = "access_token",
            ["client_id"] = settings.Introspection.ClientId,
            ["client_secret"] = settings.Introspection.ClientSecret
        });

        try
        {
            var endpoint = settings.Authority.TrimEnd('/') + "/protocol/openid-connect/token/introspect";
            using var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Keycloak no pudo completar la introspección. Estado HTTP: {StatusCode}.",
                    (int)response.StatusCode);
                return TokenActivity.Unavailable;
            }

            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("active", out var active) &&
                active.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return active.GetBoolean() ? TokenActivity.Active : TokenActivity.Inactive;
            }
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("No se pudo conectar con Keycloak para comprobar el estado del token.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Se agotó el tiempo de espera de introspección de Keycloak.");
        }
        catch (JsonException)
        {
            logger.LogWarning("Keycloak devolvió una respuesta de introspección inválida.");
        }

        return TokenActivity.Unavailable;
    }
}
