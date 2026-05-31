using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Infrastructure.Data;

public sealed class StubTriviaInscripcionRepository : ITriviaInscripcionRepository
{
    public Task AddAsync(TriviaInscripcion inscripcion, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<int> CountByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public Task<bool> ExistsByPartidaYUsuarioAsync(PartidaId partidaId, string usuarioId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<TriviaInscripcion>> ListByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TriviaInscripcion>>(new List<TriviaInscripcion>());
    }
}
