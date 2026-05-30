# Domain Entities by Context

## Active physical service topology

UMBRAL uses four physical backend microservices:

- Identity Service
- Team Service
- Trivia Game Service
- BDT Game Service

## Identity Service

Owns:

- Usuario
- KeycloakId
- RolUsuario
- EstadoUsuario
- local user references

Does not own:

- teams
- Trivia games
- BDT games
- game scoring
- game ranking
- game history

## Team Service

Owns:

- Equipo
- Equipos.Participante
- EquipoId
- NombreEquipo
- CodigoAcceso
- EstadoEquipo
- team membership
- leadership

Main invariant:

```txt
1 <= Equipo.Participantes.Count <= 5
```

The creator of a team is the first participant and leader.

Team Service does not own:

- Trivia forms
- Trivia games
- BDT games
- game scoring
- game ranking
- game answers
- QR validation

## Trivia Game Service

Owns:

- FormularioTrivia
- Pregunta
- Opcion
- PuntajeAsignado
- TiempoLimite
- PartidaTrivia
- Trivias.Participante
- RespuestaTrivia
- Trivia inscriptions
- Trivia convocations
- Trivia score
- Trivia ranking
- Trivia history/event records
- Trivia real-time updates

Scoring rule:

```txt
if respuesta.EsCorrecta:
    participante.PuntajeAcumulado += pregunta.PuntajeAsignado
```

Time rule:

- `TiempoLimite` determines availability and late-answer validation.
- Time does not modify score.
- Time must not be used in the score formula.

Optional auxiliary data:

- `TiempoRespuestaAcumulado` may be recorded for history, telemetry or UI.
- `TiempoRespuestaAcumulado` must not affect score unless a future ADR changes this decision.

Trivia Game Service does not own:

- team master data;
- BDT games;
- QR validation;
- BDT clues;
- BDT geolocation.

## BDT Game Service

Owns:

- PartidaBDT
- EtapaBDT
- Bdt.Participante
- TesoroQR
- Pista
- AreaBusqueda
- UbicacionGeografica
- CodigoQREsperado
- PuntajeEtapa
- EstadoEtapa
- ResultadoValidacionQR
- BDT inscriptions
- BDT convocations
- BDT score
- BDT ranking
- BDT history/event records
- BDT real-time updates

BDT Game Service does not own:

- team master data;
- Trivia forms;
- Trivia questions;
- Trivia answers.

## Transversal concepts

### InscripcionPartida

Ownership depends on game mode:

- Trivia inscription belongs to Trivia Game Service.
- BDT inscription belongs to BDT Game Service.

### Convocatoria

Ownership depends on game mode:

- Trivia team convocation belongs to Trivia Game Service.
- BDT team convocation belongs to BDT Game Service.

Team Service remains the owner of team membership and leadership validation.

### RegistroAuditoria / EventoHistorial

There is no physical Audit Service in the current topology.

History is owned by the service that owns the business flow:

- Trivia history belongs to Trivia Game Service.
- BDT history belongs to BDT Game Service.
- Team history belongs to Team Service.
- Identity/user history belongs to Identity Service.

### Puntaje / Ranking

There is no physical Scoring Service in the current topology.

Scoring and ranking are owned by the service that owns the game flow:

- Trivia scoring/ranking belongs to Trivia Game Service.
- BDT scoring/ranking belongs to BDT Game Service.
