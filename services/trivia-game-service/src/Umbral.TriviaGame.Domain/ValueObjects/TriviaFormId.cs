using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class TriviaFormId : ValueObject
{
    public Guid Value { get; }

    private TriviaFormId(Guid value)
    {
        Value = value;
    }

    public static TriviaFormId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException("El identificador del formulario no puede estar vacío.");
        }

        return new TriviaFormId(value);
    }

    public static TriviaFormId New() => Create(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
