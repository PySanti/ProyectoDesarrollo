using System.Linq;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class PreguntaSnapshot
{
    private readonly List<OpcionSnapshot> _opciones = new();
    private readonly List<RespuestaTrivia> _respuestas = new();

    public Guid PreguntaId { get; private set; }
    public int Orden { get; private set; }
    public string Texto { get; private set; } = null!;
    public int PuntajeAsignado { get; private set; }
    public int TiempoLimiteSegundos { get; private set; }
    public EstadoPregunta Estado { get; private set; } = EstadoPregunta.Pendiente;
    public DateTime? FechaActivacion { get; private set; }
    public DateTime? FechaCierre { get; private set; }
    public MotivoCierrePregunta? MotivoCierre { get; private set; }
    public Guid? GanadorParticipanteId { get; private set; }
    public Guid? GanadorEquipoId { get; private set; }

    public IReadOnlyList<OpcionSnapshot> Opciones => _opciones;
    public IReadOnlyList<RespuestaTrivia> Respuestas => _respuestas;

    private PreguntaSnapshot() { } // EF

    public PreguntaSnapshot(Guid preguntaId, int orden, string texto, int puntajeAsignado,
        int tiempoLimiteSegundos, IEnumerable<OpcionSnapshot> opciones)
    {
        PreguntaId = preguntaId;
        Orden = orden;
        Texto = texto;
        PuntajeAsignado = puntajeAsignado;
        TiempoLimiteSegundos = tiempoLimiteSegundos;
        _opciones.AddRange(opciones);
    }

    internal void Activar(DateTime now)
    {
        if (Estado != EstadoPregunta.Pendiente)
            throw new InvalidOperationException($"La pregunta {PreguntaId} no está pendiente.");
        Estado = EstadoPregunta.Activa;
        FechaActivacion = now;
    }

    internal void Cerrar(MotivoCierrePregunta motivo, DateTime now, Guid? ganador, Guid? ganadorEquipo = null)
    {
        if (Estado != EstadoPregunta.Activa)
            throw new InvalidOperationException($"La pregunta {PreguntaId} no está activa.");
        Estado = EstadoPregunta.Cerrada;
        FechaCierre = now;
        MotivoCierre = motivo;
        GanadorParticipanteId = ganador;
        GanadorEquipoId = ganadorEquipo;
    }

    internal ResultadoRespuesta RegistrarRespuesta(Guid participanteId, Guid? equipoId, Guid opcionId, DateTime now)
    {
        if (Estado != EstadoPregunta.Activa)
            throw new InvalidOperationException($"La pregunta {PreguntaId} no está activa.");
        var duplicada = equipoId is null
            ? _respuestas.Any(r => r.ParticipanteId == participanteId)
            : _respuestas.Any(r => r.EquipoId == equipoId);
        if (duplicada)
            throw new RespuestaDuplicadaException(participanteId);
        if (now > FechaActivacion!.Value.AddSeconds(TiempoLimiteSegundos))
            throw new PreguntaFueraDeTiempoException(PreguntaId);

        var esCorrecta = _opciones.Any(o => o.OpcionId == opcionId && o.EsCorrecta);
        _respuestas.Add(new RespuestaTrivia(participanteId, opcionId, esCorrecta, now, equipoId));

        var cerro = false;
        if (esCorrecta)
        {
            Cerrar(MotivoCierrePregunta.RespuestaCorrecta, now, participanteId, equipoId);
            cerro = true;
        }

        var tiempoMs = (long)(now - FechaActivacion!.Value).TotalMilliseconds;
        return new ResultadoRespuesta(esCorrecta, cerro, esCorrecta ? PuntajeAsignado : null,
            Guid.Empty, PreguntaId, participanteId, opcionId, now, tiempoMs, equipoId);
    }
}
