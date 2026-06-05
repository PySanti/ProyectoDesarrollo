using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MediatR;
using System.Security.Claims;
using System.Text.Json;
using Umbral.IdentityService.Api.Authentication;
using Umbral.IdentityService.Application;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Users.CreateUser;
using Umbral.IdentityService.Application.Users.DeactivateUser;
using Umbral.IdentityService.Application.Users.GetUserById;
using Umbral.IdentityService.Application.Users.GetUsers;
using Umbral.IdentityService.Application.Users.UpdateUserGeneralData;
using Umbral.IdentityService.Infrastructure;
using Umbral.IdentityService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

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
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

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
    }
}

app.UseCors("FrontendDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/identity/users", async (
        CreateUserWithInitialRoleCommand command,
        IValidator<CreateUserWithInitialRoleCommand> validator,
        ISender sender,
        CancellationToken cancellationToken) =>
    {
        ValidationResult validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var response = await sender.Send(command, cancellationToken);
            return Results.Created($"/api/identity/users/{response.UserId}", response);
        }
        catch (DuplicateEmailException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (KeycloakIntegrationException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status502BadGateway);
        }
        catch (PersistenceException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("CreateIdentityUser")
    .RequireAuthorization("AdminOnly");

app.MapGet("/api/identity/users", async (
        ISender sender,
        CancellationToken cancellationToken) =>
    {
        var response = await sender.Send(new GetUsersQuery(), cancellationToken);
        return Results.Ok(response);
    })
    .WithName("GetIdentityUsers")
    .RequireAuthorization("AdminOnly");

app.MapGet("/api/identity/users/{userId:guid}", async (
        Guid userId,
        ISender sender,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var response = await sender.Send(new GetUserByIdQuery(userId), cancellationToken);
            return Results.Ok(response);
        }
        catch (UserNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
    })
    .WithName("GetIdentityUserById")
    .RequireAuthorization("AdminOnly");

app.MapMethods("/api/identity/users/{userId:guid}", new[] { "PATCH" }, async (
        Guid userId,
        UpdateUserGeneralDataRequest request,
        IValidator<UpdateUserGeneralDataCommand> validator,
        ISender sender,
        CancellationToken cancellationToken) =>
    {
        var command = new UpdateUserGeneralDataCommand(userId, request.Name, request.Email);
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
        catch (UserNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (DuplicateEmailException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (PersistenceException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("UpdateIdentityUserGeneralData")
    .RequireAuthorization("AdminOnly");

app.MapMethods("/api/identity/users/{userId:guid}/deactivation", new[] { "PATCH" }, async (
        Guid userId,
        IValidator<DeactivateUserCommand> validator,
        ISender sender,
        CancellationToken cancellationToken) =>
    {
        var command = new DeactivateUserCommand(userId);
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
        catch (UserNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (PersistenceException ex)
        {
            return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .WithName("DeactivateIdentityUser")
    .RequireAuthorization("AdminOnly");

app.Run();

public sealed record UpdateUserGeneralDataRequest(string Name, string Email);

public partial class Program
{
}
