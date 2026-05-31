using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class JugadoresPorEquipoMax : ValueObject
{
    public int Value { get; }

    private JugadoresPorEquipoMax(int value)
    {
        Value = value;
    }

    public static JugadoresPorEquipoMax Create(int value, JugadoresPorEquipoMin min)
    {
        if (value < min.Value)
        {
            throw new DomainValidationException(
                $"El máximo de jugadores por equipo ({value}) no puede ser menor que el mínimo ({min.Value}).");
        }

        return new JugadoresPorEquipoMax(value);
    }

    public static JugadoresPorEquipoMax Create(int value)
    {
        return new JugadoresPorEquipoMax(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
