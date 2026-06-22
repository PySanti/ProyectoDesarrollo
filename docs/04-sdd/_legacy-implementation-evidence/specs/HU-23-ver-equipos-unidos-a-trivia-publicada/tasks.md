# HU-23 — Tasks

## Backend

- [x] Create `GetTriviaGameTeamsQuery`.
- [x] Create `GetTriviaGameTeamsQueryHandler`.
- [x] Reuse `ITriviaInscripcionRepository.ListByPartidaIdAsync`.
- [x] Add `GET /api/trivia-games/{id}/teams` endpoint.
- [x] Map missing game to `404`.
- [x] Keep query read-only.

## Contracts

- [x] Document endpoint in `contracts/http/trivia-game-api.md`.
- [x] Document no RabbitMQ event requirement.

## Tests

- [x] Add application handler tests.
- [x] Add API endpoint test coverage.

## Frontend Integration Pass

- [x] Add React web operator panel for team lobby list.
- [x] Add API client helper if not already present.
- [x] Render loading, empty, error and table states with unified web style.
- [x] Add frontend tests.

## Acceptance and Traceability

- [x] Create SDD files from implemented backend behavior.
- [x] Update `docs/04-sdd/traceability-matrix.md`.
- [x] Align status in `docs/04-sdd/SPECS-LIST.md`.
