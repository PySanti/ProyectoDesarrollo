# Team Service Context

## Responsibility

Manages teams, team members, access codes, team status and leadership.

## Owns

- Equipo
- Equipos.Participante
- CodigoAcceso
- EstadoEquipo
- team membership rules
- team leadership rules

## Related stories

- HU-03
- HU-04
- HU-05
- HU-06
- HU-07
- HU-08

## Does not own

- Users as identity records
- Trivia content
- Trivia sessions
- BDT sessions
- QR validation
- Game scoring
- Game ranking
- Game history

## Notes

Game services may query Team Service to validate leadership, membership or active team status through explicit HTTP contracts.
