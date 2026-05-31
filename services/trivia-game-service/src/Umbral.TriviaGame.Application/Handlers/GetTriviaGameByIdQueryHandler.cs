using MediatR;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Mappers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class GetTriviaGameByIdQueryHandler : IRequestHandler<GetTriviaGameByIdQuery, TriviaGameDetailDto?>
{
    private readonly IPartidaTriviaRepository _repository;

    public GetTriviaGameByIdQueryHandler(IPartidaTriviaRepository repository)
    {
        _repository = repository;
    }

    public async Task<TriviaGameDetailDto?> Handle(GetTriviaGameByIdQuery request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.Create(request.PartidaId);

        var partida = await _repository.GetByIdAsync(partidaId, cancellationToken);
        if (partida is null)
        {
            return null;
        }

        return TriviaGameMapper.ToDto(partida);
    }
}
