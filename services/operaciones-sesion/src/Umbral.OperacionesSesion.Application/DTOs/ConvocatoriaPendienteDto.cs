namespace Umbral.OperacionesSesion.Application.DTOs;

// NombrePartida va al final para no romper construcciones posicionales existentes.
// Es el snapshot SesionPartida.Nombre: evita que el movil (Participante) tenga que
// llegar a Partidas, que el gateway le cierra (/partidas/** -> OperadorOAdministrador).
public sealed record ConvocatoriaPendienteDto(
    Guid ConvocatoriaId, Guid PartidaId, Guid EquipoId, DateTime FechaEnvio, string NombrePartida);
