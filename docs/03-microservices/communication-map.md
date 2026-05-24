# Communication Map

## Synchronous communication

Use HTTP for direct queries required by a user action.

Examples:

- Trivia Service validates active teams through Team Service.
- Treasure Hunt Service validates active teams through Team Service.
- Frontend queries service APIs through the gateway.
- Participant dashboard queries current session state.

## Asynchronous communication

Use RabbitMQ for cross-service events that should not block the main flow.

Examples:

- SessionCreated
- SessionStateChanged
- TeamAssociatedToSession
- EvidenceSubmitted
- EvidenceValidated
- TriviaAnswerSubmitted
- TriviaQuestionClosed
- ScoreChanged
- RankingUpdated
- AuditEventRegistered

## Real-time communication

Use WebSockets / SignalR for visible live updates.

Examples:

- Ranking updated.
- Session state changed.
- Clue released.
- Timer updated.
- Trivia question activated.
- Evidence validated.
