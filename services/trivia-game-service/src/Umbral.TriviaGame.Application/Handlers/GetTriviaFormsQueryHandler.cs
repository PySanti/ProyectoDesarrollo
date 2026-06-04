using MediatR;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class GetTriviaFormsQueryHandler : IRequestHandler<GetTriviaFormsQuery, IReadOnlyList<TriviaFormListItemDto>>
{
    private readonly ITriviaFormRepository _repository;

    public GetTriviaFormsQueryHandler(ITriviaFormRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TriviaFormListItemDto>> Handle(GetTriviaFormsQuery request, CancellationToken cancellationToken)
    {
        var forms = await _repository.GetAllAsync(cancellationToken);

        return forms
            .Select(f => new TriviaFormListItemDto(
                f.Id.Value,
                f.Title.Value,
                f.IsComplete,
                f.Questions.Count,
                f.CreatedAtUtc))
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToList();
    }
}
