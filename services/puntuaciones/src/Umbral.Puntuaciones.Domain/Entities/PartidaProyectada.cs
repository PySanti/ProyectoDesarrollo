using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Domain.Entities;

// Proyección del ciclo de vida de una partida (fuente: eventos de Operaciones de Sesión).
// Tolerante al desorden de llegada: el estado nunca retrocede y las fechas no se pisan.
public sealed class PartidaProyectada
{
    private PartidaProyectada(Guid partidaId, Guid sesionPartidaId, Modalidad? modalidad, EstadoPartidaProyectada estado)
    {
        PartidaId = partidaId;
        SesionPartidaId = sesionPartidaId;
        Modalidad = modalidad;
        Estado = estado;
    }

    public Guid PartidaId { get; private set; }
    public Guid SesionPartidaId { get; private set; }
    public Modalidad? Modalidad { get; private set; }
    public EstadoPartidaProyectada Estado { get; private set; }
    public DateTime? FechaInicio { get; private set; }
    public DateTime? FechaFin { get; private set; }

    public static PartidaProyectada DesdePublicacion(Guid partidaId, Guid sesionPartidaId, Modalidad modalidad)
        => new(partidaId, sesionPartidaId, modalidad, EstadoPartidaProyectada.Lobby);

    // Un evento posterior llegó antes que la publicación (best-effort, sin garantía de orden).
    public static PartidaProyectada Stub(Guid partidaId, Guid sesionPartidaId)
        => new(partidaId, sesionPartidaId, null, EstadoPartidaProyectada.Lobby);

    public void RegistrarPublicacion(Modalidad modalidad) => Modalidad ??= modalidad;

    public void MarcarIniciada(DateTime fechaInicio)
    {
        AvanzarEstado(EstadoPartidaProyectada.Iniciada);
        FechaInicio ??= fechaInicio;
    }

    public void MarcarCancelada(DateTime fechaCancelacion)
    {
        AvanzarEstado(EstadoPartidaProyectada.Cancelada);
        FechaFin ??= fechaCancelacion;
    }

    public void MarcarTerminada(DateTime fechaFin)
    {
        AvanzarEstado(EstadoPartidaProyectada.Terminada);
        FechaFin ??= fechaFin;
    }

    private void AvanzarEstado(EstadoPartidaProyectada nuevo)
    {
        if (Rango(nuevo) > Rango(Estado))
        {
            Estado = nuevo;
        }
    }

    private static int Rango(EstadoPartidaProyectada estado) => estado switch
    {
        EstadoPartidaProyectada.Lobby => 0,
        EstadoPartidaProyectada.Iniciada => 1,
        _ => 2 // Cancelada y Terminada son terminales.
    };
}
