using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Api.Realtime;

[Authorize]
public sealed class SesionHub : Hub
{
    private const string ClavePartidaId = "partidaId";
    private const string ClaveParticipanteId = "participanteId";
    private const string ClaveEquipoId = "equipoId";

    private readonly ISesionPartidaRepository _repo;
    private readonly TimeProvider _timeProvider;
    private readonly ISesionEventsPublisher _events;
    private readonly ISender _sender;
    private readonly ILogger<SesionHub> _logger;

    public SesionHub(ISesionPartidaRepository repo, TimeProvider timeProvider, ISesionEventsPublisher events,
        ISender sender, ILogger<SesionHub> logger)
    {
        _repo = repo;
        _timeProvider = timeProvider;
        _events = events;
        _sender = sender;
        _logger = logger;
    }

    // Re-push de cortesía (SP-3i): el convocado offline recibe sus convocatorias pendientes al volver.
    // Datos → MediatR (ADR-0011 reserva el repositorio del hub para membresía de grupos).
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        var sub = user?.FindFirst("sub")?.Value ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!(user?.IsInRole("Operador") ?? false) && sub is not null && Guid.TryParse(sub, out var usuarioId))
        {
            // Canal personal por identidad, no por partida: lo que hay que notificarle al
            // participante (aceptacion, rechazo, convocatoria) ocurre ANTES de que tenga
            // participacion, que es lo que SuscribirAPartida exige.
            await Groups.AddToGroupAsync(
                Context.ConnectionId, SesionRealtimeMessages.GrupoParticipante(usuarioId), Context.ConnectionAborted);

            try
            {
                var pendientes = await _sender.Send(new ObtenerMisConvocatoriasPendientesQuery(usuarioId), Context.ConnectionAborted);
                foreach (var c in pendientes)
                {
                    await Clients.Caller.SendAsync(SesionRealtimeMessages.ConvocatoriaCreada,
                        new ConvocatoriaCreadaPayload(c.PartidaId, c.EquipoId, c.ConvocatoriaId, usuarioId),
                        Context.ConnectionAborted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo el re-push de convocatorias pendientes para {UsuarioId}; la conexión continúa", usuarioId);
            }
        }
        await base.OnConnectedAsync();
    }

    public async Task SuscribirAPartida(Guid partidaId)
    {
        var user = Context.User;
        var esOperador = user?.IsInRole("Operador") ?? false;
        if (esOperador)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
            await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoOperadorPartida(partidaId), Context.ConnectionAborted);
            return;
        }

        var sub = user?.FindFirst("sub")?.Value ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub is null || !Guid.TryParse(sub, out var participanteId))
        {
            throw new HubException("Participante no identificado.");
        }

        var sesion = await _repo.GetByParticipanteActivoAsync(participanteId, Context.ConnectionAborted);
        if (sesion is null || sesion.PartidaId != partidaId)
        {
            throw new HubException("No inscrito en la partida.");
        }

        Context.Items[ClavePartidaId] = partidaId;
        Context.Items[ClaveParticipanteId] = participanteId;
        var inscripcionEquipo = sesion.Inscripciones.FirstOrDefault(i => i.EsActiva
            && i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.EstaAceptada));
        await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoParticipante(participanteId), Context.ConnectionAborted);
        if (inscripcionEquipo?.EquipoId is { } equipoId)
        {
            Context.Items[ClaveEquipoId] = equipoId;
            await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoEquipo(equipoId), Context.ConnectionAborted);
        }
    }

    public async Task DesuscribirDePartida(Guid partidaId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoOperadorPartida(partidaId), Context.ConnectionAborted);
        // El canal personal NO se toca aqui: es de la identidad, no de la partida. Salir de una
        // partida no puede dejarte sordo a tus convocatorias.
        if (Context.Items.TryGetValue(ClaveEquipoId, out var e) && e is Guid equipoId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoEquipo(equipoId), Context.ConnectionAborted);
        }
    }

    public async Task EnviarUbicacion(double latitud, double longitud)
    {
        if (!TryObtenerSuscripcion(out var partidaId, out var participanteId))
        {
            throw new HubException("Suscríbete a la partida antes de enviar ubicación.");
        }

        if (latitud is < -90 or > 90 || longitud is < -180 or > 180)
        {
            throw new HubException("Coordenadas fuera de rango.");
        }

        var payload = new UbicacionParticipantePayload(
            partidaId, participanteId, latitud, longitud, _timeProvider.GetUtcNow().UtcDateTime);

        await Clients.Group(SesionRealtimeMessages.GrupoOperadorPartida(partidaId))
            .SendAsync(SesionRealtimeMessages.UbicacionActualizada, payload, Context.ConnectionAborted);

        await _events.PublicarUbicacionActualizadaAsync(
            new UbicacionActualizadaEvent(partidaId, participanteId, latitud, longitud, payload.TimestampUtc),
            Context.ConnectionAborted);
    }

    private bool TryObtenerSuscripcion(out Guid partidaId, out Guid participanteId)
    {
        partidaId = default;
        participanteId = default;
        if (Context.Items.TryGetValue(ClavePartidaId, out var p) && p is Guid pid &&
            Context.Items.TryGetValue(ClaveParticipanteId, out var u) && u is Guid uid)
        {
            partidaId = pid;
            participanteId = uid;
            return true;
        }
        return false;
    }
}
