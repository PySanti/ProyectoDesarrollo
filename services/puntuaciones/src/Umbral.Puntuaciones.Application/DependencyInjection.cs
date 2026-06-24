using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Umbral.Puntuaciones.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPuntuacionesApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
