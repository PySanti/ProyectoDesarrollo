using MediatR;

namespace Umbral.BdtGameService.Application.Games.JoinIndividual;

public sealed record UnirseABdtIndividualCommand(Guid PartidaId, Guid ParticipanteUserId) : IRequest<UnirseABdtIndividualResponse>;
