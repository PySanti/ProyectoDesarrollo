# Code Structure Doctrine Migration Design

Date: 2026-06-22

## Purpose

Migrate the UMBRAL **codebase** so its structure and behavior comply with the current doctrine (the four target services, the mandatory YARP gateway, the `Partida`-contains-`Juego` domain model, and the new/modified business rules). This reforms what already exists — re-homing and reshaping the implemented user stories — **without adding new HUs or features**. The architectural elements the doctrine mandates but that do not yet exist in code (the YARP gateway, the `Partida` aggregate, the four target service boundaries) are treated as part of *adapting to doctrine*, not as new features.

This is the deferred Tier-3 code migration that `docs/02-project-context/documentation-migration-status.md` lists as remaining work, now that the documentation migration is complete.

## Authoritative Inputs

- `CLAUDE.md`
- `docs/01-project-source/{srs.md,modelo-de-dominio.md,diagrama-de-clases.md,microservicios.md}`
- The migrated derived docs under `docs/02-project-context/`, `docs/03-microservices/`, `docs/05-decisions/` (esp. ADR-0008), and the reset contracts under `contracts/`.

## Current State (what exists, under the old doctrine)

- **Four services in the old decomposition**, each with the standardized Clean Architecture 4-project layout (Domain/Application/Infrastructure/Api + tests):
  - `identity-service` (`Umbral.IdentityService`) — `Usuario`, `RolUsuario`, `EstadoUsuario`, Keycloak (HU-01/02). **Stays** (Identity is a target service).
  - `team-service` (`Umbral.TeamService`) — `Equipo` (with `CodigoAcceso`), `ParticipanteEquipo`, leadership/transfer/leave. Join is **by access code** (`UnirseAEquipoPorCodigoCommand`); **no `InvitacionEquipo` exists**.
  - `trivia-game-service` (`Umbral.TriviaGame`) — `PartidaTrivia`, questions, scoring/ranking (HU-15/17/24/29/30). Standalone "a partida IS one trivia game".
  - `bdt-game-service` (`Umbral.BdtGameService`) — `PartidaBDT`, `ExploradorBDT` with `EtapasGanadas`-based ranking (HU-10/12/37/39/43/44). Standalone.
- **No YARP gateway in code** (only `gateway/gateway-context.md`).
- **Four old databases**: `umbral_identity`, `umbral_team`, `umbral_trivia_game`, `umbral_bdt_game`.
- ~448 `.cs` files; ~87% reference the old decomposition. The old BDT ranking (stages won) and the team access code are **implemented behavior**, not just names.

## Target Architecture (doctrine)

- Four physical services behind a mandatory YARP gateway: **Identity**, **Partidas**, **Operaciones de Sesion**, **Puntuaciones**. DBs: `umbral_identity`, `umbral_partidas`, `umbral_operaciones_sesion`, `umbral_puntuaciones`.
- **`Partida`** (aggregate root) contains **1..\* `Juego`** played sequentially; each `Juego` is `JuegoTrivia` or `JuegoBDT`. Lobby, inscription, modality, start mode, lifecycle, and the **consolidated ranking** are partida-level.
- **BDT native ranking by accumulated points** (sum of won-stage `Puntaje`, tie-break by lowest accumulated time of won stages; `EtapasGanadas` informative only).
- **Teams join only via `InvitacionEquipo`** (no access code), owned by Identity.
- Clients reach the backend only through the gateway, including real-time.

## Scope

### Included
- Restructure the existing code into the four target services + gateway, reshaping the `Partida`/`Juego` model and applying the new/modified business rules to already-implemented HUs.
- Recreate the target databases and update infra (docker-compose, connection strings).
- Update contracts (HTTP/events) and SignalR to the target services, gateway-mediated.
- Update clients (web/mobile) to the gateway and the new model where a slice changes their contract.
- **Obsolete-doctrine review phases** (R1/R2/R3, below) woven into the plan.

### Excluded
- New HUs / new features / new game modes.
- Production data migration (academic project → databases are dropped and recreated).
- Visual redesign beyond what a contract/model change forces.

## Decomposition Into Sub-Projects

This migration is too large for one spec. It is decomposed into sequential sub-projects; **each is its own spec → plan → implementation cycle** and leaves the system working. This document fully specifies **SP-1**; the others are scoped here and will get their own design when reached.

| # | Sub-project | Reforms (from what exists) | Depends on |
|---|---|---|---|
| **SP-0** | **Foundation** | YARP gateway (routes to the four services); target DBs; target service solution shells (Identity stays; Partidas / Operaciones de Sesion / Puntuaciones as empty destinations); docker-compose entries | — |
| **SP-1** | **Identity absorbs Equipos** | Move `team-service` into Identity; **replace access code with `InvitacionEquipo`**; merge `umbral_team` into `umbral_identity`; adapt mobile teams flow | SP-0 (minimal gateway/DB; may be folded in) |
| **SP-2** | **`Partida`/`Juego` model + Partidas (config)** | Introduce `Partida` (root) containing 1..\* `Juego`; re-home Trivia question config and BDT stage config (per-stage `Puntaje`) into Partidas | SP-0 |
| **SP-3** | **Operaciones de Sesion (runtime)** | Lobby, inscriptions, convocatorias, start, question/stage sync, answer/QR validation, clues, geolocation, reconnection, session SignalR — extracted from Trivia/BDT runtime | SP-2 |
| **SP-4** | **Puntuaciones (scoring + rankings)** | Trivia points, **BDT point-based ranking**, consolidated partida ranking, audit/history projection, ranking SignalR; RabbitMQ-fed read model | SP-3 |
| **SP-5** | **Clients (web/mobile)** | Repoint to the gateway; adapt flows/UI to the `Partida`/`Juego` model and new rules; fix Tier-2 strings | SP-2..SP-4 |

Cross-cutting per slice: HTTP/event contracts and SignalR are updated alongside the service they belong to.

## Obsolete-Doctrine Review Cadence

The migration's purpose is to remove active obsolete doctrine from the code, so detection is a first-class deliverable, not an afterthought.

### Shared Detection Ruleset
Every review scans the same obsolete-doctrine signatures, distinguishing an **active defect** from an acceptable **negation / historical / legacy / marked-migration-debt** reference:
- Old names / folders / namespaces: `Umbral.TeamService|TriviaGame|BdtGameService`; folders `team-service`, `trivia-game-service`, `bdt-game-service`; DBs `umbral_team`, `umbral_trivia_game`, `umbral_bdt_game`.
- Old aggregates/entities: `PartidaTrivia`, `PartidaBDT`, `CompetidorTrivia`, `ExploradorBDT`, `FormularioTrivia`.
- Old rules: `CodigoAcceso` / access code as a join mechanism; `EtapasGanadas` as the BDT ranking sort key.
- Direct client-to-service routing (bypassing the gateway).

A reusable detection script (`rg`-based, with the negation/legacy exception patterns) is produced so every R-phase and an optional CI gate run the same checks.

### Phases
- **R1 — Per-slice gate (after EACH SP).** A multi-agent review scoped to that slice's changed surface, confirming the obsolete doctrine the slice was meant to remove is gone and nothing reintroduced it. Blocks slice completion if active defects remain. (E.g. SP-1's gate: zero active `CodigoAcceso`/`Umbral.TeamService`/`umbral_team`/join-by-code; Equipos correctly inside Identity; `InvitacionEquipo` working.) **R1 structural checklist (per SP):** `Api/Controllers/` present and `Program.cs` contains no `app.Map{Get,Post,Put,Delete,Patch}` (only `MapControllers`/`MapHub`); `Application/` has exactly the mandated folder set; `Infrastructure/` has `Persistence/` + `Services/`; a centralized exception middleware is registered; every controller has a unit test.
- **R2 — Milestone audit (at structural milestones).** A broader cross-service sweep after key milestones (after SP-1 = Identity consolidated; after SP-2 = `Partida`/`Juego` model; after SP-4 = scoring/ranking). Measures the **global obsolete-doctrine footprint** (the percentage metric) across Tier-1/2/3 and verifies it trends toward zero.
- **R3 — Final repo audit (migration close).** An exhaustive adversarial review of the whole repo confirming zero active obsolete doctrine across Tier-1/2/3, separating real defects from negations/historical.

Each R-phase runs as a multi-agent adversarial workflow (finders by surface → adversarial verification that filters negations/false-positives → synthesis + verdict), is recorded in the progress ledger, and routes defects to a fix wave before the slice/milestone closes.

## SP-1 Detailed Design — Identity Absorbs Equipos

**Approach:** lift-and-reshape. Move `team-service`'s code into Identity and **surgically replace** the access-code join with `InvitacionEquipo`, keeping the domain logic that is already doctrine-valid and its tests. (Rejected: reimplement Equipos from scratch — discards working, tested logic.)

### Target structure
The `team-service` folder is removed. Its Equipos context enters Identity's four projects as a context distinct from Identidad, by sub-folder (`Domain/Equipos/*`, `Application/Equipos/*`, etc., alongside `Domain/Identidad/*`). One service **Identity**, one DB **`umbral_identity`** (Equipos tables added; `umbral_team` retired).

### Domain
- `Equipo`: keep cardinality 1–5 (`EnsureCardinalityInvariant`), creator-as-first-member-and-leader, `AgregarParticipante`, `Salir` (leader-with-others must transfer; lone leader leaving deletes the team), `TransferirLiderazgo`. **Remove `CodigoAcceso`** (field, constructor/factory params, validation). New factory `Equipo.CrearPorParticipante(nombreEquipo, creadorUserId)`.
- New **`InvitacionEquipo`**: `InvitacionEquipoId`, `EquipoId`, `UsuarioInvitadoId`, `EstadoInvitacion` ∈ {`Pendiente`, `Aceptada`, `Rechazada`}. Sent by the team leader; **does not expire**; accepting calls `Equipo.AgregarParticipante` (and is rejected if the team is full or the user is already in a team); deleting a team deletes its pending invitations.
- Invariant (Identity-enforced, since it owns `Usuario` and `Equipo`): a user belongs to **only one active team at a time**.
- Remove access-code exceptions (`AccessCodeGenerationException`, `TeamNotFoundByAccessCodeException`); add invitation exceptions (already-in-team, team-full, actor-not-leader, invitation-not-pending, invited-user-not-found).
- `InvitacionEquipo` is distinct from `Convocatoria` (partida-level team summoning, which belongs to Operaciones de Sesion / SP-3). SP-1 does not touch `Convocatoria`.

### Application (CQRS / MediatR)
- Replace `UnirseAEquipoPorCodigoCommand` with `InvitarParticipanteCommand`, `AceptarInvitacionCommand`, `RechazarInvitacionCommand`.
- New query `ListarParticipantesInvitablesQuery` — the dynamic participant list that excludes anyone already in a team and is blocked when the team is full.
- Keep (minus access code): `CrearEquipoCommand`, `SalirEquipoCommand`, `TransferirLiderazgoCommand`, and the team queries.
- Handlers under `Handlers/Commands/` and `Handlers/Queries/`; validators under `Validators/`.

### Infrastructure / persistence
- Equipos persistence targets `umbral_identity` (Identity's DbContext extended, or a dedicated Equipos DbContext over the same database). EF config: drop the `codigoacceso` column and index; add an `invitaciones_equipo` table. Repositories implement the Domain-defined interfaces.

### Api / contracts / events
- Equipos endpoints move into Identity's Api as controllers inheriting the native `ControllerBase` and dispatching through MediatR, exposed under the gateway route family `/api/identity/equipos/*` and `/api/identity/invitaciones/*`.
- Update `contracts/http/identity-api.md` (Equipos + invitations endpoints) and `contracts/events/identity-events.md` (`EquipoCreado`, `InvitacionEquipoEnviada`, `InvitacionEquipoAceptada`, `InvitacionEquipoRechazada`, `LiderazgoTransferido`, `IntegranteSalioDelEquipo`, `EquipoEliminado`). The legacy `team-api.md`/`team-events.md` are already archived under `_legacy/`.

### Clients (mobile)
The mobile `teams/` feature changes from join-by-code to invitation-based: the "join by access code" screen is replaced by an invitations inbox (accept/reject) and a leader "invite participant" flow over `ListarParticipantesInvitablesQuery`. This keeps the slice functional end-to-end (teams live on mobile). Web is unaffected (teams are not a web surface).

### Infra / DB
Remove the `team-service` container and its env from `infra/docker-compose.yml`; Equipos tables live in `umbral_identity`; databases dropped and recreated.

### Testing
Migrate `team-service`'s domain/application/integration tests into Identity's test projects, adapted (no access code; invitation flow), with TDD for `InvitacionEquipo` and the dynamic-list query. Controller unit tests are required.

### SP-1 R1 gate
After implementation, run the R1 multi-agent review over the SP-1 surface (Identity service, mobile teams, contracts/events, docker-compose) against the shared detection ruleset. Pass criteria: no active `CodigoAcceso`/access-code join, no `Umbral.TeamService`/`team-service`/`umbral_team` references except legacy/historical, Equipos fully inside Identity, `InvitacionEquipo` flow working, mobile teams adapted. Defects → fix wave before SP-1 closes.

### Out of SP-1 scope
The `Partida`/`Juego` model and other services; the full gateway build (minimal routing only); web; per-participant team-name history (deferred — additive, non-blocking); Identity governance/permission enhancements beyond what Equipos needs.

## Global Constraints / Conventions

- Per-service standardized layout (`*.Domain/*.Application/*.Infrastructure/*.Api` + `tests/`); `Application/` folders exactly `Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `DTOs/`, `Handlers/`, `Handlers/Commands/`, `Handlers/Queries/`, and `Exceptions/` (`DTOs/` holds request/response models; `Interfaces/` holds application-layer ports — repository interfaces stay in `Domain/`; `Exceptions/` holds application-layer exceptions); controllers inherit the native `ControllerBase`, dispatch via MediatR, hold no business logic, and have unit tests; Domain holds entities/enums/exceptions/repository interfaces; Infrastructure has `persistence/` and `services/`; centralized exception handling per service. The graded structure (Controllers + `ControllerBase` + controller unit tests, the flat `Application/` folder set, `Infrastructure/{persistence,services}/`, centralized exception middleware) is a **mandatory acceptance criterion for every SP**; the SP-1R-refactored `identity-service` is the canonical reference implementation.
- A service never reads/writes another service's database; cross-service async over RabbitMQ; user-facing real-time over SignalR through the gateway.
- TDD per task; frequent commits; each slice leaves the build/tests green.
- Do not reintroduce obsolete doctrine; old names may appear only as negations/historical/legacy/marked-debt.

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Migration is huge and risks a broken build mid-way. | Vertical slices (SP-0..SP-5), each leaving the system working; per-slice R1 gate. |
| Pattern contamination: agents copy old code patterns. | Shared detection ruleset + R1/R2/R3 reviews; agent-steering `.claude/` skills already corrected. |
| `Partida`/`Juego` restructure is deep (SP-2). | Isolated as its own sub-project with its own design/plan when reached; SP-1 first to prove the pattern on a smaller context. |
| Client breakage when a backend contract changes. | The slice that changes a contract also adapts the affected client surface (SP-1 adapts mobile teams). |
| Obsolete doctrine reintroduced silently. | Reusable detection script as an optional CI gate; R-phases recorded in the ledger. |

## Approval Status

Decomposition (full structural migration), SP-1 design (lift-and-reshape; defer team-name history; adapt mobile within SP-1), and the obsolete-doctrine review cadence (R1 per-slice, R2 milestone, R3 final, shared ruleset) approved by the user on 2026-06-22. Ready for implementation-plan writing of **SP-1**.
