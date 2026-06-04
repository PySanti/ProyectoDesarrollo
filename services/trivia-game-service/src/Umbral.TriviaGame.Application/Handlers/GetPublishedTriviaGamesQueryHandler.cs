using MediatR;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Mappers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class GetPublishedTriviaGamesQueryHandler
    : IRequestHandler<GetPublishedTriviaGamesQuery, IReadOnlyList<TriviaGameListItemDto>>
{
    private readonly IPartidaTriviaRepository _repository;

    public GetPublishedTriviaGamesQueryHandler(IPartidaTriviaRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TriviaGameListItemDto>> Handle(
        GetPublishedTriviaGamesQuery request,
        CancellationToken cancellationToken)
    {
        var partidas = await _repository.GetPublishedAsync(cancellationToken);

        var result = partidas
            .Select(TriviaGameMapper.ToListItemDto);

        if (!string.IsNullOrWhiteSpace(request.Modalidad))
        {
            var modalidad = TriviaGameMapper.ParseModalidad(request.Modalidad);
            result = result.Where(g => g.Modalidad == modalidad.ToString());
        }

        return result.ToList();
    }
}
