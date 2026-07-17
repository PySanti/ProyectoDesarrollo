using System.Security.Claims;
using Umbral.Puntuaciones.Api.Utils;

namespace Umbral.Puntuaciones.UnitTests.Api.Utils;

public class KeycloakRoleClaimsTests
{
    [Fact]
    public void Agrega_rol_normalizado_desde_realm_access_sin_duplicarlo()
    {
        var identity = new ClaimsIdentity(
            [new Claim("realm_access", "{\"roles\":[\"Operador\"]}")],
            authenticationType: "Test",
            nameType: ClaimTypes.Name,
            roleType: "roles");

        KeycloakRoleClaims.AddRolesFromKeycloakClaims(identity);
        KeycloakRoleClaims.AddRolesFromKeycloakClaims(identity);

        Assert.True(new ClaimsPrincipal(identity).IsInRole("Operador"));
        Assert.Single(identity.FindAll("roles"), claim => claim.Value == "Operador");
    }
}
