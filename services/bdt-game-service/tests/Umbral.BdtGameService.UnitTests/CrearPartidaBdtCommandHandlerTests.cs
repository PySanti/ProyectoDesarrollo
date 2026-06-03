using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Application.Games.Create;
using Umbral.BdtGameService.Domain.Entities;

namespace Umbral.BdtGameService.UnitTests;

public sealed class CrearPartidaBdtCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Persist_Valid_Bdt_Game_And_Return_Summary()
    {
        var repository = new CapturingPartidaBdtRepository();
        var handler = new CrearPartidaBdtCommandHandler(repository);
        var command = ValidIndividualCommand();

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.PartidaId);
        Assert.Equal("Busqueda QR Campus", response.Nombre);
        Assert.Equal("Individual", response.Modalidad);
        Assert.Equal("Lobby", response.Estado);
        Assert.Equal("Manual", response.ModoInicio);
        Assert.Equal(1, response.CantidadEtapas);
        Assert.NotNull(repository.SavedPartida);
    }

    [Fact]
    public async Task Handle_Should_Return_Domain_Conflict_For_Invalid_Modality_Limits()
    {
        var repository = new CapturingPartidaBdtRepository();
        var handler = new CrearPartidaBdtCommandHandler(repository);
        var command = ValidIndividualCommand() with { MaximoParticipantes = null };

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public void Validator_Should_Reject_Request_Without_Stages()
    {
        var validator = new CrearPartidaBdtCommandValidator();
        var command = ValidIndividualCommand() with { Etapas = Array.Empty<CrearEtapaBdtRequest>() };

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_Should_Reject_Null_Stages()
    {
        var validator = new CrearPartidaBdtCommandValidator();
        var command = ValidIndividualCommand() with { Etapas = null! };

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CrearPartidaBdtCommand.Etapas));
    }

    [Fact]
    public void Validator_Should_Reject_Duplicate_Stage_Order()
    {
        var validator = new CrearPartidaBdtCommandValidator();
        var command = ValidIndividualCommand() with
        {
            Etapas = new[]
            {
                new CrearEtapaBdtRequest(1, "QR-1", 300),
                new CrearEtapaBdtRequest(1, "QR-2", 300)
            }
        };

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("orden duplicado", StringComparison.OrdinalIgnoreCase));
    }

    private static CrearPartidaBdtCommand ValidIndividualCommand()
    {
        return new CrearPartidaBdtCommand(
            "Busqueda QR Campus",
            "Patio central",
            "Individual",
            2,
            20,
            null,
            null,
            "Manual",
            new[] { new CrearEtapaBdtRequest(1, "QR-1", 300) });
    }

    private sealed class CapturingPartidaBdtRepository : IPartidaBdtRepository
    {
        public PartidaBDT? SavedPartida { get; private set; }

        public Task AddAsync(PartidaBDT partida, CancellationToken cancellationToken)
        {
            SavedPartida = partida;
            return Task.CompletedTask;
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
            return Task.FromResult(SavedPartida?.PartidaId == partidaId ? SavedPartida : null);
        }

        public Task UpdateAsync(PartidaBDT partida, CancellationToken cancellationToken)
        {
            SavedPartida = partida;
            return Task.CompletedTask;
        }
    }
}
