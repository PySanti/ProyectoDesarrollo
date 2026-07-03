namespace Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

public interface IOperacionesSesionUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
