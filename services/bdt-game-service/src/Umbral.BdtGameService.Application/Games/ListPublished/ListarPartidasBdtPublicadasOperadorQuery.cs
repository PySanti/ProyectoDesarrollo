using MediatR;

namespace Umbral.BdtGameService.Application.Games.ListPublished;

public sealed record ListarPartidasBdtPublicadasOperadorQuery(Guid ActorUserId)
    : IRequest<IReadOnlyList<PartidaBdtPublicadaItem>>;
