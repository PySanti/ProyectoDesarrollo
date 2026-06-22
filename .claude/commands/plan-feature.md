# Plan Feature

Use this command before implementing any UMBRAL feature.

## Required input

- HU ID or SDD folder
- Feature name, if needed

## Mandatory validation

1. Use `umbral-context`.
2. Use `sdd-workflow`.
3. Open `docs/04-sdd/SPECS-LIST.md`.
4. Confirm the HU/folder appears in the active specs list.
5. Confirm the SDD folder is not under `docs/04-sdd/specs/_deprecated/`.
6. Confirm the owning service is one of:
   - Identity
   - Partidas
   - Operaciones de Sesion
   - Puntuaciones.
7. If any validation fails, stop and report the blocker.

## Required steps

1. Read the related SDD folder.
2. Check `spec.md`, `design.md`, `tasks.md`, and `acceptance.md`.
3. Use `contract-design` if the feature touches endpoints, DTOs, events or real-time updates.
4. Read related service context under `services/<service>/service-context.md`.
5. Identify files likely to be modified.
6. Identify tests required.
7. Produce an implementation plan without coding.

## Output required

- Feature / HU
- SDD folder
- Owning service
- Supporting services
- Files to modify
- Contracts to modify
- Tests to write
- First task to implement
- Risks or blockers
- Whether implementation is allowed now

## Rules

- Do not code.
- Do not modify files unless explicitly asked.
- Do not plan from deprecated specs.
- Do not plan a feature with unresolved TODO in SDD.
- Do not invent endpoints or events.
