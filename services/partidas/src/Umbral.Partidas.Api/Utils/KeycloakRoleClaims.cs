using System.Security.Claims;
using System.Text.Json;

namespace Umbral.Partidas.Api.Utils;

internal static class KeycloakRoleClaims
{
    private static readonly Dictionary<string, string> KnownRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["administrador"] = "Administrador",
        ["operador"] = "Operador",
        ["participante"] = "Participante"
    };

    public static void AddRolesFromKeycloakClaims(ClaimsIdentity identity)
    {
        foreach (var role in ReadRealmRoles(identity).Concat(ReadClientRoles(identity)).Select(NormalizeRole).Distinct(StringComparer.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(role) && !identity.HasClaim(identity.RoleClaimType, role))
            {
                identity.AddClaim(new Claim(identity.RoleClaimType, role));
            }
        }
    }

    private static string NormalizeRole(string role)
    {
        return KnownRoles.TryGetValue(role.Trim(), out var normalized) ? normalized : role.Trim();
    }

    private static IEnumerable<string> ReadRealmRoles(ClaimsIdentity identity)
    {
        var realmAccessClaim = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccessClaim))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(realmAccessClaim);
            return document.RootElement.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array
                ? rolesElement.EnumerateArray().Select(role => role.GetString()).OfType<string>().ToArray()
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IEnumerable<string> ReadClientRoles(ClaimsIdentity identity)
    {
        var resourceAccessClaim = identity.FindFirst("resource_access")?.Value;
        if (string.IsNullOrWhiteSpace(resourceAccessClaim))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(resourceAccessClaim);
            return document.RootElement.EnumerateObject()
                .SelectMany(client => client.Value.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array
                    ? rolesElement.EnumerateArray().Select(role => role.GetString()).OfType<string>()
                    : [])
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
