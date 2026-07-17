using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Handlers.Commands;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class EliminarMiEquipoHandlerTests
{
    private static EliminarMiEquipoCommandHandler CreateHandler(
        FakeEquipoRepository equipoRepo,
        FakeInvitacionEquipoRepository? invitacionRepo = null,
        FakeParticipacionActivaEquipoRepository? participacionRepo = null,
        FakeIdentityEventsPublisher? publisher = null)
        => new EliminarMiEquipoCommandHandler(
            equipoRepo,
            invitacionRepo ?? new FakeInvitacionEquipoRepository(),
            participacionRepo ?? new FakeParticipacionActivaEquipoRepository(),
            publisher ?? new FakeIdentityEventsPublisher());

    [Fact]
    public async Task Elimina_borra_invitaciones_y_publica_evento_con_los_miembros_a_notificar()
    {
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(miembro);
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var invitacionRepo = new FakeInvitacionEquipoRepository();
        var participacionRepo = new FakeParticipacionActivaEquipoRepository { ExistsByEquipoValue = false };
        var publisher = new FakeIdentityEventsPublisher();
        var handler = CreateHandler(equipoRepo, invitacionRepo, participacionRepo, publisher);

        var response = await handler.Handle(new EliminarMiEquipoCommand(lider), CancellationToken.None);

        Assert.Equal(equipo.EquipoId, response.EquipoId);
        Assert.Equal(EstadoEquipo.Eliminado.ToString(), response.Estado);
        Assert.Equal(EstadoEquipo.Eliminado, equipo.Estado);
        Assert.True(equipoRepo.UpdateWasCalled);
        Assert.True(invitacionRepo.DeletePendientesByEquipoWasCalled);

        // El evento es lo que dispara el correo (lo consume CredencialesTemporalesConsumer): debe
        // llevar el nombre del equipo y todos los integrantes a notificar.
        Assert.NotNull(publisher.PublishedEvent);
        Assert.Equal(equipo.EquipoId, publisher.PublishedEvent!.EquipoId);
        Assert.Equal("Equipo A", publisher.PublishedEvent.NombreEquipo);
        Assert.Equal("Lider", publisher.PublishedEvent.Origen);
        Assert.Equal(2, publisher.PublishedEvent.Miembros.Count);
        Assert.Contains(lider, publisher.PublishedEvent.Miembros);
        Assert.Contains(miembro, publisher.PublishedEvent.Miembros);
    }

    [Fact]
    public async Task Con_participacion_activa_lanza_EquipoConParticipacionActiva()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var invitacionRepo = new FakeInvitacionEquipoRepository();
        var participacionRepo = new FakeParticipacionActivaEquipoRepository { ExistsByEquipoValue = true };
        var publisher = new FakeIdentityEventsPublisher();
        var handler = CreateHandler(equipoRepo, invitacionRepo, participacionRepo, publisher);

        await Assert.ThrowsAsync<EquipoConParticipacionActivaException>(() =>
            handler.Handle(new EliminarMiEquipoCommand(lider), CancellationToken.None));

        Assert.False(equipoRepo.UpdateWasCalled);
        Assert.False(invitacionRepo.DeletePendientesByEquipoWasCalled);
        Assert.Null(publisher.PublishedEvent);
        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
    }

    [Fact]
    public async Task Actor_no_lider_lanza_NoEsLiderException()
    {
        var lider = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(actor);
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = CreateHandler(equipoRepo);

        await Assert.ThrowsAsync<NoEsLiderException>(() =>
            handler.Handle(new EliminarMiEquipoCommand(actor), CancellationToken.None));

        Assert.False(equipoRepo.UpdateWasCalled);
        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
    }

    [Fact]
    public async Task Sin_equipo_activo_lanza_NoActiveTeamForParticipantException()
    {
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = null };
        var handler = CreateHandler(equipoRepo);

        await Assert.ThrowsAsync<NoActiveTeamForParticipantException>(() =>
            handler.Handle(new EliminarMiEquipoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public Equipo? TeamToReturn { get; set; }
        public bool UpdateWasCalled { get; private set; }

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn is not null);

        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn);

        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn?.EquipoId == equipoId ? TeamToReturn : null);

        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Equipo>>(TeamToReturn is null ? Array.Empty<Equipo>() : new[] { TeamToReturn });

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
        {
            UpdateWasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInvitacionEquipoRepository : IInvitacionEquipoRepository
    {
        public Task<IReadOnlyCollection<Guid>> GetInvitadoUserIdsPendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());

        public bool DeletePendientesByEquipoWasCalled { get; private set; }

        public Task AddAsync(InvitacionEquipo invitacion, CancellationToken ct)
            => Task.CompletedTask;

        public Task UpdateAsync(InvitacionEquipo invitacion, CancellationToken ct)
            => Task.CompletedTask;

        public Task<InvitacionEquipo?> GetByIdAsync(Guid invitacionId, CancellationToken ct)
            => Task.FromResult<InvitacionEquipo?>(null);

        public Task<IReadOnlyList<InvitacionEquipo>> GetPendientesByInvitadoAsync(Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<InvitacionEquipo>>(new List<InvitacionEquipo>());

        public Task<bool> ExistsPendienteAsync(Guid equipoId, Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult(false);

        public Task DeletePendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
        {
            DeletePendientesByEquipoWasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeParticipacionActivaEquipoRepository : IParticipacionActivaEquipoRepository
    {
        public bool ExistsByEquipoValue { get; set; }

        public Task UpsertAsync(Guid equipoId, Guid partidaId, DateTime fechaUtc, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RemoveByPartidaAsync(Guid partidaId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RemoveAsync(Guid equipoId, Guid partidaId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<bool> ExistsByEquipoAsync(Guid equipoId, CancellationToken cancellationToken)
            => Task.FromResult(ExistsByEquipoValue);
    }

    private sealed class FakeIdentityEventsPublisher : IIdentityEventsPublisher
    {
        public EquipoEliminadoIntegrationEvent? PublishedEvent { get; private set; }

        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishEquipoEliminadoAsync(EquipoEliminadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            PublishedEvent = integrationEvent;
            return Task.CompletedTask;
        }

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
