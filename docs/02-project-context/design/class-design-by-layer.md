# Class Design by Layer

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

## Purpose

Maps the domain/class design to the standardized layered implementation expected in each of the four target services.

## Active backend services

The only valid physical backend services are:

- Identity
- Partidas
- Operaciones de Sesion
- Puntuaciones

All four sit behind a **mandatory YARP gateway** (single entry point; validates the Keycloak JWT and authorizes by base role at the route level; owns no domain logic or DB).

> Obsolete (superseded): do not create or reference `Team Service`, `Trivia Game Service`, `BDT Game Service`, Audit Service, Scoring Service (separate from Puntuaciones), or Notification Service as physical services.

## Standardized layering (all four services)

Every service follows the same 4-project layout under `services/<service>/src/`:

```txt
*.Domain
*.Application
*.Infrastructure
*.Api
```

### Domain layer

Contains entities, aggregate roots, value objects, enums, domain services, domain events, invariant methods, **and the infrastructure interfaces** (e.g. repository interfaces) so infrastructure depends on the domain, never the reverse. Must not depend on EF Core, ASP.NET controllers, SignalR hubs, RabbitMQ client implementations, external HTTP clients, or database details.

### Application layer

Contains strictly: `Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `Handlers/`, `Handlers/Commands/`, `Handlers/Queries/`. Commands mutate state; queries do not; handlers coordinate use cases; business invariants stay in domain objects or domain services.

### Infrastructure layer

Contains `persistence/` and `services/`: EF Core DbContext and configurations, repository implementations (of Domain-defined interfaces), RabbitMQ publishers/consumers, SignalR adapters when applicable, HTTP clients to other services, Keycloak integration adapters.

### Api layer

`Program.cs` must not build/register controllers inline. A dedicated `Controllers/` folder holds controllers that inherit from `BaseController`, define their own routes, dispatch through MediatR (`_mediator.Send(...)`), and contain no business rules. Every controller has unit tests. Centralized exception handling in every service.

## Identity

### Domain

- Usuario; KeycloakId, Correo, RolUsuario, EstadoUsuario, EstadoCredencial.
- Rol; Privilegio, PermisoFuncional.
- Equipo, ParticipanteEquipo; EquipoId, NombreEquipo, EstadoEquipo.
- InvitacionEquipo; EstadoInvitacion. HistorialEquipoUsuario.

Team aggregate invariant:

```txt
1 <= Equipo.Participantes.Count <= 5
```

The team creator is the first participant and is marked as leader. Do not enforce a minimum of 2. Members join only via `InvitacionEquipo` (no access code).

### Application

User creation/edit/deactivation and role modification; per-role governance/permissions; team creation, invitation, leadership transfer, deletion; queries.

### Infrastructure

Local user/team persistence; Keycloak adapter; RabbitMQ publisher for `UsuarioCreado` / `CredencialTemporalEmitida` and team/invitation events; EF Core configuration.

### Api

User, role/governance, and team endpoints (see `contracts/http/`).

## Partidas

### Domain

- Partida; PartidaId, NombrePartida, TiempoInicio, MinimosParticipacion, MaximosParticipacion, RankingConsolidado; EstadoPartida, Modalidad, ModoInicioPartida.
- Juego (base) + JuegoTrivia / JuegoBDT **configuration**; Pregunta, Opcion, PuntajeAsignado, TiempoLimite; EtapaBDT (CodigoQREsperado, Puntaje, TiempoLimite), AreaBusqueda.

### Application

Create/configure partidas and their sequential games; add Trivia questions (with the game); add BDT stages; validate publishability.

### Infrastructure

Configuration persistence; EF Core; RabbitMQ publisher for `PartidaCreada` when required.

### Api

Partida and game configuration endpoints (see `contracts/http/`).

## Operaciones de Sesion

### Domain

- Runtime state for JuegoTrivia/JuegoBDT sessions; ParticipanteTrivia, RespuestaTrivia, ParticipanteBDT, TesoroQR (transient), Pista, UbicacionGeografica.
- InscripcionPartida + Convocatoria; EstadoInscripcion, EstadoConvocatoria.

### Application

Publish → Lobby; manual/automatic start; question/stage synchronization; answer & QR validation; sequential advance; clue delivery; geolocation; reconnection; inscriptions and convocatorias.

### Infrastructure

Transient session persistence; QR/image decoding adapter; RabbitMQ publisher for runtime domain events; SignalR/WebSocket adapter for real-time updates (routed through the gateway).

### Api

Live session, inscription, and convocatoria endpoints, plus SignalR hubs (see `contracts/`).

## Puntuaciones

### Domain

- Projection models for scores and won stages; RankingTrivia, RankingBDT, RankingConsolidado.
- RegistroAuditoria, EventoHistorial, TipoEventoHistorial.

### Application

Consume RabbitMQ events to update scores and native rankings during play; compute the consolidated ranking on finish; team-performance queries; audit/history materialization.

### Infrastructure

Read/projection persistence; RabbitMQ consumers; SignalR adapter to broadcast ranking/score updates (through the gateway).

### Api

Ranking, team-performance, and history/audit query endpoints (see `contracts/`).
