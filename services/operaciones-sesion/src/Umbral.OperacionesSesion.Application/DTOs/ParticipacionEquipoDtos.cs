namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record PreinscripcionEquipoResponse(Guid InscripcionId, Guid EquipoId, int Convocados);
public sealed record ConvocatoriaResponse(Guid ConvocatoriaId, string Estado);
