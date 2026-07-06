using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.Puntuaciones.Api.Workers;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Interfaces;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Api;

namespace Umbral.Puntuaciones.UnitTests.Workers;

public sealed class FakeRankingRealtimePublisher : IRankingRealtimePublisher
{
    public List<(string Mensaje, Guid PartidaId, object Payload)> Publicados { get; } = new();
    public bool LanzarError { get; set; }

    private Task Registrar(string mensaje, Guid partidaId, object payload)
    {
        if (LanzarError)
        {
            throw new InvalidOperationException("hub caído");
        }
        Publicados.Add((mensaje, partidaId, payload));
        return Task.CompletedTask;
    }

    public Task PublicarRankingTriviaActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken) =>
        Registrar("Trivia", partidaId, ranking);

    public Task PublicarRankingBdtActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken) =>
        Registrar("BDT", partidaId, ranking);

    public Task PublicarRankingConsolidadoCalculadoAsync(RankingConsolidadoResponse ranking, CancellationToken cancellationToken) =>
        Registrar("Consolidado", ranking.PartidaId, ranking);
}

// ISender que siempre lanza: cubre la rama "la query falla" (p.ej. PartidaNoTerminadaException
// si PartidaFinalizada se proyectó sobre una partida que quedó Cancelada).
public sealed class SenderQueFalla : ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("query falló");

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("query falló");

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
        throw new InvalidOperationException("query falló");

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}

public class RankingBroadcastDispatcherTests
{
    private static readonly DateTime Ahora = DateTime.UtcNow;

    private static RankingJuegoResponse RankingJuego() =>
        new(Guid.NewGuid(), TipoJuego.Trivia, Ahora,
            new[] { new EntradaRankingDto(1, Guid.NewGuid(), TipoCompetidor.Participante, 10, 1500, 1) });

    private static RankingBroadcastDispatcher Construir(ISender sender, IRankingRealtimePublisher publisher) =>
        new(sender, publisher, NullLogger<RankingBroadcastDispatcher>.Instance);

    [Fact]
    public async Task Puntaje_trivia_resuelve_ranking_del_juego_y_publica_Trivia()
    {
        var ranking = RankingJuego();
        var sender = new FakeSender(ranking);
        var publisher = new FakeRankingRealtimePublisher();
        var comando = new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, 1500, null);

        await Construir(sender, publisher).DifundirAsync(comando, CancellationToken.None);

        var query = Assert.IsType<ObtenerRankingJuegoQuery>(sender.LastRequest);
        Assert.Equal(comando.PartidaId, query.PartidaId);
        Assert.Equal(comando.JuegoId, query.JuegoId);
        var publicado = Assert.Single(publisher.Publicados);
        Assert.Equal(("Trivia", comando.PartidaId), (publicado.Mensaje, publicado.PartidaId));
        Assert.Same(ranking, publicado.Payload);
    }

    [Fact]
    public async Task Etapa_bdt_ganada_publica_BDT()
    {
        var sender = new FakeSender(RankingJuego());
        var publisher = new FakeRankingRealtimePublisher();
        var comando = new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 25, 4000, null);

        await Construir(sender, publisher).DifundirAsync(comando, CancellationToken.None);

        Assert.Equal("BDT", Assert.Single(publisher.Publicados).Mensaje);
    }

    [Fact]
    public async Task Partida_finalizada_resuelve_consolidado_y_publica()
    {
        var partidaId = Guid.NewGuid();
        var consolidado = new RankingConsolidadoResponse(partidaId, Ahora, Array.Empty<EntradaRankingConsolidadoDto>());
        var sender = new FakeSender(consolidado);
        var publisher = new FakeRankingRealtimePublisher();
        var comando = new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), Ahora);

        await Construir(sender, publisher).DifundirAsync(comando, CancellationToken.None);

        var query = Assert.IsType<ObtenerRankingConsolidadoQuery>(sender.LastRequest);
        Assert.Equal(partidaId, query.PartidaId);
        Assert.Equal(("Consolidado", partidaId), (Assert.Single(publisher.Publicados).Mensaje, Assert.Single(publisher.Publicados).PartidaId));
    }

    [Fact]
    public async Task Comandos_sin_ranking_no_publican_ni_consultan()
    {
        var sender = new FakeSender(null);
        var publisher = new FakeRankingRealtimePublisher();
        var comando = new ProyectarPartidaIniciadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Ahora);

        await Construir(sender, publisher).DifundirAsync(comando, CancellationToken.None);

        Assert.Null(sender.LastRequest);
        Assert.Empty(publisher.Publicados);
    }

    [Fact]
    public async Task Fallo_de_la_query_no_propaga()
    {
        var publisher = new FakeRankingRealtimePublisher();
        var comando = new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Ahora);

        await Construir(new SenderQueFalla(), publisher).DifundirAsync(comando, CancellationToken.None);

        Assert.Empty(publisher.Publicados);
    }

    [Fact]
    public async Task Fallo_del_publisher_no_propaga()
    {
        var sender = new FakeSender(RankingJuego());
        var publisher = new FakeRankingRealtimePublisher { LanzarError = true };
        var comando = new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, 1500, null);

        await Construir(sender, publisher).DifundirAsync(comando, CancellationToken.None);

        Assert.Empty(publisher.Publicados);
    }
}
