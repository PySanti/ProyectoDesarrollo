using Microsoft.EntityFrameworkCore;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Infrastructure.Data.Repositories;

internal sealed class PartidaTriviaRepository : IPartidaTriviaRepository
{
    private readonly TriviaGameDbContext _dbContext;

    public PartidaTriviaRepository(TriviaGameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PartidaTrivia partida, CancellationToken cancellationToken = default)
    {
        await _dbContext.PartidasTrivia.AddAsync(partida, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PartidaTrivia?> GetByIdAsync(PartidaId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PartidasTrivia
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PartidaTrivia?> GetByIdWithRespuestasAsync(PartidaId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PartidasTrivia
            .Include(p => p.Respuestas)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(PartidaTrivia partida, CancellationToken cancellationToken = default)
    {
        _dbContext.PartidasTrivia.Update(partida);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PartidaTrivia>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PartidasTrivia
            .Where(p => p.Estado == PartidaEstado.Lobby)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PartidaTrivia>> GetSupervisableForOperatorAsync(CancellationToken cancellationToken = default)
    {
        var partidas = await _dbContext.PartidasTrivia
            .AsNoTracking()
            .Where(p => p.Estado == PartidaEstado.Lobby || p.Estado == PartidaEstado.Iniciada)
            .ToListAsync(cancellationToken);

        partidas.Sort((left, right) =>
        {
            var byStartTime = left.TiempoInicio.Value.CompareTo(right.TiempoInicio.Value);
            return byStartTime != 0
                ? byStartTime
                : string.Compare(left.Nombre.Value, right.Nombre.Value, StringComparison.Ordinal);
        });

        return partidas;
    }

    public async Task<int> CountInscripcionesAsync(PartidaId partidaId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TriviaInscripciones
            .CountAsync(i => i.PartidaId == partidaId, cancellationToken);
    }
}
