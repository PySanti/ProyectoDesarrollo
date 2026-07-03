# SP-5a — Autorización: enforcement coarse-role en gateway + permisos funcionales en servicios

- **Fecha:** 2026-07-03
- **Slice:** SP-5a (primero de tres: 5a enforcement, 5b gobernanza backend, 5c UI web del panel)
- **Rama:** `feature/code-migration-SP-5`
- **Deuda que cierra:** SP-3a spec §12 — política coarse-role del gateway + permisos funcionales
  (`GestionarPartidas` para publicar, `ParticiparEnPartidas` para inscribirse) diferidos a SP-5.
  También la nota forward-looking de SP-3g (Identity debe hostear bajo su prefijo).

## 1. Contexto y estado actual

- **Gateway (SP-0 + SP-3g):** JWT Keycloak validado; roles normalizados desde `realm_access`
  (claim `roles`, `KeycloakRoleClaims`); políticas `Administrador`/`Operador`/`Participante`
  definidas; fallback `RequireAuthenticatedUser`; reenvío puro sin transforms; soporte
  `access_token` por query para `/operaciones-sesion/hubs`. Rutas: `partidas`→Operador,
  `identity`/`operaciones-sesion`/`puntuaciones`→Default (solo autenticado).
- **Identity:** JWT + policies por rol (`AdminOnly` en Users, `ParticipantOnly` en
  Teams/TeamInvitations). Rutas viejas `api/identity/users` y `api/teams` — **no casan** con el
  prefijo `/identity/` del gateway (la ruta identity del gateway es hoy letra muerta; los
  clientes le pegan directo).
- **Operaciones de Sesión:** JWT validado pero `AddAuthorization()` sin políticas; cero
  `[Authorize]` en el controller HTTP (solo el hub). Endpoints abiertos en acceso directo.
- **Partidas:** cero auth (ni JWT wireado). Solo el gateway lo protege, y los clientes hoy no
  pasan por el gateway.
- **Puntuaciones:** sin auth — diferido post-SP-4 (ver §9).
- **Keycloak realm `UMBRAL-UCAB`:** 3 realm roles base; cero protocol mappers de permisos —
  el token hoy NO lleva permisos funcionales.
- **No existe código de permisos funcionales en ningún servicio.**
- **Worker de Operaciones** (`MantenimientoSesionesWorker`) usa `ISender` in-process — no hay
  auto-llamada HTTP; proteger todos los endpoints no rompe el scheduler.

## 2. Decisiones tomadas (brainstorming 2026-07-03)

| # | Decisión | Elección |
|---|---|---|
| D1 | Alcance SP-5 | Enforcement + gobernanza completa (BR-R02) + UI web |
| D2 | Gobernanza | Permisos por rol **y** cambio de rol de usuario (nunca-admin, propagación Keycloak) |
| D3 | Slicing | 3 sub-slices: 5a enforcement, 5b gobernanza backend, 5c UI web — cada uno spec→plan→implementación |
| D4 | Fuente de permisos | **Composites Keycloak**: permisos = realm roles técnicos composite de los roles base; viajan en `realm_access.roles` del token (ADR-0013) |
| D5 | Borde 5a | Incluye re-homing de rutas Identity a la convención de prefijo + update mecánico de paths en mobile |
| D6 | Clientes → gateway | Fuera de SP-5; slice posterior (base URL única) |

Enfoques descartados para D4: (B) Identity DB + eventos RabbitMQ + cache por servicio
(contradice CLAUDE.md "el token lleva permisos"; Partidas no tiene RabbitMQ; cache/warm-up/
re-sync = mucho código transversal) y (C) consulta HTTP a Identity con cache TTL (acoplamiento
runtime de 3 servicios a Identity vivo; contrario al espíritu de la directiva).

## 3. Arquitectura común (cruza 5a/5b/5c) — ADR-0013

- El realm gana **3 realm roles técnicos**: `GestionarPartidas`, `GestionarEquipos`,
  `ParticiparEnPartidas`, asignados como **composite** de los roles base según BR-R03:
  - `Operador` → `GestionarPartidas`
  - `Participante` → `GestionarEquipos` + `ParticiparEnPartidas`
  - `Administrador` → ninguno (sus privilegios de gobernanza = el rol base `Administrador`,
    protegidos e irrevocables per CLAUDE.md)
- Keycloak expande composites automáticamente → `realm_access.roles` lleva rol base + permisos.
  **Cero mappers custom.** Los normalizadores `KeycloakRoleClaims` existentes (gateway e
  Identity) pasan roles desconocidos tal cual → los servicios usan `RequireRole("<permiso>")`
  en policies con nombre del permiso. **Operaciones NO tiene normalizador hoy** (setea
  `RoleClaimType "roles"` pero nada puebla ese claim desde `realm_access`) y Partidas no tiene
  auth: ambos ganan el normalizador en 5a (§5.1, §5.2).
- `infra/keycloak/import/umbral-realm.json` actualizado (roles técnicos + composites) +
  instrucciones de re-seed en `infra/keycloak/README.md`.
- **ADR-0013** (siguiente número tras ADR-0012, en `docs/05-decisions/`): los roles técnicos de
  permiso **no son roles de usuario**; los 3 roles base siguen siendo los únicos roles
  asignables a usuarios (la regla "no se crean roles nuevos" refiere a roles base de negocio).
  Registra defaults BR-R03, la expansión composite y el mecanismo de gobernanza futuro.
- Gobernanza (5b): Identity DB = fuente de verdad para panel + auditoría; propagación a
  Keycloak vía Admin API (add/remove composite) — mismo patrón "propagado a Keycloak" que
  CLAUDE.md ya manda para cambio de rol. Cambios efectivos al siguiente refresh del token.

## 4. Gateway — matriz coarse-role por prefijo

Reenvío puro se mantiene (sin transforms). Rutas más específicas ganan; verificar precedencia
YARP en el plan y fijar `Order` explícito si hace falta.

| Ruta YARP (Match.Path) | Política |
|---|---|
| `/identity/users/{**catch-all}` | `Administrador` |
| `/identity/teams/{**catch-all}` | `Participante` |
| `/identity/{**catch-all}` (resto) | Default (autenticado) |
| `/partidas/{**catch-all}` | `OperadorOAdministrador` (nueva policy: `RequireRole("Operador","Administrador")` — admin lee, las mutaciones las frena el permiso dentro del servicio) |
| `/operaciones-sesion/{**catch-all}` | Default (autenticado — el prefijo sirve genuinamente a los 3 roles; fino adentro) |
| `/puntuaciones/{**catch-all}` | Default — **diferido post-SP-4** (§9) |

- Soporte `access_token` query para hubs queda intacto.
- `/health` del gateway sigue anónimo.

## 5. Servicios — defensa en profundidad

### 5.1 Partidas (hoy cero auth)

- Wire JWT Keycloak espejo de Operaciones: authority/audiences/issuers por env
  (`KEYCLOAK_VALID_AUDIENCES`/`KEYCLOAK_VALID_ISSUERS`), `RoleClaimType "roles"` +
  normalizador de claims Keycloak.
- `FallbackPolicy = RequireAuthenticatedUser` (fail-secure); health `[AllowAnonymous]`.
- Policy `GestionarPartidas` (`RequireRole("GestionarPartidas")`).

| Endpoint | Política |
|---|---|
| `POST /partidas`, `POST /partidas/{id}/juegos/trivia`, `POST /partidas/{id}/juegos/bdt` | `GestionarPartidas` |
| `GET /partidas/{id}` | autenticado solo — Operaciones reenvía el token del **participante** en la llamada interna (SP-3a §12); debe seguir pasando |

### 5.2 Operaciones de Sesión (JWT ya wireado)

- Gana normalizador `KeycloakRoleClaims` (`OnTokenValidated` → roles desde `realm_access`,
  patrón gateway/Identity) — hoy `RoleClaimType "roles"` está seteado pero nada lo puebla.
- Policies `GestionarPartidas` + `ParticiparEnPartidas`; `FallbackPolicy` autenticado; health
  anónimo; hub queda `[Authorize]` (ambos roles lo usan).

| Endpoints | Política |
|---|---|
| `publicacion`, `inicio`, `inicio-automatico`, `juego-actual/finalizacion`, `pregunta-actual/avance`, `etapa-actual/avance`, `pistas` (POST) | `GestionarPartidas` |
| `inscripciones` (ind + equipo, alta/baja), `convocatorias/{id}/aceptacion`, `convocatorias/{id}/rechazo`, `pregunta-actual/respuesta`, `etapa-actual/tesoro`, `mi-sesion`, `mis-convocatorias` | `ParticiparEnPartidas` |
| GETs compartidos: `lobby`, `estado`, `pregunta-actual`, `etapa-actual` | autenticado (operador opera, participante ve) |

### 5.3 Identity (re-homing + swap de política)

- **Re-homing de rutas** a la convención SP-3g (servicio hostea bajo su prefijo):
  - `api/identity/users` → `identity/users`
  - `api/teams` (Teams + TeamInvitations) → `identity/teams`
  - Churn mecánico de paths en tests con const base compartida (patrón SP-3g).
- **Swap de política:** Users queda `AdminOnly`; Teams/TeamInvitations pasan de
  `ParticipantOnly` a policy `GestionarEquipos` (`RequireRole("GestionarEquipos")` — BR-R02
  literal: el permiso, no el rol, autoriza gestión de equipos).

### 5.4 Clientes (mobile + frontend web)

- **Mobile:** update mecánico de paths teams/invitations al nuevo prefijo (`identity/teams...`).
- **Frontend web:** `src/api/identityApi.ts` pega a `api/identity/users` directo — update
  mecánico a `identity/users` (hallazgo de planificación; el spec original solo listaba mobile).
- La base URL directa se mantiene en ambos (el servicio hostea bajo su prefijo, el acceso
  directo sigue funcionando); el cambio a base URL gateway es un slice posterior (D6).

### 5.5 Puntuaciones

- Intocado (post-SP-4, §9).

## 6. Errores

- **401** = challenge (sin token / token inválido); **403** = Forbid (rol o permiso
  insuficiente). Body vacío default de ASP.NET; no se tocan los middlewares de excepción.
- Gateway y servicios consistentes en la semántica 401/403.
- TimeProvider: N/A (sin lógica temporal nueva).

## 7. Testing

- **Gateway** (extender integration tests existentes): matriz ruta→política — 401 sin token,
  403 rol equivocado, paso con rol correcto; precedencia `/identity/users` vs `/identity/teams`
  vs resto de `/identity`; health anónimo.
- **Partidas:** contract tests nuevos con TestAuthHandler — 401 sin token, 403 sin permiso,
  2xx con `GestionarPartidas`; `GET /partidas/{id}` pasa con token de participante (protege la
  llamada interna de Operaciones). Controller unit tests intactos.
- **Operaciones:** identidades de test ganan claims de permiso; contract tests 403 por clase de
  política (operador sin `ParticiparEnPartidas` en endpoints de participante y viceversa);
  suite 357/29/48 se mantiene verde.
- **Identity:** churn de paths (const base) + tests del swap (participante sin
  `GestionarEquipos` → 403). 165+ verdes.
- **Realm JSON:** validación estructural (roles técnicos + composites presentes) — assert
  ligero en script o test de infra.
- **Mobile:** suite completa + typecheck tras el update de paths.

## 8. Contratos y documentación

- `contracts/http/identity-api.md`: paths re-homed + columna auth por endpoint (401/403).
- `contracts/http/partidas-api.md` + `operaciones-sesion-api.md`: requisitos de auth por
  endpoint.
- `gateway/gateway-context.md`: matriz de rutas/políticas.
- ADR-0013 en `docs/05-decisions/`.
- Fila de traceability (pipes internos escapados).

## 9. Remanentes documentados (fuera de 5a)

- **Post-SP-4 (prefijo puntuaciones):** política coarse del prefijo `/puntuaciones`, token WS
  del hub de rankings (query `access_token` en el gateway), mini-pase de verificación.
- **SP-5b (gobernanza backend):** modelo permisos-por-rol en Identity DB; endpoints del panel
  (ver/asignar/quitar permisos por rol, protección del rol Administrador); cambio de rol de
  usuario (operador/participante, incl. promoción a admin, nunca el rol de un admin,
  propagación Keycloak); eventos de gobernanza.
- **SP-5c:** UI web del panel de gobernanza (rol Administrador).
- **Slice posterior (fuera de SP-5):** clientes web/mobile → base URL gateway única (hoy pegan
  directo; vars legacy trivia/bdt en `.env.example` se limpian ahí).

## 10. Riesgos / puntos abiertos

- **Precedencia de rutas YARP:** las sub-rutas `/identity/users` y `/identity/teams` deben
  ganar sobre `/identity/{**}`. Verificar en el plan (precedencia por especificidad de
  template; fijar `Order` explícito si hace falta).
- **Llamada interna Operaciones→Partidas:** `GET /partidas/{id}` con token de participante debe
  seguir pasando tras el wire de JWT en Partidas (cubierto por contract test dedicado).
- **Churn de paths Identity (~165 tests) + mobile:** mecánico y auto-checkeable (path olvidado
  = 404 ruidoso); const base compartida reduce divergencia (precedente SP-3g).
- **Re-seed del realm:** entornos con realm ya importado necesitan re-import o alta manual de
  los roles técnicos/composites; documentar en `infra/keycloak/README.md`.
- **Tokens emitidos antes del re-seed** no llevan los permisos → 403 hasta refresh; aceptable
  en dev (re-login).
