using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.Handlers.Commands;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ProyectarCicloDeVidaHandlersTests
{
    private readonly FakeProyeccionesRepository _repo = new();
    private readonly FakePuntuacionesUnitOfWork _uow = new();
    private static readonly DateTime Ahora = new(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PartidaPublicada_crea_la_proyeccion_en_lobby()
    {
        var cmd = new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Modalidad.Equipo);

        await new ProyectarPartidaPublicadaCommandHandler(_repo, _uow).Handle(cmd, CancellationToken.None);

        var partida = Assert.Single(_repo.Partidas);
        Assert.Equal(cmd.PartidaId, partida.PartidaId);
        Assert.Equal(Modalidad.Equipo, partida.Modalidad);
        Assert.Equal(EstadoPartidaProyectada.Lobby, partida.Estado);
        Assert.Single(_repo.EventosProcesados);
        Assert.Equal(1, _uow.Saves);
    }

    [Fact]
    public async Task Evento_duplicado_no_tiene_efecto()
    {
        var cmd = new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Modalidad.Individual);
        var handler = new ProyectarPartidaPublicadaCommandHandler(_repo, _uow);

        await handler.Handle(cmd, CancellationToken.None);
        await handler.Handle(cmd, CancellationToken.None);

        Assert.Single(_repo.Partidas);
        Assert.Single(_repo.EventosProcesados);
        Assert.Equal(1, _uow.Saves);
    }

    [Fact]
    public async Task PartidaIniciada_sin_publicacion_previa_crea_stub_iniciada()
    {
        var cmd = new ProyectarPartidaIniciadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Ahora);

        await new ProyectarPartidaIniciadaCommandHandler(_repo, _uow).Handle(cmd, CancellationToken.None);

        var partida = Assert.Single(_repo.Partidas);
        Assert.Equal(EstadoPartidaProyectada.Iniciada, partida.Estado);
        Assert.Null(partida.Modalidad);
        Assert.Equal(Ahora, partida.FechaInicio);
    }

    [Fact]
    public async Task Publicacion_tardia_completa_modalidad_sin_retroceder_estado()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        await new ProyectarPartidaFinalizadaCommandHandler(_repo, _uow).Handle(
            new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora), CancellationToken.None);

        await new ProyectarPartidaPublicadaCommandHandler(_repo, _uow).Handle(
            new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual), CancellationToken.None);

        var partida = Assert.Single(_repo.Partidas);
        Assert.Equal(EstadoPartidaProyectada.Terminada, partida.Estado);
        Assert.Equal(Modalidad.Individual, partida.Modalidad);
    }

    [Fact]
    public async Task PartidaCancelada_marca_cancelada_con_fecha_fin()
    {
        var cmd = new ProyectarPartidaCanceladaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Ahora);

        await new ProyectarPartidaCanceladaCommandHandler(_repo, _uow).Handle(cmd, CancellationToken.None);

        var partida = Assert.Single(_repo.Partidas);
        Assert.Equal(EstadoPartidaProyectada.Cancelada, partida.Estado);
        Assert.Equal(Ahora, partida.FechaFin);
    }

    [Fact]
    public async Task JuegoActivado_registra_el_juego_una_sola_vez()
    {
        var juegoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var handler = new ProyectarJuegoActivadoCommandHandler(_repo, _uow);

        await handler.Handle(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), juegoId, 1, TipoJuego.BusquedaDelTesoro), CancellationToken.None);
        await handler.Handle(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), juegoId, 1, TipoJuego.BusquedaDelTesoro), CancellationToken.None);

        var juego = Assert.Single(_repo.Juegos);
        Assert.Equal(TipoJuego.BusquedaDelTesoro, juego.TipoJuego);
        Assert.Equal(partidaId, juego.PartidaId);
    }
}
