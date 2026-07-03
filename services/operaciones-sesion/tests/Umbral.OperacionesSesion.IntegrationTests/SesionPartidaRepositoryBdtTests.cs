using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.Infrastructure.Persistence;

namespace Umbral.OperacionesSesion.IntegrationTests;

/// <summary>
/// Verifica que GetByPartidaIdAsync carga el grafo BDT completo (Juegos→Etapas→Tesoros)
/// a través del repositorio real — cierra el round-trip T12/T13.
/// </summary>
public class SesionPartidaRepositoryBdtTests
{
    private static readonly DateTime T0 = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    private static DbContextOptions<OperacionesSesionDbContext> InMemoryOptions(string name) =>
        new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase(name).Options;

    /// <summary>Decoder inline: devuelve la imagen como texto UTF-8 (simula lectura de QR de texto plano).</summary>
    private sealed class InlineDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) => Encoding.UTF8.GetString(imagen);
    }

    /// <summary>
    /// TDD core: GetByPartidaIdAsync debe cargar las etapas BDT via Include.
    /// Sin la rama Include en el repo, Etapas vuelve vacío → falla.
    /// Con la rama Include → pasa.
    /// </summary>
    [Fact]
    public async Task GetByPartidaIdAsync_carga_etapas_bdt_via_repo()
    {
        var options = InMemoryOptions("bdt-etapas-" + Guid.NewGuid());
        var partidaId = Guid.NewGuid();

        // Construir sesión BDT con 2 etapas (mismo patrón que BdtSnapshotPersistenceTests)
        var etapas = new List<EtapaSnapshot>
        {
            new(Guid.NewGuid(), 1, "QR-1", 50, 60),
            new(Guid.NewGuid(), 2, "QR-2", 70, 90),
        };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Plaza", etapas);
        var snapshot = new ConfiguracionSnapshot("Copa BDT", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);

        // Guardar vía el repositorio real
        await using (var write = new OperacionesSesionDbContext(options))
        {
            new SesionPartidaRepository(write).Add(sesion);
            await new OperacionesSesionUnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        // Recargar en un contexto NUEVO a través del repositorio real
        await using (var read = new OperacionesSesionDbContext(options))
        {
            var loaded = await new SesionPartidaRepository(read)
                .GetByPartidaIdAsync(partidaId, CancellationToken.None);

            Assert.NotNull(loaded);
            var j = loaded!.Juegos.Single();
            Assert.Equal(2, j.Etapas.Count);
            Assert.Equal("QR-1", j.Etapas.OrderBy(e => e.Orden).First().CodigoQREsperado);
        }
    }

    /// <summary>
    /// Minor-1 T12: round-trip de TesoroQR vía GetByPartidaIdAsync.
    /// Recorre el ciclo completo: Publicar → Inscribir → Iniciar → ValidarTesoro (gana etapa 1)
    /// → guardar → recargar vía repo → assert Tesoros persisted.
    /// </summary>
    [Fact]
    public async Task GetByPartidaIdAsync_carga_tesoros_bdt_via_repo()
    {
        var options = InMemoryOptions("bdt-tesoros-" + Guid.NewGuid());
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();

        var etapas = new List<EtapaSnapshot>
        {
            new(Guid.NewGuid(), 1, "QR-1", 50, 60),
            new(Guid.NewGuid(), 2, "QR-2", 70, 90),
        };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Plaza", etapas);
        var snapshot = new ConfiguracionSnapshot("Copa BDT", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);

        // Ciclo: inscribir → iniciar (activa etapa 1) → validar QR correcto (gana etapa 1, agrega TesoroQR)
        sesion.Inscribir(participanteId, false, 0, T0);
        sesion.Iniciar(T0);
        sesion.ValidarTesoro(participanteId, Encoding.UTF8.GetBytes("QR-1"), T0.AddSeconds(5), new InlineDecoder());

        await using (var write = new OperacionesSesionDbContext(options))
        {
            new SesionPartidaRepository(write).Add(sesion);
            await new OperacionesSesionUnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using (var read = new OperacionesSesionDbContext(options))
        {
            var loaded = await new SesionPartidaRepository(read)
                .GetByPartidaIdAsync(partidaId, CancellationToken.None);

            Assert.NotNull(loaded);
            var j = loaded!.Juegos.Single();
            Assert.Equal(2, j.Etapas.Count);
            var etapa1 = j.Etapas.OrderBy(e => e.Orden).First();
            Assert.Single(etapa1.Tesoros); // el TesoroQR del participante que ganó la etapa 1
        }
    }
}
