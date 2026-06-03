using MediatR;

namespace Umbral.BdtGameService.Application.Games.Create;

public sealed record CrearPartidaBdtCommand(
    string Nombre,
    string AreaBusqueda,
    string Modalidad,
    int MinimoParticipantes,
    int? MaximoParticipantes,
    int? MaximoEquipos,
    int? MinimoJugadoresPorEquipo,
    string ModoInicio,
    IReadOnlyList<CrearEtapaBdtRequest> Etapas) : IRequest<CrearPartidaBdtResponse>;

public sealed record CrearEtapaBdtRequest(
    int Orden,
    string CodigoQrEsperado,
    int TiempoLimiteSegundos);
