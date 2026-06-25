using System;
using System.Collections.Generic;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Validators;

namespace Umbral.Partidas.UnitTests.Application;

public class AgregarJuegoTriviaCommandValidatorTests
{
    private readonly AgregarJuegoTriviaCommandValidator _validator = new();

    private static AgregarJuegoTriviaCommand WithQuestions(IReadOnlyList<PreguntaRequest> preguntas)
        => new(Guid.NewGuid(), 1, preguntas);

    [Fact]
    public void Valid_command_passes()
    {
        var cmd = WithQuestions(new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", true), new("B", false) }, 10, 30)
        });
        Assert.True(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Empty_questions_fails()
    {
        Assert.False(_validator.Validate(WithQuestions(new List<PreguntaRequest>())).IsValid);
    }

    [Fact]
    public void Question_without_two_options_fails()
    {
        var cmd = WithQuestions(new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", true) }, 10, 30)
        });
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Question_without_exactly_one_correct_fails()
    {
        var cmd = WithQuestions(new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", true), new("B", true) }, 10, 30)
        });
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Question_with_zero_correct_options_fails()
    {
        var cmd = WithQuestions(new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", false), new("B", false) }, 10, 30)
        });
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Non_positive_puntaje_fails()
    {
        var cmd = WithQuestions(new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", true), new("B", false) }, 0, 30)
        });
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Non_positive_time_fails()
    {
        var cmd = WithQuestions(new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", true), new("B", false) }, 10, 0)
        });
        Assert.False(_validator.Validate(cmd).IsValid);
    }
}
