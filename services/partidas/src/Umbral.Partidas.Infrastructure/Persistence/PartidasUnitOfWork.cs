// Infrastructure/Persistence/PartidasUnitOfWork.cs
using Umbral.Partidas.Domain.Abstractions.Persistence;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class PartidasUnitOfWork : IPartidasUnitOfWork
{
    private readonly PartidasDbContext _dbContext;

    public PartidasUnitOfWork(PartidasDbContext dbContext) => _dbContext = dbContext;

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
