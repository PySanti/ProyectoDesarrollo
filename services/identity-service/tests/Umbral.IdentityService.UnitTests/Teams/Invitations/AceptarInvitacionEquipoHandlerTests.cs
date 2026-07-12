using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

using Umbral.IdentityService.Application.Handlers.Commands;
namespace Umbral.IdentityService.UnitTests.Teams.Invitations;

public sealed class AceptarInvitacionEquipoHandlerTests
{
    [Fact]
    public async Task Aceptar_Throws_InvitacionNoEncontrada_When_Invite_Not_Found()
    {
        var invRepo = new FakeInvitacionEquipoRepository { InvitacionToReturn = null };
        var equipoRepo = new FakeEquipoRepository();
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new AceptarInvitacionEquipoCommandHandler(invRepo, equipoRepo, publisher);

        await Assert.ThrowsAsync<InvitacionNoEncontradaException>(() =>
            handler.Handle(new AceptarInvitacionEquipoCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Aceptar_Throws_InvitacionNoEncontrada_When_Actor_Is_Not_Invitee()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, invitado, lider);

        var invRepo = new FakeInvitacionEquipoRepository { InvitacionToReturn = invitacion };
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new AceptarInvitacionEquipoCommandHandler(invRepo, equipoRepo, publisher);

        // actor is different from invitado
        await Assert.ThrowsAsync<InvitacionNoEncontradaException>(() =>
            handler.Handle(new AceptarInvitacionEquipoCommand(Guid.NewGuid(), invitacion.InvitacionEquipoId), CancellationToken.None));
    }

    [Fact]
    public async Task Aceptar_Throws_InvitacionNoEncontrada_When_Invitation_Not_Pending()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, invitado, lider);
        // Force non-pending state by accepting it first at domain level
        invitacion.Aceptar();

        var invRepo = new FakeInvitacionEquipoRepository { InvitacionToReturn = invitacion };
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new AceptarInvitacionEquipoCommandHandler(invRepo, equipoRepo, publisher);

        await Assert.ThrowsAsync<InvitacionNoEncontradaException>(() =>
            handler.Handle(new AceptarInvitacionEquipoCommand(invitado, invitacion.InvitacionEquipoId), CancellationToken.None));
    }

    [Fact]
    public async Task Aceptar_Throws_UsuarioYaEnEquipo_When_Invitee_Already_Has_Active_Team()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, invitado, lider);

        var invRepo = new FakeInvitacionEquipoRepository { InvitacionToReturn = invitacion };
        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            InvitadoAlreadyHasTeam = true
        };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new AceptarInvitacionEquipoCommandHandler(invRepo, equipoRepo, publisher);

        await Assert.ThrowsAsync<UsuarioYaEnEquipoException>(() =>
            handler.Handle(new AceptarInvitacionEquipoCommand(invitado, invitacion.InvitacionEquipoId), CancellationToken.None));
    }

    [Fact]
    public async Task Aceptar_Throws_EquipoLleno_When_Team_Is_Full()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, invitado, lider);

        var invRepo = new FakeInvitacionEquipoRepository { InvitacionToReturn = invitacion };
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new AceptarInvitacionEquipoCommandHandler(invRepo, equipoRepo, publisher);

        await Assert.ThrowsAsync<EquipoLlenoException>(() =>
            handler.Handle(new AceptarInvitacionEquipoCommand(invitado, invitacion.InvitacionEquipoId), CancellationToken.None));
    }

    [Fact]
    public async Task Aceptar_Adds_Member_And_Marks_Aceptada_When_Valid()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, invitado, lider);

        var invRepo = new FakeInvitacionEquipoRepository { InvitacionToReturn = invitacion };
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new AceptarInvitacionEquipoCommandHandler(invRepo, equipoRepo, publisher);

        var response = await handler.Handle(new AceptarInvitacionEquipoCommand(invitado, invitacion.InvitacionEquipoId), CancellationToken.None);

        Assert.Equal("Aceptada", response.EstadoInvitacion);
        Assert.Equal(invitado, response.InvitadoUserId);
        Assert.Equal(2, equipo.Participantes.Count);
        Assert.Contains(equipo.Participantes, p => p.UsuarioId == invitado);
        Assert.Equal(EstadoInvitacion.Aceptada, invitacion.Estado);
        Assert.True(invRepo.UpdateWasCalled);
        Assert.True(equipoRepo.UpdateWasCalled);
        Assert.True(publisher.InvitacionAceptadaWasCalled);
    }

    // Fakes

    private sealed class FakeInvitacionEquipoRepository : IInvitacionEquipoRepository
    {
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

    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public Equipo? TeamToReturn { get; set; }
        public bool InvitadoAlreadyHasTeam { get; set; }
        public bool UpdateWasCalled { get; private set; }

        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Equipo>>(Array.Empty<Equipo>());

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(InvitadoAlreadyHasTeam);

        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn);

        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn?.EquipoId == equipoId ? TeamToReturn : null);

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
        {
            UpdateWasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEquipoEventsPublisher : IIdentityEventsPublisher
    {
        public bool InvitacionAceptadaWasCalled { get; private set; }

        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            InvitacionAceptadaWasCalled = true;
            return Task.CompletedTask;
        }

        public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
