using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Events;

/// <summary>
/// Evento de dominio elevado después de crear exitosamente un TriviaForm.
/// Transporta el identificador, título y operador creador para que los handlers
/// de aplicación (auditoría, logging, notificaciones opcionales) puedan reaccionar.
/// </summary>
/// <param name="FormId">Identificador único del formulario creado.</param>
/// <param name="Title">Título del formulario al momento de creación.</param>
/// <param name="CreatedByOperatorId">Identificador del operador que creó el formulario.</param>
public sealed record TriviaFormCreatedDomainEvent(
    TriviaFormId FormId,
    FormTitle Title,
    OperatorId CreatedByOperatorId) : DomainEvent;
