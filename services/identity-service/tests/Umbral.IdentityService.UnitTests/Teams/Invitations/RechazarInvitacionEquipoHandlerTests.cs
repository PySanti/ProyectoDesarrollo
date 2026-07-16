using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

using Umbral.IdentityService.Application.Handlers.Commands;
namespace Umbral.IdentityService.UnitTests.Teams.Invitations;

public sealed class RechazarInvitacionEquipoHandlerTests
{
    [Fact]
    public async Task Rechazar_Throws_InvitacionNoEncontrada_When_Invite_Not_Found()
    {
        var invRepo = new FakeInvitacionEquipoRepository { InvitacionToReturn = null };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new RechazarInvitacionEquipoCommandHandler(invRepo, publisher);

        await Assert.ThrowsAsync<InvitacionNoEncontradaException>(() =>
            handler.Handle(new RechazarInvitacionEquipoCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Rechazar_Throws_InvitacionNoEncontrada_When_Actor_Is_Not_Invitee()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, invitado, lider);

        var invRepo = new FakeInvitacionEquipoRepository { InvitacionToReturn = invitacion };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new RechazarInvitacionEquipoCommandHandler(invRepo, publisher);

        await Assert.ThrowsAsync<InvitacionNoEncontradaException>(() =>
            handler.Handle(new RechazarInvitacionEquipoCommand(Guid.NewGuid(), invitacion.InvitacionEquipoId), CancellationToken.None));
    }

    [Fact]
    public async Task Rechazar_Throws_InvitacionNoEncontrada_When_Invitation_Not_Pending()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, invitado, lider);
        // Force non-pending state
        invitacion.Rechazar();

        var invRepo = new FakeInvitacionEquipoRepository { InvitacionToReturn = invitacion };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new RechazarInvitacionEquipoCommandHandler(invRepo, publisher);

        await Assert.ThrowsAsync<InvitacionNoEncontradaException>(() =>
            handler.Handle(new RechazarInvitacionEquipoCommand(invitado, invitacion.InvitacionEquipoId), CancellationToken.None));
    }

    [Fact]
    public async Task Rechazar_Marks_Rechazada_And_Publishes_Event_When_Valid()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, invitado, lider);

        var invRepo = new FakeInvitacionEquipoRepository { InvitacionToReturn = invitacion };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new RechazarInvitacionEquipoCommandHandler(invRepo, publisher);

        var response = await handler.Handle(new RechazarInvitacionEquipoCommand(invitado, invitacion.InvitacionEquipoId), CancellationToken.None);

        Assert.Equal("Rechazada", response.EstadoInvitacion);
        Assert.Equal(invitado, response.InvitadoUserId);
        Assert.Equal(EstadoInvitacion.Rechazada, invitacion.Estado);
        Assert.True(invRepo.UpdateWasCalled);
        Assert.True(publisher.InvitacionRechazadaWasCalled);
    }

    // Fakes

    private sealed class FakeInvitacionEquipoRepository : IInvitacionEquipoRepository
    {
        public Task<IReadOnlyCollection<Guid>> GetInvitadoUserIdsPendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());

        public InvitacionEquipo? InvitacionToReturn { get; set; }
        public bool UpdateWasCalled { get; private set; }

        public Task AddAsync(InvitacionEquipo invitacion, CancellationToken ct)
            => Task.CompletedTask;

        public Task UpdateAsync(InvitacionEquipo invitacion, CancellationToken ct)
        {
            UpdateWasCalled = true;
            return Task.CompletedTask;
        }

        public Task<InvitacionEquipo?> GetByIdAsync(Guid invitacionId, CancellationToken ct)
            => Task.FromResult(InvitacionToReturn);

        public Task<IReadOnlyList<InvitacionEquipo>> GetPendientesByInvitadoAsync(Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<InvitacionEquipo>>(new List<InvitacionEquipo>());

        public Task<bool> ExistsPendienteAsync(Guid equipoId, Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult(false);

        public Task DeletePendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class FakeEquipoEventsPublisher : IIdentityEventsPublisher
    {
        public bool InvitacionRechazadaWasCalled { get; private set; }

        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            InvitacionRechazadaWasCalled = true;
            return Task.CompletedTask;
        }

        public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishEquipoEliminadoAsync(EquipoEliminadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishLiderazgoEquipoModificadoAsync(LiderazgoEquipoModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishEquipoDesactivadoAsync(EquipoDesactivadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishEquipoReactivadoAsync(EquipoReactivadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishCredencialTemporalEmitidaAsync(CredencialTemporalEmitidaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
