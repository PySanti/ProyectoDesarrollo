using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

// HU-38: monitoreo operador de envíos TesoroQR del juego BDT activo (7d Task 1).
public class ObtenerEnviosTesoroQueryHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 28, 10, 0, 0, DateTimeKind.Utc);

    private sealed class TextoQrDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) =>
            imagen.Length == 0 ? null : Encoding.UTF8.GetString(imagen);
    }

    private static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

    [Fact]
    public async Task Bdt_activo_con_intentos_devuelve_dto_por_etapa()
    {
        var (repo, uow, fake, partidaId, jugador) = BdtBuilder.SesionIniciada(("QR-1", 60));
        var validar = new ValidarTesoroCommandHandler(
            repo, uow, fake, new FakeTimeProvider(T0.AddSeconds(5)), new TextoQrDecoder());
        await validar.Handle(new Umbral.OperacionesSesion.Application.Commands.ValidarTesoroCommand(
            partidaId, jugador, B64("QR-EQUIVOCADO")), CancellationToken.None);

        var handler = new ObtenerEnviosTesoroQueryHandler(repo);
        var dto = await handler.Handle(new ObtenerEnviosTesoroQuery(partidaId), CancellationToken.None);

        Assert.Equal(partidaId, dto.PartidaId);
        Assert.Single(dto.Etapas);
        Assert.Equal(1, dto.Etapas[0].Orden);
        Assert.Single(dto.Etapas[0].Intentos);
        var intento = dto.Etapas[0].Intentos[0];
        Assert.Equal(jugador, intento.ParticipanteId);
        Assert.Null(intento.EquipoId);
        Assert.Equal("Invalido", intento.Resultado);
    }

    [Fact]
    public async Task Juego_activo_trivia_lanza_JuegoActivoNoEsBDTException()
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var partidaId = Guid.NewGuid();
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var insc = sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0);
        sesion.Iniciar(T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);

        var handler = new ObtenerEnviosTesoroQueryHandler(repo);

        await Assert.ThrowsAsync<JuegoActivoNoEsBDTException>(
            () => handler.Handle(new ObtenerEnviosTesoroQuery(partidaId), CancellationToken.None));
    }

    [Fact]
    public async Task Sin_juego_activo_lanza_NoHayEtapaActivaException()
    {
        var (repo, _, _, partidaId) = BdtBuilder.SesionEnLobbyConInscrito(("QR-1", 60));
        var handler = new ObtenerEnviosTesoroQueryHandler(repo);

        await Assert.ThrowsAsync<NoHayEtapaActivaException>(
            () => handler.Handle(new ObtenerEnviosTesoroQuery(partidaId), CancellationToken.None));
    }
}
