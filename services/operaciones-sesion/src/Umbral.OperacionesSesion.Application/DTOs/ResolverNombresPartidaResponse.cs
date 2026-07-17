namespace Umbral.OperacionesSesion.Application.DTOs;

// Campo Nombre (no NombrePartida) por paridad con PartidaPublicadaDto dentro de este
// servicio: bajo la clave partidas no hay ambiguedad. ConvocatoriaPendienteDto si usa
// NombrePartida porque alli el nombre viaja junto a un equipoId.
public sealed record NombrePartidaDto(Guid PartidaId, string Nombre);

public sealed record ResolverNombresPartidaResponse(IReadOnlyList<NombrePartidaDto> Partidas);
