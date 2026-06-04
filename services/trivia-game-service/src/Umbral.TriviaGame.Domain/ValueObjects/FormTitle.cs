using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class FormTitle : ValueObject
{
    public const int MaxLength = 200;

    public string Value { get; }

    private FormTitle(string value)
    {
        Value = value;
    }

    public static FormTitle Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("El título del formulario es obligatorio.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new DomainValidationException(
                $"El título del formulario no puede superar {MaxLength} caracteres.");
        }

        return new FormTitle(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
