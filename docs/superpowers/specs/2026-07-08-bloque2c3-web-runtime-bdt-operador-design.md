# Bloque 2c-3 — Runtime BDT del operador en la consola de sesión (web)

**Fecha:** 2026-07-08 · **Rama:** `feature/bloque-2` (2a+2b+2c-1+2c-2 debajo, HEAD b72c2dc) · **Skill:** subagent-driven-development
**HU (lado operador):** sincronización/avance de etapas BDT, entrega de pistas, mapa de geolocalización y ranking del juego (HU-18/22/23/24 lado operador).
**Servicios (backend, ya construidos):** Operaciones de Sesión (runtime BDT + geoloc relay + pistas) + Puntuaciones (ranking). **Cliente:** web (Operador). **Contra:** gateway `:5080`.

## Contexto

2c-2 completó el runtime **Trivia** en la consola `/partidas/:id/sesion` y dejó un placeholder cuando el juego actual es BDT. Este slice llena el runtime **BDT**: etapa activa con countdown, avance/cierre de etapa, finalización del juego, **entrega de pistas** a un participante o equipo, **mapa de geolocalización en vivo** de los participantes, y ranking del juego. El consolidado y el retiro de páginas legacy son 2c-4.

Consumo web puro contra el gateway. **Cero cambios de backend, contratos ni reglas de negocio.** El operador **no** valida QR ni ve `codigoQREsperado` — la subida/validación de tesoros es del participante (móvil).

### Contratos que se consumen (fijos)

`contracts/http/operaciones-sesion-api.md`:
- `GET /operaciones-sesion/partidas/{id}/etapa-actual` — autenticado → `200 + EtapaActualDto { partidaId, juegoId, etapaId, orden, areaBusqueda, tiempoLimiteSegundos, fechaActivacion }` (participant-safe: **nunca** `codigoQREsperado`). `409` sin etapa activa · `404` sesión no existe.
- `POST /operaciones-sesion/partidas/{id}/etapa-actual/avance` — policy `GestionarPartidas` → `200 + AvanceEtapaResponse { partidaId, etapaCerradaOrden, etapaActivadaOrden?, sinMasEtapas }`. `409` no iniciada / juego no BDT / sin etapa activa.
- `POST /operaciones-sesion/partidas/{id}/juego-actual/finalizacion` — reuso de 2c-2 → `AvanceJuegoResponse { estado, juegoFinalizadoOrden?, juegoActivadoOrden?, terminada }`.
- `POST /operaciones-sesion/partidas/{id}/pistas` — policy `GestionarPartidas`; body `{ participanteDestinoId?, texto, equipoDestinoId? }` (**exactamente uno** de los dos destinos, si no `400`) → `200 + PistaEnviadaResponse { partidaId, juegoId, participanteDestinoId?, timestampUtc, equipoDestinoId? }`. `404` destino no inscrito · `409` no iniciada / juego no BDT / sin etapa activa / destino equipo en partida Individual.
- `GET /operaciones-sesion/partidas/{id}/lobby` — reuso (2c-1); el roster para elegir destino de pista sale de `LobbyDto.participantes: Guid[]` (Individual) y `LobbyDto.equipos[].equipoId` (Equipo). Funciona en estado `Iniciada`.

Hub sesión (ya conectado vía `useSesionHub`): mensajes nuevos a cablear:
- `EtapaActivada { partidaId, juegoId, etapaId, orden, fechaLimiteUtc }` · `EtapaCerrada { partidaId, juegoId, etapaId }` · `EtapaGanada { partidaId, juegoId, etapaId }`.
- `UbicacionActualizada { partidaId, participanteId, latitud, longitud, timestampUtc }` — **operador-only**: se difunde solo al grupo `operador:partida:{id}`, al que el operador se une en `SuscribirAPartida` (verificado en `SesionHub.cs:70-71`). El participante emisor no lo recibe (BR-B07). Relay puro, server-stamped, sin persistencia.
- `PistaEnviada` NO llega al operador (es destino-only) — el operador confía en el `200` del `POST /pistas`.

`contracts/http/puntuaciones-api.md`:
- `GET /puntuaciones/partidas/{id}/juegos/{juegoId}/ranking` — mismo endpoint/cliente que Trivia (2c-2). Para BDT: `puntos` = Σ `Puntaje` de las etapas ganadas; `unidadesGanadas` = nº de etapas ganadas (informativo, no clave de orden). `404` proyección no existe aún → "sin datos todavía" (best-effort ADR-0012).

### Decisiones de diseño (aprobadas)

1. **Mapa con `react-leaflet` + tiles OpenStreetMap.** Nuevas deps `leaflet` + `react-leaflet` (+ `@types/leaflet`). Marcadores con **`CircleMarker`** (no `Marker`) para evitar el gotcha de los iconos-asset de leaflet bajo Vite (los PNG del marcador default 404ean); `CircleMarker` no usa imágenes. Import obligatorio de `leaflet/dist/leaflet.css`. Área de búsqueda es texto (no coordenadas) → no hay centro fijo: el mapa centra en la media de las posiciones conocidas, con una vista default hasta la primera ubicación. Tiles externos → la demo requiere internet.
2. **Destino de pista por selector de roster** (`GET /lobby`): siempre disponible (aunque nadie haya enviado ubicación), funciona igual en Individual (participante GUID) y Equipo (equipo GUID). Desacoplado del mapa.
3. **Ranking BDT por el mismo GET disparado por señales del hub** (`EtapaGanada`/`EtapaCerrada`/`EtapaActivada`), sin segundo hub SignalR — igual que Trivia en 2c-2.

## Componentes

### 1. `frontend/src/api/operacionesApi.ts` (extender) (+ tests)

Mismo patrón (`request<T>`, `fetchImpl` injectable):
- `getEtapaActual(partidaId, accessToken, fetchImpl?): Promise<EtapaActualDto>` — `409` (sin etapa activa) lo maneja el llamador como estado válido.
- `avanzarEtapa(partidaId, accessToken, fetchImpl?): Promise<AvanceEtapaResponse>`.
- `enviarPista(partidaId, body: EnviarPistaRequest, accessToken, fetchImpl?): Promise<PistaEnviadaResponse>` — `body` = `{ texto, participanteDestinoId?, equipoDestinoId? }`.
- Tipos nuevos: `EtapaActualDto`, `AvanceEtapaResponse`, `EnviarPistaRequest`, `PistaEnviadaResponse` (campos exactos del contrato).
- Apretar `LobbyDto.participantes` de `unknown[]` a `string[]` (son GUIDs; el roster de pistas los usa). Verificar que no rompe los consumidores de 2c-1 (la página solo usa `.length`).

### 2. `frontend/src/features/partidas/useSesionHub.ts` (extender) (+ test)

`SesionHubHandlers` gana:
- `onEtapaActivada?: (p: { partidaId, juegoId, etapaId, orden: number, fechaLimiteUtc: string }) => void`
- `onEtapaCerrada?: (p: { partidaId, juegoId, etapaId }) => void`
- `onEtapaGanada?: (p: { partidaId, juegoId, etapaId }) => void`
- `onUbicacionActualizada?: (p: { partidaId, participanteId, latitud: number, longitud: number, timestampUtc: string }) => void`

Registrar los 4 `connection.on(...)` con el patrón `handlersRef`.

### 3. `frontend/src/features/partidas/GeoMapPanel.tsx` (nuevo) (+ test) + deps

Instala `leaflet`, `react-leaflet`, `@types/leaflet`. Props: `{ ubicaciones: UbicacionParticipante[] }` donde `UbicacionParticipante = { participanteId: string, latitud: number, longitud: number, timestampUtc: string }`.
- `MapContainer` (altura fija, p.ej. 360px) + `TileLayer` OSM (`https://tile.openstreetmap.org/{z}/{x}/{y}.png`, atribución OSM) + un `CircleMarker` por ubicación con `Popup` (GUID corto + "visto hace Xs" derivado de `timestampUtc`).
- Centro: media de lat/long de `ubicaciones`; si vacío, un centro default (`[0,0]` zoom bajo) y leyenda "Esperando ubicaciones…".
- `data-testid="geo-map"`; cada marcador `data-testid="geo-marker"` (o el componente mockeado en test los emite).
- **Test:** `vi.mock("react-leaflet", ...)` (leaflet no corre en jsdom) devolviendo componentes stub que rendericen sus hijos/props; asserta 1 marcador por ubicación y la leyenda de vacío. Extraer el cálculo de centro a una función pura testeable.

### 4. `frontend/src/features/partidas/BdtRuntimePanel.tsx` (nuevo) (+ test)

Espeja `TriviaRuntimePanel`. Props: `{ partidaId, juegoId, accessToken, refetchSignal, onTerminada, onJuegoAvanzado }` (sin config — `areaBusqueda` viene del runtime DTO).
- Efecto deps `[partidaId, juegoId, accessToken, refetchSignal, tick]`: fetch de etapa y ranking en paralelo (patrón `active` flag).
- **Etapa activa** (`getEtapaActual` 200): card `data-testid="etapa-activa"` con `Etapa {orden}`, `areaBusqueda`, countdown a `fechaActivacion + tiempoLimiteSegundos*1000` (reusa el patrón `Countdown` local), botón `data-testid="btn-avanzar-etapa"` "Cerrar y avanzar" (`avanzarEtapa`, disabled-while-posting; `409`→refetch).
- **Sin etapa activa** (`getEtapaActual` 409): `data-testid="sin-etapa-activa"` + botón `data-testid="btn-finalizar-juego"` (`finalizarJuegoActual`; `terminada`→`onTerminada`, `juegoActivadoOrden!=null`→`onJuegoAvanzado`).
- **Ranking BDT** (`getRankingJuego`): tabla `data-testid="ranking-juego"` posición · competidor (GUID corto) · puntos · tiempo (mm:ss desde Ms) · etapas ganadas (`unidadesGanadas`); `404`/vacío → "Sin datos de ranking todavía".
- `data-testid="bdt-runtime"` raíz.

### 5. `frontend/src/features/partidas/PistasPanel.tsx` (nuevo) (+ test)

Props: `{ partidaId, accessToken }`.
- En mount, `getLobby(partidaId)` para el roster + `modalidad`. Select `data-testid="pista-destino"` poblado con participantes (GUIDs, Individual) o equipos (`equipoId`, Equipo); textarea `data-testid="pista-texto"`; botón `data-testid="btn-enviar-pista"` (disabled si sin destino/sin texto o mientras postea).
- `enviarPista(partidaId, { texto, participanteDestinoId | equipoDestinoId })` según modalidad. Éxito → confirmación con `timestampUtc` (`data-testid="pista-enviada"`); errores `400/404/409` → `notice error` inline con mensaje mapeado.
- `data-testid="pistas-panel"` raíz. Roster vacío → leyenda "Sin inscritos para enviar pistas".

### 6. `frontend/src/features/partidas/SesionOperadorPage.tsx` (editar) (+ tests)

- Estado `const [ubicaciones, setUbicaciones] = useState<Map<string, UbicacionParticipante>>(new Map())`; handler `onUbicacionActualizada: (p) => setUbicaciones(prev => new Map(prev).set(p.participanteId, p))`. Limpiar (`new Map()`) cuando cambia `partidaId` (nueva sesión).
- Handlers `onEtapaActivada`/`onEtapaCerrada`/`onEtapaGanada` → `setRefetchSignal(s => s+1)` (no `cargar()` completo; el estado global de sesión no cambia con cierre de etapa).
- Vista `iniciada`, juego actual `BusquedaDelTesoro`: reemplazar el placeholder por `BdtRuntimePanel` + `PistasPanel` + `GeoMapPanel` (pasando `Array.from(ubicaciones.values())`). El juego actual Trivia sigue montando `TriviaRuntimePanel` (2c-2) sin cambios.
- Reuso F1 seq-guard y F5 pills (2c-2) sin tocarlos.

### 7. Manejo de errores

- `409` en `getEtapaActual` → "sin etapa activa" (válido). `409` en `avanzarEtapa` (carrera/no BDT) → refetch, sin banner fatal.
- `400` pista (destino ausente/ambos) — prevenido por la UI (deshabilitar sin destino), pero mapeado por si acaso. `404` pista (destino no inscrito) / `409` pista (sin etapa / equipo en Individual) → inline.
- `404` ranking (proyección no existe aún) → "sin datos todavía".
- El mapa nunca es fatal: sin ubicaciones → leyenda de espera; error de tiles (offline) es visual del propio leaflet (fuera de nuestro control).

## Testing

- **Unit:** apis nuevas (paths exactos, 200/409/404/400, error class); hook (4 eventos nuevos ruteados); `GeoMapPanel` (react-leaflet mockeado: 1 marcador por ubicación, leyenda vacía, función de centro pura); `BdtRuntimePanel` (etapa activa→avance→refetch, 409→finalizar→onTerminada/onJuegoAvanzado, ranking render + 404); `PistasPanel` (roster Individual/Equipo, enviar→confirmación, 404/409 inline, roster vacío); página (juego BDT monta los 3 paneles, `onUbicacionActualizada` actualiza el mapa, Trivia sigue montando su panel).
- **E2E gate (vivo, gateway :5080, tokens operador+participante):** crear partida con **juego BDT (2 etapas)** → publicar → inscribir participante → iniciar → `GET /etapa-actual` 200 (sin `codigoQREsperado`) → `POST /pistas` a participante 200 → `POST /etapa-actual/avance` (cerrar etapa 1) → segunda etapa activa → avance → `sinMasEtapas` → `POST /juego-actual/finalizacion` `terminada:true`. Policy: participante `POST /etapa-actual/avance` y `POST /pistas` → 403.
- **Smokes SignalR:** (a) operador suscrito recibe `EtapaActivada {fechaLimiteUtc}` + `EtapaCerrada`; (b) participante conecta, `SuscribirAPartida`, `EnviarUbicacion(lat,long)` → operador recibe `UbicacionActualizada {participanteId, latitud, longitud}`. Requiere Puntuaciones + RabbitMQ arriba para el ranking.

## Fuera de alcance (explícito)

- Ranking consolidado y vista de partida terminada rica → 2c-4.
- Retiro de páginas legacy `trivia/operar` + `bdt/partidas` → 2c-4.
- Validación/subida de QR (tesoro) — es del participante (móvil), no del operador web.
- Hub ranking SignalR (`/puntuaciones/hubs/ranking`) → sigue diferido (GET por señal alcanza).
- Resolución de nombres de participante/equipo (contrato sin nombres → GUID corto).
- Click-en-marcador para enviar pista (nice-to-have descartado; el selector de roster es el camino).

## Sizing sugerido (lo fija writing-plans)

~7 tasks: T1 extender operacionesApi (haiku) · T2 extender useSesionHub 4 eventos (haiku) · T3 install leaflet + GeoMapPanel (sonnet) · T4 BdtRuntimePanel (sonnet) · T5 PistasPanel (sonnet) · T6 integración página (sonnet) · T7 gate E2E + traceability (controller). Reviewers sonnet, final opus. Gate typecheck: `npx tsc -b`.
