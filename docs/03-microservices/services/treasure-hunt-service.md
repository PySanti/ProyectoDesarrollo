# Treasure Hunt Service

## Responsibility

Owns missions, stages, clues, evidence and live Treasure Hunt sessions.

## Owns

- Missions
- Stages
- Nodes
- Objectives
- Clues
- Evidence
- Treasure Hunt sessions

## Related stories

- HU-04
- HU-05
- HU-06
- HU-13
- HU-14
- HU-ranking-initial

## Rules

- A Treasure Hunt session can only be created from an active and valid mission.
- Evidence must be associated with session, team and objective/stage.
- A clue must not be released twice to the same team for the same target.
- Evidence validation should trigger score/ranking events.

## Does not own

- Trivia content
- Global score persistence
- Audit persistence
- User roles
