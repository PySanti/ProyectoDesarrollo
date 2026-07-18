namespace Umbral.OperacionesSesion.Domain.Results;

public sealed record ResultadoRespuesta(
    bool EsCorrecta,
    bool CerroPregunta,
    int? Puntaje,
    Guid JuegoId,
    Guid PreguntaId,
    Guid ParticipanteId,
    Guid OpcionId,
    DateTime Instante,
    long TiempoRespuestaMs,
    Guid? EquipoId = null,
    // Set cuando el acierto cierra la ÚLTIMA pregunta del juego: el juego se finaliza
    // (avanza al siguiente o termina la partida), igual que el camino por timeout.
    ResultadoAvance? JuegoFinalizado = null);
