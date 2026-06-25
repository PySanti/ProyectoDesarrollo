// Infrastructure/Persistence/JuegoBDTRepository.cs
using Microsoft.EntityFrameworkCore;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class JuegoBDTRepository : IJuegoBDTRepository
{
    private readonly PartidasDbContext _dbContext;

    public JuegoBDTRepository(PartidasDbContext dbContext) => _dbContext = dbContext;

    public void Add(JuegoBDT juego) => _dbContext.JuegosBDT.Add(juego);

    public async Task<IReadOnlyList<JuegoBDT>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken)
        => await _dbContext.JuegosBDT
            .Include(j => j.Etapas)
            .Where(j => j.PartidaId == partidaId)
            .ToListAsync(cancellationToken);
}
