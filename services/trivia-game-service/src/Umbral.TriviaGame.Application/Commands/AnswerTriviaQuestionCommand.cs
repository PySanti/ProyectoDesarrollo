using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Commands;

public sealed record AnswerTriviaQuestionCommand(
    Guid PartidaId,
    Guid PreguntaId,
    string UsuarioId,
    int OpcionIndex
) : IRequest<RespuestaTriviaDto>;
