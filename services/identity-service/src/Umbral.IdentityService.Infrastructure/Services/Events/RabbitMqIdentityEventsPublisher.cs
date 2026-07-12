using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Infrastructure.Services.Messaging;

namespace Umbral.IdentityService.Infrastructure.Services.Events;

public sealed class RabbitMqIdentityEventsPublisher : IIdentityEventsPublisher
{
    private readonly IRabbitMqPublishChannel _canal;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RabbitMqIdentityEventsPublisher> _logger;

    public RabbitMqIdentityEventsPublisher(IRabbitMqPublishChannel canal, TimeProvider timeProvider,
        ILogger<RabbitMqIdentityEventsPublisher> logger)
    {
        _canal = canal;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // Best-effort estricto: fallo de broker se loguea y NUNCA llega al caller (ADR-0012).
    private Task Publicar(string eventType, object payload)
    {
        try
        {
            var envelope = EventEnvelope.Create(eventType, payload, _timeProvider.GetUtcNow().UtcDateTime);
            var body = JsonSerializer.SerializeToUtf8Bytes(envelope, EventEnvelope.SerializerOptions);
            _canal.Publish(IdentityEventRouting.RoutingKeyFor(eventType), body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo publicando {EventType} a RabbitMQ (best-effort, se continúa)", eventType);
        }
        return Task.CompletedTask;
    }

    public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent e, CancellationToken ct) => Publicar("EquipoCreado", e);
    public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent e, CancellationToken ct) => Publicar("InvitacionEquipoCreada", e);
    public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent e, CancellationToken ct) => Publicar("InvitacionEquipoAceptada", e);
    public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent e, CancellationToken ct) => Publicar("InvitacionEquipoRechazada", e);
    public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent e, CancellationToken ct) => Publicar("RolUsuarioModificado", e);
    public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent e, CancellationToken ct) => Publicar("PermisosRolActualizados", e);
    public Task PublishEquipoEliminadoAsync(EquipoEliminadoIntegrationEvent e, CancellationToken ct) => Publicar("EquipoEliminado", e);
    public Task PublishLiderazgoEquipoModificadoAsync(LiderazgoEquipoModificadoIntegrationEvent e, CancellationToken ct) => Publicar("LiderazgoEquipoModificado", e);
    public Task PublishEquipoDesactivadoAsync(EquipoDesactivadoIntegrationEvent e, CancellationToken ct) => Publicar("EquipoDesactivado", e);
    public Task PublishEquipoReactivadoAsync(EquipoReactivadoIntegrationEvent e, CancellationToken ct) => Publicar("EquipoReactivado", e);
}
