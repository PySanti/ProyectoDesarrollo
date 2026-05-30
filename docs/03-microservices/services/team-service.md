# Team Service

## Purpose

Team Service owns global team management for UMBRAL.

It controls:

- team creation;
- access code generation/validation;
- team membership;
- leadership;
- leaving teams;
- leadership transfer;
- team deletion/deactivation;
- team cardinality.

## DDD context

Team Context.

## Ownership

Owns:

- Equipo
- Equipos.Participante
- EquipoId
- NombreEquipo
- CodigoAcceso
- EstadoEquipo
- leadership state
- membership rules

Does not own:

- Trivia games;
- BDT games;
- game scoring;
- game ranking;
- QR validation;
- Trivia answers.

## Cardinality rule

A team can have from 1 to 5 members.

```txt
1 <= members.Count <= 5
```

The creator is the first member and leader.

The service must reject attempts to add a sixth member.

The service must not reject a team just because it has only one member.

## Main business rules

- A participant can belong to only one active team.
- A participant joins a team using a valid access code.
- A non-leader can leave directly.
- A leader with other members must transfer leadership before leaving.
- A leader who is the only member causes the team to be deleted when leaving.
- A disabled/deleted team cannot be registered in new games.

## First-delivery HUs

- HU-03 Crear equipo
- HU-04 Unirse a equipo usando código
- HU-05 Eliminar equipo creado
- HU-06 Transferir liderazgo antes de salir del equipo
- HU-07 Salir del equipo

## Contracts

HTTP:

- `contracts/http/team-api.md`

Events:

- `contracts/events/team-events.md`
