using MediatR;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class GetTriviaGameTeamsQueryHandler
    : IRequestHandler<GetTriviaGameTeamsQuery, IReadOnlyList<TriviaEquipoLobbyDto>>
{
    private readonly IPartidaTriviaRepository _partidaRepository;
    private readonly ITriviaInscripcionRepository _inscripcionRepository;

    public GetTriviaGameTeamsQueryHandler(
        IPartidaTriviaRepository partidaRepository,
        ITriviaInscripcionRepository inscripcionRepository)
    {
        _partidaRepository = partidaRepository;
        _inscripcionRepository = inscripcionRepository;
    }

    public async Task<IReadOnlyList<TriviaEquipoLobbyDto>> Handle(
        GetTriviaGameTeamsQuery request,
        CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.Create(request.PartidaId);

        var partida = await _partidaRepository.GetByIdAsync(partidaId, cancellationToken);
        if (partida is null)
            throw new PartidaTriviaNotFoundException(request.PartidaId);

        var inscripciones = await _inscripcionRepository.ListByPartidaIdAsync(partidaId, cancellationToken);

        var equipos = inscripciones
            .Where(i => i.EquipoId is not null)
            .GroupBy(i => i.EquipoId!)
            .Select(g => new TriviaEquipoLobbyDto(g.Key, g.Min(i => i.FechaInscripcion)))
            .OrderBy(e => e.FechaInscripcion)
            .ToList();

        return equipos;
    }
}
