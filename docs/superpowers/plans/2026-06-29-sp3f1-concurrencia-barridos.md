# SP-3f-1 — Concurrencia optimista + barridos por tiempo — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que una `SesionPartida` avance sola en el tiempo — inicia automáticamente al llegar su hora y cierra por timeout la pregunta/etapa vencida — protegida con concurrencia optimista (`xmin`) frente al segundo escritor.

**Architecture:** Un `BackgroundService` (`MantenimientoSesionesWorker`) con `PeriodicTimer` dispara por tick dos comandos MediatR (`BarrerIniciosAutomaticosCommand`, `BarrerTimeoutsCommand`). Cada barrido escanea el repositorio, procesa cada candidato en su propio `SaveChanges` (xmin-guarded), y reúsa el dominio existente (`CerrarActividadVencida`, `IntentarInicioAutomatico`) + la emisión de eventos existente. El conflicto de concurrencia en el lado request se mapea a 409.

**Tech Stack:** .NET 8, EF Core 8 + Npgsql (runtime) / InMemory (tests), MediatR, `System.TimeProvider`, xUnit + fakes (sin Moq).

## Global Constraints

- **Servicio:** `services/operaciones-sesion` exclusivamente. Backend puro — sin cambios web/mobile/gateway.
- **Individual-only.** Equipo/convocatoria → slice-E. No tocar.
- **Eventos por `NoOpSesionEventsPublisher`** (RabbitMQ real = slice propio). Los barridos emiten los **mismos** eventos que el path request.
- **Sin SignalR, sin scoring/ranking** (SP-3f-2 / SP-4).
- **Reloj:** siempre `TimeProvider.GetUtcNow().UtcDateTime`. Tests: `FakeTimeProvider` (fake del repo en `tests/.../Application/Fakes/FakeTimeProvider.cs`, ctor `new FakeTimeProvider(DateTime)`).
- **Value objects:** id vía `.Valor` (p.ej. `sesion.Id.Valor`). **Enums** serializados vía `.ToString()`.
- **Sin Moq.** Reusar los fakes existentes en `tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/` (`FakeSesionPartidaRepository`, `FakeOperacionesSesionUnitOfWork`, `FakeSesionEventsPublisher`, `FakeTimeProvider`, `BdtBuilder`).
- **xmin condicional a Npgsql:** el token se aplica SOLO si el provider es Npgsql, para no romper los IntegrationTests que usan InMemory (que ignora tokens). El enforcement DB-level no se integration-testea (gap documentado en el spec §7); se testea el contrato de comportamiento (handler salta en conflicto; middleware → 409) con fakes.
- **Carve-out git (vigente, = SP-3c..3e):** la fila de `docs/04-sdd/traceability-matrix.md` se **escribe pero NO se commitea** (se une al squash pendiente del usuario). Cada `git add` lista **archivos exactos** — nunca `git add -A`/`.`/`docs/`.
- **Commits:** cada mensaje termina con `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
- **Suite completa:** `dotnet test "services/operaciones-sesion"`. Baseline al iniciar: verde (UnitTests 179 · IntegrationTests 13 · ContractTests 32 — confirmar en T1 Step 0).

---

### Task 1: Token de concurrencia `xmin` (condicional Npgsql) + migración

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs`
- Create (generado por EF): `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/<timestamp>_SP3f1ConcurrencyToken.cs` (+ `.Designer.cs` + snapshot actualizado)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/ConcurrencyTokenTests.cs`

**Interfaces:**
- Consumes: `OperacionesSesionDbContext`, `SesionPartida`.
- Produces: el modelo EF de `SesionPartida` expone una propiedad `IsConcurrencyToken` cuando el provider es Npgsql.

- [ ] **Step 0: Baseline verde**

Run: `dotnet test "services/operaciones-sesion"`
Expected: PASS. Anotar los conteos por proyecto (sirven de referencia para los +N de cada tarea).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.IntegrationTests/ConcurrencyTokenTests.cs
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class ConcurrencyTokenTests
{
    // Construir el modelo con Npgsql NO abre conexión: solo se inspecciona ctx.Model.
    private static OperacionesSesionDbContext NpgsqlCtx() =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseNpgsql("Host=localhost;Database=x;Username=x;Password=x").Options);

    [Fact]
    public void SesionPartida_tiene_token_de_concurrencia_en_npgsql()
    {
        using var ctx = NpgsqlCtx();
        var et = ctx.Model.FindEntityType(typeof(SesionPartida))!;
        Assert.Contains(et.GetProperties(), p => p.IsConcurrencyToken);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests" --filter "FullyQualifiedName~ConcurrencyToken"`
Expected: FAIL (aún no hay token de concurrencia).

- [ ] **Step 3: Write minimal implementation**

En `OperacionesSesionDbContext.OnModelCreating`, dentro del bloque `modelBuilder.Entity<SesionPartida>(entity => { ... })`, añadir como **última** línea del lambda:

```csharp
            if (Database.IsNpgsql())
            {
                entity.UseXminAsConcurrencyToken();
            }
```

> `Database.IsNpgsql()` está disponible en `OnModelCreating` (el provider ya está resuelto) y vive en el namespace `Microsoft.EntityFrameworkCore` (ya importado). Bajo InMemory la condición es falsa → el modelo no lleva token → los IntegrationTests InMemory siguen verdes sin cambios.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests" --filter "FullyQualifiedName~ConcurrencyToken"`
Expected: PASS.

- [ ] **Step 5: Generar la migración**

Run (desde la raíz del repo):
```bash
dotnet ef migrations add SP3f1ConcurrencyToken \
  --project services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure \
  --startup-project services/operaciones-sesion/src/Umbral.OperacionesSesion.Api \
  --output-dir Persistence/Migrations
```
> Si falta la herramienta: `dotnet tool install --global dotnet-ef`. El `Up()`/`Down()` muy probablemente queda **vacío** (xmin es columna de sistema de Postgres, no añade columna física); el valor de la migración es actualizar el ModelSnapshot. Commitear lo que genere EF sin editarlo a mano.

- [ ] **Step 6: Suite completa (no rompió InMemory)**

Run: `dotnet test "services/operaciones-sesion"`
Expected: PASS. IntegrationTests +1 (ConcurrencyTokenTests). Resto sin cambios.
> Si algún test InMemory se rompiera aquí, el guard `Database.IsNpgsql()` no se aplicó bien — revisar antes de continuar.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/ \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/ConcurrencyTokenTests.cs
git commit -m "SP-3f-1 T1: token de concurrencia xmin (condicional Npgsql) + migración

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Dominio — `CerrarActividadVencida` + `ResultadoCierreVencido`

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoCierreVencido.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/CerrarActividadVencidaTests.cs`

**Interfaces:**
- Consumes: métodos existentes `AvanzarPregunta(now)`, `AvanzarEtapa(now)`, `FinalizarJuegoActual(now)` y los resultados `ResultadoAvancePregunta`, `ResultadoAvanceEtapa`, `ResultadoAvance`.
- Produces:
  - `public ResultadoCierreVencido CerrarActividadVencida(DateTime now)` en `SesionPartida`.
  - `enum TipoCierreVencido { Ninguna, Trivia, Bdt }`.
  - `record ResultadoCierreVencido(TipoCierreVencido Tipo, ResultadoAvancePregunta? Pregunta, ResultadoAvanceEtapa? Etapa, ResultadoAvance? JuegoFinalizado)` con `bool HuboCambio` y factories `Ninguna`/`Trivia(...)`/`Bdt(...)`.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.UnitTests/Domain/CerrarActividadVencidaTests.cs
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Results;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class CerrarActividadVencidaTests
{
    private static readonly DateTime T0 = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot P(int orden, int limite) =>
        new(Guid.NewGuid(), orden, $"Q{orden}", 10, limite,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });

    private static SesionPartida TriviaIniciada(params PreguntaSnapshot[] preguntas)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, preguntas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(Guid.NewGuid(), snap);
        s.Inscribir(Guid.NewGuid(), false, 0, T0);
        s.Iniciar(T0);
        return s;
    }

    private static SesionPartida BdtIniciada(params (string qr, int limite)[] etapas)
    {
        var snapEtapas = etapas.Select((e, i) => new EtapaSnapshot(Guid.NewGuid(), i + 1, e.qr, 50, e.limite)).ToArray();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Área", snapEtapas);
        var snap = new ConfiguracionSnapshot("Copa BDT", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(Guid.NewGuid(), snap);
        s.Inscribir(Guid.NewGuid(), false, 0, T0);
        s.Iniciar(T0);
        return s;
    }

    [Fact]
    public void No_op_cuando_pregunta_no_vencida()
    {
        var s = TriviaIniciada(P(1, 30), P(2, 30));
        var r = s.CerrarActividadVencida(T0.AddSeconds(10)); // dentro de ventana
        Assert.False(r.HuboCambio);
        Assert.Equal(TipoCierreVencido.Ninguna, r.Tipo);
        Assert.Equal(EstadoSesion.Iniciada, s.Estado);
    }

    [Fact]
    public void Trivia_vencida_cierra_por_tiempo_y_activa_siguiente()
    {
        var s = TriviaIniciada(P(1, 30), P(2, 30));
        var r = s.CerrarActividadVencida(T0.AddSeconds(31));
        Assert.Equal(TipoCierreVencido.Trivia, r.Tipo);
        Assert.Equal(MotivoCierrePregunta.Tiempo, r.Pregunta!.MotivoCierre);
        Assert.Equal(2, r.Pregunta.PreguntaActivadaOrden);
        Assert.Null(r.JuegoFinalizado);
    }

    [Fact]
    public void Trivia_ultima_vencida_finaliza_y_termina_partida()
    {
        var s = TriviaIniciada(P(1, 30)); // única pregunta del único juego
        var r = s.CerrarActividadVencida(T0.AddSeconds(31));
        Assert.Equal(TipoCierreVencido.Trivia, r.Tipo);
        Assert.True(r.Pregunta!.SinMasPreguntas);
        Assert.NotNull(r.JuegoFinalizado);
        Assert.True(r.JuegoFinalizado!.Terminada());
        Assert.Equal(EstadoSesion.Terminada, s.Estado);
    }

    [Fact]
    public void Bdt_vencida_cierra_por_tiempo_y_activa_siguiente()
    {
        var s = BdtIniciada(("QR-1", 60), ("QR-2", 60));
        var r = s.CerrarActividadVencida(T0.AddSeconds(61));
        Assert.Equal(TipoCierreVencido.Bdt, r.Tipo);
        Assert.Equal(MotivoCierreEtapa.Tiempo, r.Etapa!.MotivoCierre);
        Assert.Equal(2, r.Etapa.EtapaActivadaOrden);
        Assert.Null(r.JuegoFinalizado);
    }

    [Fact]
    public void Bdt_ultima_vencida_finaliza_y_termina_partida()
    {
        var s = BdtIniciada(("QR-1", 60));
        var r = s.CerrarActividadVencida(T0.AddSeconds(61));
        Assert.Equal(TipoCierreVencido.Bdt, r.Tipo);
        Assert.True(r.Etapa!.SinMasEtapas);
        Assert.NotNull(r.JuegoFinalizado);
        Assert.True(r.JuegoFinalizado!.Terminada());
        Assert.Equal(EstadoSesion.Terminada, s.Estado);
    }

    [Fact]
    public void Idempotente_segunda_llamada_es_no_op()
    {
        var s = TriviaIniciada(P(1, 30), P(2, 30));
        s.CerrarActividadVencida(T0.AddSeconds(31)); // cierra Q1, activa Q2 (FechaActivacion = ese now)
        var r2 = s.CerrarActividadVencida(T0.AddSeconds(31)); // Q2 recién activada, no vencida
        Assert.False(r2.HuboCambio);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~CerrarActividadVencida"`
Expected: FAIL (`CerrarActividadVencida` / `ResultadoCierreVencido` no existen).

- [ ] **Step 3: Write minimal implementation**

Crear `Domain/Results/ResultadoCierreVencido.cs`:

```csharp
namespace Umbral.OperacionesSesion.Domain.Results;

public enum TipoCierreVencido { Ninguna, Trivia, Bdt }

public sealed record ResultadoCierreVencido(
    TipoCierreVencido Tipo,
    ResultadoAvancePregunta? Pregunta,
    ResultadoAvanceEtapa? Etapa,
    ResultadoAvance? JuegoFinalizado)
{
    public static ResultadoCierreVencido Ninguna { get; } = new(TipoCierreVencido.Ninguna, null, null, null);
    public static ResultadoCierreVencido Trivia(ResultadoAvancePregunta pregunta, ResultadoAvance? juegoFinalizado) =>
        new(TipoCierreVencido.Trivia, pregunta, null, juegoFinalizado);
    public static ResultadoCierreVencido Bdt(ResultadoAvanceEtapa etapa, ResultadoAvance? juegoFinalizado) =>
        new(TipoCierreVencido.Bdt, null, etapa, juegoFinalizado);

    public bool HuboCambio => Tipo != TipoCierreVencido.Ninguna;
}
```

En `SesionPartida.cs`, añadir el método (p.ej. tras `AvanzarEtapa`, antes de `JuegoBDTActivo`):

```csharp
    public ResultadoCierreVencido CerrarActividadVencida(DateTime now)
    {
        if (Estado != EstadoSesion.Iniciada)
            return ResultadoCierreVencido.Ninguna;

        var juego = _juegos.SingleOrDefault(j => j.Estado == EstadoJuego.Activo);
        if (juego is null)
            return ResultadoCierreVencido.Ninguna;

        if (juego.TipoJuego == TipoJuego.Trivia)
        {
            var activa = juego.PreguntaActiva;
            if (activa is null || now < activa.FechaActivacion!.Value.AddSeconds(activa.TiempoLimiteSegundos))
                return ResultadoCierreVencido.Ninguna;

            var rp = AvanzarPregunta(now); // vencida → MotivoCierre.Tiempo
            var fin = rp.SinMasPreguntas ? FinalizarJuegoActual(now) : null;
            return ResultadoCierreVencido.Trivia(rp, fin);
        }

        if (juego.TipoJuego == TipoJuego.BusquedaDelTesoro)
        {
            var activa = juego.EtapaActiva;
            if (activa is null || now < activa.FechaActivacion!.Value.AddSeconds(activa.TiempoLimiteSegundos))
                return ResultadoCierreVencido.Ninguna;

            var re = AvanzarEtapa(now); // vencida → MotivoCierre.Tiempo
            var fin = re.SinMasEtapas ? FinalizarJuegoActual(now) : null;
            return ResultadoCierreVencido.Bdt(re, fin);
        }

        return ResultadoCierreVencido.Ninguna;
    }
```

> Reúsa la maquinaria de cierre/avance existente; el único añadido es el guard de vencimiento (mismo cálculo `FechaActivacion + TiempoLimiteSegundos` que ya usan `AvanzarPregunta`/`AvanzarEtapa`) y la composición con `FinalizarJuegoActual` cuando fue el último paso.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~CerrarActividadVencida"`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoCierreVencido.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/CerrarActividadVencidaTests.cs
git commit -m "SP-3f-1 T2: dominio CerrarActividadVencida (cierre por timeout idempotente, reúsa avance+finalizar)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Scans del repositorio (interface + EF + fake) + integration tests

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/SesionPartidaRepositoryScansTests.cs`

**Interfaces:**
- Produces (añadir a `ISesionPartidaRepository`):
  - `Task<IReadOnlyList<SesionPartida>> GetSesionesConActividadVencidaAsync(DateTime now, CancellationToken cancellationToken)`
  - `Task<IReadOnlyList<SesionPartida>> GetSesionesAutoInicioPendienteAsync(DateTime now, CancellationToken cancellationToken)`

> Añadir métodos a la interface rompe la compilación hasta actualizar **ambos** implementadores (EF + fake). Esta tarea los actualiza juntos.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.IntegrationTests/SesionPartidaRepositoryScansTests.cs
using System;
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

public class SesionPartidaRepositoryScansTests
{
    private static readonly DateTime T0 = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private static OperacionesSesionDbContext NewCtx(string name) =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options);

    private static PreguntaSnapshot P(int orden, int limite) =>
        new(Guid.NewGuid(), orden, $"Q{orden}", 10, limite,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });

    private static SesionPartida TriviaPublicada(int limite, DateTime? tiempoInicio, ModoInicioPartida modo)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { P(1, limite), P(2, limite) });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, modo, tiempoInicio, 1, 5, new[] { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public async Task ConActividadVencida_devuelve_solo_iniciadas_con_paso_vencido()
    {
        await using var ctx = NewCtx("scan-venc-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);

        var vencida = TriviaPublicada(30, null, ModoInicioPartida.Manual);
        vencida.Inscribir(Guid.NewGuid(), false, 0, T0);
        vencida.Iniciar(T0); // Q1 activa, FechaActivacion = T0

        var noVencida = TriviaPublicada(30, null, ModoInicioPartida.Manual);
        noVencida.Inscribir(Guid.NewGuid(), false, 0, T0);
        noVencida.Iniciar(T0);

        var enLobby = TriviaPublicada(30, null, ModoInicioPartida.Manual);

        repo.Add(vencida); repo.Add(noVencida); repo.Add(enLobby);
        await ctx.SaveChangesAsync();

        var r = await repo.GetSesionesConActividadVencidaAsync(T0.AddSeconds(31), CancellationToken.None);

        Assert.Contains(r, s => s.PartidaId == vencida.PartidaId);
        Assert.DoesNotContain(r, s => s.PartidaId == enLobby.PartidaId);
        // noVencida: con now=T0+31 su Q (limite 30) también está vencida → también aparece;
        // el filtro clave probado aquí es "Iniciada con paso vencido" vs "Lobby".
    }

    [Fact]
    public async Task AutoInicioPendiente_devuelve_solo_lobby_automatico_con_hora_cumplida()
    {
        await using var ctx = NewCtx("scan-auto-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);

        var due = TriviaPublicada(30, T0, ModoInicioPartida.Automatico);          // Lobby, hora cumplida
        var futura = TriviaPublicada(30, T0.AddHours(1), ModoInicioPartida.Automatico); // Lobby, aún no
        var manual = TriviaPublicada(30, T0, ModoInicioPartida.Manual);            // Lobby pero Manual

        repo.Add(due); repo.Add(futura); repo.Add(manual);
        await ctx.SaveChangesAsync();

        var r = await repo.GetSesionesAutoInicioPendienteAsync(T0.AddSeconds(1), CancellationToken.None);

        Assert.Contains(r, s => s.PartidaId == due.PartidaId);
        Assert.DoesNotContain(r, s => s.PartidaId == futura.PartidaId);
        Assert.DoesNotContain(r, s => s.PartidaId == manual.PartidaId);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests" --filter "FullyQualifiedName~RepositoryScans"`
Expected: FAIL de compilación (métodos no existen).

- [ ] **Step 3: Write minimal implementation**

En `ISesionPartidaRepository` añadir las dos firmas:

```csharp
    Task<IReadOnlyList<SesionPartida>> GetSesionesConActividadVencidaAsync(DateTime now, CancellationToken cancellationToken);
    Task<IReadOnlyList<SesionPartida>> GetSesionesAutoInicioPendienteAsync(DateTime now, CancellationToken cancellationToken);
```

En `SesionPartidaRepository` (EF) añadir:

```csharp
    public async Task<IReadOnlyList<SesionPartida>> GetSesionesConActividadVencidaAsync(
        DateTime now, CancellationToken cancellationToken)
    {
        var iniciadas = await _dbContext.Sesiones
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas)
            .Include(s => s.Juegos).ThenInclude(j => j.Etapas)
            .Where(s => s.Estado == EstadoSesion.Iniciada)
            .ToListAsync(cancellationToken);

        return iniciadas
            .Where(s => TienePasoVencido(s, now))
            .ToList();
    }

    public async Task<IReadOnlyList<SesionPartida>> GetSesionesAutoInicioPendienteAsync(
        DateTime now, CancellationToken cancellationToken)
        => await _dbContext.Sesiones
            .Include(s => s.Inscripciones)
            .Include(s => s.Juegos)
            .Where(s => s.Estado == EstadoSesion.Lobby
                && (s.ModoInicioPartida == ModoInicioPartida.Automatico
                    || s.ModoInicioPartida == ModoInicioPartida.ManualYAutomatico)
                && s.TiempoInicio != null
                && s.TiempoInicio <= now)
            .ToListAsync(cancellationToken);

    private static bool TienePasoVencido(SesionPartida sesion, DateTime now)
    {
        var juego = sesion.Juegos.FirstOrDefault(j => j.Estado == EstadoJuego.Activo);
        if (juego is null) return false;
        var pregunta = juego.PreguntaActiva;
        if (pregunta is not null)
            return now >= pregunta.FechaActivacion!.Value.AddSeconds(pregunta.TiempoLimiteSegundos);
        var etapa = juego.EtapaActiva;
        if (etapa is not null)
            return now >= etapa.FechaActivacion!.Value.AddSeconds(etapa.TiempoLimiteSegundos);
        return false;
    }
```

> El predicado de vencimiento se evalúa en memoria (post-`ToList`) porque depende del paso activo dentro del grafo; a escala académica cargar las `Iniciada` por tick es aceptable (spec §4.2). `ModoInicioPartida` ya es enum mapeado; el `using Umbral.OperacionesSesion.Domain.Enums;` ya está en el archivo.

En `FakeSesionPartidaRepository` (tests) añadir, mirroreando el predicado (timeouts: devuelve todas las Iniciada — el dominio re-chequea vencida; auto-inicio: filtra por los campos de sesión):

```csharp
    public Task<IReadOnlyList<SesionPartida>> GetSesionesConActividadVencidaAsync(DateTime now, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<SesionPartida>)_store.Values
            .Where(s => s.Estado == EstadoSesion.Iniciada)
            .ToList());

    public Task<IReadOnlyList<SesionPartida>> GetSesionesAutoInicioPendienteAsync(DateTime now, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<SesionPartida>)_store.Values
            .Where(s => s.Estado == EstadoSesion.Lobby
                && (s.ModoInicioPartida == ModoInicioPartida.Automatico
                    || s.ModoInicioPartida == ModoInicioPartida.ManualYAutomatico)
                && s.TiempoInicio != null
                && s.TiempoInicio <= now)
            .ToList());
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests" --filter "FullyQualifiedName~RepositoryScans"`
Expected: PASS (2 tests). La suite completa debe seguir compilando (fake actualizado).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/SesionPartidaRepositoryScansTests.cs
git commit -m "SP-3f-1 T3: scans de repo (actividad vencida / auto-inicio pendiente) + fake + integration tests

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: `BarrerTimeoutsCommand` + handler + tests

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/BarrerTimeoutsCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/BarrerTimeoutsCommandHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/BarrerTimeoutsCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ISesionPartidaRepository.GetSesionesConActividadVencidaAsync` (T3), `SesionPartida.CerrarActividadVencida` (T2), `ISesionEventsPublisher`, helpers estáticos `IniciarPartidaCommandHandler.PublicarPreguntaActivadaSiTriviaAsync` / `PublicarEtapaActivadaSiBdtAsync`, event records existentes.
- Produces: `record BarrerTimeoutsCommand() : IRequest<int>` (devuelve nº de sesiones avanzadas).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.UnitTests/Application/BarrerTimeoutsCommandHandlerTests.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class BarrerTimeoutsCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot P(int orden, int limite) =>
        new(Guid.NewGuid(), orden, $"Q{orden}", 10, limite,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });

    private static SesionPartida TriviaIniciada(int limite)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { P(1, limite), P(2, limite) });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(Guid.NewGuid(), snap);
        s.Inscribir(Guid.NewGuid(), false, 0, T0);
        s.Iniciar(T0);
        return s;
    }

    private static BarrerTimeoutsCommandHandler Build(
        ISesionPartidaRepository repo, IOperacionesSesionUnitOfWork uow, FakeSesionEventsPublisher events, DateTime now)
        => new(repo, uow, events, new FakeTimeProvider(now), NullLogger<BarrerTimeoutsCommandHandler>.Instance);

    [Fact]
    public async Task Cierra_vencida_emite_eventos_y_cuenta()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(TriviaIniciada(30));
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, T0.AddSeconds(31));

        var n = await handler.Handle(new BarrerTimeoutsCommand(), CancellationToken.None);

        Assert.Equal(1, n);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.PreguntasCerradas);
        Assert.Single(events.PreguntasActivadas); // se activó Q2
    }

    [Fact]
    public async Task No_vencida_no_guarda_ni_emite()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(TriviaIniciada(30));
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, T0.AddSeconds(10)); // dentro de ventana

        var n = await handler.Handle(new BarrerTimeoutsCommand(), CancellationToken.None);

        Assert.Equal(0, n);
        Assert.Equal(0, uow.SaveCount);
        Assert.Empty(events.PreguntasCerradas);
    }

    [Fact]
    public async Task Conflicto_en_un_candidato_no_aborta_el_resto()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(TriviaIniciada(30));
        repo.Add(TriviaIniciada(30));
        var uow = new ThrowOnceUnitOfWork(); // 1er SaveChanges lanza DbUpdateConcurrencyException
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, T0.AddSeconds(31));

        var n = await handler.Handle(new BarrerTimeoutsCommand(), CancellationToken.None);

        Assert.Equal(1, n); // el segundo candidato sí avanzó
    }

    private sealed class ThrowOnceUnitOfWork : IOperacionesSesionUnitOfWork
    {
        private bool _thrown;
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (!_thrown) { _thrown = true; throw new DbUpdateConcurrencyException("conflict"); }
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~BarrerTimeouts"`
Expected: FAIL de compilación (`BarrerTimeoutsCommand` no existe).

- [ ] **Step 3: Write minimal implementation**

Crear `Application/Commands/BarrerTimeoutsCommand.cs`:

```csharp
using MediatR;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record BarrerTimeoutsCommand() : IRequest<int>;
```

Crear `Application/Handlers/Commands/BarrerTimeoutsCommandHandler.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class BarrerTimeoutsCommandHandler : IRequestHandler<BarrerTimeoutsCommand, int>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BarrerTimeoutsCommandHandler> _logger;

    public BarrerTimeoutsCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events, TimeProvider timeProvider,
        ILogger<BarrerTimeoutsCommandHandler> logger)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> Handle(BarrerTimeoutsCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var candidatos = await _sesiones.GetSesionesConActividadVencidaAsync(now, cancellationToken);
        var avanzadas = 0;

        foreach (var sesion in candidatos)
        {
            try
            {
                var r = sesion.CerrarActividadVencida(now);
                if (!r.HuboCambio) continue;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await PublicarAsync(sesion, r, now, cancellationToken);
                avanzadas++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Barrido de timeout: candidato {PartidaId} saltado.", sesion.PartidaId);
            }
        }

        return avanzadas;
    }

    private async Task PublicarAsync(SesionPartida sesion, ResultadoCierreVencido r, DateTime now, CancellationToken ct)
    {
        if (r.Tipo == TipoCierreVencido.Trivia)
        {
            var p = r.Pregunta!;
            await _events.PublicarPreguntaTriviaCerradaAsync(
                new PreguntaTriviaCerradaEvent(sesion.PartidaId, sesion.Id.Valor, p.JuegoId, p.PreguntaCerradaId,
                    p.MotivoCierre.ToString(), now, null), ct);
            if (p.PreguntaActivadaId is not null)
                await _events.PublicarPreguntaTriviaActivadaAsync(
                    new PreguntaTriviaActivadaEvent(sesion.PartidaId, sesion.Id.Valor, p.JuegoId, p.PreguntaActivadaId.Value,
                        p.PreguntaActivadaOrden!.Value, p.TiempoLimiteActivadaSegundos!.Value, p.FechaActivacionActivada!.Value), ct);
        }
        else if (r.Tipo == TipoCierreVencido.Bdt)
        {
            var e = r.Etapa!;
            await _events.PublicarEtapaBDTCerradaAsync(
                new EtapaBDTCerradaEvent(sesion.PartidaId, sesion.Id.Valor, e.JuegoId, e.EtapaCerradaId,
                    e.MotivoCierre.ToString(), now, null), ct);
            if (e.EtapaActivadaId is not null)
                await _events.PublicarEtapaBDTActivadaAsync(
                    new EtapaBDTActivadaEvent(sesion.PartidaId, sesion.Id.Valor, e.JuegoId, e.EtapaActivadaId.Value,
                        e.EtapaActivadaOrden!.Value, e.TiempoLimiteActivadaSegundos!.Value, e.FechaActivacionActivada!.Value), ct);
        }

        if (r.JuegoFinalizado is { } fin)
        {
            if (fin.Tipo == TipoResultadoAvance.Avanzado)
            {
                var juego = fin.JuegoActivado!;
                await _events.PublicarJuegoActivadoAsync(
                    new JuegoActivadoEvent(sesion.PartidaId, sesion.Id.Valor, juego.JuegoId, juego.Orden, juego.TipoJuego.ToString()), ct);
                await IniciarPartidaCommandHandler.PublicarPreguntaActivadaSiTriviaAsync(_events, sesion, juego, ct);
                await IniciarPartidaCommandHandler.PublicarEtapaActivadaSiBdtAsync(_events, sesion, juego, ct);
            }
            else
            {
                await _events.PublicarPartidaFinalizadaAsync(
                    new PartidaFinalizadaEvent(sesion.PartidaId, sesion.Id.Valor, now), ct);
            }
        }
    }
}
```

> Los `Event` records (`PreguntaTriviaCerradaEvent`, etc.) viven en `Application.Interfaces` (mismo namespace que `ISesionEventsPublisher`), ya en alcance por `using ...Application.Interfaces`. El `catch (Exception)` por ítem es el patrón de resiliencia del barrido (no hay base `DomainException`; cubre `DbUpdateConcurrencyException` + excepciones de dominio por estado cambiado).

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~BarrerTimeouts"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/BarrerTimeoutsCommand.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/BarrerTimeoutsCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/BarrerTimeoutsCommandHandlerTests.cs
git commit -m "SP-3f-1 T4: BarrerTimeoutsCommand + handler (cierra vencidas, emite eventos, resiliente a conflicto)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: `BarrerIniciosAutomaticosCommand` + handler + tests

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/BarrerIniciosAutomaticosCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/BarrerIniciosAutomaticosCommandHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/BarrerIniciosAutomaticosCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ISesionPartidaRepository.GetSesionesAutoInicioPendienteAsync` (T3), `SesionPartida.IntentarInicioAutomatico` (existente), helper estático `IniciarPartidaCommandHandler.PublicarEventosInicioAsync` (existente).
- Produces: `record BarrerIniciosAutomaticosCommand() : IRequest<int>` (devuelve nº de sesiones iniciadas o canceladas).

> Diseño: el handler hace el trabajo **directamente** (load candidatos → `IntentarInicioAutomatico(now)` → save si no `NoCorresponde` → `PublicarEventosInicioAsync`), reutilizando el método de dominio y el helper estático de eventos. No envía sub-comandos MediatR — así el test usa los fakes estándar (repo/uow/events/time), idéntico al resto de handler tests.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.UnitTests/Application/BarrerIniciosAutomaticosCommandHandlerTests.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class BarrerIniciosAutomaticosCommandHandlerTests
{
    private static readonly DateTime TDue = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida AutoEnLobby(DateTime tiempoInicio, int inscritos, int minimos)
    {
        var juegos = Enumerable.Range(1, 2).Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia)).ToList();
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Automatico, tiempoInicio, minimos, 5, juegos);
        var s = SesionPartida.Publicar(Guid.NewGuid(), snap);
        for (var i = 0; i < inscritos; i++) s.Inscribir(Guid.NewGuid(), false, i, tiempoInicio.AddSeconds(-1));
        return s;
    }

    private static BarrerIniciosAutomaticosCommandHandler Build(
        ISesionPartidaRepository repo, IOperacionesSesionUnitOfWork uow, FakeSesionEventsPublisher events, DateTime now)
        => new(repo, uow, events, new FakeTimeProvider(now), NullLogger<BarrerIniciosAutomaticosCommandHandler>.Instance);

    [Fact]
    public async Task Inicia_los_due_con_minimos_cumplidos()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(AutoEnLobby(TDue, inscritos: 1, minimos: 1));
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, TDue);

        var n = await handler.Handle(new BarrerIniciosAutomaticosCommand(), CancellationToken.None);

        Assert.Equal(1, n);
        Assert.Single(events.PartidasIniciadas);
        Assert.Single(events.JuegosActivados);
        Assert.Equal(EstadoSesion.Iniciada, repo.Store.Values.Single().Estado);
    }

    [Fact]
    public async Task Auto_cancela_los_due_bajo_minimos()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(AutoEnLobby(TDue, inscritos: 0, minimos: 2)); // 0 < 2 → cancela
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, TDue);

        var n = await handler.Handle(new BarrerIniciosAutomaticosCommand(), CancellationToken.None);

        Assert.Equal(1, n);
        Assert.Single(events.PartidasCanceladas);
        Assert.Equal(EstadoSesion.Cancelada, repo.Store.Values.Single().Estado);
    }

    [Fact]
    public async Task No_due_no_es_candidato_y_no_hace_nada()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(AutoEnLobby(TDue.AddHours(1), inscritos: 1, minimos: 1)); // futura
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, TDue);

        var n = await handler.Handle(new BarrerIniciosAutomaticosCommand(), CancellationToken.None);

        Assert.Equal(0, n);
        Assert.Equal(0, uow.SaveCount);
        Assert.Empty(events.PartidasIniciadas);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~BarrerIniciosAutomaticos"`
Expected: FAIL de compilación (`BarrerIniciosAutomaticosCommand` no existe).

- [ ] **Step 3: Write minimal implementation**

Crear `Application/Commands/BarrerIniciosAutomaticosCommand.cs`:

```csharp
using MediatR;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record BarrerIniciosAutomaticosCommand() : IRequest<int>;
```

Crear `Application/Handlers/Commands/BarrerIniciosAutomaticosCommandHandler.cs`:

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class BarrerIniciosAutomaticosCommandHandler : IRequestHandler<BarrerIniciosAutomaticosCommand, int>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BarrerIniciosAutomaticosCommandHandler> _logger;

    public BarrerIniciosAutomaticosCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events, TimeProvider timeProvider,
        ILogger<BarrerIniciosAutomaticosCommandHandler> logger)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> Handle(BarrerIniciosAutomaticosCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var candidatos = await _sesiones.GetSesionesAutoInicioPendienteAsync(now, cancellationToken);
        var aplicadas = 0;

        foreach (var sesion in candidatos)
        {
            try
            {
                var resultado = sesion.IntentarInicioAutomatico(now);
                if (resultado.Tipo == TipoResultadoInicio.NoCorresponde) continue;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await IniciarPartidaCommandHandler.PublicarEventosInicioAsync(_events, sesion, resultado, now, cancellationToken);
                aplicadas++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Barrido de auto-inicio: candidato {PartidaId} saltado.", sesion.PartidaId);
            }
        }

        return aplicadas;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~BarrerIniciosAutomaticos"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/BarrerIniciosAutomaticosCommand.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/BarrerIniciosAutomaticosCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/BarrerIniciosAutomaticosCommandHandlerTests.cs
git commit -m "SP-3f-1 T5: BarrerIniciosAutomaticosCommand + handler (inicia/auto-cancela los due, resiliente)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Worker `MantenimientoSesionesWorker` + opciones + DI + test

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Configuration/MantenimientoOptions.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Workers/MantenimientoSesionesWorker.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/MantenimientoSesionesWorkerTests.cs`

**Interfaces:**
- Consumes: `IServiceScopeFactory`, `ISender` (MediatR), `BarrerIniciosAutomaticosCommand` (T5), `BarrerTimeoutsCommand` (T4).
- Produces:
  - `class MantenimientoOptions { public int IntervaloMs { get; set; } = 1000; }`
  - `class MantenimientoSesionesWorker : BackgroundService` con `internal async Task EjecutarTickAsync(CancellationToken)` (testeable directamente).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.UnitTests/Api/MantenimientoSesionesWorkerTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Umbral.OperacionesSesion.Api.Workers;
using Umbral.OperacionesSesion.Application.Commands;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class MantenimientoSesionesWorkerTests
{
    [Fact]
    public async Task Un_tick_envia_ambos_barridos()
    {
        var sender = new RecordingSender();
        var sp = new ServiceCollection().AddSingleton<ISender>(sender).BuildServiceProvider();
        var worker = new MantenimientoSesionesWorker(sp.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MantenimientoSesionesWorker>.Instance);

        await worker.EjecutarTickAsync(CancellationToken.None);

        Assert.Contains(sender.Enviados, r => r is BarrerIniciosAutomaticosCommand);
        Assert.Contains(sender.Enviados, r => r is BarrerTimeoutsCommand);
    }

    [Fact]
    public async Task Excepcion_en_un_tick_no_propaga()
    {
        var sp = new ServiceCollection().AddSingleton<ISender>(new ThrowingSender()).BuildServiceProvider();
        var worker = new MantenimientoSesionesWorker(sp.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MantenimientoSesionesWorker>.Instance);

        var ex = await Record.ExceptionAsync(() => worker.EjecutarTickAsync(CancellationToken.None));
        Assert.Null(ex); // el tick swallowea
    }

    private sealed class RecordingSender : ISender
    {
        public List<object> Enviados { get; } = new();
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        { Enviados.Add(request); return Task.FromResult(default(TResponse)!); }
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        { Enviados.Add(request); return Task.FromResult<object?>(null); }
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class ThrowingSender : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
```

> Si la versión de MediatR del repo define `ISender` con menos miembros (sin `CreateStream`), eliminar los métodos sobrantes del fake — solo `Send<TResponse>(IRequest<TResponse>, CT)` se ejerce. Verificar con el `ISender` referenciado.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~MantenimientoSesionesWorker"`
Expected: FAIL de compilación (worker no existe).

- [ ] **Step 3: Write minimal implementation**

Crear `Api/Configuration/MantenimientoOptions.cs`:

```csharp
namespace Umbral.OperacionesSesion.Api.Configuration;

public sealed class MantenimientoOptions
{
    public int IntervaloMs { get; set; } = 1000;
}
```

Crear `Api/Workers/MantenimientoSesionesWorker.cs`:

```csharp
using MediatR;
using Microsoft.Extensions.Options;
using Umbral.OperacionesSesion.Api.Configuration;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Api.Workers;

public sealed class MantenimientoSesionesWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MantenimientoSesionesWorker> _logger;
    private readonly int _intervaloMs;

    // ctor de runtime (con opciones)
    public MantenimientoSesionesWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<MantenimientoSesionesWorker> logger,
        IOptions<MantenimientoOptions> options)
        : this(scopeFactory, logger, options.Value.IntervaloMs) { }

    // ctor de test (intervalo por defecto)
    public MantenimientoSesionesWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<MantenimientoSesionesWorker> logger)
        : this(scopeFactory, logger, 1000) { }

    private MantenimientoSesionesWorker(
        IServiceScopeFactory scopeFactory, ILogger<MantenimientoSesionesWorker> logger, int intervaloMs)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _intervaloMs = intervaloMs <= 0 ? 1000 : intervaloMs;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_intervaloMs));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await EjecutarTickAsync(stoppingToken);
        }
    }

    internal async Task EjecutarTickAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new BarrerIniciosAutomaticosCommand(), cancellationToken);
            await sender.Send(new BarrerTimeoutsCommand(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tick de mantenimiento de sesiones falló; se reintenta al próximo tick.");
        }
    }
}
```

En `Program.cs`, tras `builder.Services.AddOperacionesSesionInfrastructure(builder.Configuration);` añadir:

```csharp
builder.Services.Configure<Umbral.OperacionesSesion.Api.Configuration.MantenimientoOptions>(
    builder.Configuration.GetSection("Mantenimiento"));
builder.Services.AddHostedService<Umbral.OperacionesSesion.Api.Workers.MantenimientoSesionesWorker>();
```

> El worker resuelve `ISender` en un scope por tick (los handlers/repos son `Scoped`). El `catch` en `EjecutarTickAsync` garantiza que el loop nunca muere. El intervalo viene de `Mantenimiento:IntervaloMs` (default 1000 ms).

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~MantenimientoSesionesWorker"`
Expected: PASS (2 tests).

- [ ] **Step 5: Suite completa**

Run: `dotnet test "services/operaciones-sesion"`
Expected: PASS. La WebApplicationFactory de los ContractTests arrancará ahora el HostedService — el `PeriodicTimer` (1 s) no debe interferir (sin sesiones, los barridos son no-op). Si algún ContractTest se vuelve flaky por el worker, añadir `Mantenimiento:IntervaloMs` alto en la config de test de la factory (documentar) — pero lo esperado es verde.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Configuration/MantenimientoOptions.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Workers/MantenimientoSesionesWorker.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/MantenimientoSesionesWorkerTests.cs
git commit -m "SP-3f-1 T6: MantenimientoSesionesWorker (BackgroundService, 2 barridos por tick) + DI

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: Middleware — `DbUpdateConcurrencyException` → 409

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/ExceptionHandlingMiddlewareConcurrencyTests.cs`

**Interfaces:**
- Consumes: `ExceptionHandlingMiddleware`.
- Produces: respuesta 409 cuando el pipeline lanza `DbUpdateConcurrencyException`.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.UnitTests/Api/ExceptionHandlingMiddlewareConcurrencyTests.cs
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Api.Middleware;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class ExceptionHandlingMiddlewareConcurrencyTests
{
    [Fact]
    public async Task DbUpdateConcurrencyException_se_mapea_a_409()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        var mw = new ExceptionHandlingMiddleware(
            _ => throw new DbUpdateConcurrencyException("conflict"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await mw.InvokeAsync(ctx);

        Assert.Equal(409, ctx.Response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~ExceptionHandlingMiddlewareConcurrency"`
Expected: FAIL (mapea a 500, no 409).

- [ ] **Step 3: Write minimal implementation**

En `ExceptionHandlingMiddleware.cs`:
1. Añadir `using Microsoft.EntityFrameworkCore;` arriba.
2. En el `switch` de `MapStatus`, añadir un brazo (p.ej. junto a `PartidasConfigInaccesibleException`):

```csharp
        DbUpdateConcurrencyException => HttpStatusCode.Conflict,
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~ExceptionHandlingMiddlewareConcurrency"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Middleware/ExceptionHandlingMiddleware.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/ExceptionHandlingMiddlewareConcurrencyTests.cs
git commit -m "SP-3f-1 T7: middleware mapea DbUpdateConcurrencyException -> 409

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 8: Contratos HTTP + traceability

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md`
- Modify (escribir, **NO** commitear): `docs/04-sdd/traceability-matrix.md`

**Interfaces:** documentación; sin código.

> CARVE-OUT GIT (vigente): escribir la fila SP-3f-1 en `traceability-matrix.md` pero **NO** stagearla/commitearla. El commit lleva SOLO `contracts/http/operaciones-sesion-api.md`.

- [ ] **Step 1: HTTP contract — documentar el 409 y los barridos internos**

En `contracts/http/operaciones-sesion-api.md`, añadir a las Notes (al final del párrafo existente) verbatim:

```
Concurrencia (SP-3f-1): `SesionPartida` usa token optimista (`xmin`). Los endpoints de runtime/inicio (responder pregunta, validar tesoro, avanzar pregunta/etapa, iniciar) pueden devolver `409 Conflict` cuando un barrido de fondo modifica la misma sesión en el instante de la petición; el cliente refetchea (`GET /mi-sesion`) y reintenta. Dos barridos de fondo (sin endpoint, dentro de Operaciones de Sesión) avanzan el estado por tiempo: inicio automático al cumplirse `TiempoInicio` (Lobby + Automatico/ManualYAutomatico) y cierre por timeout de la pregunta/etapa vencida del juego activo. Read/write internos; emiten los mismos eventos de dominio que el path request (No-Op por ahora).
```

- [ ] **Step 2: Traceability (escribir, NO commitear)**

Añadir fila a `docs/04-sdd/traceability-matrix.md` (tras la fila SP-3e):

```
| Concurrencia + barridos por tiempo (SP-3f-1) | Token optimista `xmin` en SesionPartida (condicional Npgsql) + un BackgroundService (MantenimientoSesionesWorker) que por tick dispara BarrerIniciosAutomaticos (Lobby+Automatico, now≥TiempoInicio → inicia/auto-cancela) y BarrerTimeouts (Iniciada con pregunta/etapa vencida → CerrarActividadVencida: cierra por tiempo, avanza, finaliza si último). Reúsa dominio existente; conflicto request → 409; eventos No-Op; Individual-only | Operaciones de Sesión | — (Puntuaciones SP-4; push SignalR SP-3f-2) | docs/superpowers/specs/2026-06-29-sp3f1-concurrencia-barridos-design.md · docs/superpowers/plans/2026-06-29-sp3f1-concurrencia-barridos.md | contracts/http/operaciones-sesion-api.md | Implemented — suite verde. **Fuente:** auto-inicio diferido SP-3b, timeout diferido SP-3c/3d, token watch-item. **Diferido:** Equipo→slice-E, push tiempo real→SP-3f-2, RabbitMQ real→slice propio, scoring→SP-4, enforcement xmin DB-level no integration-testeado (InMemory). |
```

- [ ] **Step 3: Run the FULL suite**

Run: `dotnet test "services/operaciones-sesion"`
Expected: PASS (docs-only; confirma que nada se rompió).

- [ ] **Step 4: Commit (solo el contrato HTTP)**

```bash
git add contracts/http/operaciones-sesion-api.md
git commit -m "SP-3f-1 T8: contrato HTTP (409 por concurrencia + barridos de fondo); fila traceability escrita sin commitear

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```
> `traceability-matrix.md` queda modificado+unstaged, uniéndose al squash pendiente del usuario.

> === TODAS las tareas SP-3f-1 completas. Next: review final whole-branch (opus) sobre el rango SP-3f-1, luego finishing-a-development-branch (decide el usuario). ===

---

## Self-Review (autor del plan)

**1. Cobertura del spec (§ por §):**
- §4.1 token xmin → T1 (condicional Npgsql + migración + test de modelo).
- §4.2 scans → T3 (interface + EF + fake + integration tests).
- §4.3 `CerrarActividadVencida` → T2 (dominio + 6 unit tests).
- §4.4 comandos/handlers → T4 (BarrerTimeouts) + T5 (BarrerIniciosAutomaticos).
- §4.5 worker → T6 (BackgroundService + opciones + DI + test).
- §4.6 middleware 409 → T7.
- §5 flujo → T6 (tick → 2 comandos) compone T4/T5.
- §6 errores → T4/T5 (catch por candidato), T6 (catch por tick), T7 (409 request).
- §7 testing → cubierto por tarea (dominio T2, handlers T4/T5, middleware T7, worker T6, gap xmin documentado en T1/§Global y en la fila traceability T8).
- §8 fronteras → Individual-only, eventos No-Op, sin SignalR/scoring respetados por construcción en todas las tareas.
- §9 artefactos → mapean 1:1 a T1–T8.

**2. Placeholder scan:** sin TBD/TODO. Cada step de código lleva código real. Las dos notas de fallback (guard `IsNpgsql` si InMemory rompe en T1; intervalo alto si ContractTests flaky en T6; recorte del fake `ISender` según versión MediatR en T6) son contingencias verificables con instrucción exacta, no placeholders de producción.

**3. Consistencia de tipos:**
- `CerrarActividadVencida(DateTime) → ResultadoCierreVencido` idéntico T2 (def) → T4 (uso).
- `GetSesionesConActividadVencidaAsync` / `GetSesionesAutoInicioPendienteAsync` (`DateTime, CT → Task<IReadOnlyList<SesionPartida>>`) idénticos interface/EF/fake (T3) → handlers (T4/T5).
- `BarrerTimeoutsCommand` / `BarrerIniciosAutomaticosCommand` (`: IRequest<int>`) idénticos T4/T5 (def) → T6 (worker uso).
- `MantenimientoOptions.IntervaloMs` (int, default 1000) idéntico T6.
- Event records (`PreguntaTriviaCerradaEvent` 7-args, `PreguntaTriviaActivadaEvent` 7-args, `EtapaBDTCerradaEvent` 7-args, `EtapaBDTActivadaEvent` 7-args, `JuegoActivadoEvent` 5-args, `PartidaFinalizadaEvent` 3-args) y helpers estáticos (`PublicarPreguntaActivadaSiTriviaAsync`, `PublicarEtapaActivadaSiBdtAsync`, `PublicarEventosInicioAsync`) copiados verbatim de los handlers existentes verificados (AvanzarPregunta/AvanzarEtapa/Finalizar/IntentarInicioAutomatico/IniciarPartida).
- IDs vía `.Valor`; enums vía `.ToString()`.

**Nota de orden:** T1/T2/T3 independientes entre sí; T4 requiere T2+T3; T5 requiere T3; T6 requiere T4+T5; T7 independiente; T8 docs al final. Ejecutar en orden T1→T8 garantiza compilación verde en cada commit.
