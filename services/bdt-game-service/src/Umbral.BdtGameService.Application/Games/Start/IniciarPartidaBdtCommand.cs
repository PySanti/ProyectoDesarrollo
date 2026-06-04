using MediatR;

namespace Umbral.BdtGameService.Application.Games.Start;

public sealed record IniciarPartidaBdtCommand(Guid PartidaId, Guid OperadorUserId) : IRequest<IniciarPartidaBdtResponse>;
