using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Application.Ports;

/// <summary>
/// Puerto para despachar eventos de dominio después de persistir un aggregate.
/// La implementación concreta envuelve cada evento en un INotification de MediatR y lo publica.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<DomainEvent> events, CancellationToken cancellationToken = default);
}
