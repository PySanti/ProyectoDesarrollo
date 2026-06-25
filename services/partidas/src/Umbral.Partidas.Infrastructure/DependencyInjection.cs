using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Infrastructure.Persistence;

namespace Umbral.Partidas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPartidasInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PartidasDatabase");

        services.AddDbContext<PartidasDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("partidas-dev");
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        services.AddScoped<IPartidaRepository, PartidaRepository>();
        services.AddScoped<IJuegoTriviaRepository, JuegoTriviaRepository>();
        services.AddScoped<IJuegoBDTRepository, JuegoBDTRepository>();
        services.AddScoped<IPartidasUnitOfWork, PartidasUnitOfWork>();

        return services;
    }
}
