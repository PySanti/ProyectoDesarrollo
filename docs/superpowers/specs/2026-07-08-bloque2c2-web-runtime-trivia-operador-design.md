# Bloque 2c-2 — Runtime Trivia del operador en la consola de sesión (web)

**Fecha:** 2026-07-08 · **Rama:** `feature/bloque-2` (2a+2b+2c-1 debajo, HEAD 5e7e331) · **Skill:** subagent-driven-development
**HU (lado operador):** sincronización y avance de preguntas Trivia + ranking del juego en vivo (HU-18/20/21 lado operador, supervisión Trivia).
**Servicios (backend, ya construidos):** Operaciones de Sesión (runtime) + Puntuaciones (ranking, lectura). **Cliente:** web (Operador). **Contra:** gateway `:5080`.

## Contexto

2c-1 dejó la consola `/partidas/:partidaId/sesion` operativa hasta el shell post-inicio (lista de juegos, juego actual resaltado, sin controles de runtime). Este slice llena el runtime **Trivia**: pregunta activa con countdown, avance manual, finalización del juego y ranking del juego en vivo. El runtime BDT (etapa/pistas/geoloc) es 2c-3; el consolidado y el retiro de páginas legacy, 2c-4.

Consumo web puro contra el gateway. **Cero cambios de backend, contratos ni reglas de negocio.**

### Contratos que se consumen (fijos)

`contracts/http/operaciones-sesion-api.md`:
- `GET /operaciones-sesion/partidas/{id}/pregunta-actual` — autenticado → `200 + PreguntaActualDto { partidaId, juegoId, preguntaId, orden, texto, tiempoLimiteSegundos, fechaActivacion, opciones[{opcionId, texto}] }` (participant-safe: nunca `esCorrecta`). `409` sin pregunta activa · `404` sesión no existe.
- `POST /operaciones-sesion/partidas/{id}/pregunta-actual/avance` — policy `GestionarPartidas` → `200 + AvancePreguntaResponse { partidaId, preguntaCerradaOrden, preguntaActivadaOrden?, sinMasPreguntas }`. `409` no iniciada / juego no Trivia / sin pregunta activa.
- `POST /operaciones-sesion/partidas/{id}/juego-actual/finalizacion` — policy `GestionarPartidas` → `200 + AvanceJuegoResponse { partidaId, estado, juegoFinalizadoOrden?, juegoActivadoOrden?, terminada }`. `409` no iniciada.
- Hub sesión (ya conectado vía `useSesionHub`): mensajes nuevos a cablear `PreguntaActivada { partidaId, juegoId, preguntaId, orden, fechaLimiteUtc }` y `PreguntaCerrada { partidaId, juegoId, preguntaId }`. `fechaLimiteUtc` = activación + tiempo límite (countdown local). Payloads delgados: el contenido se trae por pull.
- Concurrencia (SP-3f-1): avance/inicio pueden devolver `409` si un barrido tocó la sesión en ese instante → el cliente refetchea y sigue.

`contracts/http/puntuaciones-api.md`:
- `GET /puntuaciones/partidas/{id}/juegos/{juegoId}/ranking` — autenticado → `200 + { juegoId, tipoJuego, generadoEn, entradas[{ posicion, competidorId, tipoCompetidor, puntos, tiempoAcumuladoMs, unidadesGanadas }] }`. Empates comparten posición (1,2,2,4). Juego sin marcadores → `entradas: []`. `404` juego no proyectado.
- Proyección alimentada por RabbitMQ best-effort (ADR-0012): la proyección puede no existir aún (404) → la UI lo trata como "sin datos todavía", no error fatal.

### Decisiones de diseño (aprobadas)

1. **Ranking por GET disparado por señales del hub de sesión, sin segundo hub SignalR.** En Trivia los puntos cambian exactamente al cerrar una pregunta (primera respuesta correcta la cierra para todos); `PreguntaCerrada`/`PreguntaActivada` ya llegan por el hub de sesión → refetch de `GET .../ranking` en esas señales = mismo tiempo-real efectivo sin segunda conexión. El hub ranking (SP-4c) queda diferido a cuando haga falta de verdad (2c-3/2c-4).
2. **El operador ve la opción correcta marcada.** La consola cruza la pregunta activa con la config (`getPartida`, que ya expone `esCorrecta` — mismo dato que el detalle 2b muestra con badge "Correcta"). Sin leak: la web es solo Operador/Administrador.
3. **Arrastres de 2c-1 se pagan aquí:** F1 seq-guard en `cargar()` (era benigno en 2c-1; con `PreguntaActivada`/`PreguntaCerrada` disparando recargas en ráfaga se vuelve necesario) y F5 pills de juegos coloreadas por `juego.estado` real.

## Componentes

### 1. `frontend/src/api/operacionesApi.ts` (extender) (+ tests)

Mismo patrón existente (`request<T>`, `fetchImpl` injectable):
- `getPreguntaActual(partidaId, accessToken, fetchImpl?): Promise<PreguntaActualDto>` — el `409` (sin pregunta activa) lo maneja el llamador como estado válido, no como error fatal (la error class ya porta `statusCode`).
- `avanzarPregunta(partidaId, accessToken, fetchImpl?): Promise<AvancePreguntaResponse>`.
- `finalizarJuegoActual(partidaId, accessToken, fetchImpl?): Promise<AvanceJuegoResponse>`.
- Tipos nuevos: `PreguntaActualDto`, `OpcionPregunta { opcionId, texto }`, `AvancePreguntaResponse`, `AvanceJuegoResponse` (campos exactos del contrato).

### 2. `frontend/src/api/puntuacionesApi.ts` (nuevo) (+ tests)

Cliente mínimo, mismo patrón:
- `class PuntuacionesApiError extends Error { statusCode }`.
- `getRankingJuego(partidaId, juegoId, accessToken, fetchImpl?): Promise<RankingJuegoDto>`.
- Tipos: `RankingJuegoDto { juegoId, tipoJuego, generadoEn, entradas: RankingEntrada[] }`, `RankingEntrada { posicion, competidorId, tipoCompetidor, puntos, tiempoAcumuladoMs, unidadesGanadas }`.
- **Limitación conocida (proyecto-wide):** el contrato no da resolución de nombres para `competidorId` → la UI muestra el GUID acortado (primeros 8 chars). Igual en móvil; se documenta, no se inventa endpoint.

### 3. `frontend/src/features/partidas/useSesionHub.ts` (extender) (+ test)

`SesionHubHandlers` gana:
- `onPreguntaActivada?: (p: { partidaId: string; juegoId: string; preguntaId: string; orden: number; fechaLimiteUtc: string }) => void`
- `onPreguntaCerrada?: (p: { partidaId: string; juegoId: string; preguntaId: string }) => void`

Registrar `connection.on("PreguntaActivada"/"PreguntaCerrada", ...)` con el mismo patrón `handlersRef`. Los eventos BDT (`EtapaActivada`/`EtapaCerrada`/`EtapaGanada`) son 2c-3.

### 4. `frontend/src/features/partidas/TriviaRuntimePanel.tsx` (nuevo) (+ test)

La página ya ronda las 290 líneas → el runtime vive en componente propio con frontera limpia.

Props: `{ partidaId: string; juegoId: string; accessToken: string; preguntasConfig: PreguntaDetail[]; refetchSignal: number; onTerminada: () => void; onJuegoAvanzado: () => void }`.
- `refetchSignal`: contador que la página incrementa al recibir `PreguntaActivada`/`PreguntaCerrada`/`JuegoActivado` — el panel refetchea pregunta y ranking en su efecto (evita pasar la conexión del hub hacia abajo; el hub queda en la página).
- `onTerminada`: el panel lo llama cuando `finalizarJuegoActual` devuelve `terminada: true` (la página muestra la vista terminal). `onJuegoAvanzado`: cuando devuelve `juegoActivadoOrden` (la página recarga estado — juego siguiente).

Estados internos del panel:
- **Pregunta activa** (`getPreguntaActual` 200): texto, orden, opciones en lista con la correcta marcada (cruce por `opcionId` — `OpcionDetail` de config sí lo expone; si Operaciones regenerara IDs en el snapshot, fallback por texto de opción dentro de la pregunta de mismo orden; si nada matchea, sin badge, sin romper — el E2E de T6 verifica cuál de los dos cruces aplica en vivo), countdown a `fechaLimiteUtc` calculada como `fechaActivacion + tiempoLimiteSegundos` (el pull no trae `fechaLimiteUtc`; el push sí — usar la del push cuando llegue, la calculada como fallback), botón **"Cerrar y avanzar"** (`avanzarPregunta`, disabled-while-posting; `409` → refetch, no error fatal).
- **Sin pregunta activa** (`getPreguntaActual` 409): panel "Sin pregunta activa" + botón **"Finalizar juego"** (`finalizarJuegoActual`, disabled-while-posting) — cubre el fin natural (última pregunta cerrada, `sinMasPreguntas`).
- **Ranking del juego**: tabla `posición · competidor (GUID corto) · puntos · tiempo (mm:ss desde Ms) · unidades ganadas` de `getRankingJuego`; `404` de proyección → "Sin datos de ranking todavía" (best-effort ADR-0012); `entradas: []` → misma leyenda.

testids: `trivia-runtime`, `pregunta-activa`, `sin-pregunta-activa`, `btn-avanzar-pregunta`, `btn-finalizar-juego`, `ranking-juego`, `opcion-correcta` (en la opción marcada), reuso de `inicio-countdown`-style para el countdown (`pregunta-countdown`).

### 5. `frontend/src/features/partidas/SesionOperadorPage.tsx` (editar) (+ tests)

- **F1 seq-guard:** `const seq = useRef(0)`; `cargar()` captura `const my = ++seq.current` y antes de cada `setVista` chequea `my === seq.current` (descarta respuestas stale de recargas solapadas).
- **F5:** pills de la lista de juegos coloreadas por `juego.estado` (`Activo` → `pill--live`, `Finalizado` → `pill--done`, `Pendiente` → `pill--lobby` o neutra), no `pill--done` fijo.
- Vista `iniciada`: si el juego actual (por `juegoActualOrden`) es `tipoJuego === "Trivia"` → monta `TriviaRuntimePanel` con `preguntasConfig` del juego (la vista iniciada ahora también carga `getPartida` en `cargar()` para tener la config); si es BDT → placeholder "El runtime BDT llega en 2c-3".
- Hub: `onPreguntaActivada`/`onPreguntaCerrada` incrementan `refetchSignal` (estado numérico) — NO llaman `cargar()` completo (la pregunta/ranking los refetchea el panel; el estado global de la sesión no cambió). `onJuegoActivado` sigue llamando `cargar()` (cambia el juego actual) y además incrementa la señal.

### 6. Manejo de errores

- `409` en `getPreguntaActual` → estado "sin pregunta activa" (válido).
- `409` en `avanzarPregunta`/`finalizarJuegoActual` (carrera con barrido, juego no Trivia, no iniciada) → refetch (señal/cargar), sin banner fatal; mensaje inline suave si persiste.
- `404` ranking (proyección no existe aún) → leyenda "sin datos todavía", nunca error fatal.
- Red (TypeError) → mensaje genérico acotado (patrón del slice).

### 7. Testing

- **Unit:** apis nuevas (paths exactos, 200/409/404, error class); hook (2 eventos nuevos ruteados); panel (activa→render con correcta marcada + countdown, avance→refetch, 409→sin-pregunta-activa→finalizar→onTerminada/onJuegoAvanzado, ranking render + 404 leyenda); página (panel montado solo con Trivia activo, placeholder BDT, seq-guard pinneado con test de carrera de dos cargas solapadas, pills por estado).
- **E2E gate (vivo, gateway :5080, tokens reales):** publicar (min 1) → inscribir participante (`POST /inscripciones`, token participante) → iniciar (200 Iniciada) → `GET /pregunta-actual` 200 → responder correcto (token participante, `POST /pregunta-actual/respuesta`) → `PreguntaCerrada` → `GET /puntuaciones/.../ranking` con puntos del participante → `POST /pregunta-actual/avance` (o siguiente activada) → última pregunta → `POST /juego-actual/finalizacion` → `terminada` (partida de 1 juego). Policy: participante en `/avance` → 403. Smoke SignalR: `PreguntaActivada` con `fechaLimiteUtc` + `PreguntaCerrada` recibidos en conexión de operador. **Requiere servicio Puntuaciones + RabbitMQ arriba** (proyección del ranking).

## Fuera de alcance (explícito)

- Runtime BDT (etapa actual, avance de etapa, tesoro/QR), pistas, mapa geoloc → 2c-3.
- Hub ranking SignalR (`/puntuaciones/hubs/ranking`) → diferido (GET por señal alcanza para Trivia).
- Ranking consolidado y vista de partida terminada rica → 2c-4.
- Retiro de páginas legacy `trivia/operar` + `bdt/partidas` → 2c-4.
- Resolución de nombres de competidores (contrato no la da — GUID corto documentado).

## Sizing sugerido (lo fija writing-plans)

~6 tasks: T1 extender operacionesApi (haiku verbatim) · T2 puntuacionesApi (haiku verbatim) · T3 extender useSesionHub (haiku/sonnet) · T4 TriviaRuntimePanel (sonnet) · T5 página: F1+F5+integración panel (sonnet) · T6 gate E2E + traceability (controller). Reviewers sonnet, final opus. Gate typecheck: `npx tsc -b` (o `--noEmit -p tsconfig.app.json`), nunca `tsc --noEmit` pelado.
