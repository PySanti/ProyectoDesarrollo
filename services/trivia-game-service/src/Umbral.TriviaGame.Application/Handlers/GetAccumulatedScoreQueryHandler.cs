using MediatR;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class GetAccumulatedScoreQueryHandler : IRequestHandler<GetAccumulatedScoreQuery, AccumulatedScoreDto>
{
    private readonly IPartidaTriviaRepository _partidaRepository;

    public GetAccumulatedScoreQueryHandler(IPartidaTriviaRepository partidaRepository)
    {
        _partidaRepository = partidaRepository;
    }

    public async Task<AccumulatedScoreDto> Handle(GetAccumulatedScoreQuery request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.Create(request.PartidaId);

        var partida = await _partidaRepository.GetByIdWithRespuestasAsync(partidaId, cancellationToken);
        if (partida is null)
            throw new PartidaTriviaNotFoundException(request.PartidaId);

        var respuestasDelParticipante = partida.Respuestas
            .Where(r => r.UsuarioId == request.UsuarioId)
            .ToList();

        var puntajeAcumulado = partida.ObtenerPuntajeAcumulado(request.UsuarioId);
        var tiempoAcumulado = partida.ObtenerTiempoRespuestaAcumulado(request.UsuarioId);
        var respuestasCorrectas = respuestasDelParticipante.Count(r => r.EsCorrecta);
        var totalRespuestas = respuestasDelParticipante.Count;

        return new AccumulatedScoreDto(
            request.PartidaId,
            puntajeAcumulado,
            tiempoAcumulado,
            respuestasCorrectas,
            totalRespuestas);
    }
}
