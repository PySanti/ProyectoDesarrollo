// tests/Umbral.Partidas.IntegrationTests/PartidaRepositoryTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.Infrastructure.Persistence;

namespace Umbral.Partidas.IntegrationTests;

public class PartidaRepositoryTests
{
    private static readonly DateTime T0 = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    private static PartidasDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<PartidasDbContext>().UseInMemoryDatabase(dbName).Options);

    [Fact]
    public async Task ListAsync_devuelve_la_ultima_creada_primero()
    {
        var dbName = Guid.NewGuid().ToString();

        // Se insertan desordenadas a proposito: el orden no debe depender del insert.
        var media = Partida.Crear(
            NombrePartida.Crear("Media"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, T0.AddHours(1));
        var vieja = Partida.Crear(
            NombrePartida.Crear("Vieja"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, T0);
        var nueva = Partida.Crear(
            NombrePartida.Crear("Nueva"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, T0.AddHours(2));

        await using (var ctx = NewContext(dbName))
        {
            var repo = new PartidaRepository(ctx);
            repo.Add(media);
            repo.Add(vieja);
            repo.Add(nueva);
            await new PartidasUnitOfWork(ctx).SaveChangesAsync(CancellationToken.None);
        }

        await using (var ctx = NewContext(dbName))
        {
            var listadas = await new PartidaRepository(ctx).ListAsync(CancellationToken.None);

            Assert.Equal(
                new[] { "Nueva", "Media", "Vieja" },
                listadas.Select(p => p.NombrePartida.Valor).ToArray());
        }
    }

    [Fact]
    public async Task Add_and_GetById_round_trips_partida()
    {
        var dbName = Guid.NewGuid().ToString();
        var partida = Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, T0);

        await using (var ctx = NewContext(dbName))
        {
            var repo = new PartidaRepository(ctx);
            var uow = new PartidasUnitOfWork(ctx);
            repo.Add(partida);
            await uow.SaveChangesAsync(CancellationToken.None);
        }

        await using (var ctx = NewContext(dbName))
        {
            var repo = new PartidaRepository(ctx);
            var loaded = await repo.GetByIdAsync(partida.PartidaId, CancellationToken.None);
            Assert.NotNull(loaded);
            Assert.Equal("Copa", loaded!.NombrePartida.Valor);
        }
    }

    [Fact]
    public async Task UnitOfWork_commits_partida_and_trivia_in_one_save()
    {
        var dbName = Guid.NewGuid().ToString();
        var partida = Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, T0);

        await using (var ctx = NewContext(dbName))
        {
            new PartidaRepository(ctx).Add(partida);
            await new PartidasUnitOfWork(ctx).SaveChangesAsync(CancellationToken.None);
        }

        await using (var ctx = NewContext(dbName))
        {
            var partidaRepo = new PartidaRepository(ctx);
            var triviaRepo = new JuegoTriviaRepository(ctx);
            var uow = new PartidasUnitOfWork(ctx);

            var loaded = await partidaRepo.GetByIdAsync(partida.PartidaId, CancellationToken.None);
            var trivia = JuegoTrivia.Crear(loaded!.PartidaId, 1, new[]
            {
                new PreguntaSpec("Q", new List<OpcionSpec> { new("A", true), new("B", false) }, 10, 30)
            });
            loaded.AgregarJuego(trivia.JuegoId, 1, TipoJuego.Trivia);
            triviaRepo.Add(trivia);
            partidaRepo.Update(loaded);
            await uow.SaveChangesAsync(CancellationToken.None);
        }

        await using (var ctx = NewContext(dbName))
        {
            var reloaded = await new PartidaRepository(ctx).GetByIdAsync(partida.PartidaId, CancellationToken.None);
            Assert.Single(reloaded!.Juegos);
            var trivias = await new JuegoTriviaRepository(ctx).GetByPartidaIdAsync(partida.PartidaId, CancellationToken.None);
            Assert.Single(trivias);
        }
    }
}
