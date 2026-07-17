using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbral.IdentityService.ContractTests;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    // Espeja lo que el token lleva de verdad: el composite fijo del realm
    // (Participante -> ParticiparEnPartidas) más los privilegios gobernables que el reconciliador
    // empuja desde los defaults de permisos_rol (Administrador -> GestionarEquipos,
    // Operador -> GestionarPartidas). El Participante ya NO trae GestionarEquipos: su default es
    // ninguno, y su equipo propio depende del rol, no del privilegio.
    private static readonly Dictionary<string, string[]> ComposedPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Administrador"] = ["GestionarEquipos"],
        ["Operador"] = ["GestionarPartidas"],
        ["Participante"] = ["ParticiparEnPartidas"]
    };

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers.TryGetValue("X-Test-UserId", out var userIdValue)
            ? userIdValue.ToString()
            : Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId)
        };

        if (Request.Headers.TryGetValue("X-Test-Roles", out var rawRolesValue))
        {
            // Lista literal de roles, SIN expansión composite: para probar combinaciones que el
            // reconciliador nunca produciría (p.ej. privilegio sin el rol base que lo trae por
            // default), que es justo lo que el AND de una policy compuesta necesita cubrir.
            var rawRoles = rawRolesValue.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            claims.AddRange(rawRoles.Select(r => new Claim(ClaimTypes.Role, r)));
        }
        else if (Request.Headers.TryGetValue("X-Test-Role", out var roleValue))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleValue.ToString()));
            // Simula la expansión composite de Keycloak (SP-5a): el token de un rol base
            // trae también sus permisos funcionales.
            if (ComposedPermissions.TryGetValue(roleValue.ToString(), out var permisos))
            {
                claims.AddRange(permisos.Select(p => new Claim(ClaimTypes.Role, p)));
            }
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Test-Role or X-Test-Roles header"));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
