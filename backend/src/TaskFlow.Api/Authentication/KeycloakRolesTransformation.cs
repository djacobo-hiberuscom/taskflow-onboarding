using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace TaskFlow.Api.Authentication;

public sealed class KeycloakRolesTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var transformed = new ClaimsPrincipal(principal.Identities.Select(identity => identity.Clone()));

        foreach (var identity in transformed.Identities.Where(identity => identity.IsAuthenticated))
        {
            // Solo realm_access.roles concede roles de aplicación; no los claims planos del token.
            foreach (var claim in identity.FindAll(identity.RoleClaimType).ToArray())
            {
                identity.RemoveClaim(claim);
            }

            var realmAccess = identity.FindFirst("realm_access")?.Value;
            if (realmAccess is null)
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(realmAccess);
                if (document.RootElement.ValueKind != JsonValueKind.Object ||
                    !document.RootElement.TryGetProperty("roles", out var roles) ||
                    roles.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var role in roles.EnumerateArray()
                    .Where(role => role.ValueKind == JsonValueKind.String)
                    .Select(role => role.GetString())
                    .Where(role => role is "Admin" or "User")
                    .Distinct())
                {
                    identity.AddClaim(new Claim(identity.RoleClaimType, role!));
                }
            }
            catch (JsonException)
            {
                // Un claim mal formado no concede permisos.
            }
        }

        return Task.FromResult(transformed);
    }
}
