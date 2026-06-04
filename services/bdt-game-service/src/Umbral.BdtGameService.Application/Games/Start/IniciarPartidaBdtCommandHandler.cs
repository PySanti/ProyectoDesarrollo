using MediatR;
using Microsoft.Extensions.Logging;
using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Application.Abstractions.Realtime;

namespace Umbral.BdtGameService.Application.Games.Start;

public sealed class IniciarPartidaBdtCommandHandler : IRequestHandler<IniciarPartidaBdtCommand, IniciarPartidaBdtResponse>
{
    private readonly IPartidaBdtRepository _repository;
    private readonly IPartidaBdtRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<IniciarPartidaBdtCommandHandler> _logger;

    public IniciarPartidaBdtCommandHandler(
        IPartidaBdtRepository repository,
        IPartidaBdtRealtimeNotifier realtimeNotifier,
        ILogger<IniciarPartidaBdtCommandHandler> logger)
    {
        _repository = repository;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    public async Task<IniciarPartidaBdtResponse> Handle(IniciarPartidaBdtCommand request, CancellationToken cancellationToken)
    {
        var response = await _repository.ExecuteWithPartidaRegistrationLockAsync(
            request.PartidaId,
            async innerCancellationToken =>
            {
                var partida = await _repository.GetByIdWithExploradoresAsync(request.PartidaId, innerCancellationToken);
                if (partida is null)
                {
                    throw new KeyNotFoundException("Partida BDT no encontrada.");
                }

                var etapaActiva = partida.IniciarManualmente(request.OperadorUserId, DateTime.UtcNow);
                await _repository.UpdateAsync(partida, innerCancellationToken);

                return new IniciarPartidaBdtResponse(
                    partida.PartidaId,
                    partida.Nombre,
                    partida.Estado.ToString(),
                    partida.Modalidad.ToString(),
                    new EtapaActivaBdtResponse(
                        etapaActiva.EtapaId,
                        etapaActiva.Orden,
                        etapaActiva.TiempoLimiteSegundos,
                        etapaActiva.IniciadaEnUtc!.Value,
                        etapaActiva.CierraEnUtc!.Value),
                    "Partida BDT iniciada.");
            },
            cancellationToken);

        try
        {
            await _realtimeNotifier.NotifyPartidaBdtIniciadaAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo al publicar PartidaBDTIniciada para {PartidaId} despues del commit.", response.PartidaId);
        }

        return response;
    }
}
