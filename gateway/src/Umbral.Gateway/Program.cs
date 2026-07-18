using Microsoft.AspNetCore.Authorization;
using Umbral.Gateway.Security;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));


builder.Services.AddKeycloakJwtAuth(builder.Configuration, builder.Environment);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Administrador", p => p.RequireRole("Administrador"))
    .AddPolicy("Operador", p => p.RequireRole("Operador"))
    .AddPolicy("Participante", p => p.RequireRole("Participante"))
    .AddPolicy("GestionarPartidas", p => p.RequireRole("GestionarPartidas"))
    .AddPolicy("GestionarEquipos", p => p.RequireRole("GestionarEquipos"))
    .AddPolicy("DirectorioUsuarios", p => p.RequireRole("Administrador", "GestionarEquipos"))
    .AddPolicy("ListadoEquipos", p => p.RequireRole("GestionarPartidas", "GestionarEquipos"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());


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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }))
   .AllowAnonymous();

app.MapReverseProxy();

app.Run();

public partial class Program
{
}
