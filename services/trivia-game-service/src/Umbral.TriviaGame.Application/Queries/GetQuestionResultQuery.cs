using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Queries;

public sealed record GetQuestionResultQuery(
    Guid PartidaId,
    Guid PreguntaId,
    string UsuarioId
) : IRequest<QuestionResultDto>;
