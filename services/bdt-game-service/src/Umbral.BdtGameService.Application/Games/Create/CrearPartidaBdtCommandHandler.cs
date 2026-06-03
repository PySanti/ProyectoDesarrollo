using MediatR;
using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;

namespace Umbral.BdtGameService.Application.Games.Create;

public sealed class CrearPartidaBdtCommandHandler : IRequestHandler<CrearPartidaBdtCommand, CrearPartidaBdtResponse>
{
    private readonly IPartidaBdtRepository _repository;

    public CrearPartidaBdtCommandHandler(IPartidaBdtRepository repository)
    {
        _repository = repository;
    }

    public async Task<CrearPartidaBdtResponse> Handle(CrearPartidaBdtCommand request, CancellationToken cancellationToken)
    {
        var modalidad = Enum.Parse<Modalidad>(request.Modalidad, ignoreCase: true);
        var modoInicio = Enum.Parse<ModoInicioPartida>(request.ModoInicio, ignoreCase: true);
        var etapas = request.Etapas
            .OrderBy(etapa => etapa.Orden)
            .Select(etapa => EtapaBDT.Crear(etapa.Orden, etapa.CodigoQrEsperado, etapa.TiempoLimiteSegundos))
            .ToList();

        var partida = PartidaBDT.CrearPublicada(
            request.Nombre,
            modalidad,
            new AreaBusqueda(request.AreaBusqueda),
            request.MinimoParticipantes,
            request.MaximoParticipantes,
            request.MaximoEquipos,
            request.MinimoJugadoresPorEquipo,
            modoInicio,
            etapas);

        await _repository.AddAsync(partida, cancellationToken);

        return new CrearPartidaBdtResponse(
            partida.PartidaId,
            partida.Nombre,
            partida.Modalidad.ToString(),
            partida.Estado.ToString(),
            partida.AreaBusqueda.Descripcion,
            partida.ModoInicio.ToString(),
            partida.Etapas.Count);
    }
}
