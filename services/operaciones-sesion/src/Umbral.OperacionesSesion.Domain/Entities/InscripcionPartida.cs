using System.Collections.Generic;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class InscripcionPartida
{
    private readonly List<Convocatoria> _convocatorias = new();

    public InscripcionId Id { get; private set; }
    public Guid ParticipanteId { get; private set; } // Guid.Empty en modalidad Equipo
    public Modalidad Modalidad { get; private set; }
    public Guid? EquipoId { get; private set; }
    public EstadoInscripcion Estado { get; private set; }
    public DateTime FechaInscripcion { get; private set; }

    public IReadOnlyList<Convocatoria> Convocatorias => _convocatorias;

    private InscripcionPartida() { } // EF

    // Individual (ctor existente; conservado para SesionPartida.Inscribir)
    internal InscripcionPartida(Guid participanteId, DateTime fecha)
    {
        Id = InscripcionId.New();
        ParticipanteId = participanteId;
        Modalidad = Modalidad.Individual;
        Estado = EstadoInscripcion.Activa;
        FechaInscripcion = fecha;
    }

    // Equipo
    private InscripcionPartida(Guid equipoId, IEnumerable<Guid> miembros, Guid partidaId, DateTime fecha)
    {
        Id = InscripcionId.New();
        ParticipanteId = Guid.Empty;
        Modalidad = Modalidad.Equipo;
        EquipoId = equipoId;
        Estado = EstadoInscripcion.Activa;
        FechaInscripcion = fecha;
        foreach (var m in miembros)
            _convocatorias.Add(new Convocatoria(partidaId, equipoId, m, fecha));
    }

    internal static InscripcionPartida PreinscribirEquipo(
        Guid equipoId, IEnumerable<Guid> miembros, Guid partidaId, DateTime fecha)
        => new(equipoId, miembros, partidaId, fecha);

    internal void Cancelar() => Estado = EstadoInscripcion.Cancelada;

    public bool EsActiva => Estado == EstadoInscripcion.Activa;
    public int ConvocatoriasAceptadas => _convocatorias.Count(c => c.EstaAceptada);
}
