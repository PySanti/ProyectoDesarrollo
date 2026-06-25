using System.Linq;
using MediatR;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Application.Handlers.Commands;

public sealed class AgregarJuegoBDTCommandHandler : IRequestHandler<AgregarJuegoBDTCommand, AgregarJuegoResponse>
{
    private readonly IPartidaRepository _partidas;
    private readonly IJuegoBDTRepository _juegos;
    private readonly IPartidasUnitOfWork _unitOfWork;

    public AgregarJuegoBDTCommandHandler(
        IPartidaRepository partidas,
        IJuegoBDTRepository juegos,
        IPartidasUnitOfWork unitOfWork)
    {
        _partidas = partidas;
        _juegos = juegos;
        _unitOfWork = unitOfWork;
    }

    public async Task<AgregarJuegoResponse> Handle(AgregarJuegoBDTCommand request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.From(request.PartidaId);
        var partida = await _partidas.GetByIdAsync(partidaId, cancellationToken)
            ?? throw new PartidaNoEncontradaException(request.PartidaId);

        var etapas = request.Etapas
            .Select(e => new EtapaSpec(e.Orden, e.CodigoQREsperado, e.Puntaje, e.TiempoLimiteSegundos))
            .ToList();

        var juego = JuegoBDT.Crear(partidaId, request.Orden, request.AreaBusqueda, etapas);
        partida.AgregarJuego(juego.JuegoId, request.Orden, TipoJuego.BusquedaDelTesoro);

        _juegos.Add(juego);
        _partidas.Update(partida);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AgregarJuegoResponse(juego.JuegoId.Valor);
    }
}
