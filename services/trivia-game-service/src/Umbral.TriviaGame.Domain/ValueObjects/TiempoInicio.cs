using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class TiempoInicio : ValueObject
{
    public DateTimeOffset Value { get; }

    private TiempoInicio(DateTimeOffset value)
    {
        Value = value;
    }

    public static TiempoInicio Create(DateTimeOffset value)
    {
        if (value == default)
        {
            throw new DomainValidationException("La fecha y hora de inicio son obligatorias.");
        }

        return new TiempoInicio(value);
    }

    public static TiempoInicio CreateWithFutureValidation(DateTimeOffset value, DateTimeOffset now)
    {
        if (value == default)
        {
            throw new DomainValidationException("La fecha y hora de inicio son obligatorias.");
        }

        if (value <= now)
        {
            throw new DomainValidationException(
                "La fecha y hora de inicio deben ser posteriores al momento actual.");
        }

        return new TiempoInicio(value);
    }

    public bool EsAutomatico(DateTimeOffset now) => Value <= now;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("O");
}
