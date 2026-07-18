using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Umbral.Partidas.Api.Middleware;
using Umbral.Partidas.Api.Utils;
using Umbral.Partidas.Application;
using Umbral.Partidas.Infrastructure;
using Umbral.Partidas.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPartidasApplication();
builder.Services.AddPartidasInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// fix 4: proyección del estado de runtime (Operaciones de Sesión → Partidas) vía RabbitMQ.
var rabbitConsumerOptions = builder.Configuration
    .GetSection(Umbral.Partidas.Api.Workers.RabbitMqConsumerOptions.SectionName)
    .Get<Umbral.Partidas.Api.Workers.RabbitMqConsumerOptions>()
    ?? new Umbral.Partidas.Api.Workers.RabbitMqConsumerOptions();
builder.Services.AddSingleton(rabbitConsumerOptions);
builder.Services.AddHostedService<Umbral.Partidas.Api.Workers.OperacionesSesionEventsConsumer>();

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
                }
            };
        });
}
else
{
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("GestionarPartidas", p => p.RequireRole("GestionarPartidas"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

// RNF-10: el compose activa EF_MIGRATE_ON_STARTUP=true para aplicar el esquema
// contra una base fresca. Default off: dotnet run local y tests quedan idénticos.
if (Environment.GetEnvironmentVariable("EF_MIGRATE_ON_STARTUP") == "true")
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<PartidasDbContext>().Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
