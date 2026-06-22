# Source Basis

> **Authority:** the current service topology is defined by `CLAUDE.md` and `docs/01-project-source/microservicios.md`. Where this folder and the derived context layer differ, those two sources win.

## Sources considered

| Source | Use in this folder |
|---|---|
| `CLAUDE.md` (repo root) | Operational summary of the target architecture: four services, mandatory gateway, hard boundaries, ranking doctrine, roles/permissions, teams in Identity. |
| `docs/01-project-source/microservicios.md` | Authoritative ownership table: Partidas, Operaciones de Sesion, Puntuaciones, Identity — responsibilities, DDD contexts, covered HUs and persistence. |
| `docs/01-project-source/srs.md` | Functional and non-functional requirements, business rules, real-time, RabbitMQ, Keycloak and scope. |
| `docs/01-project-source/modelo-de-dominio.md` | Bounded contexts, concepts, commands, canonical domain events and domain/application services. |
| `docs/01-project-source/diagrama-de-clases.md` | Aggregates, entities, value objects, relationships and the audit/history context. |
| `docs/01-project-source/historias-de-usuario.md` | HU scope and assignment by owning service. |
| `docs/02-project-context/` | Derived operational summaries; consistent with this folder but not authoritative over it. |

## No-assumption rules applied

- No new physical services beyond the four target services and the gateway are introduced.
- No HTTP endpoints, routes, methods, request/response payloads, status codes or pagination formats are invented.
- No queue names, exchange names, topics or routing keys are invented.
- No SignalR hub names or message shapes are invented.
- Only canonical event names already named in `CLAUDE.md` and `docs/01-project-source/modelo-de-dominio.md` are referenced.
- Concrete route/payload/event definitions are produced per HU during SDD and recorded under `contracts/`.

## Hierarchy used for this folder

1. For service names, count and ownership: `CLAUDE.md` and `docs/01-project-source/microservicios.md`.
2. For functional rules and technical constraints: `docs/01-project-source/srs.md`.
3. For concepts, aggregates and domain events: `docs/01-project-source/modelo-de-dominio.md` and `docs/01-project-source/diagrama-de-clases.md`.
4. For HU scope and assignment: `docs/01-project-source/historias-de-usuario.md`.

## Criterion on contradictions

If older on-disk material (code folders, ports, DB names) or an older spec still reflects the previous layout (`Team Service`, `Trivia Game Service`, `BDT Game Service`, BDT ranking by stages won), it is treated as **migration debt / legacy evidence**, not as the target. The target doctrine in `CLAUDE.md` and `microservicios.md` always supersedes it.
