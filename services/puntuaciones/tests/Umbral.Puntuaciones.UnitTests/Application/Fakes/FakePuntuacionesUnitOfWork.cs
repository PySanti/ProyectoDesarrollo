using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.UnitTests.Application.Fakes;

public sealed class FakePuntuacionesUnitOfWork : IPuntuacionesUnitOfWork
{
    public int Saves { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saves++;
        return Task.CompletedTask;
    }
}
