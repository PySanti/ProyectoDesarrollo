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

public class TriviaSnapshotPersistenceTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Persists_and_reads_back_trivia_questions_and_answer()
    {
        var partidaId = Guid.NewGuid();
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Capital?", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "Paris", true), new OpcionSnapshot(Guid.NewGuid(), "Londres", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var part = Guid.NewGuid();
        sesion.Inscribir(part, false, 0, T0);
        sesion.Iniciar(T0); // Q1 activa
        sesion.ResponderPregunta(part, pregunta.Opciones.Single(o => o.EsCorrecta).OpcionId, T0.AddSeconds(3));

        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("trivia-" + Guid.NewGuid()).Options;

        await using (var write = new OperacionesSesionDbContext(options))
        {
            new SesionPartidaRepository(write).Add(sesion);
            await new OperacionesSesionUnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using (var read = new OperacionesSesionDbContext(options))
        {
            var loaded = await new SesionPartidaRepository(read).GetByPartidaIdAsync(partidaId, CancellationToken.None);
            Assert.NotNull(loaded);
            var p = loaded!.Juegos.Single().Preguntas.Single();
            Assert.Equal(EstadoPregunta.Cerrada, p.Estado);
            Assert.Equal(MotivoCierrePregunta.RespuestaCorrecta, p.MotivoCierre);
            Assert.Equal(part, p.GanadorParticipanteId);
            Assert.Equal(2, p.Opciones.Count);
            Assert.Single(p.Respuestas);
        }
    }
}
