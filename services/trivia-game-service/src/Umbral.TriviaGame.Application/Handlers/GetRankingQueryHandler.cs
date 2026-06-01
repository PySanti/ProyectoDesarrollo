using MediatR;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class GetRankingQueryHandler : IRequestHandler<GetRankingQuery, IReadOnlyList<RankingEntryDto>>
{
    private readonly IPartidaTriviaRepository _partidaRepository;
    private readonly ITriviaInscripcionRepository _inscripcionRepository;

    public GetRankingQueryHandler(
        IPartidaTriviaRepository partidaRepository,
        ITriviaInscripcionRepository inscripcionRepository)
    {
        _partidaRepository = partidaRepository;
        _inscripcionRepository = inscripcionRepository;
    }

    public async Task<IReadOnlyList<RankingEntryDto>> Handle(GetRankingQuery request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.Create(request.PartidaId);

        var partida = await _partidaRepository.GetByIdWithRespuestasAsync(partidaId, cancellationToken);
        if (partida is null)
            throw new PartidaTriviaNotFoundException(request.PartidaId);

        var inscripciones = await _inscripcionRepository.ListByPartidaIdAsync(partidaId, cancellationToken);
        if (inscripciones.Count == 0)
            return Array.Empty<RankingEntryDto>();

        var entries = inscripciones
            .Select(i => new
            {
                i.UsuarioId,
                Puntaje = partida.ObtenerPuntajeAcumulado(i.UsuarioId),
                Tiempo = partida.ObtenerTiempoRespuestaAcumulado(i.UsuarioId),
                Correctas = partida.Respuestas.Count(r => r.UsuarioId == i.UsuarioId && r.EsCorrecta),
                Total = partida.Respuestas.Count(r => r.UsuarioId == i.UsuarioId)
            })
            .OrderByDescending(e => e.Puntaje)
            .ThenBy(e => e.Tiempo)
            .Select((e, idx) => new RankingEntryDto(
                e.UsuarioId,
                e.Puntaje,
                e.Tiempo,
                e.Correctas,
                e.Total,
                idx + 1))
            .ToList();

        return entries.AsReadOnly();
    }
}
