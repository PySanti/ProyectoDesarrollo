using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbral.OperacionesSesion.ContractTests;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Sub", out var subValue) ||
            string.IsNullOrWhiteSpace(subValue.ToString()))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Test-Sub header"));
        }

        var sub = subValue.ToString();

        // Roles simulados: por defecto ambos permisos funcionales (como un token que los
        // trae por composites); los tests de 403 mandan "X-Test-Roles" explícito y acotado.
        var roles = Request.Headers.TryGetValue("X-Test-Roles", out var rolesValue)
            ? rolesValue.ToString()
            : "GestionarPartidas,ParticiparEnPartidas";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sub),
            new("sub", sub)
        };
        foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
