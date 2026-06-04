using Microsoft.Extensions.Logging.Abstractions;
using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Application.Abstractions.Realtime;
using Umbral.BdtGameService.Application.Games.Start;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;

namespace Umbral.BdtGameService.UnitTests;

public sealed class IniciarPartidaBdtCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Persist_Start_And_Notify_Realtime()
    {
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        var repository = new InMemoryPartidaBdtRepository(partida);
        var notifier = new FakeRealtimeNotifier();
        var handler = CreateHandler(repository, notifier);

        var response = await handler.Handle(new IniciarPartidaBdtCommand(partida.PartidaId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(repository.UpdateCalled);
        Assert.Equal(1, repository.LockCallCount);
        Assert.Equal("Iniciada", response.Estado);
        Assert.Equal("Individual", response.Modalidad);
        Assert.Equal(1, response.EtapaActiva.Orden);
        Assert.Equal(1, notifier.CallCount);
        Assert.NotNull(notifier.LastResponse);
        Assert.Equal(partida.PartidaId, notifier.LastResponse!.PartidaId);
    }

    [Fact]
    public async Task Handle_Should_Map_Missing_Game_To_NotFound_Exception()
    {
        var handler = CreateHandler(new InMemoryPartidaBdtRepository(null), new FakeRealtimeNotifier());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(
            new IniciarPartidaBdtCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Propagate_Business_Conflict_And_Not_Notify()
    {
        var partida = CreateIndividualGame();
        var notifier = new FakeRealtimeNotifier();
        var handler = CreateHandler(new InMemoryPartidaBdtRepository(partida), notifier);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new IniciarPartidaBdtCommand(partida.PartidaId, Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal(0, notifier.CallCount);
    }

    [Fact]
    public async Task Handle_Should_Return_Success_When_PostCommit_Realtime_Fails()
    {
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        var repository = new InMemoryPartidaBdtRepository(partida);
        var notifier = new FakeRealtimeNotifier { ThrowOnNotify = true };
        var handler = CreateHandler(repository, notifier);

        var response = await handler.Handle(new IniciarPartidaBdtCommand(partida.PartidaId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(repository.UpdateCalled);
        Assert.Equal("Iniciada", response.Estado);
        Assert.Equal(EstadoPartida.Iniciada, partida.Estado);
        Assert.Equal(1, notifier.CallCount);
    }

    [Fact]
    public void Validator_Should_Reject_Empty_Ids()
    {
        var validator = new IniciarPartidaBdtCommandValidator();

        var result = validator.Validate(new IniciarPartidaBdtCommand(Guid.Empty, Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(IniciarPartidaBdtCommand.PartidaId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(IniciarPartidaBdtCommand.OperadorUserId));
    }

    private static IniciarPartidaBdtCommandHandler CreateHandler(
        IPartidaBdtRepository repository,
        IPartidaBdtRealtimeNotifier notifier)
    {
        return new IniciarPartidaBdtCommandHandler(
            repository,
            notifier,
            NullLogger<IniciarPartidaBdtCommandHandler>.Instance);
    }

    private static PartidaBDT CreateIndividualGame()
    {
        return PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 1,
            maximoParticipantes: 2,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60), EtapaBDT.Crear(2, "QR-2", 90) });
    }

    private sealed class FakeRealtimeNotifier : IPartidaBdtRealtimeNotifier
    {
        public int CallCount { get; private set; }
        public IniciarPartidaBdtResponse? LastResponse { get; private set; }
        public bool ThrowOnNotify { get; init; }

        public Task NotifyPartidaBdtIniciadaAsync(IniciarPartidaBdtResponse response, CancellationToken cancellationToken)
        {
            CallCount++;
            LastResponse = response;

            if (ThrowOnNotify)
            {
                throw new InvalidOperationException("SignalR unavailable");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPartidaBdtRepository : IPartidaBdtRepository
    {
        private readonly PartidaBDT? _partida;

        public InMemoryPartidaBdtRepository(PartidaBDT? partida)
        {
            _partida = partida;
        }

        public bool UpdateCalled { get; private set; }
        public int LockCallCount { get; private set; }

        public Task AddAsync(PartidaBDT partida, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public async Task<TResult> ExecuteWithPartidaRegistrationLockAsync<TResult>(
            Guid partidaId,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            LockCallCount++;
            return await operation(cancellationToken);
        }

        public Task<PartidaBDT?> GetByIdWithExploradoresAsync(Guid partidaId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_partida?.PartidaId == partidaId ? _partida : null);
        }

        public Task UpdateAsync(PartidaBDT partida, CancellationToken cancellationToken)
        {
            UpdateCalled = true;
            return Task.CompletedTask;
        }
    }
}
