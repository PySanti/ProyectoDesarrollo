using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Umbral.OperacionesSesion.Api.Workers;
using Umbral.OperacionesSesion.Application.Commands;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class MantenimientoSesionesWorkerTests
{
    [Fact]
    public async Task Un_tick_envia_ambos_barridos()
    {
        var sender = new RecordingSender();
        var sp = new ServiceCollection().AddSingleton<ISender>(sender).BuildServiceProvider();
        var worker = new MantenimientoSesionesWorker(sp.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MantenimientoSesionesWorker>.Instance);

        await worker.EjecutarTickAsync(CancellationToken.None);

        Assert.Contains(sender.Enviados, r => r is BarrerIniciosAutomaticosCommand);
        Assert.Contains(sender.Enviados, r => r is BarrerTimeoutsCommand);
        Assert.True(
            sender.Enviados.FindIndex(r => r is BarrerIniciosAutomaticosCommand)
            < sender.Enviados.FindIndex(r => r is BarrerTimeoutsCommand),
            "auto-inicios debe barrerse antes que timeouts");
    }

    [Fact]
    public async Task Falla_primer_barrido_igual_intenta_segundo()
    {
        var sender = new FailFirstSender();
        var sp = new ServiceCollection().AddSingleton<ISender>(sender).BuildServiceProvider();
        var worker = new MantenimientoSesionesWorker(sp.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MantenimientoSesionesWorker>.Instance);

        var ex = await Record.ExceptionAsync(() => worker.EjecutarTickAsync(CancellationToken.None));

        Assert.Null(ex); // el fallo del primer barrido no propaga
        Assert.Contains(sender.Enviados, r => r is BarrerTimeoutsCommand); // el segundo SÍ se intentó
    }

    [Fact]
    public async Task Excepcion_en_un_tick_no_propaga()
    {
        var sp = new ServiceCollection().AddSingleton<ISender>(new ThrowingSender()).BuildServiceProvider();
        var worker = new MantenimientoSesionesWorker(sp.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MantenimientoSesionesWorker>.Instance);

        var ex = await Record.ExceptionAsync(() => worker.EjecutarTickAsync(CancellationToken.None));
        Assert.Null(ex); // el tick swallowea
    }

    private sealed class RecordingSender : ISender
    {
        public List<object> Enviados { get; } = new();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Enviados.Add(request);
            return Task.FromResult(default(TResponse)!);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            Enviados.Add(request);
            return Task.FromResult<object?>(null);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            Enviados.Add(request!);
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class FailFirstSender : ISender
    {
        public List<object> Enviados { get; } = new();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is BarrerIniciosAutomaticosCommand) throw new InvalidOperationException("boom-primero");
            Enviados.Add(request);
            return Task.FromResult(default(TResponse)!);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => throw new NotImplementedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class ThrowingSender : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => throw new InvalidOperationException("boom");

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
