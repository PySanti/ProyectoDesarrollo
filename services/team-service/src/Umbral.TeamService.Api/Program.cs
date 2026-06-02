using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Umbral.TeamService.Application;
using Umbral.TeamService.Application.Exceptions;
using Umbral.TeamService.Application.Teams.CreateTeam;
using Umbral.TeamService.Application.Teams.JoinTeamByCode;
using Umbral.TeamService.Application.Teams.LeaveTeam;
using Umbral.TeamService.Infrastructure;
using Umbral.TeamService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTeamApplication();
builder.Services.AddTeamInfrastructure(builder.Configuration);

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
    var dbContext = scope.ServiceProvider.GetRequiredService<TeamDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/teams", async (
        CreateTeamRequest request,
        IValidator<CrearEquipoCommand> validator,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var actorUserId))
        {
            return Results.Forbid();
        }

        var command = new CrearEquipoCommand(actorUserId, request.NombreEquipo);
        ValidationResult validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var response = await sender.Send(command, cancellationToken);
            return Results.Created($"/api/teams/{response.EquipoId}", response);
        }
        catch (AlreadyBelongsToActiveTeamException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (ConcurrentTeamCreationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (AccessCodeGenerationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (UniqueMembershipConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (PersistenceException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("CreateTeam")
    .RequireAuthorization("ParticipantOnly");

app.MapPost("/api/teams/join-by-code", async (
        JoinTeamByCodeRequest request,
        IValidator<UnirseAEquipoPorCodigoCommand> validator,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var actorUserId))
        {
            return Results.Forbid();
        }

        var command = new UnirseAEquipoPorCodigoCommand(actorUserId, request.CodigoAcceso);
        ValidationResult validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var response = await sender.Send(command, cancellationToken);
            return Results.Ok(response);
        }
        catch (AlreadyBelongsToActiveTeamException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (ParticipantAlreadyInTargetTeamException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (TeamFullException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (TeamNotFoundByAccessCodeException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (UniqueMembershipConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (PersistenceException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("JoinTeamByCode")
    .RequireAuthorization("ParticipantOnly");

app.MapDelete("/api/teams/membership", async (
        IValidator<SalirDeEquipoCommand> validator,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var actorUserId))
        {
            return Results.Forbid();
        }

        var command = new SalirDeEquipoCommand(actorUserId);
        ValidationResult validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var response = await sender.Send(command, cancellationToken);
            return Results.Ok(response);
        }
        catch (NoActiveTeamForParticipantException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (LeaveTeamConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (PersistenceException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("LeaveTeamMembership")
    .RequireAuthorization("ParticipantOnly");

app.Run();

public sealed record CreateTeamRequest(string NombreEquipo);
public sealed record JoinTeamByCodeRequest(string CodigoAcceso);

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
