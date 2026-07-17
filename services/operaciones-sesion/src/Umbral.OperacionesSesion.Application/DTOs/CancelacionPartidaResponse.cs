namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record CancelacionPartidaResponse(
    Guid PartidaId,
    string Estado);
