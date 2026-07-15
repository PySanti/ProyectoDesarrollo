using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Umbral.IdentityService.Api.Utils;
using Umbral.IdentityService.Api.Workers;
using Umbral.IdentityService.Application;
using Umbral.IdentityService.Infrastructure;
using Umbral.IdentityService.Infrastructure.Persistence;
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

    // EnsureCreated no evoluciona esquemas existentes: si la BD ya tiene alguna tabla,
    // no crea las que falten. Estos parches cubren la deriva y deben correr ANTES del
    // backfill, que consulta historial_nombre_equipo.
    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS usuarios (
                usuarioid uuid PRIMARY KEY,
                keycloakid varchar(128) NOT NULL,
                nombre varchar(120) NOT NULL,
                correo varchar(320) NOT NULL,
                rol integer NOT NULL,
                estado integer NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_usuarios_correo ON usuarios (correo);
            """);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS permisos_rol (
                rol integer NOT NULL,
                permiso integer NOT NULL,
                PRIMARY KEY (rol, permiso)
            );

            INSERT INTO permisos_rol (rol, permiso)
            SELECT v.rol, v.permiso
            FROM (VALUES (2, 1), (3, 2), (3, 3)) AS v(rol, permiso)
            WHERE NOT EXISTS (SELECT 1 FROM permisos_rol);
            """);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS equipos (
                equipoid uuid PRIMARY KEY,
                nombreequipo varchar(120) NOT NULL,
                estado integer NOT NULL
            );

            CREATE TABLE IF NOT EXISTS equipos_participantes (
                participanteequipoid uuid PRIMARY KEY,
                equipoid uuid NOT NULL REFERENCES equipos (equipoid) ON DELETE CASCADE,
                usuarioid uuid NOT NULL,
                fechaunionutc timestamp with time zone NOT NULL,
                eslider boolean NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_equipos_participantes_usuarioid ON equipos_participantes (usuarioid);

            CREATE TABLE IF NOT EXISTS invitaciones_equipo (
                invitacionequipoid uuid PRIMARY KEY,
                equipoid uuid NOT NULL REFERENCES equipos (equipoid) ON DELETE CASCADE,
                invitadouserid uuid NOT NULL,
                invitadoporuserid uuid NOT NULL,
                estado integer NOT NULL,
                fechacreacionutc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_invitaciones_equipo_invitadouserid ON invitaciones_equipo (invitadouserid);

            CREATE TABLE IF NOT EXISTS historial_nombre_equipo (
                id uuid PRIMARY KEY,
                usuarioid uuid NOT NULL,
                equipoid uuid NOT NULL,
                nombreequipo varchar(120) NOT NULL,
                fecharegistroutc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_historial_nombre_equipo_usuarioid ON historial_nombre_equipo (usuarioid);

            CREATE TABLE IF NOT EXISTS participaciones_activas_equipo (
                equipoid uuid NOT NULL,
                partidaid uuid NOT NULL,
                fecharegistroutc timestamp with time zone NOT NULL,
                PRIMARY KEY (equipoid, partidaid)
            );
            """);
    }

    await HistorialBackfill.EjecutarAsync(dbContext, scope.ServiceProvider.GetRequiredService<TimeProvider>(), CancellationToken.None);
}

app.UseMiddleware<Umbral.IdentityService.Api.Middleware.ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
