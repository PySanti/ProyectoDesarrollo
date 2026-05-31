using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Queries;

public sealed record GetTriviaGameLobbyQuery(Guid PartidaId, string UsuarioId)
    : IRequest<TriviaGameLobbyDto>;
