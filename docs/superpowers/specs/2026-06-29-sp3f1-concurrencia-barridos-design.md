# SP-3f-1 — Concurrencia optimista + barridos por tiempo (Operaciones de Sesión)

- **Fecha:** 2026-06-29
- **Servicio dueño:** Operaciones de Sesión
- **Cliente:** ninguno (backend puro — sin web/mobile)
- **Fuente:** auto-inicio diferido en SP-3b; barrido de timeout diferido en SP-3c/3d; token de concurrencia (watch-item `sp3f-concurrency-token`).
- **Estado:** diseño aprobado, pendiente de plan.

## 1. Problema

Hoy todo el avance de una `SesionPartida` en vivo es **request-triggered**:

- El **inicio automático** (`ModoInicioPartida ∈ {Automatico, ManualYAutomatico}`, con `TiempoInicio`) solo ocurre si alguien llama `POST /partidas/{id}/inicio-automatico`. No hay nada que lo dispare al llegar la hora.
- El **cierre por timeout** de una pregunta Trivia o una etapa BDT existe en el dominio (`AvanzarPregunta`/`AvanzarEtapa` marcan `MotivoCierre.Tiempo` cuando `now ≥ FechaActivacion + TiempoLimiteSegundos`), pero solo se ejecuta si el operador avanza manualmente o si llega otra petición. Una pregunta/etapa vencida sin tráfico queda abierta indefinidamente y el juego se estanca.

F1 introduce el **segundo escritor** (workers de fondo) que disparan estos avances sin petición. Un segundo escritor concurrente con el hilo de request exige **control de concurrencia optimista** sobre `SesionPartida`, de lo contrario dos escritores pueden cerrar el mismo paso dos veces (doble publicación de eventos).

## 2. Alcance

**Incluye:**
1. Token de concurrencia optimista (`xmin` de PostgreSQL) en `SesionPartida`.
2. Un `BackgroundService` con timer que por tick dispara dos comandos MediatR.
3. Barrido de **inicio automático**: inicia (o auto-cancela por mínimos) las sesiones en `Lobby` cuyo `TiempoInicio` ya pasó.
4. Barrido de **timeout**: cierra por tiempo la pregunta/etapa vencida del juego activo, avanza al siguiente paso y, si era el último, finaliza el juego (→ siguiente juego o `Terminada`).
5. Mapeo de `DbUpdateConcurrencyException` → **409 Conflict** en el middleware de excepciones.

**Excluye (diferido):**
- Equipo/convocatoria → slice-E.
- Publicación real de eventos (RabbitMQ) — sigue por `NoOpSesionEventsPublisher`, su propio slice.
- SignalR / push de estado/timers → SP-3f-2.
- Pistas / geolocalización → SP-3f-3.
- Scoring/ranking real → SP-4.
- Integration test del enforcement xmin a nivel DB (requiere Postgres real/Testcontainers; ver §7).

## 3. Decisiones (cerradas en brainstorming)

| Decisión | Elección | Razón |
|---|---|---|
| Forma del worker | **Un** `BackgroundService`, **dos** comandos MediatR por tick | Una cadencia, una pieza de ops, manejo de conflicto compartido; comandos siguen testeables por separado; separable después si las cadencias divergen. |
| Política de conflicto (request) | `DbUpdateConcurrencyException` → **409** | Mínima superficie, semántica honesta; la ventana de choque es el instante exacto de expiración. El cliente refetchea (`/mi-sesion`) y reintenta. |
| Política de conflicto (worker) | descartar + reevaluar al próximo tick | El barrido es idempotente por construcción; no necesita reintento agresivo. |
| Token de concurrencia | `xmin` vía `UseXminAsConcurrencyToken()` | Columna de sistema de Postgres; migración schema-only (sin columna física nueva, sin migración de datos); idiomático en Npgsql. |
| Reloj | `System.TimeProvider` (ya en uso) | Testeable con `FakeTimeProvider`. |
| Reúso de dominio | Los barridos reúsan métodos/handlers existentes; cero lógica de cierre nueva | El cierre por timeout ya vive en el dominio; el worker es solo un nuevo invocador. |

## 4. Componentes

### 4.1 Token de concurrencia (`xmin`)
En la configuración EF de `SesionPartida` (`Infrastructure/Persistence/Configurations`), añadir `builder.UseXminAsConcurrencyToken()`. Genera una migración **schema-only** (Npgsql mapea `xmin`, columna de sistema; no añade columna física ni toca datos). El `UnitOfWork.SaveChangesAsync` existente lanzará `DbUpdateConcurrencyException` cuando el `xmin` cargado difiera del actual (otro escritor commiteó entremedio).

### 4.2 Scans del repositorio
Añadir a `ISesionPartidaRepository` (Domain) + impl EF (Infrastructure):

- `Task<IReadOnlyList<SesionPartida>> GetSesionesConActividadVencidaAsync(DateTime now, CancellationToken ct)`
  → `Estado == Iniciada` con el juego activo cuyo paso activo (pregunta Trivia o etapa BDT) cumple `FechaActivacion + TiempoLimiteSegundos ≤ now`. Carga el grafo necesario (juegos + pasos) para que el dominio decida. Predicado de vencimiento empujado a SQL donde sea limpio; si no, cargar las `Iniciada` con juego activo y filtrar en memoria (escala académica — aceptable).
- `Task<IReadOnlyList<SesionPartida>> GetSesionesAutoInicioPendienteAsync(DateTime now, CancellationToken ct)`
  → `Estado == Lobby ∧ ModoInicioPartida ∈ {Automatico, ManualYAutomatico} ∧ TiempoInicio != null ∧ TiempoInicio ≤ now`. Carga grafo de inscripciones **y de preguntas/etapas** (el inicio chequea mínimos y `AplicarInicio` activa el primer paso iterando esas colecciones — sin `Include` el juego quedaría `Activo` sin paso en Npgsql; corregido en review final, fix C1).

> Cada barrido procesa **un candidato por save** (load → mutar → `SaveChangesAsync`) para que cada save tenga su propio chequeo `xmin`.
>
> **Caveat de resiliencia (corregido en review final, I1):** los dos comandos del tick comparten **un** `DbContext` scoped (un scope por tick). Tras un `DbUpdateConcurrencyException` real de EF, la entidad fallida queda `Modified` en ese contexto y el siguiente `SaveChangesAsync` la re-batchea → puede re-lanzar y saltar también a los candidatos posteriores del **mismo** tick. La resiliencia efectiva es por-**tick** con **auto-sanación al próximo tick** (scope fresco), no por-candidato dentro del tick. Esto es aceptable: sin corrupción (xmin guarda + publisher No-Op no lanza), sin doble-publish, barridos idempotentes → el próximo tick reevalúa y completa. **Follow-up** (cuando suba la carga / aterrice el broker real): aislar por candidato (scope/contexto fresco por candidato vía `IServiceScopeFactory`). Los tests con fakes (`ThrowOnceUnitOfWork`) ejercitan la intención loop-continue del handler, no la semántica del change-tracker de EF.

### 4.3 Método de dominio `CerrarActividadVencida(now)`
Nuevo método en `SesionPartida`:

```
public ResultadoCierreVencido CerrarActividadVencida(DateTime now)
```

- **No-op idempotente** si la sesión no está `Iniciada`, no hay juego activo, o el paso activo no está vencido (`now < FechaActivacion + TiempoLimiteSegundos`).
- Si el paso activo está vencido: reúsa la lógica interna de cierre+avance (la misma de `AvanzarPregunta`/`AvanzarEtapa`, con `MotivoCierre.Tiempo`); activa el siguiente paso.
- Si era el **último** paso del juego (no hay siguiente): finaliza el juego (`FinalizarJuegoActual` interno) → activa el siguiente juego pendiente, o pasa a `Terminada` con `FechaFin`.
- Devuelve un `ResultadoCierreVencido` que describe qué cerró (Trivia/BDT, ids, orden, motivo), qué paso se activó, si finalizó el juego y/o terminó la partida — suficiente para que el handler emita los **mismos** eventos que el path request.

> Se prefiere un método de dominio dedicado (en vez de que el handler compute "¿vencido?" leyendo `FechaActivacion`) para no filtrar la regla de negocio del vencimiento a la capa Application. Reúsa internamente la maquinaria de cierre/avance existente; no duplica reglas.

### 4.4 Comandos + handlers (Application)

**`BarrerTimeoutsCommand : IRequest<int>`** (devuelve nº de sesiones avanzadas, útil para logging/tests)
Handler:
1. `candidatos = await repo.GetSesionesConActividadVencidaAsync(now, ct)`
2. por cada candidato (en su propio try/catch):
   - `var r = sesion.CerrarActividadVencida(now)`
   - si `r` es no-op → continuar
   - `await uow.SaveChangesAsync(ct)`
   - publicar eventos según `r` (PreguntaCerrada / EtapaCerrada / Pregunta|EtapaActivada / JuegoActivado / PartidaFinalizada) — reusar los mismos `Event` records y métodos del publisher que usan `AvanzarPreguntaCommandHandler` / `AvanzarEtapaCommandHandler` / `FinalizarJuegoActualCommandHandler`.
   - `catch (DbUpdateConcurrencyException)` → log + continuar (otro escritor ganó; próximo tick reevalúa)
   - `catch (DomainException)` → log + continuar (estado cambió entre scan y mutación)

**`BarrerIniciosAutomaticosCommand : IRequest<int>`**
Handler:
1. `candidatos = await repo.GetSesionesAutoInicioPendienteAsync(now, ct)`
2. por cada candidato (en su propio try/catch): `await mediator.Send(new IntentarInicioAutomaticoCommand(candidato.PartidaId), ct)` — **reúso total** del handler existente (ya carga, aplica, salva y emite `PartidaIniciada`/`JuegoActivado` o `PartidaCancelada`). `catch` conflicto/dominio → log + continuar.

> `IntentarInicioAutomatico(now)` del dominio ya retorna `NoCorresponde` si la sesión no está en `Lobby` o aún no llega `TiempoInicio`, y auto-cancela por mínimos dentro de `AplicarInicio` — no se añade lógica.

### 4.5 Worker `MantenimientoSesionesWorker : BackgroundService`
- Un `PeriodicTimer` (intervalo de `MantenimientoOptions.IntervaloMs`, default **1000 ms**, bindeado de configuración).
- Por tick: crea un scope DI (`IServiceScopeFactory`), resuelve `IMediator`/`ISender`, `Send(BarrerIniciosAutomaticosCommand)` y `Send(BarrerTimeoutsCommand)`.
- `try/catch` por tick: loguea y continúa — **el loop nunca muere** por una excepción de un tick.
- Registrado en `Program.cs` con `AddHostedService<MantenimientoSesionesWorker>()` y `Configure<MantenimientoOptions>(...)`.

### 4.6 Middleware
En `ExceptionHandlingMiddleware`: mapear `DbUpdateConcurrencyException` → **409 Conflict** (cuerpo de error consistente con el resto). Afecta a los endpoints existentes que escriben `SesionPartida` (responder, validar tesoro, avanzar pregunta/etapa, iniciar) — ahora pueden devolver 409 al chocar con el barrido.

## 5. Flujo de datos

```
PeriodicTimer tick
  → scope DI
    → Send(BarrerIniciosAutomaticosCommand)
        → repo.GetSesionesAutoInicioPendienteAsync(now)
        → foreach: Send(IntentarInicioAutomaticoCommand) [reúso] → save (xmin) → eventos
    → Send(BarrerTimeoutsCommand)
        → repo.GetSesionesConActividadVencidaAsync(now)
        → foreach: CerrarActividadVencida(now) → save (xmin) → eventos
```

Path request **intacto**, salvo que cualquier `SaveChangesAsync` sobre `SesionPartida` ahora es xmin-guarded → puede lanzar `DbUpdateConcurrencyException` → 409.

## 6. Manejo de errores

| Nivel | Política |
|---|---|
| Worker (tick) | `try/catch` por tick: log, continúa. El servicio nunca se cae. |
| Handler (candidato) | `try/catch` por ítem: `DbUpdateConcurrencyException` y `DomainException` → log + saltar ítem; no aborta el barrido. |
| Request | `DbUpdateConcurrencyException` → 409 (cliente refetchea + reintenta). |

## 7. Testing

- **Dominio (unit):** `CerrarActividadVencida` —
  - nada vencido → no-op (estado inalterado);
  - Trivia vencida → cierra con motivo Tiempo + activa siguiente pregunta;
  - Trivia última vencida → finaliza juego → activa siguiente juego;
  - última pregunta del último juego vencida → `Terminada` + `FechaFin`;
  - BDT análogo (etapa vencida → cierre Tiempo + siguiente etapa; última → finaliza);
  - idempotencia (2ª llamada inmediata → no-op).
- **Handlers (unit, InMemory + `FakeTimeProvider` + fakes):**
  - `BarrerTimeouts` cierra solo las sesiones vencidas, ignora las no-vencidas; emite los eventos esperados (vía `FakeSesionEventsPublisher`);
  - `BarrerIniciosAutomaticos` inicia los candidatos vencidos y auto-cancela los que no cumplen mínimos;
  - **conflicto:** fake repo/UoW que lanza `DbUpdateConcurrencyException` en un candidato → el handler lo salta y continúa el loop con el resto. **Nota:** prueba la intención loop-continue del handler contra fakes; la semántica real del change-tracker de EF (entidad fallida queda `Modified`, re-batch) no se reproduce con fakes — ver el caveat I1 en §4.2 (resiliencia efectiva = por-tick, auto-sana al próximo tick).
- **Middleware (unit):** `DbUpdateConcurrencyException` → 409.
- **Worker (unit ligero):** un tick → `Send` de ambos comandos (sender fake), y una excepción de un tick no detiene el loop.
- ⚠️ **Gap documentado:** el enforcement de `xmin` a nivel DB **no** se integration-testea — el provider InMemory que usan todos los IntegrationTests ignora tokens de concurrencia, y el repo no tiene Postgres real/Testcontainers. Se configura `UseXminAsConcurrencyToken()` y se testea el **contrato de comportamiento** (handler salta en conflicto; middleware → 409). Mismo criterio que el seam No-Op de eventos. Cerrar el gap requeriría introducir Testcontainers → fuera del alcance de F1.

## 8. Fronteras / invariantes preservados

- **Individual-only** (Equipo → slice-E); el cierre por timeout es agnóstico de modalidad, pero el alcance se mantiene Individual por consistencia.
- Eventos por `NoOpSesionEventsPublisher` (RabbitMQ real diferido).
- Sin SignalR, sin scoring.
- Sin cambios de contrato salvo la respuesta **409** añadida a los endpoints de runtime/inicio.
- El dominio sigue siendo la autoridad del cierre por timeout; el worker no contiene reglas de negocio.

## 9. Artefactos a tocar (orientativo, el plan los detalla)

- `Domain/Entities/SesionPartida.cs` (+ `CerrarActividadVencida`, `ResultadoCierreVencido`).
- `Domain/Abstractions/Persistence/ISesionPartidaRepository.cs` (+2 scans).
- `Infrastructure/Persistence/Configurations/...SesionPartida...` (`UseXminAsConcurrencyToken`).
- `Infrastructure/Persistence/.../SesionPartidaRepository.cs` (+2 scans).
- `Infrastructure/Persistence/Migrations/` (migración schema-only del token).
- `Application/Commands/BarrerTimeoutsCommand.cs`, `BarrerIniciosAutomaticosCommand.cs`.
- `Application/Handlers/Commands/BarrerTimeoutsCommandHandler.cs`, `BarrerIniciosAutomaticosCommandHandler.cs`.
- `Api/Workers/MantenimientoSesionesWorker.cs`, `Api/Configuration/MantenimientoOptions.cs` (o ubicación equivalente).
- `Api/Middleware/ExceptionHandlingMiddleware.cs` (mapeo 409).
- `Api/Program.cs` (`AddHostedService` + `Configure<MantenimientoOptions>`).
- Tests: Domain unit, Application handler unit (×3 escenarios), middleware unit, worker unit.
- `contracts/http/operaciones-sesion-api.md` (documentar 409; barridos no son endpoints).
- `docs/04-sdd/traceability-matrix.md` (fila SP-3f-1 — **escribir, NO commitear**; carve-out vigente).
