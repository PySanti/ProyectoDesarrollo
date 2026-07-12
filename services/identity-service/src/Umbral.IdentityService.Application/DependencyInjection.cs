using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Application.Services;

namespace Umbral.IdentityService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IParticipacionProjectionUpdater, ParticipacionProjectionUpdater>();
        return services;
    }
}
