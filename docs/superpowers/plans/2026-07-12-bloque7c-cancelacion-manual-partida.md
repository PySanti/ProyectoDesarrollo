# Bloque 7c — Cancelación manual de partida (HU-40) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** El operador cancela manualmente una partida (en `Lobby` o `Iniciada`) desde la consola web; los participantes lo ven en vivo. Cierra HU-40 y, por transitividad, HU-37, HU-41 y HU-26 (paquete R3 del informe de completitud — hoy la cancelación manual NO existe en ningún nivel; solo hay auto-cancelación por mínimos).

**Architecture:** Cadena nueva mínima reusando el evento existente: dominio `SesionPartida.Cancelar(now)` (Lobby/Iniciada → `Cancelada`; terminal → excepción) → `CancelarPartidaCommand` + handler (patrón de `IniciarPartidaCommandHandler`: guarda con token optimista y publica `PartidaCanceladaEvent` con motivo `"CanceladaPorOperador"` por el seam `ISesionEventsPublisher` — SignalR + RabbitMQ ya cablean ese evento de punta a punta, mobile incluido) → `POST /operaciones-sesion/partidas/{partidaId}/cancelacion` (policy `GestionarPartidas`) → web: función API + botón "Cancelar partida" con confirmación en 2 clics, visible en lobby y en runtime, gateado `puedeOperar`. **Mobile: cero cambios** (ya escucha `PartidaCancelada` en lobby y live). Puntuaciones ya proyecta el evento (su mapper ignora `motivo` — verificado).

**Tech Stack:** .NET 8 (Operaciones de Sesión: Domain/Application/Api + xUnit) · React/TS + vitest (web).

## Global Constraints

- Rama: `feature/bloque-7` (activa). NO crear ramas.
- Commits terminan con: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`
- PROHIBIDO a implementadores: `git stash/reset/checkout/restore/clean`. Solo `git add <paths exactos>` + `git commit`.
- Gate backend: `export DOTNET_ROOT=/snap/dotnet-sdk/current; dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln` (491 tests hoy, todo verde).
- Gates frontend: `cd frontend && npx vitest run && npx tsc -b && npm run build`; borrar artefactos generados (`tsconfig*.tsbuildinfo`, `vite.config.js/.d.ts`, `vitest.config.js/.d.ts`), nunca commitearlos.
- No dependencias nuevas. UI en español. `data-testid` existentes intocables; nuevos: `btn-cancelar-partida`, `btn-cancelar-partida-confirm`.
- Acciones mutantes web gateadas `ctx.puedeOperar`.

---

### Task 1: Backend — dominio, comando, endpoint y contrato

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs` (método público nuevo junto a los demás de ciclo de vida)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/` (excepción nueva `PartidaNoCancelableException` — seguir el estilo de las excepciones hermanas y el mapping del middleware a 409)
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/CancelarPartidaCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/CancelarPartidaCommandHandler.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/` (response nuevo `CancelacionPartidaResponse`)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs` (endpoint junto a `inicio`)
- Tests: unit de dominio + unit de handler + controller test + contract test (seguir los archivos hermanos de `Iniciar`)
- Modify: `contracts/http/operaciones-sesion-api.md` (fila endpoint + response + suma al conteo de la matriz de permisos `GestionarPartidas`)

**Interfaces:**
- Consumes: `ISesionEventsPublisher.PublicarPartidaCanceladaAsync(PartidaCanceladaEvent, ct)` (existente); `PartidaCanceladaEvent(PartidaId, SesionPartidaId, Motivo, FechaCancelacion)` (existente); patrón de persistencia + 409 de `IniciarPartidaCommandHandler` (léelo entero: repo, TimeProvider, publicación post-save).
- Produces (Task 2 lo consume): `POST /operaciones-sesion/partidas/{partidaId}/cancelacion` → `200 + { partidaId, estado: "Cancelada" }` (`CancelacionPartidaResponse(Guid PartidaId, string Estado)`); errores: 401/403 (policy), 404 sesión no existe, 409 estado terminal (`Cancelada`/`Terminada`).

- [ ] **Step 1: Tests de dominio que fallan.** En el archivo de tests de dominio de `SesionPartida` (donde estén los de `Iniciar`/`AplicarInicio`): (a) `Cancelar` desde `Lobby` → `Estado == Cancelada` y `FechaFin == now`; (b) desde `Iniciada` → ídem; (c) desde `Terminada` → lanza `PartidaNoCancelableException`; (d) desde `Cancelada` → lanza. Correr con filtro → RED (método no existe: error de compilación cuenta como RED).

- [ ] **Step 2: Dominio.** En `SesionPartida`:

```csharp
    // HU-40: cancelación manual por el operador — válida en Lobby e Iniciada.
    public void Cancelar(DateTime now)
    {
        if (Estado != EstadoSesion.Lobby && Estado != EstadoSesion.Iniciada)
            throw new PartidaNoCancelableException(PartidaId, Estado.ToString());
        Estado = EstadoSesion.Cancelada;
        FechaFin = now;
    }
```

  Excepción nueva (estilo hermanas; el `ExceptionHandlingMiddleware` debe mapearla a **409** — registrarla donde el middleware mapea las demás excepciones de estado, leer cómo lo hace con `SesionNoIniciadaException`):

```csharp
public sealed class PartidaNoCancelableException : Exception
{
    public PartidaNoCancelableException(Guid partidaId, string estado)
        : base($"La partida {partidaId} no puede cancelarse en estado {estado}.") { }
}
```

- [ ] **Step 3: GREEN dominio** (filtro al archivo de tests).

- [ ] **Step 4: Command + handler + tests que fallan primero.** `CancelarPartidaCommand(Guid PartidaId)` → `CancelacionPartidaResponse`. Handler calcado del esqueleto de `IniciarPartidaCommandHandler` (repo GetByPartidaId → 404 vía la excepción/patrón existente; `sesion.Cancelar(now)`; save con manejo de concurrencia idéntico; después del save publicar `PartidaCanceladaEvent(sesion.PartidaId, sesion.Id.Valor, "CanceladaPorOperador", now)`). Tests de handler: éxito publica el evento con motivo `"CanceladaPorOperador"`; sesión inexistente → excepción 404 del patrón; estado terminal → propaga `PartidaNoCancelableException`.

- [ ] **Step 5: Endpoint + controller/contract tests.** En `SesionesController`, junto a `inicio`:

```csharp
    [Authorize(Policy = "GestionarPartidas")]
    [HttpPost("partidas/{partidaId:guid}/cancelacion")]
    public async Task<IActionResult> CancelarPartida(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new CancelarPartidaCommand(partidaId), cancellationToken));
```

  Controller test (archivo hermano de los de inicio) + contract test del path público + policy (patrón de los contract tests existentes de `inicio`).

- [ ] **Step 6: Suite completa del servicio** → verde (491 + nuevos). Verificar también que ningún test existente asuma que `Cancelada` solo llega por mínimos.

- [ ] **Step 7: Contrato.** `contracts/http/operaciones-sesion-api.md`: fila `| Cancelar partida (operador, HU-40) | POST | /operaciones-sesion/partidas/{partidaId}/cancelacion | Policy GestionarPartidas | 200 + CancelacionPartidaResponse | 401 · 403 · 404 sesión no existe · 409 estado terminal |` (ajustar al formato exacto de la tabla); registrar `CancelacionPartidaResponse { partidaId, estado }` en la lista de DTOs; actualizar la fila `GestionarPartidas` de la matriz de permisos (hoy dice "Operación de la partida (9)" — pasa a 10 e incluye `cancelacion`); nota: el evento `PartidaCancelada` ahora tiene 2 motivos: `MinimosNoAlcanzados` | `CanceladaPorOperador`.

- [ ] **Step 8: Commit** `feat(operaciones): cancelación manual de partida por el operador (7c, HU-40)` + cuerpo breve + trailer.

---

### Task 2: Web — botón "Cancelar partida" con confirmación en la consola

**Files:**
- Modify: `frontend/src/api/operacionesApi.ts` (+ función) y `frontend/src/api/operacionesApi.test.ts` (+ tests, patrón Task 1 de 7b)
- Modify: `frontend/src/features/partidas/SesionOperadorPage.tsx` (botón + estado de confirmación + handler en `VistaCtx`)
- Modify: `frontend/src/features/partidas/SesionOperadorPage.test.tsx` (+ tests)

**Interfaces:**
- Consumes: endpoint de Task 1; `ctx.puedeOperar`; el push `PartidaCancelada` que la página YA maneja (verifica cómo reacciona la vista al recibirlo — la transición de vista tras cancelar puede llegar por el hub; el handler además aplica la respuesta directamente).
- Produces: `cancelarPartida(partidaId: string, accessToken: string, fetchImpl?): Promise<{ partidaId: string; estado: string }>` → POST `/operaciones-sesion/partidas/${partidaId}/cancelacion`; en `VistaCtx`: `onCancelarPartida(): void`; testids `btn-cancelar-partida` y `btn-cancelar-partida-confirm`.

- [ ] **Step 1: Tests API que fallan** (mismo molde que `aceptarInscripcion` en 7b: URL exacta, method POST, propagación de error 409) → RED → implementar `cancelarPartida` con `request<T>` → GREEN.

- [ ] **Step 2: Tests de página que fallan.** (a) en vista lobby con `puedeOperar`, `btn-cancelar-partida` visible; primer clic NO llama la API y muestra `btn-cancelar-partida-confirm`; clic en confirm → `cancelarPartida` llamada con `(partidaId, token)`; (b) con `puedeOperar: false` → ni botón ni confirm; (c) en vista runtime (juego activo) el botón también existe. Seguir el arnés real del archivo.

- [ ] **Step 3: Implementación.** Confirmación en 2 clics con estado local (patrón simple: `const [confirmandoCancelacion, setConfirmandoCancelacion] = useState(false)`; primer botón lo activa, el confirm ejecuta y lo resetea; usar clase visual de acción destructiva coherente con el archivo — si no hay, `secondary-button` + texto claro). Ubicación: junto a las acciones del header/chrome compartido de la página de modo que sea visible en lobby Y en runtime (si no existe chrome compartido, en `LobbyView` junto a `btn-actualizar-lobby` y en la vista runtime junto a sus acciones — misma `ctx`). Handler:

```tsx
const onCancelarPartida = async () => {
  try {
    await cancelarPartida(partidaId, accessToken);
    await cargar(); // el push PartidaCancelada también llega por el hub; cargar() asegura la transición inmediata
  } catch {
    await cargar();
  }
};
```

  (adaptar `cargar()` al mecanismo real de refetch de la página, igual que en 7b).

- [ ] **Step 4: GREEN feature suite** (`npx vitest run src/features/partidas`) y suite completa.

- [ ] **Step 5: Commit** `feat(web): botón cancelar partida con confirmación en consola operador (7c)` + trailer.

---

### Task 3: Gates completos + ledger

- [ ] **Step 1:** Backend: suite completa Operaciones verde. **Step 2:** frontend `npx vitest run` + `tsc -b` + build verdes, artefactos borrados, árbol limpio. **Step 3:** grep de humo: `grep -rn "cancelacion" services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs frontend/src/api/operacionesApi.ts` con hits. **Step 4:** append al ledger `.git/sdd/progress.md`: "7c DONE" + hashes + números de gates.
