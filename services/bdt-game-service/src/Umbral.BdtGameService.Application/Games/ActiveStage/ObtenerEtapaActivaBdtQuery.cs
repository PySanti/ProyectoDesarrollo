using MediatR;

namespace Umbral.BdtGameService.Application.Games.ActiveStage;

public sealed record ObtenerEtapaActivaBdtQuery(Guid PartidaId, Guid ParticipanteUserId) : IRequest<ObtenerEtapaActivaBdtResponse>;
