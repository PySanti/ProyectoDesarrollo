# Service Model Impact

The UML/domain model is global, but implementation uses four physical backend microservices.

## Active physical services

- Identity Service
- Team Service
- Trivia Game Service
- BDT Game Service

## Explicit non-services

The following are not physical backend microservices in the current topology:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

## Resolved domain decisions

### Team cardinality

Team cardinality is owned by Team Service.

```txt
1 <= members <= 5
```

A team can validly exist with one member.

### Trivia scoring

Trivia scoring is owned by Trivia Game Service.

```txt
scoreEarned = question.assignedScore
```

Time does not modify score.

## Rules

- Do not implement all UML classes in a single backend.
- Do not create one global database.
- Do not create one global DbContext.
- Do not directly access another service database.
- Use HTTP only for direct service queries justified by SDD.
- Use RabbitMQ only for asynchronous facts/events justified by SDD.
- Use SignalR/WebSockets only for user-visible real-time updates.

## Impact by service

### Identity Service

Implements:

- user local references;
- Keycloak mapping;
- user role reference;
- user status.

Does not implement:

- teams;
- game sessions;
- Trivia logic;
- BDT logic;
- game ranking;
- game history.

### Team Service

Implements:

- teams;
- team members;
- access codes;
- leadership;
- team status;
- team membership rules;
- 1-to-5 member cardinality.

Does not implement:

- Trivia forms;
- Trivia games;
- BDT games;
- QR validation;
- game ranking;
- game scoring;
- game history.

### Trivia Game Service

Implements:

- Trivia forms;
- questions;
- options;
- Trivia games;
- Trivia lobby;
- Trivia participants;
- Trivia answers;
- direct score accumulation without time weighting;
- Trivia ranking;
- Trivia history;
- Trivia real-time updates.

Does not implement:

- team master data;
- BDT stages;
- QR validation;
- BDT clues;
- BDT geolocation.

### BDT Game Service

Implements:

- BDT games;
- search area;
- stages;
- expected QR codes;
- treasure/QR uploads;
- QR validation;
- clues;
- BDT participants;
- BDT scoring;
- BDT ranking;
- BDT history;
- BDT geolocation;
- BDT real-time updates.

Does not implement:

- Trivia forms;
- Trivia questions;
- Trivia answers;
- team master data.

## Gateway

The gateway, if present, does not own domain logic. It may route or compose calls, but it must not implement business rules.
