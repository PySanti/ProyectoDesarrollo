using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Application.Abstractions.Qr;
using Umbral.BdtGameService.Application.Abstractions.Realtime;
using Umbral.BdtGameService.Application.Abstractions.Storage;
using Umbral.BdtGameService.Infrastructure.Persistence;
using Umbral.BdtGameService.Infrastructure.Qr;
using Umbral.BdtGameService.Infrastructure.Realtime;
using Umbral.BdtGameService.Infrastructure.Storage;

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

        services.AddScoped<IPartidaBdtRepository, PartidaBdtRepository>();
        services.AddScoped<IPartidaBdtReadRepository, PartidaBdtReadRepository>();
        services.AddScoped<ITesoroQrImageStorage, LocalTesoroQrImageStorage>();
        services.AddScoped<IQrImageDecoder, ZxingQrImageDecoder>();
        services.AddScoped<IPartidaBdtSubscriptionAuthorizer, PartidaBdtSubscriptionAuthorizer>();

        return services;
    }
}
