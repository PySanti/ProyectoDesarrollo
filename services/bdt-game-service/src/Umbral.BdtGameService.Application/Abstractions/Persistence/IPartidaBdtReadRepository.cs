using Umbral.BdtGameService.Application.Games.ListPublished;
using Umbral.BdtGameService.Domain.Enums;

namespace Umbral.BdtGameService.Application.Abstractions.Persistence;

public interface IPartidaBdtReadRepository
{
    Task<IReadOnlyList<PartidaBdtPublicadaItem>> ListPublishedAsync(Modalidad? modalidad, CancellationToken cancellationToken);
}
