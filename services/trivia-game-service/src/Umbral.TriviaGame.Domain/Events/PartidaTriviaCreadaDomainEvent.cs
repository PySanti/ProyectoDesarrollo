using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Events;

public sealed record PartidaTriviaCreadaDomainEvent(
    PartidaId PartidaId,
    NombrePartida Nombre,
    OperatorId CreatedByOperatorId) : DomainEvent;
