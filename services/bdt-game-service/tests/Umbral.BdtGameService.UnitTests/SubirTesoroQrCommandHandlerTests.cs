using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Application.Abstractions.Qr;
using Umbral.BdtGameService.Application.Abstractions.Storage;
using Umbral.BdtGameService.Application.Games.UploadTreasure;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;

namespace Umbral.BdtGameService.UnitTests;

public sealed class SubirTesoroQrCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Store_Decode_And_Persist_Decoded_Treasure()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        var etapaId = partida.Etapas.Single(etapa => etapa.Estado == EstadoEtapa.Activa).EtapaId;
        var repository = new InMemoryPartidaBdtRepository(partida);
        var storage = new FakeImageStorage("bdt/tesoro.jpg");
        var decoder = new FakeQrDecoder("QR-ETAPA-1");
        var handler = new SubirTesoroQrCommandHandler(repository, storage, decoder);

        var response = await handler.Handle(CreateCommand(partida.PartidaId, etapaId, participanteId), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.TesoroId);
        Assert.Equal("Decodificado", response.EstadoProcesamiento);
        Assert.Equal("QR-ETAPA-1", response.QrDecodificado);
        Assert.True(storage.Called);
        Assert.True(decoder.Called);
        Assert.True(repository.UpdateCalled);
        Assert.Single(partida.Tesoros);
    }

    [Fact]
    public async Task Handle_Should_Record_Unreadable_Qr_Attempt()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        var etapaId = partida.Etapas.Single(etapa => etapa.Estado == EstadoEtapa.Activa).EtapaId;
        var handler = new SubirTesoroQrCommandHandler(
            new InMemoryPartidaBdtRepository(partida),
            new FakeImageStorage("bdt/tesoro.jpg"),
            new FakeQrDecoder(null));

        var response = await handler.Handle(CreateCommand(partida.PartidaId, etapaId, participanteId), CancellationToken.None);

        Assert.Equal("NoLegible", response.EstadoProcesamiento);
        Assert.Null(response.QrDecodificado);
        Assert.Single(partida.Tesoros);
        Assert.Contains("no se pudo leer", response.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_Should_Allow_Multiple_Attempts()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        var etapaId = partida.Etapas.Single(etapa => etapa.Estado == EstadoEtapa.Activa).EtapaId;
        var repository = new InMemoryPartidaBdtRepository(partida);
        var handler = new SubirTesoroQrCommandHandler(repository, new FakeImageStorage("bdt/tesoro.jpg"), new FakeQrDecoder(null));

        await handler.Handle(CreateCommand(partida.PartidaId, etapaId, participanteId), CancellationToken.None);
        await handler.Handle(CreateCommand(partida.PartidaId, etapaId, participanteId), CancellationToken.None);

        Assert.Equal(2, partida.Tesoros.Count);
        Assert.Equal(2, repository.UpdateCount);
    }

    [Fact]
    public async Task Handle_Should_Reject_Missing_Game()
    {
        var handler = new SubirTesoroQrCommandHandler(
            new InMemoryPartidaBdtRepository(null),
            new FakeImageStorage("bdt/tesoro.jpg"),
            new FakeQrDecoder("QR"));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(CreateCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Reject_Unregistered_Participant_Before_Storage()
    {
        var partida = CreateStartedIndividualGame(Guid.NewGuid());
        var etapaId = partida.Etapas.Single(etapa => etapa.Estado == EstadoEtapa.Activa).EtapaId;
        var storage = new FakeImageStorage("bdt/tesoro.jpg");
        var handler = new SubirTesoroQrCommandHandler(new InMemoryPartidaBdtRepository(partida), storage, new FakeQrDecoder("QR"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(CreateCommand(partida.PartidaId, etapaId, Guid.NewGuid()), CancellationToken.None));

        Assert.False(storage.Called);
    }

    [Fact]
    public void Validator_Should_Reject_Invalid_Metadata()
    {
        var validator = new SubirTesoroQrCommandValidator();

        var result = validator.Validate(new SubirTesoroQrCommand(
            Guid.Empty,
            Guid.Empty,
            Guid.Empty,
            string.Empty,
            "text/plain",
            SubirTesoroQrCommandValidator.MaxImageSizeBytes + 1,
            Array.Empty<byte>()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SubirTesoroQrCommand.PartidaId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SubirTesoroQrCommand.EtapaId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SubirTesoroQrCommand.ParticipanteUserId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SubirTesoroQrCommand.ContentType));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SubirTesoroQrCommand.Length));
    }

    private static SubirTesoroQrCommand CreateCommand(Guid partidaId, Guid etapaId, Guid participanteId)
    {
        var content = new byte[] { 1, 2, 3 };
        return new SubirTesoroQrCommand(partidaId, etapaId, participanteId, "tesoro.jpg", "image/jpeg", content.Length, content);
    }

    private static PartidaBDT CreateStartedIndividualGame(Guid participanteId)
    {
        var partida = PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 1,
            maximoParticipantes: 5,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);
        return partida;
    }

    private sealed class InMemoryPartidaBdtRepository : IPartidaBdtRepository
    {
        private readonly PartidaBDT? _partida;

        public InMemoryPartidaBdtRepository(PartidaBDT? partida)
        {
            _partida = partida;
        }

        public bool UpdateCalled { get; private set; }
        public int UpdateCount { get; private set; }

        public Task AddAsync(PartidaBDT partida, CancellationToken cancellationToken) => throw new NotSupportedException();

        public async Task<TResult> ExecuteWithPartidaRegistrationLockAsync<TResult>(Guid partidaId, Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
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
            UpdateCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeImageStorage : ITesoroQrImageStorage
    {
        private readonly string _reference;

        public FakeImageStorage(string reference)
        {
            _reference = reference;
        }

        public bool Called { get; private set; }

        public Task<string> StoreAsync(Guid partidaId, Guid etapaId, Guid participanteUserId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult(_reference);
        }
    }

    private sealed class FakeQrDecoder : IQrImageDecoder
    {
        private readonly string? _decoded;

        public FakeQrDecoder(string? decoded)
        {
            _decoded = decoded;
        }

        public bool Called { get; private set; }

        public Task<string?> DecodeAsync(byte[] imageContent, string contentType, CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult(_decoded);
        }
    }
}
