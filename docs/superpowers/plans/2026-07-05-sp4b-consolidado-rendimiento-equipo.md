# SP-4b — Ranking consolidado + rendimiento de equipo — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Exponer por HTTP el ranking consolidado de una partida terminada (RF-45) y el rendimiento histórico de un equipo (RF-44) sobre las proyecciones SP-4a, y saldar la deuda de concurrencia `xmin` en `marcadores`.

**Architecture:** Cálculo al leer (enfoque A): un calculador puro (`CalculadorRankingConsolidado`) consume los marcadores de una partida y produce el consolidado; dos queries MediatR lo reusan (consolidado de partida y rendimiento por equipo). Sin tablas nuevas, sin eventos nuevos, sin cambios en el consumidor RabbitMQ salvo el reintento por concurrencia.

**Tech Stack:** .NET 8, MediatR, EF Core + Npgsql (InMemory sin connection string), xUnit, WebApplicationFactory.

**Spec:** `docs/superpowers/specs/2026-07-05-sp4b-consolidado-rendimiento-equipo-design.md` (aprobado 2026-07-05).

## Global Constraints

- Rama de trabajo: `feature/sp-4b-consolidado` (creada desde `feature/sp-4a-puntuaciones`; SP-4a NO se integra a develop).
- Solo se toca `services/puntuaciones` + `contracts/http/puntuaciones-api.md` + docs. **Prohibido** tocar Operaciones de Sesión o `contracts/events/`.
- Doctrina de carpetas CLAUDE.md: queries en `Application/Queries/`, handlers en `Application/Handlers/Queries/`, DTOs en `Application/DTOs/`, excepciones en `Application/Exceptions/`, controllers en `Api/Controllers/` heredando `ControllerBase` + MediatR, repos EF en `Infrastructure/Persistence/`, interfaces de repo en `Domain/Abstractions/Persistence/`.
- Regla RF-45: ganador de juego = más puntos, desempate menor tiempo, empate exacto → el juego no otorga victoria. Consolidado: `juegosGanados DESC, puntosTotales DESC, tiempoTotalMs ASC`; empate exacto triple comparte posición (1, 2, 2, 4).
- Consolidado solo con partida `Terminada` (otro estado → 409). Participación = tener ≥1 marcador.
- Best-effort ADR-0012: el worker nunca hace requeue; ack siempre.
- Commits: convención `feat|test|fix|docs(puntuaciones): ...` con `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Tests desde la raíz del repo: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`. Suite base SP-4a: 66/66 (54 unit + 8 integration + 4 contract) — debe seguir verde.

---

### Task 1: DTOs + `CalculadorRankingConsolidado` (puro, TDD)

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/RankingConsolidadoResponse.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/RendimientoEquipoResponse.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/CalculadorRankingConsolidado.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/CalculadorRankingConsolidadoTests.cs`

**Interfaces:**
- Consumes: `Marcador` (Domain, existente): `Marcador.Nuevo(juegoId, competidorId, partidaId, tipoCompetidor)` + `.Acreditar(puntos, tiempoMs)`; propiedades `JuegoId`, `CompetidorId`, `TipoCompetidor`, `PuntosAcumulados`, `TiempoAcumuladoMs`.
- Produces: `CalculadorRankingConsolidado.Calcular(IEnumerable<Marcador>) → IReadOnlyList<EntradaRankingConsolidadoDto>`; records `EntradaRankingConsolidadoDto(int Posicion, Guid CompetidorId, TipoCompetidor TipoCompetidor, int JuegosGanados, int PuntosTotales, long TiempoTotalMs)`, `RankingConsolidadoResponse(Guid PartidaId, DateTime GeneradoEn, IReadOnlyList<EntradaRankingConsolidadoDto> Entradas)`, `RendimientoPartidaDto(Guid PartidaId, DateTime? FechaFin, int Posicion, bool Gano)`, `RendimientoEquipoResponse(Guid EquipoId, IReadOnlyList<RendimientoPartidaDto> Partidas)`.

- [ ] **Step 1: Escribir los tests (fallan por compilación)**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/CalculadorRankingConsolidadoTests.cs`:

```csharp
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class CalculadorRankingConsolidadoTests
{
    private static readonly Guid Partida = Guid.NewGuid();

    private static Marcador Crear(Guid juegoId, Guid competidorId, int puntos, long tiempoMs,
        TipoCompetidor tipo = TipoCompetidor.Participante)
    {
        var marcador = Marcador.Nuevo(juegoId, competidorId, Partida, tipo);
        marcador.Acreditar(puntos, tiempoMs);
        return marcador;
    }

    [Fact]
    public void Sin_marcadores_devuelve_lista_vacia()
        => Assert.Empty(CalculadorRankingConsolidado.Calcular(Array.Empty<Marcador>()));

    [Fact]
    public void Ganador_de_cada_juego_acumula_juegos_ganados()
    {
        var juego1 = Guid.NewGuid();
        var juego2 = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego1, a, 20, 1000), Crear(juego1, b, 10, 500),
            Crear(juego2, a, 15, 2000), Crear(juego2, b, 10, 100)
        });

        Assert.Equal(a, entradas[0].CompetidorId);
        Assert.Equal(2, entradas[0].JuegosGanados);
        Assert.Equal(35, entradas[0].PuntosTotales);
        Assert.Equal(3000, entradas[0].TiempoTotalMs);
        Assert.Equal(1, entradas[0].Posicion);
        Assert.Equal(0, entradas[1].JuegosGanados);
        Assert.Equal(2, entradas[1].Posicion);
    }

    [Fact]
    public void Empate_de_puntos_en_un_juego_lo_gana_el_de_menor_tiempo()
    {
        var juego = Guid.NewGuid();
        var rapido = Guid.NewGuid();
        var lento = Guid.NewGuid();

        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego, lento, 10, 5000), Crear(juego, rapido, 10, 1000)
        });

        var deRapido = entradas.Single(e => e.CompetidorId == rapido);
        var deLento = entradas.Single(e => e.CompetidorId == lento);
        Assert.Equal(1, deRapido.JuegosGanados);
        Assert.Equal(0, deLento.JuegosGanados);
    }

    [Fact]
    public void Empate_exacto_en_un_juego_no_otorga_victoria_a_nadie()
    {
        var juego = Guid.NewGuid();

        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego, Guid.NewGuid(), 10, 1000), Crear(juego, Guid.NewGuid(), 10, 1000)
        });

        Assert.All(entradas, e => Assert.Equal(0, e.JuegosGanados));
    }

    [Fact]
    public void Juegos_ganados_manda_sobre_puntos_totales()
    {
        var juego1 = Guid.NewGuid();
        var juego2 = Guid.NewGuid();
        var juego3 = Guid.NewGuid();
        var ganador = Guid.NewGuid();
        var goleador = Guid.NewGuid();

        // ganador gana juego1 y juego2 con poco puntaje; goleador gana solo juego3 con muchos puntos.
        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego1, ganador, 10, 1000), Crear(juego1, goleador, 9, 500),
            Crear(juego2, ganador, 10, 1000), Crear(juego2, goleador, 9, 500),
            Crear(juego3, goleador, 50, 500)
        });

        Assert.Equal(ganador, entradas[0].CompetidorId);   // 2 juegos ganados, 20 puntos
        Assert.Equal(goleador, entradas[1].CompetidorId);  // 1 juego ganado, 68 puntos
    }

    [Fact]
    public void Mismos_juegos_ganados_desempata_por_puntos_y_luego_tiempo()
    {
        var juego1 = Guid.NewGuid();
        var juego2 = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        // a gana juego1, b gana juego2 (1 juego cada uno); a tiene más puntos totales que b.
        // c no gana nada, con puntos entre ambos: queda tercero por juegosGanados = 0.
        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego1, a, 30, 1000), Crear(juego1, c, 5, 500),
            Crear(juego2, b, 20, 1000), Crear(juego2, c, 6, 500)
        });

        Assert.Equal(new[] { a, b, c }, entradas.Select(e => e.CompetidorId).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, entradas.Select(e => e.Posicion).ToArray());
    }

    [Fact]
    public void Empate_total_comparte_posicion_y_la_siguiente_salta()
    {
        var juego = Guid.NewGuid();
        var primero = Guid.NewGuid();
        var empatadoA = Guid.NewGuid();
        var empatadoB = Guid.NewGuid();
        var cuarto = Guid.NewGuid();

        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego, primero, 50, 1000),
            Crear(juego, empatadoA, 20, 2000), Crear(juego, empatadoB, 20, 2000),
            Crear(juego, cuarto, 5, 3000)
        });

        Assert.Equal(new[] { 1, 2, 2, 4 }, entradas.Select(e => e.Posicion).ToArray());
    }

    [Fact]
    public void Conserva_tipo_competidor_equipo()
    {
        var juego = Guid.NewGuid();
        var equipo = Guid.NewGuid();

        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego, equipo, 10, 1000, TipoCompetidor.Equipo)
        });

        Assert.Equal(TipoCompetidor.Equipo, entradas[0].TipoCompetidor);
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter CalculadorRankingConsolidadoTests`
Expected: FAIL de compilación (`CalculadorRankingConsolidado` no existe).

- [ ] **Step 3: Implementar DTOs y calculador**

`services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/RankingConsolidadoResponse.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record EntradaRankingConsolidadoDto(
    int Posicion, Guid CompetidorId, TipoCompetidor TipoCompetidor,
    int JuegosGanados, int PuntosTotales, long TiempoTotalMs);

public sealed record RankingConsolidadoResponse(
    Guid PartidaId, DateTime GeneradoEn, IReadOnlyList<EntradaRankingConsolidadoDto> Entradas);
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/RendimientoEquipoResponse.cs`:

```csharp
namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record RendimientoPartidaDto(Guid PartidaId, DateTime? FechaFin, int Posicion, bool Gano);

public sealed record RendimientoEquipoResponse(Guid EquipoId, IReadOnlyList<RendimientoPartidaDto> Partidas);
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/CalculadorRankingConsolidado.cs`:

```csharp
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

// Consolidado de partida (RF-45): juegos ganados DESC, puntos totales DESC, tiempo total ASC.
// Ganador por juego: más puntos, desempate menor tiempo; empate exacto → el juego no otorga victoria.
// Solo necesita los marcadores de la partida: cada grupo por JuegoId define un juego a efectos
// del cálculo (tolerante a juegos no proyectados por pérdida de JuegoActivado, best-effort ADR-0012).
public static class CalculadorRankingConsolidado
{
    public static IReadOnlyList<EntradaRankingConsolidadoDto> Calcular(IEnumerable<Marcador> marcadoresDePartida)
    {
        var marcadores = marcadoresDePartida.ToList();
        if (marcadores.Count == 0)
        {
            return Array.Empty<EntradaRankingConsolidadoDto>();
        }

        var ganadoresPorJuego = new List<Guid>();
        foreach (var juego in marcadores.GroupBy(m => m.JuegoId))
        {
            var ordenados = juego
                .OrderByDescending(m => m.PuntosAcumulados)
                .ThenBy(m => m.TiempoAcumuladoMs)
                .ToList();
            var empateExacto = ordenados.Count > 1
                && ordenados[0].PuntosAcumulados == ordenados[1].PuntosAcumulados
                && ordenados[0].TiempoAcumuladoMs == ordenados[1].TiempoAcumuladoMs;
            if (!empateExacto)
            {
                ganadoresPorJuego.Add(ordenados[0].CompetidorId);
            }
        }

        var agregados = marcadores
            .GroupBy(m => m.CompetidorId)
            .Select(g => new
            {
                CompetidorId = g.Key,
                g.First().TipoCompetidor,
                JuegosGanados = ganadoresPorJuego.Count(id => id == g.Key),
                PuntosTotales = g.Sum(m => m.PuntosAcumulados),
                TiempoTotalMs = g.Sum(m => m.TiempoAcumuladoMs)
            })
            .OrderByDescending(a => a.JuegosGanados)
            .ThenByDescending(a => a.PuntosTotales)
            .ThenBy(a => a.TiempoTotalMs)
            .ToList();

        var entradas = new List<EntradaRankingConsolidadoDto>(agregados.Count);
        for (var i = 0; i < agregados.Count; i++)
        {
            var actual = agregados[i];
            var posicion = i + 1;
            if (i > 0)
            {
                var previo = agregados[i - 1];
                var empateExacto = previo.JuegosGanados == actual.JuegosGanados
                    && previo.PuntosTotales == actual.PuntosTotales
                    && previo.TiempoTotalMs == actual.TiempoTotalMs;
                if (empateExacto)
                {
                    posicion = entradas[i - 1].Posicion;
                }
            }

            entradas.Add(new EntradaRankingConsolidadoDto(
                posicion, actual.CompetidorId, actual.TipoCompetidor,
                actual.JuegosGanados, actual.PuntosTotales, actual.TiempoTotalMs));
        }

        return entradas;
    }
}
```

- [ ] **Step 4: Correr los tests y verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter CalculadorRankingConsolidadoTests`
Expected: PASS (8 tests).

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/RankingConsolidadoResponse.cs services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/RendimientoEquipoResponse.cs services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/CalculadorRankingConsolidado.cs services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/CalculadorRankingConsolidadoTests.cs
git commit -m "feat(puntuaciones): calculador de ranking consolidado RF-45 (juegos ganados, puntos, tiempo)"
```

---

### Task 2: Métodos nuevos de repositorio + fake

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/IProyeccionesRepository.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/ProyeccionesRepository.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/Fakes/FakeProyeccionesRepository.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/ProyeccionesRepositoryTests.cs` (extender)

**Interfaces:**
- Consumes: `PuntuacionesDbContext` (`_db.Marcadores`, `_db.Partidas`), enums `EstadoPartidaProyectada`, `Modalidad`, `TipoCompetidor`.
- Produces (usado por Task 3): `Task<IReadOnlyList<Marcador>> GetMarcadoresDePartidaAsync(Guid partidaId, CancellationToken ct)` y `Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeEquipoAsync(Guid equipoId, CancellationToken ct)` en `IProyeccionesRepository`.

- [ ] **Step 1: Escribir el test de integración (falla por compilación)**

Añadir a `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/ProyeccionesRepositoryTests.cs` (dentro de la clase existente):

```csharp
    [Fact]
    public async Task GetPartidasTerminadasConMarcadorDeEquipo_filtra_por_estado_modalidad_y_equipo()
    {
        var equipoId = Guid.NewGuid();
        var terminadaReciente = Guid.NewGuid();
        var terminadaAntigua = Guid.NewGuid();
        var iniciada = Guid.NewGuid();
        var individual = Guid.NewGuid();
        var sinMarcador = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProyeccionesRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IPuntuacionesUnitOfWork>();

            void SembrarPartida(Guid partidaId, Modalidad modalidad, bool terminada, DateTime? fechaFin)
            {
                var partida = PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), modalidad);
                if (terminada) { partida.MarcarTerminada(fechaFin!.Value); }
                repo.AddPartida(partida);
            }

            void SembrarMarcador(Guid partidaId, TipoCompetidor tipo)
            {
                var marcador = Marcador.Nuevo(Guid.NewGuid(), equipoId, partidaId, tipo);
                marcador.Acreditar(10, 1000);
                repo.AddMarcador(marcador);
            }

            SembrarPartida(terminadaReciente, Modalidad.Equipo, terminada: true, new DateTime(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc));
            SembrarMarcador(terminadaReciente, TipoCompetidor.Equipo);
            SembrarPartida(terminadaAntigua, Modalidad.Equipo, terminada: true, new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
            SembrarMarcador(terminadaAntigua, TipoCompetidor.Equipo);
            SembrarPartida(iniciada, Modalidad.Equipo, terminada: false, null);
            SembrarMarcador(iniciada, TipoCompetidor.Equipo);
            SembrarPartida(individual, Modalidad.Individual, terminada: true, new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));
            SembrarMarcador(individual, TipoCompetidor.Participante);
            SembrarPartida(sinMarcador, Modalidad.Equipo, terminada: true, new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc));

            await uow.SaveChangesAsync(CancellationToken.None);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProyeccionesRepository>();

            var partidas = await repo.GetPartidasTerminadasConMarcadorDeEquipoAsync(equipoId, CancellationToken.None);
            var marcadores = await repo.GetMarcadoresDePartidaAsync(terminadaReciente, CancellationToken.None);

            Assert.Equal(new[] { terminadaReciente, terminadaAntigua }, partidas.Select(p => p.PartidaId).ToArray());
            Assert.Single(marcadores);
        }
    }
```

- [ ] **Step 2: Correr el test para verificar que falla**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj" --filter GetPartidasTerminadasConMarcadorDeEquipo_filtra_por_estado_modalidad_y_equipo`
Expected: FAIL de compilación (métodos no existen en la interfaz).

- [ ] **Step 3: Implementar interfaz, EF y fake**

En `IProyeccionesRepository.cs`, añadir al final de la interfaz:

```csharp
    Task<IReadOnlyList<Marcador>> GetMarcadoresDePartidaAsync(Guid partidaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeEquipoAsync(Guid equipoId, CancellationToken cancellationToken);
```

En `ProyeccionesRepository.cs`, añadir (requiere `using Umbral.Puntuaciones.Domain.Enums;`):

```csharp
    public async Task<IReadOnlyList<Marcador>> GetMarcadoresDePartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => await _db.Marcadores.AsNoTracking()
            .Where(m => m.PartidaId == partidaId)
            .ToListAsync(cancellationToken);

    // Participación = tener ≥1 marcador (decisión SP-4b): partidas por equipos terminadas
    // donde el equipo anotó al menos una vez, más reciente primero.
    public async Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeEquipoAsync(Guid equipoId, CancellationToken cancellationToken)
        => await _db.Partidas.AsNoTracking()
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada
                && p.Modalidad == Modalidad.Equipo
                && _db.Marcadores.Any(m => m.PartidaId == p.PartidaId
                    && m.CompetidorId == equipoId
                    && m.TipoCompetidor == TipoCompetidor.Equipo))
            .OrderByDescending(p => p.FechaFin)
            .ToListAsync(cancellationToken);
```

En `FakeProyeccionesRepository.cs`, añadir (requiere `using Umbral.Puntuaciones.Domain.Enums;`):

```csharp
    public Task<IReadOnlyList<Marcador>> GetMarcadoresDePartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Marcador>>(Marcadores.Where(m => m.PartidaId == partidaId).ToList());

    public Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeEquipoAsync(Guid equipoId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PartidaProyectada>>(Partidas
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada
                && p.Modalidad == Modalidad.Equipo
                && Marcadores.Any(m => m.PartidaId == p.PartidaId
                    && m.CompetidorId == equipoId
                    && m.TipoCompetidor == TipoCompetidor.Equipo))
            .OrderByDescending(p => p.FechaFin)
            .ToList());
```

- [ ] **Step 4: Correr el test y verificar que pasa**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj" --filter GetPartidasTerminadasConMarcadorDeEquipo_filtra_por_estado_modalidad_y_equipo`
Expected: PASS. Correr también la suite unit completa para confirmar que el fake compila:
`dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj"` → PASS.

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones/src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/IProyeccionesRepository.cs services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/ProyeccionesRepository.cs services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/Fakes/FakeProyeccionesRepository.cs services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/ProyeccionesRepositoryTests.cs
git commit -m "feat(puntuaciones): queries de repositorio para consolidado y rendimiento de equipo"
```

---

### Task 3: Excepciones + queries + handlers (TDD unit)

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Exceptions/PartidaNoEncontradaException.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Exceptions/PartidaNoTerminadaException.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Queries/ObtenerRankingConsolidadoQuery.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Queries/ObtenerRendimientoEquipoQuery.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerRankingConsolidadoQueryHandler.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerRendimientoEquipoQueryHandler.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerRankingConsolidadoQueryHandlerTests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerRendimientoEquipoQueryHandlerTests.cs`

**Interfaces:**
- Consumes: Task 1 (`CalculadorRankingConsolidado`, DTOs), Task 2 (métodos de repo), `PartidaProyectada.DesdePublicacion/MarcarIniciada/MarcarCancelada/MarcarTerminada`, `FakeProyeccionesRepository`.
- Produces (usado por Task 4): `ObtenerRankingConsolidadoQuery(Guid PartidaId) : IRequest<RankingConsolidadoResponse>`; `ObtenerRendimientoEquipoQuery(Guid EquipoId) : IRequest<RendimientoEquipoResponse>`; excepciones `PartidaNoEncontradaException(Guid)`, `PartidaNoTerminadaException(Guid, EstadoPartidaProyectada)`.

- [ ] **Step 1: Escribir los tests (fallan por compilación)**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerRankingConsolidadoQueryHandlerTests.cs`:

```csharp
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ObtenerRankingConsolidadoQueryHandlerTests
{
    private static readonly DateTime Ahora = DateTime.UtcNow;

    [Fact]
    public async Task Partida_desconocida_lanza_no_encontrada()
    {
        var handler = new ObtenerRankingConsolidadoQueryHandler(new FakeProyeccionesRepository());

        await Assert.ThrowsAsync<PartidaNoEncontradaException>(
            () => handler.Handle(new ObtenerRankingConsolidadoQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Theory]
    [InlineData("Lobby")]
    [InlineData("Iniciada")]
    [InlineData("Cancelada")]
    public async Task Partida_no_terminada_lanza_conflicto(string estado)
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeProyeccionesRepository();
        var partida = PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual);
        if (estado == "Iniciada") { partida.MarcarIniciada(Ahora); }
        if (estado == "Cancelada") { partida.MarcarCancelada(Ahora); }
        repo.AddPartida(partida);
        var handler = new ObtenerRankingConsolidadoQueryHandler(repo);

        await Assert.ThrowsAsync<PartidaNoTerminadaException>(
            () => handler.Handle(new ObtenerRankingConsolidadoQuery(partidaId), CancellationToken.None));
    }

    [Fact]
    public async Task Partida_terminada_devuelve_consolidado_ordenado()
    {
        var partidaId = Guid.NewGuid();
        var juego1 = Guid.NewGuid();
        var juego2 = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var repo = new FakeProyeccionesRepository();
        var partida = PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual);
        partida.MarcarTerminada(Ahora);
        repo.AddPartida(partida);

        void Sembrar(Guid juegoId, Guid competidorId, int puntos, long tiempo)
        {
            var m = Marcador.Nuevo(juegoId, competidorId, partidaId, TipoCompetidor.Participante);
            m.Acreditar(puntos, tiempo);
            repo.AddMarcador(m);
        }
        Sembrar(juego1, a, 20, 1000);
        Sembrar(juego1, b, 10, 500);
        Sembrar(juego2, b, 30, 2000);

        var handler = new ObtenerRankingConsolidadoQueryHandler(repo);
        var response = await handler.Handle(new ObtenerRankingConsolidadoQuery(partidaId), CancellationToken.None);

        Assert.Equal(partidaId, response.PartidaId);
        Assert.NotEqual(default, response.GeneradoEn);
        Assert.Equal(2, response.Entradas.Count);
        // a y b ganan 1 juego cada uno; b tiene más puntos totales (40 > 20) → b primero.
        Assert.Equal(b, response.Entradas[0].CompetidorId);
        Assert.Equal(1, response.Entradas[0].JuegosGanados);
        Assert.Equal(40, response.Entradas[0].PuntosTotales);
    }

    [Fact]
    public async Task Partida_terminada_sin_marcadores_devuelve_entradas_vacias()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeProyeccionesRepository();
        var partida = PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual);
        partida.MarcarTerminada(Ahora);
        repo.AddPartida(partida);
        var handler = new ObtenerRankingConsolidadoQueryHandler(repo);

        var response = await handler.Handle(new ObtenerRankingConsolidadoQuery(partidaId), CancellationToken.None);

        Assert.Empty(response.Entradas);
    }
}
```

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerRendimientoEquipoQueryHandlerTests.cs`:

```csharp
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
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "ObtenerRankingConsolidadoQueryHandlerTests|ObtenerRendimientoEquipoQueryHandlerTests"`
Expected: FAIL de compilación (queries/handlers/excepciones no existen).

- [ ] **Step 3: Implementar excepciones, queries y handlers**

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Exceptions/PartidaNoEncontradaException.cs`:

```csharp
namespace Umbral.Puntuaciones.Application.Exceptions;

public sealed class PartidaNoEncontradaException : Exception
{
    public PartidaNoEncontradaException(Guid partidaId)
        : base($"No se encontró la partida {partidaId} en las proyecciones de Puntuaciones.")
    {
    }
}
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Exceptions/PartidaNoTerminadaException.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Exceptions;

// El consolidado se calcula al finalizar (RF-45/HU-50): en cualquier otro estado es 409.
public sealed class PartidaNoTerminadaException : Exception
{
    public PartidaNoTerminadaException(Guid partidaId, EstadoPartidaProyectada estado)
        : base($"La partida {partidaId} no está terminada (estado {estado}); el ranking consolidado se calcula al finalizar.")
    {
    }
}
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Queries/ObtenerRankingConsolidadoQuery.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerRankingConsolidadoQuery(Guid PartidaId) : IRequest<RankingConsolidadoResponse>;
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Queries/ObtenerRendimientoEquipoQuery.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerRendimientoEquipoQuery(Guid EquipoId) : IRequest<RendimientoEquipoResponse>;
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerRankingConsolidadoQueryHandler.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

public sealed class ObtenerRankingConsolidadoQueryHandler
    : IRequestHandler<ObtenerRankingConsolidadoQuery, RankingConsolidadoResponse>
{
    private readonly IProyeccionesRepository _repo;

    public ObtenerRankingConsolidadoQueryHandler(IProyeccionesRepository repo) => _repo = repo;

    public async Task<RankingConsolidadoResponse> Handle(ObtenerRankingConsolidadoQuery request, CancellationToken cancellationToken)
    {
        var partida = await _repo.GetPartidaAsync(request.PartidaId, cancellationToken);
        if (partida is null)
        {
            throw new PartidaNoEncontradaException(request.PartidaId);
        }
        if (partida.Estado != EstadoPartidaProyectada.Terminada)
        {
            throw new PartidaNoTerminadaException(request.PartidaId, partida.Estado);
        }

        var marcadores = await _repo.GetMarcadoresDePartidaAsync(request.PartidaId, cancellationToken);
        return new RankingConsolidadoResponse(
            request.PartidaId, DateTime.UtcNow, CalculadorRankingConsolidado.Calcular(marcadores));
    }
}
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerRendimientoEquipoQueryHandler.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

// RF-44: posición en el consolidado y si la ganó, por partida por equipos terminada donde el
// equipo anotó. "Sin duplicar el cálculo de puntajes": reusa CalculadorRankingConsolidado.
public sealed class ObtenerRendimientoEquipoQueryHandler
    : IRequestHandler<ObtenerRendimientoEquipoQuery, RendimientoEquipoResponse>
{
    private readonly IProyeccionesRepository _repo;

    public ObtenerRendimientoEquipoQueryHandler(IProyeccionesRepository repo) => _repo = repo;

    public async Task<RendimientoEquipoResponse> Handle(ObtenerRendimientoEquipoQuery request, CancellationToken cancellationToken)
    {
        var partidas = await _repo.GetPartidasTerminadasConMarcadorDeEquipoAsync(request.EquipoId, cancellationToken);
        var rendimiento = new List<RendimientoPartidaDto>(partidas.Count);
        foreach (var partida in partidas)
        {
            var marcadores = await _repo.GetMarcadoresDePartidaAsync(partida.PartidaId, cancellationToken);
            var entradas = CalculadorRankingConsolidado.Calcular(marcadores);
            // El repo garantiza ≥1 marcador del equipo en cada partida devuelta.
            var propia = entradas.First(e => e.CompetidorId == request.EquipoId);
            rendimiento.Add(new RendimientoPartidaDto(partida.PartidaId, partida.FechaFin, propia.Posicion, propia.Posicion == 1));
        }

        return new RendimientoEquipoResponse(request.EquipoId, rendimiento);
    }
}
```

- [ ] **Step 4: Correr los tests y verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "ObtenerRankingConsolidadoQueryHandlerTests|ObtenerRendimientoEquipoQueryHandlerTests"`
Expected: PASS (8 tests: consolidado 6 —1 desconocida + Theory×3 + 1 ordenado + 1 vacías— y rendimiento 2).

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones/src/Umbral.Puntuaciones.Application services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application
git commit -m "feat(puntuaciones): queries de ranking consolidado y rendimiento de equipo (404/409, RF-44/RF-45)"
```

---

### Task 4: Controllers + mapeo del middleware (TDD unit)

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/RankingsController.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/EquiposController.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Middleware/ExceptionHandlingMiddleware.cs:40-45`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/RankingsControllerTests.cs` (extender)
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/EquiposControllerTests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/ExceptionHandlingMiddlewareTests.cs` (extender)

**Interfaces:**
- Consumes: Task 3 (queries y excepciones), `FakeSender` existente.
- Produces: `GET /puntuaciones/partidas/{partidaId}/ranking-consolidado` y `GET /puntuaciones/equipos/{equipoId}/rendimiento` (usados por Tasks 5-6).

- [ ] **Step 1: Escribir los tests (fallan por compilación)**

Añadir a `RankingsControllerTests.cs`:

```csharp
    [Fact]
    public async Task ObtenerRankingConsolidado_despacha_query_y_devuelve_ok()
    {
        var partidaId = Guid.NewGuid();
        var respuesta = new RankingConsolidadoResponse(partidaId, DateTime.UtcNow, Array.Empty<EntradaRankingConsolidadoDto>());
        var sender = new FakeSender(respuesta);
        var controller = new RankingsController(sender);

        var result = await controller.ObtenerRankingConsolidado(partidaId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(respuesta, ok.Value);
        var query = Assert.IsType<ObtenerRankingConsolidadoQuery>(sender.LastRequest);
        Assert.Equal(partidaId, query.PartidaId);
    }
```

Crear `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/EquiposControllerTests.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Api.Controllers;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.UnitTests.Api;

public class EquiposControllerTests
{
    [Fact]
    public async Task ObtenerRendimiento_despacha_query_y_devuelve_ok()
    {
        var equipoId = Guid.NewGuid();
        var respuesta = new RendimientoEquipoResponse(equipoId, Array.Empty<RendimientoPartidaDto>());
        var sender = new FakeSender(respuesta);
        var controller = new EquiposController(sender);

        var result = await controller.ObtenerRendimiento(equipoId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(respuesta, ok.Value);
        var query = Assert.IsType<ObtenerRendimientoEquipoQuery>(sender.LastRequest);
        Assert.Equal(equipoId, query.EquipoId);
    }
}
```

Añadir a `ExceptionHandlingMiddlewareTests.cs`:

```csharp
    [Fact]
    public async Task PartidaNoEncontrada_mapea_404()
        => Assert.Equal(StatusCodes.Status404NotFound,
            await StatusDe(new PartidaNoEncontradaException(Guid.NewGuid())));

    [Fact]
    public async Task PartidaNoTerminada_mapea_409()
        => Assert.Equal(StatusCodes.Status409Conflict,
            await StatusDe(new PartidaNoTerminadaException(Guid.NewGuid(), Umbral.Puntuaciones.Domain.Enums.EstadoPartidaProyectada.Iniciada)));
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "RankingsControllerTests|EquiposControllerTests|ExceptionHandlingMiddlewareTests"`
Expected: FAIL de compilación (acción/controller no existen).

- [ ] **Step 3: Implementar controllers y mapeo**

Añadir a `RankingsController.cs` (después de `ObtenerMarcador`):

```csharp
    [HttpGet("partidas/{partidaId:guid}/ranking-consolidado")]
    public async Task<IActionResult> ObtenerRankingConsolidado(Guid partidaId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ObtenerRankingConsolidadoQuery(partidaId), cancellationToken);
        return Ok(response);
    }
```

Crear `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/EquiposController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.Api.Controllers;

[ApiController]
[Route("puntuaciones")]
public sealed class EquiposController : ControllerBase
{
    private readonly ISender _mediator;

    public EquiposController(ISender mediator) => _mediator = mediator;

    [HttpGet("equipos/{equipoId:guid}/rendimiento")]
    public async Task<IActionResult> ObtenerRendimiento(Guid equipoId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ObtenerRendimientoEquipoQuery(equipoId), cancellationToken);
        return Ok(response);
    }
}
```

En `ExceptionHandlingMiddleware.cs`, reemplazar el switch `MapStatus`:

```csharp
    private static HttpStatusCode MapStatus(Exception ex) => ex switch
    {
        JuegoNoEncontradoException or MarcadorNoEncontradoException or PartidaNoEncontradaException => HttpStatusCode.NotFound,
        PartidaNoTerminadaException => HttpStatusCode.Conflict,
        ValidationException or ArgumentException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
    };
```

- [ ] **Step 4: Correr los tests y verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj"`
Expected: PASS (suite unit completa, sin regresión).

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones/src/Umbral.Puntuaciones.Api services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api
git commit -m "feat(puntuaciones): endpoints de consolidado y rendimiento con mapeo 404/409"
```

---

### Task 5: Integration E2E (proyección → consolidado/rendimiento)

**Files:**
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/ConsolidadoYRendimientoE2ETests.cs`

**Interfaces:**
- Consumes: comandos de proyección SP-4a existentes (`ProyectarPartidaPublicadaCommand(EventId, OccurredAt, PartidaId, SesionPartidaId, Modalidad)`, `ProyectarPartidaFinalizadaCommand(EventId, OccurredAt, PartidaId, SesionPartidaId, FechaFin)`, `ProyectarJuegoActivadoCommand(EventId, OccurredAt, PartidaId, SesionPartidaId, JuegoId, Orden, TipoJuego)`, `ProyectarPuntajeTriviaCommand(EventId, OccurredAt, PartidaId, SesionPartidaId, JuegoId, PreguntaId, ParticipanteId, Puntaje, TiempoRespuestaMs, EquipoId?)`, `ProyectarEtapaBdtGanadaCommand(EventId, OccurredAt, PartidaId, SesionPartidaId, JuegoId, EtapaId, ParticipanteId, Puntaje, TiempoResolucionMs, EquipoId?)`) + endpoints de Task 4.
- Produces: cobertura E2E; ningún código de producción nuevo.

- [ ] **Step 1: Escribir los tests E2E**

Crear `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/ConsolidadoYRendimientoE2ETests.cs` (patrón `ProyeccionYRankingE2ETests`):

```csharp
using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class ConsolidadoYRendimientoE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly DateTime Ahora = DateTime.UtcNow;

    public ConsolidadoYRendimientoE2ETests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task Proyectar(IBaseRequest comando)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando);
    }

    private async Task SembrarPartidaEquipoTerminada(Guid partidaId, Guid equipoGanador, Guid equipoRival, int puntosGanador, int puntosRival, DateTime fechaFin)
    {
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Equipo));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.BusquedaDelTesoro));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), Guid.NewGuid(), puntosGanador, 1000, equipoGanador));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), Guid.NewGuid(), puntosRival, 2000, equipoRival));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, fechaFin));
    }

    [Fact]
    public async Task Consolidado_de_partida_terminada_ordena_por_juegos_ganados()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juego1 = Guid.NewGuid();
        var juego2 = Guid.NewGuid();
        var juego3 = Guid.NewGuid();
        var constante = Guid.NewGuid();  // gana juego1 y juego2 con 10+10 puntos
        var goleador = Guid.NewGuid();   // gana solo juego3 con 50 puntos

        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego1, 1, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego1, Guid.NewGuid(), constante, 10, 1000, null));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego1, Guid.NewGuid(), goleador, 9, 500, null));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego2, 2, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego2, Guid.NewGuid(), constante, 10, 1000, null));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego3, 3, TipoJuego.BusquedaDelTesoro));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego3, Guid.NewGuid(), goleador, 50, 800, null));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entradas = json.RootElement.GetProperty("entradas");
        Assert.Equal(2, entradas.GetArrayLength());
        Assert.Equal(constante, entradas[0].GetProperty("competidorId").GetGuid());
        Assert.Equal(2, entradas[0].GetProperty("juegosGanados").GetInt32());
        Assert.Equal(20, entradas[0].GetProperty("puntosTotales").GetInt32());
        Assert.Equal(goleador, entradas[1].GetProperty("competidorId").GetGuid());
        Assert.Equal(1, entradas[1].GetProperty("juegosGanados").GetInt32());
        Assert.Equal(59, entradas[1].GetProperty("puntosTotales").GetInt32());
    }

    [Fact]
    public async Task Consolidado_de_partida_no_terminada_devuelve_409()
    {
        var partidaId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), Modalidad.Individual));

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(json.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Consolidado_de_partida_desconocida_devuelve_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/ranking-consolidado");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Scoring_tardio_tras_finalizar_se_refleja_al_releer()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidor = Guid.NewGuid();

        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), competidor, 10, 1000, null));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));

        var client = _factory.CreateClient();
        var antes = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var jsonAntes = JsonDocument.Parse(await antes.Content.ReadAsStringAsync());
        var puntosAntes = jsonAntes.RootElement.GetProperty("entradas")[0].GetProperty("puntosTotales").GetInt32();

        // Evento de scoring que llega DESPUÉS de PartidaFinalizada (best-effort, sin orden garantizado).
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), competidor, 5, 500, null));
        var despues = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var jsonDespues = JsonDocument.Parse(await despues.Content.ReadAsStringAsync());

        Assert.Equal(10, puntosAntes);
        Assert.Equal(15, jsonDespues.RootElement.GetProperty("entradas")[0].GetProperty("puntosTotales").GetInt32());
    }

    [Fact]
    public async Task Rendimiento_de_equipo_lista_partidas_con_posicion_y_gano()
    {
        var equipo = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var ganada = Guid.NewGuid();
        var perdida = Guid.NewGuid();

        await SembrarPartidaEquipoTerminada(ganada, equipo, rival, 20, 10, new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        await SembrarPartidaEquipoTerminada(perdida, rival, equipo, 30, 5, new DateTime(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc));

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/puntuaciones/equipos/{equipo}/rendimiento");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(equipo, json.RootElement.GetProperty("equipoId").GetGuid());
        var partidas = json.RootElement.GetProperty("partidas");
        Assert.Equal(2, partidas.GetArrayLength());
        Assert.Equal(perdida, partidas[0].GetProperty("partidaId").GetGuid());
        Assert.Equal(2, partidas[0].GetProperty("posicion").GetInt32());
        Assert.False(partidas[0].GetProperty("gano").GetBoolean());
        Assert.Equal(ganada, partidas[1].GetProperty("partidaId").GetGuid());
        Assert.Equal(1, partidas[1].GetProperty("posicion").GetInt32());
        Assert.True(partidas[1].GetProperty("gano").GetBoolean());
    }

    [Fact]
    public async Task Rendimiento_de_equipo_sin_participaciones_devuelve_lista_vacia()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/puntuaciones/equipos/{Guid.NewGuid()}/rendimiento");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, json.RootElement.GetProperty("partidas").GetArrayLength());
    }
}
```

Nota: en `SembrarPartidaEquipoTerminada`, ambas etapas BDT las "gana" un equipo distinto en el mismo juego — el consolidado del juego lo gana el de más puntos (`puntosGanador > puntosRival`), que es lo que el test necesita.

- [ ] **Step 2: Correr la suite de integración y verificar verde**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj"`
Expected: PASS (8 SP-4a + 1 Task 2 + 6 nuevos = 15; el round-trip RabbitMQ retorna vacío sin broker).

- [ ] **Step 3: Commit**

```bash
git add services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/ConsolidadoYRendimientoE2ETests.cs
git commit -m "test(puntuaciones): e2e de consolidado, scoring tardio y rendimiento de equipo"
```

---

### Task 6: Contract tests

**Files:**
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/ConsolidadoContractTests.cs`

**Interfaces:**
- Consumes: endpoints de Task 4, comandos de proyección SP-4a (mismas firmas que Task 5).
- Produces: fijación del shape documentado en `contracts/http/puntuaciones-api.md` (Task 8).

- [ ] **Step 1: Escribir los contract tests**

Crear `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/ConsolidadoContractTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.ContractTests;

public class ConsolidadoContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConsolidadoContractTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task Proyectar(IBaseRequest comando)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando);
    }

    [Fact]
    public async Task Consolidado_body_matches_contract()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidorId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, Modalidad.Individual));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, Guid.NewGuid(), competidorId, 10, 1500, null));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, DateTime.UtcNow));

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = json.RootElement;
        Assert.Equal(partidaId, root.GetProperty("partidaId").GetGuid());
        Assert.True(root.TryGetProperty("generadoEn", out _));
        var entrada = root.GetProperty("entradas")[0];
        Assert.Equal(1, entrada.GetProperty("posicion").GetInt32());
        Assert.Equal(competidorId, entrada.GetProperty("competidorId").GetGuid());
        Assert.Equal("Participante", entrada.GetProperty("tipoCompetidor").GetString());
        Assert.Equal(1, entrada.GetProperty("juegosGanados").GetInt32());
        Assert.Equal(10, entrada.GetProperty("puntosTotales").GetInt32());
        Assert.Equal(1500, entrada.GetProperty("tiempoTotalMs").GetInt64());
    }

    [Fact]
    public async Task Consolidado_409_devuelve_message_json()
    {
        var partidaId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, Guid.NewGuid(), Modalidad.Individual));

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(json.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Rendimiento_body_matches_contract()
    {
        var equipoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, Modalidad.Equipo));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, 1, TipoJuego.BusquedaDelTesoro));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, Guid.NewGuid(), Guid.NewGuid(), 25, 4000, equipoId));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, DateTime.UtcNow));

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/puntuaciones/equipos/{equipoId}/rendimiento");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = json.RootElement;
        Assert.Equal(equipoId, root.GetProperty("equipoId").GetGuid());
        var partida = root.GetProperty("partidas")[0];
        Assert.Equal(partidaId, partida.GetProperty("partidaId").GetGuid());
        Assert.True(partida.TryGetProperty("fechaFin", out _));
        Assert.Equal(1, partida.GetProperty("posicion").GetInt32());
        Assert.True(partida.GetProperty("gano").GetBoolean());
    }
}
```

- [ ] **Step 2: Correr los contract tests y verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/Umbral.Puntuaciones.ContractTests.csproj"`
Expected: PASS (4 SP-4a + 3 nuevos = 7).

- [ ] **Step 3: Commit**

```bash
git add services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/ConsolidadoContractTests.cs
git commit -m "test(puntuaciones): contract tests de consolidado y rendimiento de equipo"
```

---

### Task 7: Deuda SP-4a — `xmin` en `marcadores` + reintento por concurrencia en el worker

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/PuntuacionesDbContext.cs:42-54`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/Migrations/<timestamp>_SP4bXminMarcadores.cs` (generada)
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/OperacionesSesionEventsConsumer.cs:95-114`

**Interfaces:**
- Consumes: `DbUpdateConcurrencyException` (Microsoft.EntityFrameworkCore), `ProyeccionEventMapper.Map` existente.
- Produces: upserts de `marcadores` protegidos por token de concurrencia; sin cambios de firma públicos.

Nota de testing: las ramas del worker no tienen unit tests (deuda anotada en SP-4a, se mantiene — cubiertas por el round-trip opt-in). Este task se verifica por build + suite completa verde + migración generada sin operaciones de esquema.

- [ ] **Step 1: Configurar `xmin` como token de concurrencia**

En `PuntuacionesDbContext.OnModelCreating`, después del bloque `modelBuilder.Entity<EventoProcesado>(...)`, añadir:

```csharp
        // SP-4b: token de concurrencia optimista sobre la columna de sistema xmin de PostgreSQL.
        // Solo aplica con Npgsql; el proveedor InMemory (dev/tests) no la tiene.
        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<Marcador>().UseXminAsConcurrencyToken();
        }
```

- [ ] **Step 2: Generar la migración y verificar que no toca el esquema**

Run (desde la raíz del repo):

```bash
dotnet ef migrations add SP4bXminMarcadores --project services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure --startup-project services/puntuaciones/src/Umbral.Puntuaciones.Api
```

Inspeccionar la migración generada: `xmin` es columna de sistema de PostgreSQL, así que `Up()`/`Down()` deben quedar **sin operaciones de esquema** (solo cambia el model snapshot). Si el scaffold incluyera un `AddColumn` para `xmin`, eliminar esa operación manualmente dejando `Up()`/`Down()` vacíos.

- [ ] **Step 3: Añadir el reintento por concurrencia al worker**

En `OperacionesSesionEventsConsumer.cs`, añadir `using Microsoft.EntityFrameworkCore;` y reemplazar el bloque try/catch/finally de `ProcesarMensajeAsync` (líneas 95-113) por:

```csharp
        try
        {
            await DespacharAsync(command, ct);
            _logger.LogInformation(
                "Evento proyectado {EventType} {EventId} (rk {RoutingKey}).",
                envelope!.EventType, envelope.EventId, ea.RoutingKey);
        }
        catch (DbUpdateConcurrencyException)
        {
            // xmin (SP-4b): otro upsert pisó la fila entre lectura y SaveChanges. Un único
            // reintento con scope fresco relee el estado actual; el dedup transaccional
            // garantiza que el intento fallido no dejó rastro.
            try
            {
                await DespacharAsync(command, ct);
                _logger.LogInformation(
                    "Evento proyectado tras reintento por concurrencia {EventType} {EventId}.",
                    envelope!.EventType, envelope.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Conflicto de concurrencia persistente proyectando {EventType} {EventId}; se descarta (ack).",
                    envelope!.EventType, envelope.EventId);
            }
        }
        catch (Exception ex)
        {
            // Best-effort (ADR-0012): la proyección es reconstruible; sin requeue para evitar poison-loop.
            _logger.LogError(ex, "Fallo proyectando {EventType} {EventId}; se descarta (ack).",
                envelope!.EventType, envelope.EventId);
        }
        finally
        {
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
```

Y añadir el helper privado al final de la clase:

```csharp
    private async Task DespacharAsync(object command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(command, ct);
    }
```

(El bloque original creaba el scope inline; el helper conserva el patrón scope-por-despacho y lo reusa para el reintento.)

- [ ] **Step 4: Verificar build + suite completa**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS — todas las suites verdes, sin regresión.

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/OperacionesSesionEventsConsumer.cs
git commit -m "fix(puntuaciones): xmin como token de concurrencia en marcadores y reintento del worker (deuda SP-4a)"
```

---

### Task 8: Contratos, service-context, traceability y verificación final

**Files:**
- Modify: `contracts/http/puntuaciones-api.md`
- Modify: `services/puntuaciones/service-context.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: shapes reales fijados por los contract tests (Task 6).
- Produces: documentación canónica SP-4b.

- [ ] **Step 1: Actualizar `contracts/http/puntuaciones-api.md`**

Reemplazar la sección `## Status` por:

```markdown
## Status

Endpoints registrados (4): ranking nativo por juego + marcador propio (SP-4a) y ranking
consolidado de partida + rendimiento de equipo (SP-4b), servidos por las proyecciones
alimentadas por RabbitMQ (best-effort, ADR-0012). SignalR de ranking (SP-4c) y
auditoría/historial (SP-4d) pendientes.
```

Añadir a la tabla del Endpoint Registry:

```markdown
| Ranking consolidado de la partida | GET | `/puntuaciones/partidas/{partidaId}/ranking-consolidado` | Puntuaciones | Registered (SP-4b) |
| Rendimiento histórico de un equipo | GET | `/puntuaciones/equipos/{equipoId}/rendimiento` | Puntuaciones | Registered (SP-4b) |
```

Añadir antes de la sección `## Autorización`:

```markdown
## `GET /puntuaciones/partidas/{partidaId}/ranking-consolidado`

Ranking consolidado de una partida **terminada** (RF-45): ordena por juegos ganados DESC,
puntos totales DESC, tiempo total ASC. Ganador de cada juego = más puntos con desempate por
menor tiempo; empate exacto → ese juego no otorga victoria. Empate exacto en las tres claves
comparte `posicion` (1, 2, 2, 4). Calculado al leer.

- `200`:

```json
{
  "partidaId": "guid",
  "generadoEn": "datetime (UTC)",
  "entradas": [
    {
      "posicion": 1,
      "competidorId": "guid",
      "tipoCompetidor": "Participante | Equipo",
      "juegosGanados": 2,
      "puntosTotales": 45,
      "tiempoTotalMs": 23456
    }
  ]
}
```

- `404` `{ "message": "..." }`: la partida no existe en la proyección.
- `409` `{ "message": "..." }`: la partida no está `Terminada` (el consolidado se calcula al finalizar; sin consolidado provisional).
- Partida terminada sin marcadores → `200` con `entradas: []`.
- **Participación = tener ≥1 marcador**: competidores que nunca anotaron no aparecen (no hay evento de inscripción en el broker; best-effort ADR-0012).

## `GET /puntuaciones/equipos/{equipoId}/rendimiento`

Rendimiento histórico de un equipo (RF-44/HU-49): por cada partida por equipos **terminada**
donde el equipo tiene ≥1 marcador, su posición en el ranking consolidado y si la ganó
(`gano` = posición 1; si comparten la posición 1, ambos ganaron). Ordenado por `fechaFin`
descendente. Reusa el mismo cálculo del consolidado (RF-44: sin duplicar el cálculo).

- `200`:

```json
{
  "equipoId": "guid",
  "partidas": [
    { "partidaId": "guid", "fechaFin": "datetime", "posicion": 1, "gano": true }
  ]
}
```

- Equipo sin participaciones (o desconocido) → `200` con `partidas: []`.
```

- [ ] **Step 2: Actualizar `services/puntuaciones/service-context.md`**

Reescribir el resumen de estado: añadir SP-4b (consolidado on-read RF-45 + rendimiento de equipo RF-44 sobre las mismas proyecciones; 2 endpoints nuevos; `xmin` como token de concurrencia en `marcadores` con reintento único en el worker). En la deuda anotada: **retirar** la línea de `marcadores` sin token de concurrencia; **mantener** `ArgumentException`→400 sin log, retención/índice temporal de `eventos_procesados` → SP-4d, ramas warn+ack del worker sin unit tests, `[Authorize]`/hardening → SP-4c. Pending pasa a: live ranking SignalR (SP-4c), audit/history projection (SP-4d).

- [ ] **Step 3: Añadir la fila SP-4b a `docs/04-sdd/traceability-matrix.md`**

Añadir después de la fila SP-4a (formato de la tabla existente):

```markdown
| Puntuaciones — ranking consolidado + rendimiento de equipo (SP-4b) | Sobre las proyecciones SP-4a, dos queries on-read: `GET /puntuaciones/partidas/{partidaId}/ranking-consolidado` (solo partida `Terminada`; 409 en otro estado, 404 desconocida) ordena por juegos ganados DESC → puntos totales DESC → tiempo total ASC (ganador por juego = más puntos, desempate menor tiempo, empate exacto → juego sin victoria; empate triple comparte posición 1,2,2,4) y `GET /puntuaciones/equipos/{equipoId}/rendimiento` (partidas Equipo terminadas con ≥1 marcador del equipo, posición + `gano`, `fechaFin DESC`, lista vacía si nada). Un solo `CalculadorRankingConsolidado` reusado por ambas (RF-44 sin duplicar cálculo), tolerante a scoring tardío y juegos no proyectados. Participación = tener marcador (competidores con 0 puntos no aparecen — sin evento de inscripción en el broker). Deuda SP-4a saldada: `xmin` token de concurrencia en `marcadores` + reintento único del worker ante `DbUpdateConcurrencyException`. Sin eventos nuevos ni cambios de esquema. | Puntuaciones | — | docs/superpowers/specs/2026-07-05-sp4b-consolidado-rendimiento-equipo-design.md · docs/superpowers/plans/2026-07-05-sp4b-consolidado-rendimiento-equipo.md | contracts/http/puntuaciones-api.md | Implemented — <conteos reales "N unit + N integration + N contract">. **Fuente:** RF-44, RF-45, RB-40/RB-41, HU-49, HU-50. **Diferido:** SignalR ranking (incl. `RankingConsolidadoCalculado`)→SP-4c, auditoría/historial→SP-4d, publisher propio y outbox→ADR-0012, cableado clientes→SP-5. |
```

(rellenar los conteos con la salida real de `dotnet test`)

- [ ] **Step 4: Verificación final completa**

```bash
dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"
```

Expected: PASS total. Registrar los conteos (aprox. esperado: ~72 unit + ~15 integration + ~7 contract; usar los números reales en traceability). Opcional si hay Docker: round-trip real con `docker compose -f "infra/docker-compose.yml" up -d rabbitmq` + `RABBITMQ_TEST_HOST=localhost dotnet test ... --filter RabbitMqProyeccionRoundTripTests`.

- [ ] **Step 5: Commit de cierre**

```bash
git add contracts/http/puntuaciones-api.md services/puntuaciones/service-context.md docs/04-sdd/traceability-matrix.md
git commit -m "docs(puntuaciones): contratos SP-4b, service-context y traceability"
```

---

## Cierre del slice

- Review final whole-branch del rango de commits SP-4b (`superpowers:requesting-code-review`).
- El merge/PR se decide con `superpowers:finishing-a-development-branch` (recordar: SP-4a sigue sin integrar; SP-4b apila sobre ella).
- Post-slice: SP-4c (SignalR de ranking en vivo) dispone del consolidado calculable; SP-4d cubre auditoría/historial.
