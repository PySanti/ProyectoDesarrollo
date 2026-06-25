using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Umbral.Partidas.Application;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Validators;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.UnitTests.Application;

// Validation now runs in the MediatR pipeline (doctrine audit M-2). These tests own the
// invalid-input -> ValidationException coverage that previously lived in the controller;
// the middleware maps ValidationException to 400 end-to-end (contract suite).
public class ValidationBehaviorTests
{
    [Fact]
    public async Task Throws_validation_exception_for_invalid_crear_partida()
    {
        var behavior = new ValidationBehavior<CrearPartidaCommand, CrearPartidaResponse>(
            new IValidator<CrearPartidaCommand>[] { new CrearPartidaCommandValidator() });
        var command = new CrearPartidaCommand("", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(command, () => Task.FromResult(new CrearPartidaResponse(Guid.NewGuid())), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_validation_exception_for_trivia_without_preguntas()
    {
        var behavior = new ValidationBehavior<AgregarJuegoTriviaCommand, AgregarJuegoResponse>(
            new IValidator<AgregarJuegoTriviaCommand>[] { new AgregarJuegoTriviaCommandValidator() });
        var command = new AgregarJuegoTriviaCommand(Guid.NewGuid(), 1, new List<PreguntaRequest>());

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(command, () => Task.FromResult(new AgregarJuegoResponse(Guid.NewGuid())), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_validation_exception_for_bdt_without_etapas()
    {
        var behavior = new ValidationBehavior<AgregarJuegoBDTCommand, AgregarJuegoResponse>(
            new IValidator<AgregarJuegoBDTCommand>[] { new AgregarJuegoBDTCommandValidator() });
        var command = new AgregarJuegoBDTCommand(Guid.NewGuid(), 1, "Plaza", new List<EtapaRequest>());

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(command, () => Task.FromResult(new AgregarJuegoResponse(Guid.NewGuid())), CancellationToken.None));
    }

    [Fact]
    public async Task Calls_next_when_valid()
    {
        var behavior = new ValidationBehavior<CrearPartidaCommand, CrearPartidaResponse>(
            new IValidator<CrearPartidaCommand>[] { new CrearPartidaCommandValidator() });
        var command = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);
        var expected = new CrearPartidaResponse(Guid.NewGuid());

        var result = await behavior.Handle(command, () => Task.FromResult(expected), CancellationToken.None);

        Assert.Same(expected, result);
    }
}
