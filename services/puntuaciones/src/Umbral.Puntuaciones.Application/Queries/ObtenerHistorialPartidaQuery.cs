using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerHistorialPartidaQuery(
    Guid PartidaId, int Limit, int Offset, string? TipoEvento) : IRequest<HistorialPartidaResponse>;
