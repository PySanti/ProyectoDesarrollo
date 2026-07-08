using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbral.Puntuaciones.IntegrationTests;

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
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sub),
            new("sub", sub)
        };
        // SP-4d: roles opcionales para endpoints con [Authorize(Roles = ...)] — el claim type "roles"
        // replica el RoleClaimType de la config JWT real del servicio.
        if (Request.Headers.TryGetValue("X-Test-Roles", out var rolesValue))
        {
            foreach (var role in rolesValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim("roles", role));
            }
        }
        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.NameIdentifier, "roles");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
