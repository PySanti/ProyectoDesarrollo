using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBdtInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BdtDatabase")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<BdtDbContext>(opt => opt.UseNpgsql(connectionString));
        }
        else
        {
            services.AddDbContext<BdtDbContext>(opt => opt.UseInMemoryDatabase("bdt-game-service"));
        }

        services.AddScoped<IPartidaBdtReadRepository, PartidaBdtReadRepository>();

        return services;
    }
}
