using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Application.Games.ActiveStage;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;

namespace Umbral.BdtGameService.UnitTests;

public sealed class ObtenerEtapaActivaBdtQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Active_Stage_For_Registered_Participant()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        var repository = new InMemoryPartidaBdtRepository(partida);
        var handler = new ObtenerEtapaActivaBdtQueryHandler(repository);

        var response = await handler.Handle(new ObtenerEtapaActivaBdtQuery(partida.PartidaId, participanteId), CancellationToken.None);

        Assert.Equal(partida.PartidaId, response.PartidaId);
        Assert.Equal("Iniciada", response.Estado);
        Assert.Equal("Individual", response.Modalidad);
        Assert.Equal(1, response.EtapaActiva.Orden);
        Assert.Equal("Activa", response.EtapaActiva.Estado);
        Assert.True(response.PuedeSubirTesoro);
        Assert.True(response.RequiereGeolocalizacion);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task Handle_Should_Map_Missing_Game_To_NotFound_Exception()
    {
        var handler = new ObtenerEtapaActivaBdtQueryHandler(new InMemoryPartidaBdtRepository(null));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(
            new ObtenerEtapaActivaBdtQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Reject_Unregistered_Participant()
    {
        var partida = CreateStartedIndividualGame(Guid.NewGuid());
        var handler = new ObtenerEtapaActivaBdtQueryHandler(new InMemoryPartidaBdtRepository(partida));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(
            new ObtenerEtapaActivaBdtQuery(partida.PartidaId, Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Reject_NonInitiated_Game()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        var handler = new ObtenerEtapaActivaBdtQueryHandler(new InMemoryPartidaBdtRepository(partida));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new ObtenerEtapaActivaBdtQuery(partida.PartidaId, participanteId),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Reject_Initiated_Game_Without_Active_Stage()
    {
        var partida = PartidaBDT.CrearNoPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            new[] { EtapaBDT.Crear(1, "QR-1", 60) },
            EstadoPartida.Iniciada);
        var handler = new ObtenerEtapaActivaBdtQueryHandler(new InMemoryPartidaBdtRepository(partida));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new ObtenerEtapaActivaBdtQuery(partida.PartidaId, Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public void Validator_Should_Reject_Empty_Ids()
    {
        var validator = new ObtenerEtapaActivaBdtQueryValidator();

        var result = validator.Validate(new ObtenerEtapaActivaBdtQuery(Guid.Empty, Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ObtenerEtapaActivaBdtQuery.PartidaId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ObtenerEtapaActivaBdtQuery.ParticipanteUserId));
    }

    private static PartidaBDT CreateStartedIndividualGame(Guid participanteId)
    {
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);
        return partida;
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
