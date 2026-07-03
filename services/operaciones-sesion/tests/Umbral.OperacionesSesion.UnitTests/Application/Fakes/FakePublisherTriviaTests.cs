using System;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class FakePublisherTriviaTests
{
    [Fact]
    public async Task Fake_records_trivia_events()
    {
        var fake = new FakeSesionEventsPublisher();
        await fake.PublicarRespuestaTriviaValidadaAsync(
            new RespuestaTriviaValidadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, DateTime.UtcNow), CancellationToken.None);
        await fake.PublicarPuntajeTriviaIncrementadoAsync(
            new PuntajeTriviaIncrementadoEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, 1500), CancellationToken.None);
        await fake.PublicarPreguntaTriviaActivadaAsync(
            new PreguntaTriviaActivadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 30, DateTime.UtcNow), CancellationToken.None);
        await fake.PublicarPreguntaTriviaCerradaAsync(
            new PreguntaTriviaCerradaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "RespuestaCorrecta", DateTime.UtcNow, Guid.NewGuid()), CancellationToken.None);

        Assert.Single(fake.RespuestasValidadas);
        Assert.Single(fake.PuntajesIncrementados);
        Assert.Single(fake.PreguntasActivadas);
        Assert.Single(fake.PreguntasCerradas);
    }
}
