namespace Umbral.Puntuaciones.Domain.Entities;

// Dedup por eventId exigido por el contrato de transporte (SP-3i).
public sealed class EventoProcesado
{
    private EventoProcesado(Guid eventId, string eventType, DateTime occurredAt, DateTime procesadoAt)
    {
        EventId = eventId;
        EventType = eventType;
        OccurredAt = occurredAt;
        ProcesadoAt = procesadoAt;
    }

    public Guid EventId { get; private set; }
    public string EventType { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime ProcesadoAt { get; private set; }

    public static EventoProcesado Registrar(Guid eventId, string eventType, DateTime occurredAt, DateTime procesadoAt)
        => new(eventId, eventType, occurredAt, procesadoAt);
}
