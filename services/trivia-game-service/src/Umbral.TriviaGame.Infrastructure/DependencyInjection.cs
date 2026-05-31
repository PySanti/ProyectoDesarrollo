using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Infrastructure.Data;
using Umbral.TriviaGame.Infrastructure.Data.Repositories;
using Umbral.TriviaGame.Infrastructure.Services;

namespace Umbral.TriviaGame.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<TriviaGameDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName)));

        services.AddScoped<ITriviaFormRepository, TriviaFormRepository>();
        services.AddScoped<IPartidaTriviaRepository, PartidaTriviaRepository>();
        services.AddScoped<ITriviaInscripcionRepository, TriviaInscripcionRepository>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
