using Microsoft.EntityFrameworkCore;

namespace Umbral.OperacionesSesion.Infrastructure.Persistence;

// No entities yet — live-session (runtime) state arrives in SP-3.
public sealed class OperacionesSesionDbContext : DbContext
{
    public OperacionesSesionDbContext(DbContextOptions<OperacionesSesionDbContext> options) : base(options)
    {
    }
}
