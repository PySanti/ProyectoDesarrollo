namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record PartidaPublicadaEnLobbyEvent(
    Guid PartidaId,
    Guid SesionPartidaId,
    string Modalidad,
    int MinimosParticipacion,
    int MaximosParticipacion);
