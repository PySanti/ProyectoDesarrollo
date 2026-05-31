using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class TriviaInscripcionId : ValueObject
{
    public Guid Value { get; }

    private TriviaInscripcionId(Guid value)
    {
        Value = value;
    }

    public static TriviaInscripcionId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainValidationException("El identificador de la inscripción no puede estar vacío.");

        return new TriviaInscripcionId(value);
    }

    public static TriviaInscripcionId New() => Create(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
