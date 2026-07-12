# Bloque 2c-1 — Consola de sesión del operador (web): publicación, lobby, inicio

**Fecha:** 2026-07-08 · **Rama:** `feature/bloque-2` (2a+2b debajo, HEAD 18d92a9 / 7f35dbe) · **Skill:** subagent-driven-development
**HU (lado operador):** publicación a lobby, inicio manual/automático, estado de sesión (HU-14/15/16/18 lado operador).
**Servicio dueño (backend):** Operaciones de Sesión (ya construido, SP-3a..3f). **Cliente:** web (Operador). **Contra:** gateway `:5080`.

## Contexto

2c (operación en vivo web operador) se sub-partió en 2c-1..2c-4. Este spec cubre **2c-1**: la infraestructura de tiempo real (cliente SignalR) más el primer tramo del flujo de operación — publicar una partida a lobby, ver el lobby, iniciarla (manual o automática) y mostrar el shell de la sesión iniciada (lista de juegos). El runtime per-juego (pregunta/etapa, avance, pistas, mapa geoloc, ranking vivo, consolidado) y el retiro de las páginas legacy quedan para 2c-2/2c-3/2c-4.

El backend está completo y contratado. Este slice es **consumo web puro**: **cero cambios de backend, contratos ni reglas de negocio**.

### Estado web hoy (post-2b)

- Rutas nuevas 2b vía gateway: `partidas`, `partidas/crear`, `partidas/:partidaId` (detalle config). Cliente `api/partidasApi.ts`.
- Dos páginas **legacy** siguen montadas: `trivia/operar` (`TriviaOperationsPage` → `triviaApi`) y `bdt/partidas` (`PublishedBdtGamesPage` → `bdtApi`), que llaman endpoints viejos per-juego de los servicios Trivia/BDT Game muertos. **No** son el `/operaciones-sesion/*` partida-level. Se retiran en **2c-4**, intactas en 2c-1.
- Sin `@microsoft/signalr` ni lib de mapa instaladas (deps web: keycloak-js, react, react-dom, react-router-dom).

### Contratos que se consumen (fijos)

`contracts/http/operaciones-sesion-api.md`:
- `POST /operaciones-sesion/partidas/{partidaId}/publicacion` — policy `GestionarPartidas` → `201 + LobbyDto`. Errores: `401`·`403`·`404 config no existe`·`502 Partidas inaccesible`·`409 ya publicada / no publicable`.
- `GET /operaciones-sesion/partidas/{partidaId}/lobby` — autenticado → `200 + LobbyDto`. `404 sesión no existe`.
- `POST /operaciones-sesion/partidas/{partidaId}/inicio` — policy `GestionarPartidas` → `200 + InicioPartidaResponse`. `409 no en Lobby / modo incompatible`.
- `GET /operaciones-sesion/partidas/{partidaId}/estado` — autenticado → `200 + EstadoSesionDto`. `404 sesión no existe`.

DTOs:
- `LobbyDto { partidaId, sesionPartidaId, estado, modalidad, minimosParticipacion, maximosParticipacion, inscritosActivos, participantes[], equipos[] }`; `equipos: [{ equipoId, convocados, aceptados }]` (solo modalidad Equipo).
- `InicioPartidaResponse { partidaId, estado, juegoActivadoId?, juegoActivadoOrden? }`; `estado ∈ {Iniciada, Cancelada, Lobby}` (Lobby = no-op de inicio automático; el inicio manual no lo devuelve en el flujo normal).
- `EstadoSesionDto { partidaId, sesionPartidaId, estado, modalidad, juegos[]{ juegoId, orden, tipoJuego, estado }, juegoActualOrden? }`.

`modoInicioPartida` y `tiempoInicio` **no** están en `LobbyDto` ni en `EstadoSesionDto` → se toman de `GET /partidas/{id}` (config, ya expuesto por `PartidaDetail` de 2b: `modoInicioPartida`, `tiempoInicio`). Reuso de `getPartida` — sin tocar backend.

SignalR (`contracts/http/operaciones-sesion-api.md` §Realtime): hub `GET /operaciones-sesion/hubs/sesion` (WS vía gateway, token por query `access_token` en el handshake). Cliente→servidor: `SuscribirAPartida(partidaId)` / `DesuscribirDePartida(partidaId)`. Servidor→cliente relevantes a 2c-1: `PartidaEnLobby {partidaId}`, `PartidaIniciada {partidaId}`, `PartidaCancelada {partidaId, motivo}`, `JuegoActivado {partidaId, juegoId, orden, tipoJuego}`, `PartidaFinalizada {partidaId}`.

**Restricción de contrato clave:** el hub de sesión **no emite evento de inscripción**. El operador ve por push la *transición* (inicio automático, cancelación por mínimos, juego activado) sin refrescar, pero el **conteo de `inscritosActivos` que va subiendo no se pushea** — solo se obtiene por `GET /lobby`. De ahí el diseño mixto: SignalR para transiciones + intervalo ligero para inscritos.

## Decisiones de diseño (aprobadas)

1. **SignalR en 2c-1** (no polling): el inicio automático lo dispara un barrido de fondo del backend sin acción del operador, así que necesita push; y evita código de polling desechable. La infra queda lista para 2c-2/2c-3.
2. **Ruta nueva** `/partidas/:partidaId/sesion` como consola de sesión, separada del detalle de config. El detalle (2b) gana la acción de publicar/entrar.
3. **Inscritos por intervalo ~5s** (setInterval nativo, solo mientras `estado=Lobby`) + botón "Actualizar" — única vía posible dado que el contrato no pushea inscripciones. SignalR cubre las transiciones.

## Componentes

### 1. `frontend/src/api/operacionesApi.ts` (+ `operacionesApi.test.ts`)

Cliente HTTP del servicio Operaciones de Sesión, mismo patrón que `partidasApi.ts`:
- `class OperacionesApiError extends Error { statusCode }`.
- `resolveBaseUrl()` desde `VITE_GATEWAY_BASE_URL` (idéntico a partidasApi).
- `publicarPartida(partidaId, accessToken): Promise<LobbyDto>` — `POST .../publicacion`; mapea `409`→"ya publicada / no publicable", `404`→"config no existe", `502`→"servicio de partidas inaccesible".
- `getLobby(partidaId, accessToken): Promise<LobbyDto>` — `GET .../lobby`; `404`→"no publicada".
- `iniciarPartida(partidaId, accessToken): Promise<InicioPartidaResponse>` — `POST .../inicio`; `409`→"no en Lobby / modo incompatible". `200 estado=Cancelada` es resultado válido (no error) — lo maneja la página, no el cliente.
- `getEstadoSesion(partidaId, accessToken): Promise<EstadoSesionDto>` — `GET .../estado`; `404`→"no publicada".
- Tipos `LobbyDto`, `InicioPartidaResponse`, `EstadoSesionDto`, `LobbyParticipante`, `LobbyEquipo` según los DTOs de arriba (enums como string).

### 2. `frontend/src/api/sesionHub.ts` (+ `sesionHub.test.ts`)

Instala `@microsoft/signalr` (nueva dep). Fábrica delgada:
- `crearSesionHub(accessToken): HubConnection` — `new HubConnectionBuilder().withUrl(\`${base}/operaciones-sesion/hubs/sesion\`, { accessTokenFactory: () => accessToken }).withAutomaticReconnect().build()`. `base` = `resolveBaseUrl()` (reusar de operacionesApi o duplicar mínimo).
- Sin lógica de negocio; solo construcción de la conexión. `suscribir`/`desuscribir` son `invoke("SuscribirAPartida"/"DesuscribirDePartida", partidaId)` — pueden vivir en el hook.
- Test: mock de `@microsoft/signalr` (verifica url con prefijo correcto y accessTokenFactory).

### 3. `frontend/src/features/partidas/useSesionHub.ts`

Hook React que encapsula el ciclo de vida de la conexión:
- Entrada: `partidaId`, `accessToken`, mapa de handlers (`onIniciada`, `onCancelada`, `onJuegoActivado`, `onEnLobby`, `onFinalizada`).
- Efecto: crea conexión, `start()`, `invoke("SuscribirAPartida", partidaId)`, registra `connection.on(...)` para cada mensaje; en `onreconnected` re-invoca `SuscribirAPartida`; en cleanup `desuscribir` + `stop()`.
- Aislado para poder mockearse en los tests de la página.

### 4. `frontend/src/features/partidas/SesionOperadorPage.tsx` (+ `SesionOperadorPage.test.tsx`)

Ruta `/partidas/:partidaId/sesion` (bajo `RequireRole need="Operador"`). Estados:
- **Carga:** `GET /estado`. `404` → pantalla "no publicada" con link a `/partidas/:id` para publicar.
- **`estado=Lobby`:** además `GET /lobby` + `getPartida` (para `modoInicio`/`tiempoInicio`).
  - Panel lobby: modalidad, min/max, `inscritosActivos`, lista de participantes (Individual) o equipos con `convocados`/`aceptados` (Equipo), indicador de progreso hacia mínimos. Refetch de `GET /lobby` por `setInterval` ~5s (limpiado al salir de Lobby / desmontar) + botón "Actualizar".
  - Controles de inicio según `modoInicioPartida`:
    - `Manual`: botón "Iniciar ahora" → `POST /inicio` (disabled mientras postea — anti doble-click, lección 2b).
    - `Automatico`: countdown a `tiempoInicio` (cuenta regresiva local), sin botón. El barrido de fondo dispara; la UI reacciona al push.
    - `ManualYAutomatico`: countdown + botón.
  - Resultado de `POST /inicio`: `Iniciada` → vista sesión; `Cancelada` → pantalla "mínimos no alcanzados" (200 válido).
- **`estado=Iniciada` (shell):** lista de juegos de `GET /estado` (`orden`, `tipoJuego`, `estado`) con el `juegoActualOrden` resaltado + nota "el runtime del juego llega en 2c-2/2c-3". Sin controles de avance en 2c-1.
- **`estado=Cancelada`:** pantalla cancelada (con `motivo` si vino por push).
- **`estado=Terminada`:** pantalla neutral "finalizada" (el consolidado es 2c-4).
- SignalR vía `useSesionHub`: `PartidaIniciada` → refetch `GET /estado`; `PartidaCancelada` → pantalla cancelada + `motivo`; `JuegoActivado` → actualiza juego actual; `PartidaEnLobby` → refetch lobby; `PartidaFinalizada` → pantalla finalizada.

### 5. Ediciones

- `frontend/src/features/partidas/PartidaDetailPage.tsx`: acción "Publicar y operar" → `publicarPartida` → `navigate("/partidas/:id/sesion")`; `409 ya publicada` → "Ir a la sesión" (navega igual). Errores `404`/`502` mostrados inline. Guard disabled mientras publica.
- `frontend/src/app/App.tsx`: ruta `partidas/:partidaId/sesion` bajo `RequireRole need="Operador"`.
- Nav / `titleForPath` (shell): título/breadcrumb para la ruta de sesión.

## Manejo de errores

- `401` (sin/mal token) — no debería ocurrir con sesión Keycloak viva; mensaje genérico.
- `403` (sin `GestionarPartidas`) — solo si un no-operador llega a la ruta; el `RequireRole` ya lo bloquea antes.
- `404` publicar → config inexistente; `404` lobby/estado → sesión no publicada (pantalla dedicada con salida a publicar).
- `409` publicar → ya publicada (ofrecer entrar); `409` inicio → no en Lobby / modo incompatible (refetch estado y re-render).
- `502` publicar → Partidas inaccesible; mensaje transitorio.
- Fallos de red (TypeError de fetch) → mensaje genérico acotado, no filtrar detalle (lección 2b, `PartidasApiError`-style narrowing).
- `POST /inicio` con `estado=Cancelada` **no** es error: es el camino de mínimos-no-alcanzados, renderiza la pantalla de cancelación.

## Testing

- **Unit (vitest, fetch mockeado + mock de `@microsoft/signalr`):**
  - `operacionesApi`: publicar (201, 409, 404, 502), lobby (200, 404), inicio (200 Iniciada, 200 Cancelada, 409), estado (200, 404).
  - `sesionHub`: construye la conexión con url de prefijo `operaciones-sesion` y `accessTokenFactory`.
  - `SesionOperadorPage`: render de lobby (Individual y Equipo), inicio manual → Iniciada, inicio manual → Cancelada, countdown en Automatico (sin botón), reconexión/404 (no publicada), shell post-inicio con juego actual.
  - `PartidaDetailPage`: acción publicar → navega; 409 → "Ir a la sesión".
- **E2E gate (vivo, gateway :5080, token de operador real, patrón 2b — authorization-code+PKCE via curl):**
  - `POST /publicacion` 201 → `GET /lobby` 200 → `POST /inicio` sin inscritos → `200 estado=Cancelada` (mínimos no alcanzados, camino limpio single-actor).
  - Smoke SignalR: operador conecta al hub, `SuscribirAPartida`, `POST /inicio`, recibe `PartidaIniciada`/`PartidaCancelada` en su propia conexión.
  - Policy: participante `POST /publicacion` = `403`.

## Fuera de alcance (explícito)

- Runtime per-juego: pregunta actual, avance de pregunta, etapa actual, avance/cierre de etapa (2c-2/2c-3).
- Pistas, mapa geoloc BDT, `UbicacionActualizada`, `PistaEnviada` (2c-3).
- Ranking vivo (`/puntuaciones/*`, hub ranking) y consolidado (2c-2 trivia, 2c-3 bdt, 2c-4 consolidado).
- Avance de juego (`POST /juego-actual/finalizacion`) — pertenece al runtime (2c-2/2c-3).
- Retiro de páginas legacy `trivia/operar` + `bdt/partidas` (2c-4).
- Inscripción/convocatoria del participante (es móvil, no operador web).

## Sizing sugerido (lo fija writing-plans)

~6-7 tasks: operacionesApi (haiku verbatim) · sesionHub + install signalr (sonnet) · useSesionHub hook (sonnet) · SesionOperadorPage lobby+inicio (sonnet) · shell post-inicio + reconexión (sonnet, o fusionado con el anterior) · detalle publicar + ruta + nav (sonnet) · E2E gate + traceability (controller/haiku). Reviewers sonnet por-task, review final opus whole-slice.
