using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Events;

public sealed record PartidaTriviaIniciadaDomainEvent(
    PartidaId PartidaId,
    NombrePartida Nombre,
    DateTimeOffset StartedAtUtc) : DomainEvent;
