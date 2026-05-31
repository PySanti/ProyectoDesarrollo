namespace Umbral.TriviaGame.Application.Dtos;

public sealed record QuestionInputDto(
    string Text,
    int AssignedScore,
    int TimeLimitSeconds,
    int DisplayOrder,
    IReadOnlyList<AnswerOptionInputDto> Options);
