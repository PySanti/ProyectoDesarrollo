using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Umbral.BdtGameService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBdtApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
