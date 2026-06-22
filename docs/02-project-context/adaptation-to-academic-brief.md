# Adaptation of the Academic Brief

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

## Purpose

This document explains how UMBRAL maps the generic vocabulary of the original academic brief onto its own ubiquitous language, without losing the technical objectives of the integrating project.

## Original academic brief

The brief describes a platform for immersive live experiences with generic missions, live sessions, teams, clues, answers/evidence, ranking, traceability, WebSockets, RabbitMQ, CQRS, PostgreSQL, .NET, React, and clean/hexagonal architecture.

## Team adaptation

The team concretizes the domain into exactly two game modes — **Trivia** and **Búsqueda del Tesoro (BDT)** — organized as `Juego`s inside a `Partida`. The generic terms map as follows:

| Academic brief | UMBRAL ubiquitous language |
|---|---|
| Mission | `Partida` |
| Mission stages | `Juego` (sequential `JuegoTrivia` / `JuegoBDT`), with Trivia `Pregunta`s and BDT `EtapaBDT`s as inner steps |
| Live session | The live session managed by **Operaciones de Sesion** |
| Team | `Equipo` (inside **Identity**) |
| Evidence / answer | `RespuestaTrivia` / `TesoroQR` |
| Clue | `Pista` (BDT) |
| Ranking | Native `RankingTrivia` / `RankingBDT` plus the consolidated partida ranking (**Puntuaciones**) |
| Session event | `EventoHistorial` / `RegistroAuditoria` (materialized in **Puntuaciones** and **Operaciones de Sesion**) |
| Operator panel | React web |
| Participant panel | React Native mobile |

There is **no** generic "Trivia form" anymore: questions belong directly to the `JuegoTrivia` and are created with it. Do not implement generic mission/session/evidence/form modules.

## Mobile application

The SRS adds a React Native mobile app for participants because participation flows require immediate interaction, BDT requires camera/image, BDT requires operational geolocation, and Trivia requires a synchronized answer experience. The web app is reserved for administration and operation (Administrador / Operador).

## BDT geolocation

Geolocation is limited to operational supervision during an active BDT game, mandatory for participation, sent ~every 2 seconds. It does not include complex historical routes, advanced geospatial analytics, predictive maps, complex geofencing, or AI.

## Rule

Treat this adaptation as an accepted project decision. Do not revert to the generic missions/sessions/evidence model: the SRS, domain model, and class diagram define `Partida`, sequential `Juego`, `JuegoTrivia`, and `JuegoBDT` as the concrete model, materialized across the four target services (Identity, Partidas, Operaciones de Sesion, Puntuaciones).
