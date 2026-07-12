using System;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ListarPartidasPublicadasQueryHandlerTests
{
    private static SesionPartida PublishedSession(Guid partidaId, string nombre = "Copa")
    {
        var snapshot = new ConfiguracionSnapshot(nombre, Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia) });
        return SesionPartida.Publicar(partidaId, snapshot);
    }

    [Fact]
    public async Task Lista_solo_sesiones_en_lobby_con_conteo_de_inscritos()
    {
        var repo = new FakeSesionPartidaRepository();
        var enLobby = PublishedSession(Guid.NewGuid(), "Abierta");
        enLobby.Inscribir(Guid.NewGuid(), false, 0, DateTime.UtcNow);
        repo.Add(enLobby);
        var iniciada = PublishedSession(Guid.NewGuid(), "Cerrada");
        iniciada.Inscribir(Guid.NewGuid(), false, 0, DateTime.UtcNow);
        iniciada.Iniciar(DateTime.UtcNow);
        repo.Add(iniciada);
        var handler = new ListarPartidasPublicadasQueryHandler(repo);

        var lista = await handler.Handle(new ListarPartidasPublicadasQuery(), CancellationToken.None);

        var unica = Assert.Single(lista);
        Assert.Equal("Abierta", unica.Nombre);
        Assert.Equal("Individual", unica.Modalidad);
        Assert.Equal("Manual", unica.ModoInicioPartida);
        Assert.Equal(1, unica.InscritosActivos);
        Assert.Equal(1, unica.MinimosParticipacion);
        Assert.Equal(10, unica.MaximosParticipacion);
    }

    [Fact]
    public async Task Sin_sesiones_en_lobby_devuelve_lista_vacia()
    {
        var handler = new ListarPartidasPublicadasQueryHandler(new FakeSesionPartidaRepository());
        var lista = await handler.Handle(new ListarPartidasPublicadasQuery(), CancellationToken.None);
        Assert.Empty(lista);
    }
}
