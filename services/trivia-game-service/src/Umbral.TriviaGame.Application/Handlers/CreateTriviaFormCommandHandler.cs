using MediatR;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Mappers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class CreateTriviaFormCommandHandler : IRequestHandler<CreateTriviaFormCommand, TriviaFormDetailDto>
{
    private readonly ITriviaFormRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateTriviaFormCommandHandler(
        ITriviaFormRepository repository,
        ICurrentUserService currentUser,
        IDomainEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _currentUser = currentUser;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<TriviaFormDetailDto> Handle(CreateTriviaFormCommand request, CancellationToken cancellationToken)
    {
        var title = FormTitle.Create(request.Title);
        var operatorId = OperatorId.Create(_currentUser.OperatorId);
        var drafts = TriviaFormMapper.ToDrafts(request.Questions);

        var form = TriviaForm.Create(title, operatorId, drafts);

        await _repository.AddAsync(form, cancellationToken);
        await _eventDispatcher.DispatchAsync(form.FlushDomainEvents(), cancellationToken);

        return TriviaFormMapper.ToDto(form);
    }
}
