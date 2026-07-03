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
    Guid? EquipoId = null);
