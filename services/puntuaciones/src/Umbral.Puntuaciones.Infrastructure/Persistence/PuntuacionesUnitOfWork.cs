using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Infrastructure.Persistence;

public sealed class PuntuacionesUnitOfWork : IPuntuacionesUnitOfWork
{
    private readonly PuntuacionesDbContext _db;

    public PuntuacionesUnitOfWork(PuntuacionesDbContext db) => _db = db;

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
