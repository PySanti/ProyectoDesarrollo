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
    private static string NuevoQr() => Guid.NewGuid().ToString();

    private static EtapaSpec Etapa(int orden, string? qr = null) => new(orden, qr ?? NuevoQr(), 50, 120);

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
        Assert.True(Guid.TryParse(juego.Etapas[0].CodigoQREsperado, out _));
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
        Assert.Throws<EtapaBDTInvalidaException>(() => juego.AgregarEtapa(2, NuevoQr(), 0, 120));
    }

    [Fact]
    public void AgregarEtapa_rejects_non_positive_time_limit()
    {
        var juego = CrearValido();
        Assert.Throws<EtapaBDTInvalidaException>(() => juego.AgregarEtapa(2, NuevoQr(), 50, 0));
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

    [Theory]
    [InlineData("hola")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("QR-TEXT")]
    [InlineData("12345678-1234-1234-1234-12345678")]
    public void Crear_rechaza_un_codigo_que_no_es_uuid(string codigo)
    {
        Assert.Throws<EtapaBDTInvalidaException>(
            () => JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central", new[] { Etapa(1, codigo) }));
    }

    // El codigo se guarda TAL CUAL: Operaciones compara el texto de forma exacta contra el QR
    // impreso. Normalizar el casing aqui haria que ningun tesoro validara jamas.
    [Fact]
    public void Crear_guarda_el_codigo_literal_sin_normalizar()
    {
        var codigo = Guid.NewGuid().ToString().ToUpperInvariant();

        var juego = JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central", new[] { Etapa(1, codigo) });

        Assert.Equal(codigo, juego.Etapas[0].CodigoQREsperado);
    }

    // Sin esto, el QR de la etapa 1 ganaria tambien la etapa 2: ClasificarQr compara el texto
    // decodificado contra el de la etapa activa, y dos etapas con el mismo codigo son
    // indistinguibles.
    [Fact]
    public void Crear_rechaza_dos_etapas_con_el_mismo_codigo()
    {
        var codigo = Guid.NewGuid().ToString();

        Assert.Throws<EtapaBDTInvalidaException>(
            () => JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central",
                new[] { Etapa(1, codigo), Etapa(2, codigo) }));
    }

    [Fact]
    public void Crear_rechaza_dos_codigos_que_solo_difieren_en_mayusculas()
    {
        var codigo = Guid.NewGuid().ToString();

        Assert.Throws<EtapaBDTInvalidaException>(
            () => JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central",
                new[] { Etapa(1, codigo.ToLowerInvariant()), Etapa(2, codigo.ToUpperInvariant()) }));
    }

    [Fact]
    public void Crear_acepta_dos_etapas_con_codigos_distintos()
    {
        var juego = JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central",
            new[] { Etapa(1), Etapa(2) });

        Assert.Equal(2, juego.Etapas.Count);
    }
}
