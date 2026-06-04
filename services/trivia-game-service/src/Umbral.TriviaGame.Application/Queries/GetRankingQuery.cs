using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Queries;

public sealed record GetRankingQuery(Guid PartidaId) : IRequest<IReadOnlyList<RankingEntryDto>>;
