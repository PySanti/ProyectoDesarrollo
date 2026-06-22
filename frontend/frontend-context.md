# UMBRAL Frontend Context

## Purpose

The frontend is the React web interface for UMBRAL.

It supports administration and operation flows for the only two approved game modes:

- Trivia
- Búsqueda del Tesoro / BDT

The React web frontend is **not** the participant gameplay client. Participant gameplay belongs to the React Native mobile app. The web frontend calls backend capabilities only through the mandatory YARP gateway.

Do not implement generic "missions", "sessions" or "evidence" screens from the academic base statement unless they are explicitly mapped to the current SRS vocabulary.

Use the current project vocabulary:

- partida
- lobby
- pregunta de Trivia en JuegoTrivia
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

## Active backend services and gateway

The frontend may interact only through the YARP gateway. It must not call microservices directly.

Current target services behind the gateway are:

- Identity
- Partidas
- Operaciones de Sesion
- Puntuaciones

The previous `Team Service`, `Trivia Game Service` and `BDT Game Service` names are legacy implementation-folder names, not active service boundaries.

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

- creating partidas with sequential Trivia or BDT games;
- configuring Trivia questions directly in `JuegoTrivia`;
- configuring BDT stages directly in `JuegoBDT`;
- viewing lobbies;
- starting games;
- supervising Trivia ranking;
- supervising BDT ranking;
- sending BDT clues;
- viewing uploaded treasures and QR validation results;
- viewing BDT geolocation map;
- viewing relevant history/traces exposed by the owning target service or projection context;
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

## Core frontend web flows by target service

### Identity

Web flows:

- login / authenticated access through Keycloak;
- user profile display where applicable;
- administrator user-management screens when active SDD requires them.
- administrator team-management screens when active SDD requires them.

Contracts:

- `contracts/http/identity-api.md`

### Partidas

Web flows:

- create and configure partidas;
- configure sequential Trivia and BDT games;
- configure Trivia questions and BDT stages.

Participant gameplay still belongs to React Native mobile.

Contracts:

- `contracts/http/partidas-api.md`
- `contracts/events/partidas-events.md` when asynchronous behavior is approved by SDD.

### Operaciones de Sesion

Web flows:

- view Trivia operator lobby;
- view participants/equipes in lobby;
- start Trivia as operator;
- publish and start partidas when active SDD requires it;
- send BDT clues;
- supervise uploaded treasures and QR validation results;
- view BDT geolocation map;
- cancel Trivia when allowed;
- cancel BDT when allowed.

Participant listing, joining, answering, active-stage and upload views belong to React Native mobile.

Contracts:

- `contracts/http/operaciones-sesion-api.md`
- `contracts/events/operaciones-sesion-events.md`

### Puntuaciones

Web flows:

- supervise Trivia ranking;
- view BDT ranking;
- view consolidated ranking;
- view history/audit details exposed by an approved SDD.

Contracts:

- `contracts/http/puntuaciones-api.md`
- `contracts/events/puntuaciones-events.md`

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
5. the owning service is one of the four approved target services;
6. the target client is React web according to the SDD and actor/client routing rule.
