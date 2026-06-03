using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Application.Games.JoinIndividual;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;

namespace Umbral.BdtGameService.UnitTests;

public sealed class UnirseABdtIndividualCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Persist_Individual_Explorer_And_Return_Waiting_Data()
    {
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        var repository = new InMemoryPartidaBdtRepository(partida);
        var handler = new UnirseABdtIndividualCommandHandler(repository);
        var participanteId = Guid.NewGuid();

        var response = await handler.Handle(new UnirseABdtIndividualCommand(partida.PartidaId, participanteId), CancellationToken.None);

        Assert.True(repository.UpdateCalled);
        Assert.Equal(partida.PartidaId, response.PartidaId);
        Assert.Equal("Ruta QR", response.Nombre);
        Assert.Equal("Individual", response.Modalidad);
        Assert.Equal("Lobby", response.Estado);
        Assert.Equal(participanteId, response.ParticipanteUserId);
        Assert.Equal(1, response.PosicionEnLobby);
        Assert.Contains("Espera", response.Mensaje);
        Assert.Single(partida.Exploradores);
    }

    [Fact]
    public async Task Handle_Should_Map_Missing_Game_To_NotFound_Exception()
    {
        var handler = new UnirseABdtIndividualCommandHandler(new InMemoryPartidaBdtRepository(null));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(
            new UnirseABdtIndividualCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Propagate_Business_Conflicts()
    {
        var partida = CreateIndividualGame(maximoParticipantes: 1);
        var participanteId = Guid.NewGuid();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        var handler = new UnirseABdtIndividualCommandHandler(new InMemoryPartidaBdtRepository(partida));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new UnirseABdtIndividualCommand(partida.PartidaId, participanteId),
            CancellationToken.None));
    }

    [Fact]
    public void Validator_Should_Reject_Empty_Ids()
    {
        var validator = new UnirseABdtIndividualCommandValidator();

        var result = validator.Validate(new UnirseABdtIndividualCommand(Guid.Empty, Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UnirseABdtIndividualCommand.PartidaId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UnirseABdtIndividualCommand.ParticipanteUserId));
    }

    private static PartidaBDT CreateIndividualGame(int maximoParticipantes)
    {
        return PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 1,
            maximoParticipantes: maximoParticipantes,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });
    }

    private sealed class InMemoryPartidaBdtRepository : IPartidaBdtRepository
    {
        private readonly PartidaBDT? _partida;

        public InMemoryPartidaBdtRepository(PartidaBDT? partida)
        {
            _partida = partida;
        }

        public bool UpdateCalled { get; private set; }

        public Task AddAsync(PartidaBDT partida, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public async Task<TResult> ExecuteWithPartidaRegistrationLockAsync<TResult>(
            Guid partidaId,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
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
