using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

using Umbral.IdentityService.Application.Handlers.Commands;
namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class SalirDeEquipoHandlerTests
{
    private static SalirDeEquipoCommandHandler CreateHandler(
        FakeEquipoRepository equipoRepo,
        FakeInvitacionEquipoRepository? invitacionRepo = null)
        => new SalirDeEquipoCommandHandler(equipoRepo, invitacionRepo ?? new FakeInvitacionEquipoRepository());

    [Fact]
    public async Task Should_Remove_NonLeader_From_ActiveTeam()
    {
        var lider = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(actor);
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = CreateHandler(repo);

        var response = await handler.Handle(new SalirDeEquipoCommand(actor), CancellationToken.None);

        Assert.Equal(actor, response.UserId);
        Assert.Equal(equipo.EquipoId, response.EquipoId);
        Assert.Equal(ResultadoSalidaEquipo.SalioDelEquipo.ToString(), response.Resultado);
        Assert.Equal(EstadoEquipo.Activo.ToString(), response.EquipoEstado);
        Assert.True(repo.UpdateWasCalled);
        Assert.DoesNotContain(equipo.Participantes, x => x.SubjectId == actor);
    }

    [Fact]
    public async Task Should_Mark_Team_Deleted_When_OnlyLeader_Leaves()
    {
        var actor = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", actor);
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var invRepo = new FakeInvitacionEquipoRepository();
        var handler = CreateHandler(repo, invRepo);

        var response = await handler.Handle(new SalirDeEquipoCommand(actor), CancellationToken.None);

        Assert.Equal(ResultadoSalidaEquipo.EquipoEliminado.ToString(), response.Resultado);
        Assert.Equal(EstadoEquipo.Eliminado.ToString(), response.EquipoEstado);
        Assert.True(repo.UpdateWasCalled);
        Assert.Empty(equipo.Participantes);
        Assert.True(invRepo.DeletePendientesByEquipoWasCalled);
    }

    [Fact]
    public async Task Should_Not_Delete_Invitaciones_When_NonLeader_Leaves()
    {
        var lider = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(actor);
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var invRepo = new FakeInvitacionEquipoRepository();
        var handler = CreateHandler(repo, invRepo);

        await handler.Handle(new SalirDeEquipoCommand(actor), CancellationToken.None);

        Assert.False(invRepo.DeletePendientesByEquipoWasCalled);
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_No_ActiveTeam_Exists()
    {
        var repo = new FakeEquipoRepository { TeamToReturn = null };
        var handler = CreateHandler(repo);

        await Assert.ThrowsAsync<NoActiveTeamForParticipantException>(() =>
            handler.Handle(new SalirDeEquipoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_Conflict_When_Leader_Has_Other_Members()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(Guid.NewGuid());
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = CreateHandler(repo);

        var ex = await Assert.ThrowsAsync<LeaveTeamConflictException>(() =>
            handler.Handle(new SalirDeEquipoCommand(lider), CancellationToken.None));

        Assert.Contains("debe transferir", ex.Message);
        Assert.False(repo.UpdateWasCalled);
    }

    [Fact]
    public async Task Should_Throw_Conflict_When_Team_Is_Not_Active()
    {
        var actor = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", actor);
        equipo.Salir(actor);
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = CreateHandler(repo);

        var ex = await Assert.ThrowsAsync<LeaveTeamConflictException>(() =>
            handler.Handle(new SalirDeEquipoCommand(actor), CancellationToken.None));

        Assert.Contains("no esta activo", ex.Message);
        Assert.False(repo.UpdateWasCalled);
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
}
