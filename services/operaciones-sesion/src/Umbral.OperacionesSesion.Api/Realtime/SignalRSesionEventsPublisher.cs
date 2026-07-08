using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.Api.Realtime;

public sealed class SignalRSesionEventsPublisher : ISesionEventsPublisher
{
    private readonly IHubContext<SesionHub> _hub;

    public SignalRSesionEventsPublisher(IHubContext<SesionHub> hub) => _hub = hub;

    private Task Difundir(Guid partidaId, string mensaje, object payload, CancellationToken ct) =>
        _hub.Clients.Group(SesionRealtimeMessages.GrupoPartida(partidaId)).SendAsync(mensaje, payload, ct);

    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken) =>
        Difundir(evento.PartidaId, SesionRealtimeMessages.PartidaEnLobby, new PartidaEnLobbyPayload(evento.PartidaId), cancellationToken);

    public Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent evento, CancellationToken cancellationToken) =>
        Difundir(evento.PartidaId, SesionRealtimeMessages.PartidaIniciada, new PartidaIniciadaPayload(evento.PartidaId), cancellationToken);

    public Task PublicarJuegoActivadoAsync(JuegoActivadoEvent evento, CancellationToken cancellationToken) =>
        Difundir(evento.PartidaId, SesionRealtimeMessages.JuegoActivado,
            new JuegoActivadoPayload(evento.PartidaId, evento.JuegoId, evento.Orden, evento.TipoJuego), cancellationToken);

    public Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent evento, CancellationToken cancellationToken) =>
        Difundir(evento.PartidaId, SesionRealtimeMessages.PartidaCancelada,
            new PartidaCanceladaPayload(evento.PartidaId, evento.Motivo), cancellationToken);

    public Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent evento, CancellationToken cancellationToken) =>
        Difundir(evento.PartidaId, SesionRealtimeMessages.PartidaFinalizada,
            new PartidaFinalizadaPayload(evento.PartidaId), cancellationToken);

    public Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent evento, CancellationToken cancellationToken) =>
        Difundir(evento.PartidaId, SesionRealtimeMessages.PreguntaActivada,
            new PreguntaActivadaPayload(
                evento.PartidaId,
                evento.JuegoId,
                evento.PreguntaId,
                evento.Orden,
                evento.FechaActivacion.AddSeconds(evento.TiempoLimiteSegundos)),
            cancellationToken);

    public Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent evento, CancellationToken cancellationToken) =>
        Difundir(evento.PartidaId, SesionRealtimeMessages.PreguntaCerrada,
            new PreguntaCerradaPayload(evento.PartidaId, evento.JuegoId, evento.PreguntaId), cancellationToken);

    public Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent evento, CancellationToken cancellationToken) =>
        Difundir(evento.PartidaId, SesionRealtimeMessages.EtapaActivada,
            new EtapaActivadaPayload(
                evento.PartidaId,
                evento.JuegoId,
                evento.EtapaId,
                evento.Orden,
                evento.FechaActivacion.AddSeconds(evento.TiempoLimiteSegundos)),
            cancellationToken);

    public Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent evento, CancellationToken cancellationToken) =>
        Difundir(evento.PartidaId, SesionRealtimeMessages.EtapaCerrada,
            new EtapaCerradaPayload(evento.PartidaId, evento.JuegoId, evento.EtapaId), cancellationToken);

    public Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent evento, CancellationToken cancellationToken) =>
        Difundir(evento.PartidaId, SesionRealtimeMessages.EtapaGanada,
            new EtapaGanadaPayload(evento.PartidaId, evento.JuegoId, evento.EtapaId), cancellationToken);

    // No difunden (per-participante / scoring-adjacentes → SP-4). Documentado en diseño SP-3f-2.
    public Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent evento, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent evento, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent evento, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken) =>
        _hub.Clients.Group(evento.EquipoDestinoId is { } equipo
                ? SesionRealtimeMessages.GrupoEquipo(equipo)
                : SesionRealtimeMessages.GrupoParticipante(evento.ParticipanteDestinoId!.Value))
            .SendAsync(
                SesionRealtimeMessages.PistaEnviada,
                new PistaEnviadaPayload(evento.PartidaId, evento.JuegoId, evento.ParticipanteDestinoId, evento.Texto, evento.Instante, evento.EquipoDestinoId),
                cancellationToken);

    public Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken) =>
        _hub.Clients.Group(SesionRealtimeMessages.GrupoParticipante(evento.UsuarioId))
            .SendAsync(
                SesionRealtimeMessages.ConvocatoriaCreada,
                new ConvocatoriaCreadaPayload(evento.PartidaId, evento.EquipoId, evento.ConvocatoriaId, evento.UsuarioId),
                cancellationToken);

    public Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    // No difunde: el relay vivo al grupo operador lo hace SesionHub.EnviarUbicacion directamente (BR-B07).
    public Task PublicarUbicacionActualizadaAsync(UbicacionActualizadaEvent evento, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    // Sin payload realtime documentado (feed el guard de equipos en Identity vía RabbitMQ).
    public Task PublicarInscripcionEquipoCreadaAsync(InscripcionEquipoCreadaEvent evento, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PublicarInscripcionEquipoCanceladaAsync(InscripcionEquipoCanceladaEvent evento, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
