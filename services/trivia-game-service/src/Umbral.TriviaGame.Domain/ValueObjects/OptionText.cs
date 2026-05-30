using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class OptionText : ValueObject
{
    public const int MaxLength = 500;

    public string Value { get; }

    private OptionText(string value)
    {
        Value = value;
    }

    public static OptionText Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("El texto de la opción es obligatorio.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new DomainValidationException(
                $"El texto de la opción no puede superar {MaxLength} caracteres.");
        }

        return new OptionText(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
