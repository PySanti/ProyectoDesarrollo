using System;
using System.Collections.Generic;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void SesionPartidaId_New_is_valid_and_non_empty()
    {
        var id = SesionPartidaId.New();
        Assert.True(id.EsValido());
        Assert.NotEqual(Guid.Empty, id.Valor);
    }

    [Fact]
    public void SesionPartidaId_From_empty_is_invalid()
        => Assert.False(SesionPartidaId.From(Guid.Empty).EsValido());

    [Fact]
    public void InscripcionId_New_is_valid()
        => Assert.True(InscripcionId.New().EsValido());

    [Fact]
    public void ConfiguracionSnapshot_exposes_partida_level_fields_and_juego_references()
    {
        var snapshot = new ConfiguracionSnapshot(
            "Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { new(Guid.NewGuid(), 1, TipoJuego.Trivia) });

        Assert.Equal("Copa", snapshot.Nombre);
        Assert.Equal(Modalidad.Individual, snapshot.Modalidad);
        Assert.Single(snapshot.Juegos);
        Assert.Equal(1, snapshot.Juegos[0].Orden);
        Assert.Equal(TipoJuego.Trivia, snapshot.Juegos[0].TipoJuego);
    }
}
