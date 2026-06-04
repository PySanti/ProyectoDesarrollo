using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Events;

/// <summary>
/// Evento de dominio elevado después de modificar exitosamente un TriviaForm
/// (título, preguntas o ambos). Transporta el identificador y título actual
/// para que los handlers de aplicación reaccionen.
/// </summary>
/// <param name="FormId">Identificador único del formulario actualizado.</param>
/// <param name="Title">Título del formulario después de la actualización.</param>
/// <param name="UpdateKind">Indica qué parte del formulario se modificó.</param>
public sealed record TriviaFormUpdatedDomainEvent(
    TriviaFormId FormId,
    FormTitle Title,
    TriviaFormUpdateKind UpdateKind) : DomainEvent;

/// <summary>
/// Clasifica el tipo de actualización realizada sobre el formulario.
/// </summary>
public enum TriviaFormUpdateKind
{
    /// <summary>Solo se actualizó el título.</summary>
    TitleOnly,

    /// <summary>Solo se reemplazaron las preguntas.</summary>
    QuestionsOnly,

    /// <summary>Se actualizó tanto el título como las preguntas.</summary>
    TitleAndQuestions,
}
