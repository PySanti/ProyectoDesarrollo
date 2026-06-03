using Umbral.TeamService.Application.Abstractions.Persistence;
using Umbral.TeamService.Application.Exceptions;
using Umbral.TeamService.Application.Teams.TransferLeadership;
using Umbral.TeamService.Domain.Entities;
using Umbral.TeamService.Domain.Enums;

namespace Umbral.TeamService.UnitTests;

public sealed class TransferirLiderazgoHandlerTests
{
    [Fact]
    public async Task Should_Transfer_Leadership_For_Active_Team_Leader()
    {
        var lider = Guid.NewGuid();
        var nuevoLider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", "ABCD1234", lider);
        equipo.AgregarParticipante(nuevoLider);
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = new TransferirLiderazgoCommandHandler(repo);

        var response = await handler.Handle(new TransferirLiderazgoCommand(lider, nuevoLider), CancellationToken.None);

        Assert.Equal(equipo.EquipoId, response.EquipoId);
        Assert.Equal(lider, response.LiderAnteriorUserId);
        Assert.Equal(nuevoLider, response.NuevoLiderUserId);
        Assert.Equal(EstadoEquipo.Activo.ToString(), response.EquipoEstado);
        Assert.True(repo.UpdateWasCalled);
        Assert.True(equipo.Participantes.Single(x => x.UsuarioId == nuevoLider).EsLider);
        Assert.False(equipo.Participantes.Single(x => x.UsuarioId == lider).EsLider);
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_No_Active_Team_Exists()
    {
        var repo = new FakeEquipoRepository { TeamToReturn = null };
        var handler = new TransferirLiderazgoCommandHandler(repo);

        await Assert.ThrowsAsync<NoActiveTeamForParticipantException>(() =>
            handler.Handle(new TransferirLiderazgoCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_Conflict_When_Actor_Is_Not_Leader()
    {
        var lider = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var target = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", "ABCD1234", lider);
        equipo.AgregarParticipante(actor);
        equipo.AgregarParticipante(target);
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = new TransferirLiderazgoCommandHandler(repo);

        var ex = await Assert.ThrowsAsync<TransferirLiderazgoConflictException>(() =>
            handler.Handle(new TransferirLiderazgoCommand(actor, target), CancellationToken.None));

        Assert.Contains("no es el lider", ex.Message);
        Assert.False(repo.UpdateWasCalled);
    }

    [Fact]
    public async Task Should_Throw_Conflict_When_Target_Is_Not_Member()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", "ABCD1234", lider);
        equipo.AgregarParticipante(Guid.NewGuid());
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = new TransferirLiderazgoCommandHandler(repo);

        var ex = await Assert.ThrowsAsync<TransferirLiderazgoConflictException>(() =>
            handler.Handle(new TransferirLiderazgoCommand(lider, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains("no pertenece", ex.Message);
        Assert.False(repo.UpdateWasCalled);
    }

    [Fact]
    public async Task Should_Throw_Conflict_When_Target_Is_Current_Leader()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", "ABCD1234", lider);
        equipo.AgregarParticipante(Guid.NewGuid());
        var repo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = new TransferirLiderazgoCommandHandler(repo);

        var ex = await Assert.ThrowsAsync<TransferirLiderazgoConflictException>(() =>
            handler.Handle(new TransferirLiderazgoCommand(lider, lider), CancellationToken.None));

        Assert.Contains("debe ser diferente", ex.Message);
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
