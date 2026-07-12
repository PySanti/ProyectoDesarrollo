# Bloque 3b — Vista web de equipos (diseño)

**Fecha:** 2026-07-10
**Rama:** `feature/bloque-2`
**Estado:** Aprobado por el usuario

## Objetivo

Dar a Administrador y Operador una vista web de solo lectura de todos los equipos, con enlace directo al rendimiento de equipo (2f). Cierra el minor de 2f "entrada por equipoId manual hasta que exista la vista web de equipos".

Alcance: un endpoint de listado nuevo en Identity + una ruta nueva en el gateway + una página web nueva + prefill en RendimientoEquipoPage. Sin escritura, sin eventos, sin mobile.

## Decisiones del usuario

1. **Alcance del listado:** todos los equipos (Activo/Desactivado/Eliminado) con badge de estado — los eliminados conservan historial de partidas, su enlace a rendimiento sigue siendo útil.
2. **Navegación:** área nueva "Equipos" con `role: ["Operador", "Administrador"]`, item único `/equipos` (mismo patrón que el área Puntuaciones).
3. **Payload:** miembros enriquecidos con nombre y líder en el mismo GET — sin endpoint de detalle.

## Backend — Identity

### Contrato

`GET /identity/teams` (vía gateway `http://localhost:5080/identity/teams`)

Autorización: rol base `Administrador` u `Operador`. `200 OK`:

```json
[
  {
    "equipoId": "guid",
    "nombreEquipo": "string",
    "estado": "Activo | Desactivado | Eliminado",
    "participantes": [
      { "usuarioId": "guid", "nombre": "string", "esLider": true }
    ]
  }
]
```

Lista vacía → `200` con `[]`. Orden: por `nombreEquipo` ascendente (estable para la UI).

### Piezas

- **`IEquipoRepository.GetAllAsync(CancellationToken)`** → `IReadOnlyList<Equipo>`, todos los estados, con `Participantes` incluidos. Implementación EF en el repositorio existente (`Include` + `AsNoTracking`). Sin paginación — escala académica.
- **`ListarEquiposQuery`** (sin parámetros) + **`ListarEquiposQueryHandler`**: obtiene todos los equipos y todos los usuarios (`IUsuarioRepository.GetAllAsync`, ya existe), resuelve nombre por `UsuarioId` en memoria (diccionario). Usuario no encontrado → nombre `""` (defensivo, no debería ocurrir).
- **DTO `EquipoAdminItemResponse(Guid EquipoId, string NombreEquipo, string Estado, IReadOnlyList<MiembroEquipoAdminResponse> Participantes)`** con `MiembroEquipoAdminResponse(Guid UsuarioId, string Nombre, bool EsLider)`. En `Application/DTOs/`.
- **`TeamsAdminController`** nuevo en `Api/Controllers/`: `[Route("identity/teams")]`, un solo `[HttpGet]`, autorización por rol Operador/Administrador (policy nueva en `Program.cs` siguiendo el patrón de `AdminOnly`: `RequireRole("Operador", "Administrador")`). Despacha `ListarEquiposQuery` por MediatR y devuelve `Ok(...)`.
  - **No** puede vivir en `TeamsController`: su `[Authorize(Policy = "GestionarEquipos")]` de clase es aditivo y admin/operador no tienen ese permiso funcional.

### Tests backend

- Unit test del handler (equipos con miembros → nombres resueltos, líder marcado, estados variados, lista vacía).
- Unit test del controller (obligatorio por directiva): despacha la query y devuelve 200 con el payload.

## Gateway

Ruta nueva en `appsettings.json`, **antes** de `identity-teams` en precedencia:

```json
"identity-teams-listing": {
  "ClusterId": "identity",
  "Order": 0,
  "Match": { "Path": "/identity/teams", "Methods": [ "GET" ] },
  "AuthorizationPolicy": "OperadorOAdministrador"
}
```

- Path exacto sin catch-all: `GET /identity/teams/mine` sigue cayendo en `identity-teams` (Participante) — sin cambio para mobile.
- `POST /identity/teams` no matchea `Methods: ["GET"]` → cae en `identity-teams` (Participante) — crear equipo intacto.
- La policy `OperadorOAdministrador` ya existe en el gateway.
- Test: extender la matriz de rutas existente del gateway con el caso nuevo (GET listing → OperadorOAdministrador; verificar que mine/POST siguen en Participante).

## Web

### `identityApi.ts`

```ts
export interface EquipoMiembro { usuarioId: string; nombre: string; esLider: boolean; }
export interface EquipoAdminItem {
  equipoId: string; nombreEquipo: string; estado: string; participantes: EquipoMiembro[];
}
export async function getEquipos(accessToken: string): Promise<EquipoAdminItem[]>
```

GET `{VITE_GATEWAY_BASE_URL}/identity/teams`, mismo manejo de errores que el resto del módulo (`IdentityApiError`).

### `features/identity/EquiposPage.tsx` (+ test)

Solo lectura. Estados: cargando / error con reintento / vacío ("No hay equipos registrados.") / tabla. Columnas: Nombre, Estado (badge por valor), Miembros (nombres separados por coma, líder marcado — p. ej. "Ana (líder), Luis"), Acción: botón/enlace "Ver rendimiento" → `navigate("/puntuaciones/equipos?equipoId=" + equipoId)`. Reutiliza primitivas del design system existente (tablas/badges como en UserManagementPage / páginas de puntuaciones).

### `RendimientoEquipoPage.tsx` (+ test)

Lee `?equipoId=` con `useSearchParams` al montar: si viene, prefill del campo y consulta automática. Sin param → comportamiento actual intacto. Actualizar el comentario "hasta que exista la vista web de equipos".

### Shell

- `navConfig.tsx`: área nueva `{ id: "equipos", label: "Equipos", role: ["Operador", "Administrador"], items: [{ label: "Equipos", path: "/equipos" }] }` (+ test).
- `App.tsx`: ruta `/equipos` con `RequireRole need={["Operador", "Administrador"]}` → `<EquiposPage accessToken={token} />` (+ test de App si el patrón existente lo cubre).

## Gate E2E (stack ya corriendo)

1. Token participante → crear equipo (`POST /identity/teams` vía gateway) si no existe uno.
2. Token operador → `GET /identity/teams` vía gateway → 200, equipo con miembros y nombres reales.
3. Token participante → `GET /identity/teams` → 403 (gateway).
4. Token admin → `GET /identity/teams` → 200.
5. Web: `npm test` + `npx tsc -b` + `npm run build`; Identity `dotnet test`; gateway `dotnet test`.
6. Verificación manual/headless del deep-link: `/puntuaciones/equipos?equipoId={id}` consulta automáticamente.

## Fuera de alcance

- Escritura/gestión de equipos desde web (crear, invitar, transferir — siguen siendo mobile/participante).
- Endpoint de detalle, paginación, búsqueda/filtros.
- Eventos, mobile, cambios de contrato existentes.
- Historial de nombres de equipo (existe en dominio, no se expone aquí).
