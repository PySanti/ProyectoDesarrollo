using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Events;

public sealed record RespuestaTriviaRegistradaDomainEvent(
    PartidaId PartidaId,
    Guid PreguntaId,
    string UsuarioId,
    bool EsCorrecta,
    int PuntajeObtenido) : DomainEvent;
