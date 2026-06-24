using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

using Umbral.IdentityService.Application.Handlers.Commands;
namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class CrearEquipoHandlerTests
{
    [Fact]
    public async Task Should_Create_Team_When_User_Is_Not_In_ActiveTeam()
    {
        var repo = new FakeEquipoRepository { ExistsActiveTeamByUserIdValue = false };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new CrearEquipoCommandHandler(repo, publisher);

        var response = await handler.Handle(new CrearEquipoCommand(Guid.NewGuid(), "Equipo A"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.EquipoId);
        Assert.Equal("Equipo A", response.NombreEquipo);
        Assert.Single(response.Integrantes);
        Assert.True(repo.AddWasCalled);
        Assert.True(publisher.PublishWasCalled);
    }

    [Fact]
    public async Task Should_Throw_When_User_Already_Belongs_To_ActiveTeam()
    {
        var repo = new FakeEquipoRepository { ExistsActiveTeamByUserIdValue = true };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new CrearEquipoCommandHandler(repo, publisher);

        await Assert.ThrowsAsync<AlreadyBelongsToActiveTeamException>(() =>
            handler.Handle(new CrearEquipoCommand(Guid.NewGuid(), "Equipo A"), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Map_ConcurrentTeamCreation_To_AlreadyBelongsConflict()
    {
        var repo = new FakeEquipoRepository
        {
            ExistsActiveTeamByUserIdValue = false,
            AddExceptionToThrow = new ConcurrentTeamCreationException(Guid.NewGuid())
        };
        var publisher = new FakeEquipoEventsPublisher();
        var handler = new CrearEquipoCommandHandler(repo, publisher);

        await Assert.ThrowsAsync<AlreadyBelongsToActiveTeamException>(() =>
            handler.Handle(new CrearEquipoCommand(Guid.NewGuid(), "Equipo A"), CancellationToken.None));
    }

    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public bool ExistsActiveTeamByUserIdValue { get; set; }
        public bool AddWasCalled { get; private set; }
        public Exception? AddExceptionToThrow { get; set; }

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(ExistsActiveTeamByUserIdValue);

        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<Equipo?>(null);

        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken)
            => Task.FromResult<Equipo?>(null);

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
        {
            AddWasCalled = true;

            if (AddExceptionToThrow is not null)
            {
                throw AddExceptionToThrow;
            }

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeEquipoEventsPublisher : IEquipoEventsPublisher
    {
        public bool PublishWasCalled { get; private set; }

        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            PublishWasCalled = true;
            return Task.CompletedTask;
        }

        public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
