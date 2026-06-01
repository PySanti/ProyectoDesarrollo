using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Queries;

public sealed record GetTriviaGameParticipantsQuery(Guid PartidaId)
    : IRequest<TriviaGameLobbyDto>;
