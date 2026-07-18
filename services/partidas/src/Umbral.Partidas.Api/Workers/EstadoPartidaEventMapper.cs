using System.Text.Json;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.Api.Workers;

// Traduce los eventos de ciclo de vida que publica Operaciones de Sesión al comando de proyección
// de estado. Devuelve null para cualquier otro evento (el worker lo descarta con ack). Solo se
// necesita el partidaId del payload; el estado lo determina el tipo de evento.
public static class EstadoPartidaEventMapper
{
    public static ProyectarEstadoPartidaCommand? Map(EnvelopeResumen envelope)
    {
        var estado = envelope.EventType switch
        {
            "PartidaPublicadaEnLobby" => EstadoPartida.Lobby,
            "PartidaIniciada" => EstadoPartida.Iniciada,
            "PartidaCancelada" => EstadoPartida.Cancelada,
            "PartidaFinalizada" => EstadoPartida.Terminada,
            _ => (EstadoPartida?)null
        };
        if (estado is null)
            return null;

        if (!envelope.Payload.TryGetProperty("partidaId", out var pid) || !pid.TryGetGuid(out var partidaId))
            return null;

        return new ProyectarEstadoPartidaCommand(partidaId, estado.Value);
    }
}
