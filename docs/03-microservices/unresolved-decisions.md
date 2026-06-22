# Unresolved Decisions

## Resolved: microservice topology

The topology is settled. UMBRAL uses **four** physical backend microservices behind a **mandatory YARP gateway**:

- Identity
- Partidas
- Operaciones de Sesion
- Puntuaciones

The previous decomposition (`Team Service`, `Trivia Game Service`, `BDT Game Service`, non-enforced gateway) is **obsolete** and must not be reintroduced. Teams live inside Identity; Trivia/BDT configuration inside Partidas; Trivia/BDT runtime inside Operaciones de Sesion; scoring/ranking/audit inside Puntuaciones.

The decomposition and renaming are governed by the four-service migration ADR under `docs/05-decisions/`.

## Resolved: BDT ranking

BDT native ranking is by **accumulated points** = sum of the `Puntaje` of the won stages, tie-broken by lowest accumulated time of the won stages only. The count of stages won is informative data only. The old "rank by number of stages won" rule is obsolete.

## Explicit non-services

The following must never be created as separate physical services:

- `Scoring Service` — scoring/ranking is **Puntuaciones**.
- `Audit Service` — audit/history is materialized in **Puntuaciones** and **Operaciones de Sesion**.
- `Notification Service` — async email notification lives inside **Identity**.

## Remaining items for SDD/contracts

These are not topology issues; they are resolved per HU during SDD before implementation:

- **Concrete HTTP contracts.** Routes, methods and payloads per HU live in `contracts/http/`.
- **Concrete event contracts.** Exchange/queue/routing-key names, payload schemas, idempotency and outbox policy per event live in `contracts/events/`.
- **Concrete SignalR contracts.** Hub names and message shapes per real-time feature.
- **On-disk migration debt.** Folder slugs, host ports, `run-local` scripts and `.sln` names for the four services are finalized in the migration ADR; existing folders/ports that still reflect the old layout are debt, not the target.
- **Legacy SDD folders.** Specs from the older mission/session or four-old-service model must be regenerated/reviewed against the current topology before use.
