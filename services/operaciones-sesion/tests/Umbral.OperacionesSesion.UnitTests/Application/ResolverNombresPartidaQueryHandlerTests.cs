using System;
using System.Collections.Generic;
using System.Linq;
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

public class ResolverNombresPartidaQueryHandlerTests
{
    private static SesionPartida Publicada(string nombre)
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot(nombre, Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public async Task Resuelve_nombres_por_lote()
    {
        var repo = new FakeSesionPartidaRepository();
        var copa = Publicada("Copa UMBRAL");
        var liga = Publicada("Liga UCAB");
        repo.Add(copa);
        repo.Add(liga);

        var handler = new ResolverNombresPartidaQueryHandler(repo);
        var r = await handler.Handle(
            new ResolverNombresPartidaQuery(new[] { copa.PartidaId, liga.PartidaId }), CancellationToken.None);

        Assert.Equal(2, r.Partidas.Count);
        Assert.Equal("Copa UMBRAL", r.Partidas.Single(p => p.PartidaId == copa.PartidaId).Nombre);
        Assert.Equal("Liga UCAB", r.Partidas.Single(p => p.PartidaId == liga.PartidaId).Nombre);
    }

    [Fact]
    public async Task Id_desconocido_se_omite_sin_lanzar()
    {
        var repo = new FakeSesionPartidaRepository();
        var copa = Publicada("Copa UMBRAL");
        repo.Add(copa);

        var handler = new ResolverNombresPartidaQueryHandler(repo);
        var r = await handler.Handle(
            new ResolverNombresPartidaQuery(new[] { copa.PartidaId, Guid.NewGuid() }), CancellationToken.None);

        // Contrato con el cliente: lo pedido que no vuelve se cachea como no-resoluble y
        // cae al GUID corto. Devolver nombre: null obligaria al movil a distinguir dos vacios.
        var dto = Assert.Single(r.Partidas);
        Assert.Equal(copa.PartidaId, dto.PartidaId);
    }

    [Fact]
    public async Task Lista_vacia_no_toca_el_repositorio()
    {
        var repo = new FakeSesionPartidaRepository();
        var handler = new ResolverNombresPartidaQueryHandler(repo);

        var r = await handler.Handle(
            new ResolverNombresPartidaQuery(Array.Empty<Guid>()), CancellationToken.None);

        Assert.Empty(r.Partidas);
        Assert.Equal(0, repo.GetNombresLlamadas);
    }
}
