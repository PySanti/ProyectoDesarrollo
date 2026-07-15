using System.Text.Json;
using Umbral.Puntuaciones.Application.Commands;

namespace Umbral.Puntuaciones.Api.Workers;

// Traduce cualquier evento del contrato al comando genérico de historial (SP-4d).
// Extracción declarativa por tipo: autor real y equipo acreditado/destino según el payload
// documentado; el resto del payload (sin partidaId/sesionPartidaId/juegoId ni los ids extraídos)
// queda resumido en DetalleJson. Tipo desconocido o partidaId inválido → null (warn + ack).
public static class HistorialEventMapper
{
    private sealed record Extraccion(string? ParticipanteProp, string? EquipoProp);

    private static readonly IReadOnlyDictionary<string, Extraccion> Tipos = new Dictionary<string, Extraccion>
    {
        ["PartidaPublicadaEnLobby"] = new(null, null),
        ["PartidaIniciada"] = new(null, null),
        ["JuegoActivado"] = new(null, null),
        ["PartidaCancelada"] = new(null, null),
        ["PartidaFinalizada"] = new(null, null),
        ["RespuestaTriviaValidada"] = new("participanteId", "equipoId"),
        ["PuntajeTriviaIncrementado"] = new("participanteId", "equipoId"),
        ["PreguntaTriviaActivada"] = new(null, null),
        ["PreguntaTriviaCerrada"] = new("ganadorParticipanteId", "ganadorEquipoId"),
        ["TesoroQRValidado"] = new("participanteId", "equipoId"),
        ["EtapaBDTGanada"] = new("participanteId", "equipoId"),
        ["EtapaBDTCerrada"] = new("ganadorParticipanteId", "ganadorEquipoId"),
        ["EtapaBDTActivada"] = new(null, null),
        ["PistaEnviada"] = new("participanteDestinoId", "equipoDestinoId"),
        ["ConvocatoriaCreada"] = new("usuarioId", "equipoId"),
        ["ConvocatoriaRespondida"] = new("usuarioId", null),
        ["UbicacionActualizada"] = new("participanteId", null),
        ["InscripcionSolicitada"] = new("participanteId", "equipoId"),
        ["InscripcionAceptada"] = new("participanteId", "equipoId"),
        ["InscripcionRechazada"] = new("participanteId", "equipoId"),
        ["InscripcionEquipoCreada"] = new(null, "equipoId"),
        ["InscripcionEquipoCancelada"] = new(null, "equipoId"),
    };

    public static ProyectarEventoHistorialCommand? Map(EnvelopeResumen envelope)
    {
        if (!Tipos.TryGetValue(envelope.EventType, out var extraccion))
        {
            return null;
        }

        var payload = envelope.Payload;
        if (GetGuidOpcional(payload, "partidaId") is not { } partidaId)
        {
            return null;
        }

        var participanteProp = extraccion.ParticipanteProp;
        var equipoProp = extraccion.EquipoProp;
        var excluidas = new HashSet<string> { "partidaId", "sesionPartidaId", "juegoId" };
        if (participanteProp is not null)
        {
            excluidas.Add(participanteProp);
        }
        if (equipoProp is not null)
        {
            excluidas.Add(equipoProp);
        }

        var detalle = new Dictionary<string, JsonElement>();
        foreach (var prop in payload.EnumerateObject())
        {
            if (!excluidas.Contains(prop.Name))
            {
                detalle[prop.Name] = prop.Value.Clone();
            }
        }

        return new ProyectarEventoHistorialCommand(
            envelope.EventId,
            envelope.EventType,
            envelope.OccurredAt,
            partidaId,
            GetGuidOpcional(payload, "juegoId"),
            participanteProp is null ? null : GetGuidOpcional(payload, participanteProp),
            equipoProp is null ? null : GetGuidOpcional(payload, equipoProp),
            JsonSerializer.Serialize(detalle));
    }

    private static Guid? GetGuidOpcional(JsonElement payload, string nombre)
        => payload.TryGetProperty(nombre, out var prop)
            && prop.ValueKind == JsonValueKind.String
            && prop.TryGetGuid(out var valor)
                ? valor
                : null;
}
