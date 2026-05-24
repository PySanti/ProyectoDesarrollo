# First Delivery Scope

| User story | Main service | Supporting services |
|---|---|---|
| HU-01 Users and roles | Identity Service | Keycloak |
| HU-02 Team management | Team Service | - |
| HU-03 Team members | Team Service | Identity Service |
| HU-04 Create session by mode | Trivia Service / Treasure Hunt Service | - |
| HU-05 Associate teams to session | Trivia Service / Treasure Hunt Service | Team Service |
| HU-06 Session state control | Trivia Service / Treasure Hunt Service | Audit Service |
| HU-07 Participant session access | Identity Service | Team Service + active game service |
| HU-11 Session event history | Audit Service | Trivia / Treasure Hunt / Scoring |
| HU-13 Treasure missions | Treasure Hunt Service | - |
| HU-14 Treasure mission structure | Treasure Hunt Service | - |
| HU-21 Trivia questions | Trivia Service | - |
| HU-22 Trivia session from quiz | Trivia Service | Team Service |
| Ranking inicial | Scoring Service | Trivia / Treasure Hunt |
