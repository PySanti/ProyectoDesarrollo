using MediatR;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Application.Handlers.Commands;

public sealed class CrearPartidaCommandHandler : IRequestHandler<CrearPartidaCommand, CrearPartidaResponse>
{
    private readonly IPartidaRepository _partidas;
    private readonly IPartidasUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CrearPartidaCommandHandler(
        IPartidaRepository partidas, IPartidasUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _partidas = partidas;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<CrearPartidaResponse> Handle(CrearPartidaCommand request, CancellationToken cancellationToken)
    {
        var partida = Partida.Crear(
            NombrePartida.Crear(request.NombrePartida),
            request.Modalidad,
            request.ModoInicioPartida,
            request.TiempoInicio,
            request.MinimosParticipacion,
            request.MaximosParticipacion,
            _timeProvider.GetUtcNow().UtcDateTime);

        _partidas.Add(partida);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CrearPartidaResponse(partida.PartidaId.Valor);
    }
}
