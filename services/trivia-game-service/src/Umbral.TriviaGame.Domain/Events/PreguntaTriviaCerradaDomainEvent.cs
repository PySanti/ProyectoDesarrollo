using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Events;

public sealed record PreguntaTriviaCerradaDomainEvent(
    PartidaId PartidaId,
    Guid PreguntaId,
    MotivoCierre Motivo,
    string RespuestaCorrecta) : DomainEvent;
