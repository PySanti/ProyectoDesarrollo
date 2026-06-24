using Microsoft.EntityFrameworkCore;

namespace Umbral.Partidas.Infrastructure.Persistence;

// No entities yet — the Partida/Juego model and its configuration arrive in SP-2.
public sealed class PartidasDbContext : DbContext
{
    public PartidasDbContext(DbContextOptions<PartidasDbContext> options) : base(options)
    {
    }
}
