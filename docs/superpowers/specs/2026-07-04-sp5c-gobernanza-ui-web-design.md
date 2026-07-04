# SP-5c — UI web de gobernanza (panel de permisos + cambio de rol)

**Fecha:** 2026-07-04 · **Slice:** SP-5c (cierra la gobernanza iniciada en SP-5a/SP-5b)
**Alcance:** solo `frontend/` (web React). Cero cambios de backend, contratos o mobile.
**Actor:** exclusivamente `Administrador` (regla de cliente del SRS: admin/operador → web).

## 1. Contexto

SP-5b dejó operativos los tres endpoints de gobernanza en Identity, detrás del gateway
(ruta `identity-governance` y `identity-users`, ambas policy `Administrador`, Order 1):

| Método | Path | Respuesta 200 |
|---|---|---|
| GET | `/identity/governance/roles` | `{ "roles": [ { "rol", "permisos": [], "privilegiosGobernanza" } ] }` |
| PUT | `/identity/governance/roles/{rol}/permisos` | `{ "rol", "permisos": [], "privilegiosGobernanza" }` |
| PATCH | `/identity/users/{userId}/role` | `{ "usuarioId", "rol" }` |

Errores relevantes: 400 (rol/permiso inválido), 401/403 (auth), 404 (usuario), 409
(`RolDeAdministradorInmutableException` / `UsuarioConEquipoActivoException`), 502
(Keycloak caído; el mismo request re-ejecutado repara — diff idempotente).
Contrato canónico: `contracts/http/identity-api.md` §Governance.

SP-5c construye la UI web que consume esos tres endpoints. Decisiones del usuario:
F1 ambas superficies (panel + cambio de rol); F2 checkboxes con Guardar por rol;
F3 modal desde la fila de Gestión de usuarios; F4 promoción a admin con aviso +
confirmación extra.

## 2. API client — `frontend/src/api/identityApi.ts`

Extender el archivo existente siguiendo sus patrones exactos (`fetchImpl: typeof fetch = fetch`
inyectable, `buildAuthHeaders`, `parseJsonBody`, `throwIfNotOk`, `IdentityApiError` con
`statusCode`). Tipos y funciones nuevas:

```typescript
export type PermisoFuncional = "GestionarPartidas" | "GestionarEquipos" | "ParticiparEnPartidas";

export interface RolePermissions {
  rol: "Administrador" | "Operador" | "Participante";
  permisos: PermisoFuncional[];
  privilegiosGobernanza: boolean;
}

export interface GovernanceRolesResponse {
  roles: RolePermissions[];
}

export interface ChangeUserRoleResponse {
  usuarioId: string;
  rol: "Administrador" | "Operador" | "Participante";
}

export async function getGovernanceRoles(accessToken, fetchImpl?): Promise<GovernanceRolesResponse>
// GET  {base}/identity/governance/roles

export async function updateRolePermissions(rol, permisos: PermisoFuncional[], accessToken, fetchImpl?): Promise<RolePermissions>
// PUT  {base}/identity/governance/roles/{rol}/permisos   body: { permisos }

export async function changeUserRole(userId, rol, accessToken, fetchImpl?): Promise<ChangeUserRoleResponse>
// PATCH {base}/identity/users/{userId}/role              body: { rol }
```

Sin capa de mapeo extra: los shapes del backend ya son camelCase y se usan tal cual.

## 3. Página Gobernanza — `frontend/src/features/identity/GovernancePage.tsx`

**Ruta y nav.** Ruta `/identidad/gobernanza` en `App.tsx` bajo `RequireRole need="Administrador"`
(mismo patrón que `identidad/usuarios`). Item nuevo en `navConfig.tsx`, área `identidad`:
`{ label: "Gobernanza", path: "/identidad/gobernanza", icon: Lock }` (icono `Lock` ya existe
en `shell/icons.tsx`). `titleForPath` lo resuelve solo (recorre `NAV_AREAS`).

**Comportamiento.**
- Al montar: `getGovernanceRoles`. Estados: cargando (`.muted`), error de carga
  (`.notice.error` + botón Reintentar), éxito → grid de cards.
- Una card (`.card.stack`) por rol, en el orden que devuelve el backend
  (Administrador, Operador, Participante). Cada card:
  - Título = rol.
  - Card Administrador: badge estático "Privilegios de gobernanza — protegidos"
    (render de `privilegiosGobernanza === true`; no es checkbox, no editable, no viaja
    en el PUT).
  - 3 checkboxes, uno por `PermisoFuncional` (los tres roles son gestionables — E3;
    etiquetas humanas: "Gestionar partidas", "Gestionar equipos", "Participar en partidas").
  - Botón **Guardar** por card: habilitado solo si el set marcado difiere del último
    estado confirmado por el servidor (comparación como sets, orden irrelevante).
  - Al guardar: `updateRolePermissions(rol, marcados)` con el **set completo** marcado.
    Éxito → el response reemplaza el estado confirmado de esa card + mensaje de éxito
    transitorio (`role="status"`). Error → `.notice.error` dentro de la card
    (502 → "Keycloak no disponible. Reintenta: volver a guardar repara el estado.";
    resto → mensaje del backend). Los checkboxes conservan lo marcado para re-intentar.
  - Estado `saving` por card: botón deshabilitado con texto "Guardando…". Cada card es
    independiente (un fallo en Operador no toca Participante).

**data-testid:** `gov-card-{rol}`, `gov-check-{rol}-{permiso}`, `gov-save-{rol}`,
`gov-badge-admin`, `gov-error-{rol}`, `gov-load-error`.

## 4. Cambio de rol — `frontend/src/features/identity/UserManagementPage.tsx`

**Disparador.** Columna de acciones de la tabla gana un botón "Cambiar rol"
(`.secondary-button`) por fila:
- `disabled` + `title="El rol de un Administrador es inmutable."` cuando
  `user.role === "Administrador"`.
- Click → abre el modal para ese usuario.

**Modal** (clases del design system: `.modal-backdrop`, `.modal-card`, `.modal-header`;
cierre por botón X y por Cancelar; sin cierre implícito que pierda estado a mitad de request):
- Texto: usuario (nombre + email) y rol actual.
- `<select>` de rol destino con **solo** los roles distintos del actual (el no-op es
  imposible desde la UI; el backend igual lo tolera como 200).
- Si el destino es `Administrador`: aparece `.notice` de advertencia
  ("Promover a Administrador es irreversible: el rol de un administrador no puede
  volver a cambiarse.") y el flujo exige **segundo click**: el botón primario pasa de
  "Cambiar rol" a "Entiendo, promover" (primer click arma la confirmación, segundo
  ejecuta). Para destinos no-admin basta un click.
- Ejecución: `changeUserRole(userId, rolDestino)`. Estados:
  - Éxito → cierra modal, actualiza el rol en la fila local (sin refetch completo) y
    muestra notice de éxito transitorio en la página.
  - 409 → mensaje del backend inline en el modal (cubre equipo activo; también el
    admin-inmutable si llegara por carrera).
  - 502 → "Keycloak no disponible. Inténtalo de nuevo." inline (reintento = re-click;
    el backend converge por idempotencia del remove 404-tolerante).
  - 400/404 → mensaje del backend inline.
  - `saving`: botón primario deshabilitado ("Cambiando…").

**data-testid:** `role-change-open-{userId}`, `role-change-modal`, `role-change-select`,
`role-change-confirm`, `role-change-warning`, `role-change-error`.

**Restricción redesign:** no se altera ningún `label`/`id`/`data-testid`/rol ARIA
existente de la página; solo se agregan los nuevos.

## 5. Errores y semántica compartida

- `IdentityApiError.statusCode` decide el mensaje: 502 → texto fijo de Keycloak;
  otros → `message` del backend (el middleware de Identity ya emite mensajes en español).
- Nada de retries automáticos: el reintento es humano (re-click), apoyado en la
  idempotencia documentada del backend (spec SP-5b §10).
- 401/403 no reciben tratamiento especial nuevo: las rutas ya están tras
  `RequireRole Administrador` y el shell existente maneja sesión.

## 6. Tests (vitest, patrones del repo)

- **`frontend/src/api/identityApi.test.ts` (archivo nuevo** — hoy solo existe
  `bdtApi.test.ts` como referencia de estilo): para cada función nueva: URL exacta,
  método, headers Authorization, body serializado, response feliz tipado, y error no-ok
  → `IdentityApiError` con `statusCode` y `message` del body.
- **`GovernancePage.test.tsx`:** render de matriz desde GET fake (3 cards, badge admin
  presente solo en Administrador); toggle de un checkbox habilita Guardar y el PUT
  recibe el set completo marcado; éxito actualiza estado confirmado (Guardar se
  re-deshabilita); 502 muestra el mensaje de Keycloak en la card correcta; error de
  carga muestra `gov-load-error` con Reintentar.
- **`UserManagementPage.test.tsx` (ampliar):** botón deshabilitado para fila admin con
  title; modal abre con roles destino correctos (excluye actual); destino no-admin
  ejecuta en un click y actualiza la fila; destino Administrador exige el segundo click
  ("Entiendo, promover") y no llama a la API en el primero; 409 del backend queda
  inline en el modal sin cerrarlo.
- Los tests existentes de la página deben seguir en verde sin ediciones (solo se amplía).

## 7. Fuera de alcance

Mobile; backend/contratos (ya cerrados en SP-5b); vista de auditoría de eventos de
gobernanza (llegará con Puntuaciones/audit); tratamiento de refresh de token; i18n.

## 8. Criterio de aceptación del slice

Un Administrador puede, desde la web: ver la matriz de permisos por rol con los
privilegios de gobernanza del admin visibles y protegidos; editar y guardar el set de
permisos de cualquiera de los tres roles con feedback por card y reparación por
re-intento ante 502; y cambiar el rol de un usuario no-admin desde Gestión de usuarios
con bloqueo visual para admins, confirmación reforzada para promociones a admin y
mensajes 409/502 accionables. Suites frontend en verde (vitest) sin regresiones.
