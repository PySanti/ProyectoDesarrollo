using Microsoft.EntityFrameworkCore;
using Umbral.BdtGameService.Application.Abstractions.Realtime;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.Infrastructure.Realtime;

public sealed class PartidaBdtSubscriptionAuthorizer : IPartidaBdtSubscriptionAuthorizer
{
    private readonly BdtDbContext _dbContext;

    public PartidaBdtSubscriptionAuthorizer(BdtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CanSubscribeAsync(
        Guid partidaId,
        Guid userId,
        bool isOperator,
        bool isParticipant,
        CancellationToken cancellationToken)
    {
        if (partidaId == Guid.Empty || userId == Guid.Empty)
        {
            return false;
        }

        if (isOperator)
        {
            return await _dbContext.Partidas
                .AsNoTracking()
                .AnyAsync(partida => partida.PartidaId == partidaId, cancellationToken);
        }

        if (!isParticipant)
        {
            return false;
        }

        return await _dbContext.Set<ExploradorBDT>()
            .AsNoTracking()
            .AnyAsync(explorador =>
                    explorador.PartidaId == partidaId &&
                    explorador.CompetidorId == userId &&
                    explorador.TipoCompetidor == TipoCompetidor.Usuario,
                cancellationToken);
    }
}
