using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class CantidadMaximaEquipos : ValueObject
{
    public const int MinValue = 1;

    public int Value { get; }

    private CantidadMaximaEquipos(int value)
    {
        Value = value;
    }

    public static CantidadMaximaEquipos Create(int value, CantidadMinima minima)
    {
        if (value < minima.Value)
        {
            throw new DomainValidationException(
                $"La cantidad máxima de equipos ({value}) no puede ser menor que la cantidad mínima ({minima.Value}).");
        }

        return new CantidadMaximaEquipos(value);
    }

    public static CantidadMaximaEquipos Create(int value)
    {
        if (value < MinValue)
        {
            throw new DomainValidationException(
                $"La cantidad máxima de equipos debe ser al menos {MinValue}.");
        }

        return new CantidadMaximaEquipos(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
