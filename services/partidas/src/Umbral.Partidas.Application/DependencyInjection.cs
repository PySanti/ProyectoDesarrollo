using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Umbral.Partidas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPartidasApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        // Primer reloj de este servicio: CrearPartidaCommandHandler sella FechaCreacion.
        // Misma linea que Operaciones de Sesion.
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
