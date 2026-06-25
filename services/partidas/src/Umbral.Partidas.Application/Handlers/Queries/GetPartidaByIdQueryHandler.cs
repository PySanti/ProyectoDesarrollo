using System.Linq;
using MediatR;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Application.Queries;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Application.Handlers.Queries;

public sealed class GetPartidaByIdQueryHandler : IRequestHandler<GetPartidaByIdQuery, PartidaDetailDto>
{
    private readonly IPartidaRepository _partidas;
    private readonly IJuegoTriviaRepository _trivias;
    private readonly IJuegoBDTRepository _bdts;

    public GetPartidaByIdQueryHandler(
        IPartidaRepository partidas,
        IJuegoTriviaRepository trivias,
        IJuegoBDTRepository bdts)
    {
        _partidas = partidas;
        _trivias = trivias;
        _bdts = bdts;
    }

    public async Task<PartidaDetailDto> Handle(GetPartidaByIdQuery request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.From(request.PartidaId);
        var partida = await _partidas.GetByIdAsync(partidaId, cancellationToken)
            ?? throw new PartidaNoEncontradaException(request.PartidaId);

        var trivias = (await _trivias.GetByPartidaIdAsync(partidaId, cancellationToken)).ToDictionary(j => j.JuegoId);
        var bdts = (await _bdts.GetByPartidaIdAsync(partidaId, cancellationToken)).ToDictionary(j => j.JuegoId);

        var juegos = partida.Juegos
            .OrderBy(j => j.Orden)
            .Select(reference =>
            {
                if (reference.TipoJuego == TipoJuego.Trivia && trivias.TryGetValue(reference.JuegoId, out var trivia))
                {
                    return new JuegoDto(
                        reference.JuegoId.Valor,
                        reference.Orden,
                        reference.TipoJuego.ToString(),
                        trivia.Estado.ToString(),
                        new TriviaContenidoDto(trivia.Preguntas.Select(MapPregunta).ToList()),
                        null);
                }

                if (reference.TipoJuego == TipoJuego.BusquedaDelTesoro && bdts.TryGetValue(reference.JuegoId, out var bdt))
                {
                    return new JuegoDto(
                        reference.JuegoId.Valor,
                        reference.Orden,
                        reference.TipoJuego.ToString(),
                        bdt.Estado.ToString(),
                        null,
                        new BDTContenidoDto(bdt.AreaBusqueda, bdt.Etapas.OrderBy(e => e.Orden).Select(MapEtapa).ToList()));
                }

                // Reference present but content aggregate missing — surface the reference with no content.
                return new JuegoDto(reference.JuegoId.Valor, reference.Orden, reference.TipoJuego.ToString(),
                    EstadoJuego.Pendiente.ToString(), null, null);
            })
            .ToList();

        return new PartidaDetailDto(
            partida.PartidaId.Valor,
            partida.NombrePartida.Valor,
            partida.Modalidad.ToString(),
            partida.ModoInicioPartida.ToString(),
            partida.TiempoInicio,
            partida.MinimosParticipacion,
            partida.MaximosParticipacion,
            partida.Estado?.ToString(),
            juegos);
    }

    private static PreguntaDto MapPregunta(Pregunta p) => new(
        p.PreguntaId,
        p.Texto,
        p.PuntajeAsignado.Valor,
        p.TiempoLimiteSegundos,
        p.Opciones.Select(o => new OpcionDto(o.OpcionId, o.Texto, o.EsCorrecta)).ToList());

    private static EtapaDto MapEtapa(EtapaBDT e) => new(
        e.EtapaBDTId,
        e.Orden,
        e.CodigoQREsperado,
        e.PuntajeAsignado.Valor,
        e.TiempoLimiteSegundos);
}
