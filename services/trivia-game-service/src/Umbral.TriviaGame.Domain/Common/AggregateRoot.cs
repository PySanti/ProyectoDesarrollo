using System.Collections.Immutable;

namespace Umbral.TriviaGame.Domain.Common;

/// <summary>
/// Clase base abstracta para todas las raíces de agregado del dominio.
/// Extiende Entity[TId] y sirve como marcador semántico: solo los aggregate roots
/// pueden tener repositorios directos, garantizan la consistencia de sus entidades hijas
/// y son los únicos autorizados a publicar eventos de dominio in-process.
/// </summary>
/// <typeparam name="TId">Tipo del identificador del aggregate root.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// Colección de eventos de dominio in-process que aún no han sido despachados.
    /// Los repositorios o la capa de aplicación deben invocar GetDomainEvents()
    /// después de persistir y luego ClearDomainEvents().
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents =>
        _domainEvents.ToImmutableList();

    /// <summary>
    /// Constructor protegido: solo las raíces de agregado concretas pueden instanciar.
    /// Delega la validación del identificador a Entity[TId].
    /// </summary>
    /// <param name="id">Identificador único ya validado por el value object correspondiente.</param>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>
    /// Registra un evento de dominio in-process en la lista interna.
    /// Los eventos se despachan típicamente después de guardar el aggregate.
    /// </summary>
    /// <param name="domainEvent">Evento de dominio a registrar.</param>
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Retorna y limpia la lista de eventos de dominio registrados.
    /// Útil para que el repositorio o el pipeline los publique después del commit.
    /// </summary>
    /// <returns>Lista inmutable de eventos pendientes.</returns>
    public IReadOnlyList<DomainEvent> FlushDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}
