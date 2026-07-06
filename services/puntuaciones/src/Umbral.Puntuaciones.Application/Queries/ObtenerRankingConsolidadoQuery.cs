using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerRankingConsolidadoQuery(Guid PartidaId) : IRequest<RankingConsolidadoResponse>;
