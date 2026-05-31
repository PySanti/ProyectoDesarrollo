using Microsoft.EntityFrameworkCore;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Infrastructure.Data.Repositories;

internal sealed class TriviaInscripcionRepository : ITriviaInscripcionRepository
{
    private readonly TriviaGameDbContext _dbContext;

    public TriviaInscripcionRepository(TriviaGameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TriviaInscripcion inscripcion, CancellationToken cancellationToken = default)
    {
        await _dbContext.TriviaInscripciones.AddAsync(inscripcion, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TriviaInscripciones
            .CountAsync(i => i.PartidaId == partidaId, cancellationToken);
    }

    public async Task<bool> ExistsByPartidaYUsuarioAsync(PartidaId partidaId, string usuarioId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TriviaInscripciones
            .AnyAsync(i => i.PartidaId == partidaId && i.UsuarioId == usuarioId, cancellationToken);
    }

    public async Task<IReadOnlyList<TriviaInscripcion>> ListByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TriviaInscripciones
            .Where(i => i.PartidaId == partidaId)
            .OrderBy(i => i.FechaInscripcion)
            .ToListAsync(cancellationToken);
    }
}
