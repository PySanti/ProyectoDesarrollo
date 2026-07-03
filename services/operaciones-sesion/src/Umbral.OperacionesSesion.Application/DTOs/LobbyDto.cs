namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record LobbyDto(
    Guid PartidaId,
    Guid SesionPartidaId,
    string Estado,
    string Modalidad,
    int MinimosParticipacion,
    int MaximosParticipacion,
    int InscritosActivos,
    IReadOnlyList<Guid> Participantes,
    IReadOnlyList<EquipoLobbyDto> Equipos);

public sealed record EquipoLobbyDto(Guid EquipoId, int Convocados, int Aceptados);
