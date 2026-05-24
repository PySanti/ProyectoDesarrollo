# Sistema UMBRAL

# Lista de Microservicios Propuestos

| N° | Microservicio | Responsabilidad principal | Persistencia |
|---:|---|---|---|
| 1 | Usuarios y Roles (Identity Service) | Puente con Keycloak y perfiles internos del negocio. | PostgreSQL ligera para metadata extendida. |
| 2 | Gestión de Equipos (Team Service) | Equipos, códigos de acceso y miembros de equipo. | PostgreSQL: Teams, TeamMembers. |
| 3 | Trivia (Trivia Service) | Diseño y ejecución en vivo de trivias. | PostgreSQL: TriviaQuizzes, TriviaQuestions, TriviaOptions, TriviaActiveSessions, TriviaSubmissions. |
| 4 | Búsqueda del Tesoro (Treasure Hunt Service) | Misiones, etapas, pistas, evidencias y operación en vivo. | PostgreSQL: Missions, Stages, Clues, TreasureSessions, EvidenceSubmissions. |
| 5 | Puntuación y Ranking (Scoring Service) | Cálculo reactivo de puntajes y actualización del ranking. | PostgreSQL: TeamScores, Leaderboards, ScoreLogs. |
| 6 | Auditoría e Historial (Audit Service) | Bitácora inmutable de eventos del sistema. | PostgreSQL: SystemAuditLogs. |
