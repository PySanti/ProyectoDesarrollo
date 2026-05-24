# scoring-service Context

## Responsibility

Owns scoring, score movements and ranking.

## Owns

- TeamScores
- ScoreLogs
- Leaderboards

## Related stories

- HU-ranking-initial
- HU-05
- HU-06
- HU-11

## Rules

- Every score change must have an origin.
- Ranking must be updated when score changes.
- Scoring reacts to events from Trivia and Treasure Hunt.

## Does not own

- Evidence validation
- Trivia answer correctness
- Team creation
- Session state transitions
