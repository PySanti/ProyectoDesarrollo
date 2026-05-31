using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Commands;

public sealed record JoinTriviaGameCommand(
    Guid PartidaId,
    string UsuarioId
) : IRequest<TriviaInscripcionDto>;
