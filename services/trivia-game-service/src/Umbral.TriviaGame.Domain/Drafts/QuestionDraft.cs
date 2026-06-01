using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Drafts;

/// <summary>
/// Borrador de una pregunta dentro de un formulario de Trivia.
/// Agrupa datos en bruto antes de crear la entidad Question (tarea D-03).
/// </summary>
public sealed class QuestionDraft
{
    /// <summary>
    /// Texto en bruto de la enunciado de la pregunta.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Puntaje entero asignado a la pregunta si se responde correctamente.
    /// </summary>
    public int AssignedScore { get; }

    /// <summary>
    /// Segundos de temporizador para responder la pregunta en partida.
    /// </summary>
    public int TimeLimitSeconds { get; }

    /// <summary>
    /// Orden de visualización dentro del formulario (1-based en flujo de negocio).
    /// </summary>
    public int DisplayOrder { get; }

    /// <summary>
    /// Colección de borradores de opciones asociados a esta pregunta.
    /// </summary>
    public IReadOnlyList<AnswerOptionDraft> Options { get; }

    /// <summary>
    /// Constructor privado: instanciación controlada por <see cref="Create"/>.
    /// </summary>
    private QuestionDraft(
        string text,
        int assignedScore,
        int timeLimitSeconds,
        int displayOrder,
        IReadOnlyList<AnswerOptionDraft> options)
    {
        // Guarda el texto ya recortado.
        Text = text;
        // Guarda el puntaje en bruto (rango validado al materializar AssignedScore).
        AssignedScore = assignedScore;
        // Guarda el temporizador en bruto (rango validado al materializar TimeLimit).
        TimeLimitSeconds = timeLimitSeconds;
        // Guarda el orden solicitado por el operador.
        DisplayOrder = displayOrder;
        // Expone lista de solo lectura de borradores de opción.
        Options = options;
    }

    /// <summary>
    /// Crea un borrador de pregunta validando campos obligatorios y colección de opciones.
    /// </summary>
    public static QuestionDraft Create(
        string text,
        int assignedScore,
        int timeLimitSeconds,
        int displayOrder,
        IEnumerable<AnswerOptionDraft>? options)
    {
        // El enunciado no puede ser nulo ni quedar vacío tras recortar.
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainValidationException("El texto de la pregunta es obligatorio.");
        }

        // El orden de visualización debe ser positivo para mantener secuencia coherente.
        if (displayOrder < 1)
        {
            throw new DomainValidationException("El orden de la pregunta debe ser mayor o igual a 1.");
        }

        // La lista de opciones es obligatoria (la cantidad exacta = 4 se valida en Question, D-03).
        if (options is null)
        {
            throw new DomainValidationException("Las opciones de la pregunta son obligatorias.");
        }

        // Materializa la colección para evitar mutaciones externas sobre IEnumerable.
        var optionList = options.ToList();

        // Rechaza preguntas sin ninguna opción en el borrador.
        if (optionList.Count == 0)
        {
            throw new DomainValidationException("La pregunta debe incluir al menos una opción.");
        }

        // Construye el borrador con texto recortado y opciones inmutables.
        return new QuestionDraft(
            text.Trim(),
            assignedScore,
            timeLimitSeconds,
            displayOrder,
            optionList);
    }

    /// <summary>
    /// Convierte el texto del borrador al value object <see cref="QuestionText"/>.
    /// </summary>
    public QuestionText ToQuestionText() => QuestionText.Create(Text);

    /// <summary>
    /// Convierte el puntaje del borrador al value object <see cref="AssignedScore"/>.
    /// </summary>
    public AssignedScore ToAssignedScore() =>
        ValueObjects.AssignedScore.Create(AssignedScore);

    /// <summary>
    /// Convierte el temporizador del borrador al value object <see cref="TimeLimit"/>.
    /// </summary>
    public TimeLimit ToTimeLimit() =>
        ValueObjects.TimeLimit.Create(TimeLimitSeconds);

    /// <summary>
    /// Materializa todas las opciones del borrador como value objects de dominio.
    /// </summary>
    public IReadOnlyList<AnswerOption> ToAnswerOptions() =>
        Options.Select((option, idx) => option.ToAnswerOption(idx)).ToList();
}
