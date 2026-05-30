# UMBRAL Frontend Context

## Purpose

The frontend is the web interface for UMBRAL. It supports administration, operation and participation in real-time games under the only two approved game modes:

- Trivia
- Búsqueda del Tesoro / BDT

Do not implement generic "missions", "sessions" or "evidence" screens from the academic base statement unless they are explicitly mapped to the current SRS vocabulary.

Use the current project vocabulary:

- partida
- lobby
- formulario de Trivia
- pregunta
- respuesta
- ranking
- equipo
- líder
- convocatoria
- etapa BDT
- tesoro QR
- pista
- geolocalización BDT

## Active backend services

The frontend may interact, directly or through the gateway, only with these backend services:

- Identity Service
- Team Service
- Trivia Game Service
- BDT Game Service

Do not reference these as active services:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

## Required sources before frontend work

Before implementing any frontend flow, read:

- `AGENTS.md`
- `docs/04-sdd/SPECS-LIST.md`
- related feature folder under `docs/04-sdd/specs/`
- related HTTP contract under `contracts/http/`
- related event contract under `contracts/events/` when real-time behavior is involved
- `docs/03-microservices/service-ownership.md`
- `docs/02-project-context/first-delivery-scope.md`

## Frontend boundaries

The frontend must not:

- invent backend endpoints;
- invent game modes;
- calculate authoritative scores;
- decide business rules that belong to backend domain logic;
- bypass service ownership;
- treat leadership as a Keycloak role;
- implement native mobile-only behavior.

The frontend may:

- render role-aware views;
- call documented HTTP endpoints;
- subscribe to documented SignalR/WebSocket updates;
- show validation messages returned by backend;
- manage local UI state;
- guide users through allowed flows.

## Role-aware areas

### Administrador

Allowed UI areas depend on the active SDD, but may include:

- user administration;
- role assignment at user creation when supported by Identity Service;
- team administration if included by the SRS/SDD.

### Operador

Allowed UI areas include:

- creating and managing Trivia forms;
- creating and publishing Trivia games;
- creating and publishing BDT games;
- viewing lobbies;
- starting games;
- closing Trivia questions;
- closing BDT stages;
- sending BDT clues;
- viewing rankings;
- viewing relevant history/traces exposed by the owning game service.

### Participante

Allowed UI areas include:

- seeing published Trivia games;
- seeing published BDT games;
- filtering games by modality;
- creating or joining teams;
- joining individual games;
- entering team games only when leadership rules allow it;
- responding to Trivia;
- viewing Trivia results/ranking;
- viewing BDT active stage;
- uploading a QR treasure image;
- receiving BDT clues;
- allowing geolocation for BDT when required.

## Core frontend flows by service

### Identity Service

Frontend flows:

- login / authenticated access through Keycloak;
- user profile display where applicable;
- administrator user-management screens when active SDD requires them.

Contracts:

- `contracts/http/identity-api.md`

### Team Service

Frontend flows:

- create team;
- join team using code;
- delete team;
- transfer leadership;
- leave team;
- show current team and leadership state.

Contracts:

- `contracts/http/team-api.md`
- `contracts/events/team-events.md` when asynchronous or real-time behavior is approved by SDD.

### Trivia Game Service

Frontend flows:

- list published Trivia games;
- filter Trivia games by modality;
- show warning when non-leader tries to enter team Trivia;
- create Trivia forms;
- create and publish Trivia games;
- join individual Trivia;
- join team Trivia as leader;
- lobby/waiting screen;
- view participants/equipes in lobby;
- start Trivia as operator;
- answer questions;
- show question result after close;
- show Trivia ranking.

Contracts:

- `contracts/http/trivia-game-api.md`
- `contracts/events/trivia-game-events.md`

### BDT Game Service

Frontend flows:

- list published BDT games;
- filter BDT games by modality;
- show warning when non-leader tries to enter team BDT;
- create BDT game;
- join individual BDT;
- join team BDT as leader;
- view BDT lobby participants;
- start BDT as operator;
- show active stage;
- upload QR treasure image;
- show QR validation result;
- close BDT stage;
- send and receive clues;
- show BDT ranking;
- send/show geolocation when approved by SDD.

Contracts:

- `contracts/http/bdt-game-api.md`
- `contracts/events/bdt-game-events.md`

## Real-time behavior

Use SignalR/WebSockets only when the approved SDD requires user-visible real-time updates.

Examples:

- game publication updates;
- lobby updates;
- Trivia question activation/close;
- Trivia ranking updates;
- BDT stage updates;
- BDT clue updates;
- BDT QR validation result updates;
- BDT geolocation updates.

SignalR/WebSockets must not replace persistence or backend validation.

## UI implementation guidance

Organize frontend code into:

```txt
src/
  app/ or pages/
  components/
  features/
    identity/
    teams/
    trivia/
    bdt/
  api/
  hooks/
  state/
  routes/
  tests/
```

Feature modules should map to business areas, not to backend implementation details when that makes the UI clearer. However, API clients must respect backend service ownership and documented contracts.

## SDD rule

Never implement a frontend flow unless:

1. the HU appears in `docs/04-sdd/SPECS-LIST.md`;
2. its SDD folder exists;
3. `spec.md`, `design.md`, `tasks.md` and `acceptance.md` contain no unresolved TODO;
4. the required HTTP/event contracts are documented;
5. the owning service is one of the four approved services.
