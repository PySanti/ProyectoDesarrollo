using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Infrastructure.Persistence;

namespace Umbral.Puntuaciones.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPuntuacionesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PuntuacionesDatabase");

        services.AddDbContext<PuntuacionesDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("puntuaciones-dev");
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        return services;
    }
}
