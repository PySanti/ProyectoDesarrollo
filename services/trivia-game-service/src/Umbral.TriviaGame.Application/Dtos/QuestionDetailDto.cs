namespace Umbral.TriviaGame.Application.Dtos;

public sealed record QuestionDetailDto(
    Guid Id,
    string Text,
    int AssignedScore,
    int TimeLimitSeconds,
    int DisplayOrder,
    IReadOnlyList<AnswerOptionDetailDto> Options);
