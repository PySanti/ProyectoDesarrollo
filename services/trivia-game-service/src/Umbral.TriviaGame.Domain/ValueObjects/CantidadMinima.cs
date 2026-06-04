using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class CantidadMinima : ValueObject
{
    public const int MinValue = 1;

    public int Value { get; }

    private CantidadMinima(int value)
    {
        Value = value;
    }

    public static CantidadMinima Create(int value)
    {
        if (value < MinValue)
        {
            throw new DomainValidationException(
                $"La cantidad mínima debe ser al menos {MinValue}.");
        }

        return new CantidadMinima(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
