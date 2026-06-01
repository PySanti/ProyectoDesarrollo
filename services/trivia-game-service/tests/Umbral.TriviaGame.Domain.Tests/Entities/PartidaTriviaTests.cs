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

    private static QuestionId ValidPrimeraPreguntaId => QuestionId.New();

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

        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: ValidPrimeraPreguntaId);

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

        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: ValidPrimeraPreguntaId);

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

        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: ValidPrimeraPreguntaId);

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
            partida.Iniciar(cantidadInscriptos: 1, esInicioManual: true, primeraPreguntaId: ValidPrimeraPreguntaId));
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
        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: ValidPrimeraPreguntaId);

        Assert.Throws<InvalidStateTransitionException>(() =>
            partida.Iniciar(cantidadInscriptos: 5, esInicioManual: true, primeraPreguntaId: ValidPrimeraPreguntaId));
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
        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: ValidPrimeraPreguntaId);
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
        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: ValidPrimeraPreguntaId);

        Assert.False(partida.PuedeIniciar(cantidadInscriptos: 5));
    }

    // =====================================================================
    // RegistrarRespuestaDefinitiva
    // =====================================================================

    private PartidaTrivia CreatePartidaIniciada(out QuestionId primeraPreguntaId)
    {
        primeraPreguntaId = QuestionId.New();
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);
        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: primeraPreguntaId);
        return partida;
    }

    [Fact]
    public void RegistrarRespuestaDefinitiva_Correcta_RegistraYCierraPregunta()
    {
        var partida = CreatePartidaIniciada(out var preguntaId);
        partida.FlushDomainEvents();

        var respuesta = partida.RegistrarRespuestaDefinitiva(
            preguntaId, "user-1", 0, esCorrecta: true,
            assignedScore: 100, timeLimitSeconds: 300);

        Assert.NotNull(respuesta);
        Assert.True(respuesta.EsCorrecta);
        Assert.Equal(100, respuesta.PuntajeObtenido);
        Assert.Equal("user-1", respuesta.UsuarioId);
        Assert.Equal(0, respuesta.OpcionSeleccionadaIndex);
        Assert.Equal(partida.Id.Value, respuesta.PartidaId.Value);
        Assert.Equal(preguntaId.Value, respuesta.PreguntaId.Value);
        Assert.Null(partida.PreguntaActualId);
        Assert.Contains(partida.DomainEvents, e => e is RespuestaTriviaRegistradaDomainEvent);
    }

    [Fact]
    public void RegistrarRespuestaDefinitiva_Incorrecta_RegistraSinCerrarPregunta()
    {
        var partida = CreatePartidaIniciada(out var preguntaId);
        partida.FlushDomainEvents();

        var respuesta = partida.RegistrarRespuestaDefinitiva(
            preguntaId, "user-2", 1, esCorrecta: false,
            assignedScore: 100, timeLimitSeconds: 300);

        Assert.NotNull(respuesta);
        Assert.False(respuesta.EsCorrecta);
        Assert.Equal(0, respuesta.PuntajeObtenido);
        Assert.NotNull(partida.PreguntaActualId);
        Assert.Equal(preguntaId.Value, partida.PreguntaActualId!.Value);
    }

    [Fact]
    public void RegistrarRespuestaDefinitiva_PartidaEnLobby_ThrowsEstadoPartidaInvalido()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        Assert.Throws<EstadoPartidaInvalidoException>(() =>
            partida.RegistrarRespuestaDefinitiva(
                QuestionId.New(), "user-1", 0, esCorrecta: true,
                assignedScore: 100, timeLimitSeconds: 300));
    }

    [Fact]
    public void RegistrarRespuestaDefinitiva_SinPreguntaActiva_ThrowsPreguntaNoActiva()
    {
        var partida = CreatePartidaIniciada(out var _);
        partida.CerrarPreguntaActual();
        partida.FlushDomainEvents();

        Assert.Throws<PreguntaNoActivaException>(() =>
            partida.RegistrarRespuestaDefinitiva(
                QuestionId.New(), "user-1", 0, esCorrecta: true,
                assignedScore: 100, timeLimitSeconds: 300));
    }

    [Fact]
    public void RegistrarRespuestaDefinitiva_PreguntaIdDiferente_ThrowsPreguntaNoActiva()
    {
        var partida = CreatePartidaIniciada(out var _);
        var otraPregunta = QuestionId.New();

        Assert.Throws<PreguntaNoActivaException>(() =>
            partida.RegistrarRespuestaDefinitiva(
                otraPregunta, "user-1", 0, esCorrecta: true,
                assignedScore: 100, timeLimitSeconds: 300));
    }

    [Fact]
    public void RegistrarRespuestaDefinitiva_MismaPreguntaMismoUsuario_ThrowsRespuestaDuplicada()
    {
        var partida = CreatePartidaIniciada(out var preguntaId);

        partida.RegistrarRespuestaDefinitiva(
            preguntaId, "user-1", 0, esCorrecta: false,
            assignedScore: 100, timeLimitSeconds: 300);

        Assert.Throws<RespuestaDuplicadaException>(() =>
            partida.RegistrarRespuestaDefinitiva(
                preguntaId, "user-1", 1, esCorrecta: true,
                assignedScore: 100, timeLimitSeconds: 300));
    }

    [Fact]
    public void RegistrarRespuestaDefinitiva_RespuestaCorrecta_RaisesDomainEvent()
    {
        var partida = CreatePartidaIniciada(out var preguntaId);
        partida.FlushDomainEvents();

        partida.RegistrarRespuestaDefinitiva(
            preguntaId, "user-1", 2, esCorrecta: true,
            assignedScore: 50, timeLimitSeconds: 300);

        var events = partida.DomainEvents;
        var domainEvent = Assert.Single(events);
        var registeredEvent = Assert.IsType<RespuestaTriviaRegistradaDomainEvent>(domainEvent);
        Assert.True(registeredEvent.EsCorrecta);
        Assert.Equal(50, registeredEvent.PuntajeObtenido);
    }

    // =====================================================================
    // ObtenerPuntajeAcumulado
    // =====================================================================

    [Fact]
    public void ObtenerPuntajeAcumulado_SinRespuestas_ReturnsZero()
    {
        var partida = CreatePartidaIniciada(out var _);

        var puntaje = partida.ObtenerPuntajeAcumulado("user-1");

        Assert.Equal(0, puntaje);
    }

    [Fact]
    public void ObtenerPuntajeAcumulado_VariasRespuestas_SumaCorrectas()
    {
        var partida = CreatePartidaIniciada(out var preguntaId1);
        var preguntaId2 = QuestionId.New();

        partida.RegistrarRespuestaDefinitiva(
            preguntaId1, "user-1", 0, esCorrecta: true,
            assignedScore: 100, timeLimitSeconds: 300);

        partida.AbrirPregunta(preguntaId2);

        partida.RegistrarRespuestaDefinitiva(
            preguntaId2, "user-1", 1, esCorrecta: false,
            assignedScore: 50, timeLimitSeconds: 300);

        var puntaje = partida.ObtenerPuntajeAcumulado("user-1");

        Assert.Equal(100, puntaje);
    }

    [Fact]
    public void ObtenerPuntajeAcumulado_DistintosUsuarios_NoMezclaPuntajes()
    {
        var partida = CreatePartidaIniciada(out var preguntaId1);
        var preguntaId2 = QuestionId.New();

        partida.RegistrarRespuestaDefinitiva(
            preguntaId1, "user-a", 0, esCorrecta: true,
            assignedScore: 100, timeLimitSeconds: 300);

        partida.AbrirPregunta(preguntaId2);

        partida.RegistrarRespuestaDefinitiva(
            preguntaId2, "user-b", 2, esCorrecta: true,
            assignedScore: 200, timeLimitSeconds: 300);

        Assert.Equal(100, partida.ObtenerPuntajeAcumulado("user-a"));
        Assert.Equal(200, partida.ObtenerPuntajeAcumulado("user-b"));
    }

    // =====================================================================
    // AbrirPregunta
    // =====================================================================

    [Fact]
    public void AbrirPregunta_EnIniciada_ActualizaPreguntaActual()
    {
        var partida = CreatePartidaIniciada(out var _);
        partida.FlushDomainEvents();

        var nuevaPregunta = QuestionId.New();
        partida.AbrirPregunta(nuevaPregunta);

        Assert.Equal(nuevaPregunta.Value, partida.PreguntaActualId!.Value);
        Assert.NotNull(partida.PreguntaAbiertaEnUtc);
    }

    [Fact]
    public void AbrirPregunta_EnLobby_ThrowsInvalidStateTransition()
    {
        var partida = PartidaTrivia.Create(
            ValidNombre, Modalidad.Individual, ModoInicio.Manual,
            ValidFormularioId, ValidOperatorId, ValidTiempoInicio, ValidMinimo,
            maximoJugadores: ValidMaxJugadores,
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        Assert.Throws<InvalidStateTransitionException>(() =>
            partida.AbrirPregunta(QuestionId.New()));
    }

    // =====================================================================
    // CerrarPreguntaActual
    // =====================================================================

    [Fact]
    public void CerrarPreguntaActual_LimpiaPreguntaActiva()
    {
        var partida = CreatePartidaIniciada(out var _);

        partida.CerrarPreguntaActual();

        Assert.Null(partida.PreguntaActualId);
        Assert.Null(partida.PreguntaAbiertaEnUtc);
    }
}
