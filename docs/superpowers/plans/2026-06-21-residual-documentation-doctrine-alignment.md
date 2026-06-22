# Residual Documentation Doctrine Alignment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Align all remaining documentation outside the already-migrated `docs/` tree — the agent-steering `.claude/` skills/commands, in-code READMEs, and `frontend/src`/`mobile/src` code comments — to the current UMBRAL doctrine, without changing any runtime behavior.

**Architecture:** Documentation-only continuation of the `feature/docs-migration` work. Three tasks by surface: (A) `.claude/` agent-steering docs, (B) in-code markdown docs, (C) `frontend/src`/`mobile/src` comments/JSDoc. Each task edits documentation text only, verifies with `rg`/`git diff --check` and a scope guard, then commits. A final whole-branch review follows.

**Tech Stack:** Markdown, JSDoc/CSS comments, Git, `rg`. No application-code, test, contract, or schema changes.

## Global Constraints

- **Documentation-only / behavior-safe.** Do not modify runtime code, user-facing string literals, JSX text, identifiers, file/component/event names, test names/assertions, contracts, Docker Compose, `.env`, or DB schemas.
- **Target service model (verbatim):** `Identity`, `Partidas`, `Operaciones de Sesion`, `Puntuaciones`, behind a mandatory YARP gateway (single entry point including real-time).
- **Obsolete as ACTIVE services (only allowed as negation/historical/legacy):** `Team Service`, `Trivia Game Service`, `BDT Game Service`, `Treasure Hunt Service`, `Audit Service`, `Scoring Service`, `Notification Service`, `Trivia Service`.
- **BDT native ranking (current):** order by accumulated points = sum of the `Puntaje` of the **won stages**, descending; tie-break by lowest accumulated time of the **won stages only**. `EtapasGanadas` (count of stages won) is informative data only, never the sort key.
- **`Identity Service` is NOT obsolete.** Identity is a current target service; do not rewrite `Identity Service` mentions in `services/identity-service/**/*.md` — only confirm surrounding text does not imply the old four-way split.
- **Canonical old→new term mapping** (from `CLAUDE.md`): `PartidaTrivia`→`JuegoTrivia`; `PartidaBDT`→`JuegoBDT`; `CompetidorTrivia`→`ParticipanteTrivia`; `ExploradorBDT`→`ParticipanteBDT`; `FormularioTrivia`→removed (Trivia `Pregunta` belongs directly to `JuegoTrivia`, created with the game); `CodigoAcceso` / team access code→removed (members join only via `InvitacionEquipo`).
- **Canonical source files** under `docs/01-project-source/`: `srs.md`, `modelo-de-dominio.md`, `diagrama-de-clases.md`, `microservicios.md`, `enunciado-proyecto.md`. There is **no** `historias de usuario.md` (HUs live in `srs.md`); space-named references are broken links to fix.
- **Doctrine-forward + migration-debt note.** Rewrite docs to teach current doctrine; where the adjacent runtime code still implements the old rule, append a one-line migration-debt note pointing to `docs/02-project-context/documentation-migration-status.md`. Never silently make a comment contradict the code beneath it.
- **No invention.** Do not introduce concrete endpoints, queue names, routing keys, or SignalR event shapes beyond what `CLAUDE.md` and the source/migrated docs justify. When exact concept/event names are needed, take them from the referenced current docs (`docs/02-project-context/bdt-ranking-clarification.md`, `domain-model-summary.md`, `design/domain-entities-by-context.md`).
- **Comments-only in code.** In `frontend/src`/`mobile/src`, edit only comment/JSDoc/CSS-comment text. Any non-comment line in the diff is a defect.
- **Untouched:** `opencode.json`, `.opencode/`, `docs/01-project-source/`, the already-migrated `docs/` tree + root context files + `contracts/` + `AGENTS.md`, generic vendored `.claude` skills (`impeccable`, `craft`, `critique`, `polish`, `shape`), `.superpowers/sdd/` scratch.
- **Branch & ledger:** continue on `feature/docs-migration`; record progress in `.git/sdd/progress.md` under a "Residual doc alignment" section.

---

## File Structure

**Task A — `.claude/` (edit):**
- `.claude/skills/ddd-modeling/SKILL.md` — BDT ranking section + Core concepts + participant concepts (heaviest).
- `.claude/skills/umbral-context/SKILL.md` — service list + source-file references.
- `.claude/skills/sdd-workflow/SKILL.md` — owning-service list + source-file references.
- `.claude/skills/websocket-signalr/SKILL.md` — real-time owning-service list.
- `.claude/skills/rabbitmq-events/SKILL.md` — BDT ranking/event guidance.
- `.claude/skills/contract-design/SKILL.md` — obsolete-service negation list + `<service>` paths.
- `.claude/commands/{create-feature-sdd,implement-task,plan-feature,review-boundaries,update-traceability}.md` — owning-service lists (and `create-feature-sdd` source-file ref).
- `.claude/commands/review-feature.md` — confirm its `Treasure Hunt Service` mention is a negation (likely no change).

**Task A — `.claude/` (sanity-check only, edit only if a genuine defect):**
- `.claude/skills/{cqrs-mediatr,efcore-postgres,testing,react-native-mobile}/SKILL.md`, `.claude/agents/{architect,backend,devops,frontend,mobile,qa}.md`.

**Task B — in-code markdown (verify; edit only genuine defects):**
- `mobile/README.md`, `frontend/README.md`, `infra/keycloak/README.md`, `services/identity-service/**/*.md`.
- Already-correct (confirm, no change expected): `mobile/src/features/trivia/live/README.md`, `mobile/src/features/teams/CreateTeamScreen.md`, `services/trivia-game-service/AGENTS.md`, `services/*/service-context.md`.

**Task C — code comments (edit):**
- `mobile/src/features/bdt/ranking/bdtRankingTypes.ts` — ranking-rule JSDoc (heaviest).
- `mobile/src/features/bdt/ranking/BdtRankingScreenContainer.tsx` — TODO comment.
- `mobile/src/features/trivia/live/liveTriviaTypes.ts` — service name + BDT comparison line.
- `mobile/src/features/bdt/useBdtActiveStage.js` — event-name comment (debt note).
- `frontend/src/styles/components.css` — `Trivia form` comment.

---

### Task A: Align `.claude/` Agent-Steering Docs

**Files:** the Task A edit + sanity-check files listed above.

**Interfaces:**
- Consumes: current doctrine from `CLAUDE.md` and the migrated docs under `docs/02-project-context/` and `docs/03-microservices/`.
- Produces: corrected agent-steering skills/commands that future agents load.

- [ ] **Step 1: Fix the obsolete BDT ranking section in `ddd-modeling`**

In `.claude/skills/ddd-modeling/SKILL.md`, replace the `### BDT ranking` section (currently lines ~61-84) with the point-based rule. Replace this block:

```markdown
### BDT ranking

BDT ranking does not use numeric accumulated score.

BDT ranking uses:

\`\`\`txt
rankingKey = (EtapasGanadas DESC, TiempoAcumuladoEtapasGanadas ASC)
\`\`\`

Use these concepts for BDT:

- `EtapasGanadas`
- `TiempoAcumuladoEtapasGanadas`
- `TiempoResolucionEtapa`
- `RankingBDT`

Do not model active BDT ranking through:

- `PuntajeEtapa`
- `PuntajeAcumulado`
- `PuntajeBDTIncrementado`

The BDT service may still record events for traceability, but ranking must be derived from stages won and accumulated time for won stages.
```

with:

```markdown
### BDT ranking

BDT native ranking uses accumulated points from won stages (current doctrine; see `docs/02-project-context/bdt-ranking-clarification.md`).

BDT ranking uses:

\`\`\`txt
rankingKey = (accumulated BDT points DESC, accumulated time of won stages ASC)
\`\`\`

where accumulated BDT points = sum of the `Puntaje` of the won stages.

Use these concepts for BDT:

- `Puntaje` (operator-set, per `EtapaBDT`)
- accumulated BDT points (sum of won-stage `Puntaje`)
- `TiempoAcumuladoEtapasGanadas` (tie-break: accumulated time of won stages only)
- `RankingBDT`

`EtapasGanadas` (count of stages won) is kept as informative data only — never the primary sort key.

Events: `EtapaBDTGanada` (carries `Puntaje`) and `RankingBDTActualizado`.
```

- [ ] **Step 2: Fix the Core concepts and participant lists in `ddd-modeling`**

In `.claude/skills/ddd-modeling/SKILL.md`, in the `## Core concepts` list (currently lines ~104-129): remove `CodigoAcceso`, `FormularioTrivia`, `PartidaTrivia`, `CompetidorTrivia`, `PartidaBDT`, `ExploradorBDT`; add `Partida` (aggregate root), `Juego`, `JuegoTrivia`, `JuegoBDT`, `InvitacionEquipo`, `ParticipanteTrivia`, `ParticipanteBDT`. Keep still-valid concepts (`Usuario`, `Equipo`, `InscripcionPartida`, `Convocatoria`, `Pregunta`, `Opcion`, `PuntajeAsignado`, `RespuestaTrivia`, `RankingTrivia`, `EtapaBDT`, `TesoroQR`, `CodigoQREsperado`, `AreaBusqueda`, `UbicacionGeografica`, `Pista`, `RankingBDT`, `RegistroAuditoria`, `EventoHistorial`). Add `Puntaje` (per `EtapaBDT`).

In the `## Context-specific participant concepts` list (currently lines ~131-139), replace:

```markdown
- `ParticipanteEquipo` in Team context.
- `CompetidorTrivia` in Trivia context.
- `ExploradorBDT` in BDT context.
```

with:

```markdown
- `ParticipanteEquipo` in the Equipos context (inside Identity).
- `ParticipanteTrivia` in the Trivia context.
- `ParticipanteBDT` in the BDT context.
```

Also update the `### Trivia scoring` example (lines ~47-49): keep direct accumulation but rename `competidor` to `participante` so it reads `participante.PuntajeAcumulado += scoreEarned` (identifier in a doc example, not code).

- [ ] **Step 3: Fix the service list and source references in `umbral-context`**

In `.claude/skills/umbral-context/SKILL.md`, replace lines 42-43:

```markdown
- The only valid backend services are Identity Service, Team Service, Trivia Game Service and BDT Game Service.
- Do not create Audit Service, Scoring Service, Trivia Service or Treasure Hunt Service as physical services.
```

with:

```markdown
- The only valid backend physical services are Identity, Partidas, Operaciones de Sesion and Puntuaciones, behind the mandatory YARP gateway.
- Do not create Team Service, Trivia Game Service, BDT Game Service, Treasure Hunt Service, Audit Service, Scoring Service or Notification Service as physical services (obsolete / superseded).
```

In the same file, replace the source-file list (lines 31-36) so every entry points to a real file:

```markdown
- `docs/01-project-source/srs.md`
- `docs/01-project-source/modelo-de-dominio.md`
- `docs/01-project-source/diagrama-de-clases.md`
- `docs/01-project-source/microservicios.md`
- `docs/01-project-source/enunciado-proyecto.md`
```

- [ ] **Step 4: Fix the owning-service list and source references in `sdd-workflow`**

In `.claude/skills/sdd-workflow/SKILL.md`, replace the `## Service topology rule` valid list (lines 89-96):

```markdown
Valid owning services are only:

- Identity
- Partidas
- Operaciones de Sesion
- Puntuaciones

Stop if a feature points to Team Service, Trivia Game Service, BDT Game Service, Treasure Hunt Service, Audit Service, Scoring Service or Notification Service as owning service (these are obsolete / superseded).
```

In the same file, fix the source-file list (lines 74-78) to the canonical files (same five as Step 3).

- [ ] **Step 5: Fix the real-time owning-service list in `websocket-signalr`**

In `.claude/skills/websocket-signalr/SKILL.md`, replace the `## Valid services` block (lines 53-68):

```markdown
## Valid services

SignalR/WebSocket updates are routed through the mandatory YARP gateway and owned by:

- Operaciones de Sesion — live session/lobby/runtime updates (states, timers, questions/stages, clues, geolocation, reconnection).
- Puntuaciones — score and ranking updates.
- Identity — only when explicitly required by the SDD (e.g. real-time team membership).

Do not create real-time responsibilities for the obsolete / superseded services: Team Service, Trivia Game Service, BDT Game Service, Treasure Hunt Service, Audit Service, Scoring Service, Notification Service.
```

- [ ] **Step 6: Fix BDT ranking/event guidance in `rabbitmq-events`**

In `.claude/skills/rabbitmq-events/SKILL.md`, replace line 99:

```markdown
BDT ranking is based on stages won and accumulated time for won stages.
```

with:

```markdown
BDT ranking is based on accumulated points from won stages (sum of each won stage's `Puntaje`), tie-broken by the lowest accumulated time of the won stages. `EtapaBDTGanada` carries the stage `Puntaje`; `RankingBDTActualizado` broadcasts the recomputed order. See `docs/02-project-context/bdt-ranking-clarification.md`.
```

Read the surrounding section (lines ~85-99) and confirm the "use `EtapaBDTGanada` / `RankingBDTActualizado`" guidance remains; align any "do not use points" wording with the point-based rule (points now flow via `EtapaBDTGanada`'s `Puntaje`). Do not invent new event names — use only those already in `contracts/events/` and the migrated docs.

- [ ] **Step 7: Extend the obsolete-service negation in `contract-design`**

In `.claude/skills/contract-design/SKILL.md`, replace line 43:

```markdown
- Do not create contracts for Audit Service, Scoring Service, Trivia Service or Treasure Hunt Service.
```

with:

```markdown
- Do not create contracts for the obsolete / superseded services: Team Service, Trivia Game Service, BDT Game Service, Treasure Hunt Service, Audit Service, Scoring Service or Notification Service. Active contracts cover only Identity, Partidas, Operaciones de Sesion, Puntuaciones and the gateway.
```

Confirm the `contracts/http/<service>-api.md` / `contracts/events/<service>-events.md` references (lines 37-38) read naturally for the four target services (no edit needed if `<service>` is a placeholder).

- [ ] **Step 8: Fix the owning-service lists in the 5 commands**

In each of `.claude/commands/create-feature-sdd.md`, `implement-task.md`, `plan-feature.md`, `review-boundaries.md`, `update-traceability.md`, replace the owning/valid-service list that reads:

```markdown
   - Identity Service
   - Team Service
   - Trivia Game Service
   - BDT Game Service
```

with:

```markdown
   - Identity
   - Partidas
   - Operaciones de Sesion
   - Puntuaciones
```

(Preserve each file's exact surrounding indentation/punctuation.) In `review-boundaries.md`, also update its "Do not create Audit Service, Scoring Service, Trivia Service or Treasure Hunt Service" line to the full obsolete list as in Step 7. In `create-feature-sdd.md`, fix line 26 to reference only `docs/01-project-source/srs.md` (drop the non-existent `historias de usuario.md`).

- [ ] **Step 9: Sanity-check the remaining UMBRAL skills/agents**

Read `.claude/skills/{cqrs-mediatr,efcore-postgres,testing,react-native-mobile}/SKILL.md` and `.claude/agents/{architect,backend,devops,frontend,mobile,qa}.md`. Edit only if a file presents obsolete doctrine as active (old service as a current target, stages-won ranking, team access code, `PartidaTrivia`/`PartidaBDT`/`FormularioTrivia`). Apply the same replacements. If a file is already consistent, make no change.

- [ ] **Step 10: Verify Task A**

Run:

```bash
rg -n "Team Service|Trivia Game Service|BDT Game Service|Treasure Hunt Service|Trivia Service|Audit Service|Scoring Service|Notification Service|EtapasGanadas DESC|ranks? by .*stage|derived from stages|PartidaTrivia|PartidaBDT|FormularioTrivia|CompetidorTrivia|ExploradorBDT|CodigoAcceso|historias de usuario\.md|modelo de dominio\.md|diagrama de clases\.md" .claude/skills .claude/commands .claude/agents
```

Expected: every remaining hit is a **negation** ("Do not create … Team Service …", "obsolete / superseded"). No line presents an obsolete service/rule as active, and no broken space-named source reference remains.

Run: `git diff --check` → no output.

- [ ] **Step 11: Commit Task A**

```bash
git add -- .claude/skills .claude/commands .claude/agents
git commit -m "Align .claude agent-steering docs with current doctrine

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task B: Verify And Align In-Code Markdown Docs

**Files:** the Task B verify/already-correct files listed above.

**Interfaces:**
- Consumes: current doctrine; the already-migrated `docs/` tree.
- Produces: confirmed-consistent in-code markdown (with small fixes only where a genuine active-doctrine defect exists).

- [ ] **Step 1: Scan the in-code markdown set**

Run:

```bash
rg -n "Team Service|Trivia Game Service|BDT Game Service|Treasure Hunt Service|EtapasGanadas|stages won|c[oó]digo de acceso|access code|PartidaTrivia|PartidaBDT|FormularioTrivia|formulario de Trivia" \
  mobile/README.md frontend/README.md infra/keycloak/README.md \
  mobile/src/features/trivia/live/README.md mobile/src/features/teams/CreateTeamScreen.md \
  services/trivia-game-service/AGENTS.md $(find services -name 'service-context.md') $(find services/identity-service -name '*.md')
```

- [ ] **Step 2: Classify each hit**

For every hit, decide: is it (a) a negation / historical / legacy-folder / migration-debt framing → **leave as-is**; (b) a valid `Identity Service` mention in `services/identity-service/**/*.md` → **leave as-is**; or (c) an active assertion of obsolete doctrine → **fix**. The already-verified files (`mobile/src/features/trivia/live/README.md` line 49 states the current point-based BDT rule; `CreateTeamScreen.md` marks `codigoAcceso` legacy; `services/trivia-game-service/AGENTS.md` is legacy-folder framing) are expected to be category (a)/(b) — confirm, do not change.

- [ ] **Step 3: Fix only genuine (c) defects**

For any category-(c) hit, rewrite to current doctrine using the Global Constraints mapping; where adjacent code still implements the old rule, append the one-line migration-debt note. If `mobile/README.md` / `frontend/README.md` / `infra/keycloak/README.md` describe architecture, ensure they name the four target services + gateway (or stay silent). Make no edit where the doc is already consistent.

- [ ] **Step 4: Verify Task B**

Re-run the Step 1 search. Expected: every remaining hit is a negation, historical/legacy framing, a migration-debt note, or a valid `Identity Service` mention.

Run: `git diff --check` → no output. Run `git diff --name-only` and confirm only `.md` files appear (no code/test files).

- [ ] **Step 5: Commit Task B**

```bash
git add -- mobile/README.md frontend/README.md infra/keycloak/README.md services mobile/src/features/trivia/live/README.md mobile/src/features/teams/CreateTeamScreen.md
git commit -m "Verify and align in-code markdown docs with current doctrine

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

If Step 3 produced no edits, record that in the commit by amending the scope to only the files actually changed, or skip the commit and note "Task B: verified clean, no edits" in the ledger.

---

### Task C: Align `frontend/src` And `mobile/src` Code Comments

**Files:** the Task C edit files listed above. **Comments/JSDoc/CSS-comment text only — never string literals, JSX text, identifiers, event-name strings, or test code.**

**Interfaces:**
- Consumes: current BDT ranking doctrine and the four-service model.
- Produces: in-code comments that teach current doctrine, with migration-debt notes where the surrounding maqueta code still models the old rule.

- [ ] **Step 1: Rewrite the BDT ranking JSDoc in `bdtRankingTypes.ts`**

In `mobile/src/features/bdt/ranking/bdtRankingTypes.ts`, the header JSDoc (lines ~9-17) currently asserts the OLD rule. Replace the rule paragraph:

```text
 * **REGLA DE RANKING BDT (no equivocarse):** NO es por puntaje acumulado. Ordena por:
 *   1) mayor número de **etapas ganadas** (`EtapasGanadas`), y
 *   2) desempate por **menor tiempo acumulado** de las etapas ganadas (`TiempoAcumuladoEtapasGanadas`).
 * Conceptos del dominio: `RankingBDTActualizado`. NO usar `PuntajeEtapa`/`PuntajeAcumulado`/
 * `PuntajeBDTIncrementado` aquí. Ver `docs/02-project-context/bdt-ranking-clarification.md`.
 *
 * Integración real: `load()` → endpoint/evento de ranking del BDT Game Service (push
 * `RankingBDTActualizado` vía SignalR, o GET del ranking de la partida). El backend ya entrega el orden;
 * la pantalla solo formatea y muestra (no reordena por puntaje).
```

with:

```text
 * **REGLA DE RANKING BDT (doctrina actual):** es por **puntos acumulados** de las etapas ganadas. Ordena por:
 *   1) mayor **puntaje acumulado** = suma del `Puntaje` de las etapas ganadas, y
 *   2) desempate por **menor tiempo acumulado** de las etapas ganadas (`TiempoAcumuladoEtapasGanadas`).
 * `EtapasGanadas` (número de etapas) es solo dato informativo, no la clave de orden.
 * Conceptos del dominio: `EtapaBDTGanada` (lleva `Puntaje`), `RankingBDTActualizado`.
 * Ver `docs/02-project-context/bdt-ranking-clarification.md`.
 *
 * ⚠ Deuda de migración: esta maqueta aún modela el ranking por etapas ganadas (campos `etapasGanadas` /
 * `tiempoAcumuladoSegundos`). Una `BackendBdtRankingSource` real debe ordenar por puntaje acumulado de
 * etapas ganadas. Ver `docs/02-project-context/documentation-migration-status.md`.
 *
 * Integración real: `load()` → endpoint/evento de ranking de **Puntuaciones** (push `RankingBDTActualizado`
 * vía SignalR a través del gateway, o GET del ranking de la partida). El backend ya entrega el orden;
 * la pantalla solo formatea y muestra.
```

Then update the field/interface comments (keep the field **identifiers** `etapasGanadas`, `tiempoAcumuladoSegundos`, `posicion`, `participante`, `esTu` unchanged):
- Line ~20 `/** Una fila del ranking BDT, ya ordenada por el backend (etapas desc, tiempo asc). */` → `/** Una fila del ranking BDT, ya ordenada por el backend (puntaje acumulado desc, tiempo asc). */`
- Line ~24 `/** Etapas ganadas (criterio principal del ranking BDT). */` → `/** Etapas ganadas (dato informativo bajo la doctrina actual; la clave de orden es el puntaje acumulado). */`
- Lines ~32-38 (the `BdtRankingSource` JSDoc): change `endpoint/evento de ranking del BDT Game Service` → `endpoint/evento de ranking de Puntuaciones`, and `regla BDT (etapas ganadas; desempate por tiempo acumulado)` → `regla BDT (puntaje acumulado de etapas ganadas; desempate por tiempo acumulado)`.

- [ ] **Step 2: Rewrite the TODO comment in `BdtRankingScreenContainer.tsx`**

In `mobile/src/features/bdt/ranking/BdtRankingScreenContainer.tsx`, replace the comment (lines ~10-12):

```text
  // TODO(backend): reemplazar el mock por una `BackendBdtRankingSource(apiBaseUrl, token, partidaId)` que
  // cumpla `BdtRankingSource` (endpoint/evento de ranking del BDT Game Service, orden por etapas/tiempo).
  // Ver `bdtRankingTypes.ts`. La pantalla NO cambia: solo se cambia esta fuente.
```

with:

```text
  // TODO(backend): reemplazar el mock por una `BackendBdtRankingSource(apiBaseUrl, token, partidaId)` que
  // cumpla `BdtRankingSource` (endpoint/evento de ranking de Puntuaciones, orden por puntaje acumulado de
  // etapas ganadas; desempate por tiempo). Ver `bdtRankingTypes.ts`. La pantalla NO cambia: solo la fuente.
```

(`createMockBdtRankingSource()` on line 13 is unchanged — identifier.)

- [ ] **Step 3: Fix the BDT comparison line and service name in `liveTriviaTypes.ts`**

In `mobile/src/features/trivia/live/liveTriviaTypes.ts`, replace the BDT comparison in the `finalScore` bullet (line ~29):

```text
 *       BDT, que ordena por `EtapasGanadas` + `TiempoAcumuladoEtapasGanadas`, no por puntaje.
```

with:

```text
 *       BDT, que ordena por puntaje acumulado de etapas ganadas (desempate por tiempo acumulado).
```

In the `onQuestion` bullet (lines ~15-17), replace the runtime owner and legacy pointer:

```text
 *       SignalR (Trivia Game Service) debe empujar la pregunta activa + su ventana de tiempo al
 *       participante cuando el operador la abre, y empujar `null`/evento de cierre al terminar.
 *       Ver `contracts/events/*trivia*` y `services/trivia-game-service` (SignalR hub). El timer es
```

with:

```text
 *       SignalR del runtime de Trivia en **Operaciones de Sesion** (a través del gateway) debe empujar la
 *       pregunta activa + su ventana de tiempo al participante cuando el operador la abre, y empujar
 *       `null`/evento de cierre al terminar. Ver `contracts/events/` (eventos de Trivia). El timer es
```

(Leave the `submit`/`questionResult`/`finalScore` Trivia references — `PuntajeAcumulado`, `RankingTriviaActualizado` — unchanged; Trivia ranking by points is correct.)

- [ ] **Step 4: Add a migration-debt note in `useBdtActiveStage.js`**

In `mobile/src/features/bdt/useBdtActiveStage.js`, the comment on line ~19 names the event `PartidaBDTIniciada`, which the code still subscribes to on line ~89 (a literal string, out of scope). Keep the event name (so the comment matches the code) and append a debt note. Replace line ~19:

```text
 *   - refresca solo ante el evento documentado `PartidaBDTIniciada` de la misma partida,
```

with:

```text
 *   - refresca solo ante el evento `PartidaBDTIniciada` de la misma partida (nombre heredado del antiguo
 *     agregado PartidaBDT; deuda de migración — la doctrina actual usa Partida + JuegoBDT con eventos a nivel
 *     de partida/juego; ver `docs/02-project-context/documentation-migration-status.md`),
```

- [ ] **Step 5: Fix the `Trivia form` CSS comment**

In `frontend/src/styles/components.css`, replace line ~388:

```text
/* Question editor cards (Trivia form) */
```

with:

```text
/* Question editor cards (Trivia question editor — questions belong to JuegoTrivia) */
```

- [ ] **Step 6: Scope guard — confirm comments-only**

Run:

```bash
git diff -- frontend/src mobile/src | rg '^[+-]' | rg -v '^(\+\+\+|---)' | rg -v '^[+-]\s*(\*|//|/\*|\*/|<!--|/\* )' | rg -v '^[+-]\s*$'
```

Expected: **no output** — every added/removed line is inside a comment/JSDoc/CSS comment. Any line printed here is a non-comment change and must be reverted.

- [ ] **Step 7: Verify Task C**

Run:

```bash
rg -n "Trivia Game Service|BDT Game Service|formulario de Trivia|Trivia form|EtapasGanadas\b|orden por etapas" frontend/src mobile/src
```

Expected: remaining hits are only string literals / JSX / identifiers / test code that are deliberately out of scope (e.g. `subscribe("PartidaBDTIniciada"`, error-message strings, `it("creates a Trivia form …")`, the `etapasGanadas`/`tiempoAcumuladoSegundos` interface field identifiers). No comment/JSDoc states the old BDT ranking rule or names an old service as the runtime/ranking owner.

Run: `git diff --check` → no output.

- [ ] **Step 8: Commit Task C**

```bash
git add -- frontend/src mobile/src
git commit -m "Align frontend/mobile code comments with current doctrine

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task D: Final Whole-Branch Review And Ledger

**Files:** `.git/sdd/progress.md`.

- [ ] **Step 1: Global residual search across all in-scope surfaces**

Run:

```bash
rg -n "Team Service|Trivia Game Service|BDT Game Service|Treasure Hunt Service|Trivia Service|Audit Service|Scoring Service|Notification Service|EtapasGanadas DESC|ranks? by .*stage|derived from stages|PartidaTrivia|PartidaBDT|FormularioTrivia|CompetidorTrivia|ExploradorBDT|historias de usuario\.md" \
  .claude mobile/README.md frontend/README.md infra/keycloak/README.md services mobile/src frontend/src
```

Expected: every hit is a negation, historical/legacy framing, a migration-debt note, a valid `Identity Service` mention, or an explicitly out-of-scope Tier-2/Tier-3 string/identifier/test line. Fix any active-doctrine assertion that slipped through (re-running the owning task's steps).

- [ ] **Step 2: Scope integrity**

Run `git diff --name-only <Task-A-base>..HEAD` and confirm: no `opencode.json`, no `.opencode/`, no `docs/01-project-source/`, no `.cs`/test files, and no behavior-line changes (Task C scope guard already enforced comments-only). Run `git diff --check` → no output.

- [ ] **Step 3: Dispatch the final multi-agent whole-branch review**

Use superpowers:subagent-driven-development's final-review step (or an equivalent multi-agent review) scoped to the residual-alignment commits, with the Global Constraints above as the constraints block. Adjudicate findings; dispatch one fix subagent for any Critical/Important findings; record Minors.

- [ ] **Step 4: Update the ledger**

Append a "Residual doc alignment" section to `.git/sdd/progress.md` recording Tasks A–C commit ranges, the review verdict, and any deferred Minors. Commit it.

---

## Self-Review Notes

**Spec coverage:**
- Group A (.claude agent-steering docs) → Task A (Steps 1-9).
- Group B (in-code markdown) → Task B.
- Group C (frontend/mobile comments) → Task C.
- Doctrine-forward + migration-debt convention → applied in Task A Step 1/6, Task C Steps 1/4.
- `Identity Service` carve-out → Task B Step 2.
- Comments-only rule → Task C Step 6 scope guard.
- Verification + final review + ledger → Task D.

**Placeholder scan:** every edit step contains the exact before/after text or a precise classify-and-fix rule grounded in the migrated docs; no "TBD"/"add appropriate"/"similar to" placeholders.

**Type/name consistency:** target services are uniformly `Identity`, `Partidas`, `Operaciones de Sesion`, `Puntuaciones`; old→new entity mapping is applied identically in Task A (Steps 1-2) and Task C (Step 1); event names (`EtapaBDTGanada`, `RankingBDTActualizado`) and the BDT rule (accumulated won-stage `Puntaje`, tie-break accumulated time, `EtapasGanadas` informative) are stated identically across `ddd-modeling`, `rabbitmq-events`, and `bdtRankingTypes.ts`.
