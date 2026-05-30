# Domain Business Rules

## Team Service

### TEAM-001 — Team cardinality

A team must have from 1 to 5 members.

```txt
1 <= Equipo.Participantes.Count <= 5
```

Do not enforce a minimum of 2 members.

### TEAM-002 — Creator as leader

When a participant creates a team:

- the team is created as active;
- the creator is added as first member;
- the creator is marked as leader;
- an access code is generated.

### TEAM-003 — Maximum members

Team Service must reject attempts to add a sixth member.

### TEAM-004 — One active team per participant

A participant cannot belong to more than one active team.

### TEAM-005 — Non-leader exit

A non-leader participant can leave the team directly.

### TEAM-006 — Leader exit with other members

If the leader wants to leave and other members exist, the leader must transfer leadership before leaving.

### TEAM-007 — Leader exit as only member

If the leader wants to leave and is the only member, the team is deleted.

## Trivia Game Service

### TRIVIA-FORM-001 — Complete form

A Trivia form must include:

- at least one question;
- answer options;
- one correct answer per question;
- assigned score per question;
- time limit per question.

### TRIVIA-ANSWER-001 — One definitive answer

A participant can have only one definitive answer per active question.

In team modality, the active participant is the team, so the first answer submitted for the team is definitive.

### TRIVIA-ANSWER-002 — Late answers

An answer submitted after the active question closes must be rejected.

### TRIVIA-SCORE-001 — Direct score accumulation

When a correct answer is validated, the earned score equals the assigned score of the question.

```txt
scoreEarned = question.assignedScore
```

### TRIVIA-SCORE-002 — Accumulated score

The earned score is added directly to the participant accumulated score.

```txt
participant.accumulatedScore += scoreEarned
```

### TRIVIA-SCORE-003 — Time does not affect score

The following values must not modify score:

- remaining time;
- elapsed time;
- response time;
- accumulated response time;
- total question time.

### TRIVIA-SCORE-004 — Timer validity

The timer remains part of the domain to:

- show countdown;
- close questions;
- reject late answers;
- synchronize clients.

The timer is not part of score calculation.

### TRIVIA-RANKING-001 — Ranking order

Trivia ranking is ordered by accumulated score descending.

### TRIVIA-RANKING-002 — Tie-breaking

Tie-breaking must be explicitly defined in the related SDD. Do not assume time-based tie-breaking.

## BDT Game Service

### BDT-QR-001 — Expected QR

Each active stage has an expected QR value.

### BDT-QR-002 — QR validation

A treasure QR submission is valid only when the decoded value matches the expected QR of the active stage.

### BDT-STAGE-001 — Stage progression

Stage progression is controlled by BDT Game Service according to the active SDD.

### BDT-SCORE-001 — BDT score ownership

BDT score and ranking belong to BDT Game Service.

## Cross-context rules

### CROSS-001 — Team validation

Game services may consult Team Service to validate:

- team existence;
- active state;
- membership;
- leadership;
- team cardinality constraints.

### CROSS-002 — No shared database

No service may read or write another service database directly.
