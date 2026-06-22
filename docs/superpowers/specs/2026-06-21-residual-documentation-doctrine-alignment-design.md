# Residual Documentation Doctrine Alignment Design

Date: 2026-06-21

## Purpose

Extend the completed documentation-doctrine migration so that **every remaining documentation artifact in the repository** teaches the current target doctrine, not the superseded one. The prior migration (branch `feature/docs-migration`, commits `cdea9e2..2b63c43`) regenerated the `docs/` tree, root context files, contracts, and ADRs. It deliberately stopped at "collateral documentation" and left documentation embedded elsewhere — agent-steering skills, in-code READMEs, and code comments — still describing the old service model and ranking rule.

This is a **documentation-only, behavior-safe** pass. It does not modify runtime code, user-facing strings, test assertions, contracts, Docker Compose, `.env`, or database schemas.

## Relationship To The Prior Migration

This is a continuation of the same effort and shares its doctrine and conventions:

- Same authoritative inputs and target service model (`Identity`, `Partidas`, `Operaciones de Sesion`, `Puntuaciones` behind a mandatory YARP gateway).
- Same legacy-evidence and negation/historical-framing conventions established and adjudicated in the prior final review.
- Same branch (`feature/docs-migration`) and same progress ledger (`.git/sdd/progress.md`), under a new "Residual doc alignment" section.

## Authoritative Inputs (Doctrine Source)

- `CLAUDE.md`
- `docs/01-project-source/{srs.md,diagrama-de-clases.md,modelo-de-dominio.md,microservicios.md}`
- The already-migrated derived docs under `docs/02-project-context/`, `docs/03-microservices/`, `docs/05-decisions/`, and `contracts/`.

These remain untouched and define the doctrine that the in-scope documents are aligned to.

## Superseded Doctrine To Remove From Active Documentation

- Old physical services as active targets: `Team Service`, `Trivia Game Service`, `BDT Game Service`, `Treasure Hunt Service` (also `Audit Service`, `Scoring Service`, `Notification Service`, `Trivia Service`).
- Old BDT ranking: ordering by number of stages won (`EtapasGanadas` as the primary sort key, `rankingKey = (EtapasGanadas DESC, TiempoAcumuladoEtapasGanadas ASC)`).
- Team access code / `codigo de acceso` as an active join mechanism.
- Obsolete aggregates `PartidaTrivia` / `PartidaBDT`, and the generic "Trivia form" / "formulario de Trivia".

**Not obsolete — do not "fix":** `Identity Service` (Identity is a current target service; the in-code `services/identity-service/**/*.md` design docs that say "Identity Service" are correct and need only a doctrine sanity check, not rewrites).

## Scope

### In Scope (Tier 1 — documentation)

**Group A — Agent-steering `.claude/` files.** The UMBRAL-specific skills, commands, and agents that future agents load and follow. Confirmed obsolete-doctrine carriers (8):

- `.claude/skills/ddd-modeling/SKILL.md` — old ranking formula `rankingKey = (EtapasGanadas DESC, ...)`, "ranking must be derived from stages won", `PartidaTrivia`/`PartidaBDT` aggregates. **Highest priority.**
- `.claude/skills/umbral-context/SKILL.md` — "the only valid backend services are Identity Service, Team Service, Trivia Game Service and BDT Game Service".
- `.claude/skills/sdd-workflow/SKILL.md` — old four-service owning-service list.
- `.claude/skills/websocket-signalr/SKILL.md` — old service list for real-time owners.
- `.claude/skills/rabbitmq-events/SKILL.md` — "BDT ranking is based on stages won".
- `.claude/skills/contract-design/SKILL.md` — old non-service negation list.
- `.claude/commands/{create-feature-sdd,implement-task,plan-feature,review-boundaries,review-feature,update-traceability}.md` — owning-service selection lists naming the old four services.

Also in scope for a doctrine sanity check (scanned clean for the obsolete terms, but must be confirmed consistent): `.claude/skills/{cqrs-mediatr,efcore-postgres,testing,react-native-mobile}/SKILL.md` and `.claude/agents/{architect,backend,devops,frontend,mobile,qa}.md`.

**Group B — In-code markdown docs.** Confirmed/likely targets:

- `mobile/src/features/trivia/live/README.md` — BDT ranking description (must state point-based; `EtapasGanadas` informative only).
- `services/trivia-game-service/AGENTS.md` — legacy-folder doctrine doc; align or mark as legacy folder.
- `mobile/src/features/teams/CreateTeamScreen.md` — check for access-code doctrine.
- `frontend/README.md`, `mobile/README.md`, `infra/keycloak/README.md` — scan and align any architecture/domain claims.
- `services/identity-service/**/*.md` and `services/trivia-game-service/service-context.md` — sanity check only; "Identity Service" is valid, and the service-context files were already migrated.

**Group C — Code comments / JSDoc / CSS comments in `frontend/src` and `mobile/src`.** Exactly these 9 comment targets:

- `frontend/src/styles/components.css:388` — `/* Question editor cards (Trivia form) */`.
- `mobile/src/features/trivia/live/liveTriviaTypes.ts:15,29` — JSDoc naming "Trivia Game Service" and the old BDT ordering.
- `mobile/src/features/bdt/useBdtActiveStage.js:19` — comment referencing the `PartidaBDTIniciada` event (comment text only).
- `mobile/src/features/bdt/ranking/bdtRankingTypes.ts:10,11,15,34` — JSDoc describing the old stages-won ranking and "BDT Game Service".
- `mobile/src/features/bdt/ranking/BdtRankingScreenContainer.tsx:11` — comment "BDT Game Service, orden por etapas/tiempo".

### Out Of Scope (deliberately deferred)

- **Tier 2 — user-facing string literals**: error messages, JSX headings/subtitles, and team `codigo de acceso` UI copy (e.g. `CreateTriviaFormPage.tsx:95,134`, `CreateTriviaGamePage.tsx:387`, `CreateBdtGamePage.tsx:496`, `PublishedBdtGamesPage.tsx:268,281`, `TriviaScoreScreen.tsx:37`, `joinTeamFlow.js:7`, `leaveTeamScreenContent.js:8`, `JoinTeamScreen.tsx:39`).
- **Tier 3 — code structure and behavior**: identifiers, file/component names, the event-name string `subscribe("PartidaBDTIniciada")`, the join-by-code flow, backend `.cs` (~437 hits), and test names/assertions (`CreateTriviaFormPage.test.tsx`, `PublishedBdtGamesPage.test.tsx`).
- The already-migrated `docs/` tree, root context files, `contracts/`, `AGENTS.md`.
- `opencode.json`, `.opencode/`, `docs/01-project-source/`, generic vendored `.claude` skills (`impeccable`, `craft`, `critique`, `polish`, `shape`), and `.superpowers/sdd/` scratch.

## Editing Conventions

- **Doctrine-forward + migration-debt note.** Rewrite documentation to teach the current doctrine. Where the adjacent runtime code still implements the old rule (notably BDT ranking in `bdtRankingTypes.ts` / `BdtRankingScreenContainer.tsx`), append a one-line migration-debt note, e.g. `// migration debt: code still orders by stages won; see docs/02-project-context/documentation-migration-status.md`. The doc teaches the correct rule and stays honest about the code lag.
- **Comments only in code.** In `frontend/src` and `mobile/src`, edit only comment/JSDoc/CSS-comment text. Never edit string literals, JSX text, identifiers, event-name strings, or test code. Any non-comment line in the Group C diff is a defect.
- **Negation and historical framing are correct**, not defects: "the former Team Service", "obsolete (superseded)", legacy-folder pointers, and superseded-ADR notes all stay.
- **No invention.** Do not introduce concrete endpoints, queue names, routing keys, or SignalR event shapes beyond what `CLAUDE.md` and the source docs justify.
- **`Identity Service` is valid.** Do not rewrite it; only confirm surrounding text does not imply the old four-way split.

## Approach

Subagent-driven development (the same process used for the prior migration), executed on the current session against the existing branch.

### Task Slicing

1. **Task A — `.claude/` agent-steering docs.** Highest leverage. Correct `ddd-modeling` (ranking + aggregates), `umbral-context`, `sdd-workflow`, `websocket-signalr`, `rabbitmq-events`, `contract-design`, and the 6 commands; doctrine sanity-check the remaining UMBRAL skills and agents.
2. **Task B — in-code markdown docs.** Align the `mobile/src` READMEs, `services/trivia-game-service/AGENTS.md`, team screen doc, and the frontend/mobile/infra READMEs; sanity-check the `services/identity-service/**/*.md` design docs.
3. **Task C — `frontend/src` + `mobile/src` comments/JSDoc.** Apply the 9 comment edits with the doctrine-forward + debt-note rule; strictly comments-only.

Each task: implementer subagent → per-task review (spec compliance + code quality) → fix loop until clean → ledger entry. After all three: a **final multi-agent whole-branch review** scoped to the residual-alignment commits, plus the verification sweep below.

## Verification Strategy

- `rg` for the superseded-doctrine terms across the in-scope surfaces (`.claude/` UMBRAL files, in-code markdown, `frontend/src` + `mobile/src` comments). Every residual hit must be a negation, historical/legacy framing, a migration-debt note, or an explicitly out-of-scope Tier-2/Tier-3 string/identifier/test line.
- `git diff --check` clean.
- The changeset contains **no behavior files**: no `.cs`, no changed string-literal/JSX/test lines, no identifier or event-name changes. `opencode.json`, `.opencode/`, and `docs/01-project-source/` are untouched.
- Confirm `ddd-modeling`, `umbral-context`, `sdd-workflow`, and the commands now teach the four target services and point-based BDT ranking.
- Append a "Residual doc alignment" section to `.git/sdd/progress.md`.

## Branch And Ledger

- **Branch:** continue on `feature/docs-migration`. New commits, no rebase of prior migration commits.
- **Ledger:** `.git/sdd/progress.md`, new section recording Tasks A/B/C and the final review.

## Risks And Mitigations

| Risk | Mitigation |
|---|---|
| Accidentally editing a user-facing string or identifier. | Comments-only rule for Group C; diff review flags any non-comment line as a defect. |
| Doc/code contradiction after rewriting ranking comments. | Doctrine-forward + migration-debt note convention keeps docs correct and honest about code lag. |
| Over-reaching into vendored `.claude` skills. | Explicit UMBRAL-only `.claude` target list; generic skills (`impeccable` et al.) and `.opencode/` excluded. |
| Treating valid `Identity Service` mentions as defects. | Spec explicitly marks `Identity Service` as a current target service requiring no rewrite. |
| Future agents keep loading old doctrine. | Group A prioritized first; `ddd-modeling` ranking and `umbral-context` service list are the top fixes. |

## Out Of Scope / Remaining Future Work

- Tier 2 user-facing copy alignment (UI strings, error messages, team join copy).
- Tier 3 code/service-folder migration to target service names and databases, removal of the join-by-code flow, and the corresponding test updates — tracked in `docs/02-project-context/documentation-migration-status.md`.

## Approval Status

Design approved by the user on 2026-06-21 (scope boundary = Tier-1 documentation; include `.claude/` agent-steering docs; doctrine-forward + migration-debt note; three-task slicing on `feature/docs-migration`; comments-only in code). Ready for implementation-plan writing.
