using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.Puntuaciones.Api.Workers;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Workers;

public class PurgaEventosProcesadosServiceTests
{
    [Fact]
    public async Task Pasada_elimina_lo_anterior_a_la_retencion_y_conserva_lo_reciente()
    {
        var repo = new FakeProyeccionesRepository();
        var uow = new FakePuntuacionesUnitOfWork();
        var viejo = EventoProcesado.Registrar(Guid.NewGuid(), "PartidaIniciada",
            DateTime.UtcNow.AddDays(-40), DateTime.UtcNow.AddDays(-40));
        var reciente = EventoProcesado.Registrar(Guid.NewGuid(), "PartidaIniciada",
            DateTime.UtcNow, DateTime.UtcNow);
        repo.RegistrarEventoProcesado(viejo);
        repo.RegistrarEventoProcesado(reciente);

        var services = new ServiceCollection();
        services.AddSingleton<IProyeccionesRepository>(repo);
        services.AddSingleton<IPuntuacionesUnitOfWork>(uow);
        using var provider = services.BuildServiceProvider();

        var purga = new PurgaEventosProcesadosService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new RetencionOptions { EventosProcesadosDias = 30 },
            NullLogger<PurgaEventosProcesadosService>.Instance);

        await purga.EjecutarPasadaAsync(CancellationToken.None);

        var restante = Assert.Single(repo.EventosProcesados);
        Assert.Equal(reciente.EventId, restante.EventId);
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public void Retencion_default_es_30_dias()
    {
        Assert.Equal(30, new RetencionOptions().EventosProcesadosDias);
        Assert.Equal("Retencion", RetencionOptions.SectionName);
    }
}
