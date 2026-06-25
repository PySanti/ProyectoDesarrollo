// Infrastructure/Persistence/JuegoTriviaRepository.cs
using Microsoft.EntityFrameworkCore;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class JuegoTriviaRepository : IJuegoTriviaRepository
{
    private readonly PartidasDbContext _dbContext;

    public JuegoTriviaRepository(PartidasDbContext dbContext) => _dbContext = dbContext;

    public void Add(JuegoTrivia juego) => _dbContext.JuegosTrivia.Add(juego);

    public async Task<IReadOnlyList<JuegoTrivia>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken)
        => await _dbContext.JuegosTrivia
            .Include(j => j.Preguntas).ThenInclude(p => p.Opciones)
            .Where(j => j.PartidaId == partidaId)
            .ToListAsync(cancellationToken);
}
