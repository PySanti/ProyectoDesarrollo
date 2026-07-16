# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project identity

UMBRAL is an academic software engineering project for operating real-time interactive experiences under exactly two game modes: **Trivia** and **Búsqueda del Tesoro (BDT)**. Do not create, infer, or implement additional game modes or generic workflows.

A **Partida** is the unit that is published, joined, and ranked. It contains **one or more `Juego`** played in sequential order, and each `Juego` is exactly one of two specializations: **`JuegoTrivia`** or **`JuegoBDT`** (by `TipoJuego`). Lobby, inscription, modality, start mode, lifecycle, and the consolidated ranking are **partida-level**; each game has its own internal sub-state (`Pendiente`/`Activo`/`Finalizado`).

Most domain documentation is written in Spanish and is **authoritative for the domain**. `AGENTS.md` (root) is the project ruleset; the SRS and domain model under `docs/` are the source of truth for entities, rules, and ranking. This file summarizes the operational essentials.

## ⚠️ Migration status (read first)

The canonical architecture below (four services: **Identity, Partidas, Operaciones de Sesión, Puntuaciones** behind a YARP gateway) is the **target** defined by the course directives and the project documentation. The repository is being restructured **from** a previous layout (Identity / Team / Trivia Game / BDT Game, no enforced gateway) **to** this one.

- The four documented services and the rules in this file are **authoritative**. Where on-disk folders, ports, or DB names still reflect the old layout, that is migration debt, not the target.
- Do the migration **per SDD, one service/slice at a time**, and record the decomposition and renaming in an **ADR** under `docs/05-decisions/` before large refactors.
- Do not reintroduce the old decomposition (`Team Service`, `Trivia Game Service`, `BDT Game Service`) or the old "BDT ranks by stages won, not points" rule — both are superseded (see "Ranking" and "Explicit non-services").

## Architecture

### Backend: four physical microservices

The backend is **four independent .NET 8 microservices** (Clean Architecture + CQRS via MediatR), behind a mandatory API Gateway (YARP). Do not collapse them into a monolith, and do not add or remove services without an approving ADR.

| Service | DDD contexts it materializes | Owns |
|---|---|---|
| **Identity** | Identidad (Generic) + Equipos (Support) + permission/role governance | users, local user references, Keycloak mapping; roles, functional permissions and governance privileges **per role**; role modification; temporary-credential state; teams, team membership, leadership & transfer, team invitations (`InvitacionEquipo`), per-participant team-name history; async email notification (temporary password). **Absorbs the former Team Service entirely.** |
| **Partidas** | Partidas (Core) + Trivia/BDT **configuration** | creation and configuration of a `Partida` and its `Juego`s (including sequential order, modality, min/max participation, start mode and time); game content: Trivia questions (created when the game is created) with options/correct answer/`PuntajeAsignado`/time limit, and BDT stages with expected QR text, **per-stage `Puntaje`**, and time limit. **Does not** run the live session or compute scores/ranking. |
| **Operaciones de Sesión** | Trivia/BDT **runtime** + Participación (Support) | the live experience: publishing a partida (→ `Lobby`), manual/automatic start, question/stage synchronization, answer and QR validation, sequential advance of games and stages, clue delivery, geolocation, reconnection, real-time session communication; **inscriptions and team convocatorias** (partida-level). Stores only **transient** session state and emits domain events via RabbitMQ. |
| **Puntuaciones** | Scoring + ranking + Auditoría/Historial (cross-cutting) | tracks scores and won stages, computes each game's native ranking during and at end of play, and the consolidated partida ranking; team-performance queries; **materializes audit/history**. It is a **read/projection model fed by RabbitMQ domain events**, broadcasting updates via **SignalR**. Owns neither configuration nor runtime. |

**Hard boundaries:**
- A service must **never** read or write another service's database.
- Cross-service async workflows use **RabbitMQ**; user-visible real-time updates use **SignalR/WebSockets**.
- Domain must not depend on infrastructure. Controllers must not contain business rules. Commands mutate state; queries do not.
- Backend is authoritative for business rules. Frontend/mobile validate for usability only.

Each service has its own PostgreSQL database, named `umbral_identity`, `umbral_partidas`, `umbral_operaciones_sesion`, `umbral_puntuaciones`.

### Per-service project layout (standardized across all four)

Every service follows the same 4-project layout under `services/<service>/src/`:
`*.Domain` → `*.Application` → `*.Infrastructure` → `*.Api`. Tests live under `services/<service>/tests/`. This structure is **identical across all four services** and applies to new code **and to refactoring the existing services into compliance**. The strict folder rules are in "Structure & coding rules (graded)" below.

### Gateway (YARP) — mandatory

- The gateway is **mandatory** and built on **YARP**; it is the single entry point to the backend. It is **not** an optional stub.
- **All** frontend/mobile ↔ backend traffic passes through the gateway, **including** real-time (WebSockets/SignalR). There is no direct client→service contact.
- The gateway **validates the Keycloak JWT** and applies **coarse, route-level authorization** using the token claims, **without querying Identity on every request** — by base role (`Administrador`/`Operador`/`Participante`) for most routes, and by governance privilege (`GestionarPartidas`/`GestionarEquipos`, HU-04) for the routes those two privileges govern. Both are role claims in the same token (ADR-0013), so the mechanism is identical either way.
- **Fine-grained, per-resource authorization by functional permission stays inside the microservices** — the gateway's privilege check above is still coarse (whole-route, not per-resource) and duplicated by each service's own policy for defense in depth.
- The gateway routes only (plus token validation / role authorization) and is extensible to edge concerns (rate limiting, load balancing, TLS termination). It owns **no** domain logic, scores, rankings, or DB access.

### Clients

- **`frontend/`** — React 18 + Vite + TypeScript web app. Used **only** by `Administrador` and `Operador` (user management, role/permission governance, team administration, partida creation with Trivia questions/BDT stages, publishing, lobby, live operation, rankings, clue delivery, BDT geolocation map, history — all in read/operate mode; admin views operations read-only).
- **`mobile/`** — React Native + Expo (SDK 54, RN 0.81) app. Used **only** by `Participante` / `Líder de equipo` acting as participant (single "Partidas" panel with modality filter, joining individual partidas, team actions and invitations, accepting/rejecting convocatorias, answering Trivia, QR treasure upload, receiving clues, BDT geolocation sharing).

**Client routing rule (from the SRS):** stories whose principal actor is `Administrador`/`Operador` → **web**; `Participante` (incl. `Líder de equipo` acting as participant) → **mobile**, unless a story says otherwise; `Sistema` → **backend**. `Líder de equipo` is **not** a Keycloak role — it is a business attribute (creator of, or transferee of leadership for, a team). Do not implement participant gameplay in web, or admin/operator screens in mobile, unless an SDD explicitly says so. **Explicit exception (HU-04):** a Participante holding `GestionarPartidas` or `GestionarEquipos` uses the **web** panel for that privilege, not mobile — the privilege is the same permission regardless of base role, and its UI is never duplicated for mobile.

### Other components

- **`gateway/`** — the mandatory YARP entry point described above.
- **`contracts/`** — source of truth for HTTP contracts (`contracts/http/*.md`) and event contracts (`contracts/events/*.md`).
- **`infra/`** — `docker-compose.yml` for PostgreSQL, RabbitMQ, Keycloak.

### Auth & token lifecycle

- Keycloak realm `UMBRAL-UCAB` with base realm roles `Administrador`, `Operador`, `Participante`. Web client `umbral-web`, mobile client `umbral-mobile` (PKCE S256). UMBRAL **stores no passwords**; it keeps only a local reference keyed by the Keycloak identifier.
- **Web and mobile authenticate directly with Keycloak**, not with the backend. The token carries user data, base role, and permissions; the gateway validates it and authorizes by role at the route level.
- **Token refresh happens only between the client (web/mobile) and Keycloak** — neither the gateway nor the backend participate. Refresh is **time-based** with an inactivity-control system: if the user is detected active shortly before the refresh window (e.g. a recent click), the token refreshes silently; otherwise a **modal asks whether to continue the session** before refreshing.
- Services still validate JWT audience/issuer via `KEYCLOAK_VALID_AUDIENCES` / `KEYCLOAK_VALID_ISSUERS` (defense in depth) and enforce **functional-permission** authorization locally.

## Domain model essentials

- **`Partida`** (aggregate root): `EstadoPartida` ∈ {`Lobby`, `Iniciada`, `Cancelada`, `Terminada`}; `Modalidad` ∈ {`Individual`, `Equipo`} (fixed once, applies to all its games); `ModoInicioPartida` ∈ {`Manual`, `Automatico`, `ManualYAutomatico`}; `MinimosParticipacion`/`MaximosParticipacion` (participants in `Individual`, teams in `Equipo`); contains `1..*` `Juego`; computes `RankingConsolidado` on finish. Every start requires meeting the minimums; failing them triggers automatic cancellation.
- **`Juego`** (base entity): `Orden`, `TipoJuego` ∈ {`Trivia`, `BusquedaDelTesoro`}, `EstadoJuego` ∈ {`Pendiente`, `Activo`, `Finalizado`}. Specialized as `JuegoTrivia` or `JuegoBDT`. Games activate sequentially.
- **`JuegoTrivia`** owns `Pregunta` (created with the game; each has options, correct answer, `PuntajeAsignado`, time limit — **no question bank, no reuse**), `ParticipanteTrivia`, `RespuestaTrivia`. A question closes for everyone on first correct answer or timeout. In `Equipo`, the valid answer is the first option sent by any active member.
- **`JuegoBDT`** owns `EtapaBDT` (each with expected QR **text** content, **`Puntaje`**, time limit), `ParticipanteBDT`, `TesoroQR`, `Pista`. Área de búsqueda is descriptive **text** (not coordinates). QR is validated by decoding the uploaded image and comparing to the expected text. A stage closes on first correct validation or timeout; in `Equipo`, a correct upload by any active member wins it for the whole team. **Geolocation is mandatory** for an active BDT game (mobile authorization; location sent ~every 2 seconds to the operator).
- **Participación**: `InscripcionPartida` (+ child `Convocatoria`) is **partida-level, once per partida** (one per participant in `Individual`; one per team in `Equipo`). A participant/team may have **only one active participation at a time** (active = active individual inscription or an accepted team convocatoria while the partida is in `Lobby`/`Iniciada`). Convocatoria affects only that partida, never team membership.

## Ranking (easy to get wrong — superseded vs. the old rule)

There are **two levels** of ranking. Use these concepts; do not invent others.

**Trivia native ranking (per `JuegoTrivia`)** — order by **accumulated points descending** (`PuntajeAcumulado`, summing each question's `PuntajeAsignado` on correct answers; time never modifies points), tie-break by **lowest accumulated answer time**. Events: `PuntajeTriviaIncrementado`, `RankingTriviaActualizado`.

**BDT native ranking (per `JuegoBDT`)** — order by **accumulated points in the game** = sum of the `Puntaje` of the **won stages**; tie-break by **lowest accumulated time of the won stages only**. Each `EtapaBDT` has an operator-set `Puntaje`; a won stage grants it; stages nobody wins grant nothing. The **count of stages won is kept only as informative data**, not as the sort key. Event with points: `EtapaBDTGanada` (carries `Puntaje`); ranking event: `RankingBDTActualizado`.

> **This reverses the previous CLAUDE.md rule.** BDT now **does** use per-stage points (`Puntaje`) and the BDT game ranking is **by accumulated points**, not by number of stages won. Update any code or specs still ranking BDT by `EtapasGanadas` and any "do not use puntaje for BDT" assumption.

**Consolidated partida ranking (`RankingConsolidado`, computed on finish)** — order participants/teams by (1) **number of games won**, then (2) **total accumulated points across all games**, then (3) **lowest total time**. A game's winner is whoever has the most points in it (tie-break: lowest time in that game; if still tied, the game has no winner). Concepts/events: `CalculadorRankingConsolidadoService`, `RankingConsolidadoCalculado`.

## Roles, permissions & governance

- Exactly three base roles exist — `Administrador`, `Operador`, `Participante`. **No new roles are ever created.**
- Two authorization levels: **governance privileges** (system administration) and **functional permissions** (`GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas`). Both are managed **per role**, never per user, from the admin **governance panel**.
- **The panel governs exactly two privileges: `GestionarPartidas` and `GestionarEquipos`.** Each always opens its whole area on **web**, regardless of the holder's base role — a Participante who receives either privilege uses the same web panel an Operador/Administrador would, not a mobile equivalent. `GestionarEquipos` governs only the **web panels for administering other people's teams** — a participant's own team (create, invite, lead, leave) comes with the `Participante` role and stays on mobile.
- **`ParticiparEnPartidas` is not governable.** It still exists in the domain, fixed to `Participante` as a composite declared in `umbral-realm.json`; the PUT rejects it with 400. Only that role has a client to play in, so moving it would enable nothing and removing it would take down all gameplay.
- **The área Identidad is not a privilege either** — it comes with the `Administrador` role and is protected. If it were governable, revoking it would lock everyone out of governance permanently.
- Defaults: Administrador → `GestionarEquipos`; Operador → `GestionarPartidas`; Participante → none (plus playing and own-team, which come with the role).
- **The realm declares what is fixed; `permisos_rol` governs what is variable.** They must not overlap: `keycloak-config` reapplies the realm on every `docker compose up`, so any governable privilege declared there would erase what the panel assigned. Identity's `PermisosRolKeycloakReconciler` converges Keycloak toward `permisos_rol` at startup.
- The admin may modify the role of operators/participants — **including promotion to admin** — but **never the role of an admin**, and the change is **propagated to Keycloak**. The Administrador role's governance privileges are protected and cannot be withdrawn.
- On user creation, a **temporary password** is generated and **emailed asynchronously (RabbitMQ)**; mandatory change on first login is handled by Keycloak. Changing the email while the credential is still temporary re-issues a new temporary password.

## Teams (Equipos — inside Identity)

1–5 members; the creator is the first member and leader. A user may belong to **only one active team at a time**. Members join **only via team invitations** (`InvitacionEquipo`) sent by the leader from a dynamic participant list that excludes anyone already in a team and is blocked when the team is full — **there is no team access code**. Invitations don't expire; deleting a team deletes its pending invitations but preserves history. Leadership can be transferred; a leader leaving with no other members deletes the team. Per-participant team-name **history** is preserved.

## Events & messaging

Domain events flow over **RabbitMQ** (e.g. `PartidaPublicadaEnLobby`, `PartidaIniciada`, `JuegoActivado`, `RespuestaTriviaValidada`, `TesoroQRValidado`, `EtapaBDTGanada`, `PartidaFinalizada`, `RankingConsolidadoCalculado`, `UsuarioCreado`, `CredencialTemporalEmitida`, team/invitation/convocatoria events). They feed **Puntuaciones** (scoring/ranking/audit) and audit materialization in **Operaciones de Sesión**. Async messaging also drives audit, history consolidation, internal notifications, and ranking updates so they don't block the main flow. Real-time user-facing updates (lobby, states, timers, ranking, stages, clues, geolocation, results) go over **SignalR/WebSockets** through the gateway. The canonical event/HTTP shapes live in `contracts/`.

## Explicit non-services

The four services above are the **only** physical services. Do **not** create or reference these as active physical services: Team Service, Trivia (Game) Service, BDT/Treasure Hunt Service, Audit Service, Scoring Service (separate from Puntuaciones), Notification Service. Teams/invitations/team-history and email notification live **inside Identity**; audit/history is a cross-cutting capability **materialized in Puntuaciones and Operaciones de Sesión**; scoring/ranking is **Puntuaciones**.

## Structure & coding rules (graded)

These come from the course directives and are **non-negotiable**; they apply to every service.

### `Api/` and controllers
- `Program.cs` must **not** build/register controllers inline. A dedicated `Controllers/` folder lives inside `Api/`.
- Each controller defines its own route/endpoint and **inherits from the framework `ControllerBase`** (the native ASP.NET Core base class — no custom base type is required).
- Controllers dispatch through MediatR (`_mediator.Send(...)`) and contain **no** business logic.
- **Every controller has unit tests.**

### `Application/`
Must contain exactly these top-level folders (no per-feature slice folders): `Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `DTOs/`, `Handlers/`, `Handlers/Commands/`, `Handlers/Queries/`, and `Exceptions/`.
- `Handlers/Commands/` holds the `XCommandHandler` classes; `Handlers/Queries/` holds the `XQueryHandler` classes.
- `DTOs/` holds request/response models; `Interfaces/` holds application-layer ports (repository interfaces stay in `Domain/`); `Exceptions/` holds application-layer exceptions.

### `Domain/` and `Infrastructure/`
- `Domain/` holds entities, enums, exceptions, **and the infrastructure interfaces** (e.g. repository interfaces) so infrastructure depends on the domain, never the reverse.
- `Infrastructure/` must contain `Persistence/` and `Services/` (PascalCase, matching the `identity-service` reference and the rest of the codebase).
- Repository **implementations** live in `Infrastructure/` (or `Domain/` only if pure), implementing the Domain-defined interfaces.

### Cross-cutting
- Centralized **exception handling** in every service.
- The folder structure is **standardized across all four services**.

## Commands

### Infrastructure (from repo root)

```powershell
docker compose -f "infra/docker-compose.yml" up -d postgres rabbitmq keycloak
docker compose -f "infra/docker-compose.yml" ps
docker compose -f "infra/docker-compose.yml" down        # add -v to wipe Postgres data
```

Create the per-service databases once after first start (they are not auto-created):

```powershell
docker exec -it umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE umbral_identity;"
docker exec -it umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE umbral_partidas;"
docker exec -it umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE umbral_operaciones_sesion;"
docker exec -it umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE umbral_puntuaciones;"
```

Postgres is exposed on host port **55432** (5432 inside Docker). Connection string pattern:
`Host=localhost;Port=55432;Database=<db>;Username=umbral;Password=16102005`.

### Running the backend (gateway + four services)

For local development, **run the APIs with `dotnet run`**, not Docker; the **gateway (YARP)** must be running for the clients to reach the backend. Each service has a `run-local.sh` / `run-local.ps1` that loads `services/<service>/.env` (gitignored) before launching:

```powershell
.\gateway\run-local.ps1                          # single backend entry point (YARP)
.\services\identity\run-local.ps1
.\services\partidas\run-local.ps1
.\services\operaciones-sesion\run-local.ps1
.\services\puntuaciones\run-local.ps1
```

> Exact folder slugs, host ports, `run-local` scripts, and `.sln` filenames for the four target services are **finalized in the migration ADR** (`docs/05-decisions/`) — do not assume the old names/ports. Services consumed by mobile must listen on `0.0.0.0` (not `localhost`) so a physical phone on the LAN can reach them. Before configuring Identity, set the real Keycloak `KEYCLOAK_CLIENT_SECRET`. Full env-var sets per service are documented in `GUIA-LEVANTAMIENTO.md`.

### Backend tests

Each service has Unit / Integration / Contract test projects under `services/<service>/tests/`, plus **controller unit tests** (required). Run a service's solution, or individual test projects:

```powershell
dotnet test "services/<service>/<Solution>.sln"
dotnet test "services/<service>/tests/<Project>.csproj"
```

### Frontend (web)

```powershell
cd frontend
npm install
npm run dev        # http://localhost:5173
npm test           # vitest run
npm run test:watch
npm run build      # vite build — NO hace typecheck (vite sólo transpila, no comprueba tipos)
npx tsc --noEmit -p tsconfig.app.json   # el typecheck de verdad
```

`src/` is organized as `api/`, `auth/`, `app/`, `features/`. Requires `.env` with `VITE_*` vars (see `GUIA-LEVANTAMIENTO.md`).

> ⚠️ **`npx tsc --noEmit` without `-p tsconfig.app.json` is a no-op and always exits 0.**
> `tsconfig.json` is a project-references stub (`"files": []`), so the bare command checks no files
> at all — verified by planting a type error and watching it exit 0. Always pass
> `-p tsconfig.app.json`. Note also that `npm run build` (`vite build`) does **not** typecheck: Vite
> transpiles without checking types. Nothing in this project typechecks unless you run that command.

### Mobile

Requires **Node ≥ 20.19.4** (Expo SDK 54 / RN 0.81). If you see `configs.toReversed is not a function`, you're on an incompatible Node version.

```powershell
cd mobile
npm install
npm start          # expo start --clear --host lan
npm run android
npm run ios
npm test           # node --test tests/*.test.js
npm run typecheck  # tsc --noEmit
```

For a physical phone, set the LAN IP (not `localhost`) in `mobile/.env` (`EXPO_PUBLIC_*` vars), and ensure `EXPO_PUBLIC_AUTH_REDIRECT_URI` exactly matches the `umbral-mobile` Keycloak redirect URI. `src/` is organized as `api/`, `auth/`, `config/`, `features/`, `navigation/`, `permissions/`, `screens/`, `shared/`. Run `expo start` with `--tunnel` if the phone can't reach Metro (firewall/VPN/isolated Wi-Fi).

See `GUIA-LEVANTAMIENTO.md` for the complete startup walkthrough and troubleshooting.

## SDD workflow is mandatory

Never implement directly from a vague prompt. The repo uses Spec-Driven Development under `docs/04-sdd/`. For every feature:

1. Identify the user story (`HU-xx`) and confirm its spec is in `docs/04-sdd/SPECS-LIST.md`.
2. Identify the owning microservice (Identity / Partidas / Operaciones de Sesión / Puntuaciones) and the client target (web / mobile / backend / mixed).
3. Read the relevant source context (see below) and the spec folder under `docs/04-sdd/specs/`.
4. Complete/refine `spec.md` → `design.md` → `tasks.md`, implement **one task at a time**, add/update tests, then update `acceptance.md` and `docs/04-sdd/traceability-matrix.md`.

If `spec.md`, `design.md`, `tasks.md`, or `acceptance.md` contains TODO sections, **do not code** — complete the SDD first. The four-service migration itself is driven the same way (per slice, with its governing ADR).

## Documentation hierarchy (order of authority)

1. `docs/01-project-source/` — raw team artifacts (SRS, domain model, microservice ownership table). **Domain source of truth.**
2. `docs/02-project-context/` — operational summaries (start here: `project-brief.md`, `srs-summary.md`, `business-rules.md`, `first-delivery-scope.md`, ranking clarification, and `design/*`)
3. `docs/03-microservices/` — service ownership & communication maps
4. `docs/04-sdd/` — feature specs
5. `docs/05-decisions/` — accepted ADRs (incl. the four-service migration ADR)
6. `contracts/` — HTTP and event contracts

`.claude/` holds agents, commands, and skills only — **not** domain documentation.

**Frontend redesign:** the web + mobile visual rebuild is a **visual + IA reconstruction only — no contract, business-rule, or HU changes**, and must not change `label`/`id`/`data-testid`/ARIA roles that tests rely on. Canonical in-repo docs (read these before touching the frontend):
- `docs/02-project-context/design/frontend-redesign-plan.md` — phases, per-surface status, user observations, and "how to continue".
- `docs/02-project-context/design/design-system.md` — the **implemented** CSS class API + file map (reuse these primitives, don't reinvent).
- `PRODUCT.md` + `DESIGN.md` (repo root) — brand language and tokens.
- `infra/keycloak/README.md` — realm import (`UMBRAL-UCAB`), test creds, re-seed.

## Academic brief vocabulary mapping

The original academic brief uses generic mission/session/evidence terms; UMBRAL maps them onto its own ubiquitous language: Mission → **Partida**; mission stages → **Juego** (`JuegoTrivia` / `JuegoBDT`), with Trivia `Pregunta`s and BDT `EtapaBDT`s as the inner steps; LiveSession → the live session managed by **Operaciones de Sesión**; EvidenceSubmission → **`RespuestaTrivia`** / **`TesoroQR`**; Team → **`Equipo`**; SessionEvent → **`EventoHistorial`** / **`RegistroAuditoria`**. There is **no** generic "Trivia form" anymore — questions belong directly to the `JuegoTrivia` and are created with it. Do not implement generic mission/session/evidence/form modules.
