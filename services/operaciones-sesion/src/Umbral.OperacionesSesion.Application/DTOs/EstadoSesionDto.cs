namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record EstadoSesionDto(
    Guid PartidaId,
    Guid SesionPartidaId,
    string Estado,
    string Modalidad,
    IReadOnlyList<JuegoEstadoDto> Juegos,
    int? JuegoActualOrden);

public sealed record JuegoEstadoDto(
    Guid JuegoId,
    int Orden,
    string TipoJuego,
    string Estado);
