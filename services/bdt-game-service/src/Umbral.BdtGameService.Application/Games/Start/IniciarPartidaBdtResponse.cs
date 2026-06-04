namespace Umbral.BdtGameService.Application.Games.Start;

public sealed record IniciarPartidaBdtResponse(
    Guid PartidaId,
    string Nombre,
    string Estado,
    string Modalidad,
    EtapaActivaBdtResponse EtapaActiva,
    string Mensaje);
