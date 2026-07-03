using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Infrastructure.Persistence;

public sealed class OperacionesSesionUnitOfWork : IOperacionesSesionUnitOfWork
{
    private readonly OperacionesSesionDbContext _dbContext;

    public OperacionesSesionUnitOfWork(OperacionesSesionDbContext dbContext) => _dbContext = dbContext;

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
