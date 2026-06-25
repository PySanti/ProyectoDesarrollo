using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Application.Fakes;

public sealed class FakeJuegoTriviaRepository : IJuegoTriviaRepository
{
    public readonly List<JuegoTrivia> Store = new();

    public void Add(JuegoTrivia juego) => Store.Add(juego);

    public Task<IReadOnlyList<JuegoTrivia>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<JuegoTrivia>)Store.Where(j => j.PartidaId == partidaId).ToList());
}
