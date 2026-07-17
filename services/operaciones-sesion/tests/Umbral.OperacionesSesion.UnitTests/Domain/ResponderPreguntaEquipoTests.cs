using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class ResponderPreguntaEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid OpcionOk = Guid.NewGuid();
    private static readonly Guid OpcionMal = Guid.NewGuid();

    private static SesionPartida SesionEquipoIniciada(
        out Guid liderA, out Guid miembroA, out Guid equipoA,
        out Guid liderB, out Guid equipoB)
    {
        // Nota: out params no pueden capturarse en lambdas (CS1628); se usan copias locales
        // y se asignan a los out al final (helper de test, no toca firmas de producción).
        var liderALocal = Guid.NewGuid(); var miembroALocal = Guid.NewGuid(); var equipoALocal = Guid.NewGuid();
        var liderBLocal = Guid.NewGuid(); var equipoBLocal = Guid.NewGuid();
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(OpcionOk, "ok", true), new OpcionSnapshot(OpcionMal, "no", false) });
        var pregunta2 = new PreguntaSnapshot(Guid.NewGuid(), 2, "Q2", 10, 60,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta, pregunta2 });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);

        var insA = sesion.PreinscribirEquipo(equipoALocal, true, liderALocal, new[] { liderALocal, miembroALocal }, false, 0, T0);
        sesion.AceptarInscripcion(insA.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(insA.Convocatorias.Single(c => c.UsuarioId == liderALocal).Id.Valor, liderALocal, true, false, T0);
        sesion.ResponderConvocatoria(insA.Convocatorias.Single(c => c.UsuarioId == miembroALocal).Id.Valor, miembroALocal, true, false, T0);
        var insB = sesion.PreinscribirEquipo(equipoBLocal, true, liderBLocal, new[] { liderBLocal }, false, 1, T0);
        sesion.AceptarInscripcion(insB.Id.Valor, 1, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(insB.Convocatorias.Single(c => c.UsuarioId == liderBLocal).Id.Valor, liderBLocal, true, false, T0);

        sesion.Iniciar(T0);
        liderA = liderALocal; miembroA = miembroALocal; equipoA = equipoALocal;
        liderB = liderBLocal; equipoB = equipoBLocal;
        return sesion;
    }

    [Fact]
    public void Miembro_aceptado_responde_correcta_cierra_y_gana_el_equipo()
    {
        var sesion = SesionEquipoIniciada(out var liderA, out _, out var equipoA, out _, out _);

        var r = sesion.ResponderPregunta(liderA, OpcionOk, T0.AddSeconds(5));

        Assert.True(r.EsCorrecta);
        Assert.True(r.CerroPregunta);
        Assert.Equal(equipoA, r.EquipoId);
        Assert.Equal(liderA, r.ParticipanteId);
        var pregunta = sesion.Juegos.Single().Preguntas.Single(p => p.Orden == 1);
        Assert.Equal(equipoA, pregunta.GanadorEquipoId);
        Assert.Equal(liderA, pregunta.GanadorParticipanteId);
    }

    [Fact]
    public void Respuesta_incorrecta_sella_al_equipo_entero()
    {
        var sesion = SesionEquipoIniciada(out var liderA, out var miembroA, out var equipoA, out _, out _);

        var r1 = sesion.ResponderPregunta(liderA, OpcionMal, T0.AddSeconds(5));
        Assert.False(r1.EsCorrecta);
        Assert.Equal(equipoA, r1.EquipoId);

        Assert.Throws<RespuestaDuplicadaException>(
            () => sesion.ResponderPregunta(miembroA, OpcionOk, T0.AddSeconds(6)));
    }

    [Fact]
    public void Otro_equipo_puede_responder_tras_respuesta_incorrecta_del_primero()
    {
        var sesion = SesionEquipoIniciada(out var liderA, out _, out _, out var liderB, out var equipoB);

        sesion.ResponderPregunta(liderA, OpcionMal, T0.AddSeconds(5));
        var r = sesion.ResponderPregunta(liderB, OpcionOk, T0.AddSeconds(6));

        Assert.True(r.CerroPregunta);
        Assert.Equal(equipoB, r.EquipoId);
    }

    [Fact]
    public void Convocado_pendiente_no_puede_responder()
    {
        // equipo B con 2 miembros: líder acepta, el otro queda Pendiente
        var liderB = Guid.NewGuid(); var pendiente = Guid.NewGuid(); var equipoB = Guid.NewGuid();
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(OpcionOk, "ok", true), new OpcionSnapshot(OpcionMal, "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var ins = sesion.PreinscribirEquipo(equipoB, true, liderB, new[] { liderB, pendiente }, false, 0, T0);
        sesion.AceptarInscripcion(ins.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(ins.Convocatorias.Single(c => c.UsuarioId == liderB).Id.Valor, liderB, true, false, T0);
        sesion.Iniciar(T0);

        Assert.Throws<ParticipanteNoInscritoException>(
            () => sesion.ResponderPregunta(pendiente, OpcionOk, T0.AddSeconds(5)));
    }

    [Fact]
    public void Individual_sigue_registrando_sin_equipo()
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(OpcionOk, "ok", true), new OpcionSnapshot(OpcionMal, "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var p = Guid.NewGuid();
        var insc = sesion.Inscribir(p, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        sesion.Iniciar(T0);

        var r = sesion.ResponderPregunta(p, OpcionOk, T0.AddSeconds(5));

        Assert.Null(r.EquipoId);
        Assert.Null(sesion.Juegos.Single().Preguntas.Single().GanadorEquipoId);
    }
}
