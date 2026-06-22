# Gateway HTTP Contract

## Status

Current contract index. Concrete endpoints require a current-doctrine SDD before implementation.

## Access Path

Requests enter through the YARP gateway.

## Owned Capabilities

- Single entry point for all client HTTP traffic.
- Routing to current target services.
- Routing of SignalR/WebSocket traffic.
- Keycloak JWT validation.
- Coarse route-level authorization by base role.

## Route Families

| Gateway route family | Target service |
|---|---|
| `/api/identity/*` | Identity |
| `/api/partidas/*` | Partidas |
| `/api/operaciones-sesion/*` | Operaciones de Sesion |
| `/api/puntuaciones/*` | Puntuaciones |
| `/hubs/*` | Gateway-routed SignalR/WebSockets |

## Endpoint Registry

| Capability | Method | Gateway path | Owning service | Status |
|---|---|---|---|---|
| Gateway route families | Defined by SDD | Defined by SDD | Gateway | Not registered |
