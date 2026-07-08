using System.Text.Json;
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

// HU-43: relato cronológico de la partida para el operador. La partida debe estar proyectada
// (404 si no); el historial en sí no depende de la proyección para escribirse.
public sealed class ObtenerHistorialPartidaQueryHandler
    : IRequestHandler<ObtenerHistorialPartidaQuery, HistorialPartidaResponse>
{
    private const int LimitMaximo = 500;

    private readonly IProyeccionesRepository _proyecciones;
    private readonly IHistorialRepository _historial;

    public ObtenerHistorialPartidaQueryHandler(IProyeccionesRepository proyecciones, IHistorialRepository historial)
    {
        _proyecciones = proyecciones;
        _historial = historial;
    }

    public async Task<HistorialPartidaResponse> Handle(
        ObtenerHistorialPartidaQuery request, CancellationToken cancellationToken)
    {
        if (request.Limit < 1 || request.Limit > LimitMaximo)
        {
            throw new ArgumentException($"limit debe estar entre 1 y {LimitMaximo}.");
        }
        if (request.Offset < 0)
        {
            throw new ArgumentException("offset no puede ser negativo.");
        }

        _ = await _proyecciones.GetPartidaAsync(request.PartidaId, cancellationToken)
            ?? throw new PartidaNoEncontradaException(request.PartidaId);

        var total = await _historial.ContarHistorialDePartidaAsync(request.PartidaId, request.TipoEvento, cancellationToken);
        var eventos = await _historial.GetHistorialDePartidaAsync(
            request.PartidaId, request.TipoEvento, request.Limit, request.Offset, cancellationToken);

        var entradas = eventos
            .Select(e => new EntradaHistorialDto(
                e.OccurredAt, e.TipoEvento, e.JuegoId, e.ParticipanteId, e.EquipoId, ParseDetalle(e.DetalleJson)))
            .ToList();
        return new HistorialPartidaResponse(request.PartidaId, total, entradas);
    }

    private static JsonElement ParseDetalle(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
