using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.Infrastructure.Persistence;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class SesionPersistenceTests
{
    private static OperacionesSesionDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("persist-" + Guid.NewGuid())
            .Options;
        return new OperacionesSesionDbContext(options);
    }

    [Fact]
    public async Task Persists_and_reads_back_session_with_games_and_inscription()
    {
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia), new JuegoResumen(Guid.NewGuid(), 2, TipoJuego.BusquedaDelTesoro) });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        sesion.Inscribir(participante, false, 0, DateTime.UtcNow);

        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("shared-db").Options;

        await using (var write = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(write);
            repo.Add(sesion);
            await new OperacionesSesionUnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using (var read = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(read);
            var loaded = await repo.GetByPartidaIdAsync(partidaId, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(EstadoSesion.Lobby, loaded!.Estado);
            Assert.Equal(2, loaded.Juegos.Count);
            Assert.Single(loaded.Inscripciones);
            Assert.Equal(participante, loaded.Inscripciones.Single().ParticipanteId);

            Assert.True(await repo.ExistsForPartidaAsync(partidaId, CancellationToken.None));
            Assert.True(await repo.ParticipanteTieneParticipacionActivaAsync(participante, Guid.NewGuid(), CancellationToken.None));
            Assert.False(await repo.ParticipanteTieneParticipacionActivaAsync(participante, partidaId, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Persists_lifecycle_state_after_start_and_advance()
    {
        var partidaId = Guid.NewGuid();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia), new JuegoResumen(Guid.NewGuid(), 2, TipoJuego.Trivia) });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        var insc = sesion.Inscribir(Guid.NewGuid(), false, 0, DateTime.UtcNow);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, DateTime.UtcNow); // HU-19: aceptar para que cuente en mínimos
        var now = new DateTime(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
        sesion.Iniciar(now);
        sesion.FinalizarJuegoActual(now); // game1 Finalizado, game2 Activo, still Iniciada

        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("lifecycle-" + Guid.NewGuid()).Options;

        await using (var write = new OperacionesSesionDbContext(options))
        {
            new SesionPartidaRepository(write).Add(sesion);
            await new OperacionesSesionUnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using (var read = new OperacionesSesionDbContext(options))
        {
            var loaded = await new SesionPartidaRepository(read).GetByPartidaIdAsync(partidaId, CancellationToken.None);
            Assert.NotNull(loaded);
            Assert.Equal(EstadoSesion.Iniciada, loaded!.Estado);
            Assert.Equal(now, loaded.FechaInicio);
            Assert.Null(loaded.FechaFin);
            var ordenados = loaded.Juegos.OrderBy(j => j.Orden).ToList();
            Assert.Equal(EstadoJuego.Finalizado, ordenados[0].Estado);
            Assert.Equal(EstadoJuego.Activo, ordenados[1].Estado);
        }
    }
}
