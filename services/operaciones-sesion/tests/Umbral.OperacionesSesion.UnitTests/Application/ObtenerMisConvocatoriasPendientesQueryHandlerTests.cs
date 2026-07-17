using System;
using System.Collections.Generic;
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

public class ObtenerMisConvocatoriasPendientesQueryHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida EquipoPublicada()
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public async Task Mapea_convocatorias_pendientes_a_dto()
    {
        var repo = new FakeSesionPartidaRepository();
        var usuario = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var sesion = EquipoPublicada();
        var partidaId = sesion.PartidaId;
        var inscripcion = sesion.PreinscribirEquipo(equipoId, true, usuario, new[] { usuario }, false, 0, T0);
        sesion.AceptarInscripcion(inscripcion.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        var convocatoriaId = inscripcion.Convocatorias[0].Id.Valor;
        repo.Add(sesion);

        var handler = new ObtenerMisConvocatoriasPendientesQueryHandler(repo);
        var r = await handler.Handle(new ObtenerMisConvocatoriasPendientesQuery(usuario), CancellationToken.None);

        var dto = Assert.Single(r);
        Assert.Equal(convocatoriaId, dto.ConvocatoriaId);
        Assert.Equal(partidaId, dto.PartidaId);
        Assert.Equal(equipoId, dto.EquipoId);
        // El movil pinta este nombre: el gateway le cierra /partidas/**, asi que si no
        // viaja aqui no tiene forma de nombrar la partida.
        Assert.Equal("Copa", dto.NombrePartida);
    }

    [Fact]
    public async Task Sin_pendientes_devuelve_lista_vacia()
    {
        var repo = new FakeSesionPartidaRepository();
        var handler = new ObtenerMisConvocatoriasPendientesQueryHandler(repo);

        var r = await handler.Handle(new ObtenerMisConvocatoriasPendientesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(r);
    }
}
