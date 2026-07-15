using Microsoft.Extensions.Logging;
using Umbral.IdentityService.Application.Interfaces;

namespace Umbral.IdentityService.Infrastructure.Services.Events;

public sealed class CompositeIdentityEventsPublisher : IIdentityEventsPublisher
{
    private readonly IReadOnlyList<IIdentityEventsPublisher> _publishers;
    private readonly ILogger<CompositeIdentityEventsPublisher> _logger;

    public CompositeIdentityEventsPublisher(
        IEnumerable<IIdentityEventsPublisher> publishers,
        ILogger<CompositeIdentityEventsPublisher> logger)
    {
        _publishers = publishers.ToList();
        _logger = logger;
    }

    private async Task FanOut(Func<IIdentityEventsPublisher, Task> call)
    {
        foreach (var p in _publishers)
        {
            try
            {
                await call(p);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Publicador {Publicador} falló al emitir evento de identity", p.GetType().Name);
            }
        }
    }

    public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent e, CancellationToken ct) => FanOut(p => p.PublishEquipoCreadoAsync(e, ct));
    public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent e, CancellationToken ct) => FanOut(p => p.PublishInvitacionEquipoCreadaAsync(e, ct));
    public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent e, CancellationToken ct) => FanOut(p => p.PublishInvitacionEquipoAceptadaAsync(e, ct));
    public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent e, CancellationToken ct) => FanOut(p => p.PublishInvitacionEquipoRechazadaAsync(e, ct));
    public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent e, CancellationToken ct) => FanOut(p => p.PublishRolUsuarioModificadoAsync(e, ct));
    public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent e, CancellationToken ct) => FanOut(p => p.PublishPermisosRolActualizadosAsync(e, ct));
    public Task PublishEquipoEliminadoAsync(EquipoEliminadoIntegrationEvent e, CancellationToken ct) => FanOut(p => p.PublishEquipoEliminadoAsync(e, ct));
    public Task PublishLiderazgoEquipoModificadoAsync(LiderazgoEquipoModificadoIntegrationEvent e, CancellationToken ct) => FanOut(p => p.PublishLiderazgoEquipoModificadoAsync(e, ct));
    public Task PublishEquipoDesactivadoAsync(EquipoDesactivadoIntegrationEvent e, CancellationToken ct) => FanOut(p => p.PublishEquipoDesactivadoAsync(e, ct));
    public Task PublishEquipoReactivadoAsync(EquipoReactivadoIntegrationEvent e, CancellationToken ct) => FanOut(p => p.PublishEquipoReactivadoAsync(e, ct));
    public Task PublishCredencialTemporalEmitidaAsync(CredencialTemporalEmitidaIntegrationEvent e, CancellationToken ct) => FanOut(p => p.PublishCredencialTemporalEmitidaAsync(e, ct));
}
