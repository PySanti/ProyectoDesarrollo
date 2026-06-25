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

public sealed class AgregarJuegoTriviaCommandHandler : IRequestHandler<AgregarJuegoTriviaCommand, AgregarJuegoResponse>
{
    private readonly IPartidaRepository _partidas;
    private readonly IJuegoTriviaRepository _juegos;
    private readonly IPartidasUnitOfWork _unitOfWork;

    public AgregarJuegoTriviaCommandHandler(
        IPartidaRepository partidas,
        IJuegoTriviaRepository juegos,
        IPartidasUnitOfWork unitOfWork)
    {
        _partidas = partidas;
        _juegos = juegos;
        _unitOfWork = unitOfWork;
    }

    public async Task<AgregarJuegoResponse> Handle(AgregarJuegoTriviaCommand request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.From(request.PartidaId);
        var partida = await _partidas.GetByIdAsync(partidaId, cancellationToken)
            ?? throw new PartidaNoEncontradaException(request.PartidaId);

        var preguntas = request.Preguntas
            .Select(p => new PreguntaSpec(
                p.Texto,
                p.Opciones.Select(o => new OpcionSpec(o.Texto, o.EsCorrecta)).ToList(),
                p.Puntaje,
                p.TiempoLimiteSegundos))
            .ToList();

        // Build the game first (validates question content), then register the ordered
        // reference on the Partida (validates orden uniqueness). If either throws, nothing
        // is staged, so the single SaveChanges below is never reached.
        var juego = JuegoTrivia.Crear(partidaId, request.Orden, preguntas);
        partida.AgregarJuego(juego.JuegoId, request.Orden, TipoJuego.Trivia);

        _juegos.Add(juego);
        _partidas.Update(partida);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AgregarJuegoResponse(juego.JuegoId.Valor);
    }
}
