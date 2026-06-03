using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Umbral.BdtGameService.Application;
using Umbral.BdtGameService.Application.Games.ListPublished;
using Umbral.BdtGameService.Infrastructure;
using Umbral.BdtGameService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBdtApplication();
builder.Services.AddBdtInfrastructure(builder.Configuration);

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

if (!string.IsNullOrWhiteSpace(keycloakBaseUrl) &&
    !string.IsNullOrWhiteSpace(keycloakRealm) &&
    (!string.IsNullOrWhiteSpace(keycloakClientId) || !string.IsNullOrWhiteSpace(keycloakValidAudiencesRaw)))
{
    var authority = $"{keycloakBaseUrl.TrimEnd('/')}/realms/{keycloakRealm}";

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

                    var realmAccessClaim = identity.FindFirst("realm_access")?.Value;
                    if (string.IsNullOrWhiteSpace(realmAccessClaim))
                    {
                        return Task.CompletedTask;
                    }

                    try
                    {
                        using var document = JsonDocument.Parse(realmAccessClaim);
                        if (document.RootElement.TryGetProperty("roles", out var rolesElement) &&
                            rolesElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                var roleName = role.GetString();
                                if (!string.IsNullOrWhiteSpace(roleName) && !identity.HasClaim(identity.RoleClaimType, roleName))
                                {
                                    identity.AddClaim(new Claim(identity.RoleClaimType, roleName));
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore malformed claim and continue.
                    }

                    return Task.CompletedTask;
                }
            };

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authority,
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
    builder.Services
        .AddAuthentication("UnavailableAuth")
        .AddScheme<AuthenticationSchemeOptions, UnavailableAuthHandler>("UnavailableAuth", _ => { });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ParticipantOnly", policy => policy.RequireRole("Participante"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/bdt/games/published", async (
        string? modalidad,
        IValidator<ListarPartidasBdtPublicadasQuery> validator,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var actorUserId))
        {
            return Results.Forbid();
        }

        var query = new ListarPartidasBdtPublicadasQuery(actorUserId, modalidad);
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var response = await sender.Send(query, cancellationToken);
            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Results.Json(new { message = "No se pudo consultar las partidas BDT publicadas." }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("ListPublishedBdtGames")
    .RequireAuthorization("ParticipantOnly");

app.Run();

internal sealed class UnavailableAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public UnavailableAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}

public partial class Program
{
}
