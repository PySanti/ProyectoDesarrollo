# Trivia Game Service — Flow State

## Active feature
HU-30 — Ver ranking durante Trivia

## Test counts
- Domain: 154 pass, 0 fail
- Application: 113 pass (+4 new GetRankingQueryHandlerTests), 0 fail
- API: 58 pass (+3 new TriviaGameRankingControllerTests), 1 fail (pre-existing InMemory isolation)
- Total: 325/326 pass

## Recent changes
- Created SDD folder `docs/04-sdd/specs/HU-30-ver-ranking-durante-trivia/`
- Added `RankingEntryDto`
- Added `GetRankingQuery` + `GetRankingQueryHandler`
- Added endpoint `GET /api/trivia-games/{id}/ranking` in `TriviaGamesPublicController`
- Created SignalR hub `/hubs/trivia-ranking` with `TriviaRankingHub` + `TriviaRankingNotifier`
- Added `ITriviaRankingNotifier` port in Application
- Injected `ITriviaRankingNotifier` into `AnswerTriviaQuestionCommandHandler` to emit `RankingUpdated` after each answer
- Registered hub and notifier in `Program.cs`
- Updated `AnswerTriviaQuestionCommandHandlerTests` to pass the new notifier mock
- Added 4 app handler tests and 3 API integration tests
- Updated `trivia-game-api.md` contract with ranking endpoint and hub
- Updated traceability-matrix.md and SPECS-LIST.md

## Known issues
- `GetAll_NoGames_ReturnsEmptyList` fails because InMemory database is shared across tests within same fixture class

## Next steps
- Frontend operator ranking view (React web)
- Move to next first-sprint feature
