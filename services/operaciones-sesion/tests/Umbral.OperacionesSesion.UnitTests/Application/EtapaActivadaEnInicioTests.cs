using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class EtapaActivadaEnInicioTests
{
    [Fact]
    public async Task Iniciar_bdt_emits_partida_iniciada_juego_activado_and_etapa_activada()
    {
        // Sesión BDT publicada (en Lobby), jugador inscrito, modo Manual, min=1.
        var (repo, uow, fake, partidaId) = BdtBuilder.SesionEnLobbyConInscrito(("QR-1", 60));
        var handler = new IniciarPartidaCommandHandler(repo, uow, fake, new FakeTimeProvider(new DateTime(2026, 6, 28, 10, 0, 0)));
        await handler.Handle(new IniciarPartidaCommand(partidaId), default);
        Assert.Single(fake.PartidasIniciadas);
        Assert.Single(fake.JuegosActivados);
        Assert.Single(fake.EtapasActivadas);
        Assert.Equal(1, fake.EtapasActivadas[0].Orden);
    }
}
