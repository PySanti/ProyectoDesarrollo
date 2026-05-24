# Frontend Context

## Responsibility

The frontend is a React web client for administrators, operators and participants.

## Main areas

### Administrator

- Manage users and roles.
- Manage teams.
- Manage Treasure Hunt missions.
- Manage Trivia quizzes.

### Operator

- Create and manage sessions.
- Associate teams to sessions.
- Change session state.
- Release clues.
- Validate evidence.
- Activate and close Trivia questions.
- Monitor ranking and event history.

### Participant

- Access assigned session.
- View timer, score, progress and ranking.
- View enabled clues.
- Submit evidence.
- Submit Trivia answers.
- Reconnect to active sessions.

## Rules

- Do not invent backend endpoints.
- Use contracts from `contracts/http/`.
- Use SignalR/WebSockets only for real-time session updates.
- Respect roles: Administrador, Operador and Participante.
- Keep pages, components, hooks and API clients separated.

## Real-time updates

Use SignalR/WebSockets for:

- Session state changes.
- Ranking updates.
- Timer updates.
- Clue releases.
- Evidence validation updates.
- Trivia question activation and closing.