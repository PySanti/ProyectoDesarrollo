namespace Umbral.IdentityService.Application.DTOs;

public sealed record NombresResponse(
    IReadOnlyList<NombreParticipanteResponse> Participantes,
    IReadOnlyList<NombreEquipoResponse> Equipos);

public sealed record NombreParticipanteResponse(Guid ParticipanteId, string Nombre);

public sealed record NombreEquipoResponse(Guid EquipoId, string NombreEquipo);
