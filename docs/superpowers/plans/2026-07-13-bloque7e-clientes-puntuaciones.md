# Bloque 7e — Clientes de Puntuaciones + historial — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cerrar el paquete R5: **HU-27** (el participante ve su historial de partidas en mobile), **HU-49** (el participante ve el rendimiento de su equipo en mobile), **HU-43** (el historial de partida del operador archiva los 5 eventos de inscripciones de HU-19, que hoy `HistorialEventMapper` descarta — con recorte documentado: las invitaciones de equipo NO se archivan).

**Architecture:** Todo consume backend ya existente salvo el fix del mapper. Endpoints Puntuaciones listos: `GET /puntuaciones/participantes/{id}/historial-partidas` (`[Authorize]`, cualquier autenticado → participante), `GET /puntuaciones/equipos/{id}/rendimiento` (`[Authorize]`). Los 5 eventos de inscripciones (`InscripcionSolicitada`/`Aceptada`/`Rechazada`, `InscripcionEquipoCreada`/`Cancelada`) ya se publican al broker y el contrato promete archivarlos, pero `HistorialEventMapper.Tipos` no los tiene. Mobile: sin `src/api/`, las llamadas viven en `features/*/`; patrón `${mobileEnv.gatewayApiBaseUrl}/...` con bearer (ver `partidaLobbyFlow.js:35`). HU-49 web (`RendimientoEquipoPage`) se conserva intacta.

**Tech Stack:** .NET 8 (Puntuaciones + xUnit) · React Native/Expo + `node --test` (mobile).

## Global Constraints

- Rama: `feature/bloque-7`. Commits con trailer `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a implementadores: `git stash/reset/checkout/restore/clean`. Solo `git add <paths exactos>` + `git commit`.
- Gates: backend `export DOTNET_ROOT=/snap/dotnet-sdk/current; dotnet test services/puntuaciones/Umbral.Puntuaciones.sln` · mobile `cd mobile && npm test && npm run typecheck` (baseline 113/113, Node ≥ 20.19.4).
- Sin dependencias nuevas. UI español. Pantallas nuevas siguen el patrón de las existentes (`shared/ui` AppText/Stage/Hero, `navigation/types.ts` + `RootNavigator.tsx`, `HomeScreen` NavCard).
- El `id` del participante = `sub` del JWT (patrón `parseJwtPayload(token).sub`); el `equipoId` = del equipo activo vía `GET /identity/teams/mine`.

---

### Task 1: Backend — mapper archiva inscripciones + recorte documentado de invitaciones

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/HistorialEventMapper.cs` (diccionario `Tipos`)
- Modify: tests del mapper (localizar `HistorialEventMapper*Tests.cs` en `services/puntuaciones/tests/`)
- Modify: `contracts/events/operaciones-sesion-events.md` (confirmar que los 5 quedan "Registered") y `contracts/events/identity-events.md` (recorte: las invitaciones de equipo NO se archivan en el historial de partida)

**Interfaces:**
- Consumes: patrón existente `Extraccion(ParticipanteProp, EquipoProp)` del mapper; la forma común de 7 campos de los eventos de inscripción (`participanteId`/`equipoId` según modalidad; el otro null).
- Produces: `Map()` deja de devolver null para los 5 tipos de inscripción → se archivan como `EventoHistorial`.

- [ ] **Step 1: Tests RED.** En el test del mapper, añadir casos: cada uno de `InscripcionSolicitada`, `InscripcionAceptada`, `InscripcionRechazada` (Individual → extrae `participanteId`, `equipoId` null; Equipo → al revés), `InscripcionEquipoCreada`, `InscripcionEquipoCancelada` (extraen `equipoId`) → `Map()` devuelve comando no-null con el/los id extraídos y el tipo correcto. Correr filtro → RED (hoy `Map` devuelve null → los tests fallan).
- [ ] **Step 2: Implementación.** Añadir al diccionario `Tipos`, siguiendo el shape de las claves existentes (los nombres de prop camelCase reales del payload — verificar contra el sample del contrato §349):

```csharp
        ["InscripcionSolicitada"] = new("participanteId", "equipoId"),
        ["InscripcionAceptada"] = new("participanteId", "equipoId"),
        ["InscripcionRechazada"] = new("participanteId", "equipoId"),
        ["InscripcionEquipoCreada"] = new(null, "equipoId"),
        ["InscripcionEquipoCancelada"] = new(null, "equipoId"),
```

  (Verificar contra los records reales en `ParticipacionEvents.cs` de Operaciones qué props llevan: si `InscripcionEquipoCreada` porta `equipoId` y no `participanteId`, usar `(null, "equipoId")`; ajustar a la realidad del payload serializado camelCase.)
- [ ] **Step 3: GREEN + suite completa** de Puntuaciones verde.
- [ ] **Step 4: Contratos.** En `operaciones-sesion-events.md` confirmar/dejar claro que los 5 quedan archivados (ya lo dice §33-35/349-354; si algún subtipo Equipo no estaba listado como Registered, alinearlo). En `identity-events.md` añadir nota explícita: las invitaciones de equipo (`InvitacionEquipoEnviada`/`Aceptada`/`Rechazada`) son ciclo de vida de equipo y **no** se archivan en el historial de partida de Puntuaciones (recorte documentado HU-43; Puntuaciones no consume el exchange de Identity).
- [ ] **Step 5: Commit** `feat(puntuaciones): historial archiva eventos de inscripción HU-19 (7e, HU-43)` + cuerpo (recorte invitaciones documentado) + trailer.

### Task 2: Mobile — pantalla "Mi historial de partidas" (HU-27)

**Files:**
- Create: `mobile/src/features/puntuaciones/historialPartidasApi.js` (fetch + parseo del error, patrón `partidasPublicadasApi.js`) + su test
- Create: `mobile/src/features/puntuaciones/HistorialPartidasScreen.tsx` (+ container si el patrón lo usa)
- Modify: `mobile/src/navigation/types.ts` (+ `HistorialPartidas: undefined`), `RootNavigator.tsx` (+ Screen), `mobile/src/screens/HomeScreen.tsx` (+ NavCard "Mi historial" icon "clock"/"award")

**Interfaces:**
- Consumes: `GET /puntuaciones/participantes/{sub}/historial-partidas` → `HistorialPartidasResponse { participanteId, partidas: [{ partidaId, modalidad?, fechaFin?, equipoId?, puntosTotales, posicion, gano, juegos: [{ juegoId, orden, tipoJuego, puntos }] }] }`. `sub` del JWT.
- Produces: entrada de nav `HistorialPartidas`.

- [ ] **Step 1: Test RED** del api-flow (mock fetch: 200 con payload → estructura parseada; 204/empty → lista vacía; error → shape de error). Patrón `node --test` del repo.
- [ ] **Step 2: Implementar** el flow (toma `sub` de `parseJwtPayload`, llama vía `gatewayApiBaseUrl`) → GREEN.
- [ ] **Step 3: Pantalla** — lista de partidas jugadas con puntos totales, posición, ganó/no, y por partida sus juegos con puntos; estados loading/vacío/error con el patrón visual del repo (`Stage`/`AppText`/`Notice`). Empty legible: "Aún no has jugado partidas."
- [ ] **Step 4: Navegación** — types + RootNavigator Screen (title "Mi historial") + NavCard en Home.
- [ ] **Step 5: `npm test` + `npm run typecheck`** verdes. Commit `feat(mobile): pantalla Mi historial de partidas (7e, HU-27)` + trailer.

### Task 3: Mobile — pantalla "Rendimiento de mi equipo" (HU-49)

**Files:**
- Create: `mobile/src/features/puntuaciones/rendimientoEquipoApi.js` + test
- Create: `mobile/src/features/puntuaciones/RendimientoEquipoScreen.tsx` (+ container si aplica)
- Modify: `types.ts`, `RootNavigator.tsx`, `HomeScreen.tsx` (NavCard "Rendimiento de mi equipo" en la sección de equipo)

**Interfaces:**
- Consumes: `GET /identity/teams/mine` (para el `equipoId` del equipo activo; patrón `partidaLobbyFlow.js:35`) → luego `GET /puntuaciones/equipos/{equipoId}/rendimiento` → `RendimientoEquipoResponse { equipoId, partidas: [{ partidaId, fechaFin?, posicion, gano }] }`. Sin equipo activo → estado "no perteneces a un equipo".
- Produces: entrada de nav `RendimientoEquipo`.

- [ ] **Step 1: Test RED** del flow: con equipo activo → resuelve equipoId → llama rendimiento → estructura; sin equipo (teams/mine 404/vacío) → estado sin-equipo; error de rendimiento → shape error.
- [ ] **Step 2: Implementar** flow encadenado (teams/mine → rendimiento) → GREEN.
- [ ] **Step 3: Pantalla** — por partida: posición y ganó/no; estados loading/sin-equipo/vacío/error. Empty: "Tu equipo aún no ha jugado partidas."; sin-equipo: "No perteneces a un equipo activo."
- [ ] **Step 4: Navegación** (title "Rendimiento del equipo") + NavCard en Home sección equipo.
- [ ] **Step 5: gates** verdes. Commit `feat(mobile): pantalla Rendimiento de mi equipo (7e, HU-49)` + trailer.

### Task 4: Gates completos + ledger

- [ ] Backend Puntuaciones suite completa · mobile `npm test` + `npm run typecheck` · árbol limpio · append ledger "7e DONE" con hashes y números.
