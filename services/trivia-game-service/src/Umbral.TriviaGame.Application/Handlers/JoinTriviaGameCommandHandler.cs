using MediatR;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class JoinTriviaGameCommandHandler : IRequestHandler<JoinTriviaGameCommand, TriviaInscripcionDto>
{
    private readonly IPartidaTriviaRepository _partidaRepository;
    private readonly ITriviaInscripcionRepository _inscripcionRepository;

    public JoinTriviaGameCommandHandler(
        IPartidaTriviaRepository partidaRepository,
        ITriviaInscripcionRepository inscripcionRepository)
    {
        _partidaRepository = partidaRepository;
        _inscripcionRepository = inscripcionRepository;
    }

    public async Task<TriviaInscripcionDto> Handle(JoinTriviaGameCommand request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.Create(request.PartidaId);

        var partida = await _partidaRepository.GetByIdAsync(partidaId, cancellationToken);
        if (partida is null)
            throw new PartidaTriviaNotFoundException(request.PartidaId);

        if (partida.Estado != PartidaEstado.Lobby)
            throw new InvalidStateTransitionException(partida.Estado.ToString(), "Lobby");

        if (partida.Modalidad != Modalidad.Individual)
            throw new ModalidadInvalidaException(partida.Modalidad.ToString(),
                "Debes ser líder de un equipo para entrar en este evento.");

        var inscriptos = await _inscripcionRepository.CountByPartidaIdAsync(partidaId, cancellationToken);
        if (partida.MaximoJugadores is not null && inscriptos >= partida.MaximoJugadores.Value)
            throw new CupoLlenoException(partida.MaximoJugadores.Value);

        var yaInscrito = await _inscripcionRepository.ExistsByPartidaYUsuarioAsync(partidaId, request.UsuarioId, cancellationToken);
        if (yaInscrito)
            throw new JugadorYaInscritoException(request.UsuarioId);

        var inscripcion = TriviaInscripcion.Create(partidaId, request.UsuarioId);
        await _inscripcionRepository.AddAsync(inscripcion, cancellationToken);

        return new TriviaInscripcionDto(
            inscripcion.Id.Value,
            inscripcion.PartidaId.Value,
            inscripcion.FechaInscripcion);
    }
}
