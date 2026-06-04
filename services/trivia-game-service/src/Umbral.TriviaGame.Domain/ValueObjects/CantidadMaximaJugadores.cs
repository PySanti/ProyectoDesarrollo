using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class CantidadMaximaJugadores : ValueObject
{
    public int Value { get; }

    private CantidadMaximaJugadores(int value)
    {
        Value = value;
    }

    public static CantidadMaximaJugadores Create(int value, CantidadMinima minima)
    {
        if (value < minima.Value)
        {
            throw new DomainValidationException(
                $"La cantidad máxima de jugadores ({value}) no puede ser menor que la cantidad mínima ({minima.Value}).");
        }

        return new CantidadMaximaJugadores(value);
    }

    public static CantidadMaximaJugadores Create(int value)
    {
        return new CantidadMaximaJugadores(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
