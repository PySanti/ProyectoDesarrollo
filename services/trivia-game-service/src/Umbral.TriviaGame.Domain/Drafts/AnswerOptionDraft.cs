using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Drafts;

/// <summary>
/// Borrador de una opción de respuesta recibida desde capa de aplicación o API.
/// Permite validar texto antes de materializar el value object <see cref="AnswerOption"/>.
/// </summary>
public sealed class AnswerOptionDraft
{
    /// <summary>
    /// Texto en bruto de la opción (se recortará al validar).
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Indica si el operador marcó esta opción como la correcta.
    /// </summary>
    public bool IsCorrect { get; }

    /// <summary>
    /// Constructor privado: uso exclusivo del factory <see cref="Create"/>.
    /// </summary>
    private AnswerOptionDraft(string text, bool isCorrect)
    {
        // Almacena el texto ya recortado y validado como no vacío.
        Text = text;
        // Almacena la bandera de corrección tal como la envió el cliente.
        IsCorrect = isCorrect;
    }

    /// <summary>
    /// Crea un borrador validando que el texto de la opción no esté vacío.
    /// </summary>
    public static AnswerOptionDraft Create(string text, bool isCorrect)
    {
        // Rechaza valores nulos, vacíos o solo espacios en blanco.
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainValidationException("El texto de la opción es obligatorio.");
        }

        // Normaliza espacios al inicio y final antes de persistir en el borrador.
        var trimmedText = text.Trim();

        // Devuelve el borrador listo para convertirse en AnswerOption.
        return new AnswerOptionDraft(trimmedText, isCorrect);
    }

    /// <summary>
    /// Convierte el borrador en un <see cref="AnswerOption"/> aplicando reglas de <see cref="OptionText"/>.
    /// </summary>
    /// <param name="orden">Posición 0-based de la opción en la lista.</param>
    public AnswerOption ToAnswerOption(int orden)
    {
        // Crea el value object OptionText (valida longitud máxima y contenido).
        var optionText = OptionText.Create(Text);
        // Materializa la opción de respuesta de dominio.
        return AnswerOption.Create(optionText, IsCorrect, orden);
    }
}
