# Bloque 3a — Rankings en vivo por push SignalR (SP-4c, consumo en clientes)

**Fecha:** 2026-07-10
**Rama:** `feature/bloque-2` (continúa sobre el Bloque 2 completo)
**Servicios:** solo clientes web + mobile (el hub de Puntuaciones ya existe desde SP-4; sin cambios backend/contrato)
**Partición Bloque 3:** **3a rankings push (este)** · 3b vista web de equipos · 3c RNF-24.

## Contexto

El hub `puntuaciones/hubs/ranking` está operativo en el backend (SP-4: `RankingHub` +
`SignalRRankingRealtimePublisher`) con contrato completo en
`contracts/http/puntuaciones-api.md` §"SignalR — ranking en vivo (SP-4c)":

- Conexión vía gateway (ruta `/puntuaciones/*`); JWT por query `access_token` en la
  negociación WebSocket.
- Cliente→servidor: `SuscribirAPartida(partidaId)` — `HubException("Partida no
  proyectada.")` si la partida no existe en las proyecciones (el cliente reintenta al
  recibir `PartidaEnLobby` del hub de sesión, o simplemente confía en el GET);
  `DesuscribirDePartida(partidaId)`.
- Servidor→cliente: `RankingTriviaActualizado` (disparo `PuntajeTriviaIncrementado`) y
  `RankingBDTActualizado` (disparo `EtapaBDTGanada`) — payload = shape exacto de
  `GET .../juegos/{juegoId}/ranking`; `RankingConsolidadoCalculado` (disparo
  `PartidaFinalizada`) — payload = shape de `GET .../ranking-consolidado`.
- Difusión **best-effort** (ADR-0012): push perdido no se reintenta; los GET HTTP son la
  fuente recuperable.

Hoy web y mobile refrescan rankings por GET-en-señal (señales del hub de **sesión**): el
ranking solo se mueve al cerrar pregunta/etapa. Con el push se vuelve vivo
**intra-pregunta/etapa** (cada acierto difunde).

## Decisión de diseño

**Push aditivo, no sustitutivo.** El GET inicial y el GET-en-señal existentes quedan
intactos (fuente recuperable per contrato); el push solo aplica actualizaciones más
frescas sobre el mismo estado. Última escritura gana; ambas fuentes entregan el mismo
shape. Diff mínimo y cero riesgo de regresión.

## Alcance

### 1. Web — `useRankingHub.ts` + consumo en paneles

- `frontend/src/features/partidas/useRankingHub.ts` — hook hermano de `useSesionHub`:
  crea la conexión al hub de rankings vía `VITE_GATEWAY_BASE_URL`
  (`{gw}/puntuaciones/hubs/ranking`, token por `accessTokenFactory`), registra handlers
  ANTES de `start()`, invoca `SuscribirAPartida(partidaId)` tras conectar, cleanup con
  `stop().catch`. Firma: `useRankingHub(partidaId, accessToken, handlers:
  { onRankingJuego?: (r: RankingJuegoDto) => void; onConsolidado?: (r:
  RankingConsolidadoDto) => void })` — `RankingTriviaActualizado` y
  `RankingBDTActualizado` van ambos a `onRankingJuego` (mismo shape). Handlers vía refs
  anti-stale (patrón `useSesionHub`). Fallo de conexión/suscripción → silencioso (el
  GET cubre).
- `SesionOperadorPage` monta el hook y distribuye:
  - vista Trivia/BDT activa: `onRankingJuego` → si `r.juegoId` coincide con el juego del
    panel montado → pasa las `entradas` frescas al panel (prop nueva `entradasPush` o
    set del mismo estado que hoy alimenta el ranking — writing-plans fija el mecanismo
    siguiendo el wiring real de los paneles).
  - vista terminada: `onConsolidado` → entrega el consolidado al `ConsolidadoPanel`
    (prop opcional `consolidadoPush`; el panel lo usa como dato ya resuelto — su retry
    GET queda para el caso sin push).

### 2. Mobile — `rankingHub.js` + consumo en el live

- `mobile/src/features/partidas/rankingHub.js` — `crearRankingHub(gatewayBaseUrl,
  accessToken)`, espejo de `crearSesionHub` (mismo `@microsoft/signalr@^8`, WebSockets +
  `skipNegotiation`, URL `{gw}/puntuaciones/hubs/ranking`).
- `PartidaLiveScreen` monta el segundo hub junto al de sesión (mismo lifecycle:
  handlers antes de start, refs, `stop().catch` en cleanup, suscripción best-effort
  con catch silencioso):
  - `RankingTriviaActualizado`/`RankingBDTActualizado` → estado `rankingPush:
    {juegoId, entradas} | null` → prop nueva de `TriviaPlayPanel`/`BdtPlayPanel`; el
    panel aplica `entradas` cuando `juegoId` coincide con el suyo (effect sobre la
    prop, mismo estado `entradas` que alimenta `RankingTable`).
  - `RankingConsolidadoCalculado` → en cualquier fase, set del consolidado (mapeado
    igual que el GET) — al entrar a fase finalizada ya está pintado; el GET +
    Reintentar quedan como recuperación.

### 3. Tests

- Web: test del hook (mock de `@microsoft/signalr` como en `useSesionHub.test.ts`) —
  registra handlers antes de start, suscribe con el partidaId, enruta ambos mensajes de
  juego a `onRankingJuego`; tests de wiring en la página/paneles según el mecanismo que
  el plan fije.
- Mobile: test de `rankingHub.js` (URL exacta + token factory, patrón
  `sesionHub.test.js`); el wiring de pantalla queda cubierto por typecheck (estrategia
  del repo: tests de flows/apis, no de componentes RN).

## Gate

- `cd frontend && npm test` + `npx tsc -b` + `npm run build` verdes; `cd mobile && npm
  test` + `npm run typecheck` verdes.
- E2E vivo vía gateway :5080 (stack completo): cliente Node suscrito al hub de rankings
  recibe `RankingTriviaActualizado` con `entradas` **al responder correcto sin esperar
  el cierre de la pregunta** (payload con puntos del acierto) y
  `RankingConsolidadoCalculado` al finalizar la partida. Verificar que la suscripción a
  una partida no proyectada produce el `HubException` documentado (y que el cliente lo
  traga sin romper).
- Traceability fila 3a.

## No-objetivos

- Retirar el GET inicial o el GET-en-señal (fuente recuperable; el push es aditivo).
- Cambios backend/contrato (cero — el hub y su publisher ya existen).
- Replay/retry de pushes perdidos (best-effort por ADR-0012).
- 3b (vista web equipos) y 3c (RNF-24) — slices siguientes.

## Sizing preliminar (writing-plans detalla)

~3 tareas: T1 web useRankingHub + wiring paneles + tests (sonnet) · T2 mobile
rankingHub + wiring live + tests (sonnet) · T3 gate E2E + traceability (controller).
Reviewers sonnet, review final opus.
