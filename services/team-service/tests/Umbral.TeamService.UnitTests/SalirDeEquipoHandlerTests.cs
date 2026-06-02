using Umbral.TeamService.Application.Abstractions.Persistence;
using Umbral.TeamService.Application.Exceptions;
using Umbral.TeamService.Application.Teams.LeaveTeam;
using Umbral.TeamService.Domain.Entities;
using Umbral.TeamService.Domain.Enums;

namespace Umbral.TeamService.UnitTests;

public sealed class SalirDeEquipoHandlerTests
{
    [Fact]
    public async Task Should_Remove_NonLeader_From_ActiveTeam()
    {
        var lider = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", "ABCD1234", lider);
        equipo.AgregarParticipante(actor);
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = new SalirDeEquipoCommandHandler(repo);

        var response = await handler.Handle(new SalirDeEquipoCommand(actor), CancellationToken.None);

        Assert.Equal(actor, response.UserId);
        Assert.Equal(equipo.EquipoId, response.EquipoId);
        Assert.Equal(ResultadoSalidaEquipo.SalioDelEquipo.ToString(), response.Resultado);
        Assert.Equal(EstadoEquipo.Activo.ToString(), response.EquipoEstado);
        Assert.True(repo.UpdateWasCalled);
        Assert.DoesNotContain(equipo.Participantes, x => x.UsuarioId == actor);
    }

    [Fact]
    public async Task Should_Mark_Team_Deleted_When_OnlyLeader_Leaves()
    {
        var actor = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", "ABCD1234", actor);
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = new SalirDeEquipoCommandHandler(repo);

        var response = await handler.Handle(new SalirDeEquipoCommand(actor), CancellationToken.None);

        Assert.Equal(ResultadoSalidaEquipo.EquipoEliminado.ToString(), response.Resultado);
        Assert.Equal(EstadoEquipo.Eliminado.ToString(), response.EquipoEstado);
        Assert.True(repo.UpdateWasCalled);
        Assert.Empty(equipo.Participantes);
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_No_ActiveTeam_Exists()
    {
        var repo = new FakeEquipoRepository { TeamToReturn = null };
        var handler = new SalirDeEquipoCommandHandler(repo);

        await Assert.ThrowsAsync<NoActiveTeamForParticipantException>(() =>
            handler.Handle(new SalirDeEquipoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_Conflict_When_Leader_Has_Other_Members()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", "ABCD1234", lider);
        equipo.AgregarParticipante(Guid.NewGuid());
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = new SalirDeEquipoCommandHandler(repo);

        var ex = await Assert.ThrowsAsync<LeaveTeamConflictException>(() =>
            handler.Handle(new SalirDeEquipoCommand(lider), CancellationToken.None));

        Assert.Contains("debe transferir", ex.Message);
        Assert.False(repo.UpdateWasCalled);
    }

    [Fact]
    public async Task Should_Throw_Conflict_When_Team_Is_Not_Active()
    {
        var actor = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", "ABCD1234", actor);
        equipo.Salir(actor);
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = new SalirDeEquipoCommandHandler(repo);

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

        public Task<bool> ExistsByAccessCodeAsync(string code, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<Equipo?> GetActiveByAccessCodeAsync(string code, CancellationToken cancellationToken)
            => Task.FromResult<Equipo?>(null);

        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn);

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
        {
            UpdateWasCalled = true;
            return Task.CompletedTask;
        }

        public Task AcquireAdvisoryLockAsync(string teamCode, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
