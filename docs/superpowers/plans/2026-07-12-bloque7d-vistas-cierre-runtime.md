# Bloque 7d — Vistas de cierre/monitoreo del runtime — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cerrar los 6 requisitos informativos del runtime (paquete R4): **HU-24+BR-T06** (el participante ve la respuesta correcta al cerrar la pregunta), **HU-35** (resultado por etapa con ganador o "nadie"), **HU-38** (panel operador de envíos TesoroQR), **HU-18** (lobby operador lista inscritos), **HU-12** (aviso mobile al entrar a partida de equipo sin ser líder).

**Architecture:** Todo aditivo sobre lo existente. Backend: los eventos de cierre YA portan ganador (`PreguntaTriviaCerradaEvent.GanadorParticipanteId/GanadorEquipoId`, `EtapaBDTCerradaEvent` ídem, `EtapaBDTGanadaEvent.ParticipanteId/EquipoId`) — solo los payloads SignalR eran delgados. Decisión del design maestro: revelar respuesta correcta/ganador **post-cierre** no viola anti-leak. Se extienden evento de cierre Trivia (2 campos trailing) y 3 payloads SignalR; query nueva de envíos TesoroQR (el dato ya se persiste en `EtapaSnapshot`). Clientes: render de lo nuevo. HU-18/HU-12 son solo clientes (datos ya presentes en DTOs).

**Tech Stack:** .NET 8 (Operaciones) · React/TS + vitest (web) · RN/Expo + node --test (mobile).

## Global Constraints

- Rama: `feature/bloque-7`. Commits con trailer `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a implementadores: `git stash/reset/checkout/restore/clean`. Solo `git add <paths exactos>` + `git commit`.
- Gates: backend `dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln` (baseline 506 verde; `export DOTNET_ROOT=/snap/dotnet-sdk/current` si falla) · web `cd frontend && npx vitest run && npx tsc -b && npm run build` (baseline 230; borrar artefactos generados) · mobile `cd mobile && npm test && npm run typecheck` (baseline 108).
- **Cambios de eventos/payloads ESTRICTAMENTE aditivos** (campos trailing con default null) — no romper consumidores (Puntuaciones deserializa por nombre, tolera campos nuevos) ni clientes viejos.
- Sin dependencias nuevas. UI español. Testids existentes intocables.
- Acciones/paneles web de operador: los de solo-lectura visibles también para admin observador (sin gating); no hay acciones mutantes nuevas en este slice.

---

### Task 1: Backend — payloads de cierre enriquecidos + query de envíos TesoroQR

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/TriviaRuntimeEvents.cs` (`PreguntaTriviaCerradaEvent` += `Guid? OpcionCorrectaId = null, string? TextoOpcionCorrecta = null` — trailing)
- Modify: TODOS los sitios que publican `PublicarPreguntaTriviaCerradaAsync` (localizar con grep: ResponderPregunta / AvanzarPregunta / BarrerTimeouts / CerrarActividadVencida u equivalentes) — rellenar los 2 campos desde el `PreguntaSnapshot` en scope (`Opciones.First(o => o.EsCorrecta)`: `OpcionId` y `Texto`)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs`:
  - `PreguntaCerradaPayload` += `Guid? OpcionCorrectaId = null, string? TextoOpcionCorrecta = null, Guid? GanadorParticipanteId = null, Guid? GanadorEquipoId = null`
  - `EtapaCerradaPayload` += `Guid? GanadorParticipanteId = null, Guid? GanadorEquipoId = null`
  - `EtapaGanadaPayload` += `Guid? GanadorParticipanteId = null, Guid? GanadorEquipoId = null`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs` (mapear los campos nuevos desde los eventos — ya los portan)
- Create: query `ObtenerEnviosTesoroQuery` + handler + `EnviosTesoroDto` + endpoint `GET /operaciones-sesion/partidas/{partidaId:guid}/juego-actual/envios-tesoro` (`[Authorize(Policy = "GestionarPartidas")]`) — juego BDT activo: por etapa, lista de intentos (`etapaId, orden, intentos[]{ participanteId, equipoId?, resultado, instante }`); 409 si el juego activo no es BDT (reusar `JuegoActivoNoEsBDTException` vía el patrón de `ObtenerEtapaActual`); leer de `EtapaSnapshot` la colección de `TesoroQR` persistida
- Tests: unit del handler nuevo + controller + contract (patrón hermanos de `etapa-actual`); tests existentes de eventos/payloads actualizados si asertan shape exacto
- Modify: `contracts/http/operaciones-sesion-api.md` (endpoint + DTO + payloads SignalR actualizados) y `contracts/events/operaciones-sesion-events.md` (sample de `PreguntaTriviaCerrada` con los 2 campos nuevos, nota aditiva)

**Interfaces (Tasks 2-3 consumen):**
- Payload `PreguntaCerrada` (SignalR): `{ partidaId, juegoId, preguntaId, opcionCorrectaId?, textoOpcionCorrecta?, ganadorParticipanteId?, ganadorEquipoId? }`
- Payloads `EtapaCerrada`/`EtapaGanada`: `{ ..., ganadorParticipanteId?, ganadorEquipoId? }`
- `GET .../juego-actual/envios-tesoro` → `{ partidaId, juegoId, etapas: [{ etapaId, orden, intentos: [{ participanteId, equipoId?, resultado, instante }] }] }`

- [ ] Step 1: tests RED del handler de envíos (BDT activo con intentos → DTO correcto; juego Trivia → 409; sin juego activo → 409/404 patrón hermano) + tests de publisher/eventos si aplica.
- [ ] Step 2: implementación (evento trailing, publishers rellenan, payloads, SignalR mapper, query+endpoint).
- [ ] Step 3: suite completa verde (506 + nuevos). Verificar aditividad: ningún test existente de eventos rompe (defaults null).
- [ ] Step 4: contratos actualizados.
- [ ] Step 5: commit `feat(operaciones): payloads de cierre con respuesta correcta y ganador + GET envios-tesoro (7d)` + trailer.

### Task 2: Web — resultado por etapa + panel de envíos + lista de inscritos en lobby

**Files:**
- Modify: `frontend/src/features/partidas/SesionOperadorPage.tsx` / `BdtRuntimePanel` (+ `useSesionHub` types si tipa payloads): al recibir `EtapaCerrada`/`EtapaGanada`, mostrar resultado por etapa: "Ganada por {ganadorEquipoId ?? ganadorParticipanteId}" o "Nadie consiguió el tesoro" (histórico simple en el panel, estado local acumulado por etapa) — testid `resultado-etapa`
- Add: panel "Envíos de tesoro" en la vista runtime BDT (patrón GET-en-señal existente: refetch de `getEnviosTesoro` al recibir señales de etapa): tabla intentos por etapa (participante/equipo, resultado, hora) — testid `envios-tesoro-panel`; función API `getEnviosTesoro(partidaId, token, fetchImpl?)` en `operacionesApi.ts` + tests
- Modify: `LobbyView` — en modalidad Individual, lista de `lobby.participantes` (IDs, precedente de la tabla de equipos que muestra `equipoId` crudo) — testid `lobby-participantes`
- Tests: API + render de los 3 elementos (con payload ganador, sin ganador, lista de inscritos)

- [ ] Step 1: tests RED (API + panels). Step 2: implementación. Step 3: `npx vitest run` + `tsc -b` verdes. Step 4: commit `feat(web): resultado por etapa, panel envíos tesoro y lista de inscritos (7d)` + trailer.

### Task 3: Mobile — respuesta correcta al cierre + aviso de líder

**Files:**
- Modify: `mobile/src/features/partidas/TriviaPlayPanel.tsx` (y el wiring del payload en `PartidaLiveScreen.tsx`/`sesionHub.js` si tipa): al recibir `PreguntaCerrada` con `textoOpcionCorrecta`, mostrar "La respuesta correcta era: {texto}" (visible para quien respondió y quien no) — cubre HU-24/BR-T06
- Modify: `mobile/src/features/partidas/PartidaLobbyScreen.tsx`: en modalidad Equipo cuando `esLider === false` y sin participación, aviso explícito (patrón Notice/AppText del archivo): "Solo el líder del equipo puede preinscribir al equipo." — cubre HU-12 (hoy solo se oculta el botón con copy genérico; reusar/ajustar ese bloque)
- Tests: `node --test` de los flows/panels que el repo ya teste (extender los existentes; si el panel no tiene test, cubrir la lógica extraíble)

- [ ] Step 1: tests RED donde el arnés lo permita. Step 2: implementación. Step 3: `npm test` + `npm run typecheck` verdes. Step 4: commit `feat(mobile): respuesta correcta al cierre y aviso de líder en lobby equipo (7d)` + trailer.

### Task 4: Gates completos + ledger

- [ ] Backend suite completa · web vitest+tsc+build (artefactos borrados) · mobile test+typecheck · árbol limpio · append ledger "7d DONE" con hashes y números.
