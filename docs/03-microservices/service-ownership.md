# Service Ownership

## Identity Service

Owns:

- users;
- roles as local references;
- Keycloak mapping;
- user status;
- local user references.

Does not own:

- teams;
- Trivia games;
- BDT games;
- game scoring;
- game ranking;
- game history.

## Team Service

Owns:

- teams;
- team access codes;
- team members;
- leadership;
- team status;
- team membership rules;
- team cardinality.

### Team cardinality rule

```txt
1 <= members <= 5
```

A team can exist with one member.

The creator is the first member and leader.

Team Service must reject attempts to add a sixth member.

Does not own:

- Trivia forms;
- Trivia games;
- BDT games;
- QR validation;
- game scoring;
- game ranking;
- game answers.

## Trivia Game Service

Owns:

- Trivia forms;
- questions;
- answer options;
- assigned question score;
- question time limits;
- Trivia games;
- Trivia lobby;
- Trivia active participants;
- Trivia answers;
- Trivia scoring;
- Trivia ranking;
- Trivia history/event records;
- Trivia real-time updates.

### Trivia scoring rule

Trivia score does not consider time.

```txt
scoreEarned = question.assignedScore
participant.accumulatedScore += scoreEarned
```

The timer is used for synchronization, closing and late-answer validation, but not for score calculation.

Does not own:

- team master data;
- BDT games;
- QR validation;
- BDT clues;
- BDT geolocation.

## BDT Game Service

Owns:

- BDT games;
- areas;
- stages;
- clues;
- expected QR codes;
- treasure/QR uploads;
- QR validation;
- BDT progress;
- BDT scoring;
- BDT ranking;
- BDT history/event records;
- BDT geolocation updates;
- BDT real-time updates.

Does not own:

- team master data;
- Trivia forms;
- Trivia questions;
- Trivia answers.

## Cross-service rules

- Game services may query Team Service for team, leadership and membership validation.
- Services must not read or write each other databases.
- Cross-service facts must use HTTP, RabbitMQ or SignalR only when justified by SDD and contracts.
