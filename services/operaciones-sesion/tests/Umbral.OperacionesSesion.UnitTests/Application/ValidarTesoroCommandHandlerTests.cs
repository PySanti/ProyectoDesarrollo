using System.Text;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ValidarTesoroCommandHandlerTests
{
    private sealed class TextoQrDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) =>
            imagen.Length == 0 ? null : Encoding.UTF8.GetString(imagen);
    }

    private static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

    [Fact]
    public async Task Correct_treasure_emits_validado_ganada_cerrada_activada_or_finalize()
    {
        // Arrange: sesión BDT iniciada con 2 etapas (QR-1, QR-2), jugador inscrito.
        var (repo, uow, fake, partidaId, jugador) = BdtBuilder.SesionIniciada(("QR-1", 60), ("QR-2", 60));
        var handler = new ValidarTesoroCommandHandler(
            repo, uow, fake,
            new FakeTimeProvider(new DateTime(2026, 6, 28, 10, 0, 5)),
            new TextoQrDecoder());

        // Act
        var resp = await handler.Handle(new ValidarTesoroCommand(partidaId, jugador, B64("QR-1")), default);

        // Assert
        Assert.True(resp.Gano);
        Assert.Equal(50, resp.Puntaje);
        Assert.Single(fake.TesorosValidados);
        Assert.Single(fake.EtapasGanadas);
        Assert.Single(fake.EtapasCerradas);
        Assert.Single(fake.EtapasActivadas);     // auto-avance a la etapa 2
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Winning_last_stage_publishes_partida_finalizada()
    {
        var (repo, uow, fake, partidaId, jugador) = BdtBuilder.SesionIniciada(("QR-1", 60)); // única etapa
        var handler = new ValidarTesoroCommandHandler(
            repo, uow, fake,
            new FakeTimeProvider(new DateTime(2026, 6, 28, 10, 0, 5)),
            new TextoQrDecoder());

        var resp = await handler.Handle(new ValidarTesoroCommand(partidaId, jugador, B64("QR-1")), default);

        Assert.True(resp.Gano);
        Assert.Single(fake.EtapasGanadas);
        Assert.Empty(fake.EtapasActivadas);      // no hay siguiente etapa
        Assert.Single(fake.PartidasFinalizadas); // ganar la última etapa termina la partida
        Assert.Empty(fake.JuegosActivados);
    }

    [Fact]
    public async Task Wrong_treasure_emits_only_validado()
    {
        var (repo, uow, fake, partidaId, jugador) = BdtBuilder.SesionIniciada(("QR-1", 60));
        var handler = new ValidarTesoroCommandHandler(
            repo, uow, fake,
            new FakeTimeProvider(new DateTime(2026, 6, 28, 10, 0, 5)),
            new TextoQrDecoder());

        var resp = await handler.Handle(new ValidarTesoroCommand(partidaId, jugador, B64("QR-X")), default);

        Assert.False(resp.Gano);
        Assert.Single(fake.TesorosValidados);
        Assert.Empty(fake.EtapasGanadas);
        Assert.Empty(fake.EtapasActivadas);
    }
}
