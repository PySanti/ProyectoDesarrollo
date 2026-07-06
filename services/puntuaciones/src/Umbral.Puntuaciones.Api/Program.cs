using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Umbral.Puntuaciones.Api.Middleware;
using Umbral.Puntuaciones.Application;
using Umbral.Puntuaciones.Infrastructure;

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
builder.Services.AddHostedService<Umbral.Puntuaciones.Api.Workers.OperacionesSesionEventsConsumer>();

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
        });
}
else
{
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Umbral.Puntuaciones.Api.Realtime.RankingHub>("puntuaciones/hubs/ranking");

app.Run();

public partial class Program
{
}
