---
name: cqrs-mediatr
description: Apply CQRS and MediatR conventions for UMBRAL .NET microservices.
compatibility: opencode
---

# CQRS + MediatR

Base yourself on:

- `docs/02-project-context/design/class-design-by-layer.md`
- `docs/04-sdd/SPECS-LIST.md`
- the related SDD feature folder

## Rules

- Commands change state.
- Queries never change state.
- Controllers or endpoints call MediatR.
- Handlers coordinate application flow.
- Domain enforces business invariants.
- Validators validate input and application preconditions.
- Integration events are published after successful state changes when required by SDD.
- Queries should return DTOs or read models, not aggregates.

## Command/query folder pattern

Each command/query should include, when applicable:

- request object;
- handler;
- validator;
- response / DTO;
- tests.

## Naming examples

- `CreateTeamCommand`
- `AcceptTeamInvitationCommand`
- `GetPublishedPartidasQuery`
- `SubmitTriviaAnswerCommand`
- `ValidateTreasureQrCommand`
