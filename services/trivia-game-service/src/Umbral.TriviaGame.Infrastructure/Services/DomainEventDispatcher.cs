using Microsoft.Extensions.Logging;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Common;

namespace Umbral.TriviaGame.Infrastructure.Services;

internal sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(ILogger<DomainEventDispatcher> logger)
    {
        _logger = logger;
    }

    public Task DispatchAsync(IReadOnlyList<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            _logger.LogInformation("Evento de dominio despachado: {EventType} - Ocurrido en {OccurredAt}",
                domainEvent.GetType().Name,
                domainEvent.OccurredAtUtc);
        }

        return Task.CompletedTask;
    }
}
