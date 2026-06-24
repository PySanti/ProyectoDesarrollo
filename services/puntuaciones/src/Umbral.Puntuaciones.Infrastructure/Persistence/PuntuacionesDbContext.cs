using Microsoft.EntityFrameworkCore;

namespace Umbral.Puntuaciones.Infrastructure.Persistence;

// No entities yet — scoring/ranking projections arrive in SP-4.
public sealed class PuntuacionesDbContext : DbContext
{
    public PuntuacionesDbContext(DbContextOptions<PuntuacionesDbContext> options) : base(options)
    {
    }
}
