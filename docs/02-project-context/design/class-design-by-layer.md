# Class Design by Layer

## Purpose

This document maps the domain/class design to the layered implementation expected in each approved microservice.

## Active backend services

The only valid physical backend services are:

- Identity Service
- Team Service
- Trivia Game Service
- BDT Game Service

Do not create or reference these as physical services:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

## General layering rule

Each backend service follows a Clean/Hexagonal structure:

```txt
Domain
Application
Infrastructure
Api / EntryPoint
```

## Domain layer

Contains:

- entities;
- aggregate roots;
- value objects;
- enums;
- domain services;
- domain events;
- invariant methods.

Must not depend on:

- Entity Framework Core;
- ASP.NET controllers;
- SignalR hubs;
- RabbitMQ client implementations;
- external HTTP clients;
- database details.

## Application layer

Contains:

- commands;
- queries;
- MediatR handlers;
- validators;
- DTOs/read models;
- ports/interfaces for persistence or external dependencies;
- transaction/application orchestration.

Rules:

- Commands mutate state.
- Queries do not mutate state.
- Handlers coordinate use cases.
- Business invariants remain in domain objects or domain services.

## Infrastructure layer

Contains:

- EF Core DbContext;
- EF Core configurations;
- repository implementations;
- RabbitMQ publishers/consumers;
- SignalR adapters when applicable;
- HTTP clients to other services;
- Keycloak integration adapters.

## API / EntryPoint layer

Contains:

- controllers/minimal API endpoints;
- request/response mapping;
- authentication/authorization configuration;
- SignalR hubs if applicable.

Controllers and hubs must not contain business rules.

## Identity Service

### Domain

- Usuario
- KeycloakId
- RolUsuario
- EstadoUsuario

### Application

- user creation/registration commands;
- user consultation/editing/desactivation queries/commands;
- Keycloak/local user reference coordination.

### Infrastructure

- local user persistence;
- Keycloak adapter;
- EF Core configuration.

### API

- user and identity endpoints defined in `contracts/http/identity-api.md`.

## Team Service

### Domain

- Equipo
- Equipos.Participante
- EquipoId
- NombreEquipo
- CodigoAcceso
- EstadoEquipo

### Team aggregate invariant

```txt
1 <= Equipo.Participantes.Count <= 5
```

The team creator is inserted as the first participant and marked as leader.

Do not enforce a minimum of 2 members.

### Application

- create team;
- join team by code;
- leave team;
- transfer leadership;
- delete/deactivate team;
- team membership queries.

### Infrastructure

- team persistence;
- repository implementation;
- integration client only when required by SDD.

### API

- team endpoints defined in `contracts/http/team-api.md`.

## Trivia Game Service

### Domain

- FormularioTrivia
- Pregunta
- Opcion
- PuntajeAsignado
- TiempoLimite
- PartidaTrivia
- Trivias.Participante
- RespuestaTrivia
- trivia score/ranking/history concepts

### Trivia score calculation

The domain method for score accumulation must use direct accumulation.

```txt
if respuesta.EsCorrecta:
    participante.PuntajeAcumulado += pregunta.PuntajeAsignado
```

The method must not multiply by:

- remaining time;
- elapsed time;
- response time;
- total question time.

### Timer role

The timer is still used to:

- synchronize clients;
- close questions;
- reject late answers.

The timer must not affect score.

### Application

- create/edit/query forms;
- create/publish Trivia games;
- join Trivia;
- start Trivia;
- submit Trivia answers;
- close questions;
- calculate Trivia score;
- query ranking/history.

### Infrastructure

- Trivia persistence;
- RabbitMQ event publishing when required by SDD;
- SignalR/WebSocket adapter for real-time Trivia updates;
- Team Service HTTP client for team/leadership validation when required.

### API

- Trivia endpoints defined in `contracts/http/trivia-game-api.md`.

## BDT Game Service

### Domain

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
- BDT score/ranking/history concepts

### Application

- create/publish BDT games;
- configure stages;
- join BDT;
- start BDT;
- upload treasure QR image;
- validate QR;
- close stage;
- send clues;
- update/query geolocation;
- query ranking/history.

### Infrastructure

- BDT persistence;
- QR/image decoding adapter;
- RabbitMQ event publishing when required by SDD;
- SignalR/WebSocket adapter for real-time BDT updates;
- Team Service HTTP client for team/leadership validation when required.

### API

- BDT endpoints defined in `contracts/http/bdt-game-api.md`.
