# Bloque 3c — RNF-24: refresh de sesión 270s con control de inactividad (diseño)

**Fecha:** 2026-07-10
**Rama:** `feature/bloque-2`
**Estado:** Aprobado por el usuario
**Cierra el Bloque 3** (3a rankings push · 3b vista web de equipos · 3c este).

## Requisito (RNF-24, SRS §RNF)

Tras autenticarse, el front-end refresca el token de Keycloak cada **270 segundos** (4,5 min), **directamente contra Keycloak, sin pasar por el gateway ni el backend**. El access token vive 300s (5 min) → margen de 30s. El front-end registra de forma continua la actividad del usuario (toques, clicks, desplazamiento, teclado, navegación). Aplica a **web** (admin/operador) y **mobile** (participante). Complemento CLAUDE.md: si el usuario estuvo activo poco antes de la ventana de refresh → refresh silencioso; si no → **modal que pregunta si desea continuar la sesión** antes de refrescar.

## Decisiones del usuario

1. **Modal sin countdown:** persiste hasta respuesta. "Continuar" → intenta el refresh: si la sesión SSO de Keycloak sigue viva continúa; si el refresh falla → logout con mensaje. "Salir" → logout explícito. La expiración real la decide Keycloak (SSO idle, default 30 min).
2. **Ventana de actividad = el ciclo completo:** hubo actividad desde el tick anterior (270s) → activo → refresh silencioso. Un solo timestamp.
3. **Hubs sin churn:** el token pasa a los hubs por **getter/ref**; las conexiones vivas no se reconectan al refrescar (el token solo se usa en el handshake).

## Realm

Sin cambios. El realm no fija `accessTokenLifespan` → default de Keycloak = 300s, que es exactamente la premisa de RNF-24. `ssoSessionIdleTimeout` default (30 min) gobierna la validez del refresh token.

## Núcleo compartido (concepto — un módulo puro POR cliente, sin dependencia cruzada)

Cada cliente tiene su copia del ciclo (TS en web, JS en mobile), misma semántica, testeable con fake timers sin UI ni red:

- Estado: `lastActivity` (timestamp) y `lastTick` (timestamp del último refresh/arranque).
- `marcarActividad()` — actualiza `lastActivity`.
- En cada tick (intervalo 270_000 ms):
  - Si hay un modal pendiente sin responder → no hacer nada (no se apilan ticks).
  - Si `lastActivity >= lastTick` → activo → `onRefrescar()` (silencioso).
  - Si no → `onInactivo()` (la UI muestra el modal).
- `continuar()` (desde el modal) → `onRefrescar()`.
- Tras un refresh exitoso: `lastTick = ahora`, cierra modal si estaba abierto, reprograma el ciclo.
- Refresh fallido (cualquier camino) → `onSesionExpirada()` → logout con mensaje "Tu sesión expiró. Inicia sesión de nuevo.".
- El scheduler se detiene en logout/desmontaje (clear interval + listeners).

## Web

### `frontend/src/auth/keycloak.ts`

`AuthProvider` gana `refresh(): Promise<string>`:
- `keycloak.updateToken(-1)` (forzado: refresca aunque el token siga válido — RNF pide refresh incondicional en el tick) → devuelve `keycloak.token` nuevo.
- Falla (refresh token expirado / red) → rechaza; el caller decide logout.

### Módulos nuevos

- `frontend/src/auth/sessionRefreshCore.ts` — el núcleo puro descrito arriba (+ test con `vi.useFakeTimers`).
- `frontend/src/auth/useSessionRefresh.ts` — hook: monta listeners de actividad en `window` (`pointerdown`, `keydown`, `scroll`, `popstate`; capture + passive) → `marcarActividad`; instancia el core; expone `{ modalVisible, continuar, salir }`. Callbacks: `onToken(token)` y `onLogout(mensaje?)` los inyecta App.
- `frontend/src/auth/SessionExpiryModal.tsx` — modal del design system (overlay + card, patrón de modales existente en la web): título "¿Sigues ahí?", texto "Tu sesión está por expirar.", botones "Continuar sesión" (primario) y "Salir" (secundario).

### `frontend/src/app/App.tsx`

- En estado `ready`, monta `useSessionRefresh`:
  - `onToken` → `setAuthState({ status: "ready", user: { ...user, token } })` → re-render propaga el token fresco a todas las páginas por props.
  - `onLogout` → `authProvider.logout()` (y el mensaje va a la pantalla de login si es expiración).
- Renderiza `SessionExpiryModal` cuando `modalVisible`.

### Hubs web (sin churn)

- `api/sesionHub.ts` y `api/rankingHub.ts`: `crearSesionHub` / `crearRankingHub` aceptan `getToken: () => string` en lugar de string (`accessTokenFactory: getToken`).
- `useSesionHub.ts` / `useRankingHub.ts`: guardan `accessToken` en un ref actualizado en cada render; el efecto pasa `() => tokenRef.current` y saca `accessToken` de las deps (`[partidaId]`). Comportamiento: la conexión viva sigue; cualquier reconexión (onreconnected / remount) usa el token fresco del ref.

## Mobile

### `mobile/src/auth/keycloakMobileAuth.ts`

`refreshSessionAsync(): Promise<AuthSessionState | null>` nueva:
- Lee el refresh token de SecureStore (`umbral.auth.refresh`, ya se guarda hoy).
- POST directo al token endpoint del discovery (`grant_type=refresh_token&client_id=...&refresh_token=...`) — cliente↔Keycloak, sin gateway.
- Éxito → persiste access token nuevo en `umbral.auth.session` (con `buildAuthUser`) y el refresh token rotado en `umbral.auth.refresh`; devuelve el `AuthSessionState` nuevo.
- Falla (sin refresh token / HTTP != 200 / red / token inválido) → devuelve `null` (el caller hace logout). No lanza.

### Módulos nuevos

- `mobile/src/auth/sessionRefreshCore.js` — mismo núcleo puro (+ test `mobile/tests/sessionRefreshCore.test.js` con timers falsos de `node:test`).
- `mobile/src/auth/SessionExpiryModal.tsx` — `Modal` de react-native (transparent + card con tema compartido), mismos textos y botones que web.

### `mobile/src/auth/AuthProvider.tsx`

- Con `session != null`: instancia el core (intervalo 270s).
  - `onRefrescar` → `refreshSessionAsync()`: sesión nueva → `setSession(nueva)` (context propaga token fresco); `null` → logout + estado de aviso "Tu sesión expiró...".
  - `onInactivo` → `modalVisible=true`; el modal se renderiza dentro del provider (envuelve `children`).
- Actividad: el provider envuelve `children` en un `View` raíz con `onStartShouldSetResponderCapture={() => { marcarActividad(); return false; }}` (captura todo toque sin robar gestos) + listener de cambio de estado de navegación si el contenedor de navegación está accesible (si no lo está limpiamente, los toques bastan — navegar implica tocar).
- Logout/unmount → detiene scheduler.

### Hubs mobile (sin churn)

- `features/partidas/sesionHub.js` y `rankingHub.js`: aceptan `getToken: () => string` (`accessTokenFactory: getToken`).
- `PartidaLiveScreen.tsx`: `tokenRef` actualizado en cada render; los 2 efectos de hub pasan `() => tokenRef.current` y sacan `token` de las deps (`[apiBaseUrl, partidaId]`).
- Nota: los efectos de datos (fetch por señal) siguen usando el token por prop/context — al refrescar, el re-render les da el token nuevo; no se reconecta ningún hub.

## Errores

- Refresh fallido en cualquier camino (tick silencioso o "Continuar") → logout con mensaje en español. Nunca se deja al usuario con token muerto y 401s silenciosos.
- Doble tick con modal abierto → ignorado (guard del core).
- Web: fallo de `updateToken` por red transitoria también hace logout (simple; re-login recupera). Sin reintentos — YAGNI.

## Testing y gate E2E

- **Unit core (ambos clientes):** tick con actividad → `onRefrescar`; sin actividad → `onInactivo`; `continuar()` → `onRefrescar`; refresh fallido → `onSesionExpirada`; modal abierto → tick ignorado; actividad posterior al modal no lo cierra sola.
- **Web:** test del hook (listeners marcan actividad, fake timers) + test del modal (botones llaman callbacks) + tests existentes de hubs ajustados al getter. Gates: `npm test` + `npx tsc -b` + `npm run build`.
- **Mobile:** test de `refreshSessionAsync` (mock fetch/SecureStore: éxito persiste y devuelve sesión; falla devuelve null) + core + hubs con getter. Gates: `npm test` + `npm run typecheck`.
- **E2E (controlador):** contra Keycloak real —
  1. PKCE login (script del scratchpad, pidiendo también el refresh token) → access A1 + refresh R1.
  2. `POST token grant_type=refresh_token` con R1 directo a :8080 → 200, access A2 ≠ A1 (flujo cliente↔Keycloak verificado, sin gateway).
  3. A2 funciona contra el gateway (GET autenticado 200).
  4. Esperar a expiración del access A1 (o decodificar `exp` y verificar lifetime 300s) → A1 rechazado (401) mientras A2 sigue vivo.

## Fuera de alcance

- Cambios de realm, backend, gateway o contratos (no hay).
- Reintentos de refresh con backoff; countdown en el modal.
- "Recordar sesión" / offline tokens.
- Detección de actividad más fina que el ciclo de 270s.
