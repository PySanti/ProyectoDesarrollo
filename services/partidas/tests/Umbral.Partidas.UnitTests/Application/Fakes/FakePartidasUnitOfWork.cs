using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Domain.Abstractions.Persistence;

namespace Umbral.Partidas.UnitTests.Application.Fakes;

public sealed class FakePartidasUnitOfWork : IPartidasUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
