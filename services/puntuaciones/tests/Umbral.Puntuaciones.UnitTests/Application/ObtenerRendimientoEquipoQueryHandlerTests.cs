using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ObtenerRendimientoEquipoQueryHandlerTests
{
    [Fact]
    public async Task Equipo_sin_participaciones_devuelve_lista_vacia()
    {
        var handler = new ObtenerRendimientoEquipoQueryHandler(new FakeProyeccionesRepository());

        var response = await handler.Handle(new ObtenerRendimientoEquipoQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(response.Partidas);
    }

    [Fact]
    public async Task Rendimiento_lista_posicion_y_gano_por_partida_mas_reciente_primero()
    {
        var equipo = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var ganada = Guid.NewGuid();     // el equipo queda 1º — la más antigua
        var perdida = Guid.NewGuid();    // el equipo queda 2º — la más reciente
        var repo = new FakeProyeccionesRepository();

        void SembrarPartida(Guid partidaId, DateTime fechaFin)
        {
            var partida = PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Equipo);
            partida.MarcarTerminada(fechaFin);
            repo.AddPartida(partida);
        }
        void SembrarMarcador(Guid partidaId, Guid competidorId, int puntos, long tiempo)
        {
            var m = Marcador.Nuevo(Guid.NewGuid(), competidorId, partidaId, TipoCompetidor.Equipo);
            m.Acreditar(puntos, tiempo);
            repo.AddMarcador(m);
        }

        SembrarPartida(ganada, new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        SembrarMarcador(ganada, equipo, 20, 1000);
        SembrarMarcador(ganada, rival, 10, 1000);
        SembrarPartida(perdida, new DateTime(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc));
        SembrarMarcador(perdida, equipo, 5, 1000);
        SembrarMarcador(perdida, rival, 15, 1000);

        var handler = new ObtenerRendimientoEquipoQueryHandler(repo);
        var response = await handler.Handle(new ObtenerRendimientoEquipoQuery(equipo), CancellationToken.None);

        Assert.Equal(equipo, response.EquipoId);
        Assert.Equal(2, response.Partidas.Count);
        Assert.Equal(perdida, response.Partidas[0].PartidaId);
        Assert.Equal(2, response.Partidas[0].Posicion);
        Assert.False(response.Partidas[0].Gano);
        Assert.Equal(ganada, response.Partidas[1].PartidaId);
        Assert.Equal(1, response.Partidas[1].Posicion);
        Assert.True(response.Partidas[1].Gano);
    }

    [Fact]
    public async Task Incluye_partida_donde_el_equipo_no_anoto()
    {
        var repo = new FakeProyeccionesRepository();
        var equipoId = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var partida = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Equipo);
        partida.MarcarTerminada(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        repo.AddPartida(partida);
        repo.AddParticipacion(ParticipacionProyectada.Nueva(partida.PartidaId, equipoId, TipoCompetidor.Equipo));
        var marcadorRival = Marcador.Nuevo(Guid.NewGuid(), rival, partida.PartidaId, TipoCompetidor.Equipo);
        marcadorRival.Acreditar(20, 1000);
        repo.AddMarcador(marcadorRival);

        var response = await new ObtenerRendimientoEquipoQueryHandler(repo).Handle(
            new ObtenerRendimientoEquipoQuery(equipoId), CancellationToken.None);

        // Antes: solo partidas "donde el equipo anotó".
        var fila = Assert.Single(response.Partidas);
        Assert.Equal(partida.PartidaId, fila.PartidaId);
        Assert.Equal(2, fila.Posicion);
        Assert.False(fila.Gano);
    }

    [Fact]
    public async Task Partida_anterior_al_slice_sin_participacion_proyectada_sigue_listandose()
    {
        var repo = new FakeProyeccionesRepository();
        var equipoId = Guid.NewGuid();
        var partida = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Equipo);
        partida.MarcarTerminada(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        repo.AddPartida(partida);
        // Sin fila de participación: no hay backfill. El marcador prueba que jugó.
        var marcador = Marcador.Nuevo(Guid.NewGuid(), equipoId, partida.PartidaId, TipoCompetidor.Equipo);
        marcador.Acreditar(20, 1000);
        repo.AddMarcador(marcador);

        var response = await new ObtenerRendimientoEquipoQueryHandler(repo).Handle(
            new ObtenerRendimientoEquipoQuery(equipoId), CancellationToken.None);

        Assert.Single(response.Partidas);
    }
}
