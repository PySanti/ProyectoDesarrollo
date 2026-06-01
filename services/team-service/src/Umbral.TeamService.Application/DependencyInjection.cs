using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Umbral.TeamService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTeamApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
