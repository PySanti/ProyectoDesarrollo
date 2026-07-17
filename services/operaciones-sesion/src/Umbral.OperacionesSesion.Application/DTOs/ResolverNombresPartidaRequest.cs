namespace Umbral.OperacionesSesion.Application.DTOs;

// PartidaIds nullable: un body {} o sin la clave se normaliza a lote vacio en el
// controller, igual que /identity/directory/names.
public sealed record ResolverNombresPartidaRequest(Guid[]? PartidaIds);
