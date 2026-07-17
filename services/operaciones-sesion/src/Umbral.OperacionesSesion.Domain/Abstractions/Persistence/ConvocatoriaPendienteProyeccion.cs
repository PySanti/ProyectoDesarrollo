namespace Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

// Convocatoria pendiente + el nombre de su sesion, resuelto en la misma consulta.
// El nombre vive en SesionPartida y el SelectMany hasta Convocatoria lo perdia, dejando
// al movil sin forma de nombrar la partida (el gateway le cierra /partidas/**).
// Mismo patron que ParticipacionEquipoHistorial en Puntuaciones.
public sealed record ConvocatoriaPendienteProyeccion(
    Guid ConvocatoriaId, Guid PartidaId, string NombrePartida, Guid EquipoId, DateTime FechaEnvio);
