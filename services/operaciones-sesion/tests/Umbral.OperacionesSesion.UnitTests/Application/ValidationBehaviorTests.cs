using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Umbral.OperacionesSesion.Application;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Validators;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Throws_validation_exception_for_empty_partida_id()
    {
        var behavior = new ValidationBehavior<PublicarPartidaCommand, LobbyDto>(
            new IValidator<PublicarPartidaCommand>[] { new PublicarPartidaCommandValidator() });
        var command = new PublicarPartidaCommand(Guid.Empty, null);

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(command, () => Task.FromResult(Lobby()), CancellationToken.None));
    }

    [Fact]
    public async Task Calls_next_when_valid()
    {
        var behavior = new ValidationBehavior<PublicarPartidaCommand, LobbyDto>(
            new IValidator<PublicarPartidaCommand>[] { new PublicarPartidaCommandValidator() });
        var command = new PublicarPartidaCommand(Guid.NewGuid(), "Bearer x");
        var expected = Lobby();

        var result = await behavior.Handle(command, () => Task.FromResult(expected), CancellationToken.None);

        Assert.Same(expected, result);
    }

    private static LobbyDto Lobby() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Lobby", "Individual", 1, 10, 0, Array.Empty<Guid>(), Array.Empty<EquipoLobbyDto>());
}
