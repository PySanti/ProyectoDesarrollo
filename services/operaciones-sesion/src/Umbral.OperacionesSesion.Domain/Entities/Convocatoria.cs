using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class Convocatoria
{
    public ConvocatoriaId Id { get; private set; }
    public Guid PartidaId { get; private set; }
    public Guid EquipoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public EstadoConvocatoria Estado { get; private set; }
    public DateTime FechaEnvio { get; private set; }
    public DateTime? FechaRespuesta { get; private set; }

    private Convocatoria() { } // EF

    internal Convocatoria(Guid partidaId, Guid equipoId, Guid usuarioId, DateTime fechaEnvio)
    {
        Id = ConvocatoriaId.New();
        PartidaId = partidaId;
        EquipoId = equipoId;
        UsuarioId = usuarioId;
        Estado = EstadoConvocatoria.Pendiente;
        FechaEnvio = fechaEnvio;
    }

    internal void Aceptar(DateTime now) { Estado = EstadoConvocatoria.Aceptada; FechaRespuesta = now; }
    internal void Rechazar(DateTime now) { Estado = EstadoConvocatoria.Rechazada; FechaRespuesta = now; }

    public bool EstaAceptada => Estado == EstadoConvocatoria.Aceptada;
    public bool EstaPendiente => Estado == EstadoConvocatoria.Pendiente;
}
