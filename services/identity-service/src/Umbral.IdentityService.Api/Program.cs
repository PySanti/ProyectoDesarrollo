using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MediatR;
using System.Security.Claims;
using System.Text.Json;
using Umbral.IdentityService.Application;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Users.CreateUser;
using Umbral.IdentityService.Infrastructure;

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

if (!string.IsNullOrWhiteSpace(keycloakBaseUrl) &&
    !string.IsNullOrWhiteSpace(keycloakRealm) &&
    !string.IsNullOrWhiteSpace(keycloakClientId))
{
    var authority = $"{keycloakBaseUrl.TrimEnd('/')}/realms/{keycloakRealm}";
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
                        // Ignore malformed realm_access claim and continue with default claims.
                    }

                    return Task.CompletedTask;
                }
            };
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authority,
                ValidateAudience = true,
                ValidAudience = keycloakClientId,
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

app.Run();

public partial class Program
{
}
