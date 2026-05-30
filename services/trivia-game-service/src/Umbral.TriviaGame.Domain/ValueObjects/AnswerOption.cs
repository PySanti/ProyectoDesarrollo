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
    /// Constructor privado: solo se instancia vía <see cref="Create"/>.
    /// </summary>
    private AnswerOption(OptionText text, bool isCorrect)
    {
        // Asigna el texto ya validado del value object OptionText.
        Text = text;
        // Guarda la marca de respuesta correcta (sin validar aquí el conteo de correctas).
        IsCorrect = isCorrect;
    }

    /// <summary>
    /// Crea una opción de respuesta a partir de value objects ya validados.
    /// </summary>
    public static AnswerOption Create(OptionText text, bool isCorrect)
    {
        // Rechaza referencia nula al texto (defensa adicional ante errores de integración).
        if (text is null)
        {
            throw new DomainValidationException("El texto de la opción es obligatorio.");
        }

        // Construye la instancia inmutable de la opción.
        return new AnswerOption(text, isCorrect);
    }

    /// <summary>
    /// Materializa una opción desde un borrador de construcción.
    /// </summary>
    public static AnswerOption FromDraft(AnswerOptionDraft draft)
    {
        // El borrador no puede ser nulo al convertir a entidad de dominio.
        if (draft is null)
        {
            throw new DomainValidationException("El borrador de la opción es obligatorio.");
        }

        // Delega la validación de texto al factory del borrador.
        return draft.ToAnswerOption();
    }

    /// <summary>
    /// Componentes usados para igualdad estructural entre dos AnswerOption.
    /// </summary>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        // Compara el valor del texto de la opción.
        yield return Text;
        // Compara si ambas son marcadas como correctas.
        yield return IsCorrect;
    }

    /// <summary>
    /// Representación legible para depuración y logs.
    /// </summary>
    public override string ToString() =>
        $"{Text.Value} (Correcta: {IsCorrect})";
}
