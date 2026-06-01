using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbral.TeamService.Application.Abstractions.Events;
using Umbral.TeamService.Application.Abstractions.Persistence;
using Umbral.TeamService.Application.Abstractions.Services;
using Umbral.TeamService.Infrastructure.Events;
using Umbral.TeamService.Infrastructure.Persistence;
using Umbral.TeamService.Infrastructure.Services;

namespace Umbral.TeamService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTeamInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TeamDatabase")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<TeamDbContext>(opt => opt.UseNpgsql(connectionString));
        }
        else
        {
            services.AddDbContext<TeamDbContext>(opt => opt.UseInMemoryDatabase("team-service"));
        }

        services.AddScoped<IEquipoRepository, EquipoRepository>();
        services.AddScoped<ICodigoAccesoGenerator, CodigoAccesoGenerator>();
        services.AddScoped<ITeamEventsPublisher, NoOpTeamEventsPublisher>();

        return services;
    }
}
