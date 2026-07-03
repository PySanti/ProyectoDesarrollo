// TriviaValidatorsTests.cs
using System;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Validators;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class TriviaValidatorsTests
{
    [Fact]
    public void Responder_requires_partida_and_opcion()
    {
        var v = new ResponderPreguntaCommandValidator();
        Assert.False(v.Validate(new ResponderPreguntaCommand(Guid.Empty, Guid.NewGuid(), Guid.Empty)).IsValid);
        Assert.True(v.Validate(new ResponderPreguntaCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Avanzar_requires_partida()
    {
        var v = new AvanzarPreguntaCommandValidator();
        Assert.False(v.Validate(new AvanzarPreguntaCommand(Guid.Empty)).IsValid);
        Assert.True(v.Validate(new AvanzarPreguntaCommand(Guid.NewGuid())).IsValid);
    }
}
