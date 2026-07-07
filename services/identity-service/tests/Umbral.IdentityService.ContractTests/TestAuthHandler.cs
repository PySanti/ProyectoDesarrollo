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

    private static readonly Dictionary<string, string[]> ComposedPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Operador"] = ["GestionarPartidas"],
        ["Participante"] = ["GestionarEquipos", "ParticiparEnPartidas"]
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
        if (!Request.Headers.TryGetValue("X-Test-Role", out var roleValue))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Test-Role header"));
        }

        var userId = Request.Headers.TryGetValue("X-Test-UserId", out var userIdValue)
            ? userIdValue.ToString()
            : Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new(ClaimTypes.Role, roleValue.ToString())
        };
        // Simula la expansión composite de Keycloak (SP-5a): el token de un rol base
        // trae también sus permisos funcionales.
        if (ComposedPermissions.TryGetValue(roleValue.ToString(), out var permisos))
        {
            claims.AddRange(permisos.Select(p => new Claim(ClaimTypes.Role, p)));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
