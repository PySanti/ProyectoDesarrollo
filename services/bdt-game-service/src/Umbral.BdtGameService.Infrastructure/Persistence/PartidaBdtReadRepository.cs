using Microsoft.EntityFrameworkCore;
using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Application.Games.ListPublished;
using Umbral.BdtGameService.Domain.Enums;

namespace Umbral.BdtGameService.Infrastructure.Persistence;

public sealed class PartidaBdtReadRepository : IPartidaBdtReadRepository
{
    private readonly BdtDbContext _dbContext;

    public PartidaBdtReadRepository(BdtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PartidaBdtPublicadaItem>> ListPublishedAsync(Modalidad? modalidad, CancellationToken cancellationToken)
    {
        var query = _dbContext.Partidas
            .AsNoTracking()
            .Where(partida => partida.Estado == EstadoPartida.Lobby);

        if (modalidad.HasValue)
        {
            query = query.Where(partida => partida.Modalidad == modalidad.Value);
        }

        return await query
            .OrderBy(partida => partida.Nombre)
            .ThenBy(partida => partida.PartidaId)
            .Select(partida => new PartidaBdtPublicadaItem(
                partida.PartidaId,
                partida.Nombre,
                partida.Modalidad.ToString(),
                partida.Estado.ToString(),
                partida.AreaBusqueda.Descripcion,
                partida.Etapas.Count))
            .ToListAsync(cancellationToken);
    }
}
