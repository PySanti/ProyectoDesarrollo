# UMBRAL Frontend Context

## Purpose

The frontend is the React web interface for UMBRAL.

It supports administration and operation flows for the only two approved game modes:

- Trivia
- Búsqueda del Tesoro / BDT

The React web frontend is **not** the participant gameplay client. Participant gameplay belongs to the React Native mobile app.

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

## Client split

| Actor | Client |
|---|---|
| Administrador | React web |
| Operador | React web |
| Participante | React Native mobile |
| Líder de equipo | React Native mobile when acting as participant |
| Sistema | Backend |

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

Before implementing any frontend web flow, read:

- `AGENTS.md`
- `docs/04-sdd/SPECS-LIST.md`
- related feature folder under `docs/04-sdd/specs/`
- related HTTP contract under `contracts/http/`
- related event contract under `contracts/events/` when real-time behavior is involved
- `docs/03-microservices/service-ownership.md`
- `docs/02-project-context/first-delivery-scope.md`
- `docs/02-project-context/mobile-participant-context.md`

## Frontend web boundaries

The React web frontend must not:

- implement participant gameplay flows;
- implement QR treasure upload screens for participant gameplay;
- implement Trivia answer screens for participant gameplay;
- implement BDT active-stage participant screens;
- implement participant geolocation sharing;
- invent backend endpoints;
- invent game modes;
- calculate authoritative scores;
- decide business rules that belong to backend domain logic;
- bypass service ownership;
- treat leadership as a Keycloak role;
- implement native mobile-only behavior.

The React web frontend may:

- render administrator views;
- render operator views;
- call documented HTTP endpoints;
- subscribe to documented SignalR/WebSocket updates;
- show validation messages returned by backend;
- manage local UI state;
- guide users through allowed administrative or operator flows.

## Role-aware web areas

### Administrador

Allowed UI areas depend on the active SDD, but may include:

- user administration;
- role assignment at user creation when supported by Identity Service;
- team administration if included by the SRS/SDD;
- read-only consultation of operational information when the SDD allows it.

### Operador

Allowed UI areas include:

- creating and managing Trivia forms;
- creating and publishing Trivia games;
- creating and publishing BDT games;
- configuring BDT stages;
- viewing lobbies;
- starting games;
- supervising Trivia ranking;
- supervising BDT ranking;
- sending BDT clues;
- viewing uploaded treasures and QR validation results;
- viewing BDT geolocation map;
- viewing relevant history/traces exposed by the owning game service;
- cancelling games when allowed by the SRS/SDD.

### Participante

Participant gameplay flows belong to the React Native mobile app.

The React web frontend must not implement participant gameplay flows unless a specific SDD explicitly says otherwise.

Participant-owned mobile flows include:

- seeing published Trivia games;
- seeing published BDT games;
- filtering games by modality;
- creating or joining teams;
- leaving teams;
- transferring leadership;
- joining individual games;
- preinscribing teams as leader;
- accepting or rejecting convocatorias;
- responding to Trivia;
- viewing Trivia results/ranking;
- viewing BDT active stage;
- uploading a QR treasure image;
- receiving BDT clues;
- allowing geolocation for BDT when required;
- receiving participant notifications.

## Core frontend web flows by service

### Identity Service

Web flows:

- login / authenticated access through Keycloak;
- user profile display where applicable;
- administrator user-management screens when active SDD requires them.

Contracts:

- `contracts/http/identity-api.md`

### Team Service

Web flows:

- administrator team management when active SDD requires it;
- read-only team consultation for administrator/operator when active SDD requires it.

Participant team management belongs to React Native mobile.

Contracts:

- `contracts/http/team-api.md`
- `contracts/events/team-events.md` when asynchronous or real-time behavior is approved by SDD.

### Trivia Game Service

Web flows:

- create Trivia forms;
- create and publish Trivia games;
- view Trivia operator lobby;
- view participants/equipes in lobby;
- start Trivia as operator;
- supervise Trivia ranking;
- cancel Trivia when allowed;
- view Trivia details/history when exposed by SDD.

Participant Trivia listing, joining, answering and result views belong to React Native mobile.

Contracts:

- `contracts/http/trivia-game-api.md`
- `contracts/events/trivia-game-events.md`

### BDT Game Service

Web flows:

- create BDT game;
- configure BDT stages;
- view BDT operator lobby participants;
- start BDT as operator;
- send clues to participants/equipes;
- supervise uploaded treasures;
- supervise QR validation results;
- view BDT ranking;
- view BDT geolocation map;
- cancel BDT when allowed;
- view BDT details/history when exposed by SDD.

Participant BDT listing, joining, active stage, QR treasure upload, clue receipt and geolocation sharing belong to React Native mobile.

Contracts:

- `contracts/http/bdt-game-api.md`
- `contracts/events/bdt-game-events.md`

## Real-time behavior

Use SignalR/WebSockets only when the approved SDD requires user-visible real-time updates.

Examples for React web:

- game publication updates for operator/admin views;
- lobby updates;
- Trivia ranking updates;
- BDT ranking updates;
- BDT uploaded treasure updates;
- BDT QR validation result updates;
- BDT stage updates for operator supervision;
- BDT geolocation map updates;
- cancellation/state updates.

SignalR/WebSockets must not replace persistence or backend validation.

## UI implementation guidance

Organize React web code into:

```txt
frontend/
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

Never implement a React web flow unless:

1. the HU appears in `docs/04-sdd/SPECS-LIST.md`;
2. its SDD folder exists;
3. `spec.md`, `design.md`, `tasks.md` and `acceptance.md` contain no unresolved TODO;
4. the required HTTP/event contracts are documented;
5. the owning service is one of the four approved services;
6. the target client is React web according to the SDD and actor/client routing rule.
