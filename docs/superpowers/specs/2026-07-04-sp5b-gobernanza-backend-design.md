# SP-5b — Gobernanza backend: permisos por rol + cambio de rol + broker RabbitMQ Identity

- **Fecha:** 2026-07-04
- **Slice:** SP-5b (segundo de tres: 5a enforcement ✅, **5b gobernanza backend**, 5c UI web)
- **Rama:** `feature/code-migration-SP-5` (sobre SP-5a completo, HEAD `68fe26e`)
- **Reglas que materializa:** BR-R02 (permisos por rol desde panel), BR-R04 (cambio de rol
  nunca-admin, propagado a Keycloak), ADR-0013 (composites como mecanismo de propagación).

## 1. Contexto y estado actual

- **SP-5a dejó:** permisos = realm roles técnicos composite (`GestionarPartidas`,
  `GestionarEquipos`, `ParticiparEnPartidas`) en Keycloak; defaults BR-R03 en el realm import;
  enforcement por policies en los 4 componentes; ADR-0013 manda que la gobernanza persista en
  Identity DB y propague vía Keycloak Admin API (add/remove composite).
- **Identity hoy:** `Usuario.Rol` local (enum `RolUsuario`); `KeycloakIdentityAdapter` con
  `GetAdminAccessTokenAsync` + `AssignRealmRoleAsync`/`GetRealmRoleAsync` privados (sin
  remove-role ni gestión de composites); eventos de equipos = records + `IEquipoEventsPublisher`
  con **NoOp** (sin RabbitMQ); email por SMTP directo; `UpdateUserGeneralDataCommand` no toca rol;
  ningún endpoint de gobernanza.
- **Operaciones (SP-3i)** es el patrón de broker a espejar: exchange topic durable, mapa
  explícito de routing keys, envelope camelCase, publisher best-effort estricto sobre seam
  `IRabbitMqPublishChannel`, registro condicional, integration test opt-in.

## 2. Decisiones tomadas (brainstorming 2026-07-04)

| # | Decisión | Elección |
|---|---|---|
| E1 | Eventos de gobernanza | **RabbitMQ REAL en Identity ahora** (patrón SP-3i); rewire de los eventos de equipos existentes al broker |
| E2 | Fallo Keycloak en escrituras | **Keycloak primero, DB después**: si Keycloak falla → 502 y nada persiste (patrón CreateUser) |
| E3 | Permisos sobre rol Administrador | **Los 3 roles gestionables** para los 3 permisos funcionales; los privilegios de gobernanza NO son permiso asignable (van con el rol base, no aparecen como removibles) |
| E4 | Cambio de rol con equipo activo | **Bloquear con 409** (miembro o líder) hasta salir/transferir — protege invariantes de equipo |
| E5 | Forma del API de permisos | **PUT set completo por rol** (estado deseado, diff server-side, idempotente) |

## 3. Broker RabbitMQ Identity (backbone, patrón SP-3i)

- **Exchange:** topic durable `umbral.identity`. **Routing keys:** `identity.<kebab>.v1` con
  mapa explícito (espejo de `SesionEventRouting`; evento sin entrada en el mapa = no se publica
  + log). **Envelope:** camelCase `{eventId, eventType, version, occurredAt, payload}` idéntico
  al de Operaciones (§Transport de operaciones-sesion-events.md); consumidores dedupean por
  `eventId`.
- **Publisher:** `RabbitMqIdentityEventsPublisher` best-effort estricto (jamás lanza; solo
  re-lanza `OperationCanceledException` si aplica el patrón Composite) sobre seam
  `IRabbitMqPublishChannel` propio del servicio (copia por-servicio = doctrina; Operaciones ya
  lo tiene). Canal real lazy con lock. Paquete RabbitMQ.Client 6.8.1.
- **Registro condicional:** `RabbitMq__Enabled` + `RabbitMq__Host` → Composite
  `[NoOp, RabbitMq]`; sin config → NoOp solo (tests y dev sin broker intactos).
- **Publisher unificado:** el seam actual `IEquipoEventsPublisher` (NoOp) se generaliza: los
  eventos de equipos existentes (`EquipoCreado`, `InvitacionEquipoCreada`,
  `InvitacionEquipoAceptada`, `InvitacionEquipoRechazada`) y los nuevos de gobernanza fluyen
  por la misma cañería al broker. La forma exacta (extender la interfaz vs interfaz nueva
  `IIdentityEventsPublisher` que absorbe la de equipos) se fija en el plan siguiendo el menor
  churn; el requisito es: un solo Composite, todos los eventos con routing key en el mapa.
- **Eventos nuevos (records en Application, mismo estilo que los de equipos):**
  - `RolUsuarioModificado { UsuarioId, RolAnterior, RolNuevo, OccurredOnUtc }`
  - `PermisosRolActualizados { Rol, Permisos (lista final), OccurredOnUtc }`
- **Diferidos (nota en contrato, NO en 5b):** `UsuarioCreado`, `CredencialTemporalEmitida`
  (slice de audit/notificaciones; hoy el email es SMTP directo síncrono).
- **Contrato:** `contracts/events/identity-events.md` gana §Transport (exchange, routing keys,
  envelope, dedupe) + registro de los 6 eventos con samples 1:1 contra los records.
- **Verificación real:** integration test opt-in `RABBITMQ_TEST_HOST` (skip suave sin la var)
  con round-trip real contra broker vivo (espejo del B6 de SP-3i).

## 4. Dominio y persistencia

- **Enum `PermisoFuncional`** `{ GestionarPartidas, GestionarEquipos, ParticiparEnPartidas }`
  en Domain/Enums.
- **Tabla `permisos_rol`** en `umbral_identity`: columnas `rol` + `permiso` (PK compuesta),
  strings de enum. Representa el estado vigente de asignaciones (fuente del panel; Keycloak =
  espejo runtime que viaja en el token).
- **Seed idempotente BR-R03** al arranque, solo si la tabla está vacía: Operador→GestionarPartidas;
  Participante→GestionarEquipos+ParticiparEnPartidas; Administrador→ninguno.
- **`IPermisosRolRepository`** en Domain/Abstractions/Persistence (leer matriz completa, leer
  por rol, reemplazar set de un rol); impl en Infrastructure/Persistence.
- **`Usuario.CambiarRol(RolUsuario nuevo)`**: guard de dominio — si `Rol == Administrador`
  lanza `RolDeAdministradorInmutableException` (BR-R04; cubre self-change porque el actor
  admin también es un admin target). Mismo rol → no-op del dominio.
- **Adapter Keycloak** (`IKeycloakIdentityPort` + `KeycloakIdentityAdapter`) gana:
  - `AddCompositeToRoleAsync(roleName, compositeRoleName)` — POST
    `/admin/realms/{realm}/roles/{roleName}/composites`
  - `RemoveCompositeFromRoleAsync(roleName, compositeRoleName)` — DELETE ídem
  - `ChangeUserRealmRoleAsync(keycloakId, rolAnterior, rolNuevo)` — remove del realm role viejo
    + assign del nuevo (reusa `GetRealmRoleAsync`/`AssignRealmRoleAsync`; agrega el remove).
  Errores → `KeycloakIntegrationException` (patrón existente) → 502.

## 5. Application y API (todo AdminOnly; coarse en gateway, fino en servicio)

### 5.1 `GET /identity/governance/roles`

`GetPermisosRolesQuery` → matriz completa desde DB:

```json
{
  "roles": [
    { "rol": "Administrador", "permisos": [], "privilegiosGobernanza": true },
    { "rol": "Operador", "permisos": ["GestionarPartidas"], "privilegiosGobernanza": false },
    { "rol": "Participante", "permisos": ["GestionarEquipos", "ParticiparEnPartidas"], "privilegiosGobernanza": false }
  ]
}
```

`privilegiosGobernanza` es informativo (E3): solo Administrador lo tiene, no es asignable ni
removible y no existe como permiso.

### 5.2 `PUT /identity/governance/roles/{rol}/permisos`

Body `{ "permisos": ["GestionarPartidas", ...] }` = **estado deseado** (E5).
`ActualizarPermisosRolCommand`:
1. Validar: `rol` ∈ los 3 roles base (400 si no); cada permiso ∈ `PermisoFuncional` (400);
   duplicados normalizados.
2. Diff vs DB → sets `agregar` / `quitar`.
3. **Keycloak primero (E2):** por cada `agregar` → `AddCompositeToRoleAsync(rol, permiso)`;
   por cada `quitar` → `RemoveCompositeFromRoleAsync(rol, permiso)`. Cualquier fallo → 502 y
   NADA persiste en DB (los composites ya aplicados en Keycloak antes del fallo quedan; el
   PUT es re-ejecutable — idempotente por diff — y la reconciliación es reintentar el PUT.
   Riesgo aceptado y documentado: ventana de drift solo tras un 502, reparable con el mismo
   request).
4. Persistir set final en `permisos_rol` (replace del rol).
5. Evento `PermisosRolActualizados` (set final).
6. 200 con `{ rol, permisos }` final. Diff vacío → 200 sin llamadas a Keycloak ni evento.

### 5.3 `PATCH /identity/users/{userId}/role`

Body `{ "rol": "Operador" }`. `CambiarRolUsuarioCommand`:
1. Usuario no existe → 404.
2. `rol` inválido (∉ 3 roles) → 400 (validator).
3. Target es Administrador → 409 (`RolDeAdministradorInmutableException`, BR-R04).
4. Mismo rol → 200 no-op (sin Keycloak, sin evento).
5. Usuario con **equipo activo** (miembro o líder; consulta al repo de equipos existente) →
   409 `UsuarioConEquipoActivoException` (E4). Aplica al cambio DESDE Participante; para
   Operador→(Admin|Participante) la consulta da vacío y no bloquea.
6. **Keycloak primero:** `ChangeUserRealmRoleAsync` (remove rol viejo + assign nuevo);
   fallo → 502, nada persiste.
7. `Usuario.CambiarRol` + persistir.
8. Evento `RolUsuarioModificado`.
9. 200 `{ usuarioId, rol }`.

Promoción a Administrador permitida (BR-R04). La promoción es de ida: ese usuario queda
inmutable como todo admin.

### 5.4 Controllers y gateway

- **`GovernanceController`** nuevo: `[Route("identity/governance")]`,
  `[Authorize(Policy = "AdminOnly")]`, ControllerBase + MediatR, sin lógica (regla graduada);
  hospeda 5.1 y 5.2.
- El PATCH 5.3 vive en **`UsersController`** (recurso users; AdminOnly de clase ya lo cubre).
- **Gateway:** nueva ruta YARP `identity-governance` — `Match.Path` `/identity/governance/{**catch-all}`,
  `Order` 1, `AuthorizationPolicy` `Administrador` (coherente con `identity-users`) + test de
  matriz (401 sin token / 403 Participante / pasa Administrador).
- Validators FluentValidation según patrón existente; **controller unit tests** obligatorios.

## 6. Errores

| Excepción | HTTP |
|---|---|
| `RolDeAdministradorInmutableException` | 409 |
| `UsuarioConEquipoActivoException` | 409 |
| Rol/permiso inválido (validator) | 400 |
| `KeycloakIntegrationException` (existente) | 502 |
| Usuario no encontrado (patrón existente) | 404 |

Mapeos nuevos se agregan al `ExceptionHandlingMiddleware` existente de Identity.

## 7. Testing

- **Domain:** guard `CambiarRol` (admin inmutable, no-op mismo rol).
- **Handlers (unit, fakes):** diff correcto del PUT (agregar+quitar mixto, vacío, reemplazo
  total); orden **Keycloak-antes-de-DB verificado por los fakes** (si el fake de Keycloak
  lanza, el fake de repo no recibió escrituras); evento emitido con set/datos finales; 409s y
  404 del PATCH; no-op sin efectos.
- **Contract:** GET matriz (shape 5.1); PUT 200 con set final + 400s; PATCH 404/409 admin/409
  equipo activo/400/200; **403 con token de rol no-admin** (p.ej. Participante con
  GestionarEquipos — pinnea que gobernanza exige rol, no permiso); 401 sin token.
- **Integration:** seed BR-R03 idempotente; persistencia replace de `permisos_rol`.
- **Broker:** unit — mapa routing keys 1:1 contra los 6 eventos, envelope camelCase, best-effort
  (canal que lanza no rompe el flujo), registro condicional; integration opt-in
  `RABBITMQ_TEST_HOST` round-trip real.
- **Gateway:** ruta `identity-governance` (401/403/pasa).
- Suites base (SP-5a): gateway 14; Identity 144/37/30 — deben mantenerse verdes y crecer.

## 8. Contratos y documentación

- `contracts/http/identity-api.md`: sección Governance (5.1-5.3 con auth y errores).
- `contracts/events/identity-events.md`: §Transport + 6 eventos registrados (4 equipos
  rewired + 2 gobernanza) con samples 1:1; nota de diferidos (`UsuarioCreado`,
  `CredencialTemporalEmitida`).
- `GUIA-LEVANTAMIENTO.md`: env RabbitMQ de Identity (`RabbitMq__Enabled`, `RabbitMq__Host`, …).
- Fila traceability SP-5b (pipes internos escapados `\|`).
- Sin ADR nuevo: ADR-0012 (best-effort) y ADR-0013 (composites) cubren la doctrina y se
  referencian desde el contrato de eventos.

## 9. Fuera de alcance / remanentes

- UI web del panel → **SP-5c**.
- `UsuarioCreado`/`CredencialTemporalEmitida` + email vía broker (hoy SMTP directo) → slice
  audit/notificaciones futuro.
- Consumidor de eventos identity en Puntuaciones → SP-4.
- Reconciliación automática DB↔Keycloak al arranque → no (write-through + PUT re-ejecutable
  bastan; documentado en 5.2).
- Remanentes de SP-5a intactos (política `/puntuaciones` post-SP-4, clientes→gateway, minors
  diferidos F-1/F-2 y compañía).

## 10. Riesgos / puntos abiertos

- **Drift Keycloak↔DB tras 502 parcial en el PUT** (composites aplicados, DB no): aceptado —
  el PUT por diff es la reparación (re-ejecutar). Documentado en contrato.
- **`ChangeUserRealmRoleAsync` no atómico** (remove+assign son 2 llamadas): fallo entre ambas
  deja al usuario sin rol base en Keycloak hasta reintento; el PATCH re-ejecutado repara
  (remove tolerará 404-composite/rol ausente — el plan fija la tolerancia exacta). Ventana
  mínima, solo admin-driven.
- **Consulta "equipo activo" para el 409:** reusa `IEquipoRepository.GetActiveByMemberUserIdAsync`
  existente (verificar firma exacta en el plan).
- **TestAuthHandlers de Identity** ya simulan composites (SP-5a); el contract 403 de gobernanza
  usa ese mecanismo sin cambios.
