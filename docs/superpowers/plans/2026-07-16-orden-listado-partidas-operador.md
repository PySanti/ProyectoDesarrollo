# Orden del listado de partidas del operador — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que `GET /partidas` devuelva las partidas ordenadas por fecha de creación descendente (la última creada primero), con esa fecha visible en la tabla del operador.

**Architecture:** `Partida` gana un campo obligatorio `FechaCreacion` que recibe **como parámetro** (el dominio nunca lee el reloj); `CrearPartidaCommandHandler` inyecta `TimeProvider` y se lo pasa. El `ORDER BY` vive en `PartidaRepository.ListAsync`, no en el handler. El campo se expone solo en el listado (`PartidaSummaryDto`), no en el detalle.

**Tech Stack:** .NET 8, EF Core + Npgsql, MediatR, xUnit. Frontend: React 18 + TypeScript + Vite, vitest.

**Spec:** `docs/superpowers/specs/2026-07-16-orden-listado-partidas-operador-design.md`

## Global Constraints

- `FechaCreacion` es **obligatorio** (`DateTime`, **no** nullable). La data existente es descartable.
- **El dominio no lee el reloj.** La fecha entra como parámetro, siempre el **último**, por paridad con Operaciones (`Inscribir(..., fecha)`, `Cancelar(now)`). Prohibido `DateTime.UtcNow` / `DateTime.Now` dentro de `Partida`.
- El `ORDER BY` va en el **repositorio**, no en el handler.
- Filas migradas: centinela **`'0001-01-01 00:00:00+00'`** vía `defaultValueSql`. **Nunca `now()`** — dejaría las partidas viejas fechadas hoy y al tope de la lista.
- Se guarda **y se muestra el instante completo**: `toLocaleString()`, **no** `toLocaleDateString()`.
- **`GET /partidas/{id}` no cambia.** El campo se expone solo en el listado.
- **Cero cambios** en Operaciones de Sesión, Puntuaciones, Identity, gateway y móvil.
- No tocar `data-testid`, `label` ni roles ARIA existentes (regla de la reconstrucción visual).
- Convención de columnas de este servicio: minúsculas sin separadores (`nombrepartida`, `tiempoinicio`, `modoinicio`) → **`fechacreacion`**.

---

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `services/partidas/src/Umbral.Partidas.Domain/Entities/Partida.cs` | **Modificar** — campo `FechaCreacion` + parámetro en `Crear`/ctor; borrar comentario stale de la línea 13 |
| `services/partidas/src/Umbral.Partidas.Application/Handlers/Commands/CrearPartidaCommandHandler.cs` | **Modificar** — inyecta `TimeProvider`, pasa el instante al dominio |
| `services/partidas/src/Umbral.Partidas.Application/DependencyInjection.cs` | **Modificar** — registra `TimeProvider.System` |
| `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/PartidasDbContext.cs` | **Modificar** — mapeo de la columna `fechacreacion` |
| `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/Migrations/*_AddFechaCreacionAPartida.cs` | **Crear** (generada) — `AddColumn` con el centinela |
| `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/PartidaRepository.cs` | **Modificar** — `ListAsync` ordena |
| `services/partidas/src/Umbral.Partidas.Application/DTOs/PartidaSummaryDto.cs` | **Modificar** — `FechaCreacion` al final del record |
| `services/partidas/src/Umbral.Partidas.Application/Handlers/Queries/ListPartidasQueryHandler.cs` | **Modificar** — mapea el campo nuevo |
| `services/partidas/tests/Umbral.Partidas.UnitTests/Application/Fakes/FakeTimeProvider.cs` | **Crear** — copia del de Operaciones |
| `frontend/src/api/partidasApi.ts` | **Modificar** — `PartidaSummary.fechaCreacion` |
| `frontend/src/features/partidas/PartidasListPage.tsx` | **Modificar** — columna "Creada" |

**Aviso de churn (Task 1):** cambiar la firma de `Partida.Crear` rompe **15 llamadores en 8 archivos** — 1 de producción y 14 en tests:

| Archivo | Llamadas |
|---|---|
| `src/.../Handlers/Commands/CrearPartidaCommandHandler.cs` | 1 |
| `tests/.../UnitTests/Domain/PartidaTests.cs` | 7 |
| `tests/.../IntegrationTests/PartidaRepositoryTests.cs` | 2 |
| `tests/.../IntegrationTests/PartidaPersistenceTests.cs` | 1 |
| `tests/.../UnitTests/Application/AgregarJuegoBDTCommandHandlerTests.cs` | 1 |
| `tests/.../UnitTests/Application/AgregarJuegoTriviaCommandHandlerTests.cs` | 1 |
| `tests/.../UnitTests/Application/ListPartidasQueryHandlerTests.cs` | 1 |
| `tests/.../UnitTests/Application/GetPartidaByIdQueryHandlerTests.cs` | 1 |

Es mecánico (añadir un argumento de fecha), pero **el ciclo rojo de este task es un error de compilación, no una aserción fallida** — C# no permite otra cosa al cambiar una firma. Está previsto; no es señal de que algo vaya mal.

**Cuidado con el regex:** buscar `Partida\.Crear\(` también matchea `NombrePartida.Crear(`, que **no** hay que tocar. Usar `(^|[^A-Za-z])Partida\.Crear\(`.

---

### Task 1: `FechaCreacion` en el dominio + reloj inyectado

**Files:**
- Create: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/Fakes/FakeTimeProvider.cs`
- Modify: `services/partidas/src/Umbral.Partidas.Domain/Entities/Partida.cs:13,24-52`
- Modify: `services/partidas/src/Umbral.Partidas.Application/Handlers/Commands/CrearPartidaCommandHandler.cs`
- Modify: `services/partidas/src/Umbral.Partidas.Application/DependencyInjection.cs:9-15`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Domain/PartidaTests.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/CrearPartidaCommandHandlerTests.cs`
- Modify (churn): los 6 archivos de test restantes de la tabla de arriba

**Interfaces:**
- Produces: `Partida.FechaCreacion` (`DateTime`, get privado) y
  `Partida.Crear(NombrePartida nombre, Modalidad modalidad, ModoInicioPartida modo, DateTime? tiempoInicio, int minimos, int maximos, DateTime fechaCreacion)` — la fecha es el **7º y último** parámetro.
- Produces: `CrearPartidaCommandHandler(IPartidaRepository partidas, IPartidasUnitOfWork unitOfWork, TimeProvider timeProvider)`.
- Produces: `FakeTimeProvider(DateTime utcNow)` en `Umbral.Partidas.UnitTests.Application.Fakes`.
- Consumes: nada de tasks anteriores.

- [ ] **Step 1: Crear el `FakeTimeProvider` de tests**

Copia literal del de Operaciones (`services/operaciones-sesion/tests/.../Fakes/FakeTimeProvider.cs`), cambiando solo el namespace. El csproj de UnitTests **no** trae `Microsoft.Extensions.TimeProvider.Testing` y no hace falta añadirlo: `TimeProvider` es abstracto y está en el BCL de .NET 8.

Crear `services/partidas/tests/Umbral.Partidas.UnitTests/Application/Fakes/FakeTimeProvider.cs`:

```csharp
using System;

namespace Umbral.Partidas.UnitTests.Application.Fakes;

public sealed class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FakeTimeProvider(DateTime utcNow)
        => _now = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

    public override DateTimeOffset GetUtcNow() => _now;
}
```

- [ ] **Step 2: Escribir el test de dominio que falla**

Añadir a `services/partidas/tests/Umbral.Partidas.UnitTests/Domain/PartidaTests.cs`:

```csharp
[Fact]
public void Crear_guarda_la_fecha_de_creacion_que_recibe()
{
    var t0 = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    var partida = Partida.Crear(
        NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, t0);

    // El dominio no lee el reloj: guarda el instante que le pasan. Eso es lo que hace
    // deterministas al test de orden (Task 3) y al del handler (Step 6).
    Assert.Equal(t0, partida.FechaCreacion);
}
```

- [ ] **Step 3: Correr el test y verificar que falla**

Run: `dotnet build "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj"`

Expected: FAIL con `error CS1501: No overload for method 'Crear' takes 7 arguments`.

Es el rojo esperado: un error de compilación. Ver "Aviso de churn" arriba.

- [ ] **Step 4: Implementar el dominio**

En `Partida.cs`, línea 13 — **borrar el comentario stale**. Dice `// null = configured, not yet published (SP-3 sets Lobby)` y es falso: ADR-0010 movió el estado de runtime a Operaciones de Sesión y **nada en este servicio vuelve a escribir `Estado`**. Dejarlo hace creer que la columna Estado funciona. Sustituir por:

```csharp
    // Siempre null: ADR-0010 dejó el estado de runtime en Operaciones de Sesión y este
    // servicio nunca lo escribe. La columna "Estado" del listado web es un problema
    // abierto, fuera del alcance de este slice (ver el spec § Alcance).
    public EstadoPartida? Estado { get; private set; }
```

Añadir la propiedad después de `MaximosParticipacion` (línea 18):

```csharp
    public DateTime FechaCreacion { get; private set; }
```

Cambiar el ctor privado (líneas 24-43) — añadir el parámetro al final y la asignación:

```csharp
    private Partida(
        NombrePartida nombre,
        Modalidad modalidad,
        ModoInicioPartida modo,
        DateTime? tiempoInicio,
        int minimos,
        int maximos,
        DateTime fechaCreacion)
    {
        PartidaId = PartidaId.New();
        NombrePartida = nombre;
        Modalidad = modalidad;
        ModoInicioPartida = modo;
        TiempoInicio = tiempoInicio;
        MinimosParticipacion = minimos;
        MaximosParticipacion = maximos;
        FechaCreacion = fechaCreacion;
        Estado = null;

        ValidarParametrosParticipacion();
        ValidarParametrosInicio();
    }
```

Y la factory (líneas 45-52):

```csharp
    public static Partida Crear(
        NombrePartida nombre,
        Modalidad modalidad,
        ModoInicioPartida modo,
        DateTime? tiempoInicio,
        int minimos,
        int maximos,
        DateTime fechaCreacion)
        => new(nombre, modalidad, modo, tiempoInicio, minimos, maximos, fechaCreacion);
```

- [ ] **Step 5: Cablear el reloj en el handler y el DI**

`CrearPartidaCommandHandler.cs` completo:

```csharp
using MediatR;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Application.Handlers.Commands;

public sealed class CrearPartidaCommandHandler : IRequestHandler<CrearPartidaCommand, CrearPartidaResponse>
{
    private readonly IPartidaRepository _partidas;
    private readonly IPartidasUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CrearPartidaCommandHandler(
        IPartidaRepository partidas, IPartidasUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _partidas = partidas;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<CrearPartidaResponse> Handle(CrearPartidaCommand request, CancellationToken cancellationToken)
    {
        var partida = Partida.Crear(
            NombrePartida.Crear(request.NombrePartida),
            request.Modalidad,
            request.ModoInicioPartida,
            request.TiempoInicio,
            request.MinimosParticipacion,
            request.MaximosParticipacion,
            _timeProvider.GetUtcNow().UtcDateTime);

        _partidas.Add(partida);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CrearPartidaResponse(partida.PartidaId.Valor);
    }
}
```

En `DependencyInjection.cs`, añadir la línea que Operaciones ya tiene (este servicio no tenía reloj):

```csharp
    public static IServiceCollection AddPartidasApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton(TimeProvider.System);
        return services;
    }
```

- [ ] **Step 6: Arreglar los 14 llamadores de test**

Localizar con: `rg "(^|[^A-Za-z])Partida\.Crear\(" services/partidas/tests`

A cada llamada, añadir un 7º argumento de fecha. Para los tests que no se ocupan del tiempo, usar una constante local por archivo:

```csharp
private static readonly DateTime T0 = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);
```

y pasar `T0`. Ejemplo, en `PartidaRepositoryTests.cs:23`:

```csharp
var partida = Partida.Crear(
    NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, T0);
```

En `CrearPartidaCommandHandlerTests.cs:18`, el handler gana un tercer argumento:

```csharp
var handler = new CrearPartidaCommandHandler(repo, uow, new FakeTimeProvider(T0));
```

- [ ] **Step 7: Escribir el test del handler**

Añadir a `CrearPartidaCommandHandlerTests.cs` (requiere `using Umbral.Partidas.UnitTests.Application.Fakes;`, que ya está en la línea 7):

```csharp
[Fact]
public async Task Handle_toma_la_fecha_de_creacion_del_reloj_inyectado()
{
    var t0 = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);
    var repo = new FakePartidaRepository();
    var handler = new CrearPartidaCommandHandler(repo, new FakePartidasUnitOfWork(), new FakeTimeProvider(t0));
    var command = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

    var response = await handler.Handle(command, CancellationToken.None);

    // Fija el instante exacto. Con DateTime.UtcNow ambiente esto solo podría aseverar
    // "cerca de ahora", que es la razón de que el reloj se inyecte.
    Assert.Equal(t0, repo.Store[response.PartidaId].FechaCreacion);
}
```

- [ ] **Step 8: Correr la suite completa y verificar verde**

Run: `dotnet test "services/partidas/Umbral.Partidas.sln" --nologo`

Expected: PASS, 0 fallos. Anotar la línea base de conteos para comparar en tasks siguientes.

- [ ] **Step 9: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Domain/Entities/Partida.cs \
        services/partidas/src/Umbral.Partidas.Application/Handlers/Commands/CrearPartidaCommandHandler.cs \
        services/partidas/src/Umbral.Partidas.Application/DependencyInjection.cs \
        services/partidas/tests/Umbral.Partidas.UnitTests
git commit -m "feat(partidas): la partida registra cuando se creo

El dominio recibe el instante como parametro; el handler lo toma de un
TimeProvider inyectado (patron de Operaciones). Partidas no tenia reloj.

Borra el comentario stale de Partida.cs que decia que SP-3 escribe el
Estado: ADR-0010 lo movio a Operaciones y aqui nunca se escribe.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Persistencia de `fechacreacion` + migración

**Files:**
- Modify: `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/PartidasDbContext.cs:42`
- Create: `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/Migrations/<timestamp>_AddFechaCreacionAPartida.cs` (generada)
- Test: `services/partidas/tests/Umbral.Partidas.IntegrationTests/PartidaPersistenceTests.cs`

**Interfaces:**
- Consumes: `Partida.FechaCreacion` y `Partida.Crear(..., fechaCreacion)` (Task 1).
- Produces: columna `partidas.fechacreacion`, `timestamp with time zone`, `NOT NULL`, default `'0001-01-01 00:00:00+00'`.

- [ ] **Step 1: Escribir el test de round-trip que falla**

Añadir a `PartidaPersistenceTests.cs` (usa `UseInMemoryDatabase`, igual que el resto del suite):

```csharp
[Fact]
public async Task FechaCreacion_sobrevive_el_round_trip()
{
    var dbName = Guid.NewGuid().ToString();
    var t0 = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);
    var partida = Partida.Crear(
        NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, t0);

    await using (var ctx = NewContext(dbName))
    {
        new PartidaRepository(ctx).Add(partida);
        await new PartidasUnitOfWork(ctx).SaveChangesAsync(CancellationToken.None);
    }

    await using (var ctx = NewContext(dbName))
    {
        var loaded = await new PartidaRepository(ctx).GetByIdAsync(partida.PartidaId, CancellationToken.None);
        Assert.Equal(t0, loaded!.FechaCreacion);
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.IntegrationTests/Umbral.Partidas.IntegrationTests.csproj" --filter FechaCreacion_sobrevive_el_round_trip`

Expected: FAIL. Sin mapeo explícito EF puede inferir la propiedad por convención con el nombre `FechaCreacion`, así que el fallo puede venir del nombre de columna, no de la ausencia del dato. Si pasa a la primera, **no saltar el Step 3**: el mapeo explícito hace falta igual para fijar el nombre `fechacreacion` (convención de este servicio) y `IsRequired()`.

- [ ] **Step 3: Mapear la columna**

En `PartidasDbContext.cs`, añadir después de la línea 42 (`maximos`), antes del `HasMany`:

```csharp
            entity.Property(x => x.FechaCreacion).HasColumnName("fechacreacion").IsRequired();
```

- [ ] **Step 4: Generar la migración**

Run:

```bash
dotnet ef migrations add AddFechaCreacionAPartida \
  --project services/partidas/src/Umbral.Partidas.Infrastructure \
  --startup-project services/partidas/src/Umbral.Partidas.Api
```

No necesita base viva: `migrations add` solo lee el modelo, y existe `OperacionesSesionDbContextDesignTimeFactory`-equivalente en Partidas.

- [ ] **Step 5: Corregir el default de la migración generada**

EF genera un `defaultValue` con un `DateTime` de `Kind=Unspecified`, que **Npgsql rechaza** contra `timestamp with time zone`. Sustituirlo por SQL crudo. En el archivo generado, la llamada debe quedar:

```csharp
migrationBuilder.AddColumn<DateTime>(
    name: "fechacreacion",
    table: "partidas",
    type: "timestamp with time zone",
    nullable: false,
    defaultValueSql: "'0001-01-01 00:00:00+00'");
```

**No usar `now()`.** Con `now()` las partidas existentes quedarían fechadas hoy y **al tope de la lista** — el lugar reservado a lo último creado. Con el centinela quedan al fondo y muestran una fecha obviamente falsa que nadie se cree.

- [ ] **Step 6: Correr la suite y verificar verde**

Run: `dotnet test "services/partidas/Umbral.Partidas.sln" --nologo`

Expected: PASS, 0 fallos.

- [ ] **Step 7: Verificar la migración contra la base real (manual)**

**Ningún test ejercita las migraciones** — este paso no es opcional, es la única verificación que tiene.

```bash
docker compose -f "infra/docker-compose.yml" up -d postgres
dotnet ef database update \
  --project services/partidas/src/Umbral.Partidas.Infrastructure \
  --startup-project services/partidas/src/Umbral.Partidas.Api
docker exec -it umbral-postgres psql -U umbral -d umbral_partidas -c "\d partidas"
```

Expected: la columna `fechacreacion` aparece como `timestamp with time zone`, `not null`, default `'0001-01-01 00:00:00+00'`.

- [ ] **Step 8: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Infrastructure services/partidas/tests/Umbral.Partidas.IntegrationTests
git commit -m "feat(partidas): persiste fechacreacion

Las filas existentes reciben un centinela y no now(): con now() quedarian
fechadas hoy y al tope del listado, silenciosamente equivocadas.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Orden en el repositorio

Es el task que arregla el bug reportado. Los demás lo sostienen.

**Files:**
- Modify: `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/PartidaRepository.cs:23-24`
- Test: `services/partidas/tests/Umbral.Partidas.IntegrationTests/PartidaRepositoryTests.cs`

**Interfaces:**
- Consumes: `Partida.FechaCreacion` (Task 1), columna mapeada (Task 2).
- Produces: `IPartidaRepository.ListAsync` garantiza orden `FechaCreacion DESC, PartidaId ASC`. La firma **no cambia**.

- [ ] **Step 1: Escribir el test de orden que falla**

Añadir a `PartidaRepositoryTests.cs`:

```csharp
[Fact]
public async Task ListAsync_devuelve_la_ultima_creada_primero()
{
    var dbName = Guid.NewGuid().ToString();
    var t0 = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    // Se insertan desordenadas a proposito: el orden no debe depender del insert.
    var media = Partida.Crear(
        NombrePartida.Crear("Media"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, t0.AddHours(1));
    var vieja = Partida.Crear(
        NombrePartida.Crear("Vieja"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, t0);
    var nueva = Partida.Crear(
        NombrePartida.Crear("Nueva"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, t0.AddHours(2));

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
```

Requiere `using System.Linq;` en el archivo.

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.IntegrationTests/Umbral.Partidas.IntegrationTests.csproj" --filter ListAsync_devuelve_la_ultima_creada_primero`

Expected: FAIL con las partidas en orden de inserción (`Media, Vieja, Nueva`).

Si por casualidad pasara, **no dar el task por bueno**: sin `ORDER BY` el orden es el que quiera el proveedor y coincidiría por accidente. Confirmar que el Step 3 está aplicado antes de seguir.

- [ ] **Step 3: Implementar el orden**

En `PartidaRepository.cs`, sustituir `ListAsync` (líneas 23-24):

```csharp
    // El ORDER BY vive aqui y no en el handler: es trabajo de la base y sobrevive a una
    // futura paginacion. El desempate por PartidaId no es paranoia: con precision de
    // microsegundos dos partidas reales nunca empatan, pero un reloj falso en tests
    // devuelve el mismo instante y sin ThenBy el orden seria indefinido e intermitente.
    public async Task<IReadOnlyList<Partida>> ListAsync(CancellationToken cancellationToken)
        => await _dbContext.Partidas
            .Include(p => p.Juegos)
            .OrderByDescending(p => p.FechaCreacion)
            .ThenBy(p => p.PartidaId)
            .ToListAsync(cancellationToken);
```

- [ ] **Step 4: Correr y verificar que pasa**

Run: `dotnet test "services/partidas/Umbral.Partidas.sln" --nologo`

Expected: PASS, 0 fallos.

- [ ] **Step 5: Verificar que el test muerde**

Quitar temporalmente el `.OrderByDescending(...).ThenBy(...)` y correr solo ese test.

Expected: FAIL. Si pasa, el test no prueba nada — arreglarlo antes de seguir. Restaurar el orden después.

- [ ] **Step 6: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/PartidaRepository.cs \
        services/partidas/tests/Umbral.Partidas.IntegrationTests/PartidaRepositoryTests.cs
git commit -m "fix(partidas): el listado devuelve la ultima partida creada primero

ListAsync no tenia ORDER BY: Postgres devolvia las filas en el orden fisico
que le conviniera, que ademas cambia cuando una fila se actualiza.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Exponer `fechaCreacion` en el contrato

**Files:**
- Modify: `services/partidas/src/Umbral.Partidas.Application/DTOs/PartidaSummaryDto.cs`
- Modify: `services/partidas/src/Umbral.Partidas.Application/Handlers/Queries/ListPartidasQueryHandler.cs:21-32`
- Modify: `contracts/http/partidas-config.md:112-118`
- Test: `services/partidas/tests/Umbral.Partidas.ContractTests/` (el archivo que cubre `GET /partidas`)

**Interfaces:**
- Consumes: `Partida.FechaCreacion` (Task 1), orden garantizado (Task 3).
- Produces: `PartidaSummaryDto.FechaCreacion` (`DateTime`, **último** parámetro posicional) → JSON `fechaCreacion`.

- [ ] **Step 1: Añadir el campo al DTO**

`FechaCreacion` va **al final** del record: es posicional y ponerlo en medio rompería toda construcción existente sin que el compilador señale el sitio correcto.

```csharp
namespace Umbral.Partidas.Application.DTOs;

public sealed record PartidaSummaryDto(
    Guid PartidaId,
    string NombrePartida,
    string Modalidad,
    string ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    string? Estado,
    int CantidadJuegos,
    DateTime FechaCreacion);
```

- [ ] **Step 2: Mapearlo en el handler**

En `ListPartidasQueryHandler.cs`, añadir el argumento al final del `Select`. El handler **no ordena**: el repositorio ya entrega ordenado.

```csharp
        return partidas
            .Select(p => new PartidaSummaryDto(
                p.PartidaId.Valor,
                p.NombrePartida.Valor,
                p.Modalidad.ToString(),
                p.ModoInicioPartida.ToString(),
                p.TiempoInicio,
                p.MinimosParticipacion,
                p.MaximosParticipacion,
                p.Estado?.ToString(),
                p.Juegos.Count,
                p.FechaCreacion))
            .ToList();
```

- [ ] **Step 3: Escribir el contract test**

Va en `services/partidas/tests/Umbral.Partidas.ContractTests/PartidasConfigEndpointsTests.cs`, que ya
lista con `GetFromJsonAsync<List<PartidaSummaryDto>>("/partidas")` (línea 76). Añadir un `[Fact]`
propio en vez de ampliar el existente: prueba una garantía distinta (la forma del listado), y el de
la línea 76 prueba el alta completa.

```csharp
[Fact]
public async Task List_partidas_expone_fechaCreacion()
{
    var body = new
    {
        nombrePartida = "Copa fechada",
        modalidad = "Individual",
        modoInicioPartida = "Manual",
        tiempoInicio = (DateTime?)null,
        minimosParticipacion = 1,
        maximosParticipacion = 10
    };
    var create = await _client.PostAsJsonAsync("/partidas", body);
    Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    var created = await create.Content.ReadFromJsonAsync<CrearPartidaResponse>();

    var list = await _client.GetFromJsonAsync<List<PartidaSummaryDto>>("/partidas");

    var partida = Assert.Single(list!, p => p.PartidaId == created!.PartidaId);
    // No basta con que el campo exista: si el handler no lo mapeara vendria default,
    // y el test pasaria igual aseverando solo la forma.
    Assert.NotEqual(default, partida.FechaCreacion);
}
```

Ajustar el shape del `body` al que ya usa el archivo para `POST /partidas` si difiere (mirar el
primer `[Fact]`), y añadir los `using` que falten (`System.Net.Http.Json`, y el namespace de
`CrearPartidaResponse`).

- [ ] **Step 4: Correr la suite y verificar verde**

Run: `dotnet test "services/partidas/Umbral.Partidas.sln" --nologo`

Expected: PASS, 0 fallos.

- [ ] **Step 5: Actualizar el contrato**

En `contracts/http/partidas-config.md`, sección `## GET /partidas` (líneas 112-118), sustituir por:

````markdown
## GET /partidas
List partida summaries.

**Orden garantizado:** `fechaCreacion` descendente (la última creada primero), desempate por
`partidaId` ascendente. Lo aplica `PartidaRepository.ListAsync`; el cliente **no** debe reordenar.

Response `200 OK`:
```json
[ { "partidaId": "<guid>", "nombrePartida": "Copa UMBRAL", "modalidad": "Individual", "modoInicioPartida": "Manual", "tiempoInicio": null, "minimosParticipacion": 1, "maximosParticipacion": 10, "estado": null, "cantidadJuegos": 2, "fechaCreacion": "2026-07-16T12:00:00Z" } ]
```

- `fechaCreacion` es el instante completo (fecha **y** hora), en UTC. Lo escribe el servicio al crear
  la partida, desde un `TimeProvider` inyectado.
- **`GET /partidas/{id}` no lo expone**: el campo existe solo donde sirve, que es explicar el orden
  del listado.
- Las partidas anteriores a la migración `AddFechaCreacionAPartida` traen el centinela
  `0001-01-01T00:00:00Z` y quedan al final. Es deliberado: dice "no se sabe" en vez de mentir.
- **`estado` es siempre `null`** — ver la nota de estado más abajo.
````

- [ ] **Step 6: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Application services/partidas/tests/Umbral.Partidas.ContractTests contracts/http/partidas-config.md
git commit -m "feat(partidas): GET /partidas expone fechaCreacion y garantiza el orden

El contrato no prometia orden alguno; por eso nadie lo implemento y nadie
lo noto hasta que el operador miro la pantalla.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Columna "Creada" en el listado web

**Files:**
- Modify: `frontend/src/api/partidasApi.ts:54-64`
- Modify: `frontend/src/features/partidas/PartidasListPage.tsx:86-120`
- Test: `frontend/src/features/partidas/PartidasListPage.test.tsx`

**Interfaces:**
- Consumes: `fechaCreacion` en la respuesta de `GET /partidas` (Task 4).
- Produces: nada que otros tasks consuman.

- [ ] **Step 1: Añadir el campo al tipo**

En `partidasApi.ts`, `PartidaSummary`:

```typescript
export interface PartidaSummary {
  partidaId: string;
  nombrePartida: string;
  modalidad: Modalidad;
  modoInicioPartida: ModoInicioPartida;
  tiempoInicio: string | null;
  minimosParticipacion: number;
  maximosParticipacion: number;
  estado: string | null;
  cantidadJuegos: number;
  fechaCreacion: string;
}
```

- [ ] **Step 2: Arreglar las fixtures existentes (el typecheck las rompe)**

`PartidasListPage.test.tsx:22` y `:34` declaran `summaryPublicada` y `summarySinPublicar` con tipo
explícito `PartidaSummary`. Al volverse `fechaCreacion` obligatorio, **ambas dejan de compilar**.
Añadir el campo a las dos:

```typescript
const summaryPublicada: PartidaSummary = {
  partidaId: "p1",
  nombrePartida: "Trivia de verano",
  modalidad: "Individual",
  modoInicioPartida: "Manual",
  tiempoInicio: null,
  minimosParticipacion: 1,
  maximosParticipacion: 10,
  estado: "Lobby",
  cantidadJuegos: 2,
  fechaCreacion: "2026-07-16T12:00:00Z"
};

const summarySinPublicar: PartidaSummary = {
  partidaId: "p2",
  nombrePartida: "BDT campus",
  modalidad: "Equipo",
  modoInicioPartida: "Automatico",
  tiempoInicio: "2026-08-01T10:00:00Z",
  minimosParticipacion: 2,
  maximosParticipacion: 6,
  estado: null,
  cantidadJuegos: 1,
  fechaCreacion: "2026-07-16T09:00:00Z"
};
```

Ojo con las fechas elegidas: `summaryPublicada` (12:00) es **más nueva** que `summarySinPublicar`
(09:00), y el mock de la línea 52 las devuelve en ese orden — coherente con lo que entregaría el
backend ordenado. El test de orden del Step 3 se apoya en eso.

- [ ] **Step 3: Escribir los tests que fallan**

Añadir dentro del `describe("PartidasListPage", ...)`:

```typescript
it("muestra la fecha y hora de creacion de cada partida", async () => {
  getPartidasMock.mockResolvedValueOnce([summaryPublicada]);
  renderPage();

  const fila = await screen.findByTestId("fila-partida-p1");
  // toLocaleString, no toLocaleDateString: sin la hora el operador ve varias partidas
  // del mismo dia y el orden vuelve a parecer arbitrario.
  expect(fila).toHaveTextContent(new Date("2026-07-16T12:00:00Z").toLocaleString());
});

it("respeta el orden que llega del backend y no reordena", async () => {
  getPartidasMock.mockResolvedValueOnce([summaryPublicada, summarySinPublicar]);
  renderPage();

  await screen.findByTestId("fila-partida-p1");
  const filas = screen.getAllByTestId(/^fila-partida-/);
  expect(filas[0]).toHaveTextContent("Trivia de verano");
  expect(filas[1]).toHaveTextContent("BDT campus");
});
```

El segundo test fija que el orden es responsabilidad del backend. Sin él, alguien podría "arreglar"
el orden en el cliente y quedarían dos implementaciones de la misma regla.

`new Date(...).toLocaleString()` en el test usa el mismo locale y zona del runner que el componente,
así que la comparación es estable sin fijar `TZ`.

- [ ] **Step 4: Correr y verificar que falla**

Run: `cd frontend && npx vitest run src/features/partidas/PartidasListPage.test.tsx`

Expected: FAIL — la fecha no está en el DOM. (El test de orden puede pasar ya, porque el componente
pinta en el orden recibido; su valor es de regresión: impide que alguien añada un sort en cliente.)

- [ ] **Step 5: Añadir la columna**

En `PartidasListPage.tsx`, la cabecera (líneas 88-94) — la columna va **después de "Nombre"**, no al final: la tabla se lee de izquierda a derecha y el criterio que explica el orden debe caer donde va la vista.

```tsx
                <tr>
                  <th scope="col">Nombre</th>
                  <th scope="col">Creada</th>
                  <th scope="col">Modalidad</th>
                  <th scope="col">Modo de inicio</th>
                  <th scope="col">Juegos</th>
                  <th scope="col">Estado</th>
                </tr>
```

Y la celda, justo después del `<td>` del nombre (línea 109):

```tsx
                      <td>{new Date(partida.fechaCreacion).toLocaleString()}</td>
```

`toLocaleString()`, **no** `toLocaleDateString()`: pinta fecha y hora. Es lo que ya hacen `HistorialPartidaPage` y `RendimientoEquipoPage`.

- [ ] **Step 6: Correr los tests y el typecheck**

Run: `cd frontend && npm test && npx tsc --noEmit`

Expected: PASS, 0 fallos, typecheck limpio. Los **assertions** de los tests existentes de
`PartidasListPage` deben seguir verdes sin tocarlas: añadir una columna no cambia `data-testid`,
`label` ni roles ARIA. Lo único que cambia en ese archivo son las dos fixtures del Step 2, y por el
tipo, no por el comportamiento.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/api/partidasApi.ts frontend/src/features/partidas/PartidasListPage.tsx frontend/src/features/partidas/PartidasListPage.test.tsx
git commit -m "feat(web): columna Creada en el listado de partidas

Va despues de Nombre y muestra fecha y hora: con solo el dia, el operador ve
varias partidas de hoy y el orden vuelve a parecer arbitrario.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Cierre documental

**Files:**
- Modify: `docs/04-sdd/SPECS-LIST.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: todo lo anterior.
- Produces: nada.

- [ ] **Step 1: Verificar el alcance real del diff**

Run: `git diff --stat master...HEAD -- services frontend`

Expected: **solo** `services/partidas` y `frontend`. Si aparece Operaciones, Puntuaciones, Identity, gateway o `mobile/`, se violó una restricción global — parar y entender por qué.

- [ ] **Step 2: Añadir la fila a `SPECS-LIST.md`**

Al final de la tabla:

```markdown
| Orden del listado de partidas del operador | Partidas | web | Operador | docs/superpowers/specs/2026-07-16-orden-listado-partidas-operador-design.md | Implemented (6 tasks). `GET /partidas` no tenía `ORDER BY` y el modelo no tenía por dónde ordenar: `Partida` no guardaba fecha de creación. Añade `FechaCreacion` (obligatorio, vía `TimeProvider` inyectado; el dominio recibe el instante como parámetro), `ORDER BY FechaCreacion DESC, PartidaId ASC` en el repositorio, y la columna "Creada" en la tabla del operador. Primer reloj del servicio Partidas. **Diferido:** el panel del participante tiene el mismo bug pero merece otro criterio (slice propio); la columna "Estado" está muerta desde ADR-0010 (decisión de arquitectura, ADR propio). |
```

- [ ] **Step 3: Añadir la fila a `traceability-matrix.md`**

Al final de la tabla, con los conteos reales de la suite (los anotados en Task 1 Step 8 como línea base vs. el final), la verificación de mutación de Task 3 Step 5, y las dos limitaciones declaradas del spec: el `ORDER BY` se prueba sobre **InMemory** (ordenamiento LINQ, no SQL real) y el `defaultValueSql` **no lo prueba ningún test** (verificado a mano en Task 2 Step 7).

- [ ] **Step 4: Commit**

```bash
git add docs/04-sdd/SPECS-LIST.md docs/04-sdd/traceability-matrix.md
git commit -m "docs: trazabilidad del orden del listado de partidas

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Verificación final

- `dotnet test "services/partidas/Umbral.Partidas.sln" --nologo` verde.
- `cd frontend && npm test && npx tsc --noEmit` verdes.
- La migración aplicada contra Postgres local y la columna inspeccionada con `\d partidas` (Task 2 Step 7) — **ningún test cubre esto**.
- `git diff --stat` solo toca `services/partidas`, `frontend`, `contracts/http/partidas-config.md` y `docs/`.
- Prueba manual: crear dos partidas seguidas en el web y confirmar que la segunda aparece primera, con hora visible.
