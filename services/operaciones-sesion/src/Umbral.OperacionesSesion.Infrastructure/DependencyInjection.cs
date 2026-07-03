using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Umbral.OperacionesSesion.Infrastructure.Services;

namespace Umbral.OperacionesSesion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOperacionesSesionInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OperacionesSesionDatabase");

        services.AddDbContext<OperacionesSesionDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("operaciones-sesion-dev");
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        services.AddScoped<ISesionPartidaRepository, SesionPartidaRepository>();
        services.AddScoped<IOperacionesSesionUnitOfWork, OperacionesSesionUnitOfWork>();

        services.AddScoped<ISesionEventsPublisher, NoOpSesionEventsPublisher>();
        services.AddScoped<IQrDecoder, ZXingQrDecoder>();
        var partidasBaseUrl = configuration["PartidasApi:BaseUrl"] ?? "http://localhost:5010";
        services.AddHttpClient<IConfiguracionPartidaClient, PartidasConfigHttpClient>(client =>
        {
            client.BaseAddress = new Uri(partidasBaseUrl);
        });

        var identityBaseUrl = configuration["IdentityApi:BaseUrl"] ?? "http://localhost:5000";
        services.AddHttpClient<IEquipoDirectoryClient, IdentityEquipoHttpClient>(client =>
        {
            client.BaseAddress = new Uri(identityBaseUrl);
        });

        return services;
    }
}
