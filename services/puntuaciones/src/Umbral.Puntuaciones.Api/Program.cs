using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Umbral.Puntuaciones.Api.Middleware;
using Umbral.Puntuaciones.Api.Utils;
using Umbral.Puntuaciones.Application;
using Umbral.Puntuaciones.Infrastructure;
using Umbral.Puntuaciones.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPuntuacionesApplication();
builder.Services.AddPuntuacionesInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddSingleton<Umbral.Puntuaciones.Application.Interfaces.IRankingRealtimePublisher,
    Umbral.Puntuaciones.Api.Realtime.SignalRRankingRealtimePublisher>();

var rabbitOptions = builder.Configuration
    .GetSection(Umbral.Puntuaciones.Api.Workers.RabbitMqConsumerOptions.SectionName)
    .Get<Umbral.Puntuaciones.Api.Workers.RabbitMqConsumerOptions>()
    ?? new Umbral.Puntuaciones.Api.Workers.RabbitMqConsumerOptions();
builder.Services.AddSingleton(rabbitOptions);
builder.Services.AddScoped<Umbral.Puntuaciones.Api.Workers.RankingBroadcastDispatcher>();
builder.Services.AddSingleton<Umbral.Puntuaciones.Api.Workers.ProyeccionPipeline>();
builder.Services.AddHostedService<Umbral.Puntuaciones.Api.Workers.OperacionesSesionEventsConsumer>();

var rabbitHistorialOptions = builder.Configuration
    .GetSection(Umbral.Puntuaciones.Api.Workers.RabbitMqHistorialOptions.SectionName)
    .Get<Umbral.Puntuaciones.Api.Workers.RabbitMqHistorialOptions>()
    ?? new Umbral.Puntuaciones.Api.Workers.RabbitMqHistorialOptions();
builder.Services.AddSingleton(rabbitHistorialOptions);
builder.Services.AddHostedService<Umbral.Puntuaciones.Api.Workers.HistorialEventsConsumer>();

var retencionOptions = builder.Configuration
    .GetSection(Umbral.Puntuaciones.Api.Workers.RetencionOptions.SectionName)
    .Get<Umbral.Puntuaciones.Api.Workers.RetencionOptions>()
    ?? new Umbral.Puntuaciones.Api.Workers.RetencionOptions();
builder.Services.AddSingleton(retencionOptions);
builder.Services.AddHostedService<Umbral.Puntuaciones.Api.Workers.PurgaEventosProcesadosService>();

static string? ResolveSetting(IConfiguration configuration, string key, string environmentVariable)
{
    var configuredValue = configuration[key];
    if (!string.IsNullOrWhiteSpace(configuredValue))
    {
        return configuredValue;
    }

    return Environment.GetEnvironmentVariable(environmentVariable);
}

var keycloakBaseUrl = ResolveSetting(builder.Configuration, "Keycloak:BaseUrl", "KEYCLOAK_BASE_URL");
var keycloakRealm = ResolveSetting(builder.Configuration, "Keycloak:Realm", "KEYCLOAK_REALM");
var keycloakClientId = ResolveSetting(builder.Configuration, "Keycloak:ClientId", "KEYCLOAK_CLIENT_ID");
var keycloakValidAudiencesRaw = ResolveSetting(builder.Configuration, "Keycloak:ValidAudiences", "KEYCLOAK_VALID_AUDIENCES");
var keycloakValidIssuersRaw = ResolveSetting(builder.Configuration, "Keycloak:ValidIssuers", "KEYCLOAK_VALID_ISSUERS");

if (!string.IsNullOrWhiteSpace(keycloakBaseUrl) &&
    !string.IsNullOrWhiteSpace(keycloakRealm) &&
    (!string.IsNullOrWhiteSpace(keycloakClientId) || !string.IsNullOrWhiteSpace(keycloakValidAudiencesRaw)))
{
    var authority = $"{keycloakBaseUrl.TrimEnd('/')}/realms/{keycloakRealm}";

    var validIssuers = (keycloakValidIssuersRaw ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
    if (!validIssuers.Contains(authority))
    {
        validIssuers.Add(authority);
    }

    var validAudiences = (keycloakValidAudiencesRaw ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
    if (!string.IsNullOrWhiteSpace(keycloakClientId) && !validAudiences.Contains(keycloakClientId))
    {
        validAudiences.Add(keycloakClientId);
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = keycloakClientId;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = validIssuers,
                ValidateAudience = true,
                ValidAudiences = validAudiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RoleClaimType = "roles"
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                    {
                        KeycloakRoleClaims.AddRolesFromKeycloakClaims(identity);
                    }
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    // SignalR no puede mandar el header Authorization por WebSocket: el token viaja
                    // en el query string solo para la ruta del hub (patrón de Operaciones de Sesión).
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/puntuaciones/hubs/ranking"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });
}
else
{
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization(options =>
{
    // Privilegio-sin-rol: el rol base no participa. Mismo patron que Partidas y Operaciones de
    // Sesion — el privilegio es un role claim del token (ADR-0013), asi que RequireRole lo lee
    // igual que un rol base.
    options.AddPolicy("GestionarEquipos", p => p.RequireRole("GestionarEquipos"));
    options.AddPolicy("GestionarPartidas", p => p.RequireRole("GestionarPartidas"));
});

var app = builder.Build();

// RNF-10: el compose activa EF_MIGRATE_ON_STARTUP=true para aplicar el esquema
// contra una base fresca. Default off: dotnet run local y tests quedan idénticos.
if (Environment.GetEnvironmentVariable("EF_MIGRATE_ON_STARTUP") == "true")
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<PuntuacionesDbContext>().Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Umbral.Puntuaciones.Api.Realtime.RankingHub>("puntuaciones/hubs/ranking");

app.Run();

public partial class Program
{
}
