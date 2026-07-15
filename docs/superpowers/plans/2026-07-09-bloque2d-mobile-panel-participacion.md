# Bloque 2d — Mobile panel de partidas + participación Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Participante mobile descubre partidas publicadas, se inscribe (Individual), preinscribe su equipo (líder), gestiona convocatorias y espera en un lobby con push SignalR; endpoint de listado nuevo en Operaciones de Sesión; retiro del código mobile legacy trivia/bdt.

**Architecture:** Backend: un query CQRS nuevo (`ListarPartidasPublicadasQuery`) sobre la DB de Operaciones (las `SesionPartida` en `Lobby` ya tienen nombre/modalidad/cupos por el snapshot de publicación). Mobile: feature nuevo `features/partidas/` siguiendo el patrón del repo — API modules `.js` con result objects, flows testeables con `node --test`, screens `.tsx` con containers que inyectan `mobileEnv.gatewayApiBaseUrl` + token.

**Tech Stack:** .NET 8 + MediatR + EF Core (backend); React Native + Expo SDK 54, `@microsoft/signalr` (dep nueva mobile), `node --test` (mobile tests).

## Global Constraints

- **Sin cambios de regla de negocio.** El endpoint nuevo es lectura participant-safe (solo sesiones `Lobby`; sin juegos/preguntas/QR).
- Backend: controllers heredan `ControllerBase`, despachan por MediatR, **cero lógica de negocio**; cada controller nuevo/modificado con unit tests (regla graded del curso).
- Mobile: API modules patrón exacto del repo — `(apiBaseUrl, token, ..., fetchImpl = fetch)`, result objects `{ok: true, data}` / `{ok: false, type, message}`, **sin throws**; mensajes de error en español sin acentos (patrón del repo: "Sesion expirada", "conexion").
- Gate mobile: `npm test` (`node --test tests/*.test.js`) + `npm run typecheck` (`tsc --noEmit` — en mobile ese SÍ es el gate real). Gate backend: `dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln`.
- Cada commit termina con trailer: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Subagents PROHIBIDO `git stash/reset/checkout/restore/clean`. Solo `git add <ruta exacta>` archivo por archivo (deleciones con `git rm <ruta exacta>`).
- Baseline pre-T1: suite mobile verde (`npm test`), backend Operaciones verde.

---

### Task 1: Backend — `GET /operaciones-sesion/partidas-publicadas`

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/PartidaPublicadaDto.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Queries/ListarPartidasPublicadasQuery.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ListarPartidasPublicadasQueryHandler.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs` (método nuevo)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs` (implementación)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs` (endpoint GET)
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs` (método nuevo del fake)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ListarPartidasPublicadasQueryHandlerTests.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerPartidasPublicadasTests.cs`

**Interfaces:**
- Consumes: `SesionPartida` (entidad existente: `PartidaId`, `Nombre`, `Modalidad`, `ModoInicioPartida`, `TiempoInicio`, `MinimosParticipacion`, `MaximosParticipacion`, `Estado`, `Inscripciones` con `EsActiva`), `ISesionPartidaRepository`, patrón `ObtenerLobbyQuery`/`ObtenerLobbyQueryHandler`.
- Produces: `PartidaPublicadaDto(Guid PartidaId, string Nombre, string Modalidad, string ModoInicioPartida, DateTime? TiempoInicio, int MinimosParticipacion, int MaximosParticipacion, int InscritosActivos)` — el shape JSON camelCase que consume el mobile en Task 3.

- [ ] **Step 1: Escribir tests del handler (fallan)**

`ListarPartidasPublicadasQueryHandlerTests.cs` (mismo estilo que `ObtenerLobbyQueryHandlerTests`; el fixture `PublishedSession` se copia de ahí):

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ListarPartidasPublicadasQueryHandlerTests
{
    private static SesionPartida PublishedSession(Guid partidaId, string nombre = "Copa")
    {
        var snapshot = new ConfiguracionSnapshot(nombre, Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia) });
        return SesionPartida.Publicar(partidaId, snapshot);
    }

    [Fact]
    public async Task Lista_solo_sesiones_en_lobby_con_conteo_de_inscritos()
    {
        var repo = new FakeSesionPartidaRepository();
        var enLobby = PublishedSession(Guid.NewGuid(), "Abierta");
        enLobby.Inscribir(Guid.NewGuid(), false, 0, DateTime.UtcNow);
        repo.Add(enLobby);
        var iniciada = PublishedSession(Guid.NewGuid(), "Cerrada");
        iniciada.Inscribir(Guid.NewGuid(), false, 0, DateTime.UtcNow);
        iniciada.AplicarInicio(DateTime.UtcNow);
        repo.Add(iniciada);
        var handler = new ListarPartidasPublicadasQueryHandler(repo);

        var lista = await handler.Handle(new ListarPartidasPublicadasQuery(), CancellationToken.None);

        var unica = Assert.Single(lista);
        Assert.Equal("Abierta", unica.Nombre);
        Assert.Equal("Individual", unica.Modalidad);
        Assert.Equal("Manual", unica.ModoInicioPartida);
        Assert.Equal(1, unica.InscritosActivos);
        Assert.Equal(1, unica.MinimosParticipacion);
        Assert.Equal(10, unica.MaximosParticipacion);
    }

    [Fact]
    public async Task Sin_sesiones_en_lobby_devuelve_lista_vacia()
    {
        var handler = new ListarPartidasPublicadasQueryHandler(new FakeSesionPartidaRepository());
        var lista = await handler.Handle(new ListarPartidasPublicadasQuery(), CancellationToken.None);
        Assert.Empty(lista);
    }
}
```

Nota: si `AplicarInicio` tiene otra firma (verificar en `SesionPartida.cs`), usar el método real que transiciona Lobby→Iniciada con los argumentos mínimos válidos; el objetivo del fixture es solo tener una sesión NO-Lobby en el repo.

- [ ] **Step 2: Escribir tests del controller (fallan)**

`SesionesControllerPartidasPublicadasTests.cs` (usa el `FakeSender` existente del proyecto de tests):

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Api.Controllers;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerPartidasPublicadasTests
{
    [Fact]
    public async Task Partidas_publicadas_devuelve_200_con_lista()
    {
        IReadOnlyList<PartidaPublicadaDto> dtos = new[]
        {
            new PartidaPublicadaDto(Guid.NewGuid(), "Copa", "Individual", "Manual", null, 1, 10, 3)
        };
        var sender = new FakeSender(dtos);
        var controller = new SesionesController(sender);

        var result = await controller.ListarPartidasPublicadas(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dtos, ok.Value);
        Assert.IsType<ListarPartidasPublicadasQuery>(sender.LastRequest);
    }
}
```

- [ ] **Step 3: Correr los tests, verificar que fallan**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests --filter "PartidasPublicadas" 2>&1 | tail -5`
Expected: FAIL de compilación (tipos no existen).

- [ ] **Step 4: Implementar**

`PartidaPublicadaDto.cs`:

```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record PartidaPublicadaDto(
    Guid PartidaId,
    string Nombre,
    string Modalidad,
    string ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    int InscritosActivos);
```

`ListarPartidasPublicadasQuery.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Queries;

public sealed record ListarPartidasPublicadasQuery() : IRequest<IReadOnlyList<PartidaPublicadaDto>>;
```

`ListarPartidasPublicadasQueryHandler.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ListarPartidasPublicadasQueryHandler
    : IRequestHandler<ListarPartidasPublicadasQuery, IReadOnlyList<PartidaPublicadaDto>>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ListarPartidasPublicadasQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<IReadOnlyList<PartidaPublicadaDto>> Handle(
        ListarPartidasPublicadasQuery request, CancellationToken cancellationToken)
    {
        var sesiones = await _sesiones.GetSesionesEnLobbyAsync(cancellationToken);
        return sesiones
            .Select(s => new PartidaPublicadaDto(
                s.PartidaId,
                s.Nombre,
                s.Modalidad.ToString(),
                s.ModoInicioPartida.ToString(),
                s.TiempoInicio,
                s.MinimosParticipacion,
                s.MaximosParticipacion,
                s.Inscripciones.Count(i => i.EsActiva)))
            .ToList();
    }
}
```

`ISesionPartidaRepository.cs` — añadir al final de la interface:

```csharp
    Task<IReadOnlyList<SesionPartida>> GetSesionesEnLobbyAsync(CancellationToken cancellationToken);
```

`SesionPartidaRepository.cs` — añadir implementación (junto a los otros métodos de lista):

```csharp
    public async Task<IReadOnlyList<SesionPartida>> GetSesionesEnLobbyAsync(CancellationToken cancellationToken)
        => await _dbContext.Sesiones
            .AsNoTracking()
            .Where(s => s.Estado == EstadoSesion.Lobby)
            // Solo Inscripciones: el listado cuenta activas; no necesita convocatorias ni juegos.
            .Include(s => s.Inscripciones)
            .ToListAsync(cancellationToken);
```

(Ajustar el nombre del DbSet si difiere — verificar cómo lo referencian los métodos vecinos del mismo archivo.)

`SesionesController.cs` — añadir junto a los otros GET compartidos (después de `ObtenerLobby`), **sin** atributo de policy (autenticado como lobby/estado):

```csharp
    [HttpGet("partidas-publicadas")]
    public async Task<IActionResult> ListarPartidasPublicadas(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ListarPartidasPublicadasQuery(), cancellationToken));
```

`FakeSesionPartidaRepository.cs` — implementar el método nuevo (el fake guarda sesiones en una lista interna; seguir su convención):

```csharp
    public Task<IReadOnlyList<SesionPartida>> GetSesionesEnLobbyAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<SesionPartida>>(
            _sesiones.Where(s => s.Estado == EstadoSesion.Lobby).ToList());
```

(Ajustar el nombre del campo interno al real del fake.)

- [ ] **Step 5: Correr unit tests**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests 2>&1 | tail -5`
Expected: PASS todos (los nuevos + los previos; el fake ahora compila con el método extra).

- [ ] **Step 6: Correr la solución completa**

Run: `dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln 2>&1 | tail -8`
Expected: PASS (unit + integration + contract).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/PartidaPublicadaDto.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Queries/ListarPartidasPublicadasQuery.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ListarPartidasPublicadasQueryHandler.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ListarPartidasPublicadasQueryHandlerTests.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerPartidasPublicadasTests.cs
git commit -m "feat(operaciones): GET partidas-publicadas listado participant-safe (bloque 2d)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Contrato — fila y shape en `operaciones-sesion-api.md`

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md`

**Interfaces:**
- Consumes: el shape de Task 1 (`PartidaPublicadaDto`).
- Produces: el contrato que Task 3 (mobile) transcribe.

- [ ] **Step 1: Añadir la fila al Endpoint Registry**

En la tabla de endpoints, junto a las lecturas compartidas (después de la fila "Session state"):

```markdown
| Partidas publicadas (descubrimiento) | GET | `/operaciones-sesion/partidas-publicadas` | Autenticado (cualquier rol; sin policy de permiso) | 200 + PartidaPublicadaDto[] (solo sesiones en `Lobby`; vacía si no hay) | 401 sin token |
```

- [ ] **Step 2: Añadir el shape en la sección de DTOs**

Junto a los otros DTOs de lectura:

```markdown
- `PartidaPublicadaDto { partidaId, nombre, modalidad, modoInicioPartida, tiempoInicio (nullable), minimosParticipacion, maximosParticipacion, inscritosActivos }` — listado participant-safe para el panel mobile (Bloque 2d): solo sesiones cuyo estado es `Lobby`; sin juegos, preguntas ni códigos QR. `inscritosActivos` cuenta inscripciones activas (participantes en Individual, equipos en Equipo).
```

- [ ] **Step 3: Actualizar la tabla de autorización**

En la fila "Lectura compartida", cambiar el conteo y añadir el endpoint: `lobby` (GET) · `estado` (GET) · `pregunta-actual` (GET) · `etapa-actual` (GET) · `partidas-publicadas` (GET) — y el "(4)" pasa a "(5)".

- [ ] **Step 4: Commit**

```bash
git add contracts/http/operaciones-sesion-api.md
git commit -m "docs(contracts): partidas-publicadas listado participant-safe (bloque 2d)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Mobile — API modules de participación

**Files:**
- Create: `mobile/src/features/partidas/partidasPublicadasApi.js`
- Create: `mobile/src/features/partidas/inscripcionApi.js`
- Create: `mobile/src/features/partidas/convocatoriasApi.js`
- Create: `mobile/src/features/partidas/miSesionApi.js`
- Test: `mobile/tests/partidasApis.test.js`

**Interfaces:**
- Consumes: contrato de Task 2 + endpoints existentes del contrato (`inscripciones`, `inscripciones-equipo`, `convocatorias/{id}/aceptacion|rechazo`, `mi-sesion`, `mis-convocatorias`).
- Produces (los flows/screens de Tasks 5-7 consumen exactamente estas firmas):
  - `getPartidasPublicadas(apiBaseUrl, token, fetchImpl?)` → `{ok: true, data: PartidaPublicada[]}` | `{ok: false, type, message}`
  - `inscribirse(apiBaseUrl, token, partidaId, fetchImpl?)` → `{ok: true, data}` | error
  - `cancelarInscripcion(apiBaseUrl, token, partidaId, fetchImpl?)` → `{ok: true}` | error
  - `preinscribirEquipo(apiBaseUrl, token, partidaId, fetchImpl?)` → `{ok: true, data}` | error
  - `cancelarPreinscripcionEquipo(apiBaseUrl, token, partidaId, fetchImpl?)` → `{ok: true}` | error
  - `getMisConvocatorias(apiBaseUrl, token, fetchImpl?)` → `{ok: true, data: ConvocatoriaPendiente[]}` | error
  - `aceptarConvocatoria(apiBaseUrl, token, convocatoriaId, fetchImpl?)` / `rechazarConvocatoria(...)` → `{ok: true, data}` | error
  - `getMiSesion(apiBaseUrl, token, fetchImpl?)` → `{ok: true, sesion}` con `sesion: null` si 204 | error

- [ ] **Step 1: Escribir tests (fallan)**

`mobile/tests/partidasApis.test.js`:

```js
const test = require("node:test");
const assert = require("node:assert/strict");

const { getPartidasPublicadas } = require("../src/features/partidas/partidasPublicadasApi.js");
const {
  inscribirse,
  cancelarInscripcion,
  preinscribirEquipo,
} = require("../src/features/partidas/inscripcionApi.js");
const { getMisConvocatorias, aceptarConvocatoria } = require("../src/features/partidas/convocatoriasApi.js");
const { getMiSesion } = require("../src/features/partidas/miSesionApi.js");

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("getPartidasPublicadas hace GET autenticado y devuelve data", async () => {
  const calls = [];
  const fetchImpl = async (url, init) => {
    calls.push({ url, init });
    return jsonResponse(200, [{ partidaId: "p1", nombre: "Copa", modalidad: "Individual" }]);
  };
  const r = await getPartidasPublicadas("http://gw", "tok", fetchImpl);
  assert.equal(r.ok, true);
  assert.equal(r.data[0].nombre, "Copa");
  assert.equal(calls[0].url, "http://gw/operaciones-sesion/partidas-publicadas");
  assert.equal(calls[0].init.headers.Authorization, "Bearer tok");
});

test("getPartidasPublicadas mapea fallo de red", async () => {
  const fetchImpl = async () => {
    throw new Error("boom");
  };
  const r = await getPartidasPublicadas("http://gw", "tok", fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "network");
});

test("inscribirse POST correcto y mapea 409 a conflict con mensaje del backend", async () => {
  const okImpl = async (url, init) => {
    assert.equal(url, "http://gw/operaciones-sesion/partidas/p1/inscripciones");
    assert.equal(init.method, "POST");
    return jsonResponse(201, { inscripcionId: "i1" });
  };
  const r1 = await inscribirse("http://gw", "tok", "p1", okImpl);
  assert.equal(r1.ok, true);
  assert.equal(r1.data.inscripcionId, "i1");

  const conflictImpl = async () => jsonResponse(409, { message: "Ya tienes una participacion activa." });
  const r2 = await inscribirse("http://gw", "tok", "p1", conflictImpl);
  assert.equal(r2.ok, false);
  assert.equal(r2.type, "conflict");
  assert.equal(r2.message, "Ya tienes una participacion activa.");
});

test("cancelarInscripcion DELETE a inscripciones/mia", async () => {
  const calls = [];
  const fetchImpl = async (url, init) => {
    calls.push({ url, init });
    return { ok: true, status: 204, json: async () => ({}) };
  };
  const r = await cancelarInscripcion("http://gw", "tok", "p1", fetchImpl);
  assert.equal(r.ok, true);
  assert.equal(calls[0].url, "http://gw/operaciones-sesion/partidas/p1/inscripciones/mia");
  assert.equal(calls[0].init.method, "DELETE");
});

test("preinscribirEquipo mapea 403 no-lider", async () => {
  const fetchImpl = async () => jsonResponse(403, { message: "Solo el lider puede preinscribir." });
  const r = await preinscribirEquipo("http://gw", "tok", "p1", fetchImpl);
  assert.equal(r.ok, false);
  assert.equal(r.type, "forbidden");
});

test("getMisConvocatorias GET y aceptarConvocatoria POST", async () => {
  const listImpl = async (url) => {
    assert.equal(url, "http://gw/operaciones-sesion/mis-convocatorias");
    return jsonResponse(200, [{ convocatoriaId: "c1", partidaId: "p1", equipoId: "e1" }]);
  };
  const r1 = await getMisConvocatorias("http://gw", "tok", listImpl);
  assert.equal(r1.ok, true);
  assert.equal(r1.data.length, 1);

  const acceptImpl = async (url, init) => {
    assert.equal(url, "http://gw/operaciones-sesion/convocatorias/c1/aceptacion");
    assert.equal(init.method, "POST");
    return jsonResponse(200, { estado: "Aceptada" });
  };
  const r2 = await aceptarConvocatoria("http://gw", "tok", "c1", acceptImpl);
  assert.equal(r2.ok, true);
});

test("getMiSesion 200 devuelve sesion y 204 devuelve null", async () => {
  const conSesion = async () => jsonResponse(200, { partidaId: "p1", estadoPartida: "Lobby" });
  const r1 = await getMiSesion("http://gw", "tok", conSesion);
  assert.equal(r1.ok, true);
  assert.equal(r1.sesion.partidaId, "p1");

  const sinSesion = async () => ({ ok: true, status: 204, json: async () => ({}) });
  const r2 = await getMiSesion("http://gw", "tok", sinSesion);
  assert.equal(r2.ok, true);
  assert.equal(r2.sesion, null);
});
```

- [ ] **Step 2: Correr tests, verificar que fallan**

Run: `cd mobile && node --test tests/partidasApis.test.js`
Expected: FAIL (módulos no existen).

- [ ] **Step 3: Implementar los 4 módulos**

Los cuatro comparten dos helpers privados por archivo NO — un solo helper compartido: crear los 4 archivos donde `partidasPublicadasApi.js` define y exporta también el helper de errores comunes reutilizado vía import por los otros tres.

`mobile/src/features/partidas/partidasPublicadasApi.js`:

```js
// Errores comunes de los endpoints de participacion (Bloque 2d).
export function mapCommonError(status, body) {
  if (status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }
  if (status === 403) {
    return { ok: false, type: "forbidden", message: body?.message || "No tienes permiso para esta accion." };
  }
  if (status === 404) {
    return { ok: false, type: "not_found", message: body?.message || "La partida no existe o no esta publicada." };
  }
  if (status === 409) {
    return { ok: false, type: "conflict", message: body?.message || "La accion entra en conflicto con el estado actual." };
  }
  return { ok: false, type: "error", message: body?.message || "Ocurrio un error inesperado." };
}

export const networkError = () => ({
  ok: false,
  type: "network",
  message: "No se pudo conectar con el servidor. Verifica tu conexion e intenta de nuevo.",
});

export async function getPartidasPublicadas(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/operaciones-sesion/partidas-publicadas`, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, data: body ?? [] };
}
```

`mobile/src/features/partidas/inscripcionApi.js`:

```js
import { mapCommonError, networkError } from "./partidasPublicadasApi.js";

async function send(apiBaseUrl, token, path, method, fetchImpl) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}${path}`, {
      method,
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return networkError();
  }
  if (response.status === 204) {
    return { ok: true };
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, data: body };
}

export function inscribirse(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  return send(apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/inscripciones`, "POST", fetchImpl);
}

export function cancelarInscripcion(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  return send(apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/inscripciones/mia`, "DELETE", fetchImpl);
}

export function preinscribirEquipo(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  return send(apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/inscripciones-equipo`, "POST", fetchImpl);
}

export function cancelarPreinscripcionEquipo(apiBaseUrl, token, partidaId, fetchImpl = fetch) {
  return send(apiBaseUrl, token, `/operaciones-sesion/partidas/${partidaId}/inscripciones-equipo/mia`, "DELETE", fetchImpl);
}
```

`mobile/src/features/partidas/convocatoriasApi.js`:

```js
import { mapCommonError, networkError } from "./partidasPublicadasApi.js";

export async function getMisConvocatorias(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/operaciones-sesion/mis-convocatorias`, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, data: body ?? [] };
}

async function responder(apiBaseUrl, token, convocatoriaId, accion, fetchImpl) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/operaciones-sesion/convocatorias/${convocatoriaId}/${accion}`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, data: body };
}

export function aceptarConvocatoria(apiBaseUrl, token, convocatoriaId, fetchImpl = fetch) {
  return responder(apiBaseUrl, token, convocatoriaId, "aceptacion", fetchImpl);
}

export function rechazarConvocatoria(apiBaseUrl, token, convocatoriaId, fetchImpl = fetch) {
  return responder(apiBaseUrl, token, convocatoriaId, "rechazo", fetchImpl);
}
```

`mobile/src/features/partidas/miSesionApi.js`:

```js
import { mapCommonError, networkError } from "./partidasPublicadasApi.js";

export async function getMiSesion(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/operaciones-sesion/mi-sesion`, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return networkError();
  }
  if (response.status === 204) {
    return { ok: true, sesion: null };
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, sesion: body };
}
```

Nota ESM/CJS: los tests del repo cargan módulos `src` con `require` (node --test los transpila vía la config existente — verificar cómo lo hacen `invitationsFlow.test.js` y compañía y usar EXACTAMENTE el mismo mecanismo de import en el test; si los tests existentes usan `import` estático o `require` de ESM, copiar ese patrón).

- [ ] **Step 4: Correr tests**

Run: `cd mobile && node --test tests/partidasApis.test.js`
Expected: PASS 7/7.

- [ ] **Step 5: Suite completa + typecheck**

Run: `cd mobile && npm test && npm run typecheck`
Expected: verde.

- [ ] **Step 6: Commit**

```bash
cd mobile && git add src/features/partidas/partidasPublicadasApi.js src/features/partidas/inscripcionApi.js src/features/partidas/convocatoriasApi.js src/features/partidas/miSesionApi.js tests/partidasApis.test.js
git commit -m "feat(mobile): api modules de participacion — publicadas, inscripciones, convocatorias, mi-sesion (bloque 2d)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Mobile — `sesionHub` + dep `@microsoft/signalr`

**Files:**
- Modify: `mobile/package.json` (dep nueva vía `npm install @microsoft/signalr`)
- Create: `mobile/src/features/partidas/sesionHub.js`
- Test: `mobile/tests/sesionHub.test.js`

**Interfaces:**
- Consumes: `mobileEnv.gatewayApiBaseUrl` (lo inyecta el caller — el módulo NO importa env).
- Produces:
  - `sesionHubUrl(gatewayBaseUrl)` → `"{base sin / final}/operaciones-sesion/hubs/sesion"`
  - `crearSesionHub(gatewayBaseUrl, accessToken)` → `HubConnection` (sin arrancar; el caller hace `start()`/`invoke("SuscribirAPartida", partidaId)`/`on(...)`/`stop()`)

- [ ] **Step 1: Instalar la dep**

Run: `cd mobile && npm install @microsoft/signalr`
Expected: añade `@microsoft/signalr` a dependencies (verificar versión instalada en package.json; cualquier 8.x/9.x sirve — es la misma lib que usa el frontend web).

- [ ] **Step 2: Test de la URL (falla)**

`mobile/tests/sesionHub.test.js`:

```js
const test = require("node:test");
const assert = require("node:assert/strict");
const { sesionHubUrl } = require("../src/features/partidas/sesionHub.js");

test("sesionHubUrl arma la URL del hub sin doble slash", () => {
  assert.equal(sesionHubUrl("http://gw:5080"), "http://gw:5080/operaciones-sesion/hubs/sesion");
  assert.equal(sesionHubUrl("http://gw:5080/"), "http://gw:5080/operaciones-sesion/hubs/sesion");
});
```

(Mismo mecanismo de import que el resto de los tests mobile — ver nota de Task 3.)

- [ ] **Step 3: Correr test, verificar que falla**

Run: `cd mobile && node --test tests/sesionHub.test.js`
Expected: FAIL (módulo no existe).

- [ ] **Step 4: Implementar `sesionHub.js`**

```js
// Hub de sesion via gateway (mismo hub que la web). El caller arranca/detiene la conexion.
import { HubConnectionBuilder } from "@microsoft/signalr";

export function sesionHubUrl(gatewayBaseUrl) {
  return `${gatewayBaseUrl.replace(/\/$/, "")}/operaciones-sesion/hubs/sesion`;
}

export function crearSesionHub(gatewayBaseUrl, accessToken) {
  return new HubConnectionBuilder()
    .withUrl(sesionHubUrl(gatewayBaseUrl), { accessTokenFactory: () => accessToken })
    .withAutomaticReconnect()
    .build();
}
```

- [ ] **Step 5: Correr test + typecheck**

Run: `cd mobile && node --test tests/sesionHub.test.js && npm run typecheck`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
cd mobile && git add package.json package-lock.json src/features/partidas/sesionHub.js tests/sesionHub.test.js
git commit -m "feat(mobile): sesionHub signalr via gateway (bloque 2d)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: Mobile — PartidasPanelScreen (listado + filtro + banner mi-sesión)

**Files:**
- Create: `mobile/src/features/partidas/partidasPanelFlow.js`
- Create: `mobile/src/features/partidas/PartidasPanelScreen.tsx`
- Create: `mobile/src/features/partidas/PartidasPanelScreenContainer.tsx`
- Modify: `mobile/src/navigation/types.ts` (rutas nuevas)
- Modify: `mobile/src/navigation/RootNavigator.tsx` (registrar pantalla)
- Test: `mobile/tests/partidasPanelFlow.test.js`

**Interfaces:**
- Consumes: `getPartidasPublicadas`, `getMiSesion` (Task 3).
- Produces:
  - `cargarPanel({apiBaseUrl, token, fetchImpl})` → `{ok: true, partidas, miSesion}` (miSesion `null` si no hay) | `{ok: false, message}`
  - `filtrarPorModalidad(partidas, filtro)` → array (filtro `"Todas" | "Individual" | "Equipo"`)
  - Ruta de navegación `PartidasPanel: undefined`; navega a `PartidaLobby: { partidaId: string; nombre: string }` (Task 6 la registra) y a `Convocatorias: undefined` (Task 7).

- [ ] **Step 1: Test del flow (falla)**

`mobile/tests/partidasPanelFlow.test.js`:

```js
const test = require("node:test");
const assert = require("node:assert/strict");
const { cargarPanel, filtrarPorModalidad } = require("../src/features/partidas/partidasPanelFlow.js");

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("cargarPanel combina listado + mi-sesion", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/partidas-publicadas")) {
      return jsonResponse(200, [{ partidaId: "p1", nombre: "Copa", modalidad: "Individual" }]);
    }
    if (url.endsWith("/mi-sesion")) {
      return jsonResponse(200, { partidaId: "p9", estadoPartida: "Lobby" });
    }
    throw new Error(`URL inesperada: ${url}`);
  };
  const r = await cargarPanel({ apiBaseUrl: "http://gw", token: "tok", fetchImpl });
  assert.equal(r.ok, true);
  assert.equal(r.partidas.length, 1);
  assert.equal(r.miSesion.partidaId, "p9");
});

test("cargarPanel con listado caido reporta error pero mi-sesion 204 no bloquea", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/partidas-publicadas")) {
      return jsonResponse(500, { message: "boom" });
    }
    return { ok: true, status: 204, json: async () => ({}) };
  };
  const r = await cargarPanel({ apiBaseUrl: "http://gw", token: "tok", fetchImpl });
  assert.equal(r.ok, false);
});

test("filtrarPorModalidad", () => {
  const partidas = [
    { partidaId: "a", modalidad: "Individual" },
    { partidaId: "b", modalidad: "Equipo" },
  ];
  assert.equal(filtrarPorModalidad(partidas, "Todas").length, 2);
  assert.deepEqual(filtrarPorModalidad(partidas, "Equipo").map((p) => p.partidaId), ["b"]);
});
```

- [ ] **Step 2: Correr test, verificar que falla**

Run: `cd mobile && node --test tests/partidasPanelFlow.test.js`
Expected: FAIL.

- [ ] **Step 3: Implementar `partidasPanelFlow.js`**

```js
import { getPartidasPublicadas } from "./partidasPublicadasApi.js";
import { getMiSesion } from "./miSesionApi.js";

export async function cargarPanel({ apiBaseUrl, token, fetchImpl }) {
  const [listado, miSesion] = await Promise.all([
    getPartidasPublicadas(apiBaseUrl, token, fetchImpl ?? fetch),
    getMiSesion(apiBaseUrl, token, fetchImpl ?? fetch),
  ]);
  if (!listado.ok) {
    return { ok: false, message: listado.message };
  }
  // mi-sesion caida no bloquea el panel: banner simplemente no aparece.
  return { ok: true, partidas: listado.data, miSesion: miSesion.ok ? miSesion.sesion : null };
}

export function filtrarPorModalidad(partidas, filtro) {
  if (filtro === "Todas") {
    return partidas;
  }
  return partidas.filter((p) => p.modalidad === filtro);
}
```

- [ ] **Step 4: Implementar screen + container**

`PartidasPanelScreen.tsx` (usa `Button`, `Card`, `Notice`, `ScreenHeader` de `../../shared/ui` y theme, como `InvitationsScreen`):

```tsx
import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, View } from "react-native";
import { Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, spacing, typography } from "../../shared/theme";
import { AppText } from "../../shared/ui";
import { cargarPanel, filtrarPorModalidad } from "./partidasPanelFlow.js";

type PartidaPublicada = {
  partidaId: string;
  nombre: string;
  modalidad: "Individual" | "Equipo";
  modoInicioPartida: string;
  tiempoInicio: string | null;
  minimosParticipacion: number;
  maximosParticipacion: number;
  inscritosActivos: number;
};

type MiSesion = { partidaId: string; estadoPartida: string } | null;

type Filtro = "Todas" | "Individual" | "Equipo";
const FILTROS: Filtro[] = ["Todas", "Individual", "Equipo"];

type Props = {
  apiBaseUrl: string;
  token: string;
  onOpenPartida: (partida: { partidaId: string; nombre: string }) => void;
};

export function PartidasPanelScreen({ apiBaseUrl, token, onOpenPartida }: Props) {
  const [partidas, setPartidas] = useState<PartidaPublicada[]>([]);
  const [miSesion, setMiSesion] = useState<MiSesion>(null);
  const [filtro, setFiltro] = useState<Filtro>("Todas");
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setErrorMessage(null);
    const result = await cargarPanel({ apiBaseUrl, token, fetchImpl: undefined });
    if (!result.ok) {
      setErrorMessage(result.message ?? "No se pudieron cargar las partidas.");
      return;
    }
    setPartidas(result.partidas as PartidaPublicada[]);
    setMiSesion(result.miSesion as MiSesion);
  }, [apiBaseUrl, token]);

  useEffect(() => {
    (async () => {
      setLoading(true);
      await load();
      setLoading(false);
    })();
  }, [load]);

  async function onRefresh() {
    setRefreshing(true);
    await load();
    setRefreshing(false);
  }

  const visibles = filtrarPorModalidad(partidas, filtro) as PartidaPublicada[];

  return (
    <ScrollView
      style={styles.container}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => void onRefresh()} />}
    >
      <ScreenHeader title="Partidas" subtitle="Únete a una partida publicada" />
      {errorMessage ? <Notice tone="error">{errorMessage}</Notice> : null}
      {miSesion ? (
        <Pressable
          accessibilityLabel="Ir a mi participación activa"
          onPress={() => onOpenPartida({ partidaId: miSesion.partidaId, nombre: "Mi partida" })}
        >
          <Notice tone="info">Tienes una participación activa. Toca para volver a tu partida.</Notice>
        </Pressable>
      ) : null}
      <View style={styles.filtros}>
        {FILTROS.map((f) => (
          <Button key={f} variant={f === filtro ? "primary" : "secondary"} onPress={() => setFiltro(f)}>
            {f}
          </Button>
        ))}
      </View>
      {loading ? <ActivityIndicator style={styles.spinner} /> : null}
      {!loading && visibles.length === 0 ? (
        <AppText style={styles.empty}>No hay partidas publicadas ahora mismo.</AppText>
      ) : null}
      {visibles.map((p) => (
        <Pressable key={p.partidaId} onPress={() => onOpenPartida({ partidaId: p.partidaId, nombre: p.nombre })}>
          <Card>
            <AppText variant="bodyStrong">{p.nombre}</AppText>
            <AppText>
              {p.modalidad} · {p.inscritosActivos}/{p.maximosParticipacion} · min {p.minimosParticipacion}
            </AppText>
            <AppText>
              Inicio {p.modoInicioPartida}
              {p.tiempoInicio ? ` — ${new Date(p.tiempoInicio).toLocaleTimeString()}` : ""}
            </AppText>
          </Card>
        </Pressable>
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#f4f7fb" },
  filtros: { flexDirection: "row", gap: spacing.sm, margin: spacing.md },
  spinner: { marginTop: spacing.lg },
  empty: { margin: spacing.md, color: colors.muted ?? "#6b7280" },
});
```

**IMPORTANTE:** verificar los exports reales de `../../shared/ui` (`Button`, `Card`, `Notice`, `ScreenHeader`, `AppText`) y sus props (`variant`, `tone`) leyendo `mobile/src/shared/ui/index.ts` (o el archivo equivalente) ANTES de escribir el JSX; ajustar a la API real del design system del repo manteniendo la estructura y los textos de esta pantalla. Igual con `colors/spacing/typography`.

`PartidasPanelScreenContainer.tsx` (patrón exacto de `InvitationsScreenContainer`):

```tsx
import React from "react";
import { Text } from "react-native";
import { useNavigation } from "@react-navigation/native";
import type { NativeStackNavigationProp } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { PartidasPanelScreen } from "./PartidasPanelScreen";

export function PartidasPanelScreenContainer() {
  const { session } = useAuth();
  const navigation = useNavigation<NativeStackNavigationProp<AppStackParamList>>();

  if (!session) {
    return <Text>Sesion no disponible.</Text>;
  }

  return (
    <PartidasPanelScreen
      apiBaseUrl={mobileEnv.gatewayApiBaseUrl}
      token={session.token}
      onOpenPartida={({ partidaId, nombre }) => navigation.navigate("PartidaLobby", { partidaId, nombre })}
    />
  );
}
```

- [ ] **Step 5: Registrar navegación**

`navigation/types.ts` — añadir a `AppStackParamList` (las rutas legacy se retiran en Task 8; aquí solo se AÑADE):

```ts
  PartidasPanel: undefined;
  PartidaLobby: { partidaId: string; nombre: string };
  Convocatorias: undefined;
```

`RootNavigator.tsx` — import + screen (las pantallas de Tasks 6-7 se registran en sus tareas; aquí solo PartidasPanel):

```tsx
import { PartidasPanelScreenContainer } from "../features/partidas/PartidasPanelScreenContainer";
// dentro del AppStack.Navigator:
<AppStack.Screen name="PartidasPanel" component={PartidasPanelScreenContainer} options={{ title: "Partidas" }} />
```

Nota: `PartidaLobby` y `Convocatorias` quedan tipadas en types.ts pero sin `<AppStack.Screen>` hasta Tasks 6-7 — `tsc --noEmit` no exige que toda ruta tipada esté registrada, y el `navigation.navigate("PartidaLobby", …)` compila contra el tipo.

- [ ] **Step 6: Correr tests + typecheck**

Run: `cd mobile && npm test && npm run typecheck`
Expected: verde (tests nuevos del flow + suite previa).

- [ ] **Step 7: Commit**

```bash
cd mobile && git add src/features/partidas/partidasPanelFlow.js src/features/partidas/PartidasPanelScreen.tsx src/features/partidas/PartidasPanelScreenContainer.tsx src/navigation/types.ts src/navigation/RootNavigator.tsx tests/partidasPanelFlow.test.js
git commit -m "feat(mobile): PartidasPanelScreen listado + filtro + banner mi-sesion (bloque 2d)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Mobile — PartidaLobbyScreen (acciones por modalidad + SignalR)

**Files:**
- Create: `mobile/src/features/partidas/partidaLobbyFlow.js`
- Create: `mobile/src/features/partidas/PartidaLobbyScreen.tsx`
- Create: `mobile/src/features/partidas/PartidaLobbyScreenContainer.tsx`
- Modify: `mobile/src/navigation/RootNavigator.tsx` (registrar pantalla)
- Test: `mobile/tests/partidaLobbyFlow.test.js`

**Interfaces:**
- Consumes: `inscribirse`/`cancelarInscripcion`/`preinscribirEquipo`/`cancelarPreinscripcionEquipo` (Task 3), `getMiSesion` (Task 3), `crearSesionHub` (Task 4), `GET /operaciones-sesion/partidas/{id}/lobby` (endpoint existente — el flow lo llama directo).
- Produces:
  - `cargarLobby({apiBaseUrl, token, partidaId, fetchImpl})` → `{ok: true, lobby, inscrito}` (`inscrito` = boolean: mi-sesión activa apunta a ESTA partida) | `{ok: false, type, message}`
  - `accionParticipacion({apiBaseUrl, token, partidaId, modalidad, inscrito, fetchImpl})` → despacha inscribir/cancelar según modalidad+estado y devuelve el result object del api module.

- [ ] **Step 1: Test del flow (falla)**

`mobile/tests/partidaLobbyFlow.test.js`:

```js
const test = require("node:test");
const assert = require("node:assert/strict");
const { cargarLobby, accionParticipacion } = require("../src/features/partidas/partidaLobbyFlow.js");

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("cargarLobby trae lobby y marca inscrito si mi-sesion apunta a la partida", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/lobby")) {
      return jsonResponse(200, { partidaId: "p1", estado: "Lobby", modalidad: "Individual", inscritosActivos: 2 });
    }
    if (url.endsWith("/mi-sesion")) {
      return jsonResponse(200, { partidaId: "p1", estadoPartida: "Lobby" });
    }
    throw new Error(`URL inesperada: ${url}`);
  };
  const r = await cargarLobby({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl });
  assert.equal(r.ok, true);
  assert.equal(r.lobby.inscritosActivos, 2);
  assert.equal(r.inscrito, true);
});

test("cargarLobby con mi-sesion en otra partida marca inscrito false", async () => {
  const fetchImpl = async (url) => {
    if (url.endsWith("/lobby")) {
      return jsonResponse(200, { partidaId: "p1", estado: "Lobby", modalidad: "Individual", inscritosActivos: 0 });
    }
    return jsonResponse(200, { partidaId: "OTRA", estadoPartida: "Lobby" });
  };
  const r = await cargarLobby({ apiBaseUrl: "http://gw", token: "tok", partidaId: "p1", fetchImpl });
  assert.equal(r.inscrito, false);
});

test("accionParticipacion Individual no inscrito hace POST inscripciones", async () => {
  const calls = [];
  const fetchImpl = async (url, init) => {
    calls.push({ url, method: init.method });
    return jsonResponse(201, { inscripcionId: "i1" });
  };
  const r = await accionParticipacion({
    apiBaseUrl: "http://gw", token: "tok", partidaId: "p1",
    modalidad: "Individual", inscrito: false, fetchImpl,
  });
  assert.equal(r.ok, true);
  assert.deepEqual(calls, [{ url: "http://gw/operaciones-sesion/partidas/p1/inscripciones", method: "POST" }]);
});

test("accionParticipacion Equipo inscrito hace DELETE inscripciones-equipo/mia", async () => {
  const calls = [];
  const fetchImpl = async (url, init) => {
    calls.push({ url, method: init.method });
    return { ok: true, status: 204, json: async () => ({}) };
  };
  const r = await accionParticipacion({
    apiBaseUrl: "http://gw", token: "tok", partidaId: "p1",
    modalidad: "Equipo", inscrito: true, fetchImpl,
  });
  assert.equal(r.ok, true);
  assert.deepEqual(calls, [{ url: "http://gw/operaciones-sesion/partidas/p1/inscripciones-equipo/mia", method: "DELETE" }]);
});
```

- [ ] **Step 2: Correr test, verificar que falla**

Run: `cd mobile && node --test tests/partidaLobbyFlow.test.js`
Expected: FAIL.

- [ ] **Step 3: Implementar `partidaLobbyFlow.js`**

```js
import { mapCommonError, networkError } from "./partidasPublicadasApi.js";
import {
  inscribirse,
  cancelarInscripcion,
  preinscribirEquipo,
  cancelarPreinscripcionEquipo,
} from "./inscripcionApi.js";
import { getMiSesion } from "./miSesionApi.js";

export async function cargarLobby({ apiBaseUrl, token, partidaId, fetchImpl }) {
  const f = fetchImpl ?? fetch;
  let response;
  try {
    response = await f(`${apiBaseUrl}/operaciones-sesion/partidas/${partidaId}/lobby`, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  const mia = await getMiSesion(apiBaseUrl, token, f);
  const inscrito = mia.ok && mia.sesion != null && mia.sesion.partidaId === partidaId;
  return { ok: true, lobby: body, inscrito };
}

export function accionParticipacion({ apiBaseUrl, token, partidaId, modalidad, inscrito, fetchImpl }) {
  const f = fetchImpl ?? fetch;
  if (modalidad === "Equipo") {
    return inscrito
      ? cancelarPreinscripcionEquipo(apiBaseUrl, token, partidaId, f)
      : preinscribirEquipo(apiBaseUrl, token, partidaId, f);
  }
  return inscrito
    ? cancelarInscripcion(apiBaseUrl, token, partidaId, f)
    : inscribirse(apiBaseUrl, token, partidaId, f);
}
```

- [ ] **Step 4: Implementar screen + container**

`PartidaLobbyScreen.tsx` (misma nota que Task 5: verificar la API real de `shared/ui` antes de escribir el JSX):

```tsx
import React, { useCallback, useEffect, useRef, useState } from "react";
import { ActivityIndicator, ScrollView, StyleSheet } from "react-native";
import { Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { AppText } from "../../shared/ui";
import { spacing } from "../../shared/theme";
import { cargarLobby, accionParticipacion } from "./partidaLobbyFlow.js";
import { crearSesionHub } from "./sesionHub.js";

type Lobby = {
  partidaId: string;
  estado: string;
  modalidad: "Individual" | "Equipo";
  minimosParticipacion: number;
  maximosParticipacion: number;
  inscritosActivos: number;
};

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  nombre: string;
};

type Aviso = { tone: "info" | "error" | "success"; texto: string } | null;

export function PartidaLobbyScreen({ apiBaseUrl, token, partidaId, nombre }: Props) {
  const [lobby, setLobby] = useState<Lobby | null>(null);
  const [inscrito, setInscrito] = useState(false);
  const [loading, setLoading] = useState(true);
  const [posting, setPosting] = useState(false);
  const [aviso, setAviso] = useState<Aviso>(null);

  const load = useCallback(async () => {
    const r = await cargarLobby({ apiBaseUrl, token, partidaId, fetchImpl: undefined });
    if (!r.ok) {
      setAviso({ tone: "error", texto: r.message ?? "No se pudo cargar el lobby." });
      return;
    }
    setLobby(r.lobby as Lobby);
    setInscrito(r.inscrito as boolean);
  }, [apiBaseUrl, token, partidaId]);

  useEffect(() => {
    (async () => {
      setLoading(true);
      await load();
      setLoading(false);
    })();
  }, [load]);

  // Hub: refetch en EnLobby, avisos terminales en Iniciada/Cancelada.
  const loadRef = useRef(load);
  loadRef.current = load;
  useEffect(() => {
    const hub = crearSesionHub(apiBaseUrl, token);
    hub.on("PartidaEnLobby", () => void loadRef.current());
    hub.on("PartidaIniciada", () => setAviso({ tone: "success", texto: "La partida comenzó." }));
    hub.on("PartidaCancelada", (p: { motivo?: string }) =>
      setAviso({ tone: "error", texto: p?.motivo ? `Partida cancelada: ${p.motivo}` : "Partida cancelada." })
    );
    hub
      .start()
      .then(() => hub.invoke("SuscribirAPartida", partidaId))
      .catch(() => setAviso({ tone: "info", texto: "Sin conexión en vivo; usa recargar." }));
    return () => {
      void hub.stop().catch(() => {});
    };
  }, [apiBaseUrl, token, partidaId]);

  async function onAccion() {
    if (!lobby) return;
    setPosting(true);
    setAviso(null);
    const r = await accionParticipacion({
      apiBaseUrl, token, partidaId, modalidad: lobby.modalidad, inscrito, fetchImpl: undefined,
    });
    setPosting(false);
    if (!r.ok) {
      setAviso({ tone: "error", texto: r.message ?? "No se pudo completar la acción." });
      return;
    }
    setAviso({ tone: "success", texto: inscrito ? "Participación cancelada." : "¡Listo! Estás dentro." });
    await load();
  }

  const labelAccion = lobby?.modalidad === "Equipo"
    ? (inscrito ? "Cancelar preinscripción del equipo" : "Preinscribir mi equipo")
    : (inscrito ? "Cancelar mi inscripción" : "Inscribirme");

  return (
    <ScrollView style={styles.container}>
      <ScreenHeader title={nombre} subtitle="Lobby de la partida" />
      {aviso ? <Notice tone={aviso.tone}>{aviso.texto}</Notice> : null}
      {loading ? <ActivityIndicator style={styles.spinner} /> : null}
      {lobby ? (
        <Card>
          <AppText variant="bodyStrong">{lobby.modalidad}</AppText>
          <AppText>
            Inscritos: {lobby.inscritosActivos} / max {lobby.maximosParticipacion} (min {lobby.minimosParticipacion})
          </AppText>
          <Button onPress={() => void onAccion()} disabled={posting}>
            {labelAccion}
          </Button>
          <Button variant="secondary" onPress={() => void load()} disabled={posting}>
            Recargar
          </Button>
        </Card>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#f4f7fb" },
  spinner: { marginTop: spacing.lg },
});
```

`PartidaLobbyScreenContainer.tsx`:

```tsx
import React from "react";
import { Text } from "react-native";
import { RouteProp, useRoute } from "@react-navigation/native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { PartidaLobbyScreen } from "./PartidaLobbyScreen";

export function PartidaLobbyScreenContainer() {
  const { session } = useAuth();
  const route = useRoute<RouteProp<AppStackParamList, "PartidaLobby">>();

  if (!session) {
    return <Text>Sesion no disponible.</Text>;
  }

  return (
    <PartidaLobbyScreen
      apiBaseUrl={mobileEnv.gatewayApiBaseUrl}
      token={session.token}
      partidaId={route.params.partidaId}
      nombre={route.params.nombre}
    />
  );
}
```

`RootNavigator.tsx`:

```tsx
import { PartidaLobbyScreenContainer } from "../features/partidas/PartidaLobbyScreenContainer";
// dentro del AppStack.Navigator:
<AppStack.Screen name="PartidaLobby" component={PartidaLobbyScreenContainer} options={{ title: "Lobby" }} />
```

- [ ] **Step 5: Correr tests + typecheck**

Run: `cd mobile && npm test && npm run typecheck`
Expected: verde.

- [ ] **Step 6: Commit**

```bash
cd mobile && git add src/features/partidas/partidaLobbyFlow.js src/features/partidas/PartidaLobbyScreen.tsx src/features/partidas/PartidaLobbyScreenContainer.tsx src/navigation/RootNavigator.tsx tests/partidaLobbyFlow.test.js
git commit -m "feat(mobile): PartidaLobbyScreen acciones por modalidad + push signalr (bloque 2d)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 7: Mobile — ConvocatoriasScreen + Home

**Files:**
- Create: `mobile/src/features/partidas/convocatoriasFlow.js`
- Create: `mobile/src/features/partidas/ConvocatoriasScreen.tsx`
- Create: `mobile/src/features/partidas/ConvocatoriasScreenContainer.tsx`
- Modify: `mobile/src/navigation/RootNavigator.tsx` (registrar pantalla)
- Modify: `mobile/src/screens/HomeScreen.tsx` (NavCards nuevos)
- Test: `mobile/tests/convocatoriasFlow.test.js`

**Interfaces:**
- Consumes: `getMisConvocatorias`/`aceptarConvocatoria`/`rechazarConvocatoria` (Task 3).
- Produces: `fetchConvocatorias({apiBaseUrl, token, fetchImpl})`, `responderConvocatoria({apiBaseUrl, token, convocatoriaId, aceptar, fetchImpl})` (wrappers estilo `invitationsFlow`); ruta `Convocatorias` registrada.

- [ ] **Step 1: Test del flow (falla)**

`mobile/tests/convocatoriasFlow.test.js`:

```js
const test = require("node:test");
const assert = require("node:assert/strict");
const { fetchConvocatorias, responderConvocatoria } = require("../src/features/partidas/convocatoriasFlow.js");

const jsonResponse = (status, body) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => body,
});

test("fetchConvocatorias devuelve data", async () => {
  const fetchImpl = async () => jsonResponse(200, [{ convocatoriaId: "c1", partidaId: "p1", equipoId: "e1" }]);
  const r = await fetchConvocatorias({ apiBaseUrl: "http://gw", token: "tok", fetchImpl });
  assert.equal(r.ok, true);
  assert.equal(r.data[0].convocatoriaId, "c1");
});

test("responderConvocatoria aceptar=false hace POST rechazo y mapea 409", async () => {
  const calls = [];
  const fetchImpl = async (url, init) => {
    calls.push({ url, method: init.method });
    return jsonResponse(409, { message: "La partida ya no esta en lobby." });
  };
  const r = await responderConvocatoria({
    apiBaseUrl: "http://gw", token: "tok", convocatoriaId: "c1", aceptar: false, fetchImpl,
  });
  assert.equal(r.ok, false);
  assert.equal(r.type, "conflict");
  assert.deepEqual(calls, [{ url: "http://gw/operaciones-sesion/convocatorias/c1/rechazo", method: "POST" }]);
});
```

- [ ] **Step 2: Correr test, verificar que falla**

Run: `cd mobile && node --test tests/convocatoriasFlow.test.js`
Expected: FAIL.

- [ ] **Step 3: Implementar `convocatoriasFlow.js`**

```js
import { getMisConvocatorias, aceptarConvocatoria, rechazarConvocatoria } from "./convocatoriasApi.js";

export function fetchConvocatorias({ apiBaseUrl, token, fetchImpl }) {
  return getMisConvocatorias(apiBaseUrl, token, fetchImpl ?? fetch);
}

export function responderConvocatoria({ apiBaseUrl, token, convocatoriaId, aceptar, fetchImpl }) {
  const f = fetchImpl ?? fetch;
  return aceptar
    ? aceptarConvocatoria(apiBaseUrl, token, convocatoriaId, f)
    : rechazarConvocatoria(apiBaseUrl, token, convocatoriaId, f);
}
```

- [ ] **Step 4: Implementar screen + container + Home**

`ConvocatoriasScreen.tsx` (estructura espejo de `InvitationsScreen`, misma nota shared/ui):

```tsx
import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, ScrollView, StyleSheet, View } from "react-native";
import { Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { AppText } from "../../shared/ui";
import { spacing } from "../../shared/theme";
import { fetchConvocatorias, responderConvocatoria } from "./convocatoriasFlow.js";

type Convocatoria = {
  convocatoriaId: string;
  partidaId: string;
  equipoId: string;
  fechaEnvio: string;
};

type Props = { apiBaseUrl: string; token: string };

export function ConvocatoriasScreen({ apiBaseUrl, token }: Props) {
  const [convocatorias, setConvocatorias] = useState<Convocatoria[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionId, setActionId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const load = useCallback(async () => {
    setErrorMessage(null);
    const r = await fetchConvocatorias({ apiBaseUrl, token, fetchImpl: undefined });
    if (!r.ok) {
      setErrorMessage(r.message ?? "No se pudieron cargar las convocatorias.");
      return;
    }
    setConvocatorias(r.data as Convocatoria[]);
  }, [apiBaseUrl, token]);

  useEffect(() => {
    (async () => {
      setLoading(true);
      await load();
      setLoading(false);
    })();
  }, [load]);

  async function onResponder(convocatoriaId: string, aceptar: boolean) {
    setActionId(convocatoriaId);
    setErrorMessage(null);
    setFeedback(null);
    const r = await responderConvocatoria({ apiBaseUrl, token, convocatoriaId, aceptar, fetchImpl: undefined });
    setActionId(null);
    if (!r.ok) {
      setErrorMessage(r.message ?? "No se pudo responder la convocatoria.");
      return;
    }
    setFeedback(aceptar ? "Convocatoria aceptada. ¡Nos vemos en el lobby!" : "Convocatoria rechazada.");
    setConvocatorias((prev) => prev.filter((c) => c.convocatoriaId !== convocatoriaId));
  }

  return (
    <ScrollView style={styles.container}>
      <ScreenHeader title="Convocatorias" subtitle="Tu equipo te espera" />
      {errorMessage ? <Notice tone="error">{errorMessage}</Notice> : null}
      {feedback ? <Notice tone="success">{feedback}</Notice> : null}
      {loading ? <ActivityIndicator style={styles.spinner} /> : null}
      {!loading && convocatorias.length === 0 ? (
        <AppText style={styles.empty}>No tienes convocatorias pendientes.</AppText>
      ) : null}
      {convocatorias.map((c) => (
        <Card key={c.convocatoriaId}>
          <AppText variant="bodyStrong">Partida {c.partidaId.slice(0, 8)}</AppText>
          <AppText>Equipo {c.equipoId.slice(0, 8)}</AppText>
          <View style={styles.acciones}>
            <Button onPress={() => void onResponder(c.convocatoriaId, true)} disabled={actionId === c.convocatoriaId}>
              Aceptar
            </Button>
            <Button
              variant="secondary"
              onPress={() => void onResponder(c.convocatoriaId, false)}
              disabled={actionId === c.convocatoriaId}
            >
              Rechazar
            </Button>
          </View>
        </Card>
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#f4f7fb" },
  spinner: { marginTop: spacing.lg },
  empty: { margin: spacing.md },
  acciones: { flexDirection: "row", gap: spacing.sm, marginTop: spacing.sm },
});
```

`ConvocatoriasScreenContainer.tsx` (patrón container estándar):

```tsx
import React from "react";
import { Text } from "react-native";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { ConvocatoriasScreen } from "./ConvocatoriasScreen";

export function ConvocatoriasScreenContainer() {
  const { session } = useAuth();

  if (!session) {
    return <Text>Sesion no disponible.</Text>;
  }

  return <ConvocatoriasScreen apiBaseUrl={mobileEnv.gatewayApiBaseUrl} token={session.token} />;
}
```

`RootNavigator.tsx`:

```tsx
import { ConvocatoriasScreenContainer } from "../features/partidas/ConvocatoriasScreenContainer";
// dentro del AppStack.Navigator:
<AppStack.Screen name="Convocatorias" component={ConvocatoriasScreenContainer} options={{ title: "Convocatorias" }} />
```

`HomeScreen.tsx` — reemplazar los dos NavCard de juego viejos ("Jugar Trivia" → `TriviaGamesList`, "Buscar tesoro" → `BdtPublishedGames`) por:

```tsx
<NavCard
  icon="flag"
  label="Partidas"
  sublabel="Descubre y únete a una partida"
  feature
  onPress={() => navigation.navigate("PartidasPanel")}
/>
<NavCard
  icon="mail"
  label="Convocatorias"
  sublabel="Responde el llamado de tu equipo"
  feature
  onPress={() => navigation.navigate("Convocatorias")}
/>
```

(Respetar las props reales de `NavCard` en el archivo — `icon`/`label`/`sublabel`/`feature` según el uso existente; los NavCards de equipos NO se tocan.)

- [ ] **Step 5: Correr tests + typecheck**

Run: `cd mobile && npm test && npm run typecheck`
Expected: verde. (Las rutas Trivia/Bdt siguen existiendo hasta Task 8, así que Home compila aunque ya no las use.)

- [ ] **Step 6: Commit**

```bash
cd mobile && git add src/features/partidas/convocatoriasFlow.js src/features/partidas/ConvocatoriasScreen.tsx src/features/partidas/ConvocatoriasScreenContainer.tsx src/navigation/RootNavigator.tsx src/screens/HomeScreen.tsx tests/convocatoriasFlow.test.js
git commit -m "feat(mobile): ConvocatoriasScreen inbox + entradas Home (bloque 2d)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 8: Mobile — retiro legacy trivia/bdt

**Files:**
- Delete: `mobile/src/features/trivia/` (directorio completo), `mobile/src/features/bdt/` (directorio completo), `mobile/src/api/triviaApi.ts`
- Delete tests legacy: `mobile/tests/bdtActiveStageFlow.test.js`, `bdtGeolocationPermission.test.js`, `bdtPublishedGamesFlow.test.js`, `bdtTreasureImagePicker.test.js`, `bdtTreasureUploadFlow.test.js`, `triviaParticipantFlow.test.js`, `triviaPublishedGamesFlow.test.js`, `useBdtActiveStage.test.js`, `useBdtPublishedGames.test.js`, `useBdtTreasureUpload.test.js` (verificar con grep si algún otro test de `mobile/tests/` importa de `features/trivia|bdt` y borrarlo también)
- Modify: `mobile/src/navigation/RootNavigator.tsx` (quitar imports + screens Trivia*/Bdt*)
- Modify: `mobile/src/navigation/types.ts` (quitar rutas Trivia*/Bdt* de `AppStackParamList`)
- Modify: `mobile/src/config/env.ts` (quitar `bdtApiBaseUrl` y `triviaApiBaseUrl`)
- Modify: `mobile/.env.example` (o el archivo de ejemplo que exista: quitar `EXPO_PUBLIC_BDT_API_BASE_URL`, `EXPO_PUBLIC_TRIVIA_API_BASE_URL`, `EXPO_PUBLIC_TEAM_API_BASE_URL`)

**Interfaces:**
- Consumes: nada. Pure removal — el gameplay renace en 2e.
- Produces: mobile compila y navega solo con Home/equipos/partidas nuevos.

- [ ] **Step 1: Borrar archivos**

```bash
cd mobile
git rm -r src/features/trivia src/features/bdt
git rm src/api/triviaApi.ts
git rm tests/bdtActiveStageFlow.test.js tests/bdtGeolocationPermission.test.js tests/bdtPublishedGamesFlow.test.js tests/bdtTreasureImagePicker.test.js tests/bdtTreasureUploadFlow.test.js tests/triviaParticipantFlow.test.js tests/triviaPublishedGamesFlow.test.js tests/useBdtActiveStage.test.js tests/useBdtPublishedGames.test.js tests/useBdtTreasureUpload.test.js
```

(`git rm -r` sobre directorios exactos está permitido — es deleción por ruta exacta, no un `git clean`.)

- [ ] **Step 2: Limpiar RootNavigator + types**

`RootNavigator.tsx`: quitar los imports `TriviaGamesListScreenContainer`, `TriviaLobbyScreenContainer`, `TriviaLivePlayScreenContainer`, `TriviaAnswerScreenContainer`, `TriviaResultScreenContainer`, `TriviaScoreScreenContainer`, `BdtPublishedGamesScreenContainer`, `BdtRankingScreenContainer`, `BdtActiveStageScreenContainer`, `BdtTreasureUploadScreenContainer` y sus `<AppStack.Screen name="Trivia…|Bdt…">`.

`types.ts`: quitar de `AppStackParamList` las claves `TriviaGamesList`, `TriviaLobby`, `TriviaLivePlay`, `TriviaAnswer`, `TriviaResult`, `TriviaScore`, `BdtPublishedGames`, `BdtRanking`, `BdtActiveStage`, `BdtTreasureUpload`.

- [ ] **Step 3: Limpiar env**

`config/env.ts`: quitar las líneas `bdtApiBaseUrl` y `triviaApiBaseUrl` del objeto `mobileEnv`.
`mobile/.env.example` (y cualquier `.env.sample`/`.env.template` presente): quitar `EXPO_PUBLIC_BDT_API_BASE_URL`, `EXPO_PUBLIC_TRIVIA_API_BASE_URL`, `EXPO_PUBLIC_TEAM_API_BASE_URL`.

- [ ] **Step 4: Grep de limpieza**

Run: `cd mobile && grep -rn "features/trivia\|features/bdt\|triviaApi\|bdtApiBaseUrl\|triviaApiBaseUrl\|TEAM_API_BASE_URL" src tests`
Expected: vacío. Resolver cualquier hit.

- [ ] **Step 5: Suite + typecheck**

Run: `cd mobile && npm test && npm run typecheck`
Expected: verde (conteo baja por los tests borrados). `expo-location`/`expo-image-picker` quedan como deps instaladas sin consumidor — NO desinstalarlas (2e las reusa para geoloc/QR).

- [ ] **Step 6: Commit**

```bash
cd mobile
git add src/navigation/RootNavigator.tsx src/navigation/types.ts src/config/env.ts .env.example
git commit -m "chore(mobile): retiro codigo legacy trivia/bdt y vars env muertas (bloque 2d)" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

(Los `git rm` del Step 1 ya están staged; el `git add` cubre los modificados. Si `.env.example` no existe con ese nombre, ajustar a los archivos de ejemplo reales del directorio.)

---

### Task 9: Gate final — E2E vivo + traceability (controller)

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila 2d)
- Modify: `GUIA-LEVANTAMIENTO.md` (sección mobile: panel nuevo, endpoint listado, deps)

El controller la ejecuta con el stack vivo (infra compose + partidas + operaciones-sesion + puntuaciones + gateway; tokens PKCE).

- [ ] **Step 1: Suites en HEAD**

Run: `cd mobile && npm test && npm run typecheck` y `dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln`
Expected: verdes.

- [ ] **Step 2: E2E vivo vía gateway :5080**

1. Operador: crear partida Individual + juego, publicar → participante: `GET /operaciones-sesion/partidas-publicadas` **200** con la partida (nombre/modalidad/cupos); crear otra partida SIN publicar → no aparece; iniciar la primera → desaparece del listado.
2. Individual: `POST /inscripciones` **201** → `GET /mi-sesion` **200** apuntando a la partida → `DELETE /inscripciones/mia` **204** → `mi-sesion` **204**.
3. Equipo: partida modalidad Equipo publicada; líder (participante con equipo — crear equipo vía identity si hace falta) `POST /inscripciones-equipo` **201** → miembro `GET /mis-convocatorias` con la convocatoria → `POST /convocatorias/{id}/aceptacion` **200**.
4. Smoke SignalR (node + `@microsoft/signalr` de `mobile/node_modules`): participante suscrito a la partida recibe `PartidaIniciada` al `POST /inicio` del operador.
5. Registrar resultados en el ledger.

- [ ] **Step 3: Traceability + GUIA**

Fila 2d en `docs/04-sdd/traceability-matrix.md` (hashes de T1-T8 verificados con `git cat-file -t`). `GUIA-LEVANTAMIENTO.md`: nota del panel mobile + endpoint `partidas-publicadas` + dep `@microsoft/signalr` mobile.

- [ ] **Step 4: Commit docs**

```bash
git add docs/04-sdd/traceability-matrix.md GUIA-LEVANTAMIENTO.md
git commit -m "docs(bloque2d): traceability panel mobile + participacion" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage:** §1 endpoint → T1; contrato → T2; §2 api modules → T3; hub → T4; §3 pantallas → T5/T6/T7 (panel/lobby/convocatorias+Home); §4 retiro → T8; §5 gate → T9. ✅

**Placeholder scan:** los puntos "verificar shared/ui antes de escribir JSX" y "mismo mecanismo de import que los tests existentes" son verificaciones dirigidas contra el repo real (con archivo exacto a leer), no TODOs. Sin TBD. ✅

**Type consistency:** `PartidaPublicadaDto` de T1 = shape camelCase consumido en T3/T5; firmas de T3 (`inscribirse(apiBaseUrl, token, partidaId, fetchImpl?)` etc.) consumidas idénticas en T5/T6/T7; `crearSesionHub(gatewayBaseUrl, accessToken)` de T4 usada así en T6; rutas nav (`PartidasPanel`/`PartidaLobby {partidaId, nombre}`/`Convocatorias`) tipadas en T5 y registradas en T5/T6/T7; `mapCommonError`/`networkError` exportados en T3 y reusados en T6. ✅

**Modelos:** T1 sonnet (multi-file backend) · T2 haiku (doc) · T3 haiku (verbatim) · T4 sonnet (dep+RN) · T5-T7 sonnet (screens+nav) · T8 sonnet (removal cuidadoso) · T9 controller. Reviewers sonnet; review final opus.
