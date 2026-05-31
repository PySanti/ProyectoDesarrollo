namespace Umbral.TriviaGame.Domain.Common;

/// <summary>
/// Clase base abstracta para todos los eventos de dominio in-process.
/// Permanece pura en la capa Domain sin depender de MediatR ni infraestructura.
/// La capa Application será responsable de despachar estos eventos a través
/// de MediatR (INotification) después de persistir el aggregate.
/// </summary>
public abstract record DomainEvent
{
    /// <summary>
    /// Timestamp UTC de cuándo ocurrió el evento.
    /// Se asigna en el constructor para preservar el momento exacto.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; init; }

    protected DomainEvent()
    {
        OccurredAtUtc = DateTimeOffset.UtcNow;
    }

    protected DomainEvent(DateTimeOffset occurredAtUtc)
    {
        OccurredAtUtc = occurredAtUtc;
    }
}
