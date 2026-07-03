using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ObtenerEstadoSesionQueryHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Returns_estado_games_and_active_game_orden()
    {
        var partidaId = Guid.NewGuid();
        var lista = Enumerable.Range(1, 2).Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia)).ToList();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, lista);
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.Iniciar(T0); // game 1 → Activo
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerEstadoSesionQueryHandler(repo);

        var dto = await handler.Handle(new ObtenerEstadoSesionQuery(partidaId), CancellationToken.None);

        Assert.Equal("Iniciada", dto.Estado);
        Assert.Equal("Individual", dto.Modalidad);
        Assert.Equal(2, dto.Juegos.Count);
        Assert.Equal(1, dto.JuegoActualOrden);
        Assert.Equal("Activo", dto.Juegos.Single(j => j.Orden == 1).Estado);
        Assert.Equal("Pendiente", dto.Juegos.Single(j => j.Orden == 2).Estado);
    }

    [Fact]
    public async Task Unknown_partida_throws()
    {
        var handler = new ObtenerEstadoSesionQueryHandler(new FakeSesionPartidaRepository());
        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new ObtenerEstadoSesionQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
