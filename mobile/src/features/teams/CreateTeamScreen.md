# CreateTeamScreen (HU-03)

This screen represents the participant flow for team creation in React Native.

## Form fields

- `nombreEquipo` (required)

## Submit behavior

1. Validate `nombreEquipo` locally.
2. Call `POST /api/teams` through `createTeamApi`.
3. On `201`, render created team data (`equipoId`, `codigoAcceso`, `liderUserId`, `integrantes`).
4. On `409`, show participant message:
   - `Ya perteneces a un equipo activo. No puedes crear otro equipo.`

## Backend ownership

- Team Service (`HU-03`)

## Contract reference

- `contracts/http/team-api.md`
