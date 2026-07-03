using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ResponderConvocatoriaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static (FakeSesionPartidaRepository repo, Guid convocatoriaId, Guid usuario) SesionConConvocatoria()
    {
        var partidaId = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var usuario = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { usuario }, false, 0, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        return (repo, insc.Convocatorias[0].Id.Valor, usuario);
    }

    [Fact]
    public async Task Aceptar_publica_convocatoria_respondida_aceptada()
    {
        var (repo, convocatoriaId, usuario) = SesionConConvocatoria();
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderConvocatoriaCommandHandler(repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        var resp = await handler.Handle(new ResponderConvocatoriaCommand(convocatoriaId, usuario, true), default);

        Assert.Equal("Aceptada", resp.Estado);
        var evt = Assert.Single(events.ConvocatoriasRespondidas);
        Assert.Equal("Aceptada", evt.EstadoConvocatoria);
        Assert.Equal(usuario, evt.UsuarioId);
    }

    [Fact]
    public async Task Convocatoria_inexistente_lanza()
    {
        var (repo, _, usuario) = SesionConConvocatoria();
        var handler = new ResponderConvocatoriaCommandHandler(
            repo, new FakeSesionEventsPublisher(), new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<ConvocatoriaNoEncontradaException>(
            () => handler.Handle(new ResponderConvocatoriaCommand(Guid.NewGuid(), usuario, true), default));
    }
}
