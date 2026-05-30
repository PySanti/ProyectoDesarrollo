using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class QuestionText : ValueObject
{
    public const int MaxLength = 1000;

    public string Value { get; }

    private QuestionText(string value)
    {
        Value = value;
    }

    public static QuestionText Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("El texto de la pregunta es obligatorio.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new DomainValidationException(
                $"El texto de la pregunta no puede superar {MaxLength} caracteres.");
        }

        return new QuestionText(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
