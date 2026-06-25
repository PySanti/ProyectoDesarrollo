using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Application.Fakes;

public sealed class FakeJuegoBDTRepository : IJuegoBDTRepository
{
    public readonly List<JuegoBDT> Store = new();

    public void Add(JuegoBDT juego) => Store.Add(juego);

    public Task<IReadOnlyList<JuegoBDT>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<JuegoBDT>)Store.Where(j => j.PartidaId == partidaId).ToList());
}
