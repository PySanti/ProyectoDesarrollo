using MediatR;
using Microsoft.Extensions.Options;
using Umbral.OperacionesSesion.Api.Configuration;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Api.Workers;

public sealed class MantenimientoSesionesWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MantenimientoSesionesWorker> _logger;
    private readonly int _intervaloMs;

    // ctor de runtime (con opciones)
    public MantenimientoSesionesWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<MantenimientoSesionesWorker> logger,
        IOptions<MantenimientoOptions> options)
        : this(scopeFactory, logger, options.Value.IntervaloMs) { }

    // ctor de test (intervalo por defecto)
    public MantenimientoSesionesWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<MantenimientoSesionesWorker> logger)
        : this(scopeFactory, logger, 1000) { }

    private MantenimientoSesionesWorker(
        IServiceScopeFactory scopeFactory, ILogger<MantenimientoSesionesWorker> logger, int intervaloMs)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _intervaloMs = intervaloMs <= 0 ? 1000 : intervaloMs;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_intervaloMs));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await EjecutarTickAsync(stoppingToken);
        }
    }

    public async Task EjecutarTickAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await EnviarBarridoAsync(sender, new BarrerIniciosAutomaticosCommand(), cancellationToken);
            await EnviarBarridoAsync(sender, new BarrerTimeoutsCommand(), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw; // shutdown del host: salida limpia, sin log de error espurio
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tick de mantenimiento de sesiones falló (scope/resolución); se reintenta al próximo tick.");
        }
    }

    // Cada barrido es independiente: el fallo de uno no debe impedir que el otro corra en el mismo tick.
    private async Task EnviarBarridoAsync<TRespuesta>(ISender sender, IRequest<TRespuesta> comando, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(comando, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw; // shutdown: aborta el tick (no se corre el segundo barrido)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Barrido {Comando} falló; se reintenta al próximo tick.", comando.GetType().Name);
        }
    }
}
