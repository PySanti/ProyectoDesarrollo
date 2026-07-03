using System;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Validators;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class EnviarPistaCommandValidatorTests
{
    private readonly EnviarPistaCommandValidator _v = new();

    [Fact]
    public void Valido() =>
        Assert.True(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), "Mira el faro")).IsValid);

    [Fact]
    public void Texto_vacio_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), "")).IsValid);

    [Fact]
    public void Texto_whitespace_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), "   ")).IsValid);

    [Fact]
    public void Texto_muy_largo_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 501))).IsValid);

    [Fact]
    public void Destino_vacio_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.Empty, "hola")).IsValid);

    [Fact]
    public void Partida_vacia_es_invalida() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.Empty, Guid.NewGuid(), "hola")).IsValid);

    [Fact]
    public void Sin_ningun_destino_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), null, "hola")).IsValid);

    [Fact]
    public void Con_ambos_destinos_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), "hola", Guid.NewGuid())).IsValid);

    [Fact]
    public void Solo_equipo_destino_es_valido() =>
        Assert.True(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), null, "hola", Guid.NewGuid())).IsValid);
}
