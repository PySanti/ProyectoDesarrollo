using Umbral.TeamService.Application.Abstractions.Persistence;
using Umbral.TeamService.Application.Exceptions;
using Umbral.TeamService.Application.Teams.JoinTeamByCode;
using Umbral.TeamService.Domain.Entities;

namespace Umbral.TeamService.UnitTests;

public sealed class UnirseAEquipoHandlerTests
{
    [Fact]
    public async Task Should_Join_Team_When_Code_Is_Valid_And_User_Not_In_ActiveTeam()
    {
        var actorUserId = Guid.NewGuid();
        var repo = new FakeEquipoRepository
        {
            ExistsActiveTeamByUserIdValue = false,
            TeamToReturn = Equipo.CrearPorParticipante("Equipo A", "ABCD1234", Guid.NewGuid())
        };
        var handler = new UnirseAEquipoPorCodigoCommandHandler(repo);

        var response = await handler.Handle(new UnirseAEquipoPorCodigoCommand(actorUserId, "ABCD1234"), CancellationToken.None);

        Assert.Equal("ABCD1234", response.CodigoAcceso);
        Assert.Contains(response.Integrantes, x => x.UserId == actorUserId && !x.EsLider);
        Assert.True(repo.UpdateWasCalled);
    }

    [Fact]
    public async Task Should_Throw_When_User_Already_Belongs_To_An_ActiveTeam()
    {
        var repo = new FakeEquipoRepository { ExistsActiveTeamByUserIdValue = true };
        var handler = new UnirseAEquipoPorCodigoCommandHandler(repo);

        await Assert.ThrowsAsync<AlreadyBelongsToActiveTeamException>(() =>
            handler.Handle(new UnirseAEquipoPorCodigoCommand(Guid.NewGuid(), "ABCD1234"), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Team_NotFound_By_Code()
    {
        var repo = new FakeEquipoRepository
        {
            ExistsActiveTeamByUserIdValue = false,
            TeamToReturn = null
        };
        var handler = new UnirseAEquipoPorCodigoCommandHandler(repo);

        await Assert.ThrowsAsync<TeamNotFoundByAccessCodeException>(() =>
            handler.Handle(new UnirseAEquipoPorCodigoCommand(Guid.NewGuid(), "ABCD1234"), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Team_Is_Full()
    {
        var fullTeam = Equipo.CrearPorParticipante("Equipo A", "ABCD1234", Guid.NewGuid());
        fullTeam.AgregarParticipante(Guid.NewGuid());
        fullTeam.AgregarParticipante(Guid.NewGuid());
        fullTeam.AgregarParticipante(Guid.NewGuid());
        fullTeam.AgregarParticipante(Guid.NewGuid());

        var repo = new FakeEquipoRepository
        {
            ExistsActiveTeamByUserIdValue = false,
            TeamToReturn = fullTeam
        };
        var handler = new UnirseAEquipoPorCodigoCommandHandler(repo);

        await Assert.ThrowsAsync<TeamFullException>(() =>
            handler.Handle(new UnirseAEquipoPorCodigoCommand(Guid.NewGuid(), "ABCD1234"), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Map_UniqueMembershipConflict_To_AlreadyBelongsConflict()
    {
        var actorUserId = Guid.NewGuid();
        var repo = new FakeEquipoRepository
        {
            ExistsActiveTeamByUserIdValue = false,
            TeamToReturn = Equipo.CrearPorParticipante("Equipo A", "ABCD1234", Guid.NewGuid()),
            ThrowUniqueMembershipConflictOnUpdate = true
        };
        var handler = new UnirseAEquipoPorCodigoCommandHandler(repo);

        await Assert.ThrowsAsync<AlreadyBelongsToActiveTeamException>(() =>
            handler.Handle(new UnirseAEquipoPorCodigoCommand(actorUserId, "ABCD1234"), CancellationToken.None));
    }

    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public bool ExistsActiveTeamByUserIdValue { get; set; }
        public Equipo? TeamToReturn { get; set; }
        public bool UpdateWasCalled { get; private set; }
        public bool ThrowUniqueMembershipConflictOnUpdate { get; set; }

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(ExistsActiveTeamByUserIdValue);

        public Task<bool> ExistsByAccessCodeAsync(string code, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<Equipo?> GetActiveByAccessCodeAsync(string code, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn);

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
        {
            UpdateWasCalled = true;

            if (ThrowUniqueMembershipConflictOnUpdate)
            {
                throw new UniqueMembershipConflictException("Unique membership conflict.");
            }

            return Task.CompletedTask;
        }
    }
}
