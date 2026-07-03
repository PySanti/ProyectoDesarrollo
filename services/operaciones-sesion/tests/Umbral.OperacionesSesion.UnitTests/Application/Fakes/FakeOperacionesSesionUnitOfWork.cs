using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeOperacionesSesionUnitOfWork : IOperacionesSesionUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
