using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerRankingJuegoQuery(Guid PartidaId, Guid JuegoId) : IRequest<RankingJuegoResponse>;
