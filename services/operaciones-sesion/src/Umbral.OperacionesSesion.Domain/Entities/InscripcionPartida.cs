using System.Collections.Generic;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class InscripcionPartida
{
    private readonly List<Convocatoria> _convocatorias = new();
    private readonly List<Guid> _miembrosSnapshot = new();

    public InscripcionId Id { get; private set; }
    public Guid ParticipanteId { get; private set; } // Guid.Empty en modalidad Equipo
    public Modalidad Modalidad { get; private set; }
    public Guid? EquipoId { get; private set; }
    public EstadoInscripcion Estado { get; private set; }
    public DateTime FechaInscripcion { get; private set; }

    public IReadOnlyList<Convocatoria> Convocatorias => _convocatorias;
    public IReadOnlyList<Guid> MiembrosSnapshot => _miembrosSnapshot;

    private InscripcionPartida() { } // EF

    // Individual: nace Pendiente (requiere aprobación del operador — HU-19).
    internal InscripcionPartida(Guid participanteId, DateTime fecha)
    {
        Id = InscripcionId.New();
        ParticipanteId = participanteId;
        Modalidad = Modalidad.Individual;
        Estado = EstadoInscripcion.Pendiente;
        FechaInscripcion = fecha;
    }

    // Equipo: nace Pendiente, guarda el snapshot de miembros; las convocatorias se
    // emiten al aceptar (decisión 2 del spec). partidaId se guarda en el campo
    // transitorio (no mapeado por EF) para que Aceptar() funcione de inmediato tras la
    // construcción en memoria; tras recargar desde persistencia, SesionPartida debe
    // reinyectarlo con FijarPartidaIdParaConvocar antes de llamar a Aceptar (Task 2).
    private InscripcionPartida(Guid equipoId, IEnumerable<Guid> miembros, Guid partidaId, DateTime fecha)
    {
        Id = InscripcionId.New();
        ParticipanteId = Guid.Empty;
        Modalidad = Modalidad.Equipo;
        EquipoId = equipoId;
        Estado = EstadoInscripcion.Pendiente;
        FechaInscripcion = fecha;
        _miembrosSnapshot.AddRange(miembros);
        _partidaIdParaConvocar = partidaId;
    }

    internal static InscripcionPartida PreinscribirEquipo(
        Guid equipoId, IEnumerable<Guid> miembros, Guid partidaId, DateTime fecha)
        => new(equipoId, miembros, partidaId, fecha);

    // El operador acepta: pasa a Activa. En Equipo crea las convocatorias desde el
    // snapshot y las devuelve (para emitir ConvocatoriaCreada). Individual → lista vacía.
    internal IReadOnlyList<Convocatoria> Aceptar(DateTime now)
    {
        Estado = EstadoInscripcion.Activa;
        if (Modalidad != Modalidad.Equipo)
            return System.Array.Empty<Convocatoria>();

        var creadas = _miembrosSnapshot
            .Select(m => new Convocatoria(_partidaIdParaConvocar, EquipoId!.Value, m, now))
            .ToList();
        _convocatorias.AddRange(creadas);
        return creadas;
    }

    internal void Rechazar() => Estado = EstadoInscripcion.Rechazada;

    // Conservado: usado por SesionPartida.CancelarInscripcion / CancelarInscripcionEquipo
    // (Task 2 decide si su orquestación de alto nivel cambia; el método en sí sigue siendo
    // válido para Activa/Pendiente → Cancelada y no requiere cambios de esta tarea).
    internal void Cancelar() => Estado = EstadoInscripcion.Cancelada;

    public bool EsActiva => Estado == EstadoInscripcion.Activa;
    public bool EstaPendiente => Estado == EstadoInscripcion.Pendiente;
    public bool OcupaParticipacion =>
        Estado is EstadoInscripcion.Pendiente or EstadoInscripcion.Activa;
    public int ConvocatoriasAceptadas => _convocatorias.Count(c => c.EstaAceptada);

    // La Convocatoria requiere el partidaId; no se persiste en la inscripción (deriva de
    // la sesión), así que el agregado lo inyecta al aceptar. Ver nota en Task 2.
    private Guid _partidaIdParaConvocar;
    internal void FijarPartidaIdParaConvocar(Guid partidaId) => _partidaIdParaConvocar = partidaId;
}
