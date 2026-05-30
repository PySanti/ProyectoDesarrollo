# Trivia Game Service

## Purpose

Trivia Game Service owns the complete Trivia game flow.

It controls:

- Trivia forms;
- questions;
- options;
- assigned scores;
- time limits;
- Trivia game publication;
- lobby;
- player/team registration;
- answer submission;
- answer validation;
- score accumulation;
- ranking;
- Trivia history;
- real-time Trivia updates.

## DDD context

Trivia Context.

## Ownership

Owns:

- FormularioTrivia
- Pregunta
- Opcion
- PuntajeAsignado
- TiempoLimite
- PartidaTrivia
- Trivias.Participante
- RespuestaTrivia
- Trivia scoring
- Trivia ranking
- Trivia event history

Does not own:

- team master data;
- BDT games;
- QR validation;
- BDT clues;
- BDT geolocation.

## Scoring rule

Trivia score does not consider time.

For each correct answer:

```txt
scoreEarned = question.assignedScore
participant.accumulatedScore += scoreEarned
```

The following formula is explicitly rejected:

```txt
scoreEarned = question.assignedScore * (remainingTime / totalTime)
```

## Timer rule

The timer remains valid for:

- synchronizing the active question;
- closing the question;
- rejecting late answers;
- showing countdown information.

The timer must not affect the score.

## Ranking rule

The main ranking is ordered by accumulated score descending.

Tie-breaking must be defined in the related SDD. Do not assume time-based tie-breaking.

## Team modality

In team modality:

- the active Trivia participant represents a team;
- Team Service remains the owner of team membership and leadership;
- Trivia Game Service may query Team Service to validate leadership and active team state;
- a team can have 1 to 5 members.

## First-delivery HUs

- HU-09 Ver partidas de Trivia publicadas
- HU-11 Filtrar partidas de Trivia por modalidad
- HU-13 Advertencia al entrar a Trivia por equipo sin ser líder
- HU-15 Crear formularios de Trivia
- HU-17 Crear y publicar partida de Trivia
- HU-18 Unirse a Trivia individual
- HU-19 Unir equipo a Trivia por equipos
- HU-21 Ver pantalla de espera de Trivia
- HU-22 Ver participantes unidos a Trivia publicada
- HU-23 Ver equipos unidos a Trivia publicada
- HU-24 Iniciar manualmente Trivia
- HU-26 Responder Trivia individual
- HU-27 Responder Trivia por equipo
- HU-28 Ver resultado al cerrar pregunta de Trivia
- HU-29 Calcular puntaje de respuesta en Trivia
- HU-30 Ver ranking durante Trivia
- HU-35 Ver lista de partidas de Trivia publicadas

## Contracts

HTTP:

- `contracts/http/trivia-game-api.md`

Events:

- `contracts/events/trivia-game-events.md`
