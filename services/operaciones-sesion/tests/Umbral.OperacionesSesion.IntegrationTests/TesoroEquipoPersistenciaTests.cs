using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class TesoroEquipoPersistenciaTests
{
    private static readonly DateTime T0 = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private sealed class TextoQrDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) =>
            imagen.Length == 0 ? null : System.Text.Encoding.UTF8.GetString(imagen);
    }

    private static OperacionesSesionDbContext NewCtx(string name) =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options);

    [Fact]
    public async Task Tesoro_de_equipo_persiste_equipoid_y_ganadorequipoid()
    {
        var db = "tesoro-eq-" + Guid.NewGuid();
        var lider = Guid.NewGuid(); var equipo = Guid.NewGuid();
        Guid partidaId;

        await using (var ctx = NewCtx(db))
        {
            var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
            var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
            var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
            var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
            partidaId = sesion.PartidaId;
            var ins = sesion.PreinscribirEquipo(equipo, true, new[] { lider }, false, 0, T0);
            sesion.ResponderConvocatoria(ins.Convocatorias.Single().Id.Valor, lider, true, false, T0);
            sesion.Iniciar(T0);
            sesion.ValidarTesoro(lider, System.Text.Encoding.UTF8.GetBytes("QR-1"), T0.AddSeconds(5), new TextoQrDecoder());
            new SesionPartidaRepository(ctx).Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx).GetByPartidaIdAsync(partidaId, default);
            var etapa = r!.Juegos.Single().Etapas.Single(e => e.Orden == 1);
            Assert.Equal(equipo, etapa.GanadorEquipoId);
            Assert.Equal(equipo, etapa.Tesoros.Single().EquipoId);
        }
    }
}
