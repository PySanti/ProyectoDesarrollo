using MediatR;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Mappers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class GetOperatorSupervisableTriviaGamesQueryHandler
    : IRequestHandler<GetOperatorSupervisableTriviaGamesQuery, IReadOnlyList<TriviaGameListItemDto>>
{
    private readonly IPartidaTriviaRepository _repository;

    public GetOperatorSupervisableTriviaGamesQueryHandler(IPartidaTriviaRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TriviaGameListItemDto>> Handle(
        GetOperatorSupervisableTriviaGamesQuery request,
        CancellationToken cancellationToken)
    {
        var partidas = await _repository.GetSupervisableForOperatorAsync(cancellationToken);

        return partidas
            .Select(TriviaGameMapper.ToListItemDto)
            .ToList();
    }
}
