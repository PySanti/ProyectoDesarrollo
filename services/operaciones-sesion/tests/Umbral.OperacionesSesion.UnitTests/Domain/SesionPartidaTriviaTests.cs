// SesionPartidaTriviaTests.cs
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class SesionPartidaTriviaTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot P(int orden, out Guid correcta)
    {
        var ok = new OpcionSnapshot(Guid.NewGuid(), "ok", true);
        var no = new OpcionSnapshot(Guid.NewGuid(), "no", false);
        correcta = ok.OpcionId;
        return new PreguntaSnapshot(Guid.NewGuid(), orden, $"Q{orden}", 10, 30, new[] { ok, no });
    }

    private static SesionPartida IniciadaConTrivia(Guid partidaId, Guid participante, params PreguntaSnapshot[] preguntas)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, preguntas);
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        var insc = sesion.Inscribir(participante, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        sesion.Iniciar(T0); // juego Trivia activo, Q1 activa
        return sesion;
    }

    [Fact]
    public void Responder_correcta_cierra_y_devuelve_puntaje_y_juegoId()
    {
        var part = Guid.NewGuid();
        var p1 = P(1, out var correcta);
        var sesion = IniciadaConTrivia(Guid.NewGuid(), part, p1);
        var juegoId = sesion.Juegos.Single().JuegoId;

        var r = sesion.ResponderPregunta(part, correcta, T0.AddSeconds(3));

        Assert.True(r.EsCorrecta);
        Assert.True(r.CerroPregunta);
        Assert.Equal(10, r.Puntaje);
        Assert.Equal(juegoId, r.JuegoId);
        Assert.Equal(p1.PreguntaId, r.PreguntaId);
    }

    [Fact]
    public void Responder_sin_inscripcion_lanza_no_inscrito()
    {
        var p1 = P(1, out var correcta);
        var sesion = IniciadaConTrivia(Guid.NewGuid(), Guid.NewGuid(), p1);
        Assert.Throws<ParticipanteNoInscritoException>(
            () => sesion.ResponderPregunta(Guid.NewGuid(), correcta, T0.AddSeconds(1)));
    }

    // FU2: el guard de inscripción (403) ahora precede a cualquier check de estado de juego (409),
    // incluso a SesionNoIniciadaException. Un participante no inscrito en una sesión Lobby
    // obtiene 403, no 409. El test fue actualizado de SesionNoIniciadaException a
    // ParticipanteNoInscritoException para reflejar el nuevo orden correcto.
    [Fact]
    public void Responder_sin_inscripcion_en_lobby_lanza_403_antes_que_SesionNoIniciada()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { P(1, out _) });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap); // Lobby, sin inscripciones
        Assert.Throws<ParticipanteNoInscritoException>(
            () => sesion.ResponderPregunta(Guid.NewGuid(), Guid.NewGuid(), T0));
    }

    [Fact]
    public void Avanzar_cierra_y_activa_siguiente()
    {
        var part = Guid.NewGuid();
        var sesion = IniciadaConTrivia(Guid.NewGuid(), part, P(1, out _), P(2, out _));
        var r = sesion.AvanzarPregunta(T0.AddSeconds(5));
        Assert.Equal(1, r.PreguntaCerradaOrden);
        Assert.Equal(MotivoCierrePregunta.AvanceOperador, r.MotivoCierre);
        Assert.Equal(2, r.PreguntaActivadaOrden);
        Assert.False(r.SinMasPreguntas);
    }

    [Fact]
    public void Avanzar_pasado_el_limite_cierra_por_tiempo()
    {
        var sesion = IniciadaConTrivia(Guid.NewGuid(), Guid.NewGuid(), P(1, out _), P(2, out _));
        var r = sesion.AvanzarPregunta(T0.AddSeconds(31)); // límite 30
        Assert.Equal(MotivoCierrePregunta.Tiempo, r.MotivoCierre);
    }

    [Fact]
    public void Avanzar_en_ultima_reporta_sin_mas_preguntas()
    {
        var sesion = IniciadaConTrivia(Guid.NewGuid(), Guid.NewGuid(), P(1, out _));
        var r = sesion.AvanzarPregunta(T0.AddSeconds(5));
        Assert.True(r.SinMasPreguntas);
        Assert.Null(r.PreguntaActivadaOrden);
    }

    [Fact]
    public void Finalizar_con_preguntas_abiertas_lanza()
    {
        var sesion = IniciadaConTrivia(Guid.NewGuid(), Guid.NewGuid(), P(1, out _));
        Assert.Throws<JuegoConPreguntasPendientesException>(() => sesion.FinalizarJuegoActual(T0.AddSeconds(5)));
    }

    [Fact]
    public void Agotar_preguntas_luego_finalizar_termina_la_sesion()
    {
        var sesion = IniciadaConTrivia(Guid.NewGuid(), Guid.NewGuid(), P(1, out _));
        sesion.AvanzarPregunta(T0.AddSeconds(5)); // cierra Q1, sin más
        var avance = sesion.FinalizarJuegoActual(T0.AddSeconds(6)); // único juego → Terminada
        Assert.True(avance.Terminada());
        Assert.Equal(EstadoSesion.Terminada, sesion.Estado);
    }

    [Fact]
    public void Responder_correcta_auto_activa_la_siguiente_pregunta()
    {
        var part = Guid.NewGuid();
        var p1 = P(1, out var c1);
        var p2 = P(2, out _);
        var sesion = IniciadaConTrivia(Guid.NewGuid(), part, p1, p2);

        sesion.ResponderPregunta(part, c1, T0.AddSeconds(3));

        var juego = sesion.Juegos.Single();
        Assert.Equal(EstadoPregunta.Cerrada, juego.Preguntas.Single(p => p.Orden == 1).Estado);
        Assert.NotNull(juego.PreguntaActiva);
        Assert.Equal(2, juego.PreguntaActiva!.Orden);
    }

    [Fact]
    public void Responder_correcta_en_ultima_no_deja_pregunta_activa_y_permite_finalizar()
    {
        var part = Guid.NewGuid();
        var p1 = P(1, out var c1);
        var sesion = IniciadaConTrivia(Guid.NewGuid(), part, p1);

        sesion.ResponderPregunta(part, c1, T0.AddSeconds(3));

        Assert.Null(sesion.Juegos.Single().PreguntaActiva);
        var avance = sesion.FinalizarJuegoActual(T0.AddSeconds(4));
        Assert.True(avance.Terminada());
    }

    // FU2: 403-antes-409 — el chequeo de inscripción debe preceder al de estado del juego.
    // Si no se moviera el guard, este test fallaría con NoHayPreguntaActivaException (409)
    // porque la única pregunta ya fue avanzada por el operador antes de que llegue el intruso.
    [Fact]
    public void Responder_sin_inscripcion_lanza_403_aunque_no_haya_pregunta_activa()
    {
        var part = Guid.NewGuid();
        var intruso = Guid.NewGuid();
        var p1 = P(1, out _);
        var sesion = IniciadaConTrivia(Guid.NewGuid(), part, p1);
        var tAvanzado = T0.AddSeconds(5);

        // Operador avanza la única pregunta → ya no hay pregunta activa (daría NoHayPreguntaActivaException/409 si llegara)
        sesion.AvanzarPregunta(tAvanzado);

        // El intruso llama ResponderPregunta: debe obtener 403 (inscripción) antes que 409 (sin pregunta activa)
        Assert.Throws<ParticipanteNoInscritoException>(() =>
            sesion.ResponderPregunta(intruso, Guid.NewGuid(), tAvanzado.AddSeconds(1)));
    }
}
