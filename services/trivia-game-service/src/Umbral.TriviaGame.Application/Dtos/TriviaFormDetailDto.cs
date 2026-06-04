namespace Umbral.TriviaGame.Application.Dtos;

public sealed record TriviaFormDetailDto(
    Guid Id,
    string Title,
    bool IsComplete,
    IReadOnlyList<string> IncompleteReasons,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<QuestionDetailDto> Questions);
