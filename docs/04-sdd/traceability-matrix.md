# UMBRAL Traceability Matrix — First Delivery

> Valid owning services: Identity Service, Team Service, Trivia Game Service and BDT Game Service.

> Scope: only the active first-delivery specs listed in `docs/04-sdd/SPECS-LIST.md`.

| HU | Feature | Requirement | Owning service | Supporting services | SDD folder | Contract files | Status |
|---|---|---|---|---|---|---|---|
| HU-03 | Crear equipo | RF-07 | Team Service | Identity Service / Keycloak | HU-03-crear-equipo | contracts/http/team-api.md | Pending |
| HU-04 | Unirse a equipo usando código | RF-07 | Team Service | Identity Service / Keycloak | HU-04-unirse-equipo-codigo | contracts/http/team-api.md | Pending |
| HU-05 | Eliminar equipo creado | RF-07, RF-08 | Team Service | Identity Service / Keycloak | HU-05-eliminar-equipo | contracts/http/team-api.md; contracts/events/team-events.md | Pending |
| HU-06 | Transferir liderazgo antes de salir | RF-08 | Team Service | Identity Service / Keycloak | HU-06-transferir-liderazgo | contracts/http/team-api.md | Pending |
| HU-07 | Salir del equipo | RF-08 | Team Service | Identity Service / Keycloak | HU-07-salir-equipo | contracts/http/team-api.md | Pending |
| HU-09 | Ver partidas de Trivia publicadas | RF-05 | Trivia Game Service | Identity Service / Keycloak | HU-09-ver-trivias-publicadas | contracts/http/trivia-game-api.md | Pending |
| HU-10 | Ver partidas de BDT publicadas | RF-05 | BDT Game Service | Identity Service / Keycloak | HU-10-ver-bdt-publicadas | contracts/http/bdt-game-api.md | Pending |
| HU-11 | Filtrar partidas de Trivia por modalidad | RF-05 | Trivia Game Service | Identity Service / Keycloak | HU-11-filtrar-trivias-modalidad | contracts/http/trivia-game-api.md | Pending |
| HU-12 | Filtrar partidas de BDT por modalidad | RF-05 | BDT Game Service | Identity Service / Keycloak | HU-12-filtrar-bdt-modalidad | contracts/http/bdt-game-api.md | Pending |
| HU-13 | Advertencia al entrar a Trivia por equipo sin ser líder | RF-06 | Trivia Game Service | Team Service; Identity Service / Keycloak | HU-13-advertencia-trivia-no-lider | contracts/http/trivia-game-api.md; contracts/http/team-api.md | Pending |
| HU-14 | Advertencia al entrar a BDT por equipo sin ser líder | RF-06 | BDT Game Service | Team Service; Identity Service / Keycloak | HU-14-advertencia-bdt-no-lider | contracts/http/bdt-game-api.md; contracts/http/team-api.md | Pending |
| HU-15 | Crear formularios de Trivia | RF-15, RF-16 | Trivia Game Service | Identity Service / Keycloak | HU-15-crear-formularios-trivia | contracts/http/trivia-game-api.md | Pending |
| HU-17 | Crear y publicar partida de Trivia | RF-17, RF-18 | Trivia Game Service | Team Service; Identity Service / Keycloak | HU-17-crear-publicar-trivia | contracts/http/trivia-game-api.md; contracts/events/trivia-game-events.md | Pending |
| HU-18 | Unirse a Trivia individual | RF-18 | Trivia Game Service | Identity Service / Keycloak | HU-18-unirse-trivia-individual | contracts/http/trivia-game-api.md | Pending |
| HU-19 | Unir equipo a Trivia por equipos | RF-10, RF-18 | Trivia Game Service | Team Service; Identity Service / Keycloak | HU-19-unir-equipo-trivia | contracts/http/trivia-game-api.md; contracts/http/team-api.md | Pending |
| HU-21 | Ver pantalla de espera de Trivia | RF-13, RF-18 | Trivia Game Service | Identity Service / Keycloak | HU-21-pantalla-espera-trivia | contracts/http/trivia-game-api.md; contracts/events/trivia-game-events.md | Pending |
| HU-22 | Ver participantes unidos a Trivia publicada | RF-13, RF-18 | Trivia Game Service | Identity Service / Keycloak | HU-22-ver-participantes-trivia | contracts/http/trivia-game-api.md; contracts/events/trivia-game-events.md | Pending |
| HU-23 | Ver equipos unidos a Trivia publicada | RF-13, RF-18 | Trivia Game Service | Team Service; Identity Service / Keycloak | HU-23-ver-equipos-trivia | contracts/http/trivia-game-api.md; contracts/http/team-api.md; contracts/events/trivia-game-events.md | Pending |
| HU-24 | Iniciar manualmente Trivia | RF-18 | Trivia Game Service | Identity Service / Keycloak | HU-24-iniciar-trivia | contracts/http/trivia-game-api.md; contracts/events/trivia-game-events.md | Pending |
| HU-26 | Responder Trivia individual | RF-20, RF-21 | Trivia Game Service | Identity Service / Keycloak | HU-26-responder-trivia-individual | contracts/http/trivia-game-api.md; contracts/events/trivia-game-events.md | Pending |
| HU-27 | Responder Trivia por equipo | RF-20, RF-21 | Trivia Game Service | Team Service; Identity Service / Keycloak | HU-27-responder-trivia-equipo | contracts/http/trivia-game-api.md; contracts/http/team-api.md; contracts/events/trivia-game-events.md | Pending |
| HU-28 | Ver resultado al cerrar pregunta de Trivia | RF-21, RF-22 | Trivia Game Service | Identity Service / Keycloak | HU-28-resultado-cierre-pregunta-trivia | contracts/http/trivia-game-api.md; contracts/events/trivia-game-events.md | Pending |
| HU-29 | Calcular puntaje de respuesta en Trivia | RF-22 | Trivia Game Service | Identity Service / Keycloak | HU-29-calcular-puntaje-trivia | contracts/http/trivia-game-api.md; contracts/events/trivia-game-events.md | Pending |
| HU-30 | Ver ranking durante Trivia | RF-13, RF-23 | Trivia Game Service | Identity Service / Keycloak | HU-30-ranking-trivia | contracts/http/trivia-game-api.md; contracts/events/trivia-game-events.md | Pending |
| HU-35 | Ver lista de partidas de Trivia publicadas | RF-35 | Trivia Game Service | Identity Service / Keycloak | HU-35-lista-trivias-publicadas | contracts/http/trivia-game-api.md | Pending |
| HU-34 | Crear partida de Búsqueda del Tesoro | RF-25, RF-26 | BDT Game Service | Team Service; Identity Service / Keycloak | HU-34-crear-partida-bdt | contracts/http/bdt-game-api.md | Pending |
| HU-37 | Ver lista de partidas de BDT publicadas | RF-35 | BDT Game Service | Identity Service / Keycloak | HU-37-lista-bdt-publicadas | contracts/http/bdt-game-api.md | Pending |
| HU-39 | Unirse a BDT individual | RF-27 | BDT Game Service | Identity Service / Keycloak | HU-39-unirse-bdt-individual | contracts/http/bdt-game-api.md | Pending |
| HU-40 | Unir equipo a BDT por equipos | RF-10, RF-27 | BDT Game Service | Team Service; Identity Service / Keycloak | HU-40-unir-equipo-bdt | contracts/http/bdt-game-api.md; contracts/http/team-api.md | Pending |
| HU-42 | Ver participantes unidos a BDT publicada | RF-13, RF-27 | BDT Game Service | Identity Service / Keycloak | HU-42-ver-participantes-bdt | contracts/http/bdt-game-api.md; contracts/events/bdt-game-events.md | Pending |
| HU-43 | Iniciar partida BDT | RF-27 | BDT Game Service | Identity Service / Keycloak | HU-43-iniciar-bdt | contracts/http/bdt-game-api.md; contracts/events/bdt-game-events.md | Pending |
| HU-44 | Ver etapa activa y opción de subir tesoro | RF-28 | BDT Game Service | Identity Service / Keycloak | HU-44-etapa-activa-subir-tesoro | contracts/http/bdt-game-api.md; contracts/events/bdt-game-events.md | Pending |
| HU-45 | Subir foto del tesoro QR | RF-28, RF-29, RF-30 | BDT Game Service | Identity Service / Keycloak | HU-45-subir-foto-tesoro-qr | contracts/http/bdt-game-api.md; contracts/events/bdt-game-events.md | Pending |
| HU-46 | Validar automáticamente QR enviado | RF-29, RF-30 | BDT Game Service | Identity Service / Keycloak | HU-46-validar-qr-bdt | contracts/http/bdt-game-api.md; contracts/events/bdt-game-events.md | Pending |
| HU-47 | Cerrar etapa BDT | RF-31, RF-32 | BDT Game Service | Identity Service / Keycloak | HU-47-cerrar-etapa-bdt | contracts/http/bdt-game-api.md; contracts/events/bdt-game-events.md | Pending |
| HU-49 | Enviar pistas a participantes durante BDT | RF-33 | BDT Game Service | Identity Service / Keycloak | HU-49-enviar-pistas-bdt | contracts/http/bdt-game-api.md; contracts/events/bdt-game-events.md | Pending |
