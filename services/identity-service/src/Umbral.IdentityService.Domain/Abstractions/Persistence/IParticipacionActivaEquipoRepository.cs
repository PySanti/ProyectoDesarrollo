namespace Umbral.IdentityService.Domain.Abstractions.Persistence;

public interface IParticipacionActivaEquipoRepository
{
    Task UpsertAsync(Guid equipoId, Guid partidaId, DateTime fechaUtc, CancellationToken cancellationToken);
    Task RemoveByPartidaAsync(Guid partidaId, CancellationToken cancellationToken);
    Task RemoveAsync(Guid equipoId, Guid partidaId, CancellationToken cancellationToken);
    Task<bool> ExistsByEquipoAsync(Guid equipoId, CancellationToken cancellationToken);
}
