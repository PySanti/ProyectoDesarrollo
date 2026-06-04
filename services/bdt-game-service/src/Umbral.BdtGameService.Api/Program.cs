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
using Umbral.BdtGameService.Application.Abstractions.Realtime;
using Umbral.BdtGameService.Application.Games.Create;
using Umbral.BdtGameService.Application.Games.ActiveStage;
using Umbral.BdtGameService.Application.Games.JoinIndividual;
using Umbral.BdtGameService.Application.Games.ListPublished;
using Umbral.BdtGameService.Application.Games.Start;
using Umbral.BdtGameService.Application.Games.UploadTreasure;
using Umbral.BdtGameService.Api.Realtime;
using Umbral.BdtGameService.Infrastructure;
using Umbral.BdtGameService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBdtApplication();
builder.Services.AddBdtInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddScoped<IPartidaBdtRealtimeNotifier, SignalRPartidaBdtRealtimeNotifier>();

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
    options.AddPolicy("OperatorOnly", policy => policy.RequireRole("Operador"));
    options.AddPolicy("BdtHubAuthenticated", policy => policy.RequireRole("Operador", "Participante"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<BdtPartidaHub>("/hubs/bdt").RequireAuthorization("BdtHubAuthenticated");

app.MapPost("/api/bdt/games", async (
        CrearPartidaBdtCommand command,
        IValidator<CrearPartidaBdtCommand> validator,
        ISender sender,
        CancellationToken cancellationToken) =>
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var response = await sender.Send(command, cancellationToken);
            return Results.Created($"/api/bdt/games/{response.PartidaId}", response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Results.Json(new { message = "No se pudo crear la partida BDT." }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("CreateBdtGame")
    .RequireAuthorization("OperatorOnly");

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

app.MapGet("/api/bdt/operator/games/published", async (
        IValidator<ListarPartidasBdtPublicadasOperadorQuery> validator,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var actorUserId))
        {
            return Results.Forbid();
        }

        var query = new ListarPartidasBdtPublicadasOperadorQuery(actorUserId);
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
    .WithName("ListOperatorPublishedBdtGames")
    .RequireAuthorization("OperatorOnly");

app.MapPost("/api/bdt/games/{partidaId}/individual-inscriptions", async (
        string partidaId,
        IValidator<UnirseABdtIndividualCommand> validator,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(partidaId, out var parsedPartidaId))
        {
            return Results.BadRequest(new { message = "PartidaId invalido." });
        }

        var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var participanteUserId))
        {
            return Results.Forbid();
        }

        var command = new UnirseABdtIndividualCommand(parsedPartidaId, participanteUserId);
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var response = await sender.Send(command, cancellationToken);
            return Results.Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Results.Json(new { message = "No se pudo registrar la inscripcion individual BDT." }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("JoinIndividualBdtGame")
    .RequireAuthorization("ParticipantOnly");

app.MapPost("/api/bdt/games/{partidaId}/start", async (
        string partidaId,
        IValidator<IniciarPartidaBdtCommand> validator,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(partidaId, out var parsedPartidaId))
        {
            return Results.BadRequest(new { message = "PartidaId invalido." });
        }

        var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var operadorUserId))
        {
            return Results.Forbid();
        }

        var command = new IniciarPartidaBdtCommand(parsedPartidaId, operadorUserId);
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var response = await sender.Send(command, cancellationToken);
            return Results.Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Results.Json(new { message = "No se pudo iniciar la partida BDT." }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("StartBdtGame")
    .RequireAuthorization("OperatorOnly");

app.MapGet("/api/bdt/games/{partidaId}/active-stage", async (
        string partidaId,
        IValidator<ObtenerEtapaActivaBdtQuery> validator,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(partidaId, out var parsedPartidaId))
        {
            return Results.BadRequest(new { message = "PartidaId invalido." });
        }

        var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var participanteUserId))
        {
            return Results.Forbid();
        }

        var query = new ObtenerEtapaActivaBdtQuery(parsedPartidaId, participanteUserId);
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
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Results.Json(new { message = "No se pudo consultar la etapa activa BDT." }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("GetBdtActiveStage")
    .RequireAuthorization("ParticipantOnly");

app.MapPost("/api/bdt/games/{partidaId}/stages/{etapaId}/treasures", async (
        string partidaId,
        string etapaId,
        IFormFile? image,
        IValidator<SubirTesoroQrCommand> validator,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        if (!Guid.TryParse(partidaId, out var parsedPartidaId))
        {
            return Results.BadRequest(new { message = "PartidaId invalido." });
        }

        if (!Guid.TryParse(etapaId, out var parsedEtapaId))
        {
            return Results.BadRequest(new { message = "EtapaId invalido." });
        }

        var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var participanteUserId))
        {
            return Results.Forbid();
        }

        if (image is null || image.Length == 0)
        {
            return Results.BadRequest(new { message = "La imagen es requerida." });
        }

        if (!image.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) &&
            !image.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Json(new { message = "Solo se aceptan imagenes JPEG o PNG." }, statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        if (image.Length > SubirTesoroQrCommandValidator.MaxImageSizeBytes)
        {
            return Results.Json(new { message = "La imagen no puede superar 5 MB." }, statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        byte[] imageContent;
        await using (var stream = image.OpenReadStream())
        using (var memoryStream = new MemoryStream())
        {
            await stream.CopyToAsync(memoryStream, cancellationToken);
            imageContent = memoryStream.ToArray();
        }

        var command = new SubirTesoroQrCommand(
            parsedPartidaId,
            parsedEtapaId,
            participanteUserId,
            image.FileName,
            image.ContentType,
            image.Length,
            imageContent);

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var response = await sender.Send(command, cancellationToken);
            return Results.Created(
                $"/api/bdt/games/{response.PartidaId}/stages/{response.EtapaId}/treasures/{response.TesoroId}",
                response);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Results.Json(new { message = "No se pudo registrar el tesoro QR." }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("UploadBdtTreasureQr")
    .DisableAntiforgery()
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
