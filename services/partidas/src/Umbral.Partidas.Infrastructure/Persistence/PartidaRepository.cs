// Infrastructure/Persistence/PartidaRepository.cs
using Microsoft.EntityFrameworkCore;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class PartidaRepository : IPartidaRepository
{
    private readonly PartidasDbContext _dbContext;

    public PartidaRepository(PartidasDbContext dbContext) => _dbContext = dbContext;

    public void Add(Partida partida) => _dbContext.Partidas.Add(partida);

    public void Update(Partida partida) => _dbContext.Partidas.Update(partida);

    public Task<Partida?> GetByIdAsync(PartidaId id, CancellationToken cancellationToken)
        => _dbContext.Partidas.Include(p => p.Juegos)
            .FirstOrDefaultAsync(p => p.PartidaId == id, cancellationToken);

    // El ORDER BY vive aqui y no en el handler: es trabajo de la base y sobrevive a una
    // futura paginacion. El desempate por PartidaId no es paranoia: con precision de
    // microsegundos dos partidas reales nunca empatan, pero un reloj falso en tests
    // devuelve el mismo instante y sin ThenBy el orden seria indefinido e intermitente.
    public async Task<IReadOnlyList<Partida>> ListAsync(CancellationToken cancellationToken)
        => await _dbContext.Partidas
            .Include(p => p.Juegos)
            .OrderByDescending(p => p.FechaCreacion)
            .ThenBy(p => p.PartidaId)
            .ToListAsync(cancellationToken);
}
