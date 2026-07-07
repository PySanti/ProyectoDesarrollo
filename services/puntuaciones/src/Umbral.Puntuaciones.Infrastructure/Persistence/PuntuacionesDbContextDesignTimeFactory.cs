using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbral.Puntuaciones.Infrastructure.Persistence;

public sealed class PuntuacionesDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PuntuacionesDbContext>
{
    public PuntuacionesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PuntuacionesDbContext>()
            .UseNpgsql("Host=localhost;Port=55432;Database=umbral_puntuaciones;Username=umbral;Password=16102005")
            .Options;
        return new PuntuacionesDbContext(options);
    }
}
