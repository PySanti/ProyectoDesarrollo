---
name: rabbitmq-events
description: Design and implement RabbitMQ integration events for UMBRAL cross-service asynchronous workflows using the four-service topology.
compatibility: opencode
---

# RabbitMQ Events

Base yourself on:

- `docs/05-decisions/ADR-0006-four-service-topology.md`
- `docs/03-microservices/events-catalog.md`
- `contracts/events/`

## Valid event contract files

Use only:

- `contracts/events/identity-events.md`
- `contracts/events/team-events.md`
- `contracts/events/trivia-game-events.md`
- `contracts/events/bdt-game-events.md`

Do not use:

- `contracts/events/audit-events.md`
- `contracts/events/scoring-events.md`
- `contracts/events/session-events.md`
- `contracts/events/trivia-events.md`
- `contracts/events/treasure-hunt-events.md`

## Rules

- Events must be versioned.
- Events must be defined under the event file of the owning service.
- Services publish only events they own.
- Consumers must be idempotent where possible.
- Events represent facts that already happened.
- Do not use RabbitMQ for direct user-facing queries.
- Do not put business decisions in consumers that belong to another service's domain.
- Do not create Audit Service or Scoring Service events as standalone service contracts.
- Do not use generic `Mission`, `Session` or `Evidence` event names unless the SDD explicitly maps them to current UMBRAL vocabulary.

## Event naming style

Use past-tense names tied to the owning service context.

Recommended examples:

- `TeamCreated`
- `TeamMemberJoined`
- `TeamLeadershipTransferred`
- `TeamDeleted`
- `TriviaGameCreated`
- `TriviaLobbyPublished`
- `TriviaAnswerSubmitted`
- `TriviaAnswerValidated`
- `TriviaScoreIncremented`
- `TriviaQuestionClosed`
- `TriviaRankingUpdated`
- `TreasureGameCreated`
- `TreasureLobbyPublished`
- `TreasureQrSubmitted`
- `TreasureQrValidated`
- `BdtStageWon`
- `BdtStageClosed`
- `BdtRankingUpdated`
- `BdtClueSent`
- `BdtGeolocationUpdated`

## Trivia scoring events

Trivia may publish score-related events because Trivia uses numeric score accumulation.

Valid concepts:

- `PuntajeAsignado`
- `PuntajeAcumulado`
- `PuntajeTriviaIncrementado`
- `RankingTriviaActualizado`

## BDT ranking events

BDT must not publish events implying numeric score accumulation for ranking.

Avoid as active BDT event names:

- `PuntajeBDTIncrementado`
- `BdtScoreIncremented`
- `PuntajeEtapaAsignado`

Use instead:

- `EtapaBDTGanada`
- `BdtStageWon`
- `RankingBDTActualizado`
- `BdtRankingUpdated`

BDT ranking is based on stages won and accumulated time for won stages.
