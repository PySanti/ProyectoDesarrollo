# Domain Business Rules

## General rules

- The system only supports Trivia and Búsqueda del Tesoro.
- Every session must belong to exactly one game mode.
- A session cannot start without at least one associated team.
- Session state changes must follow valid transitions.
- Participants can only access their assigned session and team.
- Every relevant session event must be recorded.

## Treasure Hunt rules

- A Treasure Hunt session can only be created from an active and valid mission.
- Evidence must not be accepted if the session is paused, finalized or cancelled.
- Evidence must be associated with participant/team, session and stage/objective.
- A clue cannot be released twice to the same team for the same stage/objective.

## Trivia rules

- A Trivia session can only be created from a published and valid quiz.
- Each team can submit only one valid answer per active question.
- Repeated, late or invalid-state answers must be rejected.
- Closing or validating a question can update score and ranking.

## Scoring and audit rules

- Every score change must have traceability of origin.
- Ranking must be ordered from highest to lowest score.
- Tie-breakers must follow the criterion defined for the session.
- Every penalty must record reason, moment and responsible user.