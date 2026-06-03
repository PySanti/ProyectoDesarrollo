using Umbral.BdtGameService.Domain.Entities;

namespace Umbral.BdtGameService.Application.Abstractions.Persistence;

public interface IPartidaBdtRepository
{
    Task AddAsync(PartidaBDT partida, CancellationToken cancellationToken);
    Task<TResult> ExecuteWithPartidaRegistrationLockAsync<TResult>(
        Guid partidaId,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
    Task<PartidaBDT?> GetByIdWithExploradoresAsync(Guid partidaId, CancellationToken cancellationToken);
    Task UpdateAsync(PartidaBDT partida, CancellationToken cancellationToken);
}
