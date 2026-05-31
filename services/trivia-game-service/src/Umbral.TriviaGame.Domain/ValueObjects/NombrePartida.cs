using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class NombrePartida : ValueObject
{
    public const int MinLength = 3;
    public const int MaxLength = 100;

    public string Value { get; }

    private NombrePartida(string value)
    {
        Value = value;
    }

    public static NombrePartida Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("El nombre de la partida es obligatorio.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length < MinLength)
        {
            throw new DomainValidationException(
                $"El nombre de la partida debe tener al menos {MinLength} caracteres.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new DomainValidationException(
                $"El nombre de la partida no puede superar {MaxLength} caracteres.");
        }

        return new NombrePartida(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
