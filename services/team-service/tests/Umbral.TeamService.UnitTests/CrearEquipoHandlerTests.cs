using Umbral.TeamService.Application.Abstractions.Events;
using Umbral.TeamService.Application.Abstractions.Persistence;
using Umbral.TeamService.Application.Abstractions.Services;
using Umbral.TeamService.Application.Exceptions;
using Umbral.TeamService.Application.Teams.CreateTeam;
using Umbral.TeamService.Domain.Entities;

namespace Umbral.TeamService.UnitTests;

public sealed class CrearEquipoHandlerTests
{
    [Fact]
    public async Task Should_Create_Team_When_User_Is_Not_In_ActiveTeam()
    {
        var repo = new FakeEquipoRepository { ExistsActiveTeamByUserIdValue = false };
        var generator = new FakeCodigoAccesoGenerator("ZXCV1234");
        var publisher = new FakeTeamEventsPublisher();
        var handler = new CrearEquipoCommandHandler(repo, generator, publisher);

        var response = await handler.Handle(new CrearEquipoCommand(Guid.NewGuid(), "Equipo A"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.EquipoId);
        Assert.Equal("ZXCV1234", response.CodigoAcceso);
        Assert.Single(response.Integrantes);
        Assert.True(repo.AddWasCalled);
        Assert.True(publisher.PublishWasCalled);
    }

    [Fact]
    public async Task Should_Throw_When_User_Already_Belongs_To_ActiveTeam()
    {
        var repo = new FakeEquipoRepository { ExistsActiveTeamByUserIdValue = true };
        var generator = new FakeCodigoAccesoGenerator("ZXCV1234");
        var publisher = new FakeTeamEventsPublisher();
        var handler = new CrearEquipoCommandHandler(repo, generator, publisher);

        await Assert.ThrowsAsync<AlreadyBelongsToActiveTeamException>(() =>
            handler.Handle(new CrearEquipoCommand(Guid.NewGuid(), "Equipo A"), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_AccessCodeGeneration_Fails()
    {
        var repo = new FakeEquipoRepository { ExistsActiveTeamByUserIdValue = false };
        var generator = new ThrowingCodigoAccesoGenerator();
        var publisher = new FakeTeamEventsPublisher();
        var handler = new CrearEquipoCommandHandler(repo, generator, publisher);

        await Assert.ThrowsAsync<AccessCodeGenerationException>(() =>
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
        var generator = new FakeCodigoAccesoGenerator("ZXCV1234");
        var publisher = new FakeTeamEventsPublisher();
        var handler = new CrearEquipoCommandHandler(repo, generator, publisher);

        await Assert.ThrowsAsync<AlreadyBelongsToActiveTeamException>(() =>
            handler.Handle(new CrearEquipoCommand(Guid.NewGuid(), "Equipo A"), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Retry_When_AccessCode_Collision_Happens_On_Persistence()
    {
        var repo = new FakeEquipoRepository
        {
            ExistsActiveTeamByUserIdValue = false,
            AddExceptionsQueue = new Queue<Exception>(
                new[]
                {
                    new AccessCodeGenerationException("Colision"),
                })
        };
        var generator = new FakeCodigoAccesoGenerator("ZXCV1234");
        var publisher = new FakeTeamEventsPublisher();
        var handler = new CrearEquipoCommandHandler(repo, generator, publisher);

        var response = await handler.Handle(new CrearEquipoCommand(Guid.NewGuid(), "Equipo A"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.EquipoId);
        Assert.Equal(2, repo.AddCallCount);
    }

    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public bool ExistsActiveTeamByUserIdValue { get; set; }
        public bool AddWasCalled { get; private set; }
        public int AddCallCount { get; private set; }
        public Exception? AddExceptionToThrow { get; set; }
        public Queue<Exception>? AddExceptionsQueue { get; set; }

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(ExistsActiveTeamByUserIdValue);

        public Task<bool> ExistsByAccessCodeAsync(string code, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
        {
            AddWasCalled = true;
            AddCallCount++;

            if (AddExceptionsQueue is { Count: > 0 })
            {
                throw AddExceptionsQueue.Dequeue();
            }

            if (AddExceptionToThrow is not null)
            {
                throw AddExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeCodigoAccesoGenerator : ICodigoAccesoGenerator
    {
        private readonly string _code;

        public FakeCodigoAccesoGenerator(string code)
        {
            _code = code;
        }

        public Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
            => Task.FromResult(_code);
    }

    private sealed class ThrowingCodigoAccesoGenerator : ICodigoAccesoGenerator
    {
        public Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
            => throw new AccessCodeGenerationException("No unique code.");
    }

    private sealed class FakeTeamEventsPublisher : ITeamEventsPublisher
    {
        public bool PublishWasCalled { get; private set; }

        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            PublishWasCalled = true;
            return Task.CompletedTask;
        }
    }
}
