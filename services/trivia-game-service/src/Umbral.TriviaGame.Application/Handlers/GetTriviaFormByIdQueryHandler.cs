using MediatR;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Mappers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class GetTriviaFormByIdQueryHandler : IRequestHandler<GetTriviaFormByIdQuery, TriviaFormDetailDto?>
{
    private readonly ITriviaFormRepository _repository;

    public GetTriviaFormByIdQueryHandler(ITriviaFormRepository repository)
    {
        _repository = repository;
    }

    public async Task<TriviaFormDetailDto?> Handle(GetTriviaFormByIdQuery request, CancellationToken cancellationToken)
    {
        var formId = TriviaFormId.Create(request.FormId);

        var form = await _repository.GetByIdAsync(formId, cancellationToken);
        if (form is null)
        {
            return null;
        }

        return TriviaFormMapper.ToDto(form);
    }
}
