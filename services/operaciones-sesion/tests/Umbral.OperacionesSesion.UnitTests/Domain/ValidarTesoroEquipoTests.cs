using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class ValidarTesoroEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private sealed class TextoQrDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) =>
            imagen.Length == 0 ? null : System.Text.Encoding.UTF8.GetString(imagen);
    }

    private static byte[] Img(string texto) => System.Text.Encoding.UTF8.GetBytes(texto);

    // Sesión BDT Equipo iniciada: equipo A (líder + miembro aceptados, convocadoPendiente sin responder),
    // equipo B (solo líder aceptado). 2 etapas para observar auto-avance.
    // Nota: out params no pueden capturarse en lambdas (CS1628); copias locales, asignación al final.
    private static SesionPartida SesionBdtEquipoIniciada(
        out Guid liderA, out Guid miembroA, out Guid convocadoPendienteA, out Guid equipoA,
        out Guid liderB, out Guid equipoB)
    {
        var liderALocal = Guid.NewGuid(); var miembroALocal = Guid.NewGuid();
        var pendienteALocal = Guid.NewGuid(); var equipoALocal = Guid.NewGuid();
        var liderBLocal = Guid.NewGuid(); var equipoBLocal = Guid.NewGuid();

        var etapas = new[]
        {
            new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60),
            new EtapaSnapshot(Guid.NewGuid(), 2, "QR-2", 30, 60)
        };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio central", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);

        var insA = sesion.PreinscribirEquipo(equipoALocal, true, liderALocal, new[] { liderALocal, miembroALocal, pendienteALocal }, false, 0, T0);
        sesion.AceptarInscripcion(insA.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(insA.Convocatorias.Single(c => c.UsuarioId == liderALocal).Id.Valor, liderALocal, true, false, T0);
        sesion.ResponderConvocatoria(insA.Convocatorias.Single(c => c.UsuarioId == miembroALocal).Id.Valor, miembroALocal, true, false, T0);
        // pendienteALocal NO responde su convocatoria.
        var insB = sesion.PreinscribirEquipo(equipoBLocal, true, liderBLocal, new[] { liderBLocal }, false, 1, T0);
        sesion.AceptarInscripcion(insB.Id.Valor, 1, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(insB.Convocatorias.Single(c => c.UsuarioId == liderBLocal).Id.Valor, liderBLocal, true, false, T0);

        sesion.Iniciar(T0);
        liderA = liderALocal; miembroA = miembroALocal; convocadoPendienteA = pendienteALocal; equipoA = equipoALocal;
        liderB = liderBLocal; equipoB = equipoBLocal;
        return sesion;
    }

    [Fact]
    public void Miembro_aceptado_qr_valido_gana_etapa_para_el_equipo()
    {
        var sesion = SesionBdtEquipoIniciada(out _, out var miembroA, out _, out var equipoA, out _, out _);

        var r = sesion.ValidarTesoro(miembroA, Img("QR-1"), T0.AddSeconds(5), new TextoQrDecoder());

        Assert.Equal(ResultadoValidacionQR.Valido, r.Resultado);
        Assert.True(r.Gano);
        Assert.Equal(50, r.Puntaje);
        Assert.Equal(equipoA, r.EquipoId);
        Assert.Equal(equipoA, r.GanadorEquipoId);
        Assert.Equal(miembroA, r.ParticipanteId);
        var juego = sesion.Juegos.Single();
        var etapa1 = juego.Etapas.Single(e => e.Orden == 1);
        Assert.Equal(EstadoEtapa.Ganada, etapa1.Estado);
        Assert.Equal(miembroA, etapa1.GanadorParticipanteId);
        Assert.Equal(equipoA, etapa1.GanadorEquipoId);
        Assert.Equal(2, juego.EtapaActiva!.Orden); // cierre global + auto-avance
    }

    [Fact]
    public void Qr_invalido_no_sella_ambos_miembros_reintentan()
    {
        var sesion = SesionBdtEquipoIniciada(out var liderA, out var miembroA, out _, out var equipoA, out _, out _);

        var r1 = sesion.ValidarTesoro(liderA, Img("QR-MALO"), T0.AddSeconds(2), new TextoQrDecoder());
        Assert.False(r1.Gano);
        Assert.Equal(equipoA, r1.EquipoId);
        Assert.Null(r1.GanadorEquipoId);

        // Mismo miembro reintenta, luego otro miembro del mismo equipo — sin excepción (sin dedup).
        var r2 = sesion.ValidarTesoro(liderA, Img("QR-MALO"), T0.AddSeconds(3), new TextoQrDecoder());
        Assert.False(r2.Gano);
        var r3 = sesion.ValidarTesoro(miembroA, Img("QR-1"), T0.AddSeconds(4), new TextoQrDecoder());
        Assert.True(r3.Gano);

        var etapa1 = sesion.Juegos.Single().Etapas.Single(e => e.Orden == 1);
        Assert.Equal(3, etapa1.Tesoros.Count);
        Assert.All(etapa1.Tesoros, t => Assert.Equal(equipoA, t.EquipoId));
    }

    [Fact]
    public void Convocado_pendiente_no_puede_validar()
    {
        var sesion = SesionBdtEquipoIniciada(out _, out _, out var pendienteA, out _, out _, out _);

        Assert.Throws<ParticipanteNoInscritoException>(
            () => sesion.ValidarTesoro(pendienteA, Img("QR-1"), T0.AddSeconds(5), new TextoQrDecoder()));
    }

    [Fact]
    public void Convocado_rechazado_no_puede_validar()
    {
        var rechazado = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var lider = Guid.NewGuid();
        var ins = sesion.PreinscribirEquipo(Guid.NewGuid(), true, lider, new[] { lider, rechazado }, false, 0, T0);
        sesion.AceptarInscripcion(ins.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(ins.Convocatorias.Single(c => c.UsuarioId == lider).Id.Valor, lider, true, false, T0);
        sesion.ResponderConvocatoria(ins.Convocatorias.Single(c => c.UsuarioId == rechazado).Id.Valor, rechazado, false, false, T0);
        sesion.Iniciar(T0);

        Assert.Throws<ParticipanteNoInscritoException>(
            () => sesion.ValidarTesoro(rechazado, Img("QR-1"), T0.AddSeconds(5), new TextoQrDecoder()));
    }

    [Fact]
    public void Qr_valido_de_equipo_A_cierra_etapa_para_equipo_B()
    {
        var sesion = SesionBdtEquipoIniciada(out var liderA, out _, out _, out var equipoA, out var liderB, out var equipoB);

        var rA = sesion.ValidarTesoro(liderA, Img("QR-1"), T0.AddSeconds(5), new TextoQrDecoder());
        Assert.True(rA.Gano);

        // La etapa 1 quedó Ganada para todos; B ahora valida contra la etapa 2 activa.
        var rB = sesion.ValidarTesoro(liderB, Img("QR-2"), T0.AddSeconds(10), new TextoQrDecoder());
        Assert.True(rB.Gano);
        Assert.Equal(equipoB, rB.GanadorEquipoId);
        var etapa1 = sesion.Juegos.Single().Etapas.Single(e => e.Orden == 1);
        Assert.Equal(equipoA, etapa1.GanadorEquipoId);
    }

    [Fact]
    public void Individual_regression_equipoid_null_en_todo_el_flujo()
    {
        var jugador = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var insc = sesion.Inscribir(jugador, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        sesion.Iniciar(T0);

        var r = sesion.ValidarTesoro(jugador, Img("QR-1"), T0.AddSeconds(5), new TextoQrDecoder());

        Assert.True(r.Gano);
        Assert.Null(r.EquipoId);
        Assert.Null(r.GanadorEquipoId);
        var etapa1 = sesion.Juegos.Single().Etapas.Single(e => e.Orden == 1);
        Assert.Null(etapa1.GanadorEquipoId);
        Assert.Null(etapa1.Tesoros.Single().EquipoId);
    }
}
