using MediatR;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Application.Queries;

public sealed record GetPublishedTriviaGamesQuery(string? Modalidad = null)
    : IRequest<IReadOnlyList<TriviaGameListItemDto>>;
