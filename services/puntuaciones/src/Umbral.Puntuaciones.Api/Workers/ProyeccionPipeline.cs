using MediatR;

namespace Umbral.Puntuaciones.Api.Workers;

// Camino único proyección→difusión (SP-4c): scope propio por comando; la difusión solo ocurre si la
// proyección tuvo éxito y nunca lanza (el dispatcher degrada todo fallo a warning). Extraído del
// worker para que integración pueda ejercitar el mismo código sin broker.
public sealed class ProyeccionPipeline
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ProyeccionPipeline(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task EjecutarAsync(object comando, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando, ct);
        var difusor = scope.ServiceProvider.GetRequiredService<RankingBroadcastDispatcher>();
        await difusor.DifundirAsync(comando, ct);
    }
}
