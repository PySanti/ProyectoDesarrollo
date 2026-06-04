using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Events;

public sealed record PartidaTriviaPublicadaDomainEvent(
    PartidaId PartidaId,
    NombrePartida Nombre) : DomainEvent;
