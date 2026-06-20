---
name: ddd-modeling
description: Apply DDD modeling for UMBRAL entities, aggregates, value objects, domain services, domain events and invariants.
compatibility: opencode
---

# DDD Modeling

Base yourself on:

- `docs/02-project-context/domain-model-summary.md`
- `docs/02-project-context/business-rules.md`
- `docs/02-project-context/bdt-ranking-clarification.md`
- `docs/02-project-context/design/domain-entities-by-context.md`
- `docs/02-project-context/design/domain-business-rules.md`
- `docs/03-microservices/service-ownership.md`

## Rules

- Keep domain independent from infrastructure.
- Use aggregates to protect invariants.
- Use value objects for meaningful domain concepts.
- Use domain events for important business facts.
- Do not put business rules in controllers, hubs or EF configurations.
- Do not leak EF Core attributes into domain entities unless explicitly accepted.
- Do not reuse the same `Participante` class across contexts; keep context-specific meanings separate.
- Do not create generic `Mission`, `Session` or `Evidence` aggregates unless the SDD explicitly maps them to current UMBRAL vocabulary.

## Resolved domain decisions

### Team cardinality

A team can exist with 1 to 5 members.

```txt
1 <= Equipo.Participantes.Count <= 5
```

Do not enforce a minimum of 2 members.

The creator is the first member and leader.

### Trivia scoring

Trivia score uses direct accumulation.

```txt
scoreEarned = pregunta.PuntajeAsignado
competidor.PuntajeAcumulado += scoreEarned
```

Do not use remaining time, elapsed time, response time or accumulated response time to calculate score.

The timer remains valid for synchronization, closing and late-answer validation.

Trivia ranking uses:

1. highest accumulated score;
2. if tied, lowest accumulated response time.

### BDT ranking

BDT ranking does not use numeric accumulated score.

BDT ranking uses:

```txt
rankingKey = (EtapasGanadas DESC, TiempoAcumuladoEtapasGanadas ASC)
```

Use these concepts for BDT:

- `EtapasGanadas`
- `TiempoAcumuladoEtapasGanadas`
- `TiempoResolucionEtapa`
- `RankingBDT`

Do not model active BDT ranking through:

- `PuntajeEtapa`
- `PuntajeAcumulado`
- `PuntajeBDTIncrementado`

The BDT service may still record events for traceability, but ranking must be derived from stages won and accumulated time for won stages.

### BDT QR

The expected QR is stored as textual content.

```txt
CodigoQREsperado.Valor
```

The uploaded image is processed so the backend can decode the QR and compare the decoded content with the expected textual content.

### BDT area and geolocation

`AreaBusqueda` is a simple textual description.

Participant `UbicacionGeografica` is used for BDT supervision and is required for active BDT participation according to the SRS.

Do not model advanced geospatial validation, route history or polygon-based area enforcement unless a future SDD adds it.

## Core concepts

- Usuario
- Equipo
- CodigoAcceso
- InscripcionPartida
- Convocatoria
- FormularioTrivia
- Pregunta
- Opcion
- PuntajeAsignado
- PartidaTrivia
- CompetidorTrivia
- RespuestaTrivia
- RankingTrivia
- PartidaBDT
- EtapaBDT
- ExploradorBDT
- TesoroQR
- CodigoQREsperado
- AreaBusqueda
- UbicacionGeografica
- Pista
- RankingBDT
- RegistroAuditoria
- EventoHistorial

## Context-specific participant concepts

Use separate concepts per context:

- `ParticipanteEquipo` in Team context.
- `CompetidorTrivia` in Trivia context.
- `ExploradorBDT` in BDT context.

Do not share one generic participant entity across all contexts.
