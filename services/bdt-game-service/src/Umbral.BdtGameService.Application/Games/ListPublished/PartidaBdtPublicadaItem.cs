namespace Umbral.BdtGameService.Application.Games.ListPublished;

public sealed record PartidaBdtPublicadaItem(
    Guid PartidaId,
    string Nombre,
    string Modalidad,
    string Estado,
    string AreaBusqueda,
    int CantidadEtapas);
