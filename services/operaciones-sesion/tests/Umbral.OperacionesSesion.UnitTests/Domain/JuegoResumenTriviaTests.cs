// JuegoResumenTriviaTests.cs
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class JuegoResumenTriviaTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot P(int orden) =>
        new(Guid.NewGuid(), orden, $"Q{orden}", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });

    [Fact]
    public void Activar_trivia_activates_first_question_by_orden()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { P(2), P(1) });
        juego.Activar(T0);
        Assert.Equal(EstadoJuego.Activo, juego.Estado);
        Assert.NotNull(juego.PreguntaActiva);
        Assert.Equal(1, juego.PreguntaActiva!.Orden);
        Assert.True(juego.TienePreguntasAbiertas);
    }

    [Fact]
    public void Activar_siguiente_returns_next_then_null()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { P(1), P(2) });
        juego.Activar(T0); // Q1 activa
        juego.PreguntaActiva!.Cerrar(MotivoCierrePregunta.AvanceOperador, T0, null);
        var sig = juego.ActivarSiguientePregunta(T0);
        Assert.NotNull(sig);
        Assert.Equal(2, sig!.Orden);
        sig.Cerrar(MotivoCierrePregunta.AvanceOperador, T0, null);
        Assert.Null(juego.ActivarSiguientePregunta(T0));
        Assert.False(juego.TienePreguntasAbiertas);
    }

    [Fact]
    public void Trivia_without_questions_has_no_open_questions()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia); // sin preguntas
        juego.Activar(T0);
        Assert.Null(juego.PreguntaActiva);
        Assert.False(juego.TienePreguntasAbiertas);
    }
}
