using Microsoft.AspNetCore.Authorization;
using Umbral.Gateway.Security;

var builder = WebApplication.CreateBuilder(args);

// Routing: entirely from appsettings (ReverseProxy section).
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// AuthN: Keycloak JWT validation (values from config/env).
builder.Services.AddKeycloakJwtAuth(builder.Configuration, builder.Environment);

// AuthZ: the three fixed base roles + secure-by-default fallback.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Administrador", p => p.RequireRole("Administrador"))
    .AddPolicy("Operador", p => p.RequireRole("Operador"))
    .AddPolicy("Participante", p => p.RequireRole("Participante"))
    .AddPolicy("OperadorOAdministrador", p => p.RequireRole("Operador", "Administrador"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Gateway's own liveness check — anonymous, minimal-API (the proxy host owns no domain logic).
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }))
   .AllowAnonymous();

// Reverse proxy (WebSocket passthrough is automatic). Routes without an explicit
// AuthorizationPolicy inherit the fallback policy → fail-secure.
app.MapReverseProxy();

app.Run();

public partial class Program
{
}
