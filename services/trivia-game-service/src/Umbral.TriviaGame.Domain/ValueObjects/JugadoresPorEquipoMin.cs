using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class JugadoresPorEquipoMin : ValueObject
{
    public const int MinValue = 1;

    public int Value { get; }

    private JugadoresPorEquipoMin(int value)
    {
        Value = value;
    }

    public static JugadoresPorEquipoMin Create(int value)
    {
        if (value < MinValue)
        {
            throw new DomainValidationException(
                $"El mínimo de jugadores por equipo debe ser al menos {MinValue}.");
        }

        return new JugadoresPorEquipoMin(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
