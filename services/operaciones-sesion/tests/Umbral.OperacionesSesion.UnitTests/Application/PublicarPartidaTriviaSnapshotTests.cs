// PublicarPartidaTriviaSnapshotTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class PublicarPartidaTriviaSnapshotTests
{
    private sealed class StubClient : IConfiguracionPartidaClient
    {
        private readonly ConfiguracionPartidaDto _dto;
        public StubClient(ConfiguracionPartidaDto dto) => _dto = dto;
        public Task<ConfiguracionPartidaDto?> ObtenerConfiguracionAsync(Guid p, string? b, CancellationToken c)
            => Task.FromResult<ConfiguracionPartidaDto?>(_dto);
    }

    [Fact]
    public async Task Publicar_captures_trivia_questions_into_snapshot()
    {
        var partidaId = Guid.NewGuid();
        var trivia = new TriviaConfigDto(new List<PreguntaConfigDto>
        {
            new(Guid.NewGuid(), "Capital?", 10, 30, new List<OpcionConfigDto>
            {
                new(Guid.NewGuid(), "Paris", true), new(Guid.NewGuid(), "Londres", false)
            })
        });
        var config = new ConfiguracionPartidaDto("Copa", "Individual", "Manual", null, 1, 10,
            new List<JuegoResumenDto> { new(Guid.NewGuid(), 1, "Trivia", trivia) });

        var repo = new FakeSesionPartidaRepository();
        var handler = new PublicarPartidaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(),
            new StubClient(config), new FakeSesionEventsPublisher());

        await handler.Handle(new PublicarPartidaCommand(partidaId, null), CancellationToken.None);

        var sesion = repo.Store[partidaId];
        var juego = sesion.Juegos.Single();
        Assert.Equal(TipoJuego.Trivia, juego.TipoJuego);
        Assert.Single(juego.Preguntas);
        var pregunta = juego.Preguntas.Single();
        Assert.Equal(1, pregunta.Orden);
        Assert.Equal(10, pregunta.PuntajeAsignado);
        Assert.Equal(2, pregunta.Opciones.Count);
        Assert.Single(pregunta.Opciones.Where(o => o.EsCorrecta));
    }
}
