using System;
using System.Linq;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.Events;
using Umbral.TriviaGame.Domain.ValueObjects;
using Xunit;

namespace Umbral.TriviaGame.Domain.Tests.Entities;

public class PartidaTriviaTests
{
    private static NombrePartida ValidNombre => NombrePartida.Create("Trivia Demo Sprint 1");
    private static TriviaFormId ValidFormularioId => TriviaFormId.New();
    private static OperatorId ValidOperatorId => OperatorId.Create("op-12345-abc");
    private static TiempoInicio ValidTiempoInicio => TiempoInicio.Create(DateTimeOffset.UtcNow.AddDays(1));
    private static CantidadMinima ValidMinimo => CantidadMinima.Create(2);
    private static CantidadMaximaJugadores ValidMaxJugadores =>
        CantidadMaximaJugadores.Create(10, ValidMinimo);
    private static CantidadMaximaEquipos ValidMaxEquipos =>
        CantidadMaximaEquipos.Create(5);
    private static JugadoresPorEquipoMin ValidMinPorEquipo => JugadoresPorEquipoMin.Create(2);
    private static JugadoresPorEquipoMax ValidMaxPorEquipo =>
        JugadoresPorEquipoMax.Create(5, ValidMinPorEquipo);

    // =====================================================================
    // Creación — Individual
    // =====================================================================

    [Fact]
    public void Create_Individual_ReturnsPartidaInLobby()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        Assert.Equal(PartidaEstado.Lobby, partida.Estado);
    }

    [Fact]
    public void Create_Individual_RaisesCreatedDomainEvent()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        var events = partida.FlushDomainEvents();
        Assert.Contains(events, e => e is PartidaTriviaCreadaDomainEvent);
    }

    [Fact]
    public void Create_Individual_StartedAtUtcIsNull()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        Assert.Null(partida.StartedAtUtc);
    }

    // =====================================================================
    // Creación — Equipo
    // =====================================================================

    [Fact]
    public void Create_Equipo_ReturnsPartidaInLobby()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Equipo, ModoInicio.Automatico,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: null,
            maximoEquipos: ValidMaxEquipos,
            minJugadoresPorEquipo: ValidMinPorEquipo,
            maxJugadoresPorEquipo: ValidMaxPorEquipo);

        Assert.Equal(PartidaEstado.Lobby, partida.Estado);
    }

    [Fact]
    public void Create_Equipo_RaisesCreatedDomainEvent()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Equipo, ModoInicio.Automatico,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: null,
            maximoEquipos: ValidMaxEquipos,
            minJugadoresPorEquipo: ValidMinPorEquipo,
            maxJugadoresPorEquipo: ValidMaxPorEquipo);

        var events = partida.FlushDomainEvents();
        Assert.Contains(events, e => e is PartidaTriviaCreadaDomainEvent);
    }

    // =====================================================================
    // Creación — Null argumentos
    // =====================================================================

    [Theory]
    [InlineData("nombre")]
    [InlineData("formularioId")]
    [InlineData("operatorId")]
    [InlineData("tiempoInicio")]
    [InlineData("minimoParticipantes")]
    public void Create_NullArgument_ThrowsDomainValidation(string paramName)
    {
        void Act()
        {
            var nombre = paramName == "nombre" ? null : ValidNombre;
            var formularioId = paramName == "formularioId" ? null : ValidFormularioId;
            var operatorId = paramName == "operatorId" ? null : ValidOperatorId;
            var tiempoInicio = paramName == "tiempoInicio" ? null : ValidTiempoInicio;
            var minimo = paramName == "minimoParticipantes" ? null : ValidMinimo;

            PartidaTrivia.Create(
                nombre!, Modalidad.Individual, ModoInicio.Manual,
                formularioId!, operatorId!, tiempoInicio!, minimo!,
                maximoJugadores: ValidMaxJugadores,
                maximoEquipos: null,
                minJugadoresPorEquipo: null,
                maxJugadoresPorEquipo: null);
        }

        Assert.Throws<DomainValidationException>(Act);
    }

    // =====================================================================
    // Creación — Modalidad inválida
    // =====================================================================

    [Fact]
    public void Create_IndividualSinMaxJugadores_ThrowsModalidadInvalida()
    {
        Assert.Throws<ModalidadInvalidaException>(() =>
            PartidaTrivia.Create(
                ValidNombre, Modalidad.Individual, ModoInicio.Manual,
                ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
                maximoJugadores: null,
                maximoEquipos: null,
                minJugadoresPorEquipo: null,
                maxJugadoresPorEquipo: null));
    }

    [Fact]
    public void Create_IndividualConMaxEquipos_ThrowsModalidadInvalida()
    {
        Assert.Throws<ModalidadInvalidaException>(() =>
            PartidaTrivia.Create(
                ValidNombre, Modalidad.Individual, ModoInicio.Manual,
                ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
                maximoJugadores: ValidMaxJugadores,
                maximoEquipos: ValidMaxEquipos,
                minJugadoresPorEquipo: null,
                maxJugadoresPorEquipo: null));
    }

    [Fact]
    public void Create_IndividualConLimitesPorEquipo_ThrowsModalidadInvalida()
    {
        Assert.Throws<ModalidadInvalidaException>(() =>
            PartidaTrivia.Create(
                ValidNombre, Modalidad.Individual, ModoInicio.Manual,
                ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
                maximoJugadores: ValidMaxJugadores,
                maximoEquipos: null,
                minJugadoresPorEquipo: ValidMinPorEquipo,
                maxJugadoresPorEquipo: ValidMaxPorEquipo));
    }

    [Fact]
    public void Create_EquipoSinMaxEquipos_ThrowsModalidadInvalida()
    {
        Assert.Throws<ModalidadInvalidaException>(() =>
            PartidaTrivia.Create(
                ValidNombre, Modalidad.Equipo, ModoInicio.Manual,
                ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
                maximoJugadores: null,
                maximoEquipos: null,
                minJugadoresPorEquipo: ValidMinPorEquipo,
                maxJugadoresPorEquipo: ValidMaxPorEquipo));
    }

    [Fact]
    public void Create_EquipoConMaxJugadores_ThrowsModalidadInvalida()
    {
        Assert.Throws<ModalidadInvalidaException>(() =>
            PartidaTrivia.Create(
                ValidNombre, Modalidad.Equipo, ModoInicio.Manual,
                ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
                maximoJugadores: ValidMaxJugadores,
                maximoEquipos: ValidMaxEquipos,
                minJugadoresPorEquipo: ValidMinPorEquipo,
                maxJugadoresPorEquipo: ValidMaxPorEquipo));
    }

    [Fact]
    public void Create_EquipoSinMinPorEquipo_ThrowsModalidadInvalida()
    {
        Assert.Throws<ModalidadInvalidaException>(() =>
            PartidaTrivia.Create(
                ValidNombre, Modalidad.Equipo, ModoInicio.Manual,
                ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
                maximoJugadores: null,
                maximoEquipos: ValidMaxEquipos,
                minJugadoresPorEquipo: null,
                maxJugadoresPorEquipo: ValidMaxPorEquipo));
    }

    [Fact]
    public void Create_EquipoSinMaxPorEquipo_ThrowsModalidadInvalida()
    {
        Assert.Throws<ModalidadInvalidaException>(() =>
            PartidaTrivia.Create(
                ValidNombre, Modalidad.Equipo, ModoInicio.Manual,
                ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
                maximoJugadores: null,
                maximoEquipos: ValidMaxEquipos,
                minJugadoresPorEquipo: ValidMinPorEquipo,
                maxJugadoresPorEquipo: null));
    }

    // =====================================================================
    // Iniciar
    // =====================================================================

    [Fact]
    public void Iniciar_ConInscriptosSuficientes_CambiaAIniciada()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        partida.Iniciar(cantidadInscriptos: 2);

        Assert.Equal(PartidaEstado.Iniciada, partida.Estado);
    }

    [Fact]
    public void Iniciar_RaisesIniciadaDomainEvent()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        partida.Iniciar(cantidadInscriptos: 2);

        Assert.Contains(partida.DomainEvents, e => e is PartidaTriviaIniciadaDomainEvent);
    }

    [Fact]
    public void Iniciar_SetsStartedAtUtc()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        partida.Iniciar(cantidadInscriptos: 2);

        Assert.NotNull(partida.StartedAtUtc);
    }

    [Fact]
    public void Iniciar_SinInscriptosSuficientes_ThrowsMinimosNoCumplidos()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        Assert.Throws<MinimosNoCumplidosException>(() =>
            partida.Iniciar(cantidadInscriptos: 1));
    }

    [Fact]
    public void Iniciar_PartidaNoEnLobby_ThrowsInvalidStateTransition()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);
        partida.Iniciar(cantidadInscriptos: 2);

        Assert.Throws<InvalidStateTransitionException>(() =>
            partida.Iniciar(cantidadInscriptos: 5));
    }

    // =====================================================================
    // Cancelar
    // =====================================================================

    [Fact]
    public void Cancelar_EnLobby_CambiaACancelada()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);
        partida.FlushDomainEvents();

        partida.Cancelar();

        Assert.Equal(PartidaEstado.Cancelada, partida.Estado);
    }

    [Fact]
    public void Cancelar_EnLobby_RaisesCanceladaDomainEvent()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);
        partida.FlushDomainEvents();

        partida.Cancelar();

        Assert.Contains(partida.DomainEvents, e => e is PartidaTriviaCanceladaDomainEvent);
    }

    [Fact]
    public void Cancelar_EnIniciada_CambiaACancelada()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);
        partida.Iniciar(cantidadInscriptos: 2);
        partida.FlushDomainEvents();

        partida.Cancelar();

        Assert.Equal(PartidaEstado.Cancelada, partida.Estado);
    }

    // =====================================================================
    // PublicarLobby
    // =====================================================================

    [Fact]
    public void PublicarLobby_EnLobby_RaisesPublicadaDomainEvent()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);
        partida.FlushDomainEvents();

        partida.PublicarLobby();

        Assert.Contains(partida.DomainEvents, e => e is PartidaTriviaPublicadaDomainEvent);
    }

    // =====================================================================
    // PuedeIniciar
    // =====================================================================

    [Fact]
    public void PuedeIniciar_ConInscriptosSuficientes_ReturnsTrue()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        Assert.True(partida.PuedeIniciar(cantidadInscriptos: 2));
    }

    [Fact]
    public void PuedeIniciar_ConInscriptosInsuficientes_ReturnsFalse()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        Assert.False(partida.PuedeIniciar(cantidadInscriptos: 0));
    }

    [Fact]
    public void PuedeIniciar_DespuesDeIniciar_ReturnsFalse()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);
        partida.Iniciar(cantidadInscriptos: 2);

        Assert.False(partida.PuedeIniciar(cantidadInscriptos: 5));
    }
}
