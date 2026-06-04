using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.Events;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Entities;

public sealed class PartidaTrivia : AggregateRoot<PartidaId>
{
    public NombrePartida Nombre { get; private set; }
    public PartidaEstado Estado { get; private set; }
    public Modalidad Modalidad { get; private set; }
    public ModoInicio ModoInicio { get; private set; }
    public TriviaFormId FormularioAsociadoId { get; }
    public OperatorId CreatedByOperatorId { get; }
    public TiempoInicio TiempoInicio { get; private set; }
    public CantidadMinima MinimoParticipantes { get; private set; }

    public CantidadMaximaJugadores? MaximoJugadores { get; private set; }
    public CantidadMaximaEquipos? MaximoEquipos { get; private set; }
    public JugadoresPorEquipoMin? MinimoJugadoresPorEquipo { get; private set; }
    public JugadoresPorEquipoMax? MaximoJugadoresPorEquipo { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? StartedAtUtc { get; private set; }

    public QuestionId? PreguntaActualId { get; private set; }
    public DateTimeOffset? PreguntaAbiertaEnUtc { get; private set; }

    private readonly List<RespuestaTrivia> _respuestas = new();
    public IReadOnlyCollection<RespuestaTrivia> Respuestas => _respuestas.AsReadOnly();

    private PartidaTrivia() : base(PartidaId.New()) { }

    private PartidaTrivia(
        PartidaId id,
        NombrePartida nombre,
        Modalidad modalidad,
        ModoInicio modoInicio,
        TriviaFormId formularioId,
        OperatorId operatorId,
        TiempoInicio tiempoInicio,
        CantidadMinima minimoParticipantes,
        CantidadMaximaJugadores? maximoJugadores,
        CantidadMaximaEquipos? maximoEquipos,
        JugadoresPorEquipoMin? minJugadoresPorEquipo,
        JugadoresPorEquipoMax? maxJugadoresPorEquipo,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Nombre = nombre;
        Estado = PartidaEstado.Lobby;
        Modalidad = modalidad;
        ModoInicio = modoInicio;
        FormularioAsociadoId = formularioId;
        CreatedByOperatorId = operatorId;
        TiempoInicio = tiempoInicio;
        MinimoParticipantes = minimoParticipantes;
        MaximoJugadores = maximoJugadores;
        MaximoEquipos = maximoEquipos;
        MinimoJugadoresPorEquipo = minJugadoresPorEquipo;
        MaximoJugadoresPorEquipo = maxJugadoresPorEquipo;
        CreatedAtUtc = createdAtUtc;
        StartedAtUtc = null;
    }

    public static PartidaTrivia Create(
        NombrePartida nombre,
        Modalidad modalidad,
        ModoInicio modoInicio,
        TriviaFormId formularioId,
        OperatorId operatorId,
        TiempoInicio tiempoInicio,
        CantidadMinima minimoParticipantes,
        CantidadMaximaJugadores? maximoJugadores,
        CantidadMaximaEquipos? maximoEquipos,
        JugadoresPorEquipoMin? minJugadoresPorEquipo,
        JugadoresPorEquipoMax? maxJugadoresPorEquipo)
    {
        if (nombre is null)
            throw new DomainValidationException("El nombre de la partida es obligatorio.");
        if (formularioId is null)
            throw new DomainValidationException("El identificador del formulario es obligatorio.");
        if (operatorId is null)
            throw new DomainValidationException("El identificador del operador es obligatorio.");
        if (tiempoInicio is null)
            throw new DomainValidationException("La fecha y hora de inicio son obligatorias.");
        if (minimoParticipantes is null)
            throw new DomainValidationException("La cantidad mínima de participantes es obligatoria.");

        if (modalidad == Enums.Modalidad.Individual)
        {
            if (maximoJugadores is null)
                throw new ModalidadInvalidaException("Individual",
                    "Debe especificar la cantidad máxima de jugadores.");
            if (maximoEquipos is not null)
                throw new ModalidadInvalidaException("Individual",
                    "No debe especificar cantidad máxima de equipos.");
            if (minJugadoresPorEquipo is not null || maxJugadoresPorEquipo is not null)
                throw new ModalidadInvalidaException("Individual",
                    "No debe especificar límites de jugadores por equipo.");
        }
        else if (modalidad == Enums.Modalidad.Equipo)
        {
            if (maximoJugadores is not null)
                throw new ModalidadInvalidaException("Equipo",
                    "No debe especificar cantidad máxima de jugadores.");
            if (maximoEquipos is null)
                throw new ModalidadInvalidaException("Equipo",
                    "Debe especificar la cantidad máxima de equipos.");
            if (minJugadoresPorEquipo is null)
                throw new ModalidadInvalidaException("Equipo",
                    "Debe especificar el mínimo de jugadores por equipo.");
            if (maxJugadoresPorEquipo is null)
                throw new ModalidadInvalidaException("Equipo",
                    "Debe especificar el máximo de jugadores por equipo.");
        }

        var id = PartidaId.New();
        var now = DateTimeOffset.UtcNow;

        var partida = new PartidaTrivia(
            id, nombre, modalidad, modoInicio, formularioId, operatorId,
            tiempoInicio, minimoParticipantes,
            maximoJugadores, maximoEquipos,
            minJugadoresPorEquipo, maxJugadoresPorEquipo,
            now);

        partida.AddDomainEvent(new PartidaTriviaCreadaDomainEvent(id, nombre, operatorId));
        return partida;
    }

    public void PublicarLobby()
    {
        if (Estado != PartidaEstado.Lobby)
            throw new InvalidStateTransitionException(Estado.ToString(), "Lobby");

        AddDomainEvent(new PartidaTriviaPublicadaDomainEvent(Id, Nombre));
    }

    public void Iniciar(int cantidadInscriptos, bool esInicioManual, QuestionId primeraPreguntaId)
    {
        if (Estado != PartidaEstado.Lobby)
            throw new InvalidStateTransitionException(Estado.ToString(), "Iniciada");

        if (cantidadInscriptos < MinimoParticipantes.Value)
            throw new MinimosNoCumplidosException(cantidadInscriptos, MinimoParticipantes.Value);

        if (esInicioManual && ModoInicio == ModoInicio.Automatico)
            throw new ModoInicioAutomaticoException();

        if (primeraPreguntaId is null)
            throw new DomainValidationException("El identificador de la primera pregunta es obligatorio para iniciar la partida.");

        Estado = PartidaEstado.Iniciada;
        StartedAtUtc = DateTimeOffset.UtcNow;
        PreguntaActualId = primeraPreguntaId;
        PreguntaAbiertaEnUtc = StartedAtUtc;

        AddDomainEvent(new PartidaTriviaIniciadaDomainEvent(Id, Nombre, StartedAtUtc.Value));
    }

    public void AbrirPregunta(QuestionId questionId)
    {
        if (Estado != PartidaEstado.Iniciada)
            throw new InvalidStateTransitionException(Estado.ToString(), "Iniciada");

        PreguntaActualId = questionId ?? throw new DomainValidationException("El identificador de la pregunta es obligatorio.");
        PreguntaAbiertaEnUtc = DateTimeOffset.UtcNow;
    }

    public RespuestaTrivia RegistrarRespuestaDefinitiva(
        QuestionId preguntaId,
        string usuarioId,
        int opcionIndex,
        bool esCorrecta,
        int assignedScore,
        int timeLimitSeconds,
        string respuestaCorrecta = "")
    {
        if (Estado != PartidaEstado.Iniciada)
            throw new EstadoPartidaInvalidoException(Id.Value, "La partida debe estar en estado Iniciada para registrar respuestas.");

        if (PreguntaActualId is null)
            throw new PreguntaNoActivaException(Id.Value);

        if (PreguntaActualId.Value != preguntaId.Value)
            throw new PreguntaNoActivaException(Id.Value);

        if (PreguntaAbiertaEnUtc is null)
            throw new DomainValidationException("La pregunta activa no tiene registro de apertura.");

        var elapsed = DateTimeOffset.UtcNow - PreguntaAbiertaEnUtc.Value;
        if (elapsed.TotalSeconds > timeLimitSeconds)
            throw new RespuestaTardiaException(timeLimitSeconds);

        if (_respuestas.Any(r => r.UsuarioId == usuarioId && r.PreguntaId.Value == preguntaId.Value))
            throw new RespuestaDuplicadaException(usuarioId, preguntaId.Value);

        var tiempoEmpleadoSegundos = elapsed.TotalSeconds;

        var respuesta = RespuestaTrivia.Create(
            Id,
            preguntaId,
            usuarioId,
            opcionIndex,
            esCorrecta,
            assignedScore,
            tiempoEmpleadoSegundos);

        _respuestas.Add(respuesta);

        AddDomainEvent(new RespuestaTriviaRegistradaDomainEvent(
            Id,
            preguntaId.Value,
            usuarioId,
            esCorrecta,
            respuesta.PuntajeObtenido));

        if (esCorrecta)
        {
            CerrarPreguntaActual(MotivoCierre.CorrectAnswer, respuestaCorrecta);
        }

        return respuesta;
    }

    public void CerrarPreguntaActual(MotivoCierre motivo, string respuestaCorrecta)
    {
        if (PreguntaActualId is null)
            return;

        var preguntaIdCerrada = PreguntaActualId.Value;

        PreguntaActualId = null;
        PreguntaAbiertaEnUtc = null;

        AddDomainEvent(new PreguntaTriviaCerradaDomainEvent(
            Id,
            preguntaIdCerrada,
            motivo,
            respuestaCorrecta));
    }

    public void AvanzarPregunta(QuestionId nextQuestionId)
    {
        if (Estado != PartidaEstado.Iniciada)
            throw new InvalidStateTransitionException(Estado.ToString(), "Iniciada");

        if (nextQuestionId is null)
            throw new DomainValidationException("El identificador de la siguiente pregunta es obligatorio.");

        PreguntaActualId = nextQuestionId;
        PreguntaAbiertaEnUtc = DateTimeOffset.UtcNow;
    }

    public void FinalizarPartida()
    {
        if (Estado != PartidaEstado.Iniciada)
            throw new InvalidStateTransitionException(Estado.ToString(), "Terminada");

        Estado = PartidaEstado.Terminada;
        PreguntaActualId = null;
        PreguntaAbiertaEnUtc = null;
    }

    public int ObtenerPuntajeAcumulado(string usuarioId)
    {
        return _respuestas
            .Where(r => r.UsuarioId == usuarioId && r.EsCorrecta)
            .Sum(r => r.PuntajeObtenido);
    }

    public int ObtenerTiempoRespuestaAcumulado(string usuarioId)
    {
        return (int)_respuestas
            .Where(r => r.UsuarioId == usuarioId)
            .Sum(r => r.TiempoEmpleadoSegundos);
    }

    public void Cancelar()
    {
        if (Estado != PartidaEstado.Lobby && Estado != PartidaEstado.Iniciada)
            throw new InvalidStateTransitionException(Estado.ToString(), "Cancelada");

        Estado = PartidaEstado.Cancelada;

        AddDomainEvent(new PartidaTriviaCanceladaDomainEvent(Id, Nombre));
    }

    public bool PuedeIniciar(int cantidadInscriptos)
    {
        return Estado == PartidaEstado.Lobby
            && cantidadInscriptos >= MinimoParticipantes.Value;
    }

    public override string ToString() =>
        $"PartidaTrivia {{ Id: {Id}, Nombre: \"{Nombre.Value}\", Estado: {Estado}, " +
        $"Modalidad: {Modalidad}, FormularioId: {FormularioAsociadoId.Value} }}";
}
