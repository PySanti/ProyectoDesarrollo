# Documentation Doctrine Replacement Design

Date: 2026-06-21

## Purpose

Replace UMBRAL's active collateral documentation so it follows the new project doctrine defined by the updated primary source documents and `CLAUDE.md`.

This is a documentation-only migration. It does not modify backend code, frontend code, service folders, Docker Compose, tests, runtime configuration, or database schemas.

## Authoritative Inputs

The migration uses only these sources as active doctrine inputs:

- `docs/01-project-source/srs.md`
- `docs/01-project-source/diagrama-de-clases.md`
- `docs/01-project-source/modelo-de-dominio.md`
- `docs/01-project-source/microservicios.md`
- `CLAUDE.md`

All other documentation is either regenerated from those inputs or archived as historical implementation evidence.

## Core Doctrine To Enforce

The active target architecture is the one currently summarized in `CLAUDE.md`:

- Backend is four physical .NET microservices: `Identity`, `Partidas`, `Operaciones de Sesion`, and `Puntuaciones`.
- A YARP gateway is mandatory and is the single backend entry point for web and mobile clients, including real-time traffic.
- The previous physical services `Team Service`, `Trivia Game Service`, and `BDT Game Service` are obsolete as active doctrine.
- `Identity` owns users, Keycloak mapping, roles, permissions, governance, teams, team membership, leadership, invitations, and temporary credential notification behavior.
- `Partidas` owns partida and game configuration: sequential `Juego`s, Trivia questions/options, BDT stages, modality, limits, and start configuration.
- `Operaciones de Sesion` owns live runtime execution: lobby publication, inscriptions, convocatorias, game start, question/stage synchronization, answer/QR validation, clue delivery, geolocation, reconnection, and real-time session communication.
- `Puntuaciones` owns scoring, rankings, consolidated ranking, audit/history materialization, and score/ranking real-time updates.
- BDT native ranking is now based on accumulated points from won stages, with lower accumulated time across won stages as tie-breaker. Counting stages won is informative, not the primary sort key.
- Web remains for `Administrador` and `Operador`; mobile remains for `Participante` and `Lider de equipo` acting as participant.

## Selected Approach

Use source-driven regeneration with legacy archive.

Rejected alternatives:

- Hard wipe all documentation: too destructive because it loses useful implementation evidence.
- Overlay warnings on existing docs: too risky because obsolete doctrine remains searchable and likely to mislead future agents.

The selected approach creates clean current documentation while preserving old implementation evidence in a clearly marked legacy area.

## Scope

### Included

- Replace or rewrite active collateral documentation derived from the old doctrine.
- Archive old SDD implementation evidence under `docs/04-sdd/_legacy-implementation-evidence/`.
- Create a fresh current SDD workspace aligned with the new doctrine.
- Add a new ADR that supersedes old architecture ADRs that contradict the new doctrine.
- Reset active HTTP and event contract documentation around the new services and gateway-mediated access.
- Update active root and component context docs that currently mention obsolete service ownership or direct client-to-service routing.
- Add a migration status/checklist document describing what was regenerated, archived, superseded, and intentionally deferred to later code-migration work.

### Excluded

- Renaming service folders.
- Moving code between microservices.
- Changing Docker Compose, runtime ports, `.env` files, or database names.
- Rewriting tests.
- Changing frontend/mobile behavior.
- Implementing the new architecture in code.

## Documentation Areas To Update

### Primary Sources

The updated source files live directly in `docs/01-project-source/` using the canonical filenames:

- `srs.md`
- `diagrama-de-clases.md`
- `modelo-de-dominio.md`
- `microservicios.md`

If incoming filenames use underscores, they are normalized to these hyphenated names before derived documentation is regenerated.

### Root Guidance

Update `AGENTS.md` so it no longer contradicts `CLAUDE.md`.

The active `AGENTS.md` must describe:

- the new four services;
- the mandatory gateway;
- source authority based on the new source documents and `CLAUDE.md`;
- BDT point-based ranking;
- Teams inside `Identity`, not a physical Team Service;
- SDD and contract regeneration rules for the new doctrine.

`CLAUDE.md` is treated as already authoritative. Only small consistency edits are needed if the new source files introduce naming or wording refinements.

### Derived Project Context

Regenerate `docs/02-project-context/` from the new source files and `CLAUDE.md`.

Expected current files include:

- project brief;
- SRS summary;
- business rules;
- first delivery or current scope summary if the new SRS defines one;
- glossary;
- source priority;
- known ambiguities and decisions;
- mobile participant context;
- design index;
- domain business rules;
- domain entities by context;
- class design by layer;
- service model impact;
- documentation migration status.

Pure visual redesign history under `docs/02-project-context/design/` can remain if it does not assert obsolete architecture. Any architecture/domain claims inside those files must be updated or marked as historical.

### Microservices And Gateway Context

Rewrite `docs/03-microservices/` around:

- `Identity`;
- `Partidas`;
- `Operaciones de Sesion`;
- `Puntuaciones`;
- mandatory YARP gateway routing and boundary rules.

The active microservice documentation must not describe `Team Service`, `Trivia Game Service`, or `BDT Game Service` as target physical services.

### SDD Workspace

Move current `docs/04-sdd/` active implementation evidence into:

```txt
docs/04-sdd/_legacy-implementation-evidence/
```

This legacy area must include a README stating:

- the content documents old implemented work;
- it is useful for evidence and migration context;
- it is not valid current planning input;
- new feature work requires regenerated SDDs under the new doctrine.

Recreate current `docs/04-sdd/` with clean files aligned to the new doctrine:

- `README.md` or equivalent overview;
- `SPECS-LIST.md` as the current active-spec registry;
- `traceability-matrix.md` as the current traceability file;
- SDD workflow;
- definition of ready;
- definition of done;
- feature template;
- `specs/README.md`.

The regenerated current SDD files may start with no active specs if the new source documents do not define the next implementation slice.

### ADRs

Do not silently rewrite architectural history.

Add a new ADR under `docs/05-decisions/` that records the doctrinal replacement and supersedes contradictory ADRs.

The new ADR must supersede at least:

- `ADR-0006-four-service-topology.md` where it defines `Identity Service`, `Team Service`, `Trivia Game Service`, and `BDT Game Service` as the active topology.
- `ADR-0002-service-per-business-capability.md` where it names service capabilities that no longer match the target doctrine.
- Any ADR or decision text that forbids BDT point-based ranking.

Existing ADRs can be marked as superseded by adding a short status note that points to the new ADR. Their historical decision text should remain intact.

### Contracts

Reset active contract documentation in `contracts/` for the new doctrine.

Current active contracts must be organized around:

- gateway route policy;
- Identity HTTP/events;
- Partidas HTTP/events;
- Operaciones de Sesion HTTP/events;
- Puntuaciones HTTP/events.

Old contract files tied to `team`, `trivia-game`, and `bdt-game` must not remain active. They can be archived or clearly labeled as legacy implementation evidence.

Contracts should remain placeholders or high-level indices unless the new source documents define concrete endpoints/events. Do not invent endpoint payloads, queue names, or SignalR event shapes beyond what the source documents and `CLAUDE.md` justify.

### Component Context And Guides

Update active context files that currently mention obsolete service topology or direct client-to-service routing:

- `frontend/frontend-context.md`
- `mobile/mobile-context.md`
- `gateway/gateway-context.md`
- `services/*/service-context.md`
- `README.md`
- `GUIA-LEVANTAMIENTO.md`
- `GUIA-USO-AGENTE.md`

These docs must acknowledge that code folders may still reflect the old layout until a later code migration. Documentation must distinguish target doctrine from current implementation debt.

## Migration Phases

### Phase 1: Source Replacement And Baseline

Confirm the updated source files are present in `docs/01-project-source/` and that `CLAUDE.md` represents the intended target doctrine.

Record the migration baseline in `docs/02-project-context/documentation-migration-status.md`.

### Phase 2: Legacy Quarantine

Archive old SDD specs, matrix, and spec list into the legacy evidence area.

Preserve old implementation evidence but remove it from active planning paths.

### Phase 3: Current Doctrine Regeneration

Regenerate active derived docs from the new source files and `CLAUDE.md`.

Use concise summaries and avoid importing old assumptions that are not present in the new source set.

### Phase 4: ADR And Contract Reset

Add the new superseding ADR.

Reset contracts around the new services and gateway.

Mark obsolete contracts as legacy or archive them.

### Phase 5: Context And Guide Alignment

Update root/component context docs and operational guides so future agents and developers do not follow obsolete service ownership.

### Phase 6: Consistency Pass

Run text-based checks for obsolete active doctrine and fix remaining contradictions.

## Validation Strategy

Validation is documentation-focused:

- Run `git diff --check`.
- Search active documentation for obsolete target-doctrine terms:
  - `Team Service`;
  - `Trivia Game Service`;
  - `BDT Game Service`;
  - `Treasure Hunt Service`;
  - old BDT ranking statements that reject point-based ranking;
  - direct client-to-service backend access;
  - team access code as active doctrine.
- Confirm any remaining obsolete terms are inside clearly marked legacy evidence or quoted historical context.
- Confirm every active document has a clear status: source, current derived doc, current contract, current ADR, superseded ADR, or legacy evidence.
- Confirm active docs consistently route clients through the gateway.
- Confirm active docs consistently use point-based BDT ranking.
- Confirm active docs consistently place teams inside Identity.

## Deliverables

- Updated `AGENTS.md` aligned with `CLAUDE.md` and the new source docs.
- Regenerated `docs/02-project-context/` and design context files.
- Rewritten `docs/03-microservices/` for the new four-service topology and gateway.
- Clean current `docs/04-sdd/` workspace.
- Archived old SDD implementation evidence under `docs/04-sdd/_legacy-implementation-evidence/`.
- New superseding ADR under `docs/05-decisions/`.
- Reset active contract docs under `contracts/`.
- Updated context docs and guides where they contain obsolete doctrine.
- Documentation migration status/checklist file.

## Risks And Mitigations

| Risk | Mitigation |
|---|---|
| Old implemented code still uses the previous service layout. | Documentation explicitly labels this as implementation migration debt and does not pretend code was migrated. |
| Future agents search old SDDs and follow obsolete assumptions. | Move old SDDs into `_legacy-implementation-evidence/` and add strong README warnings. |
| ADR history becomes confusing. | Add a superseding ADR and mark old conflicting ADRs as superseded instead of deleting history. |
| Contracts invent details not present in source docs. | Keep new contracts as indices/placeholders unless the source docs define exact routes/events. |
| Pure design-system history gets unnecessarily rewritten. | Preserve visual-only records unless they assert obsolete architecture or domain ownership. |

## Approval Status

The design was reviewed section by section with the user and approved for documentation-spec writing on 2026-06-21.
