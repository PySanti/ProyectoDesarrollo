# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project identity

UMBRAL is an academic software engineering project for operating real-time interactive experiences under exactly two game modes: **Trivia** and **Búsqueda del Tesoro (BDT)**. Do not create, infer, or implement additional game modes.

Most domain documentation, instructions, and rules are written in Spanish. `AGENTS.md` (root) is the canonical project ruleset; read it for the full set of constraints. This file summarizes the operational essentials.

## Architecture

### Backend: four physical microservices

The backend is **four independent .NET 8 microservices** (Clean/Hexagonal Architecture + CQRS via MediatR). Do not collapse them into a modular monolith, and do not add new services without an approving ADR.

| Service | Owns |
|---|---|
| Identity Service | users, roles, Keycloak mapping, local user references |
| Team Service | teams, team codes, members, leadership, team status & membership rules |
| Trivia Game Service | trivia forms, questions, options, sessions, participants, answers, scoring, ranking, lobby, history, real-time |
| BDT Game Service | BDT sessions, areas, stages, clues, expected QR codes, treasure uploads, QR validation, progress, ranking, history, geolocation, real-time |

Each service follows the same 4-project layout under `services/<service>/src/`:
`*.Domain` (entities/enums/exceptions, no infrastructure deps) → `*.Application` (CQRS handlers, abstractions) → `*.Infrastructure` (EF Core persistence, RabbitMQ events, external services) → `*.Api` (controllers, SignalR, auth). Tests live under `services/<service>/tests/`.

**Hard boundaries:**
- A service must **never** read or write another service's database.
- Cross-service async workflows use **RabbitMQ**; user-visible real-time updates use **SignalR/WebSockets**.
- Domain must not depend on infrastructure. Controllers must not contain business rules. Commands mutate state; queries do not.
- Backend is authoritative for business rules. Frontend/mobile validate for usability only.

Each service has its own PostgreSQL database (`umbral_identity`, `umbral_team`, `umbral_trivia_game`, `umbral_bdt_game`).

### Clients

- **`frontend/`** — React 18 + Vite + TypeScript web app. Used **only** by `Administrador` and `Operador` (administration, operator flows, game creation/supervision, lobby, rankings, BDT map, history).
- **`mobile/`** — React Native + Expo (SDK 54, RN 0.81) app. Used **only** by `Participante` / `Líder de equipo` acting as participant (listing/joining games, team actions, answering trivia, QR treasure upload, clues, geolocation sharing).

**Client routing rule:** stories with actor `Administrador`/`Operador` → web; `Participante`/`Líder de equipo` → mobile; `Sistema` → backend. Do not implement participant gameplay in web, or admin/operator screens in mobile, unless an SDD explicitly says so.

### Other components

- **`gateway/`** — optional entry-point/router (currently a stub; `gateway/src` is empty). Routes only; never owns domain logic, scores, rankings, or DB access. See `gateway/gateway-context.md`.
- **`contracts/`** — source of truth for HTTP contracts (`contracts/http/*.md`) and event contracts (`contracts/events/*.md`).
- **`infra/`** — `docker-compose.yml` for PostgreSQL, RabbitMQ, Keycloak.

### Auth

Keycloak realm `UMBRAL-UCAB` with realm roles `Administrador`, `Operador`, `Participante`. Web client `umbral-web`, mobile client `umbral-mobile` (PKCE S256). Services validate JWT audience/issuer via `KEYCLOAK_VALID_AUDIENCES` / `KEYCLOAK_VALID_ISSUERS` env vars.

## BDT ranking rule (easy to get wrong)

BDT ranking is **not** based on accumulated numeric score. It ranks by: (1) highest number of stages won; (2) tie-break by lowest accumulated time across stages won. Use concepts `EtapasGanadas`, `TiempoAcumuladoEtapasGanadas`, `RankingBDTActualizado`. Do **not** use `PuntajeEtapa` / `PuntajeAcumulado` / `PuntajeBDTIncrementado` for BDT ranking. Trivia still uses `PuntajeAsignado` / `PuntajeAcumulado` / `PuntajeTriviaIncrementado`.

## Explicit non-services

Do not create or reference these as active physical services: Audit Service, Scoring Service, Trivia Service, Treasure Hunt Service, Notification Service. Audit/ranking/history/notifications are responsibilities **inside** the owning service of each flow.

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
docker exec -it umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE umbral_team;"
docker exec -it umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE umbral_trivia_game;"
docker exec -it umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE umbral_bdt_game;"
```

Postgres is exposed on host port **55432** (5432 inside Docker). Connection string pattern:
`Host=localhost;Port=55432;Database=<db>;Username=umbral;Password=16102005`.

### Running microservices

`infra/docker-compose.yml` declares builds for all four services, but only `team-service` has a Dockerfile. For local development, **run the APIs with `dotnet run`**, not Docker. Each service has `run-local.sh` / `run-local.ps1` that load `services/<service>/.env` (gitignored) before launching:

```powershell
.\services\identity-service\run-local.ps1     # http://localhost:5000
.\services\team-service\run-local.ps1         # http://0.0.0.0:5099
.\services\trivia-game-service\run-local.ps1  # http://0.0.0.0:5015
.\services\bdt-game-service\run-local.ps1     # http://0.0.0.0:5016
```

Services consumed by mobile must listen on `0.0.0.0` (not `localhost`) so a physical phone on the LAN can reach them. Before configuring `identity-service`, set the real Keycloak `KEYCLOAK_CLIENT_SECRET`. Full env-var sets per service are documented in `GUIA-LEVANTAMIENTO.md`.

### Backend tests

Identity and Trivia have solution files; Team and BDT are tested per-project:

```powershell
dotnet test "services/identity-service/Umbral.IdentityService.sln"
dotnet test "services/trivia-game-service/Umbral.TriviaGame.sln"

dotnet test "services/team-service/tests/Umbral.TeamService.UnitTests/Umbral.TeamService.UnitTests.csproj"
dotnet test "services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj"
dotnet test "services/team-service/tests/Umbral.TeamService.ContractTests/Umbral.TeamService.ContractTests.csproj"

dotnet test "services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj"
dotnet test "services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj"
dotnet test "services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj"
```

Each service has Unit / Integration / Contract test projects. **Known issue:** `GetAll_NoGames_ReturnsEmptyList` in Trivia can fail due to InMemory test isolation — see `services/trivia-game-service/AGENTS.md` before assuming a regression.

### Frontend (web)

```powershell
cd frontend
npm install
npm run dev        # http://localhost:5173
npm test           # vitest run
npm run test:watch
npm run build      # vite build (tsc + bundle)
```

`src/` is organized as `api/`, `auth/`, `app/`, `features/`. Requires `.env` with `VITE_*` vars (see `GUIA-LEVANTAMIENTO.md`).

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

For a physical phone, set LAN IP (not `localhost`) in `mobile/.env` (`EXPO_PUBLIC_*` vars), and ensure `EXPO_PUBLIC_AUTH_REDIRECT_URI` exactly matches the `umbral-mobile` Keycloak redirect URI. `src/` is organized as `api/`, `auth/`, `config/`, `features/`, `navigation/`, `permissions/`, `screens/`, `shared/`. Run `expo start` with `--tunnel` if the phone can't reach Metro (firewall/VPN/isolated Wi-Fi).

See `GUIA-LEVANTAMIENTO.md` for the complete startup walkthrough and troubleshooting.

## SDD workflow is mandatory

Never implement directly from a vague prompt. The repo uses Spec-Driven Development under `docs/04-sdd/`. For every feature:

1. Identify the user story and confirm its spec is in `docs/04-sdd/SPECS-LIST.md`.
2. Identify the owning microservice and the client target (web / mobile / backend / mixed).
3. Read the relevant source context (see below) and the spec folder under `docs/04-sdd/specs/`.
4. Complete/refine `spec.md` → `design.md` → `tasks.md`, implement **one task at a time**, add/update tests, then update `acceptance.md` and `docs/04-sdd/traceability-matrix.md`.

If `spec.md`, `design.md`, `tasks.md`, or `acceptance.md` contains TODO sections, **do not code** — complete the SDD first.

## Documentation hierarchy (order of authority)

1. `docs/01-project-source/` — raw team artifacts
2. `docs/02-project-context/` — operational summaries (start here: `project-brief.md`, `srs-summary.md`, `business-rules.md`, `first-delivery-scope.md`, `bdt-ranking-clarification.md`, and `design/*`)
3. `docs/03-microservices/` — service ownership & communication maps
4. `docs/04-sdd/` — feature specs
5. `docs/05-decisions/` — accepted ADRs (ADR-0001 … ADR-0007)
6. `contracts/` — HTTP and event contracts

`.claude/` (formerly `.opencode/`, renamed) holds agents, commands, and skills only — **not** domain documentation.

**Frontend redesign:** the web + mobile visual rebuild is a **visual + IA reconstruction only — no contract, business-rule, or HU changes**, and must not change `label`/`id`/`data-testid`/ARIA roles that tests rely on. Canonical in-repo docs (read these before touching the frontend):
- `docs/02-project-context/design/frontend-redesign-plan.md` — phases, per-surface status, user observations (OBS-01…07), and "how to continue".
- `docs/02-project-context/design/design-system.md` — the **implemented** CSS class API + file map (reuse these primitives, don't reinvent).
- `PRODUCT.md` + `DESIGN.md` (repo root) — brand language and tokens.
- `infra/keycloak/README.md` — realm import (`UMBRAL-UCAB` auto-seeded), test creds, re-seed.

Status: **Fase 0 (foundation) and Fase 1 (web: app-shell + all Administrador/Operador surfaces) complete**; verified by `tsc` + `vite build` + 52 `vitest` tests. Pending: in-browser visual pass with real data, and **Fase 2 — Mobile (React Native)** (impeccable does not run on RN; mirror `DESIGN.md` tokens via a TS theme, verify with Expo).

## Academic brief vocabulary mapping

The original academic brief uses mission/session/evidence terms; UMBRAL maps them: Mission → Trivia form / BDT configuration; mission stages → Trivia questions / BDT stages; LiveSession → PartidaTrivia / PartidaBDT; EvidenceSubmission → RespuestaTrivia / TesoroQR; Team → Equipo; SessionEvent → EventoHistorial. Do not implement generic mission/session/evidence modules.
