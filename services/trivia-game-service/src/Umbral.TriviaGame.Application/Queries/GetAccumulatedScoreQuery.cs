using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Queries;

public sealed record GetAccumulatedScoreQuery(
    Guid PartidaId,
    string UsuarioId
) : IRequest<AccumulatedScoreDto>;
