# Panel de Gestión de Equipo (mobile) — diseño

## Problema

`mobile/src/screens/HomeScreen.tsx` expone las 8 acciones de equipo (crear, invitaciones,
invitar, transferir liderazgo, salir, eliminar, historial, rendimiento) como tarjetas planas,
siempre visibles, sin importar si el usuario pertenece a un equipo, si es líder, ni quiénes son
sus compañeros. Todas las pantallas de acción individuales ya existen (spec
`docs/superpowers/specs/2026-07-08-bloque4a-equipos-admin-design.md`, HU-06/HU-48) — falta el
hub que muestre el estado del usuario y exponga solo las acciones que le corresponden.

## Modelo

Tres estados de usuario respecto a su equipo, determinados por `GET /identity/teams/mine`:

1. **Sin equipo** (`404`): no pertenece a ningún equipo activo.
2. **Con equipo, líder**: pertenece y su `usuarioId` (Keycloak `sub`) aparece en
   `participantes` con `esLider: true`.
3. **Con equipo, miembro**: pertenece y no es líder.

Botones visibles por estado (todo navega a pantallas ya existentes, ninguna se crea):

| Estado | Botones |
|---|---|
| Sin equipo | Crear equipo, Invitaciones, Historial de equipos |
| Líder | Invitaciones, Invitar miembro, Transferir liderazgo, Salir del equipo, Eliminar equipo, Historial de equipos, Rendimiento de equipo |
| Miembro (no líder) | Invitaciones, Salir del equipo, Historial de equipos, Rendimiento de equipo |

"Invitaciones" es siempre la misma bandeja de invitaciones **recibidas** (`InvitationsScreen`
existente) en los 3 casos — no hay una vista separada de invitaciones enviadas por el líder.

## Decisiones

**D1 — Extender el contrato `GET /identity/teams/mine`, no crear un endpoint nuevo.**
Hoy devuelve `participantes: [{ usuarioId, esLider }]`, sin nombre — inútil para mostrar una
lista de integrantes legible. Se le agrega `nombre`, resuelto igual que ya hace el endpoint de
administración (`GET /identity/teams`): vía `Usuario.KeycloakId`, `""` si no hay referencia
local. Shape final:
```
200 { equipoId, nombreEquipo, estado, participantes: [{ usuarioId, nombre, esLider }] }
```
Sigue devolviendo `404` si el caller no tiene equipo activo. Ningún otro campo ni código de
estado cambia.

**D2 — Panel nuevo (`TeamPanelScreen`), no exploto los 3 casos en Home.**
`HomeScreen.tsx` reemplaza las 8 tarjetas de "Tu equipo" por **una** tarjeta "Gestión de
equipo" que navega a la pantalla nueva, mismo patrón que la tarjeta "Partidas" ya usa para
navegar a `PartidasPanel`.

**D3 — Refetch al volver a foco (`useFocusEffect`), no solo al montar.**
Las acciones (salir, eliminar, transferir, crear) mutan la membresía del usuario. Al volver de
esas pantallas al panel, se vuelve a pedir `GET /identity/teams/mine` para reflejar el estado
nuevo sin reiniciar la app.

**D4 — Sigue el patrón Container/Screen/Flow/Api ya establecido en `mobile/src/features/teams/`**
(ver `InvitationsScreenContainer.tsx` / `InvitationsScreen.tsx` / `invitationsFlow.js` /
`invitationsApi.js` como referencia). Nuevos archivos: `teamPanelApi.js`, `teamPanelFlow.js`,
`TeamPanelScreen.tsx`, `TeamPanelScreenContainer.tsx`.

## Componentes

### Backend (Identity)
- Handler de `GET /identity/teams/mine`: agrega resolución de `nombre` por participante
  (mismo mecanismo que el listado admin).
- `contracts/http/identity-api.md` (línea 103): documentar el campo `nombre` nuevo.
- Tests: unit del handler (nombre resuelto / `""` sin referencia local), contract test
  actualizado con el shape nuevo.

### Mobile
- `teamPanelApi.js`: `GET /identity/teams/mine` crudo (fetch + manejo de red).
- `teamPanelFlow.js`: mapea la respuesta a los 3 estados (`sinEquipo` en 404, `conEquipo` en
  200 con `soyLider` derivado comparando `session.user.sub` contra `participantes`, `error` en
  fallo de red u otro status).
- `TeamPanelScreen.tsx`: UI. Muestra estado de carga, mensaje de error, o según el estado
  resuelto: nombre del equipo + badge (Líder/Miembro) + lista de integrantes (nombre + tag
  líder) + el set de botones correspondiente. Cada botón navega con
  `navigation.navigate("<Screen>")` a una pantalla ya existente.
- `TeamPanelScreenContainer.tsx`: wiring de `session`/`mobileEnv`, igual patrón que
  `InvitationsScreenContainer.tsx`.
- `HomeScreen.tsx`: sección "Tu equipo" pasa de 8 `NavCard` a 1 `NavCard` ("Gestión de
  equipo") → `navigation.navigate("TeamPanel")`.
- `navigation/types.ts`: agrega `TeamPanel: undefined` a `AppStackParamList`.
- `navigation/RootNavigator.tsx`: registra la pantalla `TeamPanel`.

## Manejo de errores

- `404` en `GET /identity/teams/mine` → estado "sin equipo" (no es un error, es un estado
  válido de UI).
- Fallo de red / otros status → mensaje de error + botón reintentar, mismo patrón que
  `InvitationsScreen.tsx` (`errorMessage` + `load()` reintentable).

## Testing

- Mobile: `node --test tests/*.test.js` — tests de `teamPanelFlow.js` cubriendo los 3 estados
  (sin equipo, líder, miembro) y el cálculo de `soyLider`.
- Backend: unit test del handler (nombre resuelto correctamente), contract test del shape
  nuevo de `/identity/teams/mine`.
- `npm run typecheck` en mobile.

## Fuera de alcance

- No se toca ninguna pantalla de acción existente (crear, invitar, transferir, salir,
  eliminar, historial, rendimiento) — solo se las referencia por navegación.
- No se agrega una vista de invitaciones "enviadas por el líder".
- No se toca web, gateway, ni ningún otro microservicio.
- No se cambian reglas de negocio de equipos (BR-E06/E10/E11 intactas).
