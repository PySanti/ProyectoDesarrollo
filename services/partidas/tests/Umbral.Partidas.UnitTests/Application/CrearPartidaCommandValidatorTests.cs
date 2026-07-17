using System;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.Validators;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.UnitTests.Application;

public class CrearPartidaCommandValidatorTests
{
    private readonly CrearPartidaCommandValidator _validator = new();

    [Fact]
    public void Valid_manual_command_passes()
    {
        var cmd = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);
        Assert.True(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Blank_name_fails()
    {
        var cmd = new CrearPartidaCommand("", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Maximos_below_minimos_fails()
    {
        var cmd = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 5, 2);
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Automatico_without_tiempo_inicio_fails()
    {
        var cmd = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Automatico, null, 1, 10);
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Manual_with_tiempo_inicio_fails()
    {
        var cmd = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, DateTime.UtcNow, 1, 10);
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void ManualYAutomatico_without_tiempo_inicio_fails()
    {
        var cmd = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.ManualYAutomatico, null, 1, 10);
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void ManualYAutomatico_with_tiempo_inicio_passes()
    {
        var cmd = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.ManualYAutomatico, DateTime.UtcNow, 1, 10);
        Assert.True(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Name_over_120_chars_fails()
    {
        var cmd = new CrearPartidaCommand(new string('x', 121), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Theory]
    [InlineData("****")]
    [InlineData("   ")]
    [InlineData("123 !!")]
    public void Name_without_any_letter_fails(string nombre)
    {
        var cmd = new CrearPartidaCommand(nombre, Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);
        Assert.False(_validator.Validate(cmd).IsValid);
    }
}
