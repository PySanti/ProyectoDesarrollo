using System;

namespace Umbral.OperacionesSesion.Api.Realtime;

public static class SesionRealtimeMessages
{
    public const string PartidaEnLobby = nameof(PartidaEnLobby);
    public const string PartidaIniciada = nameof(PartidaIniciada);
    public const string JuegoActivado = nameof(JuegoActivado);
    public const string PartidaCancelada = nameof(PartidaCancelada);
    public const string PartidaFinalizada = nameof(PartidaFinalizada);
    public const string PreguntaActivada = nameof(PreguntaActivada);
    public const string PreguntaCerrada = nameof(PreguntaCerrada);
    public const string EtapaActivada = nameof(EtapaActivada);
    public const string EtapaCerrada = nameof(EtapaCerrada);
    public const string EtapaGanada = nameof(EtapaGanada);
    public const string UbicacionActualizada = nameof(UbicacionActualizada);
    public const string PistaEnviada = nameof(PistaEnviada);
    public const string ConvocatoriaCreada = nameof(ConvocatoriaCreada);
    public const string RespuestaEquipoRegistrada = nameof(RespuestaEquipoRegistrada);
    public const string InscripcionResuelta = nameof(InscripcionResuelta);

    public static string GrupoPartida(Guid partidaId) => $"partida:{partidaId}";
    public static string GrupoOperadorPartida(Guid partidaId) => $"operador:partida:{partidaId}";
    public static string GrupoParticipante(Guid participanteId) => $"participante:{participanteId}";
    public static string GrupoEquipo(Guid equipoId) => $"equipo:{equipoId}";
}
