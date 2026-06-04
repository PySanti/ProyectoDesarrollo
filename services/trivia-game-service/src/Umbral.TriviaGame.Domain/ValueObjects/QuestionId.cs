using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Domain.ValueObjects;

public sealed class QuestionId : ValueObject
{
    public Guid Value { get; }

    private QuestionId(Guid value)
    {
        Value = value;
    }

    public static QuestionId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException("El identificador de la pregunta no puede estar vacío.");
        }

        return new QuestionId(value);
    }

    public static QuestionId New() => Create(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
