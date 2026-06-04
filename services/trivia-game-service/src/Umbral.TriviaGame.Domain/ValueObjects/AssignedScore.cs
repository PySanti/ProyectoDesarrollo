using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class AssignedScore : ValueObject
{
    public const int MinValue = 1;
    public const int MaxValue = 1000;

    public int Value { get; }

    private AssignedScore(int value)
    {
        Value = value;
    }

    public static AssignedScore Create(int value)
    {
        if (value < MinValue || value > MaxValue)
        {
            throw new DomainValidationException(
                $"El puntaje asignado debe estar entre {MinValue} y {MaxValue}.");
        }

        return new AssignedScore(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
