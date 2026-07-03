using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ObtenerMiSesionQueryHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Trivia(Guid partidaId, Guid participante, Guid opcionOk, Guid opcionMala, bool iniciar)
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "2+2?", 100, 3600,
            new[] { new OpcionSnapshot(opcionOk, "4", true), new OpcionSnapshot(opcionMala, "5", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Q", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var s = SesionPartida.Publicar(partidaId, snap);
        s.Inscribir(participante, false, 0, T0);
        if (iniciar) s.Iniciar(T0);   // activa juego 1 + pregunta 1
        return s;
    }

    private static SesionPartida Bdt(Guid partidaId, Guid participante)
    {
        var etapa = new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 3600);
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Plaza", new[] { etapa });
        var snap = new ConfiguracionSnapshot("B", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var s = SesionPartida.Publicar(partidaId, snap);
        s.Inscribir(participante, false, 0, T0);
        s.Iniciar(T0);                // activa juego 1 + etapa 1
        return s;
    }

    [Fact]
    public async Task Sin_participacion_devuelve_null()
    {
        var handler = new ObtenerMiSesionQueryHandler(new FakeSesionPartidaRepository());
        var dto = await handler.Handle(new ObtenerMiSesionQuery(Guid.NewGuid()), CancellationToken.None);
        Assert.Null(dto);
    }

    [Fact]
    public async Task Lobby_devuelve_estado_sin_juego_activo()
    {
        var repo = new FakeSesionPartidaRepository();
        var partidaId = Guid.NewGuid(); var pid = Guid.NewGuid();
        repo.Add(Trivia(partidaId, pid, Guid.NewGuid(), Guid.NewGuid(), iniciar: false)); // queda en Lobby
        var dto = await new ObtenerMiSesionQueryHandler(repo).Handle(new ObtenerMiSesionQuery(pid), CancellationToken.None);
        Assert.NotNull(dto);
        Assert.Equal("Lobby", dto!.EstadoPartida);
        Assert.Null(dto.JuegoActivo);
        Assert.Null(dto.PreguntaActual);
        Assert.Null(dto.YaRespondioPreguntaActual);
        Assert.Equal(partidaId, dto.PartidaId);
    }

    [Fact]
    public async Task Trivia_activa_sin_responder_yaRespondio_false()
    {
        var repo = new FakeSesionPartidaRepository();
        var partidaId = Guid.NewGuid(); var pid = Guid.NewGuid();
        repo.Add(Trivia(partidaId, pid, Guid.NewGuid(), Guid.NewGuid(), iniciar: true));
        var dto = await new ObtenerMiSesionQueryHandler(repo).Handle(new ObtenerMiSesionQuery(pid), CancellationToken.None);
        Assert.Equal("Iniciada", dto!.EstadoPartida);
        Assert.Equal("Trivia", dto.JuegoActivo!.TipoJuego);
        Assert.NotNull(dto.PreguntaActual);
        Assert.Equal("2+2?", dto.PreguntaActual!.Texto);
        Assert.False(dto.YaRespondioPreguntaActual);
        Assert.Null(dto.EtapaActual);
    }

    [Fact]
    public async Task Trivia_activa_respondida_incorrecto_yaRespondio_true()
    {
        var repo = new FakeSesionPartidaRepository();
        var partidaId = Guid.NewGuid(); var pid = Guid.NewGuid();
        var opcionOk = Guid.NewGuid(); var opcionMala = Guid.NewGuid();
        var sesion = Trivia(partidaId, pid, opcionOk, opcionMala, iniciar: true);
        sesion.ResponderPregunta(pid, opcionMala, T0);   // incorrecto → la pregunta sigue activa
        repo.Add(sesion);
        var dto = await new ObtenerMiSesionQueryHandler(repo).Handle(new ObtenerMiSesionQuery(pid), CancellationToken.None);
        Assert.NotNull(dto!.PreguntaActual);             // sigue activa
        Assert.True(dto.YaRespondioPreguntaActual);
    }

    [Fact]
    public async Task Bdt_activo_expone_etapa_y_yaRespondio_null()
    {
        var repo = new FakeSesionPartidaRepository();
        var partidaId = Guid.NewGuid(); var pid = Guid.NewGuid();
        repo.Add(Bdt(partidaId, pid));
        var dto = await new ObtenerMiSesionQueryHandler(repo).Handle(new ObtenerMiSesionQuery(pid), CancellationToken.None);
        Assert.Equal("BusquedaDelTesoro", dto!.JuegoActivo!.TipoJuego);
        Assert.NotNull(dto.EtapaActual);
        Assert.Equal("Plaza", dto.EtapaActual!.AreaBusqueda);
        Assert.Null(dto.PreguntaActual);
        Assert.Null(dto.YaRespondioPreguntaActual);
    }
}
