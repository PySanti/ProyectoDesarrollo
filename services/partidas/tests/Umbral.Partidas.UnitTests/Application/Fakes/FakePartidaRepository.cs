using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Application.Fakes;

public sealed class FakePartidaRepository : IPartidaRepository
{
    private readonly Dictionary<Guid, Partida> _store = new();
    public IReadOnlyDictionary<Guid, Partida> Store => _store;

    public void Add(Partida partida) => _store[partida.PartidaId.Valor] = partida;
    public void Update(Partida partida) => _store[partida.PartidaId.Valor] = partida;

    public Task<Partida?> GetByIdAsync(PartidaId id, CancellationToken cancellationToken)
        => Task.FromResult(_store.TryGetValue(id.Valor, out var p) ? p : null);

    public Task<IReadOnlyList<Partida>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<Partida>)_store.Values.ToList());
}
