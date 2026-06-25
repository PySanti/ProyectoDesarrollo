// Infrastructure/Persistence/PartidasDbContextDesignTimeFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class PartidasDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PartidasDbContext>
{
    public PartidasDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PartidasDbContext>()
            .UseNpgsql("Host=localhost;Port=55432;Database=umbral_partidas;Username=umbral;Password=16102005")
            .Options;
        return new PartidasDbContext(options);
    }
}
