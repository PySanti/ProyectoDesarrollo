using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Queries;

public sealed record GetTriviaGameTeamsQuery(Guid PartidaId)
    : IRequest<IReadOnlyList<TriviaEquipoLobbyDto>>;
