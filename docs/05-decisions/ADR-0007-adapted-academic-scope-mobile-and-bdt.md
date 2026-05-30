# ADR-0007 — Adapted Academic Scope: Trivia, BDT, Mobile Participant App and BDT Geolocation

## Status

Accepted

## Context

The academic brief describes UMBRAL as a web platform for immersive investigation missions and live sessions. It emphasizes:

- real-time operation;
- teams;
- responses/evidence;
- scoring/ranking;
- traceability;
- WebSockets;
- RabbitMQ;
- CQRS/MediatR;
- PostgreSQL/EF Core;
- Clean/Hexagonal Architecture.

The team SRS adapts this general mission/session concept into two concrete game modes:

- Trivia;
- Búsqueda del Tesoro / BDT.

The SRS also introduces a React Native mobile app for participant flows, while the web React app remains focused on administrators and operators.

The academic brief mentions that native mobile applications and precise geolocation are outside the original advanced scope. This ADR documents the team’s interpretation and bounded adaptation.

## Decision

UMBRAL will be implemented as a system with:

1. React web application for:
   - Administrador;
   - Operador.

2. React Native mobile application for:
   - Participante;
   - Líder de equipo when acting as participant.

3. Backend as physical microservices:
   - Identity Service;
   - Team Service;
   - Trivia Game Service;
   - BDT Game Service.

4. Two concrete game modes:
   - Trivia;
   - Búsqueda del Tesoro / BDT.

5. BDT geolocation limited to:
   - active BDT session supervision;
   - current participant location;
   - no historical route analytics;
   - no advanced geospatial processing.

## Rationale

This adaptation preserves the academic goals of the brief while making the product more concrete and implementable.

Mapping:

| Academic brief concept | UMBRAL adaptation |
|---|---|
| Mission | Trivia form / BDT configuration |
| Mission nodes/stages | Trivia questions / BDT stages |
| LiveSession | PartidaTrivia / PartidaBDT |
| EvidenceSubmission | RespuestaTrivia / TesoroQR |
| Team | Equipo |
| SessionEvent | EventoHistorial |
| ScoreEntry | Trivia score / BDT ranking event |
| Panel del equipo | React Native participant app |
| Panel del operador | React web operator console |

## Consequences

- Participant stories must be implemented in React Native unless an SDD explicitly says otherwise.
- Administrator/operator stories must be implemented in React web unless an SDD explicitly says otherwise.
- BDT geolocation must remain operational and bounded.
- The project must clearly justify this adaptation in documentation and presentation.
- The adaptation must not create extra backend microservices.

## Non-goals

This ADR does not approve:

- advanced geospatial analytics;
- route history;
- native platform-specific apps outside React Native;
- additional game modes;
- extra physical microservices;
- Audit Service, Scoring Service, Notification Service, Trivia Service or Treasure Hunt Service as active services.
