using System;
using System.Collections.Generic;
using System.Linq;
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

    private Task DifundirAPersonales(IReadOnlyList<Guid> destinatarios, string mensaje, object payload, CancellationToken ct)
    {
        if (destinatarios.Count == 0) return Task.CompletedTask;
        var grupos = destinatarios.Select(SesionRealtimeMessages.GrupoParticipante).ToList();
        return _hub.Clients.Groups(grupos).SendAsync(mensaje, payload, ct);
    }

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
            new PreguntaCerradaPayload(evento.PartidaId, evento.JuegoId, evento.PreguntaId,
                evento.OpcionCorrectaId, evento.TextoOpcionCorrecta,
                evento.GanadorParticipanteId, evento.GanadorEquipoId), cancellationToken);

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
            new EtapaCerradaPayload(evento.PartidaId, evento.JuegoId, evento.EtapaId,
                evento.GanadorParticipanteId, evento.GanadorEquipoId), cancellationToken);

    public Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent evento, CancellationToken cancellationToken) =>
        Difundir(evento.PartidaId, SesionRealtimeMessages.EtapaGanada,
            new EtapaGanadaPayload(evento.PartidaId, evento.JuegoId, evento.EtapaId,
                evento.ParticipanteId, evento.EquipoId), cancellationToken);

    // En Equipo la primera respuesta de cualquier miembro sella al equipo entero (acierte o falle),
    // así que el resto del equipo debe ver el mismo resultado en el acto. Se difunde SOLO al grupo
    // del equipo: en Individual la respuesta sigue siendo privada (sin difusión, como en SP-3f-2).
    public Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent evento, CancellationToken cancellationToken) =>
        evento.EquipoId is { } equipoId
            ? _hub.Clients.Group(SesionRealtimeMessages.GrupoEquipo(equipoId))
                .SendAsync(
                    SesionRealtimeMessages.RespuestaEquipoRegistrada,
                    new RespuestaEquipoRegistradaPayload(evento.PartidaId, evento.JuegoId, evento.PreguntaId,
                        evento.EsCorrecta, evento.ParticipanteId),
                    cancellationToken)
            : Task.CompletedTask;

    // No difunden (per-participante / scoring-adjacentes → SP-4). Documentado en diseño SP-3f-2.

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

    // InscripcionSolicitada no difunde: el lobby del operador se refresca por polling (SP-3f-2).
    // Aceptada/Rechazada SI difunden desde este slice: el participante las espera en vivo, y la
    // decisión original de no difundir se tomó mirando solo al operador.
    public Task PublicarInscripcionSolicitadaAsync(InscripcionSolicitadaEvent evento, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PublicarInscripcionAceptadaAsync(
        InscripcionAceptadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken) =>
        DifundirAPersonales(destinatarios, SesionRealtimeMessages.InscripcionResuelta,
            new InscripcionResueltaPayload(evento.PartidaId, evento.InscripcionId, evento.Modalidad, true),
            cancellationToken);

    public Task PublicarInscripcionRechazadaAsync(
        InscripcionRechazadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken) =>
        DifundirAPersonales(destinatarios, SesionRealtimeMessages.InscripcionResuelta,
            new InscripcionResueltaPayload(evento.PartidaId, evento.InscripcionId, evento.Modalidad, false),
            cancellationToken);
}
