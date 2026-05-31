using MediatR;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Mappers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Handlers;

public sealed class CreateTriviaGameCommandHandler : IRequestHandler<CreateTriviaGameCommand, TriviaGameDetailDto>
{
    private readonly IPartidaTriviaRepository _partidaRepository;
    private readonly ITriviaFormRepository _formRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateTriviaGameCommandHandler(
        IPartidaTriviaRepository partidaRepository,
        ITriviaFormRepository formRepository,
        ICurrentUserService currentUser,
        IDomainEventDispatcher eventDispatcher)
    {
        _partidaRepository = partidaRepository;
        _formRepository = formRepository;
        _currentUser = currentUser;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<TriviaGameDetailDto> Handle(CreateTriviaGameCommand request, CancellationToken cancellationToken)
    {
        var formularioId = TriviaFormId.Create(request.FormularioId);

        var formulario = await _formRepository.GetByIdAsync(formularioId, cancellationToken);
        if (formulario is null)
        {
            throw new TriviaFormNotFoundException(formularioId.Value);
        }

        if (!formulario.IsComplete)
        {
            throw new FormularioIncompletoException(formularioId.Value);
        }

        var nombre = NombrePartida.Create(request.Nombre);
        var modalidad = TriviaGameMapper.ToModalidad(request.Modalidad);
        var modoInicio = TriviaGameMapper.ToModoInicio(request.ModoInicio);
        var tiempoInicio = TiempoInicio.Create(request.TiempoInicio);
        var minimo = CantidadMinima.Create(request.MinimoParticipantes);

        CantidadMaximaJugadores? maxJugadores = request.MaximoJugadores.HasValue
            ? CantidadMaximaJugadores.Create(request.MaximoJugadores.Value, minimo)
            : null;

        CantidadMaximaEquipos? maxEquipos = request.MaximoEquipos.HasValue
            ? CantidadMaximaEquipos.Create(request.MaximoEquipos.Value)
            : null;

        JugadoresPorEquipoMin? minPorEquipo = null;
        JugadoresPorEquipoMax? maxPorEquipo = null;

        if (request.MinimoJugadoresPorEquipo.HasValue)
        {
            minPorEquipo = JugadoresPorEquipoMin.Create(request.MinimoJugadoresPorEquipo.Value);
            maxPorEquipo = request.MaximoJugadoresPorEquipo.HasValue
                ? JugadoresPorEquipoMax.Create(request.MaximoJugadoresPorEquipo.Value, minPorEquipo)
                : null;
        }

        var operatorId = OperatorId.Create(_currentUser.OperatorId);

        var partida = PartidaTrivia.Create(
            nombre, modalidad, modoInicio, formularioId, operatorId,
            tiempoInicio, minimo,
            maxJugadores, maxEquipos,
            minPorEquipo, maxPorEquipo);

        await _partidaRepository.AddAsync(partida, cancellationToken);
        await _eventDispatcher.DispatchAsync(partida.FlushDomainEvents(), cancellationToken);

        return TriviaGameMapper.ToDto(partida);
    }
}
