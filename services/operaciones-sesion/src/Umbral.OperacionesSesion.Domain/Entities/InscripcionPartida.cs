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

    // Guid.Empty en Individual. Se persiste porque las convocatorias no se crean aqui sino al
    // aceptar el operador, y para entonces el flag EsLider del equipo ya se perdio:
    // MiembrosSnapshot es una lista de Guid pelados.
    public Guid LiderId { get; private set; }
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
    private InscripcionPartida(Guid equipoId, Guid liderId, IEnumerable<Guid> miembros, Guid partidaId, DateTime fecha)
    {
        Id = InscripcionId.New();
        ParticipanteId = Guid.Empty;
        LiderId = liderId;
        Modalidad = Modalidad.Equipo;
        EquipoId = equipoId;
        Estado = EstadoInscripcion.Pendiente;
        FechaInscripcion = fecha;
        _miembrosSnapshot.AddRange(miembros);
        _partidaIdParaConvocar = partidaId;
    }

    internal static InscripcionPartida PreinscribirEquipo(
        Guid equipoId, Guid liderId, IEnumerable<Guid> miembros, Guid partidaId, DateTime fecha)
        => new(equipoId, liderId, miembros, partidaId, fecha);

    // El operador acepta: pasa a Activa. En Equipo crea las convocatorias desde el
    // snapshot y las devuelve (para emitir ConvocatoriaCreada). Individual → lista vacía.
    // liderPuedeAutoAceptar viene invertido a proposito, y por defecto en false. El resto de flags
    // del dominio nombran el problema ("...TieneParticipacionActivaEnOtra"), pero aqui el default
    // tiene que fallar CERRADO: si un llamador lo omite, lo que pasa es que no se auto-acepta (la
    // partida no arranca — ruidoso y visible), no que se auto-acepte saltandose BR-G09 (silencioso).
    // Lo calcula el handler, que es quien puede consultar otras partidas; el dominio no lo hace.
    internal IReadOnlyList<Convocatoria> Aceptar(DateTime now, bool liderPuedeAutoAceptar = false)
    {
        Estado = EstadoInscripcion.Activa;
        if (Modalidad != Modalidad.Equipo)
            return System.Array.Empty<Convocatoria>();

        var creadas = _miembrosSnapshot
            .Select(m => new Convocatoria(_partidaIdParaConvocar, EquipoId!.Value, m, now))
            .ToList();
        _convocatorias.AddRange(creadas);

        // Preinscribir el equipo ya era la declaracion de intencion del lider (HU-15): no tiene
        // que ademas convocarse a si mismo. Sin esto, un equipo de solo el lider no arranca nunca.
        if (liderPuedeAutoAceptar && LiderId != Guid.Empty)
        {
            var delLider = creadas.SingleOrDefault(c => c.UsuarioId == LiderId);
            delLider?.Aceptar(now);
        }

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
