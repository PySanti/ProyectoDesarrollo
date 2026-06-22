# Update Traceability

Use this command after a feature or task changes status.

## Files to read

- `docs/04-sdd/SPECS-LIST.md`
- related SDD folder under `docs/04-sdd/specs/`
- `docs/04-sdd/traceability-matrix.md`

## File to update

```txt
docs/04-sdd/traceability-matrix.md
```

## Mandatory validation

1. Confirm the HU appears in `docs/04-sdd/SPECS-LIST.md`.
2. Confirm the SDD folder is not deprecated.
3. Confirm the owning service is one of:
   - Identity
   - Partidas
   - Operaciones de Sesion
   - Puntuaciones.
4. Confirm acceptance evidence exists before marking a feature Done.
5. Confirm tests exist before marking implemented business rules Done.

## Required columns

- HU
- Feature
- Requirement
- Owning service
- Supporting services
- SDD folder
- Contract files
- Status

## Rules

- Do not mark a feature as Done without acceptance evidence.
- Do not mark a feature as Done without tests for business rules.
- Do not invent requirement IDs.
- Trace every implemented endpoint/event to the SDD.
- Do not add rows for inactive specs.
