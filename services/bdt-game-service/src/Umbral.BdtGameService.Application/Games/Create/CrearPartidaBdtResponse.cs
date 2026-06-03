namespace Umbral.BdtGameService.Application.Games.Create;

public sealed record CrearPartidaBdtResponse(
    Guid PartidaId,
    string Nombre,
    string Modalidad,
    string Estado,
    string AreaBusqueda,
    string ModoInicio,
    int CantidadEtapas);
