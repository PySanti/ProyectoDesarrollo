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

// CORS del borde: el navegador (web :5173) llama al gateway; los orígenes vienen de env.
// AllowCredentials: lo requerirá la negociación SignalR de los slices 2c/2f.
var corsOrigins = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

app.UseCors();
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
