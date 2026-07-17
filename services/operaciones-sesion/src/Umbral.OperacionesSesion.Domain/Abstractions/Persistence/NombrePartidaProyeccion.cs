namespace Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

// Nombre de una partida, resuelto por lote para el directorio del movil.
// Operaciones snapshotea el nombre al publicar, asi que hay fila para toda partida
// publicada; el movil no puede leerlo de Partidas (el gateway le cierra /partidas/**).
// Mismo patron de proyeccion que ConvocatoriaPendienteProyeccion.
public sealed record NombrePartidaProyeccion(Guid PartidaId, string Nombre);
