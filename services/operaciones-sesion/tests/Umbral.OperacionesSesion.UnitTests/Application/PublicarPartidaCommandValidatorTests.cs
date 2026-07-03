using System;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Validators;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class PublicarPartidaCommandValidatorTests
{
    [Fact]
    public void Empty_partida_id_is_invalid()
    {
        var result = new PublicarPartidaCommandValidator().Validate(new PublicarPartidaCommand(Guid.Empty, null));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Non_empty_partida_id_is_valid()
    {
        var result = new PublicarPartidaCommandValidator().Validate(new PublicarPartidaCommand(Guid.NewGuid(), null));
        Assert.True(result.IsValid);
    }
}
