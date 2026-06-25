using System;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Domain;

public class PartidaTests
{
    private static Partida CrearManual() =>
        Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

    [Fact]
    public void Crear_manual_sets_null_estado_and_no_games()
    {
        var partida = CrearManual();
        Assert.Null(partida.Estado);          // SEAM: not published yet
        Assert.True(partida.PartidaId.EsValido());
        Assert.Empty(partida.Juegos);
    }

    [Fact]
    public void Crear_automatico_requires_tiempo_inicio()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Automatico, null, 1, 10));
    }

    [Fact]
    public void Crear_manual_rejects_tiempo_inicio()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, DateTime.UtcNow, 1, 10));
    }

    [Fact]
    public void Crear_rejects_maximos_below_minimos()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 5, 2));
    }

    [Fact]
    public void AgregarJuego_appends_reference()
    {
        var partida = CrearManual();
        var juegoId = JuegoId.New();
        partida.AgregarJuego(juegoId, 1, TipoJuego.Trivia);
        Assert.Single(partida.Juegos);
        Assert.Equal(juegoId, partida.Juegos[0].JuegoId);
        Assert.Equal(TipoJuego.Trivia, partida.Juegos[0].TipoJuego);
    }

    [Fact]
    public void AgregarJuego_rejects_orden_below_one()
    {
        var partida = CrearManual();
        Assert.Throws<ArgumentException>(() => partida.AgregarJuego(JuegoId.New(), 0, TipoJuego.Trivia));
    }

    [Fact]
    public void AgregarJuego_rejects_duplicate_orden()
    {
        var partida = CrearManual();
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia);
        Assert.Throws<OrdenJuegoDuplicadoException>(() => partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.BusquedaDelTesoro));
    }

    [Fact]
    public void AgregarJuego_rejects_duplicate_juego_id()
    {
        var partida = CrearManual();
        var juegoId = JuegoId.New();
        partida.AgregarJuego(juegoId, 1, TipoJuego.Trivia);
        Assert.Throws<JuegoDuplicadoException>(() => partida.AgregarJuego(juegoId, 2, TipoJuego.Trivia));
    }

    [Fact]
    public void ValidarIntegridadJuegos_throws_when_no_games()
    {
        Assert.Throws<PartidaSinJuegosException>(() => CrearManual().ValidarIntegridadJuegos());
    }

    [Fact]
    public void ValidarIntegridadJuegos_throws_when_orden_not_contiguous()
    {
        var partida = CrearManual();
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia);
        partida.AgregarJuego(JuegoId.New(), 3, TipoJuego.Trivia); // gap
        Assert.Throws<OrdenJuegosNoContiguoException>(() => partida.ValidarIntegridadJuegos());
    }

    [Fact]
    public void ValidarIntegridadJuegos_passes_for_contiguous_orden()
    {
        var partida = CrearManual();
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia);
        partida.AgregarJuego(JuegoId.New(), 2, TipoJuego.BusquedaDelTesoro);
        partida.ValidarIntegridadJuegos(); // no throw
    }

    [Fact]
    public void Crear_manualYAutomatico_requires_tiempo_inicio()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.ManualYAutomatico, null, 1, 10));
    }

    [Fact]
    public void Crear_manualYAutomatico_with_tiempo_inicio_succeeds()
    {
        var partida = Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.ManualYAutomatico, DateTime.UtcNow, 1, 10);
        Assert.True(partida.PartidaId.EsValido());
    }

    [Fact]
    public void Crear_rejects_minimos_below_one()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 0, 5));
    }
}
