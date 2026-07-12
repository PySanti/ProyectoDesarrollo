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
    IReadOnlyList<EquipoLobbyDto> Equipos,
    IReadOnlyList<SolicitudIndividualDto> SolicitudesPendientesIndividual,
    IReadOnlyList<SolicitudEquipoDto> SolicitudesPendientesEquipo);

public sealed record EquipoLobbyDto(Guid EquipoId, int Convocados, int Aceptados);

public sealed record SolicitudIndividualDto(Guid InscripcionId, Guid ParticipanteId, DateTime FechaInscripcion);

public sealed record SolicitudEquipoDto(Guid InscripcionId, Guid EquipoId, int Miembros, DateTime FechaInscripcion);
