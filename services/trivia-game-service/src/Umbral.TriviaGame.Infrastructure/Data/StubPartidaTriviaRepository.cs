using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Infrastructure.Data;

public sealed class StubPartidaTriviaRepository : IPartidaTriviaRepository
{
    public Task AddAsync(PartidaTrivia partida, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<PartidaTrivia?> GetByIdAsync(PartidaId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<PartidaTrivia?>(null);
    }

    public Task UpdateAsync(PartidaTrivia partida, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PartidaTrivia>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PartidaTrivia>>(new List<PartidaTrivia>());
    }

    public Task<int> CountInscripcionesAsync(PartidaId partidaId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public Task<PartidaTrivia?> GetByIdWithRespuestasAsync(PartidaId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<PartidaTrivia?>(null);
    }
}
