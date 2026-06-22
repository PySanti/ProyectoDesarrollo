# API Contracts

> **Authority:** derived from `CLAUDE.md` and `docs/01-project-source/microservicios.md`. This file does **not** invent HTTP routes, methods, payloads, status codes, pagination formats or per-endpoint auth. Concrete contracts are produced per HU during SDD and recorded under `contracts/http/`.

## Routing model

All HTTP traffic reaches the backend through the **YARP gateway** — there is no direct client → service contact. The gateway validates the Keycloak JWT and applies coarse, route-level authorization by base role (`Administrador`/`Operador`/`Participante`). Each service then enforces its functional permissions and domain rules locally. Contracts are therefore organized by **owning service**, and routes live behind the gateway.

## HTTP capability areas per service

These are functional capability areas, **not** endpoints. They indicate which service would own a given HTTP contract.

### Identity

- User creation with initial role; consultation and editing of general user data.
- Role/permission/governance management **per role** from the governance panel; role modification for operators/participants propagated to Keycloak.
- Teams: creation, membership, leadership and transfer.
- Team invitations (`InvitacionEquipo`) from a dynamic participant list; per-participant team-name history.

### Partidas

- Creation and configuration of a `Partida` and its `Juego`s (sequential order, modality, min/max participation, start mode/time).
- Trivia `Pregunta` configuration (options, correct answer, `PuntajeAsignado`, time limit) created with the game.
- BDT `EtapaBDT` configuration (expected QR text, per-stage `Puntaje`, time limit).
- Configuration queries for the above.

### Operaciones de Sesion

- Publishing a partida (→ `Lobby`) and manual/automatic start.
- Inscriptions (`InscripcionPartida`) and convocatorias (`Convocatoria`).
- Runtime queries: active question/stage, lobby/participant state, session status.
- Answer submission, QR upload/validation, clue delivery, geolocation, reconnection.

### Puntuaciones

- Ranking queries: Trivia native ranking, BDT native ranking, consolidated partida ranking.
- Score and won-stage queries; team-performance queries.
- Audit/history queries.

## Rule for creating HTTP contracts

Each HTTP contract is created during the SDD of a concrete HU, once `spec.md` and `design.md` define: the user story, the owning service, the user action, input/output data, validations, business errors, the authorized role/permission, whether it mutates state or is a query, and any real-time or event effects.

## Mandatory template for future contracts

When an HU requires an HTTP contract, document it in `contracts/http/<service>.md` with this format:

```md
## <Capability name>

| Field | Value |
|---|---|
| HU | HU-XX |
| Owning service | Identity / Partidas / Operaciones de Sesion / Puntuaciones |
| Type | Command / Query |
| HTTP method | Defined in SDD |
| Route (behind gateway) | Defined in SDD |
| Base role (gateway) | Administrador / Operador / Participante |
| Functional permission (service) | GestionarPartidas / GestionarEquipos / ParticiparEnPartidas |
| Mutates state | Yes / No |
| Publishes event | Yes / No |
| Real-time effect | Yes / No |

### Request

Defined in SDD.

### Response

Defined in SDD.

### Business errors

Defined in SDD.
```
