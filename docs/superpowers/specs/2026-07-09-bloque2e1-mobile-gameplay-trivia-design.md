# Bloque 2e-1 — Mobile gameplay Trivia + estructura live del participante

**Fecha:** 2026-07-09
**Rama:** `feature/bloque-2`
**Servicios:** solo cliente mobile (consume Operaciones de Sesión + Puntuaciones vía gateway; sin cambios backend)
**Precede:** [2d panel + participación]. **Partición de 2e:** **2e-1 Trivia + estructura live (este)** · 2e-2 BDT (QR + pistas + geoloc).

## Contexto

2d dejó al participante mobile inscrito y esperando en `PartidaLobbyScreen`, que hoy solo
muestra el aviso "La partida comenzó" al recibir `PartidaIniciada`. 2e-1 construye la
experiencia de juego en vivo para Trivia y la estructura live que 2e-2 reusa para BDT.

Contrato ya vigente (sin cambios): `GET /pregunta-actual` (lectura compartida,
participant-safe — no trae la opción correcta), `POST /pregunta-actual/respuesta`
(`{opcionId}`; `RespuestaTriviaResponse {partidaId, preguntaId, esCorrecta, cerroPregunta,
puntaje?}`; 409 duplicada — en Equipo la PRIMERA respuesta del equipo sella — / fuera de
tiempo / sin pregunta activa), `GET /puntuaciones/.../ranking` y `/ranking-consolidado`
(gateway `/puntuaciones/**` = autenticado; el participante puede leer). Push existente:
`PreguntaActivada {fechaLimiteUtc}` / `PreguntaCerrada` / `JuegoActivado` /
`PartidaFinalizada` / `PartidaCancelada`.

## Alcance

### 1. Ruta y navegación al live

- Ruta nueva `PartidaLive: { partidaId: string; nombre: string }` en `AppStackParamList` +
  screen registrado en `RootNavigator`.
- `PartidaLobbyScreen`: al recibir `PartidaIniciada` **o** si `cargarLobby` encuentra la
  sesión ya `Iniciada` → `navigation.replace("PartidaLive", { partidaId, nombre })`
  (replace: back no vuelve a un lobby muerto). El aviso "La partida comenzó" desaparece
  (lo reemplaza la navegación).
- `PartidasPanelScreen`, banner mi-sesión: si `estadoPartida === "Iniciada"` navega a
  `PartidaLive`; si `"Lobby"` navega a `PartidaLobby` (hoy siempre lobby).

### 2. `gameplayApi.js` (nuevo, patrón result-object del feature)

- `getPreguntaActual(apiBaseUrl, token, partidaId, fetchImpl?)` — GET
  `/operaciones-sesion/partidas/{id}/pregunta-actual`; `409` → `{ok: false, type:
  "sin_pregunta"}` (estado válido "entre preguntas", no error fatal).
- `responderPregunta(apiBaseUrl, token, partidaId, opcionId, fetchImpl?)` — POST
  `.../pregunta-actual/respuesta` body `{opcionId}`; 200 → `{ok: true, data}`; 409 →
  conflict con `body.message` (duplicada / fuera de tiempo / carrera de barrido).
- `getRankingJuego(apiBaseUrl, token, partidaId, juegoId, fetchImpl?)` — GET
  `/puntuaciones/partidas/{id}/juegos/{juegoId}/ranking`.
- `getRankingConsolidado(apiBaseUrl, token, partidaId, fetchImpl?)` — GET
  `/puntuaciones/partidas/{id}/ranking-consolidado` (404/409 → mapeado; la vista ofrece
  Reintentar).
- Reusa `mapCommonError`/`networkError` de `partidasPublicadasApi.js`.

### 3. `partidaLiveFlow.js` (nuevo)

- `cargarLive({apiBaseUrl, token, partidaId, fetchImpl})` → via `getMiSesion`:
  - sesión `null` o `partidaId` distinto → `{ok: true, fase: "sin-participacion"}`.
  - `estadoPartida === "Lobby"` → `{ok: true, fase: "lobby"}` (la pantalla vuelve al lobby).
  - `estadoPartida === "Iniciada"` → `{ok: true, fase: "iniciada", juegoActivo,
    yaRespondio: yaRespondioPreguntaActual}`.
  - error → result object mapeado.

### 4. `PartidaLiveScreen.tsx` + `TriviaPlayPanel`

- Estados de vista: cargando / sin-participación (mensaje + volver al panel) / iniciada /
  finalizada / cancelada.
- Hub propio (mismo patrón lifecycle que `PartidaLobbyScreen`: handlers antes de `start`,
  ref anti-stale, `stop().catch` en cleanup):
  - `PreguntaActivada` → refetch pregunta (y resetea el estado "respondido" local).
  - `PreguntaCerrada` → refetch pregunta (409 esperado → "esperando la siguiente pregunta")
    + refetch ranking.
  - `JuegoActivado` → refetch mi-sesión (`cargarLive`) — cambia el juego activo.
  - `PartidaFinalizada` → fase finalizada.
  - `PartidaCancelada` → aviso con motivo.
- **`TriviaPlayPanel`** (juego activo `Trivia`):
  - `getPreguntaActual` → texto + opciones como botones; countdown a
    `fechaActivacion + tiempoLimiteSegundos` (mismo cómputo que web 2c-2).
  - Responder UNA vez: tras `responderPregunta` muestra resultado propio (`esCorrecta`,
    `puntaje` si vino) + "esperando el cierre de la pregunta"; botones deshabilitados. El
    flag `yaRespondio` de mi-sesión cubre reconexión (en Equipo significa "mi equipo ya
    respondió"). 409 duplicada → mismo estado "ya respondida" (no error fatal).
  - Ranking del juego bajo la pregunta: `getRankingJuego` refetcheado en señales
    (GET-en-señal, patrón 2c-2; sin segundo hub — SP-4c sigue diferido). Tabla RN simple:
    posición · competidor (`slice(0,8)`) · puntos.
- Juego activo `BusquedaDelTesoro` → placeholder "Búsqueda del tesoro disponible en la
  próxima actualización" (2e-2 lo reemplaza).
- **Fase finalizada**: `getRankingConsolidado` (un intento + botón Reintentar; sin el retry
  automático de la web — el push `PartidaFinalizada` y el tap manual bastan en mobile).
  Tabla posición · competidor · juegos ganados · puntos totales; **fila propia resaltada**
  comparando `competidorId` con el `sub` del token (`tokenClaims.js` existente; en Equipo
  la fila del equipo no matchea el sub — sin resaltado, aceptable).

### 5. Fix del minor 2d — lobby Equipo para miembros

- `cargarLobby` (flow existente) gana `esLider`: en modalidad `Equipo`, GET
  `/identity/teams/mine` → `liderUserId === sub` del token; en `Individual` no se consulta.
- `PartidaLobbyScreen`, modalidad Equipo: líder ve Preinscribir/Cancelar como hoy; miembro
  no-líder NO ve botón de acción — texto "El líder gestiona la preinscripción del equipo"
  (su convocatoria se gestiona en la pantalla Convocatorias).

## Gate

- Tests de flows/apis (`node --test`, imports ESM) + `npm run typecheck` verdes.
- E2E vivo vía gateway :5080: participante en partida Trivia iniciada → `GET
  /pregunta-actual` 200 sin campo de correcta → responder correcto 200
  `{esCorrecta:true, puntaje}` → segunda respuesta 409 duplicada → ranking del juego 200
  con puntos → finalizar → consolidado 200. Smoke SignalR: participante recibe
  `PreguntaActivada {fechaLimiteUtc}` + `PreguntaCerrada`.
- Traceability fila 2e-1.

## No-objetivos

- BDT: QR (cámara/galería), pistas (`PistaEnviada`), geolocalización (`EnviarUbicacion`) →
  **2e-2** (reusará `permissions/bdtGeolocationPermission.js` y `bdtTreasureImagePicker.js`
  huérfanos, o los reemplazará — decisión de 2e-2).
- Hub de rankings SignalR (SP-4c) — GET-en-señal alcanza.
- RNF-24 (refresh 270s) — mini-slice aparte.
- Resolución de nombres de competidor (contrato entrega GUIDs).

## Sizing preliminar (writing-plans detalla)

~6 tareas: T1 `gameplayApi` + tests (haiku, verbatim) · T2 `partidaLiveFlow` + tests
(sonnet) · T3 `PartidaLiveScreen` + `TriviaPlayPanel` + registro de ruta (sonnet) · T4
navegación lobby→live + banner del panel (sonnet) · T5 fix líder en lobby Equipo (sonnet) ·
T6 gate E2E + traceability (controller). Reviewers sonnet, review final opus.
