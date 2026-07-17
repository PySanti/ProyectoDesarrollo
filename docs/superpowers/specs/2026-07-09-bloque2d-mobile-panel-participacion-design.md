# Bloque 2d — Mobile: panel de partidas + participación (Individual + Equipo) + SignalR

**Fecha:** 2026-07-09
**Rama:** `feature/bloque-2`
**Servicios:** Operaciones de Sesión (endpoint nuevo de listado) · cliente mobile (Participante)
**Precede:** [2c-4, cierre del Bloque 2c]. **Partición del Bloque 2:** 2a ✓ · 2b ✓ · 2c ✓ · **2d (este)** · 2e mobile gameplay · 2f UI Puntuaciones.

## Contexto

El participante mobile hoy no tiene forma de descubrir ni unirse a partidas: las pantallas
viejas de trivia/bdt apuntan a los servicios muertos (:5015/:5016) y no existe endpoint de
listado participant-safe — `GET /partidas` (servicio Partidas) está detrás de la ruta gateway
`RequireRole("Operador","Administrador")` y su payload incluye la configuración (respuestas
correctas), así que no puede abrirse.

2d construye el flujo de participación completo previo al gameplay: descubrimiento, lobby,
inscripción individual, preinscripción de equipo (líder), convocatorias, reconexión
(`mi-sesion`) y push SignalR en el lobby. El gameplay (responder Trivia, subir QR, pistas,
geoloc) queda para 2e.

## Alcance

### 1. Backend — `GET /operaciones-sesion/partidas-publicadas` (Operaciones de Sesión)

Endpoint nuevo, **autenticado sin policy de permiso** (consistente con las 4 lecturas
compartidas existentes: lobby/estado/pregunta-actual/etapa-actual).

- `200` → lista de sesiones cuyo `Estado == Lobby` (las demás no aparecen; lista vacía válida):

```json
[
  {
    "partidaId": "guid",
    "nombre": "string",
    "modalidad": "Individual | Equipo",
    "modoInicioPartida": "Manual | Automatico | ManualYAutomatico",
    "tiempoInicio": "datetime UTC | null",
    "minimosParticipacion": 1,
    "maximosParticipacion": 10,
    "inscritosActivos": 0
  }
]
```

- Todos los campos salen de `SesionPartida` (el snapshot de publicación ya incluye `Nombre`);
  `inscritosActivos` = mismo conteo que usa `LobbyDto` (participantes activos en `Individual`,
  equipos preinscritos en `Equipo`). **Participant-safe por construcción**: sin juegos,
  preguntas ni códigos QR.
- CQRS: `ListarPartidasPublicadasQuery` + handler + DTO en Application; controller GET en Api
  (patrón idéntico a `GetLobbyQuery`). Unit tests del handler + controller tests (obligatorios).
- Gateway: cubierto por la ruta `/operaciones-sesion/**` existente — sin cambio de matriz.
- Contrato: fila + shape en `contracts/http/operaciones-sesion-api.md` (sección de lecturas
  compartidas).

### 2. Mobile — API modules nuevos

Patrón existente del repo (funciones `(apiBaseUrl, token, ..., fetchImpl = fetch)` que
devuelven result objects `{ok, ...}` / `{ok: false, type, message}`, sin throws; `.js` +
tests `node --test`):

- `features/partidas/partidasPublicadasApi.js` — `getPartidasPublicadas(apiBaseUrl, token)`.
- `features/partidas/inscripcionApi.js` — `inscribirse(…, partidaId)` (POST
  `/operaciones-sesion/partidas/{id}/inscripciones`), `cancelarInscripcion(…)` (DELETE
  `…/inscripciones/mia`), `preinscribirEquipo(…)` (POST `…/inscripciones-equipo`),
  `cancelarPreinscripcionEquipo(…)` (DELETE `…/inscripciones-equipo/mia`). Mapea los 409 del
  contrato a mensajes distinguibles (ya inscrito / participación activa en otra / cupo lleno /
  modalidad) y el 403 no-líder.
- `features/partidas/convocatoriasApi.js` — `getMisConvocatorias(…)` (GET
  `/operaciones-sesion/mis-convocatorias`), `aceptarConvocatoria(…, convocatoriaId)`,
  `rechazarConvocatoria(…, convocatoriaId)` (POST `…/convocatorias/{id}/aceptacion|rechazo`).
- `features/partidas/miSesionApi.js` — `getMiSesion(…)` (GET `/operaciones-sesion/mi-sesion`;
  `204` → `{ok: true, sesion: null}`).
- `features/partidas/sesionHub.js` — `crearSesionHub(gatewayBaseUrl, accessToken)` con
  `@microsoft/signalr` (**dep nueva en mobile**), token por `accessTokenFactory`, URL
  `{gateway}/operaciones-sesion/hubs/sesion` (mismo hub que web). Expone `SuscribirAPartida` y
  handlers `PartidaEnLobby`/`PartidaIniciada`/`PartidaCancelada`.

Base URL: `mobileEnv.gatewayApiBaseUrl` (ya existe desde 2a).

### 3. Mobile — pantallas (feature nuevo `features/partidas/`)

- **PartidasPanelScreen** — listado de publicadas con filtro de modalidad
  (Todas/Individual/Equipo, filtro client-side) y pull-to-refresh. Al montar, además,
  `getMiSesion`: si hay participación activa muestra banner "Tienes una participación activa
  en <nombre|partida>" que navega a su lobby. Item → PartidaLobbyScreen.
- **PartidaLobbyScreen** (recibe `partidaId`) — `GET /lobby` para estado (inscritos / min /
  max, countdown a `tiempoInicio` si modo automático) + hub SignalR suscrito a la partida:
  `PartidaEnLobby` → refetch lobby; `PartidaIniciada` → aviso "La partida comenzó" (el
  gameplay llega en 2e); `PartidaCancelada` → aviso con motivo. Acciones según modalidad:
  - `Individual`: botón Inscribirme / Cancelar inscripción (según si ya estoy, vía
    `mi-sesion`).
  - `Equipo`: botón Preinscribir equipo / Cancelar preinscripción (el backend valida líder;
    403 no-líder mapeado a mensaje claro). La preinscripción genera convocatorias a los
    miembros (backend).
- **ConvocatoriasScreen** — inbox de `mis-convocatorias` pendientes: partida + equipo +
  acciones Aceptar/Rechazar (los 409 de "no en lobby"/"participación activa" mapeados).
- **HomeScreen**: entradas nuevas **Partidas** y **Convocatorias** reemplazan los accesos
  viejos de trivia/bdt. Navegación: rutas nuevas en `RootNavigator` + `AppStackParamList`.

Contenedores/pantallas en `.tsx`, lógica de modelo/flow en `.js` testeable con `node --test`
(patrón del feature `teams`).

### 4. Retiro legacy mobile

- Borrar `features/trivia/` y `features/bdt/` completos (pantallas, apis, flows, modelos,
  tests) y `api/triviaApi.ts` — todos consumen los servicios muertos :5015/:5016.
- `RootNavigator.tsx` / `navigation/types.ts`: quitar rutas e imports trivia/bdt; añadir las
  rutas nuevas de partidas/convocatorias.
- `config/env.ts` + `mobile/.env*`: quitar `EXPO_PUBLIC_BDT_API_BASE_URL`,
  `EXPO_PUBLIC_TRIVIA_API_BASE_URL` y la var muerta `EXPO_PUBLIC_TEAM_API_BASE_URL`
  (auditoría Bloque 2 ya la marcó para borrar).
- Grep de limpieza: sin referencias colgantes.
- El gameplay mobile renace en 2e sobre `mi-sesion` + hub (no se conserva código viejo "por
  si acaso").

### 5. Gate

- Backend: `dotnet test` solución Operaciones de Sesión (unit + controller + integration).
- Mobile: `npm test` (`node --test tests/*.test.js`) + `npm run typecheck` (`tsc --noEmit` —
  en mobile sí es el gate real; el gate `tsc -b` aplica solo a frontend web).
- E2E vivo vía gateway :5080: `GET /partidas-publicadas` con token participante (partida en
  Lobby aparece; Iniciada/Terminada no) → inscripción individual 201 → `mi-sesion` 200 →
  cancelar 204; flujo Equipo: líder preinscribe 201 → convocatoria aparece en
  `mis-convocatorias` del miembro → aceptar 200; smoke SignalR: participante suscrito recibe
  `PartidaIniciada`.
- Traceability: fila 2d.

## No-objetivos

- Gameplay (responder Trivia, subir QR, pistas, recibir geoloc) → **2e**.
- UI de rankings/historial → **2f**.
- RNF-24 (refresh 270s + modal de continuidad) → mini-slice aparte (definido así en 2a).
- Paginación del listado (volúmenes académicos; YAGNI).
- Resolución de nombres de equipo/participante más allá de lo que den los DTOs del contrato.

## Sizing preliminar (writing-plans lo detalla)

~8 tareas: T1 backend query+handler+controller+tests (sonnet) · T2 contrato + E2E backend
smoke (controller o haiku) · T3 mobile api modules + tests (haiku, verbatim) · T4 sesionHub
mobile + dep signalr (sonnet) · T5 PartidasPanelScreen (sonnet) · T6 PartidaLobbyScreen +
acciones + hub (sonnet) · T7 ConvocatoriasScreen + Home/navegación (sonnet) · T8 retiro
legacy + gate E2E (sonnet/controller). Reviewers sonnet, review final opus.
