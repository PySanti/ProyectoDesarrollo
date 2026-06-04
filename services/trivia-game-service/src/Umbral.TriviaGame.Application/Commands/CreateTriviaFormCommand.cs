using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Commands;

public sealed record CreateTriviaFormCommand(
    string Title,
    IReadOnlyList<QuestionInputDto> Questions
) : IRequest<TriviaFormDetailDto>;
