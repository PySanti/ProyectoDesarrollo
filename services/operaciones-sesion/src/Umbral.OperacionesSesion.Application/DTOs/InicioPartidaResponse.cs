namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record InicioPartidaResponse(
    Guid PartidaId,
    string Estado,
    Guid? JuegoActivadoId,
    int? JuegoActivadoOrden);
