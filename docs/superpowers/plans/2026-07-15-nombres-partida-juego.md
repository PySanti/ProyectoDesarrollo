# Nombres de partida y de juego en las pantallas — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que las tres pantallas que aún muestran `partidaId`/`juegoId` como GUID muestren el nombre de la partida y una etiqueta legible del juego.

**Architecture:** Dos cambios aditivos de backend, cada uno en el servicio que ya tiene el dato: Puntuaciones une `JuegoProyectado` para devolver `juegoOrden`/`tipoJuego` en el historial, y Operaciones proyecta `SesionPartida.Nombre` en el DTO de convocatorias. La web resuelve nombres de partida con `GET /partidas`, que ya existe. Sin endpoints nuevos, sin tocar Partidas ni el gateway.

**Tech Stack:** .NET 8 (Clean Architecture + MediatR + xUnit), React 18 + Vite + TypeScript + vitest, React Native + Expo + `node --test`.

**Spec:** `docs/superpowers/specs/2026-07-15-nombres-partida-juego-design.md`

## Global Constraints

- **`Juego` no tiene nombre en el dominio.** No añadir `NombreJuego`. La etiqueta se deriva de `Orden` + `TipoJuego`: `"Juego 1 · Trivia"`, `"Juego 2 · Búsqueda del Tesoro"`.
- **El backend no fabrica strings de presentación.** Devuelve `juegoOrden` y `tipoJuego`; el cliente compone la etiqueta.
- **Resolver un nombre nunca rompe la pantalla.** Todo fallo degrada al GUID corto (`id.slice(0, 8)`), sin error visible.
- **Los dos "vacíos" de la columna Juego no se colapsan:** `juegoId` null → `—` (evento de partida, no hay juego); `juegoId` presente pero `juegoOrden` null → `guidCorto(juegoId)` (hay juego, se desconoce cuál).
- **`useNombresPartida` NO lleva caché incremental ni troceo**, a diferencia de `useNombres`. No hay push de SignalR aquí y `GET /partidas` trae todo en un request.
- **No usar `GET /partidas/{id}` para leer un nombre**: arrastra respuestas correctas y códigos QR. Usar `GET /partidas` (resumen ligero).
- **No tocar `data-testid`, `label` ni roles ARIA** existentes. Cambiar el texto pintado sí es esperado.
- **Nullable:** `juegoOrden` y `tipoJuego` son nullable porque `PartidaIniciada`/`PartidaFinalizada` no tienen juego.
- **Fuera de alcance:** `HistorialPartidasScreen.tsx` y `RendimientoEquipoScreen.tsx` (móvil) — usan `partidaId` solo como `key`, no muestran GUID. Cabeceras de `SesionOperadorPage` — mismo motivo.

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ConvocatoriaPendienteProyeccion.cs` | record de lectura con el nombre |
| `.../Domain/Abstractions/Persistence/ISesionPartidaRepository.cs:18` | firma del método |
| `.../Infrastructure/Persistence/SesionPartidaRepository.cs:123-132` | proyección EF |
| `.../Application/DTOs/ConvocatoriaPendienteDto.cs` | DTO + `NombrePartida` |
| `.../Application/Handlers/Queries/ObtenerMisConvocatoriasPendientesQueryHandler.cs` | mapeo |
| `services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/HistorialPartidaResponse.cs` | `EntradaHistorialDto` + orden/tipo |
| `.../Application/Handlers/Queries/ObtenerHistorialPartidaQueryHandler.cs` | join con `JuegoProyectado` |
| `frontend/src/features/partidas/juegoLabels.ts` | `etiquetaJuego` — puro |
| `frontend/src/features/shared/useNombresPartida.ts` | hook: `getPartidas()` + fallback |

---

### Task 1: `EntradaHistorialDto` gana `juegoOrden` y `tipoJuego` (Puntuaciones)

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/HistorialPartidaResponse.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerHistorialPartidaQueryHandler.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/ObtenerHistorialPartidaQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `IProyeccionesRepository.GetJuegosDePartidaAsync(Guid partidaId, CancellationToken)` → `IReadOnlyList<JuegoProyectado>` (ya existe); `JuegoProyectado` expone `JuegoId`, `PartidaId`, `Orden`, `TipoJuego`; `IHistorialRepository.GetHistorialDePartidaAsync(...)` y `.ContarHistorialDePartidaAsync(...)` (ya existen).
- Produces: `EntradaHistorialDto(DateTime OccurredAt, string TipoEvento, Guid? JuegoId, Guid? ParticipanteId, Guid? EquipoId, JsonElement Detalle, int? JuegoOrden, TipoJuego? TipoJuego)` — los dos campos nuevos van **al final** para no romper construcciones posicionales existentes.

Se trae los juegos de la partida **una vez** con `GetJuegosDePartidaAsync` y se arma un diccionario. No hacer una llamada por entrada (N+1).

- [ ] **Step 1: Write the failing test**

Añadir a `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/ObtenerHistorialPartidaQueryHandlerTests.cs` (si el archivo no existe, crearlo siguiendo el patrón de fakes del proyecto; leer primero un test vecino de `Handlers/Queries` para copiar la forma de los fakes de `IProyeccionesRepository` e `IHistorialRepository`):

```csharp
[Fact]
public async Task Entrada_con_juego_proyectado_trae_orden_y_tipo()
{
    var partidaId = Guid.NewGuid();
    var juegoId = Guid.NewGuid();
    var proyecciones = new FakeProyeccionesRepository();
    proyecciones.Partidas.Add(PartidaProyectada.Nueva(partidaId, Guid.NewGuid()));
    proyecciones.Juegos.Add(JuegoProyectado.Nuevo(juegoId, partidaId, 2, TipoJuego.Trivia));
    var historial = new FakeHistorialRepository();
    historial.Eventos.Add(EventoHistorial.Nuevo(
        partidaId, "RespuestaTriviaValidada", DateTime.UtcNow, juegoId, null, null, "{}"));
    var handler = new ObtenerHistorialPartidaQueryHandler(proyecciones, historial);

    var result = await handler.Handle(
        new ObtenerHistorialPartidaQuery(partidaId, null, 50, 0), CancellationToken.None);

    var entrada = Assert.Single(result.Entradas);
    Assert.Equal(2, entrada.JuegoOrden);
    Assert.Equal(TipoJuego.Trivia, entrada.TipoJuego);
}

[Fact]
public async Task Evento_de_partida_sin_juego_deja_orden_y_tipo_en_null()
{
    var partidaId = Guid.NewGuid();
    var proyecciones = new FakeProyeccionesRepository();
    proyecciones.Partidas.Add(PartidaProyectada.Nueva(partidaId, Guid.NewGuid()));
    var historial = new FakeHistorialRepository();
    historial.Eventos.Add(EventoHistorial.Nuevo(
        partidaId, "PartidaIniciada", DateTime.UtcNow, null, null, null, "{}"));
    var handler = new ObtenerHistorialPartidaQueryHandler(proyecciones, historial);

    var result = await handler.Handle(
        new ObtenerHistorialPartidaQuery(partidaId, null, 50, 0), CancellationToken.None);

    var entrada = Assert.Single(result.Entradas);
    Assert.Null(entrada.JuegoOrden);
    Assert.Null(entrada.TipoJuego);
}

[Fact]
public async Task Evento_con_juegoId_sin_proyeccion_deja_orden_null_sin_lanzar()
{
    // Lag de proyección o evento perdido: el juego existe pero Puntuaciones no lo tiene.
    // El cliente cae al GUID corto; el handler no debe explotar.
    var partidaId = Guid.NewGuid();
    var proyecciones = new FakeProyeccionesRepository();
    proyecciones.Partidas.Add(PartidaProyectada.Nueva(partidaId, Guid.NewGuid()));
    var historial = new FakeHistorialRepository();
    historial.Eventos.Add(EventoHistorial.Nuevo(
        partidaId, "JuegoActivado", DateTime.UtcNow, Guid.NewGuid(), null, null, "{}"));
    var handler = new ObtenerHistorialPartidaQueryHandler(proyecciones, historial);

    var result = await handler.Handle(
        new ObtenerHistorialPartidaQuery(partidaId, null, 50, 0), CancellationToken.None);

    var entrada = Assert.Single(result.Entradas);
    Assert.NotNull(entrada.JuegoId);
    Assert.Null(entrada.JuegoOrden);
    Assert.Null(entrada.TipoJuego);
}
```

**Ajustar los factories** (`PartidaProyectada.Nueva`, `JuegoProyectado.Nuevo`, `EventoHistorial.Nuevo`, `ObtenerHistorialPartidaQuery`) a las firmas reales: leerlas de `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Entities/` y `.../Application/Queries/` antes de escribir. No inventarlas.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~ObtenerHistorialPartidaQueryHandlerTests"`

Expected: FAIL de compilación — `EntradaHistorialDto` no tiene `JuegoOrden` ni `TipoJuego`.

- [ ] **Step 3: Write minimal implementation**

En `HistorialPartidaResponse.cs`:

```csharp
using System.Text.Json;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.DTOs;

// JuegoOrden/TipoJuego son nullable: los eventos de partida (PartidaIniciada,
// PartidaFinalizada) no tienen juego. Van al final para no romper construcciones
// posicionales existentes.
public sealed record EntradaHistorialDto(
    DateTime OccurredAt, string TipoEvento, Guid? JuegoId, Guid? ParticipanteId, Guid? EquipoId,
    JsonElement Detalle, int? JuegoOrden, TipoJuego? TipoJuego);

public sealed record HistorialPartidaResponse(
    Guid PartidaId, int Total, IReadOnlyList<EntradaHistorialDto> Entradas);
```

En `ObtenerHistorialPartidaQueryHandler.Handle`, reemplazar el `Select` final:

```csharp
        // Los juegos de la partida se traen una vez y se indexan: una llamada por entrada
        // sería N+1 sobre una tabla que ya cabe entera en memoria (1..* juegos por partida).
        var juegos = await _proyecciones.GetJuegosDePartidaAsync(request.PartidaId, cancellationToken);
        var porJuegoId = juegos.ToDictionary(j => j.JuegoId);

        var entradas = eventos
            .Select(e =>
            {
                JuegoProyectado? juego = null;
                if (e.JuegoId.HasValue) porJuegoId.TryGetValue(e.JuegoId.Value, out juego);
                return new EntradaHistorialDto(
                    e.OccurredAt, e.TipoEvento, e.JuegoId, e.ParticipanteId, e.EquipoId,
                    ParseDetalle(e.DetalleJson), juego?.Orden, juego?.TipoJuego);
            })
            .ToList();
        return new HistorialPartidaResponse(request.PartidaId, total, entradas);
```

Añadir `using Umbral.Puntuaciones.Domain.Entities;` si no está.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~ObtenerHistorialPartidaQueryHandlerTests"`

Expected: PASS — 3 tests nuevos.

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/HistorialPartidaResponse.cs services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerHistorialPartidaQueryHandler.cs services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/ObtenerHistorialPartidaQueryHandlerTests.cs
git commit -m "feat(puntuaciones): historial devuelve juegoOrden y tipoJuego"
```

---

### Task 2: Contract test de la forma del historial (Puntuaciones)

**Files:**
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/HistorialContractTests.cs` (ya existe)

**Interfaces:**
- Consumes: `EntradaHistorialDto` con `JuegoOrden`/`TipoJuego` (Task 1); el arnés `PuntuacionesWebFactory` + `TestAuthHandler` que el archivo ya usa.

- [ ] **Step 1: Write the failing test**

Añadir a `HistorialContractTests.cs`, reutilizando el sembrado y el cliente que sus tests ya montan:

```csharp
[Fact]
public async Task Entrada_de_historial_expone_juegoOrden_y_tipoJuego()
{
    // Sembrar una partida proyectada con un juego y un evento con juegoId, luego:
    var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/historial");
    var body = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(body);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var entrada = doc.RootElement.GetProperty("entradas")[0];
    Assert.Equal(2, entrada.GetProperty("juegoOrden").GetInt32());
    Assert.Equal("Trivia", entrada.GetProperty("tipoJuego").GetString());
}
```

Ajustar el sembrado y la ruta a los que el archivo ya usa. Si `tipoJuego` se serializa como número en vez de string, asertar el número — **no cambiar la serialización del servicio para acomodar el test**.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/Umbral.Puntuaciones.ContractTests.csproj" --filter "FullyQualifiedName~HistorialContractTests"`

Expected: FAIL — `juegoOrden` no existe en la respuesta si Task 1 no está aplicada; si ya lo está, PASS directo.

- [ ] **Step 3: No hay implementación nueva**

La forma la produce Task 1. Cualquier fallo es defecto de ahí.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS — suite completa sin regresiones.

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/
git commit -m "test(puntuaciones): contrato de juegoOrden y tipoJuego en el historial"
```

---

### Task 3: Proyección del nombre en el repositorio de convocatorias (Operaciones)

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ConvocatoriaPendienteProyeccion.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs:18`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs:123-132`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs:65-69`

**Interfaces:**
- Produces: `ConvocatoriaPendienteProyeccion(Guid ConvocatoriaId, Guid PartidaId, string NombrePartida, Guid EquipoId, DateTime FechaEnvio)`; `ISesionPartidaRepository.GetConvocatoriasPendientesByUsuarioAsync(Guid, CancellationToken)` pasa a devolver `Task<IReadOnlyList<ConvocatoriaPendienteProyeccion>>`.

**Por qué cambia la firma:** hoy devuelve `IReadOnlyList<Convocatoria>` y hace `SelectMany` de `Sesiones` → `Inscripciones` → `Convocatorias`, así que la `SesionPartida` (y su `Nombre`) se pierde. El record de lectura junto a la interfaz sigue el patrón de `ParticipacionEquipoHistorial` en Puntuaciones. Solo hay **un caller de producción** (el handler de Task 4) y 4 archivos de test que referencian la interfaz.

- [ ] **Step 1: Write the failing test**

Añadir a `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/` (archivo nuevo `ConvocatoriasPendientesPersistenceTests.cs`, siguiendo el patrón de los `*PersistenceTests.cs` que ya existen ahí):

```csharp
[Fact]
public async Task Convocatoria_pendiente_trae_el_nombre_de_la_sesion()
{
    // Sembrar una SesionPartida en Lobby con nombre "Copa UCAB", una inscripción de equipo
    // activa y una convocatoria Pendiente para el usuario. Ajustar el sembrado a los
    // factories reales del dominio (SesionPartida.Publicar / PreinscribirEquipo).
    var resultado = await repo.GetConvocatoriasPendientesByUsuarioAsync(usuarioId, CancellationToken.None);

    var c = Assert.Single(resultado);
    Assert.Equal("Copa UCAB", c.NombrePartida);
    Assert.Equal(partidaId, c.PartidaId);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj" --filter "FullyQualifiedName~ConvocatoriasPendientesPersistenceTests"`

Expected: FAIL de compilación — `NombrePartida` no existe (el método devuelve `Convocatoria`).

- [ ] **Step 3: Write minimal implementation**

Crear `Domain/Abstractions/Persistence/ConvocatoriaPendienteProyeccion.cs`:

```csharp
namespace Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

// Convocatoria pendiente + el nombre de su sesión, resuelto en la misma consulta.
// El nombre vive en SesionPartida y el SelectMany hasta Convocatoria lo perdía.
public sealed record ConvocatoriaPendienteProyeccion(
    Guid ConvocatoriaId, Guid PartidaId, string NombrePartida, Guid EquipoId, DateTime FechaEnvio);
```

En `ISesionPartidaRepository.cs:18`, cambiar la firma:

```csharp
    Task<IReadOnlyList<ConvocatoriaPendienteProyeccion>> GetConvocatoriasPendientesByUsuarioAsync(
        Guid usuarioId, CancellationToken cancellationToken);
```

En `SesionPartidaRepository.cs:123-132`:

```csharp
    public async Task<IReadOnlyList<ConvocatoriaPendienteProyeccion>> GetConvocatoriasPendientesByUsuarioAsync(
        Guid usuarioId, CancellationToken cancellationToken)
    {
        // Proyección anónima entidad+escalar (EF la traduce bien) y mapeo en memoria:
        // proyectar c.Id.Valor directo en la consulta no traduce con el value object.
        var filas = await _dbContext.Sesiones
            .Where(s => s.Estado == EstadoSesion.Lobby)
            .SelectMany(s => s.Inscripciones
                .Where(i => i.Estado == EstadoInscripcion.Activa)
                .SelectMany(i => i.Convocatorias
                    .Where(c => c.UsuarioId == usuarioId && c.Estado == EstadoConvocatoria.Pendiente)
                    .Select(c => new { Convocatoria = c, s.Nombre })))
            .ToListAsync(cancellationToken);

        return filas
            .Select(f => new ConvocatoriaPendienteProyeccion(
                f.Convocatoria.Id.Valor, f.Convocatoria.PartidaId, f.Nombre,
                f.Convocatoria.EquipoId, f.Convocatoria.FechaEnvio))
            .OrderBy(x => x.FechaEnvio)
            .ToList();
    }
```

El `OrderBy(c => c.FechaEnvio)` original estaba en la consulta; aquí se aplica tras materializar. El orden observable no cambia.

En `FakeSesionPartidaRepository.cs:65-69`, adaptar el stub a la nueva firma navegando desde `_store.Values` (que son sesiones, así que el `Nombre` está disponible):

```csharp
    public Task<IReadOnlyList<ConvocatoriaPendienteProyeccion>> GetConvocatoriasPendientesByUsuarioAsync(
        Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ConvocatoriaPendienteProyeccion>>(_store.Values
            .Where(s => s.Estado == EstadoSesion.Lobby)
            .SelectMany(s => s.Inscripciones
                .Where(i => i.Estado == EstadoInscripcion.Activa)
                .SelectMany(i => i.Convocatorias
                    .Where(c => c.UsuarioId == usuarioId && c.Estado == EstadoConvocatoria.Pendiente)
                    .Select(c => new ConvocatoriaPendienteProyeccion(
                        c.Id.Valor, c.PartidaId, s.Nombre, c.EquipoId, c.FechaEnvio))))
            .OrderBy(x => x.FechaEnvio)
            .ToList());
```

Leer el stub actual antes de editar: los nombres de campos internos (`_store`, `Inscripciones`) deben coincidir con los reales.

Si `SesionHubTests.cs`, `BarrerIniciosAutomaticosCommandHandlerTests.cs` o `BarrerTimeoutsCommandHandlerTests.cs` declaran su propio fake de la interfaz, adaptar también ese stub (basta devolver lista vacía si no usan el método).

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj" --filter "FullyQualifiedName~ConvocatoriasPendientesPersistenceTests"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs services/operaciones-sesion/tests/
git commit -m "feat(operaciones): el repo de convocatorias pendientes proyecta el nombre de la sesion"
```

---

### Task 4: `ConvocatoriaPendienteDto` gana `nombrePartida` (Operaciones)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/ConvocatoriaPendienteDto.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMisConvocatoriasPendientesQueryHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerMisConvocatoriasPendientesQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `ConvocatoriaPendienteProyeccion(Guid ConvocatoriaId, Guid PartidaId, string NombrePartida, Guid EquipoId, DateTime FechaEnvio)` (Task 3).
- Produces: `ConvocatoriaPendienteDto(Guid ConvocatoriaId, Guid PartidaId, Guid EquipoId, DateTime FechaEnvio, string NombrePartida)` — `NombrePartida` **al final**, para no romper construcciones posicionales existentes.

- [ ] **Step 1: Write the failing test**

Añadir a `ObtenerMisConvocatoriasPendientesQueryHandlerTests.cs` (leer el archivo primero para reutilizar su fake y sus factories):

```csharp
[Fact]
public async Task Devuelve_el_nombre_de_la_partida_en_cada_convocatoria()
{
    // Sembrar en el fake una sesión en Lobby llamada "Copa UCAB" con una convocatoria
    // Pendiente para el usuario. Ajustar al sembrado que el archivo ya usa.
    var handler = new ObtenerMisConvocatoriasPendientesQueryHandler(repo);

    var result = await handler.Handle(
        new ObtenerMisConvocatoriasPendientesQuery(usuarioId), CancellationToken.None);

    var dto = Assert.Single(result);
    Assert.Equal("Copa UCAB", dto.NombrePartida);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj" --filter "FullyQualifiedName~ObtenerMisConvocatoriasPendientesQueryHandlerTests"`

Expected: FAIL de compilación — `ConvocatoriaPendienteDto` no tiene `NombrePartida`.

- [ ] **Step 3: Write minimal implementation**

`ConvocatoriaPendienteDto.cs`:

```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record ConvocatoriaPendienteDto(
    Guid ConvocatoriaId, Guid PartidaId, Guid EquipoId, DateTime FechaEnvio, string NombrePartida);
```

`ObtenerMisConvocatoriasPendientesQueryHandler.Handle`:

```csharp
        var pendientes = await _sesiones.GetConvocatoriasPendientesByUsuarioAsync(request.UsuarioId, cancellationToken);
        return pendientes
            .Select(c => new ConvocatoriaPendienteDto(
                c.ConvocatoriaId, c.PartidaId, c.EquipoId, c.FechaEnvio, c.NombrePartida))
            .ToList();
```

- [ ] **Step 4: Añadir el contract test de la forma del DTO**

Crear `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/MisConvocatoriasContractTests.cs`, reutilizando `OperacionesSesionWebFactory` + `TestAuthHandler` (el patrón de `RealtimeContractTests.cs` / `BdtRuntimeEndpointsTests.cs`):

```csharp
[Fact]
public async Task Mis_convocatorias_expone_nombrePartida()
{
    // Sembrar una sesión en Lobby llamada "Copa UCAB" con una convocatoria Pendiente
    // para el usuario del token, siguiendo el sembrado de los contract tests vecinos.
    var response = await client.GetAsync("/operaciones-sesion/mis-convocatorias");
    var body = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(body);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("Copa UCAB", doc.RootElement[0].GetProperty("nombrePartida").GetString());
}
```

Si el arnés de contract tests no puede sembrar convocatorias con razonable esfuerzo, **no forzarlo**: la resolución real ya la cubre el integration test de Task 3 y el mapeo el unit test de este Task. En ese caso, asertar solo que la propiedad `nombrePartida` existe en la forma con una lista vacía no aporta nada — anotar la omisión y seguir.

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"`
Expected: PASS — suite completa sin regresiones. Si `SesionesControllerMisConvocatoriasTests.cs` construye `ConvocatoriaPendienteDto` posicionalmente, añadirle el nombre.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/ services/operaciones-sesion/tests/
git commit -m "feat(operaciones): mis-convocatorias devuelve nombrePartida"
```

---

### Task 5: `etiquetaJuego` (web)

**Files:**
- Create: `frontend/src/features/partidas/juegoLabels.ts`
- Test: `frontend/src/features/partidas/juegoLabels.test.ts`

**Interfaces:**
- Produces: `etiquetaJuego(orden: number | null | undefined, tipoJuego: string | null | undefined, juegoId: string | null | undefined): string`

Módulo puro, sin React: se testea sin renderizar.

- [ ] **Step 1: Write the failing test**

Crear `frontend/src/features/partidas/juegoLabels.test.ts`:

```ts
import { describe, expect, it } from "vitest";
import { etiquetaJuego } from "./juegoLabels";

const JUEGO = "abcdef12-0000-0000-0000-000000000000";

describe("etiquetaJuego", () => {
  it("compone orden y tipo para Trivia", () => {
    expect(etiquetaJuego(1, "Trivia", JUEGO)).toBe("Juego 1 · Trivia");
  });

  it("traduce BusquedaDelTesoro a texto legible", () => {
    expect(etiquetaJuego(2, "BusquedaDelTesoro", JUEGO)).toBe("Juego 2 · Búsqueda del Tesoro");
  });

  it("sin juego devuelve raya: el evento es de partida", () => {
    expect(etiquetaJuego(null, null, null)).toBe("—");
  });

  it("con juegoId pero sin orden cae al GUID corto, no a raya", () => {
    // Hay un juego pero no se sabe cuál (lag de proyección): pintar "—" mentiría.
    expect(etiquetaJuego(null, null, JUEGO)).toBe("abcdef12");
  });

  it("tipo desconocido se pinta tal cual, sin romper", () => {
    expect(etiquetaJuego(3, "AlgoNuevo", JUEGO)).toBe("Juego 3 · AlgoNuevo");
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/juegoLabels.test.ts`
Expected: FAIL — `Cannot find module './juegoLabels'`.

- [ ] **Step 3: Write minimal implementation**

Crear `frontend/src/features/partidas/juegoLabels.ts`:

```ts
// Etiqueta de un juego para el operador. `Juego` no tiene nombre en el dominio: su
// única identidad propia es el orden dentro de la partida y su tipo.
const TIPO_LEGIBLE: Record<string, string> = {
  Trivia: "Trivia",
  BusquedaDelTesoro: "Búsqueda del Tesoro"
};

export function etiquetaJuego(
  orden: number | null | undefined,
  tipoJuego: string | null | undefined,
  juegoId: string | null | undefined
): string {
  if (orden == null) {
    // Dos vacíos distintos: sin juegoId el evento es de partida y no hay juego que
    // nombrar; con juegoId el juego existe pero su proyección falta, y decir "—"
    // ocultaría que hay uno.
    return juegoId ? juegoId.slice(0, 8) : "—";
  }

  const tipo = tipoJuego ? (TIPO_LEGIBLE[tipoJuego] ?? tipoJuego) : "";
  return tipo ? `Juego ${orden} · ${tipo}` : `Juego ${orden}`;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd frontend && npx vitest run src/features/partidas/juegoLabels.test.ts`
Expected: PASS — 5 tests.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/juegoLabels.ts frontend/src/features/partidas/juegoLabels.test.ts
git commit -m "feat(web): etiquetaJuego derivada de orden y tipo"
```

---

### Task 6: Hook `useNombresPartida` (web)

**Files:**
- Create: `frontend/src/features/shared/useNombresPartida.ts`
- Test: `frontend/src/features/shared/useNombresPartida.test.ts`

**Interfaces:**
- Consumes: `getPartidas(accessToken): Promise<PartidaSummary[]>` de `frontend/src/api/partidasApi.ts` (ya existe); `PartidaSummary` incluye `partidaId` y `nombrePartida`.
- Produces: `useNombresPartida(accessToken: string): (partidaId: string) => string`

**Sin caché de módulo ni troceo**, a diferencia de `useNombres`: no hay push de SignalR y `GET /partidas` trae todo en un request. Un fetch por montaje.

- [ ] **Step 1: Write the failing test**

Crear `frontend/src/features/shared/useNombresPartida.test.ts`:

```ts
import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useNombresPartida } from "./useNombresPartida";
import * as partidasApi from "../../api/partidasApi";

const P1 = "aaaaaaaa-0000-0000-0000-000000000000";
const P2 = "bbbbbbbb-0000-0000-0000-000000000000";

const resumen = (partidaId: string, nombrePartida: string) =>
  ({ partidaId, nombrePartida, modalidad: "Individual", modoInicioPartida: "Manual",
     tiempoInicio: null, minimosParticipacion: 1, maximosParticipacion: 10,
     estado: null, cantidadJuegos: 1 }) as unknown as partidasApi.PartidaSummary;

beforeEach(() => vi.restoreAllMocks());

describe("useNombresPartida", () => {
  it("resuelve el nombre de una partida conocida", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([resumen(P1, "Copa UCAB")]);

    const { result } = renderHook(() => useNombresPartida("tok"));

    await waitFor(() => expect(result.current(P1)).toBe("Copa UCAB"));
  });

  it("cae al GUID corto para una partida que no está en la lista", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([resumen(P1, "Copa UCAB")]);

    const { result } = renderHook(() => useNombresPartida("tok"));

    await waitFor(() => expect(partidasApi.getPartidas).toHaveBeenCalled());
    expect(result.current(P2)).toBe("bbbbbbbb");
  });

  it("cae al GUID corto cuando la llamada falla, sin lanzar", async () => {
    vi.spyOn(partidasApi, "getPartidas").mockRejectedValue(new Error("caido"));

    const { result } = renderHook(() => useNombresPartida("tok"));

    await waitFor(() => expect(partidasApi.getPartidas).toHaveBeenCalled());
    expect(result.current(P1)).toBe("aaaaaaaa");
  });

  it("pide la lista una sola vez por montaje", async () => {
    const spy = vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([resumen(P1, "Copa UCAB")]);

    const { rerender } = renderHook(() => useNombresPartida("tok"));
    rerender();

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1));
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/shared/useNombresPartida.test.ts`
Expected: FAIL — `Cannot find module './useNombresPartida'`.

- [ ] **Step 3: Write minimal implementation**

Crear `frontend/src/features/shared/useNombresPartida.ts`:

```ts
// Nombres de partida para las vistas de revisión del operador (historial, rendimiento).
//
// A diferencia de useNombres (competidores), aquí no hay caché incremental ni troceo:
// no llegan partidas nuevas por push de SignalR y GET /partidas trae el listado entero
// en un request. Un fetch por montaje basta.
//
// Contrato con las pantallas: nombrePartidaDe(id) SIEMPRE devuelve algo pintable.
import { useEffect, useState } from "react";
import { getPartidas } from "../../api/partidasApi";

export function useNombresPartida(accessToken: string): (partidaId: string) => string {
  const [nombres, setNombres] = useState<Map<string, string>>(new Map());

  useEffect(() => {
    let activo = true;
    getPartidas(accessToken)
      .then((partidas) => {
        if (!activo) return;
        setNombres(new Map(partidas.map((p) => [p.partidaId, p.nombrePartida])));
      })
      .catch(() => {
        // Degradación deliberada: las pantallas se quedan con GUIDs cortos y siguen
        // siendo operativas. Resolver un nombre nunca rompe la pantalla.
      });
    return () => {
      activo = false;
    };
  }, [accessToken]);

  return (partidaId: string) => nombres.get(partidaId) ?? partidaId.slice(0, 8);
}
```

Verificar la firma real de `getPartidas` en `frontend/src/api/partidasApi.ts:198` antes de escribir: si toma `(accessToken, fetchImpl?)`, encaja; si difiere, adaptar la llamada, **no la del api**.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd frontend && npx vitest run src/features/shared/useNombresPartida.test.ts`
Expected: PASS — 4 tests.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/shared/useNombresPartida.ts frontend/src/features/shared/useNombresPartida.test.ts
git commit -m "feat(web): hook useNombresPartida"
```

---

### Task 7: Cabecera y columna Juego del historial (web)

**Files:**
- Modify: `frontend/src/features/partidas/HistorialPartidaPage.tsx` (cabecera `<h1>` ~línea 92; columna Juego ~línea 137)
- Modify: `frontend/src/api/puntuacionesApi.ts` — el tipo de la entrada de historial gana `juegoOrden` y `tipoJuego`
- Modify: `frontend/src/features/partidas/HistorialPartidaPage.test.tsx`

**Interfaces:**
- Consumes: `etiquetaJuego` (Task 5), `useNombresPartida` (Task 6), `juegoOrden`/`tipoJuego` del DTO (Task 1).

`guidCorto` **no se borra**: lo usa `etiquetaJuego` internamente vía su propio fallback, y el archivo lo sigue empleando para nada más — si tras el cambio queda sin usos, borrarlo; si lo usan otras columnas, dejarlo.

- [ ] **Step 1: Write the failing test**

Añadir a `HistorialPartidaPage.test.tsx` (leer primero cómo mockea `getHistorialPartida`):

```tsx
it("la cabecera muestra el nombre de la partida", async () => {
  vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([
    { partidaId: "p1", nombrePartida: "Copa UCAB" } as never
  ]);
  renderHistorial();

  expect(await screen.findByText(/Copa UCAB/)).toBeInTheDocument();
});

it("la columna Juego muestra orden y tipo, no el GUID", async () => {
  vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue({
    partidaId: "p1",
    total: 1,
    entradas: [{
      occurredAt: "2026-07-08T12:00:00Z", tipoEvento: "RespuestaTriviaValidada",
      juegoId: "abcdef12-0000-0000-0000-000000000000", participanteId: null, equipoId: null,
      detalle: {}, juegoOrden: 1, tipoJuego: "Trivia"
    }]
  } as never);
  renderHistorial();

  expect(await screen.findByText("Juego 1 · Trivia")).toBeInTheDocument();
});

it("un evento de partida sin juego muestra raya", async () => {
  vi.spyOn(puntuacionesApi, "getHistorialPartida").mockResolvedValue({
    partidaId: "p1",
    total: 1,
    entradas: [{
      occurredAt: "2026-07-08T12:00:00Z", tipoEvento: "PartidaIniciada",
      juegoId: null, participanteId: null, equipoId: null,
      detalle: {}, juegoOrden: null, tipoJuego: null
    }]
  } as never);
  renderHistorial();

  expect(await screen.findByTestId("tabla-historial")).toBeInTheDocument();
});
```

Adaptar `renderHistorial` al arnés real del archivo. Añadir `resetNombresCache()` en `beforeEach` **solo si** el archivo usa `useNombres` (lo usa: las columnas participante/equipo del slice anterior).

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/HistorialPartidaPage.test.tsx`
Expected: FAIL — se pinta el GUID del juego y la cabecera no trae nombre.

- [ ] **Step 3: Write minimal implementation**

En `frontend/src/api/puntuacionesApi.ts`, añadir al tipo de la entrada de historial:

```ts
  juegoOrden: number | null;
  tipoJuego: string | null;
```

En `HistorialPartidaPage.tsx`:

```tsx
import { etiquetaJuego } from "./juegoLabels";
import { useNombresPartida } from "../shared/useNombresPartida";

// dentro del componente:
const nombrePartidaDe = useNombresPartida(accessToken);

// cabecera (~línea 92):
<h1>Historial de la partida{partidaId ? ` — ${nombrePartidaDe(partidaId)}` : ""}</h1>

// columna Juego (~línea 137):
<td>{etiquetaJuego(e.juegoOrden, e.tipoJuego, e.juegoId)}</td>
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd frontend && npx vitest run src/features/partidas/HistorialPartidaPage.test.tsx`
Expected: PASS — incluidos los tests que ya existían.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/puntuacionesApi.ts frontend/src/features/partidas/HistorialPartidaPage.tsx frontend/src/features/partidas/HistorialPartidaPage.test.tsx
git commit -m "feat(web): nombre de partida en la cabecera y etiqueta de juego en el historial"
```

---

### Task 8: Nombre de partida en rendimiento de equipo (web)

**Files:**
- Modify: `frontend/src/features/puntuaciones/RendimientoEquipoPage.tsx:118`
- Modify: `frontend/src/features/puntuaciones/RendimientoEquipoPage.test.tsx`

**Interfaces:**
- Consumes: `useNombresPartida` (Task 6).

- [ ] **Step 1: Write the failing test**

Añadir a `RendimientoEquipoPage.test.tsx`:

```tsx
it("muestra el nombre de la partida en vez del GUID corto", async () => {
  vi.spyOn(partidasApi, "getPartidas").mockResolvedValue([
    { partidaId: GUID, nombrePartida: "Copa UCAB" } as never
  ]);
  renderPage(`/puntuaciones/equipos?equipoId=${GUID}`);

  expect(await screen.findByText("Copa UCAB")).toBeInTheDocument();
});

it("mantiene el GUID corto si el listado de partidas falla", async () => {
  vi.spyOn(partidasApi, "getPartidas").mockRejectedValue(new Error("caido"));
  renderPage(`/puntuaciones/equipos?equipoId=${GUID}`);

  await waitFor(() => expect(screen.getByText(GUID.slice(0, 8))).toBeInTheDocument());
});
```

Adaptar al arnés real: el archivo ya tiene `renderPage(url)` y una constante `GUID` (ver `RendimientoEquipoPage.test.tsx:20,121-131`). El fixture de rendimiento debe traer una partida cuyo `partidaId` sea el GUID esperado.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/puntuaciones/RendimientoEquipoPage.test.tsx`
Expected: FAIL — se pinta el GUID corto.

- [ ] **Step 3: Write minimal implementation**

En `RendimientoEquipoPage.tsx`:

```tsx
import { useNombresPartida } from "../shared/useNombresPartida";

// dentro del componente:
const nombrePartidaDe = useNombresPartida(accessToken);

// línea 118:
<td>{nombrePartidaDe(p.partidaId)}</td>
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd frontend && npm test`
Expected: PASS — toda la suite web, sin regresiones.

Run: `cd frontend && npx tsc --noEmit`
Expected: exit 0.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/puntuaciones/
git commit -m "feat(web): nombre de partida en rendimiento de equipo"
```

---

### Task 9: Nombre de partida en convocatorias (móvil)

**Files:**
- Modify: `mobile/src/features/partidas/ConvocatoriasScreen.tsx` — el tipo `Convocatoria` (~línea 10) y la línea que pinta `Partida {c.partidaId.slice(0, 8)}`
- Test: `mobile/tests/convocatoriasFlow.test.js`

**Interfaces:**
- Consumes: `nombrePartida` del DTO de `/operaciones-sesion/mis-convocatorias` (Task 4).

**Limitación declarada en el spec:** `ConvocatoriasScreen` es `.tsx` y `node --test` no puede importarlo, así que el render no queda cubierto — solo el flujo que lo alimenta. Aquí no hay lógica que extraer a un `.js`: el cambio es leer un campo del DTO.

- [ ] **Step 1: Write the failing test**

Añadir a `mobile/tests/convocatoriasFlow.test.js` (leer primero el arnés del archivo):

```js
test("fetchConvocatorias propaga nombrePartida del backend", async () => {
  const fetchImpl = async () => ({
    ok: true,
    status: 200,
    json: async () => [{
      convocatoriaId: "c1", partidaId: "p1", equipoId: "e1",
      fechaEnvio: "2026-07-08T12:00:00Z", nombrePartida: "Copa UCAB",
    }],
  });

  const r = await fetchConvocatorias("http://api.test", "tok", fetchImpl);

  assert.strictEqual(r.ok, true);
  assert.strictEqual(r.data[0].nombrePartida, "Copa UCAB");
});
```

Ajustar la firma de `fetchConvocatorias` a la real de `convocatoriasFlow.js`.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile && npm test`
Expected: PASS o FAIL según si `fetchConvocatorias` mapea campos o los pasa tal cual. **Si pasa a la primera**, el flujo ya es transparente y el test queda como red de seguridad contra un mapeo futuro que lo pierda — anotarlo y seguir.

- [ ] **Step 3: Write minimal implementation**

En `ConvocatoriasScreen.tsx`, añadir el campo al tipo (línea ~10):

```tsx
type Convocatoria = {
  convocatoriaId: string;
  partidaId: string;
  equipoId: string;
  fechaEnvio: string;
  nombrePartida: string;
};
```

Sustituir **solo** la línea que muestra la partida (`<AppText variant="bodyStrong">Partida {c.partidaId.slice(0, 8)}</AppText>`):

```tsx
<AppText variant="bodyStrong">{c.nombrePartida}</AppText>
```

**La línea del equipo no se toca.** El slice anterior ya la dejó como `Equipo {nombreDe(c.equipoId)}` resolviendo por `useNombres`; sigue igual. Los números de línea del spec (78/79) son previos a ese cambio y pueden haberse desplazado: localizar la línea por su contenido (`c.partidaId.slice(0, 8)`), no por el número.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile && npm test`
Expected: PASS.

Run: `cd mobile && npm run typecheck`
Expected: exit 0.

- [ ] **Step 5: Commit**

```bash
git add mobile/src/features/partidas/ConvocatoriasScreen.tsx mobile/tests/convocatoriasFlow.test.js
git commit -m "feat(mobile): nombre de partida en convocatorias"
```

---

### Task 10: Contratos y trazabilidad

**Files:**
- Modify: `contracts/http/puntuaciones-api.md`
- Modify: `contracts/http/operaciones-sesion-api.md`
- Modify: `docs/04-sdd/SPECS-LIST.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

- [ ] **Step 1: `contracts/http/puntuaciones-api.md`**

En la sección del historial de partida, añadir a la forma de la entrada:

```markdown
- `juegoOrden` (int, nullable) y `tipoJuego` (nullable) acompañan a `juegoId`, unidos desde la
  proyección `JuegoProyectado`. Son `null` en eventos de partida (`PartidaIniciada`,
  `PartidaFinalizada`), que no tienen juego, y también cuando el `juegoId` existe pero su
  proyección falta (lag / evento perdido) — el cliente distingue ambos casos: `—` para el
  primero, GUID corto para el segundo. **`Juego` no tiene nombre en el dominio**: la etiqueta
  legible ("Juego 1 · Trivia") la compone el cliente.
```

- [ ] **Step 2: `contracts/http/operaciones-sesion-api.md`**

Actualizar el DTO de convocatorias pendientes:

```markdown
- `ConvocatoriaPendienteDto { convocatoriaId, partidaId, equipoId, fechaEnvio, nombrePartida }` —
  `nombrePartida` es el snapshot `SesionPartida.Nombre`, proyectado en la misma consulta del
  repositorio. Evita que el móvil (Participante) tenga que llegar a Partidas, que el gateway le
  cierra (`/partidas/{**catch-all}` → `OperadorOAdministrador`).
```

- [ ] **Step 3: `docs/04-sdd/SPECS-LIST.md`**

```markdown
| Nombres de partida y de juego en las pantallas (refinamiento transversal) | Puntuaciones + Operaciones de Sesión | web + mobile | Operador / Participante | docs/superpowers/specs/2026-07-15-nombres-partida-juego-design.md | Implemented (10 tasks). Slice hermano del de nombres de competidores. `Juego` no tiene nombre en el dominio: la columna muestra "Juego 1 · Trivia" derivado de orden y tipo. Sin endpoints nuevos, sin cambios en Partidas ni en el gateway. |
```

- [ ] **Step 4: `docs/04-sdd/traceability-matrix.md`**

Tabla de 7 columnas (`Feature | Requirement | Owning service | Supporting services | SDD folder | Contracts | Status`). Añadir al final:

```markdown
| Nombres de partida y de juego en las pantallas (refinamiento transversal) | Las tres superficies que aún mostraban GUID muestran identidad legible: cabecera y columna Juego del historial, celda de partida en rendimiento de equipo, y convocatorias en móvil. `EntradaHistorialDto` gana `juegoOrden`/`tipoJuego` (join con `JuegoProyectado`); `ConvocatoriaPendienteDto` gana `nombrePartida` (proyectado de `SesionPartida.Nombre`, cambiando la firma del repo). La web resuelve nombres de partida con `GET /partidas`, que ya existía | Puntuaciones + Operaciones de Sesión | Web + Mobile (consumidores); Partidas y Gateway **sin cambios** | docs/superpowers/specs/2026-07-15-nombres-partida-juego-design.md · docs/superpowers/plans/2026-07-15-nombres-partida-juego.md | contracts/http/puntuaciones-api.md · contracts/http/operaciones-sesion-api.md | Implemented — 10 tasks. **Fuente:** slice diferido explícitamente en el spec de nombres de competidores (2026-07-14). **Decisión:** `Juego` no tiene nombre en el dominio y no se le añade — la etiqueta se deriva de `Orden` + `TipoJuego`; añadir `NombreJuego` sería capacidad de negocio nueva con HU propia. **Hallazgo:** la cabecera del historial no mostraba identidad alguna ("Historial de la partida" a secas), peor que un GUID; entró al alcance. **Diferido:** `HistorialPartidasScreen`/`RendimientoEquipoScreen` (móvil) y cabeceras de `SesionOperadorPage` — no muestran GUID, así que caen fuera de "sustituir GUIDs visibles". |
```

- [ ] **Step 5: Verificar todo y commitear**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"`
Run: `cd frontend && npm test && npx tsc --noEmit`
Run: `cd mobile && npm test && npm run typecheck`
Expected: PASS en las cuatro.

```bash
git add contracts/http/puntuaciones-api.md contracts/http/operaciones-sesion-api.md docs/04-sdd/SPECS-LIST.md docs/04-sdd/traceability-matrix.md
git commit -m "docs: contratos de nombres de partida y juego + trazabilidad"
```

---

## Notas para quien ejecute

- **`HistorialPartidaPage` ya usa `useNombres`** (slice anterior, columnas participante y equipo). Sus tests necesitan `resetNombresCache()` en `beforeEach`. `useNombresPartida` en cambio no tiene caché de módulo y no necesita reset.
- **Los campos nuevos van al final de los records** (`EntradaHistorialDto`, `ConvocatoriaPendienteDto`) para no romper construcciones posicionales existentes en tests.
- **No añadir `NombreJuego` al dominio** por conveniencia si la etiqueta derivada resulta incómoda. Es una decisión tomada: sería capacidad de negocio nueva y necesita su propia HU.
