using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

public sealed class ObtenerMarcadorQueryHandler : IRequestHandler<ObtenerMarcadorQuery, MarcadorResponse>
{
    private readonly IProyeccionesRepository _repo;

    public ObtenerMarcadorQueryHandler(IProyeccionesRepository repo) => _repo = repo;

    public async Task<MarcadorResponse> Handle(ObtenerMarcadorQuery request, CancellationToken cancellationToken)
    {
        var juego = await _repo.GetJuegoAsync(request.JuegoId, cancellationToken);
        if (juego is null || juego.PartidaId != request.PartidaId)
        {
            throw new JuegoNoEncontradoException(request.JuegoId);
        }

        var marcadores = await _repo.GetMarcadoresDeJuegoAsync(request.JuegoId, cancellationToken);
        var participaciones = await _repo.GetParticipacionesDePartidaAsync(request.PartidaId, cancellationToken);
        // Quien se inscribio ve su 0 y su posicion en vez de un 404: aparece en el ranking del
        // juego, asi que consultar su propio marcador no puede decir que no existe.
        var entradas = RankingCalculator.Calcular(marcadores, participaciones);
        var propia = entradas.FirstOrDefault(e => e.CompetidorId == request.CompetidorId)
            ?? throw new MarcadorNoEncontradoException(request.JuegoId, request.CompetidorId);

        return new MarcadorResponse(
            propia.CompetidorId, propia.TipoCompetidor, propia.Puntos,
            propia.TiempoAcumuladoMs, propia.UnidadesGanadas, propia.Posicion);
    }
}
