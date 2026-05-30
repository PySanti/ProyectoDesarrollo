using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class TimeLimit : ValueObject
{
    public const int MinSeconds = 5;
    public const int MaxSeconds = 300;

    public int Seconds { get; }

    private TimeLimit(int seconds)
    {
        Seconds = seconds;
    }

    public static TimeLimit Create(int seconds)
    {
        if (seconds < MinSeconds || seconds > MaxSeconds)
        {
            throw new DomainValidationException(
                $"El temporizador debe estar entre {MinSeconds} y {MaxSeconds} segundos.");
        }

        return new TimeLimit(seconds);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Seconds;
    }

    public override string ToString() => Seconds.ToString();
}
