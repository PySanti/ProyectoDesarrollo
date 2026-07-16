using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Umbral.IdentityService.Api.Utils;
using Umbral.IdentityService.Api.Workers;
using Umbral.IdentityService.Application;
using Umbral.IdentityService.Infrastructure;
using Umbral.IdentityService.Infrastructure.Persistence;
using Umbral.IdentityService.Infrastructure.Services.Identity;
using Umbral.IdentityService.Infrastructure.Services.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddControllers();

// Primer consumidor RabbitMQ de Identity (Task D3): proyecta participaciones activas de
// equipo desde eventos de Operaciones de Sesión. El BackgroundService no arranca si está
// deshabilitado o sin host (mismo patrón que el consumidor de Puntuaciones).
var rabbitConsumerOptions = builder.Configuration
    .GetSection(RabbitMqConsumerOptions.SectionName)
    .Get<RabbitMqConsumerOptions>()
    ?? new RabbitMqConsumerOptions();
builder.Services.AddSingleton(rabbitConsumerOptions);
builder.Services.AddHostedService<OperacionesInscripcionesConsumer>();

// Segundo consumidor RabbitMQ de Identity (7f, RNF-23): se autoconsume CredencialTemporalEmitida
// (exchange umbral.identity) para disparar el correo SMTP de bienvenida de forma asíncrona.
var rabbitCredencialesConsumerOptions = builder.Configuration
    .GetSection(RabbitMqCredencialesConsumerOptions.SectionName)
    .Get<RabbitMqCredencialesConsumerOptions>()
    ?? new RabbitMqCredencialesConsumerOptions();
builder.Services.AddSingleton(rabbitCredencialesConsumerOptions);
// publicacion del consumer del servicio
builder.Services.AddHostedService<CredencialesTemporalesConsumer>();

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
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is not ClaimsIdentity identity)
                    {
                        return Task.CompletedTask;
                    }

                    KeycloakRoleClaims.AddRolesFromKeycloakClaims(identity);

                    return Task.CompletedTask;
                }
            };
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("GestionarEquipos", policy => policy.RequireRole("GestionarEquipos"));
    options.AddPolicy("OperadorOAdministrador", policy => policy.RequireRole("Operador", "Administrador"));
    // El flujo propio del participante (su equipo, invitaciones) lo concede el rol: el panel de
    // gobernanza deja al Participante sin GestionarEquipos, que ahora solo abre los paneles de
    // administrar equipos ajenos.
    options.AddPolicy("Participante", policy => policy.RequireRole("Participante"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    if (dbContext.Database.IsRelational())
    {
        await EsquemaLegadoPatch.AplicarAsync(dbContext, CancellationToken.None);
    }

    await HistorialBackfill.EjecutarAsync(dbContext, scope.ServiceProvider.GetRequiredService<TimeProvider>(), CancellationToken.None);

    // La DB manda sobre la gobernanza; Keycloak es su espejo. Corre después del seed de permisos_rol.
    await scope.ServiceProvider
        .GetRequiredService<PermisosRolKeycloakReconciler>()
        .ReconcileAsync(CancellationToken.None);
}

app.UseMiddleware<Umbral.IdentityService.Api.Middleware.ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
