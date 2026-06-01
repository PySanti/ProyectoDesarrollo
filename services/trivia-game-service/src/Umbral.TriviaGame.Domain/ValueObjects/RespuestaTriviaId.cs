using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class RespuestaTriviaId : ValueObject
{
    public Guid Value { get; }

    private RespuestaTriviaId(Guid value)
    {
        Value = value;
    }

    public static RespuestaTriviaId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainValidationException("El identificador de la respuesta no puede estar vacío.");
        return new RespuestaTriviaId(value);
    }

    public static RespuestaTriviaId New() => Create(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
