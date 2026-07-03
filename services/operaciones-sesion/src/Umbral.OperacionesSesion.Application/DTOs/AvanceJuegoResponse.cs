namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record AvanceJuegoResponse(
    Guid PartidaId,
    string Estado,
    int? JuegoFinalizadoOrden,
    int? JuegoActivadoOrden,
    bool Terminada);
