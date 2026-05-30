# Service Ownership

This file defines the owning backend service for each active first-sprint story.

## Valid owning services

- Identity Service
- Team Service
- Trivia Game Service
- BDT Game Service

Do not create these as physical services:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

## Ownership by active story

| HU | Feature | Owning service | Client target |
| --- | --- | --- | --- |
| HU-01 | Crear usuario con rol inicial | Identity Service | React web |
| HU-02 | Consultar y editar datos generales de usuario | Identity Service | React web |
| HU-03 | Crear equipo | Team Service | React Native mobile |
| HU-04 | Unirse a equipo usando código | Team Service | React Native mobile |
| HU-06 | Transferir liderazgo antes de salir del equipo | Team Service | React Native mobile |
| HU-07 | Salir del equipo | Team Service | React Native mobile |
| HU-10 | Ver partidas de BDT publicadas | BDT Game Service | React Native mobile |
| HU-12 | Filtrar partidas de BDT por modalidad | BDT Game Service | React Native mobile |
| HU-14 | Advertencia al entrar a BDT por equipo sin ser líder | BDT Game Service | React Native mobile |
| HU-34 | Crear partida de Búsqueda del Tesoro | BDT Game Service | React web |
| HU-37 | Ver lista de partidas de BDT publicadas | BDT Game Service | React web |
| HU-39 | Unirse a BDT individual | BDT Game Service | React Native mobile |
| HU-40 | Unir equipo a BDT por equipos | BDT Game Service | React Native mobile |
| HU-42 | Ver participantes unidos a BDT publicada | BDT Game Service | React web |
| HU-43 | Iniciar partida BDT | BDT Game Service | React web |
| HU-44 | Ver etapa activa y opción de subir tesoro | BDT Game Service | React Native mobile |
| HU-45 | Subir foto del tesoro QR | BDT Game Service | React Native mobile |
| HU-46 | Validar automáticamente QR enviado | BDT Game Service | Backend |
| HU-47 | Cerrar etapa BDT | BDT Game Service | Backend / React Native mobile |
| HU-49 | Enviar pistas a participantes durante BDT | BDT Game Service | React web |
| HU-05 | Eliminar equipo creado | Team Service | React Native mobile |
| HU-09 | Ver partidas de Trivia publicadas | Trivia Game Service | React Native mobile |
| HU-11 | Filtrar partidas de Trivia por modalidad | Trivia Game Service | React Native mobile |
| HU-13 | Advertencia al entrar a Trivia por equipo sin ser líder | Trivia Game Service | React Native mobile |
| HU-15 | Crear formularios de Trivia | Trivia Game Service | React web |
| HU-17 | Crear y publicar partida de Trivia | Trivia Game Service | React web |
| HU-18 | Unirse a Trivia individual | Trivia Game Service | React Native mobile |
| HU-19 | Unir equipo a Trivia por equipos | Trivia Game Service | React Native mobile |
| HU-21 | Ver pantalla de espera de Trivia | Trivia Game Service | React Native mobile |
| HU-22 | Ver participantes unidos a Trivia publicada | Trivia Game Service | React web |
| HU-23 | Ver equipos unidos a Trivia publicada | Trivia Game Service | React web |
| HU-24 | Iniciar manualmente Trivia | Trivia Game Service | React web |
| HU-26 | Responder Trivia individual | Trivia Game Service | React Native mobile |
| HU-27 | Responder Trivia por equipo | Trivia Game Service | React Native mobile |
| HU-28 | Ver resultado al cerrar pregunta de Trivia | Trivia Game Service | React Native mobile |
| HU-29 | Calcular puntaje de respuesta en Trivia | Trivia Game Service | Backend / React Native mobile |
| HU-30 | Ver ranking durante Trivia | Trivia Game Service | React web |
| HU-35 | Ver lista de partidas de Trivia publicadas | Trivia Game Service | React web |

## Identity Service

Owns:

- user creation through UMBRAL/Keycloak integration;
- initial role assignment at user creation;
- local user reference mapped to Keycloak;
- user consultation;
- editing general user data;
- user deactivation when required by SDD.

Active first-sprint stories:

- HU-01 Crear usuario con rol inicial.
- HU-02 Consultar y editar datos generales de usuario.

## Team Service

Owns:

- teams;
- team members;
- access codes;
- leadership;
- transfer of leadership;
- team leave/delete rules;
- team status.

## Trivia Game Service

Owns:

- Trivia forms;
- Trivia publication/listing;
- Trivia joining;
- Trivia lobby;
- Trivia answers;
- Trivia scoring;
- Trivia ranking;
- Trivia real-time updates.

## BDT Game Service

Owns:

- BDT publication/listing;
- BDT stages;
- expected textual QR;
- treasure uploads;
- QR validation;
- clues;
- BDT lobby;
- BDT ranking by stages won and accumulated time across won stages;
- BDT real-time updates.
