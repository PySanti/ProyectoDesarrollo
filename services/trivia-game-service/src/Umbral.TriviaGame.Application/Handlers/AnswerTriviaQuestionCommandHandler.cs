using MediatR;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class AnswerTriviaQuestionCommandHandler : IRequestHandler<AnswerTriviaQuestionCommand, RespuestaTriviaDto>
{
    private readonly IPartidaTriviaRepository _partidaRepository;
    private readonly ITriviaFormRepository _formRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public AnswerTriviaQuestionCommandHandler(
        IPartidaTriviaRepository partidaRepository,
        ITriviaFormRepository formRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _partidaRepository = partidaRepository;
        _formRepository = formRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<RespuestaTriviaDto> Handle(AnswerTriviaQuestionCommand request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.Create(request.PartidaId);
        var preguntaId = QuestionId.Create(request.PreguntaId);

        var partida = await _partidaRepository.GetByIdWithRespuestasAsync(partidaId, cancellationToken);
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

        var question = form.Questions.FirstOrDefault(q => q.Id.Value == request.PreguntaId);
        if (question is null)
        {
            throw new DomainValidationException(
                $"La pregunta '{request.PreguntaId}' no pertenece al formulario asociado a la partida.");
        }

        var selectedOption = question.Options.ElementAtOrDefault(request.OpcionIndex);
        if (selectedOption is null)
        {
            throw new DomainValidationException(
                $"La opción en el índice '{request.OpcionIndex}' no existe en la pregunta.");
        }

        var esCorrecta = selectedOption.IsCorrect;
        var assignedScore = question.AssignedScore.Value;
        var timeLimitSeconds = question.TimeLimit.Seconds;

        var correctOptionText = question.GetCorrectOption().Text.Value;

        var respuesta = partida.RegistrarRespuestaDefinitiva(
            preguntaId,
            request.UsuarioId,
            request.OpcionIndex,
            esCorrecta,
            assignedScore,
            timeLimitSeconds,
            respuestaCorrecta: correctOptionText);

        if (esCorrecta)
        {
            var preguntasOrdenadas = form.Questions
                .OrderBy(q => q.DisplayOrder)
                .ToList();

            var currentIndex = preguntasOrdenadas.FindIndex(q => q.Id.Value == request.PreguntaId);
            var nextQuestion = currentIndex >= 0 && currentIndex < preguntasOrdenadas.Count - 1
                ? preguntasOrdenadas[currentIndex + 1]
                : null;

            if (nextQuestion is not null)
            {
                partida.AvanzarPregunta(nextQuestion.Id);
            }
            else
            {
                partida.FinalizarPartida();
            }
        }

        await _partidaRepository.UpdateAsync(partida, cancellationToken);
        await _eventDispatcher.DispatchAsync(partida.FlushDomainEvents(), cancellationToken);

        return new RespuestaTriviaDto(
            respuesta.Id.Value,
            respuesta.PartidaId.Value,
            respuesta.PreguntaId.Value,
            respuesta.EsCorrecta,
            respuesta.PuntajeObtenido,
            respuesta.TiempoEmpleadoSegundos,
            respuesta.FechaRespuesta);
    }
}
