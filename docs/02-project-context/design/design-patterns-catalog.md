# Design Patterns Catalog — UMBRAL

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

This catalog defines how to evidence design patterns without over-engineering, applied across the four target services (Identity, Partidas, Operaciones de Sesion, Puntuaciones).

## General rule

Do not use patterns for decoration. A pattern must solve a real design problem, reduce coupling, encapsulate variation, or evidence a defensible academic decision.

## Mandatory architectural patterns

| Pattern | Use in UMBRAL | Evidence |
|---|---|---|
| CQRS | Separate writes and reads. | Commands, Queries, Handlers (`Handlers/Commands/`, `Handlers/Queries/`). |
| Mediator | Orchestrate use cases. | MediatR handlers invoked by controllers inheriting from `BaseController`. |
| Repository | Abstract persistence. | Interfaces defined in `Domain/`, EF Core implementations in `Infrastructure/persistence/`. |
| Adapter / Ports | Isolate infrastructure. | Adapters for PostgreSQL, RabbitMQ, SignalR, Keycloak, QR decoder. |
| Dependency Injection | Invert dependencies. | Per-layer service/interface registration. |

## Recommended tactical patterns

| Pattern | When to use | Example |
|---|---|---|
| Factory Method | Create aggregates with invariants. | `Equipo.Crear(...)`, `Partida.Crear(...)`, `JuegoTrivia` / `JuegoBDT` creation. |
| Strategy | Interchangeable algorithms. | Ranking criteria (Trivia by points, BDT by won-stage points). |
| State | Complex partida/game/stage transitions. | `EstadoPartida`, `EstadoJuego`, `EstadoEtapa` if logic grows. |
| Specification | Combinable validations. | Validate inscription: `Lobby` state + cupo + leadership + active team + one-active-participation. |
| Domain Event | Domain facts. | `RespuestaTriviaValidada`, `EtapaBDTGanada`, `RankingConsolidadoCalculado`. |
| Observer / PubSub | Real-time updates. | SignalR hubs for ranking/lobby/stages/clues/geolocation. |
| Outbox | Reliable event publication. | Persist event + publish to RabbitMQ after commit. |
| Unit of Work | Persist aggregate changes and events. | EF Core DbContext. |
| Result Pattern | Domain responses without exceptions for expected flow. | Invalid QR, late answer, full cupo. |

## Patterns by HU type

### Identity (users, roles, teams)

| HU | Suggested patterns |
|---|---|
| Create user / temporary credential | Factory Method, Domain Event (`UsuarioCreado` / `CredencialTemporalEmitida`), Repository. |
| Modify role / per-role governance | Specification or domain service, Domain Event, Repository. |
| Create team | Factory Method (`Equipo.Crear`), Repository, CQRS/Mediator. |
| Invite member / accept invitation | Specification or aggregate validation, Domain Event, Repository. |
| Leave / transfer leadership / delete team | State or aggregate methods, Domain Event for notification, Repository. |

### Partidas (configuration)

| HU | Suggested patterns |
|---|---|
| Create partida with sequential games | Factory Method, Composite (Partida–Juego). |
| Add Trivia game with its questions | Composite (JuegoTrivia–Pregunta–Opcion), Specification for completeness. |
| Add BDT game with its stages | Composite (JuegoBDT–EtapaBDT), Specification for valid stages. |

### Operaciones de Sesion (runtime)

| HU | Suggested patterns |
|---|---|
| Publish / start partida | State, Domain Event, Specification for minimums. |
| Answer Trivia question | Command Handler, Domain Event, Strategy if variants. |
| Validate QR | Adapter for QR decoder, Strategy if multiple methods. |
| Close stage / advance | State if logic grows, Domain Event. |
| Send clue / geolocation | Command Handler, PubSub/SignalR, Domain Event for audit. |

### Puntuaciones (scoring, ranking)

| HU | Suggested patterns |
|---|---|
| Update native ranking | Domain Service (`CalculadorRankingTriviaService` / `CalculadorRankingBDTService`), Strategy. |
| Consolidated ranking | Domain Service (`CalculadorRankingConsolidadoService`). |
| Real-time ranking broadcast | Observer/PubSub via SignalR. |

## Mandatory section in each `design.md`

Each feature SDD must include:

```md
## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
```

If no additional tactical pattern is introduced:

```md
No additional tactical pattern is introduced. The feature uses the mandatory architectural patterns: CQRS, Mediator, Repository, Adapter and Dependency Injection.
```

## Antipatterns to avoid

| Antipattern | Avoid because |
|---|---|
| God Service | Mixes logic from several contexts. |
| Anemic Domain without rules | Pushes rules into handlers/controllers and weakens DDD. |
| Controller with business logic | Breaks Clean Architecture. |
| Shared database between services | Breaks microservice boundaries. |
| Gateway with domain logic | The gateway routes only; it owns no business rules. |
| Unnecessary pattern | Adds complexity without benefit. |
| DTO as domain entity | Mixes API with domain. |
