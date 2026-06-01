using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Drafts;

namespace Umbral.TriviaGame.Domain.ValueObjects;

/// <summary>
/// Representa una opción de respuesta dentro de una pregunta de Trivia.
/// Es un value object: se identifica por su contenido (texto + si es correcta).
/// </summary>
public sealed class AnswerOption : ValueObject
{
    /// <summary>
    /// Texto visible de la opción; ya validado por <see cref="OptionText"/>.
    /// </summary>
    public OptionText Text { get; }

    /// <summary>
    /// Indica si esta opción es la respuesta correcta de la pregunta.
    /// Solo una opción por pregunta debe ser correcta (regla aplicada en la entidad Question, D-03).
    /// </summary>
    public bool IsCorrect { get; }

    /// <summary>
    /// Posición 0-based de esta opción en la lista de opciones de la pregunta.
    /// Garantiza orden determinístico al persistir y recuperar desde la base de datos.
    /// </summary>
    public int Orden { get; }

    /// <summary>
    /// Constructor privado: solo se instancia vía <see cref="Create"/>.
    /// </summary>
    private AnswerOption(OptionText text, bool isCorrect, int orden)
    {
        Text = text;
        IsCorrect = isCorrect;
        Orden = orden;
    }

    /// <summary>
    /// Crea una opción de respuesta a partir de value objects ya validados.
    /// </summary>
    public static AnswerOption Create(OptionText text, bool isCorrect, int orden)
    {
        if (text is null)
        {
            throw new DomainValidationException("El texto de la opción es obligatorio.");
        }

        return new AnswerOption(text, isCorrect, orden);
    }

    /// <summary>
    /// Materializa una opción desde un borrador de construcción.
    /// </summary>
    public static AnswerOption FromDraft(AnswerOptionDraft draft, int orden)
    {
        if (draft is null)
        {
            throw new DomainValidationException("El borrador de la opción es obligatorio.");
        }

        return draft.ToAnswerOption(orden);
    }

    /// <summary>
    /// Componentes usados para igualdad estructural entre dos AnswerOption.
    /// </summary>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Text;
        yield return IsCorrect;
        yield return Orden;
    }

    /// <summary>
    /// Representación legible para depuración y logs.
    /// </summary>
    public override string ToString() =>
        $"{Text.Value} (Correcta: {IsCorrect})";
}
