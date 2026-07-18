using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Domain;

public class JuegoTriviaTests
{
    private static PreguntaSpec ValidPregunta(string texto = "Capital de Francia?") =>
        new(texto,
            new List<OpcionSpec> { new("Paris", true), new("Londres", false) },
            10, 30);

    [Fact]
    public void Crear_builds_game_with_questions_and_pendiente_state()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.True(juego.JuegoId.EsValido());
        Assert.Equal(EstadoJuego.Pendiente, juego.Estado);
        Assert.Single(juego.Preguntas);
        Assert.Equal(2, juego.Preguntas[0].Opciones.Count);
        Assert.Equal(10, juego.Preguntas[0].PuntajeAsignado.Valor);
    }

    [Fact]
    public void Crear_assigns_sequential_orden_to_preguntas_and_opciones()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[]
        {
            new PreguntaSpec("Q0", new List<OpcionSpec> { new("A", true), new("B", false), new("C", false) }, 10, 30),
            new PreguntaSpec("Q1", new List<OpcionSpec> { new("D", false), new("E", true) }, 10, 30),
        });

        Assert.Equal(new[] { 0, 1 }, juego.Preguntas.Select(p => p.Orden).ToArray());
        Assert.Equal(new[] { 0, 1, 2 }, juego.Preguntas[0].Opciones.Select(o => o.Orden).ToArray());
        Assert.Equal(new[] { 0, 1 }, juego.Preguntas[1].Opciones.Select(o => o.Orden).ToArray());
    }

    [Fact]
    public void Crear_rejects_empty_question_list()
    {
        Assert.Throws<JuegoTriviaSinPreguntasException>(() =>
            JuegoTrivia.Crear(PartidaId.New(), 1, Enumerable.Empty<PreguntaSpec>()));
    }

    [Fact]
    public void AgregarPregunta_rejects_blank_text()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("  ", new[] { ("A", true), ("B", false) }, 10, 30));
    }

    [Fact]
    public void AgregarPregunta_rejects_less_than_two_options()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("Q", new[] { ("only", true) }, 10, 30));
    }

    [Fact]
    public void AgregarPregunta_rejects_not_exactly_one_correct()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("Q", new[] { ("A", true), ("B", true) }, 10, 30));
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("Q", new[] { ("A", false), ("B", false) }, 10, 30));
    }

    [Fact]
    public void AgregarPregunta_rejects_non_positive_time_limit()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("Q", new[] { ("A", true), ("B", false) }, 10, 0));
    }

    [Fact]
    public void AgregarPregunta_rejects_non_positive_puntaje()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("Q", new[] { ("A", true), ("B", false) }, 0, 30));
    }

    [Fact]
    public void AgregarPregunta_rejects_blank_option_text()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("Q", new[] { ("  ", true), ("B", false) }, 10, 30));
    }
}
