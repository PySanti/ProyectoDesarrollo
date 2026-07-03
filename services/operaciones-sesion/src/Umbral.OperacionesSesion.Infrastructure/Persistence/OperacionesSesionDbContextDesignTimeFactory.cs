using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbral.OperacionesSesion.Infrastructure.Persistence;

public sealed class OperacionesSesionDbContextDesignTimeFactory : IDesignTimeDbContextFactory<OperacionesSesionDbContext>
{
    public OperacionesSesionDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseNpgsql("Host=localhost;Port=55432;Database=umbral_operaciones_sesion;Username=umbral;Password=16102005")
            .Options;
        return new OperacionesSesionDbContext(options);
    }
}
