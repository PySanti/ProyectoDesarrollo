# Gateway HTTP Contract

## Status

Route/policy matrix implemented since SP-5a (coarse-role + functional-permission-aware routing; pure pass-through, no path transforms). Reenvío puro se mantiene: YARP no reescribe paths (`PathRemovePrefix` no se usa) — cada servicio hostea bajo su propio prefijo (SP-3g).

## Access Path

Requests enter through the YARP gateway.

## Owned Capabilities

- Single entry point for all client HTTP traffic.
- Routing to current target services.
- Routing of SignalR/WebSocket traffic.
- Keycloak JWT validation.
- Coarse route-level authorization by base role.

## Route matrix (SP-5a)

Reenvío puro (sin `PathRemovePrefix`); rutas más específicas ganan por `Order` explícito donde
hace falta precedencia sobre un prefijo más general del mismo servicio.

| Ruta YARP (Match.Path) | Order | Política | Servicio destino |
|---|---|---|---|
| `/identity/governance/{**catch-all}` | 1 | `Administrador` | Identity |
| `/identity/users/{**catch-all}` | 1 | `Administrador` | Identity |
| `/identity/teams/{**catch-all}` | 1 | `Participante` | Identity |
| `/identity/{**catch-all}` (resto) | 2 | Default (autenticado) | Identity |
| `/partidas/{**catch-all}` | — | `OperadorOAdministrador` (`RequireRole("Operador","Administrador")`) | Partidas |
| `/operaciones-sesion/{**catch-all}` | — | Default (autenticado) | Operaciones de Sesión |
| `/puntuaciones/{**catch-all}` | — | Default (autenticado) — **política fina diferida post-SP-4** (SP-4 aún no expone endpoints HTTP propios; mismo diferimiento aplica al token WS por query del futuro hub de rankings) | Puntuaciones |
| `/health` | — | Anónimo (endpoint propio del gateway) | Gateway |

Notas:
- `/identity/governance` (Administrador, SP-5b), `/identity/users` (Administrador) y
  `/identity/teams` (Participante) son sub-rutas más específicas que ganan sobre
  `/identity/{**catch-all}` (Default) por `Order` explícito (1 < 2).
- Autorización por **rol base** en el gateway (coarse); la autorización por **permiso
  funcional** (`GestionarPartidas`/`GestionarEquipos`/`ParticiparEnPartidas`) vive dentro de
  cada servicio (ver `identity-api.md`, `partidas-config.md`, `operaciones-sesion-api.md`) —
  ver ADR-0013.
- Soporte `access_token` por query string para el handshake WebSocket de hubs SignalR se
  mantiene sin cambios (`/operaciones-sesion/hubs/sesion`).
- `401` = sin token / token inválido (challenge); `403` = rol insuficiente (Forbid). Body vacío
  default de ASP.NET Core.

## Endpoint Registry

| Capability | Method | Gateway path | Owning service | Status |
|---|---|---|---|---|
| Route matrix above | Defined by SDD | See "Route matrix (SP-5a)" | Gateway | Registered |
