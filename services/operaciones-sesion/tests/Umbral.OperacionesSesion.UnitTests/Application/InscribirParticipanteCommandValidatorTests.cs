using System;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Validators;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class InscribirParticipanteCommandValidatorTests
{
    [Fact]
    public void Empty_ids_are_invalid()
    {
        var result = new InscribirParticipanteCommandValidator()
            .Validate(new InscribirParticipanteCommand(Guid.Empty, Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Valid_ids_pass()
    {
        var result = new InscribirParticipanteCommandValidator()
            .Validate(new InscribirParticipanteCommand(Guid.NewGuid(), Guid.NewGuid()));
        Assert.True(result.IsValid);
    }
}
