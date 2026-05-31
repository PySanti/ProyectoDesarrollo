using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Umbral.IdentityService.Application.Abstractions.Identity;
using Umbral.IdentityService.Application.Abstractions.Persistence;
using Umbral.IdentityService.Infrastructure.Identity;
using Umbral.IdentityService.Infrastructure.Persistence;

namespace Umbral.IdentityService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDatabase");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.EnableDetailedErrors();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("identity-service-dev");
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));
        services.PostConfigure<KeycloakOptions>(options =>
        {
            options.BaseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? Environment.GetEnvironmentVariable("KEYCLOAK_BASE_URL") ?? string.Empty
                : options.BaseUrl;
            options.Realm = string.IsNullOrWhiteSpace(options.Realm)
                ? Environment.GetEnvironmentVariable("KEYCLOAK_REALM") ?? string.Empty
                : options.Realm;
            options.ClientId = string.IsNullOrWhiteSpace(options.ClientId)
                ? Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID") ?? string.Empty
                : options.ClientId;
            options.ClientSecret = string.IsNullOrWhiteSpace(options.ClientSecret)
                ? Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_SECRET") ?? string.Empty
                : options.ClientSecret;
        });
        services.AddHttpClient<IKeycloakIdentityPort, KeycloakIdentityAdapter>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        return services;
    }
}
