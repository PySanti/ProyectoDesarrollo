namespace Umbral.TriviaGame.Application.Dtos;

public sealed record TriviaFormListItemDto(
    Guid Id,
    string Title,
    bool IsComplete,
    int QuestionsCount,
    DateTimeOffset CreatedAtUtc);
