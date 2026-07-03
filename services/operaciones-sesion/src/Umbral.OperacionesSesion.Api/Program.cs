using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Umbral.OperacionesSesion.Api.Middleware;
using Umbral.OperacionesSesion.Application;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOperacionesSesionApplication();
builder.Services.AddOperacionesSesionInfrastructure(builder.Configuration);
builder.Services.Configure<Umbral.OperacionesSesion.Api.Configuration.MantenimientoOptions>(
    builder.Configuration.GetSection("Mantenimiento"));
builder.Services.AddHostedService<Umbral.OperacionesSesion.Api.Workers.MantenimientoSesionesWorker>();

builder.Services.AddSignalR();
builder.Services.AddScoped<Umbral.OperacionesSesion.Infrastructure.Services.NoOpSesionEventsPublisher>();
builder.Services.AddScoped<Umbral.OperacionesSesion.Api.Realtime.SignalRSesionEventsPublisher>();

var rabbitOptions = builder.Configuration
    .GetSection(Umbral.OperacionesSesion.Infrastructure.Services.Messaging.RabbitMqOptions.SectionName)
    .Get<Umbral.OperacionesSesion.Infrastructure.Services.Messaging.RabbitMqOptions>()
    ?? new Umbral.OperacionesSesion.Infrastructure.Services.Messaging.RabbitMqOptions();
var rabbitHabilitado = rabbitOptions.Enabled && !string.IsNullOrWhiteSpace(rabbitOptions.Host);
if (rabbitHabilitado)
{
    builder.Services.AddSingleton(rabbitOptions);
    builder.Services.AddSingleton<Umbral.OperacionesSesion.Infrastructure.Services.Messaging.IRabbitMqPublishChannel,
        Umbral.OperacionesSesion.Infrastructure.Services.Messaging.RabbitMqPublishChannel>();
    builder.Services.AddScoped<Umbral.OperacionesSesion.Infrastructure.Services.RabbitMqSesionEventsPublisher>();
}

builder.Services.AddScoped<ISesionEventsPublisher>(sp =>
{
    var publishers = new List<ISesionEventsPublisher>
    {
        sp.GetRequiredService<Umbral.OperacionesSesion.Infrastructure.Services.NoOpSesionEventsPublisher>(),
        sp.GetRequiredService<Umbral.OperacionesSesion.Api.Realtime.SignalRSesionEventsPublisher>(),
    };
    if (rabbitHabilitado)
    {
        publishers.Add(sp.GetRequiredService<Umbral.OperacionesSesion.Infrastructure.Services.RabbitMqSesionEventsPublisher>());
    }
    return new Umbral.OperacionesSesion.Infrastructure.Services.CompositeSesionEventsPublisher(
        publishers,
        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Umbral.OperacionesSesion.Infrastructure.Services.CompositeSesionEventsPublisher>>());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

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
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/operaciones-sesion/hubs/sesion"))
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

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Umbral.OperacionesSesion.Api.Realtime.SesionHub>("operaciones-sesion/hubs/sesion");

app.Run();

public partial class Program
{
}
