namespace Umbral.Partidas.Domain.Abstractions.Persistence;

public interface IPartidasUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
