using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Abstractions.Persistence;

public interface IPartidaRepository
{
    void Add(Partida partida);
    void Update(Partida partida);
    Task<Partida?> GetByIdAsync(PartidaId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Partida>> ListAsync(CancellationToken cancellationToken);
}
