using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Drafts;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Entities;

/// <summary>
/// Entidad que representa una pregunta dentro de un formulario de Trivia.
/// Cada pregunta es identificada por un QuestionId único.
/// Protege las invariantes: exactamente 4 opciones, exactamente 1 correcta,
/// puntaje asignado en [1..1000] y temporizador en [5..300] segundos.
/// </summary>
public sealed class Question : Entity<QuestionId>
{
    /// <summary>
    /// Texto del enunciado de la pregunta, validado por el value object QuestionText.
    /// </summary>
    public QuestionText Text { get; private set; }

    /// <summary>
    /// Puntaje fijo que otorga esta pregunta si se responde correctamente en una partida.
    /// </summary>
    public AssignedScore AssignedScore { get; private set; }

    /// <summary>
    /// Tiempo límite en segundos para responder esta pregunta durante una partida.
    /// No modifica el puntaje; solo controla disponibilidad y cierre de la pregunta.
    /// </summary>
    public TimeLimit TimeLimit { get; private set; }

    /// <summary>
    /// Orden de visualización de la pregunta dentro del formulario (1-based).
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Lista interna inmutable de opciones de respuesta. Solo se modifica vía Create o UpdateFrom.
    /// </summary>
    private readonly List<AnswerOption> _options = new();

    /// <summary>
    /// Expone las opciones como colección de solo lectura para evitar mutaciones externas.
    /// </summary>
    public IReadOnlyCollection<AnswerOption> Options => _options.AsReadOnly();

    /// <summary>
    /// Constructor privado sin parámetros requerido por EF Core para materializar la entidad desde la base de datos.
    /// </summary>
    private Question() : base(QuestionId.New()) { }

    /// <summary>
    /// Constructor privado: solo se instancia a través del factory method Create.
    /// </summary>
    private Question(
        QuestionId id,
        QuestionText text,
        AssignedScore assignedScore,
        TimeLimit timeLimit,
        int displayOrder,
        IReadOnlyList<AnswerOption> options)
        : base(id) // Llama al constructor de Entity<QuestionId> con el identificador ya validado.
    {
        // Asigna cada value object ya validado a su propiedad correspondiente.
        Text = text;
        AssignedScore = assignedScore;
        TimeLimit = timeLimit;
        DisplayOrder = displayOrder;

        // Agrega cada opción validada a la lista interna inmutable.
        _options.AddRange(options);
    }

    /// <summary>
    /// Factory method que crea una entidad Question a partir de un borrador (QuestionDraft).
    /// Valida todas las invariantes de la pregunta antes de construir la instancia.
    /// </summary>
    /// <param name="draft">Borrador con los datos en bruto de la pregunta.</param>
    /// <param name="displayOrder">Orden de visualización asignado dentro del formulario.</param>
    /// <returns>Instancia inmutable de Question con todas las validaciones aprobadas.</returns>
    /// <exception cref="DomainValidationException">Si el borrador es nulo.</exception>
    /// <exception cref="InvalidQuestionOptionsCountException">Si no hay exactamente 4 opciones.</exception>
    /// <exception cref="InvalidCorrectOptionCountException">Si no hay exactamente 1 opción correcta.</exception>
    internal static Question Create(QuestionDraft draft, int displayOrder)
    {
        // Valida que el borrador no sea nulo (defensa contra errores de integración en capas superiores).
        if (draft is null)
        {
            throw new DomainValidationException(
                "El borrador de la pregunta es obligatorio para crear una entidad Question.");
        }

        // Valida que el orden de visualización sea positivo (1-based).
        if (displayOrder < 1)
        {
            throw new DomainValidationException(
                "El orden de visualización de la pregunta debe ser mayor o igual a 1.");
        }

        // Convierte el texto del borrador al value object QuestionText (valida longitud y contenido).
        var text = draft.ToQuestionText();

        // Convierte el puntaje del borrador al value object AssignedScore (valida rango 1..1000).
        var assignedScore = draft.ToAssignedScore();

        // Convierte el temporizador del borrador al value object TimeLimit (valida rango 5..300).
        var timeLimit = draft.ToTimeLimit();

        // Materializa las opciones del borrador como value objects AnswerOption.
        var options = draft.ToAnswerOptions();

        // INVARIANTE 1: La pregunta debe tener exactamente 4 opciones (HU-15-FORM-001).
        if (options.Count != 4)
        {
            throw new InvalidQuestionOptionsCountException(options.Count);
        }

        // INVARIANTE 2: Debe haber exactamente 1 opción marcada como correcta (HU-15-FORM-002).
        var correctCount = options.Count(o => o.IsCorrect);
        if (correctCount != 1)
        {
            throw new InvalidCorrectOptionCountException(correctCount);
        }

        // Genera un identificador único para esta pregunta.
        var questionId = QuestionId.New();

        // Construye y retorna la entidad Question con todas las validaciones aprobadas.
        return new Question(
            questionId,
            text,
            assignedScore,
            timeLimit,
            displayOrder,
            options);
    }

    /// <summary>
    /// Actualiza todas las propiedades de la pregunta a partir de un nuevo borrador,
    /// preservando el mismo identificador (QuestionId).
    /// Se usa cuando el operador edita un formulario existente (HU-15, edición).
    /// </summary>
    /// <param name="draft">Nuevo borrador con los datos actualizados.</param>
    /// <param name="displayOrder">Nuevo orden de visualización.</param>
    internal void UpdateFrom(QuestionDraft draft, int displayOrder)
    {
        // Reutiliza las mismas validaciones del factory, pero sin generar un nuevo Id.
        // Valida que el borrador no sea nulo.
        if (draft is null)
        {
            throw new DomainValidationException(
                "El borrador de la pregunta es obligatorio para actualizar una entidad Question.");
        }

        // Valida el orden de visualización.
        if (displayOrder < 1)
        {
            throw new DomainValidationException(
                "El orden de visualización de la pregunta debe ser mayor o igual a 1.");
        }

        // Materializa los value objects desde el borrador (valida rangos/longitudes).
        var text = draft.ToQuestionText();
        var assignedScore = draft.ToAssignedScore();
        var timeLimit = draft.ToTimeLimit();

        // Materializa las opciones desde el borrador.
        var options = draft.ToAnswerOptions();

        // INVARIANTE 1: validar cantidad de opciones.
        if (options.Count != 4)
        {
            throw new InvalidQuestionOptionsCountException(options.Count);
        }

        // INVARIANTE 2: validar cantidad de opciones correctas.
        var correctCount = options.Count(o => o.IsCorrect);
        if (correctCount != 1)
        {
            throw new InvalidCorrectOptionCountException(correctCount);
        }

        // -------------------------------
        // Todas las validaciones pasaron; ahora se muta el estado del aggregate.
        // -------------------------------

        // Reconvierte y reasigna cada value object (vuelve a validar rangos/longitudes).
        Text = text;
        AssignedScore = assignedScore;
        TimeLimit = timeLimit;
        DisplayOrder = displayOrder;

        // Reemplaza la lista interna de opciones con las nuevas (el Id de la entidad no cambia).
        _options.Clear();
        _options.AddRange(options);
    }

    /// <summary>
    /// Obtiene la única opción marcada como respuesta correcta.
    /// </summary>
    /// <returns>El value object AnswerOption que es la respuesta correcta.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si no hay exactamente 1 opción correcta (esto indicaría un estado de corrupción del agregado).
    /// </exception>
    public AnswerOption GetCorrectOption()
    {
        // Busca la única opción con IsCorrect = true.
        // SingleOrDefaul + validación explícita permite un mejor mensaje de error que Single() desnudo.
        var correctOption = _options.SingleOrDefault(o => o.IsCorrect);

        // Si no se encontró ninguna opción correcta, el estado del agregado está corrupto.
        if (correctOption is null)
        {
            throw new InvalidOperationException(
                "Estado inválido: la pregunta no tiene ninguna opción marcada como correcta.");
        }

        return correctOption;
    }

    /// <summary>
    /// Verifica si la pregunta tiene exactamente una opción correcta.
    /// Útil para validaciones de completitud desde el agregado TriviaForm.
    /// </summary>
    /// <returns>True si hay exactamente 1 opción correcta; false en cualquier otro caso.</returns>
    public bool HasExactlyOneCorrectOption()
    {
        // Cuenta las opciones correctas y verifica que sea exactamente 1.
        return _options.Count(o => o.IsCorrect) == 1;
    }

    /// <summary>
    /// Representación legible para depuración y logs.
    /// </summary>
    public override string ToString() =>
        $"Question {{ Id: {Id}, Text: \"{Text.Value.Substring(0, Math.Min(Text.Value.Length, 50))}...\", DisplayOrder: {DisplayOrder}, Options: {_options.Count} }}";
}
