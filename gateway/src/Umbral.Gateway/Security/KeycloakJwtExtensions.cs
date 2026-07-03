using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Umbral.Gateway.Security;

public static class KeycloakJwtExtensions
{
    public static IServiceCollection AddKeycloakJwtAuth(
        this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var baseUrl = Resolve(configuration, "Keycloak:BaseUrl", "KEYCLOAK_BASE_URL");
        var realm = Resolve(configuration, "Keycloak:Realm", "KEYCLOAK_REALM");
        var clientId = Resolve(configuration, "Keycloak:ClientId", "KEYCLOAK_CLIENT_ID");
        var audiencesRaw = Resolve(configuration, "Keycloak:ValidAudiences", "KEYCLOAK_VALID_AUDIENCES");
        var issuersRaw = Resolve(configuration, "Keycloak:ValidIssuers", "KEYCLOAK_VALID_ISSUERS");

        // No realm configured (e.g. offline tests): still register the scheme so the fallback
        // policy 401s unauthenticated requests. No metadata fetch happens without a token to validate.
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(realm))
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            return services;
        }

        var authority = $"{baseUrl.TrimEnd('/')}/realms/{realm}";
        var validIssuers = Split(issuersRaw);
        if (!validIssuers.Contains(authority)) validIssuers.Add(authority);
        var validAudiences = Split(audiencesRaw);
        if (!string.IsNullOrWhiteSpace(clientId) && !validAudiences.Contains(clientId)) validAudiences.Add(clientId);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = clientId;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
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
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["access_token"];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/operaciones-sesion/hubs"))
                        {
                            ctx.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = ctx =>
                    {
                        if (ctx.Principal?.Identity is ClaimsIdentity identity)
                        {
                            KeycloakRoleClaims.AddRolesFromKeycloakClaims(identity);
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    private static string? Resolve(IConfiguration c, string key, string env)
        => !string.IsNullOrWhiteSpace(c[key]) ? c[key] : Environment.GetEnvironmentVariable(env);

    private static List<string> Split(string? raw)
        => (raw ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}
