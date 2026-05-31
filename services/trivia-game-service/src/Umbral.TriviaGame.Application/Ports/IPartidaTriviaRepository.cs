using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Ports;

public interface IPartidaTriviaRepository
{
    Task AddAsync(PartidaTrivia partida, CancellationToken cancellationToken = default);

    Task<PartidaTrivia?> GetByIdAsync(PartidaId id, CancellationToken cancellationToken = default);

    Task UpdateAsync(PartidaTrivia partida, CancellationToken cancellationToken = default);

    Task<int> CountInscripcionesAsync(PartidaId partidaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartidaTrivia>> GetPublishedAsync(CancellationToken cancellationToken = default);
}
