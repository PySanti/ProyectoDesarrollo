using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Domain;

public class JuegoBDTTests
{
    private static EtapaSpec Etapa(int orden, string qr = "QR-TEXT") => new(orden, qr, 50, 120);

    private static JuegoBDT CrearValido() =>
        JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central", new[] { Etapa(1) });

    [Fact]
    public void Crear_builds_game_with_stages_and_pendiente_state()
    {
        var juego = CrearValido();
        Assert.True(juego.JuegoId.EsValido());
        Assert.Equal(EstadoJuego.Pendiente, juego.Estado);
        Assert.Equal("Plaza central", juego.AreaBusqueda);
        Assert.Single(juego.Etapas);
        Assert.Equal(50, juego.Etapas[0].PuntajeAsignado.Valor);
        Assert.Equal("QR-TEXT", juego.Etapas[0].CodigoQREsperado);
    }

    [Fact]
    public void Crear_rejects_blank_area_busqueda()
    {
        Assert.Throws<AreaBusquedaRequeridaException>(() =>
            JuegoBDT.Crear(PartidaId.New(), 1, "  ", new[] { Etapa(1) }));
    }

    [Fact]
    public void Crear_rejects_empty_stage_list()
    {
        Assert.Throws<JuegoBDTSinEtapasException>(() =>
            JuegoBDT.Crear(PartidaId.New(), 1, "Plaza", Enumerable.Empty<EtapaSpec>()));
    }

    [Fact]
    public void AgregarEtapa_rejects_blank_codigo_qr()
    {
        var juego = CrearValido();
        Assert.Throws<EtapaBDTInvalidaException>(() => juego.AgregarEtapa(2, "  ", 50, 120));
    }

    [Fact]
    public void AgregarEtapa_rejects_non_positive_puntaje()
    {
        var juego = CrearValido();
        Assert.Throws<EtapaBDTInvalidaException>(() => juego.AgregarEtapa(2, "QR", 0, 120));
    }

    [Fact]
    public void AgregarEtapa_rejects_non_positive_time_limit()
    {
        var juego = CrearValido();
        Assert.Throws<EtapaBDTInvalidaException>(() => juego.AgregarEtapa(2, "QR", 50, 0));
    }

    [Fact]
    public void Crear_rejects_non_contiguous_stage_orden()
    {
        Assert.Throws<EtapaBDTInvalidaException>(() =>
            JuegoBDT.Crear(PartidaId.New(), 1, "Plaza", new[] { Etapa(1), Etapa(3) }));
    }

    [Fact]
    public void AgregarEtapa_rejects_non_positive_orden()
    {
        var juego = CrearValido();
        Assert.Throws<EtapaBDTInvalidaException>(() => juego.AgregarEtapa(0, "QR", 50, 120));
    }
}
