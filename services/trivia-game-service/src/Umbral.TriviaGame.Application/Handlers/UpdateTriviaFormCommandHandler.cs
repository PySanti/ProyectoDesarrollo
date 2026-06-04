using MediatR;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Mappers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class UpdateTriviaFormCommandHandler : IRequestHandler<UpdateTriviaFormCommand, TriviaFormDetailDto>
{
    private readonly ITriviaFormRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UpdateTriviaFormCommandHandler(
        ITriviaFormRepository repository,
        IDomainEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<TriviaFormDetailDto> Handle(UpdateTriviaFormCommand request, CancellationToken cancellationToken)
    {
        var formId = TriviaFormId.Create(request.FormId);

        var form = await _repository.GetByIdAsync(formId, cancellationToken);
        if (form is null)
        {
            throw new TriviaFormNotFoundException(formId.Value);
        }

        var newTitle = FormTitle.Create(request.Title);
        var newDrafts = TriviaFormMapper.ToDrafts(request.Questions);

        if (!string.Equals(form.Title.Value, newTitle.Value, StringComparison.Ordinal))
        {
            form.UpdateTitle(newTitle);
        }

        form.ReplaceQuestions(newDrafts);

        await _repository.UpdateAsync(form, cancellationToken);
        await _eventDispatcher.DispatchAsync(form.FlushDomainEvents(), cancellationToken);

        return TriviaFormMapper.ToDto(form);
    }
}
