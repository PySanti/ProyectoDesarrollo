namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record TesoroQRValidadoEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
    Guid ParticipanteId, string Resultado, DateTime Instante, Guid? EquipoId = null);

public sealed record EtapaBDTGanadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
    Guid ParticipanteId, int Puntaje, long TiempoResolucionMs, Guid? EquipoId = null);

public sealed record EtapaBDTCerradaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
    string Motivo, DateTime FechaCierre, Guid? GanadorParticipanteId, Guid? GanadorEquipoId = null);

public sealed record EtapaBDTActivadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
    int Orden, int TiempoLimiteSegundos, DateTime FechaActivacion);

public sealed record PistaEnviadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid? ParticipanteDestinoId,
    string Texto, DateTime Instante, Guid? EquipoDestinoId = null);
