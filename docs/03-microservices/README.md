# 03-microservices — UMBRAL

> **Authority:** this folder describes the **current target topology**. It is derived from `CLAUDE.md` and `docs/01-project-source/microservicios.md`, which are authoritative for service ownership and communication. Where on-disk folders, ports, or DB names still reflect the old layout, that is migration debt, not the target.

## Purpose

This folder defines the operational microservices context for UMBRAL so that the SDD workflow can identify:

- which physical services exist;
- what each service owns and explicitly does not own;
- which DDD bounded contexts each service materializes;
- how the services communicate (gateway, RabbitMQ, SignalR/WebSockets);
- where ranking, scoring, audit and notification responsibilities actually live.

## Current topology

UMBRAL is **four independent .NET 8 microservices** behind a **mandatory YARP gateway**:

1. **Identity** — users, Keycloak mapping, roles/permissions/governance, teams (absorbs the former Team Service), temporary-credential notification.
2. **Partidas** — partida and game configuration (Trivia questions + BDT stages), modality, participation limits, start configuration.
3. **Operaciones de Sesion** — live runtime, inscriptions and convocatorias, transient session state, session SignalR.
4. **Puntuaciones** — scoring, native and consolidated rankings, audit/history projections, ranking SignalR.

The YARP gateway is the single entry point for all client traffic (including real time) and owns no domain state.

## Files

| File | Purpose |
|---|---|
| `source-basis.md` | Sources used and the no-assumption rules. |
| `microservices-map.md` | The four target services and the gateway note. |
| `service-ownership.md` | Owns / Does not own for each service; old services declared obsolete. |
| `communication-map.md` | Gateway, RabbitMQ and SignalR communication rules. |
| `api-contracts.md` | HTTP guidance organized around the four services (no invented endpoints). |
| `events-catalog.md` | Event guidance organized around the four services (canonical names only). |
| `unresolved-decisions.md` | Items that still require SDD/ADR resolution. |
| `services/identity-service.md` | Identity service context. |
| `services/partidas-service.md` | Partidas service context. |
| `services/operaciones-sesion-service.md` | Operaciones de Sesion service context. |
| `services/puntuaciones-service.md` | Puntuaciones service context. |
| `services/team-service.md` | Legacy pointer (obsolete path, redirects to current services). |
| `services/trivia-game-service.md` | Legacy pointer (obsolete path, redirects to current services). |
| `services/bdt-game-service.md` | Legacy pointer (obsolete path, redirects to current services). |

## Obsolete decomposition

The previous layout (`Team Service`, `Trivia Game Service`, `BDT Game Service`, plus a non-enforced gateway) is **superseded**. Those names must not be reintroduced as active physical services. Teams live inside Identity; Trivia/BDT configuration lives inside Partidas; Trivia/BDT runtime lives inside Operaciones de Sesion; scoring, ranking and audit/history live inside Puntuaciones (and Operaciones de Sesion materializes its own audit). The old "BDT ranks by stages won, not points" rule is also superseded — see `service-ownership.md` and `microservices-map.md`.

## How to use this folder with SDD

Before creating or implementing a user story:

1. Read `microservices-map.md`.
2. Read `service-ownership.md`.
3. Read the corresponding file in `services/`.
4. If the HU requires events, read `events-catalog.md`.
5. If the HU requires HTTP, complete contracts in `contracts/http/` only after the HU `spec.md` and `design.md` justify them.
6. If the HU touches an item flagged in `unresolved-decisions.md`, resolve it first.
