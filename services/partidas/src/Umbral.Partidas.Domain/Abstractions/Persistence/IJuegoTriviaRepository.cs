using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Abstractions.Persistence;

public interface IJuegoTriviaRepository
{
    void Add(JuegoTrivia juego);
    Task<IReadOnlyList<JuegoTrivia>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken);
}
