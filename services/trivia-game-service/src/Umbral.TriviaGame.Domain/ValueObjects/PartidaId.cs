using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class PartidaId : ValueObject
{
    public Guid Value { get; }

    private PartidaId(Guid value)
    {
        Value = value;
    }

    public static PartidaId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException("El identificador de la partida no puede estar vacío.");
        }

        return new PartidaId(value);
    }

    public static PartidaId New() => Create(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
