# Documentation Doctrine Replacement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Regenerate UMBRAL's active collateral documentation from the new source doctrine and archive obsolete SDD/contract evidence without changing code.

**Architecture:** This is a documentation migration with a clean separation between current doctrine and legacy implementation evidence. The active doctrine is generated from `docs/01-project-source/{srs.md,diagrama-de-clases.md,modelo-de-dominio.md,microservicios.md}` and `CLAUDE.md`; old first-sprint SDDs/contracts are preserved only as historical evidence. The resulting docs describe the target service model: `Identity`, `Partidas`, `Operaciones de Sesion`, `Puntuaciones`, and mandatory YARP gateway.

**Tech Stack:** Markdown documentation, Git, PowerShell 5.1, `rg`/OpenCode grep, no application-code changes.

## Global Constraints

- Documentation-only: do not modify backend code, frontend code, service folders, Docker Compose, tests, runtime configuration, or database schemas.
- Authoritative inputs: only `docs/01-project-source/srs.md`, `docs/01-project-source/diagrama-de-clases.md`, `docs/01-project-source/modelo-de-dominio.md`, `docs/01-project-source/microservicios.md`, and `CLAUDE.md` define current doctrine.
- Current target services: `Identity`, `Partidas`, `Operaciones de Sesion`, `Puntuaciones`.
- Gateway: YARP gateway is mandatory and is the single backend entry point for web and mobile clients, including real-time traffic.
- Obsolete active services: `Team Service`, `Trivia Game Service`, and `BDT Game Service` must not appear as active target physical services outside legacy evidence or historical quotes.
- BDT ranking: active doctrine uses accumulated points from won stages, with lower accumulated time across won stages as tie-breaker; stages-won count is informative only.
- SDD evidence: old active `docs/04-sdd` specs, `SPECS-LIST.md`, and `traceability-matrix.md` must be archived under `docs/04-sdd/_legacy-implementation-evidence/` and removed from active planning paths.
- Contracts: do not invent concrete endpoint payloads, queue names, routing keys, or SignalR event shapes not justified by the new source docs and `CLAUDE.md`.
- Preserve user changes: do not modify unrelated working tree changes such as the existing `opencode.json` modification unless the user explicitly asks.

---

## File Structure

### New Or Recreated Current Files

- `docs/02-project-context/documentation-migration-status.md`: migration checklist and status report.
- `docs/04-sdd/README.md`: current SDD workspace overview.
- `docs/04-sdd/SPECS-LIST.md`: current active spec registry under new doctrine.
- `docs/04-sdd/traceability-matrix.md`: current traceability matrix under new doctrine.
- `docs/04-sdd/sdd-workflow.md`: current SDD workflow.
- `docs/04-sdd/sdd-definition-of-ready.md`: current readiness rule.
- `docs/04-sdd/sdd-definition-of-done.md`: current completion rule.
- `docs/04-sdd/feature-template.md`: current SDD feature template.
- `docs/04-sdd/specs/README.md`: current spec-folder rules.
- `docs/05-decisions/ADR-0008-documentation-doctrine-replacement.md`: superseding architecture/documentation ADR.
- `contracts/http/README.md`: current HTTP contract index.
- `contracts/events/README.md`: current event contract index.
- `contracts/http/gateway-api.md`: current gateway routing policy contract.
- `contracts/http/identity-api.md`: current Identity HTTP contract index.
- `contracts/http/partidas-api.md`: current Partidas HTTP contract index.
- `contracts/http/operaciones-sesion-api.md`: current Operaciones de Sesion HTTP contract index.
- `contracts/http/puntuaciones-api.md`: current Puntuaciones HTTP contract index.
- `contracts/events/identity-events.md`: current Identity event contract index.
- `contracts/events/partidas-events.md`: current Partidas event contract index.
- `contracts/events/operaciones-sesion-events.md`: current Operaciones de Sesion event contract index.
- `contracts/events/puntuaciones-events.md`: current Puntuaciones event contract index.

### Files To Rewrite As Current Derived Documentation

- `AGENTS.md`: OpenCode project rules aligned to `CLAUDE.md`.
- `docs/02-project-context/README.md`: current project context index.
- `docs/02-project-context/project-brief.md`: current product/architecture summary.
- `docs/02-project-context/adaptation-to-academic-brief.md`: current adaptation summary if still needed by the new SRS.
- `docs/02-project-context/srs-summary.md`: current SRS summary.
- `docs/02-project-context/business-rules.md`: current business rules summary.
- `docs/02-project-context/first-delivery-scope.md`: current scope summary if the new SRS defines a first delivery; otherwise a current-scope document stating that no active implementation slice is defined.
- `docs/02-project-context/mobile-participant-context.md`: current mobile client ownership.
- `docs/02-project-context/bdt-ranking-clarification.md`: current BDT point-based ranking clarification.
- `docs/02-project-context/glossary.md`: current glossary.
- `docs/02-project-context/domain-model-summary.md`: current domain model summary.
- `docs/02-project-context/class-design-summary.md`: current class design summary.
- `docs/02-project-context/source-priority.md`: current source priority.
- `docs/02-project-context/known-ambiguities-and-decisions.md`: current known decisions and ambiguities.
- `docs/02-project-context/design/design-index.md`: current design index.
- `docs/02-project-context/design/domain-business-rules.md`: current domain business rules by service/context.
- `docs/02-project-context/design/domain-entities-by-context.md`: current entities and contexts.
- `docs/02-project-context/design/class-design-by-layer.md`: current Clean Architecture mapping.
- `docs/02-project-context/design/service-model-impact.md`: current service model impact.
- `docs/02-project-context/design/design-patterns-catalog.md`: current patterns guidance.
- `docs/03-microservices/README.md`: current services and gateway index.
- `docs/03-microservices/source-basis.md`: current source basis.
- `docs/03-microservices/microservices-map.md`: current target services map.
- `docs/03-microservices/service-ownership.md`: current ownership map.
- `docs/03-microservices/communication-map.md`: current communication map.
- `docs/03-microservices/api-contracts.md`: current API-contract guidance.
- `docs/03-microservices/events-catalog.md`: current event catalog guidance.
- `docs/03-microservices/unresolved-decisions.md`: current unresolved decisions.
- `docs/03-microservices/services/identity-service.md`: current Identity service context.
- `docs/03-microservices/services/partidas-service.md`: current Partidas service context.
- `docs/03-microservices/services/operaciones-sesion-service.md`: current Operaciones de Sesion service context.
- `docs/03-microservices/services/puntuaciones-service.md`: current Puntuaciones service context.
- `gateway/gateway-context.md`: current mandatory gateway context.
- `frontend/frontend-context.md`: current web client context.
- `mobile/mobile-context.md`: current mobile client context.
- `services/identity-service/service-context.md`: current note that existing folder name may be migration debt if code has not moved.
- `services/team-service/service-context.md`: legacy implementation folder note, not current target service.
- `services/trivia-game-service/service-context.md`: legacy implementation folder note, not current target service.
- `services/bdt-game-service/service-context.md`: legacy implementation folder note, not current target service.
- `README.md`: high-level repository doctrine and migration-state note.
- `GUIA-LEVANTAMIENTO.md`: target doctrine and code-migration caveat.
- `GUIA-USO-AGENTE.md`: agent usage under new doctrine.

### Files To Archive Or Mark Historical

- `docs/04-sdd/specs/**`: move to `docs/04-sdd/_legacy-implementation-evidence/specs/`.
- `docs/04-sdd/SPECS-LIST.md`: move a copy to `docs/04-sdd/_legacy-implementation-evidence/SPECS-LIST.md` before rewriting current file.
- `docs/04-sdd/traceability-matrix.md`: move a copy to `docs/04-sdd/_legacy-implementation-evidence/traceability-matrix.md` before rewriting current file.
- `contracts/http/team-api.md`: archive to `contracts/http/_legacy/team-api.md` or replace with a stub that points to legacy evidence.
- `contracts/http/trivia-game-api.md`: archive to `contracts/http/_legacy/trivia-game-api.md` or replace with a stub that points to legacy evidence.
- `contracts/http/bdt-game-api.md`: archive to `contracts/http/_legacy/bdt-game-api.md` or replace with a stub that points to legacy evidence.
- `contracts/events/team-events.md`: archive to `contracts/events/_legacy/team-events.md` or replace with a stub that points to legacy evidence.
- `contracts/events/trivia-game-events.md`: archive to `contracts/events/_legacy/trivia-game-events.md` or replace with a stub that points to legacy evidence.
- `contracts/events/bdt-game-events.md`: archive to `contracts/events/_legacy/bdt-game-events.md` or replace with a stub that points to legacy evidence.

---

### Task 1: Verify New Source Baseline And Create Migration Status

**Files:**
- Read: `docs/01-project-source/srs.md`
- Read: `docs/01-project-source/diagrama-de-clases.md`
- Read: `docs/01-project-source/modelo-de-dominio.md`
- Read: `docs/01-project-source/microservicios.md`
- Read: `CLAUDE.md`
- Create: `docs/02-project-context/documentation-migration-status.md`

**Interfaces:**
- Consumes: authoritative source files and `CLAUDE.md`.
- Produces: migration status file used by all later tasks as the active migration checklist.

- [ ] **Step 1: Confirm source files exist**

Run: `Test-Path -LiteralPath "docs/01-project-source/srs.md"; Test-Path -LiteralPath "docs/01-project-source/diagrama-de-clases.md"; Test-Path -LiteralPath "docs/01-project-source/modelo-de-dominio.md"; Test-Path -LiteralPath "docs/01-project-source/microservicios.md"; Test-Path -LiteralPath "CLAUDE.md"`

Expected: five `True` lines.

- [ ] **Step 2: Read and extract doctrine bullets**

Read the five source files. Capture exact terminology for:

- service names;
- gateway role;
- BDT ranking rule;
- team ownership;
- client routing rule;
- SDD expectations.

- [ ] **Step 3: Create migration status file**

Create `docs/02-project-context/documentation-migration-status.md` with this content, replacing only the `Source snapshot` bullet details with exact wording from the source files:

```markdown
# Documentation Migration Status

## Status

Current phase: baseline captured.

## Authority

Active doctrine is derived only from:

- `docs/01-project-source/srs.md`
- `docs/01-project-source/diagrama-de-clases.md`
- `docs/01-project-source/modelo-de-dominio.md`
- `docs/01-project-source/microservicios.md`
- `CLAUDE.md`

## Source Snapshot

- Target services: `Identity`, `Partidas`, `Operaciones de Sesion`, `Puntuaciones`.
- Gateway: YARP is mandatory and is the single backend entry point for clients.
- Teams: owned by `Identity` in the target doctrine.
- BDT ranking: accumulated points from won stages, tie-break by lower accumulated time across won stages.
- SDD: old implementation SDDs are archived as legacy evidence before new specs are planned.

## Migration Checklist

| Area | State | Evidence |
|---|---|---|
| Source files | Captured | `docs/01-project-source/` and `CLAUDE.md` reviewed |
| Legacy SDD archive | Deferred to Task 2 | No files moved yet |
| Project context docs | Deferred to Task 3 | No current rewrite yet |
| Microservice and gateway docs | Deferred to Task 4 | No current rewrite yet |
| SDD workspace reset | Deferred to Task 5 | No current rewrite yet |
| ADR supersession | Deferred to Task 6 | No ADR added yet |
| Contract reset | Deferred to Task 7 | No current rewrite yet |
| Component contexts and guides | Deferred to Task 8 | No current rewrite yet |
| Consistency pass | Deferred to Task 9 | No final search yet |

## Legacy Policy

Old documentation that describes `Team Service`, `Trivia Game Service`, `BDT Game Service`, or BDT ranking by stages won can remain only when it is clearly marked as historical implementation evidence.
```

- [ ] **Step 4: Validate Task 1**

Run: `git diff --check -- "docs/02-project-context/documentation-migration-status.md"`

Expected: no output.

- [ ] **Step 5: Commit Task 1**

Run:

```powershell
git add -- "docs/02-project-context/documentation-migration-status.md"
git commit -m "Document documentation migration baseline"
```

Expected: commit succeeds with one new file.

---

### Task 2: Archive Legacy SDD Implementation Evidence

**Files:**
- Move: `docs/04-sdd/specs/` to `docs/04-sdd/_legacy-implementation-evidence/specs/`
- Copy or move: `docs/04-sdd/SPECS-LIST.md` to `docs/04-sdd/_legacy-implementation-evidence/SPECS-LIST.md`
- Copy or move: `docs/04-sdd/traceability-matrix.md` to `docs/04-sdd/_legacy-implementation-evidence/traceability-matrix.md`
- Create: `docs/04-sdd/_legacy-implementation-evidence/README.md`
- Modify: `docs/02-project-context/documentation-migration-status.md`

**Interfaces:**
- Consumes: current old SDD workspace.
- Produces: legacy evidence directory and empty path for a clean current SDD workspace in Task 5.

- [ ] **Step 1: Create legacy archive directory**

Run: `Test-Path -LiteralPath "docs/04-sdd"`

Expected: `True`.

Create directory path with PowerShell only after verifying parent exists:

```powershell
New-Item -ItemType Directory -Path "docs/04-sdd/_legacy-implementation-evidence" -Force
```

- [ ] **Step 2: Move legacy SDD content**

Move these paths:

```powershell
Move-Item -LiteralPath "docs/04-sdd/specs" -Destination "docs/04-sdd/_legacy-implementation-evidence/specs"
Move-Item -LiteralPath "docs/04-sdd/SPECS-LIST.md" -Destination "docs/04-sdd/_legacy-implementation-evidence/SPECS-LIST.md"
Move-Item -LiteralPath "docs/04-sdd/traceability-matrix.md" -Destination "docs/04-sdd/_legacy-implementation-evidence/traceability-matrix.md"
```

Expected: the original active `specs/`, `SPECS-LIST.md`, and `traceability-matrix.md` no longer exist at the top of `docs/04-sdd/`.

- [ ] **Step 3: Write legacy README**

Create `docs/04-sdd/_legacy-implementation-evidence/README.md`:

```markdown
# Legacy Implementation Evidence

This directory preserves SDDs, traceability, and evidence from the previous implementation doctrine.

## Status

Historical evidence only. Do not use these files as current planning input.

## Why This Exists

The project doctrine now uses the target service model documented in `CLAUDE.md` and the current source files under `docs/01-project-source/`.

The archived files may still help explain implemented behavior, prior tests, and migration debt, but they describe the old service decomposition and old contract assumptions.

## Rules

- Do not implement new features from this directory.
- Do not treat archived contracts, service ownership, or traceability rows as active doctrine.
- If old behavior is needed, create a new SDD under the current doctrine and cite this archive only as historical evidence.
```

- [ ] **Step 4: Update migration status**

In `docs/02-project-context/documentation-migration-status.md`, change the legacy archive row to:

```markdown
| Legacy SDD archive | Completed | `docs/04-sdd/_legacy-implementation-evidence/` created with archived specs, spec list, and traceability matrix |
```

- [ ] **Step 5: Validate Task 2**

Run: `git diff --check -- "docs/04-sdd" "docs/02-project-context/documentation-migration-status.md"`

Expected: no output.

- [ ] **Step 6: Commit Task 2**

Run:

```powershell
git add -- "docs/04-sdd" "docs/02-project-context/documentation-migration-status.md"
git commit -m "Archive legacy SDD evidence"
```

Expected: commit succeeds with moved files and legacy README.

---

### Task 3: Regenerate Project Context Documentation

**Files:**
- Modify: `docs/02-project-context/README.md`
- Modify: `docs/02-project-context/project-brief.md`
- Modify: `docs/02-project-context/adaptation-to-academic-brief.md`
- Modify: `docs/02-project-context/srs-summary.md`
- Modify: `docs/02-project-context/business-rules.md`
- Modify: `docs/02-project-context/first-delivery-scope.md`
- Modify: `docs/02-project-context/mobile-participant-context.md`
- Modify: `docs/02-project-context/bdt-ranking-clarification.md`
- Modify: `docs/02-project-context/glossary.md`
- Modify: `docs/02-project-context/domain-model-summary.md`
- Modify: `docs/02-project-context/class-design-summary.md`
- Modify: `docs/02-project-context/source-priority.md`
- Modify: `docs/02-project-context/known-ambiguities-and-decisions.md`
- Modify: `docs/02-project-context/design/design-index.md`
- Modify: `docs/02-project-context/design/domain-business-rules.md`
- Modify: `docs/02-project-context/design/domain-entities-by-context.md`
- Modify: `docs/02-project-context/design/class-design-by-layer.md`
- Modify: `docs/02-project-context/design/service-model-impact.md`
- Modify: `docs/02-project-context/design/design-patterns-catalog.md`
- Modify: `docs/02-project-context/documentation-migration-status.md`

**Interfaces:**
- Consumes: source files and Task 1 migration baseline.
- Produces: current derived context layer used by AGENTS, SDD, contracts, and service docs.

- [ ] **Step 1: Rewrite `docs/02-project-context/README.md`**

Use this structure:

```markdown
# UMBRAL Project Context

This directory contains current derived context for the target UMBRAL doctrine.

## Authority

The source of truth is `docs/01-project-source/` plus `CLAUDE.md`. Files here are derived and must be corrected when they contradict the source files.

## Target Architecture

- `Identity`
- `Partidas`
- `Operaciones de Sesion`
- `Puntuaciones`
- Mandatory YARP gateway

## Current Files

| File | Purpose |
|---|---|
| `project-brief.md` | Product and architecture summary |
| `srs-summary.md` | SRS-derived requirements summary |
| `business-rules.md` | Normalized business rules |
| `first-delivery-scope.md` | Current implementation-scope summary if defined by the source docs |
| `glossary.md` | Ubiquitous language |
| `domain-model-summary.md` | Aggregates, entities, events, and services |
| `class-design-summary.md` | Class design summary |
| `source-priority.md` | Source priority and contradiction rules |
| `known-ambiguities-and-decisions.md` | Current decisions and open questions |
| `documentation-migration-status.md` | Migration checklist |
```

- [ ] **Step 2: Rewrite project/domain summaries**

For `project-brief.md`, `srs-summary.md`, `business-rules.md`, `glossary.md`, `domain-model-summary.md`, and `class-design-summary.md`, extract current content from the source files. Every file must include a short status block:

```markdown
> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.
```

Each file must mention `Partida`, sequential `Juego`, `JuegoTrivia`, `JuegoBDT`, and the target service boundaries if those concepts appear in the source docs.

- [ ] **Step 3: Rewrite source priority and ambiguity docs**

In `source-priority.md`, set priority as:

```markdown
1. `CLAUDE.md` for operational target doctrine and repository rules.
2. `docs/01-project-source/srs.md` for requirements, rules, actors, and scope.
3. `docs/01-project-source/modelo-de-dominio.md` for domain concepts and invariants.
4. `docs/01-project-source/diagrama-de-clases.md` for tactical classes and relationships.
5. `docs/01-project-source/microservicios.md` for target service ownership.
6. Derived docs under `docs/02-project-context/`.
```

In `known-ambiguities-and-decisions.md`, include resolved decisions:

- new target service topology;
- mandatory gateway;
- teams inside Identity;
- BDT point-based ranking;
- legacy SDD archive policy.

- [ ] **Step 4: Rewrite BDT ranking clarification**

Replace old stages-won doctrine with:

```markdown
# BDT Ranking Clarification

## Decision

BDT native ranking is based on accumulated points from won stages.

Ordering:

1. Higher accumulated BDT points ranks higher.
2. If tied, lower accumulated time across won stages ranks higher.

`EtapasGanadas` may be retained as informative data but is not the primary sort key.

## Forbidden Active Assumption

Do not state that BDT ranking is primarily ordered by number of stages won in current doctrine.
```

- [ ] **Step 5: Rewrite design context files**

Ensure `design/domain-entities-by-context.md`, `design/class-design-by-layer.md`, and `design/service-model-impact.md` map the domain to `Identity`, `Partidas`, `Operaciones de Sesion`, and `Puntuaciones`. Include gateway as routing/entry-point, not domain owner.

- [ ] **Step 6: Update migration status**

Set the project context row to:

```markdown
| Project context docs | Completed | `docs/02-project-context/` regenerated for target doctrine |
```

- [ ] **Step 7: Validate Task 3**

Run: `git diff --check -- "docs/02-project-context"`

Run: `rg "BDT ranking is not based|EtapasGanadas.*primary|Team Service|Trivia Game Service|BDT Game Service" docs/02-project-context`

Expected: any hits are either in historical notes or explicitly marked obsolete. No active guidance hit remains.

- [ ] **Step 8: Commit Task 3**

Run:

```powershell
git add -- "docs/02-project-context"
git commit -m "Regenerate project context doctrine"
```

Expected: commit succeeds.

---

### Task 4: Rewrite Microservice And Gateway Documentation

**Files:**
- Modify: `docs/03-microservices/README.md`
- Modify: `docs/03-microservices/source-basis.md`
- Modify: `docs/03-microservices/microservices-map.md`
- Modify: `docs/03-microservices/service-ownership.md`
- Modify: `docs/03-microservices/communication-map.md`
- Modify: `docs/03-microservices/api-contracts.md`
- Modify: `docs/03-microservices/events-catalog.md`
- Modify: `docs/03-microservices/unresolved-decisions.md`
- Modify or create: `docs/03-microservices/services/identity-service.md`
- Create: `docs/03-microservices/services/partidas-service.md`
- Create: `docs/03-microservices/services/operaciones-sesion-service.md`
- Create: `docs/03-microservices/services/puntuaciones-service.md`
- Delete or replace with legacy pointer: `docs/03-microservices/services/team-service.md`
- Delete or replace with legacy pointer: `docs/03-microservices/services/trivia-game-service.md`
- Delete or replace with legacy pointer: `docs/03-microservices/services/bdt-game-service.md`
- Modify: `docs/02-project-context/documentation-migration-status.md`

**Interfaces:**
- Consumes: regenerated project context.
- Produces: current target service ownership and communication docs.

- [ ] **Step 1: Rewrite services map**

In `microservices-map.md`, define this table:

```markdown
| Service | Responsibility | Persistence |
|---|---|---|
| Identity | Users, Keycloak mapping, roles, permissions, governance, teams, team membership, leadership, invitations, temporary credential notification | `umbral_identity` |
| Partidas | Partida configuration, sequential Juegos, Trivia questions/options, BDT stages and expected QR text, modality, participation limits, start configuration | `umbral_partidas` |
| Operaciones de Sesion | Runtime lobby, inscriptions, convocatorias, live start, synchronization, answer/QR validation, clues, geolocation, reconnection, session SignalR | `umbral_operaciones_sesion` |
| Puntuaciones | Scoring, native rankings, consolidated ranking, audit/history projections, ranking SignalR | `umbral_puntuaciones` |
```

Add gateway note:

```markdown
The YARP gateway is mandatory but does not own domain state.
```

- [ ] **Step 2: Rewrite ownership docs**

In `service-ownership.md`, create sections for the four services. For each service, include `Owns` and `Does not own` lists using `CLAUDE.md` as the source. Explicitly state old physical services are obsolete.

- [ ] **Step 3: Rewrite communication map**

Include these rules:

- clients call gateway, not services directly;
- gateway validates Keycloak JWT and coarse route role authorization;
- services enforce functional permissions and domain rules;
- RabbitMQ carries domain events between services;
- SignalR/WebSockets are user-visible and routed through the gateway;
- no service reads another service database.

- [ ] **Step 4: Create service context files**

Create or rewrite the four current service files with this structure:

```markdown
# <Service> Service

## Status

Current target service.

## Responsibility

<responsibility from CLAUDE.md and source docs>

## Owns

- <owned concepts>

## Does Not Own

- <excluded concepts>

## Communication

- HTTP through the YARP gateway.
- RabbitMQ for cross-service domain events where required.
- SignalR/WebSockets through the gateway for user-visible updates where required.
```

For obsolete service files, either delete them or replace each with:

```markdown
# Legacy Service Context

This file path belongs to the previous implementation layout. It is not a current target service under the active doctrine.

Current target services are `Identity`, `Partidas`, `Operaciones de Sesion`, and `Puntuaciones`, behind the mandatory YARP gateway.
```

- [ ] **Step 5: Update API/event guidance**

In `api-contracts.md` and `events-catalog.md`, remove old active endpoint/event assignments and replace them with guidance for the four new services. State that concrete route/payload definitions require current SDDs and contracts.

- [ ] **Step 6: Update migration status**

Set the microservice row to:

```markdown
| Microservice and gateway docs | Completed | `docs/03-microservices/` rewritten for target services and gateway |
```

- [ ] **Step 7: Validate Task 4**

Run: `git diff --check -- "docs/03-microservices" "docs/02-project-context/documentation-migration-status.md"`

Run: `rg "Team Service|Trivia Game Service|BDT Game Service" docs/03-microservices`

Expected: any hits are only in explicit legacy-obsolete notes.

- [ ] **Step 8: Commit Task 4**

Run:

```powershell
git add -- "docs/03-microservices" "docs/02-project-context/documentation-migration-status.md"
git commit -m "Rewrite service doctrine documentation"
```

Expected: commit succeeds.

---

### Task 5: Recreate Current SDD Workspace

**Files:**
- Create: `docs/04-sdd/README.md`
- Create: `docs/04-sdd/SPECS-LIST.md`
- Create: `docs/04-sdd/traceability-matrix.md`
- Modify: `docs/04-sdd/sdd-workflow.md`
- Modify: `docs/04-sdd/sdd-definition-of-ready.md`
- Modify: `docs/04-sdd/sdd-definition-of-done.md`
- Modify: `docs/04-sdd/feature-template.md`
- Create: `docs/04-sdd/specs/README.md`
- Modify: `docs/02-project-context/documentation-migration-status.md`

**Interfaces:**
- Consumes: legacy archive from Task 2 and service doctrine from Task 4.
- Produces: current SDD workspace for future feature specs.

- [ ] **Step 1: Recreate current specs directory**

Run:

```powershell
New-Item -ItemType Directory -Path "docs/04-sdd/specs" -Force
```

- [ ] **Step 2: Write SDD overview**

Create `docs/04-sdd/README.md`:

```markdown
# Current SDD Workspace

This directory is the active SDD workspace for the current UMBRAL doctrine.

## Current Doctrine

- Services: `Identity`, `Partidas`, `Operaciones de Sesion`, `Puntuaciones`.
- Gateway: mandatory YARP entry point for clients.
- Source authority: `docs/01-project-source/` and `CLAUDE.md`.

## Legacy Evidence

Previous implementation SDDs are archived in `_legacy-implementation-evidence/` and are not current planning input.

## Rule

Every new feature requires a regenerated SDD folder under `specs/` before implementation.
```

- [ ] **Step 3: Write clean specs list**

Create `docs/04-sdd/SPECS-LIST.md`:

```markdown
# Active Specs List

No active current-doctrine implementation specs are registered yet.

## Rule

Add a spec row only after the feature has been selected from the current source documents and assigned to one of the target services.

| Feature | Owning service | Client target | Actor | SDD folder | Status |
|---|---|---|---|---|---|
```

- [ ] **Step 4: Write clean traceability matrix**

Create `docs/04-sdd/traceability-matrix.md`:

```markdown
# Current Traceability Matrix

No current-doctrine implementation features have been traced yet.

## Rule

Traceability rows must reference the current source documents, target service ownership, current contracts, tests, and acceptance evidence.

| Feature | Requirement | Owning service | Supporting services | SDD folder | Contracts | Status |
|---|---|---|---|---|---|---|
```

- [ ] **Step 5: Rewrite workflow and definitions**

In workflow/definition/template files, replace old service names with the target services. Include gateway-aware contract questions:

```markdown
- Does the feature use HTTP through the gateway?
- Does it require RabbitMQ events?
- Does it require SignalR/WebSockets through the gateway?
- Which target service owns the command/query?
```

- [ ] **Step 6: Write specs README**

Create `docs/04-sdd/specs/README.md`:

```markdown
# Active Current-Doctrine Specs

Only specs listed in `../SPECS-LIST.md` are active.

Do not implement from `_legacy-implementation-evidence/`.

Each active spec must contain:

- `spec.md`
- `design.md`
- `tasks.md`
- `acceptance.md`
```

- [ ] **Step 7: Update migration status**

Set the SDD row to:

```markdown
| SDD workspace reset | Completed | `docs/04-sdd/` recreated as current-doctrine workspace |
```

- [ ] **Step 8: Validate Task 5**

Run: `git diff --check -- "docs/04-sdd" "docs/02-project-context/documentation-migration-status.md"`

Run: `rg "Team Service|Trivia Game Service|BDT Game Service" docs/04-sdd --glob "!_legacy-implementation-evidence/**"`

Expected: no active hits.

- [ ] **Step 9: Commit Task 5**

Run:

```powershell
git add -- "docs/04-sdd" "docs/02-project-context/documentation-migration-status.md"
git commit -m "Reset SDD workspace for new doctrine"
```

Expected: commit succeeds.

---

### Task 6: Add Superseding ADR And Mark Conflicts

**Files:**
- Create: `docs/05-decisions/ADR-0008-documentation-doctrine-replacement.md`
- Modify: `docs/05-decisions/ADR-0002-service-per-business-capability.md`
- Modify: `docs/05-decisions/ADR-0006-four-service-topology.md`
- Modify if needed: `docs/05-decisions/ADR-0007-adapted-academic-scope-mobile-and-bdt.md`
- Modify: `docs/02-project-context/documentation-migration-status.md`

**Interfaces:**
- Consumes: current doctrine and archived legacy policy.
- Produces: formal decision record that supersedes old architecture decisions.

- [ ] **Step 1: Create ADR-0008**

Create `docs/05-decisions/ADR-0008-documentation-doctrine-replacement.md`:

```markdown
# ADR-0008 - Documentation Doctrine Replacement

## Status

Accepted

## Context

The project documentation has been updated with a new target doctrine. The repository still contains documentation and implementation evidence from an older service decomposition.

## Decision

Active documentation is regenerated from `docs/01-project-source/` and `CLAUDE.md`.

The target backend services are:

- `Identity`
- `Partidas`
- `Operaciones de Sesion`
- `Puntuaciones`

The YARP gateway is mandatory and is the single backend entry point for web and mobile clients.

The previous active service doctrine using `Team Service`, `Trivia Game Service`, and `BDT Game Service` is superseded.

BDT native ranking is point-based: accumulated points from won stages, tie-break by lower accumulated time across won stages.

Old SDDs and contracts may be preserved only as historical implementation evidence.

## Supersedes

- `ADR-0002-service-per-business-capability.md` where it conflicts with this service model.
- `ADR-0006-four-service-topology.md` where it defines the old four-service topology.
- Any previous documentation statement that rejects point-based BDT ranking as active doctrine.

## Consequences

- Active documentation must not direct new work to the old physical services.
- New contracts must be organized around the target services and gateway.
- New SDDs must be created under the current doctrine.
- Existing code migration is separate future work.
```

- [ ] **Step 2: Mark ADR-0002 superseded**

At the top of `ADR-0002-service-per-business-capability.md`, add:

```markdown
> Superseded where conflicting by `ADR-0008-documentation-doctrine-replacement.md`.
```

- [ ] **Step 3: Mark ADR-0006 superseded**

At the top of `ADR-0006-four-service-topology.md`, add:

```markdown
> Superseded by `ADR-0008-documentation-doctrine-replacement.md`. This ADR is retained as historical decision context for the previous service topology.
```

- [ ] **Step 4: Review ADR-0007**

If `ADR-0007` contradicts the new doctrine, add a top note:

```markdown
> Partially superseded by `ADR-0008-documentation-doctrine-replacement.md` for service topology and BDT ranking. Client split remains valid where consistent with current source documents.
```

- [ ] **Step 5: Update migration status**

Set the ADR row to:

```markdown
| ADR supersession | Completed | `ADR-0008-documentation-doctrine-replacement.md` added and conflicting ADRs marked |
```

- [ ] **Step 6: Validate Task 6**

Run: `git diff --check -- "docs/05-decisions" "docs/02-project-context/documentation-migration-status.md"`

Expected: no output.

- [ ] **Step 7: Commit Task 6**

Run:

```powershell
git add -- "docs/05-decisions" "docs/02-project-context/documentation-migration-status.md"
git commit -m "Record doctrine replacement ADR"
```

Expected: commit succeeds.

---

### Task 7: Reset Active Contracts

**Files:**
- Create: `contracts/http/_legacy/`
- Create: `contracts/events/_legacy/`
- Move: old `team`, `trivia-game`, and `bdt-game` contracts to legacy directories.
- Modify: `contracts/http/identity-api.md`
- Create: `contracts/http/README.md`
- Create: `contracts/http/gateway-api.md`
- Create: `contracts/http/partidas-api.md`
- Create: `contracts/http/operaciones-sesion-api.md`
- Create: `contracts/http/puntuaciones-api.md`
- Modify: `contracts/events/identity-events.md`
- Create: `contracts/events/README.md`
- Create: `contracts/events/partidas-events.md`
- Create: `contracts/events/operaciones-sesion-events.md`
- Create: `contracts/events/puntuaciones-events.md`
- Modify: `docs/02-project-context/documentation-migration-status.md`

**Interfaces:**
- Consumes: current service docs and ADR-0008.
- Produces: active contract index for current target services.

- [ ] **Step 1: Create legacy contract directories**

Run:

```powershell
New-Item -ItemType Directory -Path "contracts/http/_legacy" -Force
New-Item -ItemType Directory -Path "contracts/events/_legacy" -Force
```

- [ ] **Step 2: Move old service contracts**

Run:

```powershell
Move-Item -LiteralPath "contracts/http/team-api.md" -Destination "contracts/http/_legacy/team-api.md"
Move-Item -LiteralPath "contracts/http/trivia-game-api.md" -Destination "contracts/http/_legacy/trivia-game-api.md"
Move-Item -LiteralPath "contracts/http/bdt-game-api.md" -Destination "contracts/http/_legacy/bdt-game-api.md"
Move-Item -LiteralPath "contracts/events/team-events.md" -Destination "contracts/events/_legacy/team-events.md"
Move-Item -LiteralPath "contracts/events/trivia-game-events.md" -Destination "contracts/events/_legacy/trivia-game-events.md"
Move-Item -LiteralPath "contracts/events/bdt-game-events.md" -Destination "contracts/events/_legacy/bdt-game-events.md"
```

- [ ] **Step 3: Write HTTP contract README**

Create `contracts/http/README.md`:

```markdown
# HTTP Contracts

Active HTTP contracts are organized around the current target services and the mandatory YARP gateway.

## Active Files

- `gateway-api.md`
- `identity-api.md`
- `partidas-api.md`
- `operaciones-sesion-api.md`
- `puntuaciones-api.md`

## Rule

Clients call the gateway, not services directly. Concrete endpoints are documented only after a current-doctrine SDD defines them.

## Legacy

Previous service contracts are archived in `_legacy/` as implementation evidence.
```

- [ ] **Step 4: Write active HTTP contract indexes**

For each active HTTP contract, write sections:

```markdown
# <Service> HTTP Contract

## Status

Current contract index. Concrete endpoints require a current-doctrine SDD before implementation.

## Access Path

Requests enter through the YARP gateway.

## Owned Capabilities

- <capabilities from service ownership docs>

## Endpoint Registry

| Capability | Method | Gateway path | Owning service | Status |
|---|---|---|---|---|
```

For `gateway-api.md`, include route families:

```markdown
| Gateway route family | Target service |
|---|---|
| `/api/identity/*` | Identity |
| `/api/partidas/*` | Partidas |
| `/api/operaciones-sesion/*` | Operaciones de Sesion |
| `/api/puntuaciones/*` | Puntuaciones |
| `/hubs/*` | Gateway-routed SignalR/WebSockets |
```

- [ ] **Step 5: Write event contract README and indexes**

Create `contracts/events/README.md` with the same active/legacy policy.

For active event files, write:

```markdown
# <Service> Events

## Status

Current event contract index. Concrete payloads require a current-doctrine SDD before implementation.

## Publisher

`<Service>`

## Event Registry

| Event | Trigger | Consumers | Status |
|---|---|---|---|
```

- [ ] **Step 6: Update migration status**

Set the contract row to:

```markdown
| Contract reset | Completed | Active contracts reset around gateway and target services; old contracts archived in `_legacy/` |
```

- [ ] **Step 7: Validate Task 7**

Run: `git diff --check -- "contracts" "docs/02-project-context/documentation-migration-status.md"`

Run: `rg "Team Service|Trivia Game Service|BDT Game Service" contracts --glob "!**/_legacy/**"`

Expected: no active hits.

- [ ] **Step 8: Commit Task 7**

Run:

```powershell
git add -- "contracts" "docs/02-project-context/documentation-migration-status.md"
git commit -m "Reset contracts for target services"
```

Expected: commit succeeds.

---

### Task 8: Align Root, Client, Gateway, Service Context, And Guide Docs

**Files:**
- Modify: `AGENTS.md`
- Modify: `README.md`
- Modify: `frontend/frontend-context.md`
- Modify: `mobile/mobile-context.md`
- Modify: `gateway/gateway-context.md`
- Modify: `services/identity-service/service-context.md`
- Modify: `services/team-service/service-context.md`
- Modify: `services/trivia-game-service/service-context.md`
- Modify: `services/bdt-game-service/service-context.md`
- Modify: `GUIA-LEVANTAMIENTO.md`
- Modify: `GUIA-USO-AGENTE.md`
- Modify: `docs/02-project-context/documentation-migration-status.md`

**Interfaces:**
- Consumes: current project context, service docs, contracts, and ADR-0008.
- Produces: top-level guidance that future agents and users read first.

- [ ] **Step 1: Rewrite AGENTS.md doctrine sections**

Update `AGENTS.md` to match `CLAUDE.md`:

- project identity;
- client split;
- source hierarchy;
- target service topology;
- mandatory gateway;
- BDT ranking rule;
- legacy evidence rule;
- no code migration claim.

Include this explicit note:

```markdown
The repository may still contain folders from the previous implementation layout. Those folders are migration debt, not active doctrine.
```

- [ ] **Step 2: Update frontend/mobile/gateway contexts**

Ensure:

- frontend context says web calls gateway and serves `Administrador`/`Operador`;
- mobile context says mobile calls gateway and serves `Participante`/`Lider de equipo` acting as participant;
- gateway context says gateway is mandatory YARP, validates JWT/coarse roles, and owns no domain logic.

- [ ] **Step 3: Update service-context files under legacy folders**

For old folder paths that no longer match target services, replace active doctrine with:

```markdown
# Legacy Implementation Folder Context

This folder belongs to the previous implementation layout. It may contain useful code and tests, but it is not a current target service boundary.

Current target services are `Identity`, `Partidas`, `Operaciones de Sesion`, and `Puntuaciones`, behind the mandatory YARP gateway.

Use current documentation under `docs/02-project-context/`, `docs/03-microservices/`, and `contracts/` before planning new work.
```

For `services/identity-service/service-context.md`, state one of two explicit outcomes after reading the source files: either the folder still maps to the current `Identity` service, or it is a legacy implementation folder that needs future code migration.

- [ ] **Step 4: Update README and guides**

Add a migration-state section to each guide that references `docs/02-project-context/documentation-migration-status.md` and says documentation doctrine has changed before code migration.

- [ ] **Step 5: Update migration status**

Set context/guides row to:

```markdown
| Component contexts and guides | Completed | Root, client, gateway, service-context, and guide docs aligned to current doctrine |
```

- [ ] **Step 6: Validate Task 8**

Run: `git diff --check -- "AGENTS.md" "README.md" "frontend/frontend-context.md" "mobile/mobile-context.md" "gateway/gateway-context.md" "services" "GUIA-LEVANTAMIENTO.md" "GUIA-USO-AGENTE.md" "docs/02-project-context/documentation-migration-status.md"`

Run: `rg "directly to.*service|client.*service directly|Team Service|Trivia Game Service|BDT Game Service" AGENTS.md README.md frontend mobile gateway services GUIA-LEVANTAMIENTO.md GUIA-USO-AGENTE.md`

Expected: hits are absent or explicitly marked legacy/migration debt.

- [ ] **Step 7: Commit Task 8**

Run:

```powershell
git add -- "AGENTS.md" "README.md" "frontend/frontend-context.md" "mobile/mobile-context.md" "gateway/gateway-context.md" "services" "GUIA-LEVANTAMIENTO.md" "GUIA-USO-AGENTE.md" "docs/02-project-context/documentation-migration-status.md"
git commit -m "Align repository guidance with new doctrine"
```

Expected: commit succeeds.

---

### Task 9: Final Consistency Pass

**Files:**
- Modify: files found by searches from previous tasks.
- Modify: `docs/02-project-context/documentation-migration-status.md`

**Interfaces:**
- Consumes: all current documentation updates.
- Produces: final consistency report and ready-to-execute documentation state.

- [ ] **Step 1: Run global obsolete-doctrine search**

Run:

```powershell
rg "Team Service|Trivia Game Service|BDT Game Service|Treasure Hunt Service|BDT ranking is not based|ranking.*stages won|direct client-to-service|access code" AGENTS.md CLAUDE.md README.md docs contracts frontend mobile gateway services GUIA-LEVANTAMIENTO.md GUIA-USO-AGENTE.md
```

Expected: remaining hits are only inside:

- `_legacy-implementation-evidence/`;
- `contracts/**/_legacy/`;
- superseded ADRs;
- explicit migration-debt notes.

- [ ] **Step 2: Fix active contradictions**

For every active-document hit that is not legacy/historical, edit the file so it states current doctrine. Use these replacements:

- `Team Service` as active owner -> `Identity` owns teams and membership.
- `Trivia Game Service` as active owner -> split between `Partidas`, `Operaciones de Sesion`, and `Puntuaciones` according to configuration/runtime/scoring responsibility.
- `BDT Game Service` as active owner -> split between `Partidas`, `Operaciones de Sesion`, and `Puntuaciones` according to configuration/runtime/scoring responsibility.
- BDT stages-won ranking -> accumulated points from won stages, time tie-break.
- direct client-to-service routing -> gateway-mediated access.

- [ ] **Step 3: Update final migration status**

In `documentation-migration-status.md`, set:

```markdown
Current phase: documentation doctrine replacement complete.
```

Set final checklist row:

```markdown
| Consistency pass | Completed | Obsolete-doctrine search performed; remaining hits are legacy, superseded, or migration-debt notes |
```

Add:

```markdown
## Remaining Work Outside This Migration

- Code/service-folder migration to target service names and databases.
- Runtime gateway and route implementation review.
- Regeneration of feature SDDs for the next selected implementation slice.
```

- [ ] **Step 4: Run final validation**

Run: `git diff --check`

Run: `git status --short`

Expected:

- `git diff --check` has no output.
- `git status --short` shows only intended documentation changes and any pre-existing unrelated user changes.

- [ ] **Step 5: Commit Task 9**

Run:

```powershell
git add -- "AGENTS.md" "README.md" "docs" "contracts" "frontend/frontend-context.md" "mobile/mobile-context.md" "gateway/gateway-context.md" "services" "GUIA-LEVANTAMIENTO.md" "GUIA-USO-AGENTE.md"
git commit -m "Complete documentation doctrine migration"
```

Expected: commit succeeds with only documentation changes.

---

## Self-Review Notes

Spec coverage:

- Source replacement and authority: Task 1.
- Legacy SDD archive: Task 2.
- Derived project context regeneration: Task 3.
- Microservices/gateway docs: Task 4.
- Fresh current SDD workspace: Task 5.
- ADR supersession: Task 6.
- Contract reset: Task 7.
- Root/context/guide alignment: Task 8.
- Text consistency validation: Task 9.

Placeholder scan:

- The plan avoids unresolved-marker wording and vague instructions.
- Concrete endpoint payloads are intentionally not specified because the design forbids inventing contract details absent from the source docs.

Type/name consistency:

- Target service names are consistently `Identity`, `Partidas`, `Operaciones de Sesion`, and `Puntuaciones`.
- Legacy archive paths are consistently `docs/04-sdd/_legacy-implementation-evidence/` and `contracts/**/_legacy/`.
