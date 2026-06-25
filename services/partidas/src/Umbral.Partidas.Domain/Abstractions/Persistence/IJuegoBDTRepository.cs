using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Abstractions.Persistence;

public interface IJuegoBDTRepository
{
    void Add(JuegoBDT juego);
    Task<IReadOnlyList<JuegoBDT>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken);
}
