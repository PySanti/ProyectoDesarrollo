using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class SesionPartidaAprobacionTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Individual(int min = 1, int max = 5)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Individual, ModoInicioPartida.Manual, null, min, max,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    private static SesionPartida Equipo(int min = 1, int max = 5)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Equipo, ModoInicioPartida.Manual, null, min, max,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public void Inscribir_nace_pendiente_y_no_cuenta_como_activa()
    {
        var s = Individual();
        var insc = s.Inscribir(Guid.NewGuid(), tieneParticipacionActivaEnOtra: false, inscritosActivos: 0, T0);

        Assert.Equal(EstadoInscripcion.Pendiente, insc.Estado);
        Assert.Equal(0, s.Inscripciones.Count(i => i.EsActiva));
    }

    [Fact]
    public void Aceptar_pendiente_individual_pasa_a_activa()
    {
        var s = Individual();
        var insc = s.Inscribir(Guid.NewGuid(), false, 0, T0);

        var creadas = s.AceptarInscripcion(insc.Id.Valor, inscritosActivos: 0, T0);

        Assert.True(insc.EsActiva);
        Assert.Empty(creadas);
    }

    [Fact]
    public void Aceptar_pendiente_equipo_crea_convocatorias_con_partidaId_correcto()
    {
        var s = Equipo();
        var m1 = Guid.NewGuid();
        var insc = s.PreinscribirEquipo(Guid.NewGuid(), callerEsLider: true, new[] { m1 },
            equipoTieneParticipacionActivaEnOtra: false, equiposActivos: 0, T0);

        var creadas = s.AceptarInscripcion(insc.Id.Valor, inscritosActivos: 0, T0);

        var c = Assert.Single(creadas);
        Assert.Equal(m1, c.UsuarioId);
        Assert.Equal(s.PartidaId, c.PartidaId);
        Assert.True(insc.EsActiva);
    }

    [Fact]
    public void Aceptar_con_cupo_de_activos_lleno_lanza_CupoLleno()
    {
        var s = Individual(max: 1);
        var insc = s.Inscribir(Guid.NewGuid(), false, 0, T0);

        Assert.Throws<CupoLlenoException>(
            () => s.AceptarInscripcion(insc.Id.Valor, inscritosActivos: 1, T0));
        Assert.True(insc.EstaPendiente); // sin efecto
    }

    [Fact]
    public void Aceptar_inscripcion_inexistente_lanza_NoEncontrada()
    {
        var s = Individual();
        Assert.Throws<InscripcionNoEncontradaException>(
            () => s.AceptarInscripcion(Guid.NewGuid(), 0, T0));
    }

    [Fact]
    public void Aceptar_una_ya_activa_lanza_NoPendiente()
    {
        var s = Individual();
        var insc = s.Inscribir(Guid.NewGuid(), false, 0, T0);
        s.AceptarInscripcion(insc.Id.Valor, 0, T0);

        Assert.Throws<InscripcionNoPendienteException>(
            () => s.AceptarInscripcion(insc.Id.Valor, 0, T0));
    }

    [Fact]
    public void Rechazar_pendiente_pasa_a_rechazada_y_devuelve_equipoId()
    {
        var s = Equipo();
        var equipoId = Guid.NewGuid();
        var insc = s.PreinscribirEquipo(equipoId, true, new[] { Guid.NewGuid() }, false, 0, T0);

        var (inscId, equipo) = s.RechazarInscripcion(insc.Id.Valor, T0);

        Assert.Equal(insc.Id.Valor, inscId);
        Assert.Equal(equipoId, equipo);
        Assert.Equal(EstadoInscripcion.Rechazada, insc.Estado);
    }

    [Fact]
    public void Rechazada_no_bloquea_reinscribir_al_mismo_participante()
    {
        var s = Individual();
        var participante = Guid.NewGuid();
        var insc1 = s.Inscribir(participante, false, 0, T0);
        s.RechazarInscripcion(insc1.Id.Valor, T0);

        var insc2 = s.Inscribir(participante, false, 0, T0); // no lanza ParticipanteYaInscrito
        Assert.Equal(EstadoInscripcion.Pendiente, insc2.Estado);
    }

    [Fact]
    public void Pendiente_bloquea_reinscribir_al_mismo_participante()
    {
        var s = Individual();
        var participante = Guid.NewGuid();
        s.Inscribir(participante, false, 0, T0);

        Assert.Throws<ParticipanteYaInscritoException>(
            () => s.Inscribir(participante, false, 0, T0));
    }

    [Fact]
    public void Inscribir_con_cupo_de_activos_lleno_lanza_CupoLleno()
    {
        var s = Individual(max: 1);
        Assert.Throws<CupoLlenoException>(
            () => s.Inscribir(Guid.NewGuid(), false, inscritosActivos: 1, T0));
    }
}
