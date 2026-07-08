using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Api.Workers;

// Retención de eventos_procesados (deuda SP-4a): el dedup solo necesita cubrir la ventana de
// redelivery del broker; 30 días sobra. Jamás toca eventos_historial (RB-31 exige el historial
// visible) ni su dedup propio (índice único de EventId).
public sealed class PurgaEventosProcesadosService : BackgroundService
{
    private static readonly TimeSpan PrimeraPasada = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RetencionOptions _options;
    private readonly ILogger<PurgaEventosProcesadosService> _logger;

    public PurgaEventosProcesadosService(
        IServiceScopeFactory scopeFactory,
        RetencionOptions options,
        ILogger<PurgaEventosProcesadosService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(PrimeraPasada, stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EjecutarPasadaAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo en la purga de eventos_procesados; se reintenta en la próxima pasada.");
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    public async Task EjecutarPasadaAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProyeccionesRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IPuntuacionesUnitOfWork>();

        var limite = DateTime.UtcNow.AddDays(-_options.EventosProcesadosDias);
        var eliminados = await repo.EliminarEventosProcesadosAnterioresAsync(limite, ct);
        await uow.SaveChangesAsync(ct);

        if (eliminados > 0)
        {
            _logger.LogInformation(
                "Purga de eventos_procesados: {Eliminados} filas anteriores a {Limite:o} eliminadas.",
                eliminados, limite);
        }
    }
}
