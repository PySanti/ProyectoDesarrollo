using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Umbral.IdentityService.Application.Interfaces;



using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Infrastructure.Persistence;
using Umbral.IdentityService.Infrastructure.Services.Events;
using Umbral.IdentityService.Infrastructure.Services.Identity;
using Umbral.IdentityService.Infrastructure.Services.Notifications;
using Umbral.IdentityService.Infrastructure.Services.Security;

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
        services.AddScoped<IEquipoRepository, EquipoRepository>();
        services.AddScoped<IInvitacionEquipoRepository, InvitacionEquipoRepository>();
        services.AddScoped<IEquipoEventsPublisher, NoOpEquipoEventsPublisher>();

        services.AddSingleton<ITemporaryPasswordGenerator, CryptoTemporaryPasswordGenerator>();

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.PostConfigure<SmtpOptions>(options =>
        {
            options.Host = FirstNonEmpty(options.Host, Environment.GetEnvironmentVariable("SMTP_HOST"));
            options.Username = FirstNonEmpty(options.Username, Environment.GetEnvironmentVariable("SMTP_USERNAME"));
            options.Password = FirstNonEmpty(options.Password, Environment.GetEnvironmentVariable("SMTP_PASSWORD"));
            options.FromAddress = FirstNonEmpty(options.FromAddress, Environment.GetEnvironmentVariable("SMTP_FROM_ADDRESS"));
            options.FromName = FirstNonEmpty(options.FromName, Environment.GetEnvironmentVariable("SMTP_FROM_NAME"));

            var portEnv = Environment.GetEnvironmentVariable("SMTP_PORT");
            if (!string.IsNullOrWhiteSpace(portEnv) && int.TryParse(portEnv, out var port))
            {
                options.Port = port;
            }

            var startTlsEnv = Environment.GetEnvironmentVariable("SMTP_USE_STARTTLS");
            if (!string.IsNullOrWhiteSpace(startTlsEnv) && bool.TryParse(startTlsEnv, out var useStartTls))
            {
                options.UseStartTls = useStartTls;
            }
        });
        services.AddScoped<IUserWelcomeEmailSender, SmtpUserWelcomeEmailSender>();

        return services;
    }

    private static string FirstNonEmpty(string current, string? fallback)
        => string.IsNullOrWhiteSpace(current) ? fallback ?? string.Empty : current;
}
