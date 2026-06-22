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
- `contracts/events/partidas-events.md`
- `contracts/events/operaciones-sesion-events.md`
- `contracts/events/puntuaciones-events.md`

Do not use (obsolete / superseded — archived under `contracts/events/_legacy/` or never valid):

- `contracts/events/team-events.md`
- `contracts/events/trivia-game-events.md`
- `contracts/events/bdt-game-events.md`
- `contracts/events/audit-events.md`
- `contracts/events/scoring-events.md`
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
- Do not create events for the obsolete / superseded services (Team Service, Trivia Game Service, BDT Game Service, Treasure Hunt Service, Audit Service, Scoring Service or Notification Service) as standalone service contracts.
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

BDT ranking is based on accumulated points from won stages (sum of each won stage's `Puntaje`), tie-broken by the lowest accumulated time of the won stages. `EtapaBDTGanada` carries the stage `Puntaje`; `RankingBDTActualizado` broadcasts the recomputed order. See `docs/02-project-context/bdt-ranking-clarification.md`.

Use these BDT event names:

- `EtapaBDTGanada` (carries stage `Puntaje`)
- `BdtStageWon`
- `RankingBDTActualizado`
- `BdtRankingUpdated`

Avoid obsolete BDT event names (points now flow via `EtapaBDTGanada`'s `Puntaje` field, not as separate increment events):

- `PuntajeBDTIncrementado`
- `BdtScoreIncremented`
- `PuntajeEtapaAsignado`
