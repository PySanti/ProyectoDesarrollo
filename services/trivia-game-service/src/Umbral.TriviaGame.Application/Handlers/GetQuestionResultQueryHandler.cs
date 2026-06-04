using MediatR;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class GetQuestionResultQueryHandler : IRequestHandler<GetQuestionResultQuery, QuestionResultDto>
{
    private readonly IPartidaTriviaRepository _partidaRepository;
    private readonly ITriviaFormRepository _formRepository;

    public GetQuestionResultQueryHandler(
        IPartidaTriviaRepository partidaRepository,
        ITriviaFormRepository formRepository)
    {
        _partidaRepository = partidaRepository;
        _formRepository = formRepository;
    }

    public async Task<QuestionResultDto> Handle(GetQuestionResultQuery request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.Create(request.PartidaId);
        var preguntaId = QuestionId.Create(request.PreguntaId);

        var partida = await _partidaRepository.GetByIdWithRespuestasAsync(partidaId, cancellationToken);
        if (partida is null)
            throw new PartidaTriviaNotFoundException(request.PartidaId);

        if (partida.PreguntaActualId?.Value == request.PreguntaId)
            throw new DomainValidationException("La pregunta aún está activa. Espere a que se cierre para ver el resultado.");

        var form = await _formRepository.GetByIdAsync(partida.FormularioAsociadoId, cancellationToken);
        if (form is null)
            throw new DomainValidationException($"El formulario '{partida.FormularioAsociadoId.Value}' asociado a la partida no existe.");

        var question = form.Questions.FirstOrDefault(q => q.Id.Value == request.PreguntaId);
        if (question is null)
            throw new DomainValidationException($"La pregunta '{request.PreguntaId}' no pertenece al formulario asociado a la partida.");

        var correctOption = question.GetCorrectOption();
        var correctIndex = question.Options
            .Select((o, i) => new { o, i })
            .First(x => x.o.IsCorrect)
            .i;

        var miRespuesta = partida.Respuestas
            .FirstOrDefault(r => r.UsuarioId == request.UsuarioId && r.PreguntaId.Value == request.PreguntaId);

        var hayRespuestaCorrecta = partida.Respuestas
            .Any(r => r.PreguntaId.Value == request.PreguntaId && r.EsCorrecta);

        var motivoCierre = hayRespuestaCorrecta ? "RespuestaCorrecta" : "TiempoAgotado";

        return new QuestionResultDto(
            request.PreguntaId,
            question.Text.Value,
            correctIndex,
            correctOption.Text.Value,
            miRespuesta?.OpcionSeleccionadaIndex,
            miRespuesta is not null
                ? question.Options.ElementAtOrDefault(miRespuesta.OpcionSeleccionadaIndex)?.Text.Value
                : null,
            miRespuesta?.EsCorrecta,
            miRespuesta?.PuntajeObtenido ?? 0,
            miRespuesta?.TiempoEmpleadoSegundos ?? 0,
            motivoCierre
        );
    }
}
