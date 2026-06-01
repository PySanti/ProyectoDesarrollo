using MediatR;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Mappers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class StartTriviaGameCommandHandler : IRequestHandler<StartTriviaGameCommand, TriviaGameDetailDto>
{
    private readonly IPartidaTriviaRepository _partidaRepository;
    private readonly ITriviaFormRepository _formRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ITriviaLobbyNotifier _lobbyNotifier;

    public StartTriviaGameCommandHandler(
        IPartidaTriviaRepository partidaRepository,
        ITriviaFormRepository formRepository,
        IDomainEventDispatcher eventDispatcher,
        ITriviaLobbyNotifier lobbyNotifier)
    {
        _partidaRepository = partidaRepository;
        _formRepository = formRepository;
        _eventDispatcher = eventDispatcher;
        _lobbyNotifier = lobbyNotifier;
    }

    public async Task<TriviaGameDetailDto> Handle(StartTriviaGameCommand request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.Create(request.PartidaId);

        var partida = await _partidaRepository.GetByIdAsync(partidaId, cancellationToken);
        if (partida is null)
        {
            throw new PartidaTriviaNotFoundException(request.PartidaId);
        }

        var form = await _formRepository.GetByIdAsync(partida.FormularioAsociadoId, cancellationToken);
        if (form is null)
        {
            throw new DomainValidationException(
                $"El formulario '{partida.FormularioAsociadoId.Value}' asociado a la partida no existe.");
        }

        var firstQuestion = form.Questions
            .OrderBy(q => q.DisplayOrder)
            .FirstOrDefault();
        if (firstQuestion is null)
        {
            throw new DomainValidationException(
                "El formulario asociado a la partida no contiene preguntas.");
        }

        var cantidadInscriptos = await _partidaRepository.CountInscripcionesAsync(partidaId, cancellationToken);

        partida.Iniciar(cantidadInscriptos, esInicioManual: true, primeraPreguntaId: firstQuestion.Id);

        await _partidaRepository.UpdateAsync(partida, cancellationToken);
        await _eventDispatcher.DispatchAsync(partida.FlushDomainEvents(), cancellationToken);

        await _lobbyNotifier.NotifyGameStarted(request.PartidaId, cancellationToken);

        return TriviaGameMapper.ToDto(partida);
    }
}
