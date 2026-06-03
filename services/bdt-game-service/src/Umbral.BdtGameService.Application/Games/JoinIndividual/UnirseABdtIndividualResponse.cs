namespace Umbral.BdtGameService.Application.Games.JoinIndividual;

public sealed record UnirseABdtIndividualResponse(
    Guid PartidaId,
    string Nombre,
    string Modalidad,
    string Estado,
    Guid InscripcionId,
    Guid ParticipanteUserId,
    int PosicionEnLobby,
    string Mensaje);
