using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class RespuestaEquipoPersistenciaTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private static OperacionesSesionDbContext NewCtx(string name) =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options);

    [Fact]
    public async Task Respuesta_de_equipo_persiste_equipoid_y_ganadorequipoid()
    {
        var db = "resp-eq-" + Guid.NewGuid();
        var lider = Guid.NewGuid(); var equipo = Guid.NewGuid();
        var opcionOk = Guid.NewGuid();
        Guid partidaId;

        await using (var ctx = NewCtx(db))
        {
            var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
                new[] { new OpcionSnapshot(opcionOk, "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
            var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
            var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
            var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
            partidaId = sesion.PartidaId;
            var ins = sesion.PreinscribirEquipo(equipo, true, new[] { lider }, false, 0, T0);
            sesion.ResponderConvocatoria(ins.Convocatorias.Single().Id.Valor, lider, true, false, T0);
            sesion.Iniciar(T0);
            sesion.ResponderPregunta(lider, opcionOk, T0.AddSeconds(5));
            new SesionPartidaRepository(ctx).Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx).GetByPartidaIdAsync(partidaId, default);
            var preg = r!.Juegos.Single().Preguntas.Single(p => p.Orden == 1);
            Assert.Equal(equipo, preg.GanadorEquipoId);
            Assert.Equal(equipo, preg.Respuestas.Single().EquipoId);
        }
    }
}
