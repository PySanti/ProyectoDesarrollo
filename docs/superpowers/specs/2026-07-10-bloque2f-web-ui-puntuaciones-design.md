# Bloque 2f — UI Puntuaciones web (historial de partida + rendimiento de equipo)

**Fecha:** 2026-07-10
**Rama:** `feature/bloque-2`
**Servicios:** solo frontend web (consume Puntuaciones vía gateway; endpoints SP-4 ya vigentes, sin cambios backend/contrato)
**Precede:** [2e gameplay mobile]. **Último slice del Bloque 2.**

## Contexto

La web (operador/admin) ya consume Puntuaciones en vivo: ranking por juego en la consola de
sesión (2c-2/2c-3) y consolidado al terminar (2c-4). Del contrato quedan sin UI web dos
lecturas históricas ya implementadas en el backend (SP-4):

- `GET /puntuaciones/partidas/{partidaId}/historial` (HU-43) — relato cronológico de los 17
  tipos de evento; query `limit` (default 100, máx 500) / `offset` / `tipo` (filtro exacto);
  orden fijo `occurredAt ASC`; `total` con filtro aplicado. **403 con rol `Participante`**
  (primer endpoint de Puntuaciones con chequeo de rol propio); 404 partida desconocida;
  partida sin eventos → `200 entradas: []`.
- `GET /puntuaciones/equipos/{equipoId}/rendimiento` (HU-49/RF-44) — por cada partida por
  equipos terminada con ≥1 marcador del equipo: `{partidaId, fechaFin, posicion, gano}`,
  orden `fechaFin DESC`; equipo sin participaciones o desconocido → `200 partidas: []`.

Decisiones del usuario (brainstorming): alcance = historial + rendimiento · entrada a
rendimiento = página con campo de equipoId (no hay vista web de equipos aún) · historial =
ruta hija de partida.

## Alcance

### 1. `puntuacionesApi.ts` — +2 funciones (patrón `PuntuacionesApiError` existente)

- `getHistorialPartida(apiBaseUrl, token, partidaId, opts?)` con `opts = {limit?, offset?,
  tipo?}` — arma query string solo con los presentes; devuelve `HistorialPartidaDto
  {partidaId, total, entradas: EventoHistorialDto[]}` con `EventoHistorialDto {occurredAt,
  tipoEvento, juegoId: string|null, participanteId: string|null, equipoId: string|null,
  detalle: unknown}`. Errores → `PuntuacionesApiError` con status (mismo manejo que
  `getRankingConsolidado`).
- `getRendimientoEquipo(apiBaseUrl, token, equipoId)` — devuelve `RendimientoEquipoDto
  {equipoId, partidas: RendimientoPartidaDto[]}` con `RendimientoPartidaDto {partidaId,
  fechaFin, posicion, gano}`.

### 2. `HistorialPartidaPage.tsx` — ruta `partidas/:partidaId/historial`

- Tabla cronológica: hora local legible (`occurredAt`), `tipoEvento`, `juegoId`/
  `participanteId`/`equipoId` en GUID corto (`slice(0,8)`, "—" si null), `detalle` como JSON
  compacto (`JSON.stringify`) en celda con elipsis/scroll.
- Paginación: `limit` fijo 100; botones Anterior/Siguiente deshabilitados en los bordes;
  indicador "X–Y de {total}".
- Filtro por tipo: `<select>` con "Todos" + los 17 tipos canónicos del contrato de eventos
  (lista fija en el módulo: PartidaPublicadaEnLobby, PartidaIniciada, PartidaCancelada,
  PartidaFinalizada, JuegoActivado, JuegoFinalizado, PreguntaTriviaActivada,
  RespuestaTriviaValidada, PuntajeTriviaIncrementado, PreguntaTriviaCerrada,
  EtapaBDTActivada, TesoroQRValidado, EtapaBDTGanada, EtapaBDTCerrada, PistaEnviada,
  ConvocatoriaCreada, ConvocatoriaRespondida, UbicacionActualizada — la lista exacta se
  toma de `contracts/events/operaciones-sesion-events.md` en writing-plans). Cambiar el
  filtro resetea `offset` a 0.
- Estados: cargando (spinner) / error (404 → "La partida no existe en la proyección";
  otros → mensaje del error) / vacío ("Sin eventos registrados").
- Entradas: botón/Link "Historial" en `PartidaDetailPage` + Link en `SesionOperadorPage`
  (vista terminada, junto al consolidado).

### 3. `RendimientoEquipoPage.tsx` — ruta `puntuaciones/equipos` + nav

- Item de navegación nuevo "Puntuaciones" → "Rendimiento de equipo" (visible para
  Operador y Administrador, como el resto de la web).
- Input de `equipoId` + botón Consultar; validación de formato GUID client-side (usabilidad;
  el backend es autoritativo).
- Resultado: tabla partida (GUID corto) · fecha fin (local) · posición · ganó (✓/—).
- `partidas: []` → "El equipo no tiene participaciones en partidas terminadas."
- Estados: cargando / error de red / resultado.

### 4. Tests (vitest, patrón del feature)

- Api: query string con/sin filtros, shapes, error mapping (mock fetch como
  `puntuacionesApi.test.ts` existente).
- Páginas: render de tabla con datos, estados vacío/error, interacción de filtro
  (resetea offset) y paginación, validación GUID en rendimiento (patrón de tests de
  páginas existentes del feature partidas).

## Gate

- `cd frontend && npm test` + `npx tsc -b` (borrar artefactos si genera) + `npm run build`
  verdes.
- E2E vivo vía gateway :5080: historial de una partida jugada → token operador `200` con
  eventos reales (los 17 tipos no exigidos; basta ≥1), filtro `tipo` aplicado reduce
  `total`, paginación `limit/offset` respeta `total`; **`403` con token participante**;
  `404` con partidaId inexistente. Rendimiento: `200` con `partidas: []` para GUID sin
  datos (el cálculo con datos ya está probado en SP-4b backend; wiring web es lo que se
  verifica aquí).
- Traceability fila 2f + nota GUIA (**cierre del Bloque 2**).

## No-objetivos

- `GET .../marcadores/{competidorId}` (marcador propio — participante/mobile).
- HU-27 historial del participante (actor Participante → mobile; mini-slice aparte si se
  pide).
- Buscador/listado de equipos web (llega con la vista web de equipos).
- Resolución de nombres (contrato entrega GUIDs).
- Hub de rankings SignalR (SP-4c).

## Sizing preliminar (writing-plans detalla)

~4 tareas: T1 api +2 fns + tests (haiku/sonnet) · T2 `HistorialPartidaPage` + ruta +
enlaces + tests (sonnet) · T3 `RendimientoEquipoPage` + nav + ruta + tests (sonnet) ·
T4 gate E2E + traceability (controller). Reviewers sonnet, review final opus.
