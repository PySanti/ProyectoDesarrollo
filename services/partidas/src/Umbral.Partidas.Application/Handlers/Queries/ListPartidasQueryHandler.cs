using System.Linq;
using MediatR;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Queries;
using Umbral.Partidas.Domain.Abstractions.Persistence;

namespace Umbral.Partidas.Application.Handlers.Queries;

public sealed class ListPartidasQueryHandler : IRequestHandler<ListPartidasQuery, IReadOnlyList<PartidaSummaryDto>>
{
    private readonly IPartidaRepository _partidas;

    public ListPartidasQueryHandler(IPartidaRepository partidas)
    {
        _partidas = partidas;
    }

    public async Task<IReadOnlyList<PartidaSummaryDto>> Handle(ListPartidasQuery request, CancellationToken cancellationToken)
    {
        // Sin ordenar aqui: ListAsync ya entrega ordenado (FechaCreacion DESC).
        var partidas = await _partidas.ListAsync(cancellationToken);
        return partidas
            .Select(p => new PartidaSummaryDto(
                p.PartidaId.Valor,
                p.NombrePartida.Valor,
                p.Modalidad.ToString(),
                p.ModoInicioPartida.ToString(),
                p.TiempoInicio,
                p.MinimosParticipacion,
                p.MaximosParticipacion,
                p.Estado?.ToString(),
                p.Juegos.Count,
                p.FechaCreacion))
            .ToList();
    }
}
