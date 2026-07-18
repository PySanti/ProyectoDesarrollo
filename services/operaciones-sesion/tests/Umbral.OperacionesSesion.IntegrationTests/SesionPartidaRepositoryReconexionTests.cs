using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class SesionPartidaRepositoryReconexionTests
{
    private static readonly DateTime T0 = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    private static DbContextOptions<OperacionesSesionDbContext> InMemoryOptions(string name) =>
        new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options;

    // Sesión BDT Individual con N etapas; opcionalmente inscrita + iniciada.
    // min: MinimosParticipacion (por defecto 1); las 3 llamadas existentes no cambian.
    private static SesionPartida BuildSesion(Guid partidaId, bool inscribir, Guid participanteId, bool iniciar, int min = 1)
    {
        var etapas = new List<EtapaSnapshot> { new(Guid.NewGuid(), 1, "QR-1", 50, 3600) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Plaza", etapas);
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, min, 10,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        if (inscribir)
        {
            // HU-19: la inscripción nace Pendiente; aceptarla para que quede Activa.
            var insc = sesion.Inscribir(participanteId, false, 0, T0);
            sesion.AceptarInscripcion(insc.Id.Valor, 0, T0);
        }
        if (iniciar) sesion.Iniciar(T0);
        return sesion;
    }

    [Fact]
    public async Task Devuelve_sesion_viva_con_inscripcion_activa_y_grafo_cargado()
    {
        var options = InMemoryOptions("recon-hit-" + Guid.NewGuid());
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            ctx.Sesiones.Add(BuildSesion(partidaId, inscribir: true, participante, iniciar: true));
            await ctx.SaveChangesAsync();
        }
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(ctx);
            var sesion = await repo.GetByParticipanteActivoAsync(participante, CancellationToken.None);
            Assert.NotNull(sesion);
            Assert.Equal(partidaId, sesion!.PartidaId);
            Assert.Single(sesion.Juegos);                       // grafo cargado vía Include
            Assert.Single(sesion.Juegos[0].Etapas);
        }
    }

    [Fact]
    public async Task Null_cuando_participante_no_tiene_inscripcion()
    {
        var options = InMemoryOptions("recon-miss-" + Guid.NewGuid());
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            ctx.Sesiones.Add(BuildSesion(Guid.NewGuid(), inscribir: false, Guid.NewGuid(), iniciar: false));
            await ctx.SaveChangesAsync();
        }
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(ctx);
            Assert.Null(await repo.GetByParticipanteActivoAsync(Guid.NewGuid(), CancellationToken.None));
        }
    }

    [Fact]
    public async Task Null_cuando_inscripcion_cancelada()
    {
        var options = InMemoryOptions("recon-cancel-" + Guid.NewGuid());
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        var sesion = BuildSesion(partidaId, inscribir: true, participante, iniciar: false);
        sesion.CancelarInscripcion(participante);               // queda en Lobby, inscripción Cancelada
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            ctx.Sesiones.Add(sesion);
            await ctx.SaveChangesAsync();
        }
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(ctx);
            Assert.Null(await repo.GetByParticipanteActivoAsync(participante, CancellationToken.None));
        }
    }

    // ── Tests de regresión: filtro "live" (Lobby || Iniciada) en GetByParticipanteActivoAsync ──

    [Fact]
    public async Task Null_cuando_partida_cancelada_con_inscripcion_activa()
    {
        // Sesión Cancelada con la inscripción aún Activa (cancelación manual del operador — HU-40 —
        // no toca las inscripciones). Prueba que el null viene del filtro de estado de la sesión, no
        // del filtro de inscripción.
        var options = InMemoryOptions("recon-sesion-cancelada-" + Guid.NewGuid());
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();

        var sesion = BuildSesion(partidaId, inscribir: true, participante, iniciar: false, min: 2);
        sesion.Cancelar(T0); // Estado=Cancelada; la inscripción permanece Activa

        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            ctx.Sesiones.Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(ctx);
            Assert.Null(await repo.GetByParticipanteActivoAsync(participante, CancellationToken.None));
        }

        // Belt-and-suspenders: confirmar que la inscripción sigue Activa (el null no es por inscripción)
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(ctx);
            var sesionRaw = await repo.GetByPartidaIdAsync(partidaId, CancellationToken.None);
            Assert.NotNull(sesionRaw);
            Assert.Contains(sesionRaw!.Inscripciones, i => i.ParticipanteId == participante && i.EsActiva);
        }
    }

    [Fact]
    public async Task Null_cuando_partida_terminada_con_inscripcion_activa()
    {
        // Ciclo completo BDT (1 etapa): Iniciada → AvanzarEtapa (cierra la única etapa) →
        // FinalizarJuegoActual (sin etapas abiertas, sin juegos pendientes) → Estado=Terminada.
        // La inscripción permanece Activa; el null viene del filtro de estado de la sesión.
        var options = InMemoryOptions("recon-sesion-terminada-" + Guid.NewGuid());
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();

        var sesion = BuildSesion(partidaId, inscribir: true, participante, iniciar: true);
        // Estado=Iniciada, único juego BDT Activo, única etapa Activa
        sesion.AvanzarEtapa(T0);         // cierra etapa por operador; ActivarSiguienteEtapa→null (sin más etapas)
        sesion.FinalizarJuegoActual(T0); // TieneEtapasAbiertas=false → Finalizar juego; sin juegos pendientes → Terminada

        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            ctx.Sesiones.Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(ctx);
            Assert.Null(await repo.GetByParticipanteActivoAsync(participante, CancellationToken.None));
        }

        // Belt-and-suspenders: confirmar que la inscripción sigue Activa (el null no es por inscripción)
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(ctx);
            var sesionRaw = await repo.GetByPartidaIdAsync(partidaId, CancellationToken.None);
            Assert.NotNull(sesionRaw);
            Assert.Contains(sesionRaw!.Inscripciones, i => i.ParticipanteId == participante && i.EsActiva);
        }
    }
}
