---
name: websocket-signalr
description: Apply SignalR/WebSocket patterns for UMBRAL real-time lobby, partida, ranking, clue, stage and event updates.
compatibility: opencode
---

# WebSocket / SignalR

Base yourself on:

- `docs/02-project-context/srs-summary.md`
- `docs/03-microservices/events-catalog.md`
- `contracts/events/`
- the related SDD feature folder

## Preferred vocabulary

Use current UMBRAL vocabulary:

- partida
- lobby
- Trivia
- BDT
- pregunta
- respuesta
- etapa
- tesoro QR
- pista
- ranking
- geolocalización

Avoid generic academic-base vocabulary unless explicitly mapped by the SDD:

- mission
- generic session
- evidence

## Use real-time updates for

- published game changes;
- lobby participant changes;
- partida state changes;
- timer updates;
- Trivia question activation and closing;
- Trivia answer result updates;
- ranking updates;
- BDT stage changes;
- clue releases;
- QR validation result updates;
- participant reconnection state;
- BDT geolocation updates where approved by the SDD.

## Valid services

SignalR/WebSocket updates must be owned by one of:

- Trivia Game Service
- BDT Game Service
- Team Service, only when team membership updates are explicitly real-time in the SDD
- Identity Service, only when explicitly required by SDD

Do not create real-time responsibilities for:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

## Rules

- Do not use SignalR as a replacement for persistence.
- Do not put business rules inside hubs.
- Hubs notify clients about state changes already accepted by the backend.
- Hub payloads must match approved contracts.
- Authorization must respect roles and participant/partida access.
- Real-time events must be traceable to the SDD and contracts.
