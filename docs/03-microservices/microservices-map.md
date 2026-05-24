# UMBRAL Microservices Map

UMBRAL is implemented using physical microservices.

| Service | Responsibility | Persistence |
|---|---|---|
| Identity Service | Keycloak bridge and internal business roles | PostgreSQL metadata |
| Team Service | Teams, access codes and team members | PostgreSQL: Teams, TeamMembers |
| Trivia Service | Trivia design and live execution | PostgreSQL: TriviaQuizzes, TriviaQuestions, TriviaOptions, TriviaActiveSessions, TriviaSubmissions |
| Treasure Hunt Service | Missions, stages, clues, evidence and live operation | PostgreSQL: Missions, Stages, Clues, TreasureSessions, EvidenceSubmissions |
| Scoring Service | Reactive scoring and ranking | PostgreSQL: TeamScores, Leaderboards, ScoreLogs |
| Audit Service | Immutable system and session event log | PostgreSQL: SystemAuditLogs |