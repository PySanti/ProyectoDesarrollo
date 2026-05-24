# Business Rules

## General rules

- The system only supports Trivia and Búsqueda del Tesoro.
- Every session must belong to exactly one game mode.
- A session cannot start without at least one associated team.
- Session state changes must be validated before being applied.
- Users can only access functionality allowed by their role.
- Participants can only access their assigned team and session.
- Ranking must be ordered by accumulated score and tie-breaker criteria.

## Treasure Hunt rules

- A Treasure Hunt session can only be created from an active and valid mission.
- A mission must have a coherent structure.
- Evidence must be associated with participant or team, session, stage/node/objective, date and validation state.
- The same clue must not be released twice to the same team in the same session for the same stage or objective.
- Evidence validation can update progress, score or penalties.

## Trivia rules

- A Trivia session can only be created from a published and valid quiz.
- A valid question must have options, one correct answer, score and time limit.
- Each team can submit only one valid answer per active question.
- Late, repeated or invalid-state answers must be rejected.
- Closing a question updates result, score and ranking.

## Traceability rules

- Every important session event must be recorded.
- Every score movement must have an origin.
- Every penalty must include team, session, reason, moment, responsible user and deducted points.
