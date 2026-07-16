using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class PrepararPistaEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida SesionBdtEquipoIniciada(out Guid equipoA)
    {
        var lider = Guid.NewGuid();
        var equipoALocal = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var ins = sesion.PreinscribirEquipo(equipoALocal, true, lider, new[] { lider }, false, 0, T0);
        sesion.AceptarInscripcion(ins.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(ins.Convocatorias.Single().Id.Valor, lider, true, false, T0);
        sesion.Iniciar(T0);
        equipoA = equipoALocal;
        return sesion;
    }

    [Fact]
    public void Equipo_inscrito_devuelve_juegoid()
    {
        var sesion = SesionBdtEquipoIniciada(out var equipoA);

        var juegoId = sesion.PrepararPistaEquipo(equipoA);

        Assert.Equal(sesion.Juegos.Single().JuegoId, juegoId);
    }

    [Fact]
    public void Sesion_individual_lanza_modalidad_no_soportada()
    {
        var jugador = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        sesion.Inscribir(jugador, false, 0, T0);
        sesion.Iniciar(T0);

        Assert.Throws<ModalidadNoSoportadaException>(() => sesion.PrepararPistaEquipo(Guid.NewGuid()));
    }

    [Fact]
    public void Equipo_no_inscrito_lanza_inscripcion_no_encontrada()
    {
        var sesion = SesionBdtEquipoIniciada(out _);

        Assert.Throws<InscripcionNoEncontradaException>(() => sesion.PrepararPistaEquipo(Guid.NewGuid()));
    }

    [Fact]
    public void Sin_etapa_activa_lanza_no_hay_etapa_activa()
    {
        var sesion = SesionBdtEquipoIniciada(out var equipoA);
        sesion.AvanzarEtapa(T0.AddSeconds(5)); // cierra la única etapa; no queda activa

        Assert.Throws<NoHayEtapaActivaException>(() => sesion.PrepararPistaEquipo(equipoA));
    }
}
