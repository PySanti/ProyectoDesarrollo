using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Queries;

public sealed record GetOperatorSupervisableTriviaGamesQuery()
    : IRequest<IReadOnlyList<TriviaGameListItemDto>>;
