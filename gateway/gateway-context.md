# Gateway — context

The mandatory YARP entry point. Validates the Keycloak JWT and applies coarse, route-level
authorization by base role; routes to the four services; passes WebSockets through for SignalR.
Owns no domain logic, scores, rankings, or DB access.

Config-first: routes/clusters/role-mapping live in `appsettings.json` (`ReverseProxy`); the
security border (JWT scheme, role/permission policies, `RequireAuthenticatedUser` fallback) is
the only code.

Status: SP-5a — reenvío puro (sin `PathRemovePrefix`), rutas por prefijo de servicio hacia
identity (5000), partidas (5010), operaciones-sesion (5020), puntuaciones (5030). Matriz de
rutas/políticas (`ReverseProxy:Routes` en `appsettings.json`):

| Ruta (Match.Path) | Order | Policy |
|---|---|---|
| `/identity/governance/{**catch-all}` | 1 | `Administrador` |
| `/identity/users/{**catch-all}` | 1 | `Administrador` |
| `/identity/teams/{**catch-all}` | 1 | `Participante` |
| `/identity/{**catch-all}` | 2 | Default (autenticado) |
| `/partidas/{**catch-all}` | — | `OperadorOAdministrador` |
| `/operaciones-sesion/{**catch-all}` | — | Default (autenticado) |
| `/puntuaciones/{**catch-all}` | — | Default (autenticado) — política fina diferida post-SP-4 |

Las sub-rutas `/identity/governance`, `/identity/users` y `/identity/teams` ganan sobre
`/identity/{**catch-all}` por `Order` explícito (1 < 2). El coarse-role del gateway autoriza por rol base; la autorización por
permiso funcional (`GestionarPartidas`/`GestionarEquipos`/`ParticiparEnPartidas`) vive dentro de
cada servicio (ADR-0013). `access_token` por query sigue soportado para el handshake WebSocket
de hubs SignalR (`/operaciones-sesion/hubs/sesion`); `/health` del gateway es anónimo. Detalle
completo en `contracts/http/gateway-api.md`.
