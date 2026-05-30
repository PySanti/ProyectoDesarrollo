# Known Ambiguities and Decisions

This file records ambiguities detected in the source material and the decisions adopted by the project setup.

## Source priority

When sources conflict, use this priority:

1. Explicit user decisions recorded in this file.
2. `docs/05-decisions/` ADRs.
3. `docs/01-project-source/srs.md`.
4. `docs/01-project-source/modelo de dominio.md`.
5. `docs/01-project-source/diagrama de clases.md`.
6. Operational summaries under `docs/02-project-context/`.
7. OpenCode commands, agents and skills.

## 1. Microservice topology

### Status

Resolved.

### Decision

UMBRAL uses four physical backend microservices:

- Identity Service
- Team Service
- Trivia Game Service
- BDT Game Service

The following must not be implemented as active physical backend services:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

### Reference

```txt
docs/05-decisions/ADR-0006-four-service-topology.md
```

## 2. Team cardinality

### Status

Resolved.

### Decision

A team can exist with 1 to 5 members.

```txt
1 <= Equipo.Participantes.Count <= 5
```

The creator of the team is automatically registered as:

- first team member;
- team leader.

### Consequences

- HU-03 creates a valid team with exactly one member: the creator.
- HU-04 can add members until the team reaches 5 members.
- The system must reject attempts to add a sixth member.
- The system must not reject a team just because it has one member.
- If a non-leader leaves, the participant is removed directly.
- If the leader leaves and there are other members, leadership must be transferred first.
- If the leader leaves and is the only member, the team is deleted.
- A team with one member may participate where the game modality and SDD allow it.

### Applies to

- HU-03
- HU-04
- HU-05
- HU-06
- HU-07
- HU-13
- HU-14
- HU-19
- HU-40

## 3. Trivia scoring

### Status

Resolved.

### Decision

Trivia score does not consider time.

When a Trivia answer is correct, the system adds the assigned score of the question directly to the participant accumulated score.

```txt
scoreEarned = question.assignedScore
participant.accumulatedScore += scoreEarned
```

Do not use this formula:

```txt
scoreEarned = question.assignedScore * (remainingTime / totalTime)
```

### Consequences

- `TiempoLimite` still exists.
- The timer still controls question visibility, closing and late-answer validation.
- Time is not part of score calculation.
- Remaining time must not multiply or modify the score.
- Elapsed time must not multiply or modify the score.
- Accumulated response time must not multiply or modify the score.
- The main ranking is ordered by accumulated score descending.
- Tie-breaking must be explicitly defined in the related SDD. Do not assume time-based tie-breaking.

### Applies to

- HU-26
- HU-27
- HU-28
- HU-29
- HU-30

## 4. Remaining open questions

No currently known microservice, team-cardinality or Trivia-scoring ambiguity remains open.

Future SDD files may define local details such as:

- endpoint paths;
- event payloads;
- ranking tie-breaking;
- UI wording;
- exact validation/error response shape.
