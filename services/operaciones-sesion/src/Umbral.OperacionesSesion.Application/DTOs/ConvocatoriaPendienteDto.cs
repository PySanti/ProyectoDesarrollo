namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record ConvocatoriaPendienteDto(
    Guid ConvocatoriaId, Guid PartidaId, Guid EquipoId, DateTime FechaEnvio);
