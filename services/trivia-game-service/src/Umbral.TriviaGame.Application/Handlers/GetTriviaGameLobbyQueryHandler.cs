using MediatR;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class GetTriviaGameLobbyQueryHandler : IRequestHandler<GetTriviaGameLobbyQuery, TriviaGameLobbyDto>
{
    private readonly IPartidaTriviaRepository _partidaRepository;
    private readonly ITriviaInscripcionRepository _inscripcionRepository;

    public GetTriviaGameLobbyQueryHandler(
        IPartidaTriviaRepository partidaRepository,
        ITriviaInscripcionRepository inscripcionRepository)
    {
        _partidaRepository = partidaRepository;
        _inscripcionRepository = inscripcionRepository;
    }

    public async Task<TriviaGameLobbyDto> Handle(GetTriviaGameLobbyQuery request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.Create(request.PartidaId);

        var partida = await _partidaRepository.GetByIdAsync(partidaId, cancellationToken);
        if (partida is null)
            throw new PartidaTriviaNotFoundException(request.PartidaId);

        var inscrito = await _inscripcionRepository.ExistsByPartidaYUsuarioAsync(partidaId, request.UsuarioId, cancellationToken);
        if (!inscrito)
            throw new UsuarioNoInscritoException(request.UsuarioId, request.PartidaId);

        var inscripciones = await _inscripcionRepository.ListByPartidaIdAsync(partidaId, cancellationToken);

        var participantes = inscripciones
            .Select(i => new TriviaInscripcionLobbyDto(i.Id.Value, i.UsuarioId, i.FechaInscripcion))
            .ToList();

        return new TriviaGameLobbyDto(
            partida.Id.Value,
            partida.Nombre.Value,
            partida.Estado.ToString(),
            partida.Modalidad.ToString(),
            partida.TiempoInicio.Value,
            partida.MinimoParticipantes is not null ? partida.MinimoParticipantes.Value : 0,
            partida.MaximoJugadores is not null ? partida.MaximoJugadores.Value : 0,
            inscripciones.Count,
            participantes);
    }
}
