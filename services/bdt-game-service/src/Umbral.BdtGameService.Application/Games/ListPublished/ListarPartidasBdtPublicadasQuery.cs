using MediatR;

namespace Umbral.BdtGameService.Application.Games.ListPublished;

public sealed record ListarPartidasBdtPublicadasQuery(Guid ActorUserId, string? Modalidad)
    : IRequest<IReadOnlyList<PartidaBdtPublicadaItem>>;
