using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Exceptions;

using Umbral.IdentityService.Application.Handlers.Commands;
namespace Umbral.IdentityService.UnitTests.Teams.Invitations;

public sealed class EnviarInvitacionEquipoHandlerTests
{
    [Fact]
    public async Task Enviar_Throws_NoEsLider_When_Actor_Has_No_Active_Team()
    {
        var actorId = Guid.NewGuid();
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = null };
        var invRepo = new FakeInvitacionEquipoRepository();
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new EnviarInvitacionEquipoCommandHandler(equipoRepo, invRepo, publisher);

        await Assert.ThrowsAsync<NoEsLiderException>(() =>
            handler.Handle(new EnviarInvitacionEquipoCommand(actorId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Enviar_Throws_NoEsLider_When_Actor_Is_Not_Leader_Of_Team()
    {
        var lider = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(actor);

        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var invRepo = new FakeInvitacionEquipoRepository();
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new EnviarInvitacionEquipoCommandHandler(equipoRepo, invRepo, publisher);

        await Assert.ThrowsAsync<NoEsLiderException>(() =>
            handler.Handle(new EnviarInvitacionEquipoCommand(actor, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Enviar_Throws_EquipoLleno_When_Team_Has_Five_Members()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());

        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var invRepo = new FakeInvitacionEquipoRepository();
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new EnviarInvitacionEquipoCommandHandler(equipoRepo, invRepo, publisher);

        await Assert.ThrowsAsync<EquipoLlenoException>(() =>
            handler.Handle(new EnviarInvitacionEquipoCommand(lider, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Enviar_Throws_UsuarioYaEnEquipo_When_Invitee_Already_In_Team()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);

        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            ExistsActiveForInvitadoValue = true
        };
        var invRepo = new FakeInvitacionEquipoRepository();
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new EnviarInvitacionEquipoCommandHandler(equipoRepo, invRepo, publisher);

        await Assert.ThrowsAsync<UsuarioYaEnEquipoException>(() =>
            handler.Handle(new EnviarInvitacionEquipoCommand(lider, invitado), CancellationToken.None));
    }

    [Fact]
    public async Task Enviar_Throws_InvitacionPendienteYaExiste_When_Duplicate_Pending_Invite()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);

        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var invRepo = new FakeInvitacionEquipoRepository { ExistsPendienteValue = true };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new EnviarInvitacionEquipoCommandHandler(equipoRepo, invRepo, publisher);

        await Assert.ThrowsAsync<InvitacionPendienteYaExisteException>(() =>
            handler.Handle(new EnviarInvitacionEquipoCommand(lider, invitado), CancellationToken.None));
    }

    [Fact]
    public async Task Enviar_Creates_Invitation_And_Publishes_Event_When_Valid()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);

        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var invRepo = new FakeInvitacionEquipoRepository();
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new EnviarInvitacionEquipoCommandHandler(equipoRepo, invRepo, publisher);

        var response = await handler.Handle(new EnviarInvitacionEquipoCommand(lider, invitado), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.InvitacionEquipoId);
        Assert.Equal(equipo.EquipoId, response.EquipoId);
        Assert.Equal(invitado, response.InvitadoUserId);
        Assert.Equal(lider, response.InvitadoPorUserId);
        Assert.Equal("Pendiente", response.Estado);
        Assert.True(invRepo.AddWasCalled);
        Assert.True(publisher.InvitacionCreadaWasCalled);
    }

    // Fakes

    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public Equipo? TeamToReturn { get; set; }
        public bool ExistsActiveForInvitadoValue { get; set; }

        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Equipo>>(Array.Empty<Equipo>());

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            // Return true for invitado checks (not for the actor leader)
            if (TeamToReturn is not null && TeamToReturn.Participantes.Any(p => p.UsuarioId == userId))
                return Task.FromResult(false);
            return Task.FromResult(ExistsActiveForInvitadoValue);
        }

        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn);

        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn?.EquipoId == equipoId ? TeamToReturn : null);

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeInvitacionEquipoRepository : IInvitacionEquipoRepository
    {
        public bool AddWasCalled { get; private set; }
        public bool ExistsPendienteValue { get; set; }

        public Task AddAsync(InvitacionEquipo invitacion, CancellationToken ct)
        {
            AddWasCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(InvitacionEquipo invitacion, CancellationToken ct)
            => Task.CompletedTask;

        public Task<InvitacionEquipo?> GetByIdAsync(Guid invitacionId, CancellationToken ct)
            => Task.FromResult<InvitacionEquipo?>(null);

        public Task<IReadOnlyList<InvitacionEquipo>> GetPendientesByInvitadoAsync(Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<InvitacionEquipo>>(new List<InvitacionEquipo>());

        public Task<bool> ExistsPendienteAsync(Guid equipoId, Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult(ExistsPendienteValue);

        public Task DeletePendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class FakeEquipoEventsPublisher : IIdentityEventsPublisher
    {
        public bool InvitacionCreadaWasCalled { get; private set; }

        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            InvitacionCreadaWasCalled = true;
            return Task.CompletedTask;
        }

        public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
