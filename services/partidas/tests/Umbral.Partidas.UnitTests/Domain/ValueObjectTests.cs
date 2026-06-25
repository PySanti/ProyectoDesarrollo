using System;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void PartidaId_New_is_valid_and_nonempty()
    {
        var id = PartidaId.New();
        Assert.True(id.EsValido());
        Assert.NotEqual(Guid.Empty, id.Valor);
    }

    [Fact]
    public void PartidaId_From_empty_guid_is_invalid()
    {
        Assert.False(PartidaId.From(Guid.Empty).EsValido());
    }

    [Fact]
    public void JuegoId_New_is_valid()
    {
        Assert.True(JuegoId.New().EsValido());
    }

    [Fact]
    public void NombrePartida_trims_and_accepts_valid_value()
    {
        var nombre = NombrePartida.Crear("  Copa UMBRAL  ");
        Assert.Equal("Copa UMBRAL", nombre.Valor);
        Assert.True(nombre.EsValido());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NombrePartida_rejects_blank(string value)
    {
        Assert.Throws<ArgumentException>(() => NombrePartida.Crear(value));
    }

    [Fact]
    public void NombrePartida_rejects_over_max_length()
    {
        var tooLong = new string('x', NombrePartida.LongitudMaxima + 1);
        Assert.Throws<ArgumentException>(() => NombrePartida.Crear(tooLong));
    }

    [Fact]
    public void PuntajeAsignado_accepts_positive()
    {
        var p = PuntajeAsignado.Crear(10);
        Assert.Equal(10, p.Valor);
        Assert.True(p.EsValido());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void PuntajeAsignado_rejects_non_positive(int value)
    {
        Assert.Throws<ArgumentException>(() => PuntajeAsignado.Crear(value));
    }
}
