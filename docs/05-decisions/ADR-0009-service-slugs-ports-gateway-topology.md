# ADR-0009: Service Slugs, Namespaces, Ports & Gateway Topology

## Status
Accepted (2026-06-23) — supersedes the "slugs finalized in the migration ADR" placeholder in CLAUDE.md.

## Context
The migration target is four services behind a YARP gateway. On-disk folders use a `-service`
suffix (`identity-service`); CLAUDE.md's run-local/commands use suffix-less slugs. SP-0 creates
three new shells and the gateway and must fix the convention.

## Decision
New shells use suffix-less doctrine slugs and `Umbral.<Svc>.*` namespaces. The existing
`identity-service` and the legacy `trivia-game-service` / `bdt-game-service` are NOT renamed in
SP-0; their rename/replacement is deferred (Identity rename is cosmetic and out of scope; the game
services are dismantled in SP-3/SP-4). The temporary coexistence of `identity-service` and
`partidas` is accepted migration debt.

| Component | Folder | Root namespace | Local port | Compose host port | Database | Conn-string key |
|---|---|---|---|---|---|---|
| Gateway | `gateway/` | `Umbral.Gateway` | 5080 | 5080:8080 | — | — |
| Identity (exists) | `services/identity-service` | `Umbral.IdentityService` | 5000 | 5001:8080 | `umbral_identity` | `IdentityDatabase` |
| Partidas | `services/partidas` | `Umbral.Partidas` | 5010 | (internal) | `umbral_partidas` | `PartidasDatabase` |
| Operaciones de Sesión | `services/operaciones-sesion` | `Umbral.OperacionesSesion` | 5020 | (internal) | `umbral_operaciones_sesion` | `OperacionesSesionDatabase` |
| Puntuaciones | `services/puntuaciones` | `Umbral.Puntuaciones` | 5030 | (internal) | `umbral_puntuaciones` | `PuntuacionesDatabase` |

Gateway routes: `/identity/*`→identity, `/partidas/*`→partidas, `/operaciones-sesion/*`→operaciones-sesion,
`/puntuaciones/*`→puntuaciones. Legacy trivia/bdt are not routed through the gateway (clients hit them
directly until SP-5). Only the gateway publishes a host port; the four services stay on the internal network.

## Consequences
- CLAUDE.md's suffix-less run-local slugs are now authoritative for the three new services.
- Mixed naming (`identity-service` vs `partidas`) persists until a later cosmetic-rename slice.
