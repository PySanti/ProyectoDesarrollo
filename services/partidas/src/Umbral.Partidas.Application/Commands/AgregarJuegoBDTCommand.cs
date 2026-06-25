using MediatR;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Commands;

public sealed record AgregarJuegoBDTCommand(
    Guid PartidaId,
    int Orden,
    string AreaBusqueda,
    IReadOnlyList<EtapaRequest> Etapas) : IRequest<AgregarJuegoResponse>;
