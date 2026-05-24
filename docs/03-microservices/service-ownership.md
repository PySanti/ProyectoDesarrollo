# Service Ownership Rules

## Identity Service

Owns:

- User metadata.
- Business roles.
- Keycloak user mapping.

Does not own:

- Teams.
- Sessions.
- Scores.
- Audit history.

## Team Service

Owns:

- Teams.
- Team members.
- Team status.
- Team access codes.

Does not own:

- Trivia questions.
- Missions.
- Scores.
- Rankings.

## Trivia Service

Owns:

- Quizzes.
- Questions.
- Options.
- Trivia sessions.
- Trivia answers.

Does not own:

- Team master data.
- Global score calculation.
- Audit persistence.
- Treasure Hunt missions.

## Treasure Hunt Service

Owns:

- Missions.
- Stages.
- Nodes.
- Objectives.
- Clues.
- Evidence.
- Treasure Hunt sessions.

Does not own:

- Global ranking.
- Identity.
- Team master data.
- Trivia content.

## Scoring Service

Owns:

- Scores.
- Score movements.
- Ranking.
- Leaderboards.

Does not own:

- Evidence validation.
- Trivia answer correctness.
- Team creation.
- Session state transitions.

## Audit Service

Owns:

- Audit log.
- Historical event trail.
- Session event history.

Does not own:

- Business decisions.
- Score calculation.
- Session state transitions.
