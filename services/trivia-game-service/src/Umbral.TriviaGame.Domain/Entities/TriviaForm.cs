using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.DomainServices;
using Umbral.TriviaGame.Domain.Drafts;
using Umbral.TriviaGame.Domain.Events;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Entities;

/// <summary>
/// Aggregate root que representa un formulario de Trivia.
///
/// Un formulario agrupa preguntas (entidades hijas) bajo un título y protege
/// la invariante global: debe contener al menos 1 pregunta para considerarse
/// completo y utilizable en una partida (RF-15, RF-16).
///
/// Responsabilidades:
/// - Crear un formulario a partir de un título, un operador creador y un conjunto
///   de borradores de pregunta.
/// - Actualizar el título.
/// - Reemplazar el conjunto completo de preguntas (edición desde el panel operador).
/// - Garantizar que las preguntas queden ordenadas sin gaps en DisplayOrder.
/// - Mantener timestamps de creación y última modificación.
/// - Preservar la identidad del operador creador como inmutable.
/// </summary>
public sealed class TriviaForm : AggregateRoot<TriviaFormId>
{
    /// <summary>
    /// Título del formulario. Validado por el value object FormTitle (no vacío, max 200 caracteres).
    /// </summary>
    public FormTitle Title { get; private set; }

    /// <summary>
    /// Identificador del operador que creó originalmente el formulario.
    /// Es inmutable después de la creación (solo lectura pública, sin setter).
    /// </summary>
    public OperatorId CreatedByOperatorId { get; }

    /// <summary>
    /// Marca temporal UTC de creación del formulario.
    /// Es inmutable después de la creación (solo lectura pública, sin setter).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// Marca temporal UTC de la última modificación del formulario.
    /// Se actualiza en cada mutación (UpdateTitle, ReplaceQuestions).
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Lista interna de preguntas que componen el formulario.
    /// Solo se modifica a través de Create o ReplaceQuestions.
    /// </summary>
    private readonly List<Question> _questions = new();

    /// <summary>
    /// Expone las preguntas como colección de solo lectura.
    /// Los detalles de cada pregunta (opciones, score, timer) se consultan a través de la entidad Question.
    /// </summary>
    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    // ---------------------------------------------------------------------------
    // IsComplete: delega al servicio de dominio TriviaFormCompletenessValidator.
    // Evalúa las reglas RF-16 / TRIVIA-FORM-001:
    //   1. Al menos una pregunta.
    //   2. Cada pregunta: 4 opciones, 1 correcta.
    //   3. Sin textos duplicados de opciones dentro de la misma pregunta.
    // ---------------------------------------------------------------------------
    /// <summary>
    /// Indica si el formulario está completo según las reglas de dominio.
    /// Delega al servicio de dominio TriviaFormCompletenessValidator para evaluar
    /// todas las reglas de completitud (RF-16 / TRIVIA-FORM-001):
    /// - al menos una pregunta;
    /// - cada pregunta con 4 opciones y exactamente 1 correcta;
    /// - sin textos duplicados de opciones dentro de la misma pregunta.
    /// </summary>
    public bool IsComplete =>
        DomainServices.TriviaFormCompletenessValidator.IsComplete(this);

    /// <summary>
    /// Constructor privado sin parámetros requerido por EF Core para materializar la entidad desde la base de datos.
    /// </summary>
    private TriviaForm() : base(TriviaFormId.New()) { }

    /// <summary>
    /// Constructor privado: solo el factory method Create puede instanciar TriviaForm.
    /// Recibe todos los datos ya validados y construye la entidad con su identificador único.
    /// </summary>
    /// <param name="id">Identificador único del formulario, generado por TriviaFormId.New().</param>
    /// <param name="title">Título ya validado por FormTitle.Create.</param>
    /// <param name="operatorId">Identificador del operador creador, ya validado.</param>
    /// <param name="questions">Lista de preguntas ya validadas y materializadas.</param>
    /// <param name="createdAtUtc">Marca temporal de creación.</param>
    private TriviaForm(
        TriviaFormId id,
        FormTitle title,
        OperatorId operatorId,
        List<Question> questions,
        DateTimeOffset createdAtUtc)
        : base(id) // Llama al constructor de AggregateRoot<TriviaFormId> con el Id ya validado.
    {
        // Asigna propiedades inmutables y mutables.
        Title = title;
        CreatedByOperatorId = operatorId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc; // Al crearse, updated = created.

        // Agrega todas las preguntas validadas a la lista interna.
        _questions.AddRange(questions);
    }

    // ===========================================================================
    // FACTORY METHOD
    // ===========================================================================

    /// <summary>
    /// Crea un nuevo formulario de Trivia a partir de un título, el operador creador
    /// y una colección de borradores de pregunta. Valida todas las invariantes del
    /// agregado antes de construir la instancia.
    /// </summary>
    /// <param name="title">Título del formulario (no vacío).</param>
    /// <param name="operatorId">Identificador del operador que crea el formulario.</param>
    /// <param name="questionDrafts">Borradores de las preguntas a incluir en el formulario.</param>
    /// <returns>Instancia de TriviaForm con todas las validaciones aprobadas.</returns>
    /// <exception cref="DomainValidationException">Si el título o el operador son inválidos.</exception>
    /// <exception cref="EmptyQuestionSetException">Si no se proporciona al menos una pregunta.</exception>
    public static TriviaForm Create(
        FormTitle title,
        OperatorId operatorId,
        IEnumerable<QuestionDraft> questionDrafts)
    {
        // -----------------------------------------------------------------------
        // VALIDACIONES DE PARÁMETROS DIRECTOS (fail-fast antes de materializar).
        // -----------------------------------------------------------------------

        // Valida que el título no sea nulo (la validación de contenido ya la hace FormTitle internamente).
        if (title is null)
        {
            throw new DomainValidationException(
                "El título del formulario es obligatorio para crear un TriviaForm.");
        }

        // Valida que el operador creador no sea nulo.
        if (operatorId is null)
        {
            throw new DomainValidationException(
                "El identificador del operador es obligatorio para crear un TriviaForm.");
        }

        // Valida que la colección de borradores no sea nula.
        if (questionDrafts is null)
        {
            throw new DomainValidationException(
                "La lista de borradores de preguntas es obligatoria para crear un TriviaForm.");
        }

        // -----------------------------------------------------------------------
        // MATERIALIZACIÓN Y VALIDACIÓN DE PREGUNTAS.
        // -----------------------------------------------------------------------

        // Convierte los borradores en entidades Question, validando cada pregunta
        // individualmente y resecuenciando el DisplayOrder a 1..N sin gaps.
        var questions = MaterializeQuestions(questionDrafts);

        // INVARIANTE GLOBAL DEL AGREGADO: debe contener al menos 1 pregunta.
        if (questions.Count == 0)
        {
            throw new EmptyQuestionSetException();
        }

        // -----------------------------------------------------------------------
        // CONSTRUCCIÓN.
        // -----------------------------------------------------------------------

        // Genera un identificador único para el formulario.
        var formId = TriviaFormId.New();

        // Marca temporal de creación (UTC).
        var now = DateTimeOffset.UtcNow;

        // Construye el aggregate root.
        var form = new TriviaForm(formId, title, operatorId, questions, now);

        // Eleva el evento de dominio TriviaFormCreatedDomainEvent para que los
        // handlers de aplicación (auditoría, logging) puedan reaccionar.
        form.AddDomainEvent(new TriviaFormCreatedDomainEvent(formId, title, operatorId));

        return form;
    }

    // ===========================================================================
    // COMANDOS DE MUTACIÓN
    // ===========================================================================

    /// <summary>
    /// Actualiza el título del formulario. La validación del nuevo título se delega
    /// al value object FormTitle (no vacío, max 200 caracteres).
    /// </summary>
    /// <param name="newTitle">Nuevo título del formulario.</param>
    /// <exception cref="DomainValidationException">Si el nuevo título es nulo.</exception>
    public void UpdateTitle(FormTitle newTitle)
    {
        // Valida que el nuevo título no sea nulo (FormTitle.Create hace la validación de contenido).
        if (newTitle is null)
        {
            throw new DomainValidationException(
                "El nuevo título del formulario es obligatorio.");
        }

        // Reemplaza el título y actualiza la marca de modificación.
        Title = newTitle;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        // Eleva evento de dominio indicando actualización de título.
        AddDomainEvent(new TriviaFormUpdatedDomainEvent(
            Id, Title, TriviaFormUpdateKind.TitleOnly));
    }

    /// <summary>
    /// Reemplaza el conjunto completo de preguntas del formulario por uno nuevo.
    /// Se utiliza cuando el operador edita el formulario desde el panel web.
    /// Las preguntas se resecuencian automáticamente (DisplayOrder 1..N sin gaps).
    /// </summary>
    /// <param name="questionDrafts">Nuevos borradores de pregunta que reemplazan a los actuales.</param>
    /// <exception cref="DomainValidationException">Si la colección de borradores es nula.</exception>
    /// <exception cref="EmptyQuestionSetException">Si el nuevo conjunto está vacío.</exception>
    public void ReplaceQuestions(IEnumerable<QuestionDraft> questionDrafts)
    {
        // Valida que la colección de borradores no sea nula.
        if (questionDrafts is null)
        {
            throw new DomainValidationException(
                "La lista de borradores de preguntas es obligatoria para reemplazar las preguntas.");
        }

        // Materializa las nuevas preguntas validándolas individualmente.
        var newQuestions = MaterializeQuestions(questionDrafts);

        // INVARIANTE: no se puede dejar el formulario sin preguntas.
        if (newQuestions.Count == 0)
        {
            throw new EmptyQuestionSetException();
        }

        // -------------------------------
        // Todas las validaciones pasaron; se muta el estado del aggregate.
        // -------------------------------

        // Reemplaza completamente la lista interna de preguntas.
        _questions.Clear();
        _questions.AddRange(newQuestions);

        // Actualiza la marca temporal de modificación.
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        // Eleva evento de dominio indicando actualización de preguntas.
        AddDomainEvent(new TriviaFormUpdatedDomainEvent(
            Id, Title, TriviaFormUpdateKind.QuestionsOnly));
    }

    // ===========================================================================
    // MÉTODOS PRIVADOS
    // ===========================================================================

    /// <summary>
    /// Convierte una colección de borradores en entidades Question validadas.
    /// Las preguntas se ordenan por DisplayOrder del borrador y luego se resecuencian
    /// a 1, 2, 3... N para evitar gaps o duplicados en el orden.
    /// </summary>
    /// <param name="drafts">Borradores de pregunta a materializar.</param>
    /// <returns>Lista de entidades Question con DisplayOrder normalizado.</returns>
    private static List<Question> MaterializeQuestions(IEnumerable<QuestionDraft> drafts)
    {
        // Convierte el enumerable a lista para evitar múltiples iteraciones.
        var draftList = drafts.ToList();

        // Si no hay borradores, retorna lista vacía (la invariante se valida en Create/ReplaceQuestions).
        if (draftList.Count == 0)
        {
            return new List<Question>();
        }

        // Ordena los borradores por su DisplayOrder original para preservar la intención del operador.
        var orderedDrafts = draftList
            .OrderBy(d => d.DisplayOrder)
            .ToList();

        // Materializa cada borrador como entidad Question, asignando un DisplayOrder
        // resecuenciado (1, 2, 3...) para garantizar que no haya gaps ni duplicados.
        var questions = new List<Question>(orderedDrafts.Count);
        for (int i = 0; i < orderedDrafts.Count; i++)
        {
            // El displayOrder interno se asigna como i + 1 (1-based).
            // Question.Create valida que displayOrder >= 1 y que el draft sea válido.
            var question = Question.Create(orderedDrafts[i], displayOrder: i + 1);
            questions.Add(question);
        }

        return questions;
    }

    // ===========================================================================
    // ToString
    // ===========================================================================

    /// <summary>
    /// Representación legible para depuración y logs.
    /// </summary>
    public override string ToString() =>
        $"TriviaForm {{ Id: {Id}, Title: \"{Title.Value}\", Questions: {_questions.Count}, " +
        $"IsComplete: {IsComplete}, CreatedBy: {CreatedByOperatorId.Value} }}";
}
