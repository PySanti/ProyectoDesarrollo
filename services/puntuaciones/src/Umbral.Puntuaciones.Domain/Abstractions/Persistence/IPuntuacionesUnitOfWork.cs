namespace Umbral.Puntuaciones.Domain.Abstractions.Persistence;

public interface IPuntuacionesUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
