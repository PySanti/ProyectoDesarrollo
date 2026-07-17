using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Api.Workers;

// Traduce el envelope del broker al comando de proyección (SP-4a).
// Devuelve null para eventos sin proyección en este slice o payloads no deserializables (warn + ack en el worker).
public static class ProyeccionEventMapper
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static IBaseRequest? Map(EnvelopeResumen envelope)
    {
        try
        {
            return envelope.EventType switch
            {
                "PartidaPublicadaEnLobby" => MapPartidaPublicada(envelope),
                "PartidaIniciada" => MapPartidaIniciada(envelope),
                "JuegoActivado" => MapJuegoActivado(envelope),
                "PartidaCancelada" => MapPartidaCancelada(envelope),
                "PartidaFinalizada" => MapPartidaFinalizada(envelope),
                "PuntajeTriviaIncrementado" => MapPuntajeTrivia(envelope),
                "EtapaBDTGanada" => MapEtapaBdtGanada(envelope),
                "InscripcionAceptada" => MapInscripcionAceptada(envelope),
                "ConvocatoriaCreada" => MapConvocatoriaCreada(envelope),
                "ConvocatoriaRespondida" => MapConvocatoriaRespondida(envelope),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record PartidaPublicadaPayload(Guid PartidaId, Guid SesionPartidaId, Modalidad Modalidad);
    private sealed record PartidaIniciadaPayload(Guid PartidaId, Guid SesionPartidaId, DateTime FechaInicio);
    private sealed record JuegoActivadoPayload(Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, int Orden, TipoJuego TipoJuego);
    private sealed record PartidaCanceladaPayload(Guid PartidaId, Guid SesionPartidaId, DateTime FechaCancelacion);
    private sealed record PartidaFinalizadaPayload(Guid PartidaId, Guid SesionPartidaId, DateTime FechaFin);
    private sealed record PuntajeTriviaPayload(
        Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
        Guid ParticipanteId, int Puntaje, long TiempoRespuestaMs, Guid? EquipoId);
    private sealed record EtapaBdtGanadaPayload(
        Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
        Guid ParticipanteId, int Puntaje, long TiempoResolucionMs, Guid? EquipoId);

    // Modalidad/EstadoConvocatoria viajan como string, no como enum: un valor inesperado no
    // revienta la deserializacion del envelope, el handler simplemente no proyecta.
    private sealed record InscripcionAceptadaPayload(
        Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, string Modalidad,
        Guid? ParticipanteId, Guid? EquipoId, DateTime Instante);
    private sealed record ConvocatoriaCreadaPayload(
        Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid EquipoId, Guid UsuarioId);
    private sealed record ConvocatoriaRespondidaPayload(
        Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid UsuarioId, string EstadoConvocatoria);

    private static T? Deserializar<T>(EnvelopeResumen envelope) where T : class
        => envelope.Payload.Deserialize<T>(JsonOpts);

    private static IBaseRequest? MapPartidaPublicada(EnvelopeResumen e)
        => Deserializar<PartidaPublicadaPayload>(e) is { } p
            ? new ProyectarPartidaPublicadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.Modalidad)
            : null;

    private static IBaseRequest? MapPartidaIniciada(EnvelopeResumen e)
        => Deserializar<PartidaIniciadaPayload>(e) is { } p
            ? new ProyectarPartidaIniciadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.FechaInicio)
            : null;

    private static IBaseRequest? MapJuegoActivado(EnvelopeResumen e)
        => Deserializar<JuegoActivadoPayload>(e) is { } p
            ? new ProyectarJuegoActivadoCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.JuegoId, p.Orden, p.TipoJuego)
            : null;

    private static IBaseRequest? MapPartidaCancelada(EnvelopeResumen e)
        => Deserializar<PartidaCanceladaPayload>(e) is { } p
            ? new ProyectarPartidaCanceladaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.FechaCancelacion)
            : null;

    private static IBaseRequest? MapPartidaFinalizada(EnvelopeResumen e)
        => Deserializar<PartidaFinalizadaPayload>(e) is { } p
            ? new ProyectarPartidaFinalizadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.FechaFin)
            : null;

    private static IBaseRequest? MapPuntajeTrivia(EnvelopeResumen e)
        => Deserializar<PuntajeTriviaPayload>(e) is { } p
            ? new ProyectarPuntajeTriviaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.JuegoId, p.PreguntaId, p.ParticipanteId, p.Puntaje, p.TiempoRespuestaMs, p.EquipoId)
            : null;

    private static IBaseRequest? MapEtapaBdtGanada(EnvelopeResumen e)
        => Deserializar<EtapaBdtGanadaPayload>(e) is { } p
            ? new ProyectarEtapaBdtGanadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.JuegoId, p.EtapaId, p.ParticipanteId, p.Puntaje, p.TiempoResolucionMs, p.EquipoId)
            : null;

    private static IBaseRequest? MapInscripcionAceptada(EnvelopeResumen e)
        => Deserializar<InscripcionAceptadaPayload>(e) is { } p
            ? new ProyectarInscripcionAceptadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.Modalidad, p.ParticipanteId, p.EquipoId)
            : null;

    private static IBaseRequest? MapConvocatoriaCreada(EnvelopeResumen e)
        => Deserializar<ConvocatoriaCreadaPayload>(e) is { } p
            ? new ProyectarConvocatoriaCreadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.ConvocatoriaId, p.EquipoId, p.UsuarioId)
            : null;

    private static IBaseRequest? MapConvocatoriaRespondida(EnvelopeResumen e)
        => Deserializar<ConvocatoriaRespondidaPayload>(e) is { } p
            ? new ProyectarConvocatoriaRespondidaCommand(e.EventId, e.OccurredAt, p.ConvocatoriaId, p.UsuarioId, p.EstadoConvocatoria)
            : null;
}
