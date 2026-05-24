# Trivia Service

## Responsibility

Owns the design and live execution of Trivia.

## Owns

- Trivia quizzes
- Questions
- Answer options
- Correct answers
- Question score
- Question time limit
- Trivia sessions
- Active questions
- Trivia submissions

## Related stories

- HU-04
- HU-05
- HU-06
- HU-21
- HU-22
- HU-ranking-initial

## Rules

- A Trivia session can only be created from a published and valid quiz.
- A question must have options, one correct answer, score and time limit.
- Each team can submit one answer per active question.
- Repeated, late or invalid-state answers must be rejected.
- Closing a question should trigger score/ranking update events.

## Does not own

- Team master data
- User roles
- Global ranking persistence
- Audit history persistence
- Treasure Hunt missions
