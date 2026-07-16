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
        var historial = new FakeHistorialNombreEquipoRepository();
        var handler = new CrearEquipoCommandHandler(repo, publisher, historial, TimeProvider.System);

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
        var historial = new FakeHistorialNombreEquipoRepository();
        var handler = new CrearEquipoCommandHandler(repo, publisher, historial, TimeProvider.System);

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
        var historial = new FakeHistorialNombreEquipoRepository();
        var handler = new CrearEquipoCommandHandler(repo, publisher, historial, TimeProvider.System);

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

        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Equipo>>(Array.Empty<Equipo>());

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

    private sealed class FakeHistorialNombreEquipoRepository : IHistorialNombreEquipoRepository
    {
        public List<HistorialNombreEquipo> Registros { get; } = new();

        public Task AddRangeAsync(IEnumerable<HistorialNombreEquipo> registros, CancellationToken cancellationToken)
        {
            Registros.AddRange(registros);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistorialNombreEquipo>> GetByUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<HistorialNombreEquipo>>(Registros.Where(x => x.SubjectId == usuarioId).ToList());

        public Task<bool> AnyAsync(CancellationToken cancellationToken) => Task.FromResult(Registros.Count > 0);
    }

    private sealed class FakeEquipoEventsPublisher : IIdentityEventsPublisher
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
