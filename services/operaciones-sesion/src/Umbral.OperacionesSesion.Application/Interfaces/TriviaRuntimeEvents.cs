namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record RespuestaTriviaValidadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    Guid ParticipanteId, Guid OpcionId, bool EsCorrecta, DateTime Instante,
    Guid? EquipoId = null);

public sealed record PuntajeTriviaIncrementadoEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    Guid ParticipanteId, int Puntaje, long TiempoRespuestaMs,
    Guid? EquipoId = null);

public sealed record PreguntaTriviaActivadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    int Orden, int TiempoLimiteSegundos, DateTime FechaActivacion);

public sealed record PreguntaTriviaCerradaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    string Motivo, DateTime FechaCierre, Guid? GanadorParticipanteId,
    Guid? GanadorEquipoId = null);
