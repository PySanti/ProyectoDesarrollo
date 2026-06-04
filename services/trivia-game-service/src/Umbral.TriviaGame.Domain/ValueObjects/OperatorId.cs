using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class OperatorId : ValueObject
{
    public const int MaxLength = 256;

    public string Value { get; }

    private OperatorId(string value)
    {
        Value = value;
    }

    public static OperatorId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("El identificador del operador es obligatorio.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new DomainValidationException(
                $"El identificador del operador no puede superar {MaxLength} caracteres.");
        }

        return new OperatorId(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
