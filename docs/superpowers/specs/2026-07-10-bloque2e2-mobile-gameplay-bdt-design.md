# Bloque 2e-2 — Mobile gameplay BDT (QR + pistas + geolocalización)

**Fecha:** 2026-07-10
**Rama:** `feature/bloque-2`
**Servicios:** solo cliente mobile (consume Operaciones de Sesión + Puntuaciones vía gateway; sin cambios backend/contrato)
**Precede:** [2e-1 Trivia + estructura live]. **Cierra el Bloque 2e**; sigue 2f (UI Puntuaciones web).

## Contexto

2e-1 dejó `PartidaLiveScreen` con máquina de 6 fases, hub SignalR propio, `liveShared`
(`Countdown`/`RankingTable`) y `TriviaPlayPanel`; el juego activo `BusquedaDelTesoro` muestra
un placeholder. 2e-2 lo reemplaza por el gameplay BDT completo del participante.

Contrato ya vigente (sin cambios): `GET /etapa-actual` (lectura compartida, participant-safe —
`EtapaActualDto {partidaId, juegoId, etapaId, orden, areaBusqueda, tiempoLimiteSegundos,
fechaActivacion}`, nunca `codigoQREsperado`; 409 sin etapa activa), `POST /etapa-actual/tesoro`
(`{imagenBase64}` → `ValidacionTesoroResponse {partidaId, etapaId, resultado ∈
{Valido, Invalido, NoLegible, NoCorrespondeEtapaActiva}, gano, cerroEtapa, puntaje?}`;
**reintentos ilimitados** — a diferencia de Trivia, un QR incorrecto no sella nada, sin 409 de
duplicado; el backend decodifica la imagen server-side, RF-29). Push existente: `EtapaActivada
{fechaLimiteUtc}` / `EtapaCerrada` / `EtapaGanada` / `PistaEnviada {texto, timestampUtc, ...}`
(event-only al grupo `participante:{id}` o `equipo:{id}`; sin replay — si el destino está
offline la pista se pierde, transitorio por contrato). Cliente→servidor: `EnviarUbicacion(lat,
long)` (~cada 2s; solo participante, requiere `SuscribirAPartida` previo; relay puro al grupo
operador; rango validado server-side). Geolocalización **mandatoria** para BDT activo (SRS).

Decisiones del usuario (brainstorming): reusar huérfanos `permissions/bdt*` adaptados ·
incluir los 2 minors de código de 2e-1 · cámara + galería como fuentes de imagen.

## Alcance

### 1. `gameplayApi.js` — +2 funciones (mismo patrón result-object del feature)

- `getEtapaActual(apiBaseUrl, token, partidaId, fetchImpl?)` — GET
  `/operaciones-sesion/partidas/{id}/etapa-actual`; `409` → `{ok: false, type: "sin_etapa"}`
  (estado válido "entre etapas", no error fatal); resto → `mapCommonError`.
- `validarTesoro(apiBaseUrl, token, partidaId, imagenBase64, fetchImpl?)` — POST
  `.../etapa-actual/tesoro` body `{imagenBase64}`; 200 → `{ok: true, data}` (el resultado de
  la validación viene en `data.resultado`/`gano`/`puntaje` — un `Invalido` es 200, no error).
- Reusa `mapCommonError`/`networkError` y el helper `get()` interno existentes.

### 2. Huérfanos `permissions/bdt*` — reuso adaptado

- `bdtGeolocationPermission.js`: se reusa **tal cual** (foreground permission, result-object
  `{granted, unavailable}`).
- `bdtTreasureImagePicker.js`: `pickBdtTreasureImage` gana `base64: true` en las opciones de
  `launchCameraAsync`/`launchImageLibraryAsync` y el resultado expone `image.base64` (el
  contrato pide `{imagenBase64}`, no upload multipart). El resto (permisos cámara+galería,
  fallbacks, result-objects) queda igual. Tests existentes de estos módulos se ajustan si los
  hay; si no, se añaden los mínimos del nuevo campo.

### 3. `BdtPlayPanel.tsx` (nuevo, hermano de `TriviaPlayPanel`)

- `getEtapaActual` → `areaBusqueda` (texto descriptivo) + orden de etapa + `Countdown`
  (liveShared) a `fechaActivacion + tiempoLimiteSegundos` (mismo cómputo que Trivia).
- Botones **Cámara** y **Galería** → `pickBdtTreasureImage(source)` → `validarTesoro` con
  `image.base64`. Resultado:
  - `Valido` + `gano` → aviso success "¡Etapa ganada!" (+ `puntaje` si vino).
  - `Invalido` / `NoLegible` / `NoCorrespondeEtapaActiva` → aviso error con el motivo +
    botones habilitados (reintento ilimitado, contrato BDT).
  - Permiso denegado / picker cancelado → aviso info, sin request.
- **Pistas**: lista acumulada de la sesión (prop `pistas: {texto, timestampUtc}[]` desde el
  screen), render simple bajo la etapa (Notice/Card por pista, más reciente arriba).
- Ranking del juego bajo la etapa: `getRankingJuego` (ya existe, genérico) refetcheado en
  señales (GET-en-señal, patrón 2c-2/2e-1); `RankingTable` de liveShared.
- `resetSignal`/`refetchSignal` con la misma semántica que TriviaPlayPanel (reset limpia
  resultado del intento y el aviso al activarse etapa nueva).

### 4. `PartidaLiveScreen` — rama BDT + geolocalización + pistas

- Placeholder `BusquedaDelTesoro` → `<BdtPlayPanel key={juegoId} ...>`.
- Handlers hub nuevos (mismo patrón refs anti-stale): `EtapaActivada` → reset+refetch;
  `EtapaCerrada` / `EtapaGanada` → refetch (etapa + ranking); `PistaEnviada` → append a
  estado `pistas` (se limpia al cambiar de juego).
- **Geolocalización**: efecto activo mientras el juego activo es BDT y la fase es `iniciada`:
  `requestBdtGeolocationPermission()` → si `granted`, `Location.watchPositionAsync({accuracy:
  Balanced, timeInterval: 2000, distanceInterval: 0}, cb)` y cada callback invoca
  `hub.invoke("EnviarUbicacion", lat, long)` (best-effort, `.catch(() => {})`); `remove()` del
  watcher al cambiar de juego/fase/desmontar. Permiso denegado o unavailable → Notice
  persistente "La geolocalización es obligatoria en Búsqueda del Tesoro" (el backend es
  autoritativo; el mobile no bloquea el gameplay).
- El envío usa el hub existente de la pantalla (ya suscrito vía `SuscribirAPartida`).

### 5. Minors diferidos de 2e-1 (mismo terreno)

- `liveShared.RankingTable`: columna **opcional** `juegosGanados` (solo se renderiza si las
  entradas la traen); la fase finalizada del consolidado pasa a mostrar posición · competidor
  · juegos ganados · puntos (spec 2e-1 §4 original).
- `TriviaPlayPanel`: `setError(null)` en el reset effect (el banner de error de la pregunta
  anterior no debe sangrar a la siguiente).

## Gate

- Tests de flows/apis (`node --test`, imports ESM) + `npm run typecheck` verdes.
- E2E vivo vía gateway :5080 (stack completo + puntuaciones): partida BDT 2 etapas →
  participante `GET /etapa-actual` 200 **sin `codigoQREsperado`** (anti-leak) → tesoro con QR
  incorrecto 200 `{resultado: Invalido, gano: false}` → reintento (sin 409) → QR correcto 200
  `{resultado: Valido, gano: true, puntaje}` → ranking del juego 200 con token participante →
  finalizar → consolidado 200. QR de prueba generado localmente (imagen PNG con el texto
  esperado de la etapa).
- Smoke SignalR participante: `EtapaActivada {fechaLimiteUtc}` + `EtapaGanada`/`EtapaCerrada`
  + `PistaEnviada {texto}` (operador envía pista vía POST `/pistas`). Smoke geoloc: cliente
  participante invoca `EnviarUbicacion` y un cliente operador suscrito recibe
  `UbicacionActualizada`.
- Traceability fila 2e-2 + nota GUIA (cierre Bloque 2e).

## No-objetivos

- Mapa de geolocalización del participante (el mapa es del operador/web, 2c-3).
- Hub de rankings SignalR (SP-4c) — GET-en-señal alcanza.
- RNF-24 (refresh 270s) — mini-slice aparte.
- Resolución de nombres de competidor (contrato entrega GUIDs).
- Persistencia/replay de pistas (event-only por contrato; offline las pierde).

## Sizing preliminar (writing-plans detalla)

~6 tareas: T1 `gameplayApi` BDT + tests · T2 picker base64 + tests · T3 `BdtPlayPanel` ·
T4 screen (hub BDT + geoloc + pistas) · T5 minors 2e-1 (RankingTable columna + error reset) ·
T6 gate E2E + traceability (controller). Reviewers sonnet, review final opus.
