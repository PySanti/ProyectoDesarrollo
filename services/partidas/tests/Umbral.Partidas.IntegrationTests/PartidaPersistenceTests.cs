// tests/Umbral.Partidas.IntegrationTests/PartidaPersistenceTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.Infrastructure.Persistence;

namespace Umbral.Partidas.IntegrationTests;

public class PartidaPersistenceTests
{
    private static readonly DateTime T0 = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    private static PartidasDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<PartidasDbContext>().UseInMemoryDatabase(dbName).Options);

    [Fact]
    public async Task Partida_with_trivia_and_bdt_games_round_trips()
    {
        var dbName = Guid.NewGuid().ToString();
        var partida = Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, T0);
        var trivia = JuegoTrivia.Crear(partida.PartidaId, 1, new[]
        {
            new PreguntaSpec("Q", new List<OpcionSpec> { new("A", true), new("B", false) }, 10, 30)
        });
        var bdt = JuegoBDT.Crear(partida.PartidaId, 2, "Plaza", new[] { new EtapaSpec(1, "QR", 50, 120) });
        partida.AgregarJuego(trivia.JuegoId, 1, TipoJuego.Trivia);
        partida.AgregarJuego(bdt.JuegoId, 2, TipoJuego.BusquedaDelTesoro);

        await using (var ctx = NewContext(dbName))
        {
            ctx.Partidas.Add(partida);
            ctx.JuegosTrivia.Add(trivia);
            ctx.JuegosBDT.Add(bdt);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext(dbName))
        {
            var loaded = await ctx.Partidas.Include(p => p.Juegos)
                .FirstAsync(p => p.PartidaId == partida.PartidaId);
            Assert.Equal("Copa", loaded.NombrePartida.Valor);
            Assert.Null(loaded.Estado);
            Assert.Equal(2, loaded.Juegos.Count);

            var loadedTrivia = await ctx.JuegosTrivia
                .Include(j => j.Preguntas).ThenInclude(p => p.Opciones)
                .FirstAsync(j => j.JuegoId == trivia.JuegoId);
            Assert.Single(loadedTrivia.Preguntas);
            Assert.Equal(10, loadedTrivia.Preguntas[0].PuntajeAsignado.Valor);
            Assert.Equal(2, loadedTrivia.Preguntas[0].Opciones.Count);

            var loadedBdt = await ctx.JuegosBDT.Include(j => j.Etapas)
                .FirstAsync(j => j.JuegoId == bdt.JuegoId);
            Assert.Equal("Plaza", loadedBdt.AreaBusqueda);
            Assert.Single(loadedBdt.Etapas);
            Assert.Equal("QR", loadedBdt.Etapas[0].CodigoQREsperado);
        }
    }
}
