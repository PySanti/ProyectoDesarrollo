# Create Feature SDD

Use this command when a user story does not yet have a complete SDD folder.

## Input required

- HU ID
- Feature name
- Responsible service, if known

## Mandatory validation before creating anything

1. Use `umbral-context`.
2. Use `sdd-workflow`.
3. Open `docs/04-sdd/SPECS-LIST.md`.
4. Confirm the HU appears in the active specs list.
5. If the HU does not appear in `SPECS-LIST.md`, stop and report that it is outside the active delivery scope.
6. Confirm the owning service is one of:
   - Identity
   - Partidas
   - Operaciones de Sesion
   - Puntuaciones.

## Process

1. Locate the HU in `docs/01-project-source/srs.md`.
2. Identify related RF/RB/RNF requirements.
3. Identify the owning service using:
   - `docs/04-sdd/SPECS-LIST.md`
   - `docs/03-microservices/service-ownership.md`
   - `docs/04-sdd/traceability-matrix.md`
4. Create or update:

```txt
docs/04-sdd/specs/<HU-ID>-<short-name>/
  spec.md
  design.md
  tasks.md
  acceptance.md
```

## Required SDD content

### `spec.md`

Must include:

- HU ID and name;
- source references;
- actor;
- user goal;
- scope;
- out of scope;
- preconditions;
- postconditions;
- business rules;
- related RF/RB/RNF;
- acceptance criteria;
- open questions, only if truly unavoidable.

### `design.md`

Must include:

- owning service;
- supporting services;
- domain entities/value objects involved;
- commands;
- queries;
- events;
- HTTP contracts;
- real-time updates if applicable;
- design patterns applied;
- tests required.

### `tasks.md`

Must include small implementation tasks grouped by layer:

- Domain;
- Application;
- Infrastructure;
- API;
- Contracts;
- Tests;
- Frontend if applicable;
- Acceptance/traceability.

### `acceptance.md`

Must include:

- acceptance checklist;
- manual verification steps;
- automated test evidence placeholders;
- traceability status.

## Rules

- Do not write implementation code.
- Do not modify unrelated features.
- Do not create TODO-only SDD files.
- Mark unavoidable assumptions explicitly.
- Do not invent requirements.
- Do not create specs for deprecated folders.
- Stop after SDD creation and ask for review.
