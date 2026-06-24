using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbral.OperacionesSesion.Infrastructure.Persistence;

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

        return services;
    }
}
