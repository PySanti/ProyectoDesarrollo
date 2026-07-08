namespace Umbral.Puntuaciones.Domain.Entities;

// Registro de auditoría/historial de partida (RB-15, RB-31): una fila por evento de dominio
// recibido de Operaciones de Sesión. La propia fila es el registro de dedup del historial
// (índice único por EventId); jamás se purga.
public sealed class EventoHistorial
{
    private EventoHistorial(
        Guid eventId, Guid partidaId, Guid? juegoId, string tipoEvento,
        DateTime occurredAt, Guid? participanteId, Guid? equipoId, string detalleJson)
    {
        EventId = eventId;
        PartidaId = partidaId;
        JuegoId = juegoId;
        TipoEvento = tipoEvento;
        OccurredAt = occurredAt;
        ParticipanteId = participanteId;
        EquipoId = equipoId;
        DetalleJson = detalleJson;
    }

    public long Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid PartidaId { get; private set; }
    public Guid? JuegoId { get; private set; }
    public string TipoEvento { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public Guid? ParticipanteId { get; private set; }
    public Guid? EquipoId { get; private set; }
    public string DetalleJson { get; private set; }

    public static EventoHistorial Registrar(
        Guid eventId, Guid partidaId, Guid? juegoId, string tipoEvento,
        DateTime occurredAt, Guid? participanteId, Guid? equipoId, string detalleJson)
        => new(eventId, partidaId, juegoId, tipoEvento, occurredAt, participanteId, equipoId, detalleJson);
}
