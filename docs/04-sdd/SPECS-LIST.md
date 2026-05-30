# UMBRAL — Active SDD Specs List

## Purpose

This file is the official list of SDD specs to create/regenerate for the current UMBRAL project.

It replaces the old mission/session-oriented spec folders currently present under `docs/04-sdd/specs/`.

## Source basis

The active first-delivery scope is defined by:

- `docs/01-project-source/historias de usuario.md`
- `docs/02-project-context/first-delivery-scope.md`
- `docs/03-microservices/service-ownership.md`
- `docs/05-decisions/ADR-0006-four-service-topology.md`

## Valid owning services

Only these backend services may own a spec:

- Identity Service
- Team Service
- Trivia Game Service
- BDT Game Service

Do not assign specs to:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

## Active first-delivery specs

### Team Service

| HU | Spec folder | Feature | Responsible |
|---|---|---|---|
| HU-03 | `HU-03-crear-equipo` | Crear equipo | Santiago |
| HU-04 | `HU-04-unirse-equipo-codigo` | Unirse a equipo usando código | Santiago |
| HU-05 | `HU-05-eliminar-equipo` | Eliminar equipo creado | Mariangel |
| HU-06 | `HU-06-transferir-liderazgo` | Transferir liderazgo antes de salir del equipo | Santiago |
| HU-07 | `HU-07-salir-equipo` | Salir del equipo | Santiago |

### Trivia Game Service

| HU | Spec folder | Feature | Responsible |
|---|---|---|---|
| HU-09 | `HU-09-ver-trivias-publicadas` | Ver partidas de Trivia publicadas | Mariangel |
| HU-11 | `HU-11-filtrar-trivias-modalidad` | Filtrar partidas de Trivia por modalidad | Mariangel |
| HU-13 | `HU-13-advertencia-trivia-no-lider` | Advertencia al entrar a Trivia por equipo sin ser líder | Mariangel |
| HU-15 | `HU-15-crear-formularios-trivia` | Crear formularios de Trivia | Mariangel |
| HU-17 | `HU-17-crear-publicar-trivia` | Crear y publicar partida de Trivia | Mariangel |
| HU-18 | `HU-18-unirse-trivia-individual` | Unirse a Trivia individual | Mariangel |
| HU-19 | `HU-19-unir-equipo-trivia` | Unir equipo a Trivia por equipos | Mariangel |
| HU-21 | `HU-21-pantalla-espera-trivia` | Ver pantalla de espera de Trivia | Mariangel |
| HU-22 | `HU-22-ver-participantes-trivia` | Ver participantes unidos a Trivia publicada | Mariangel |
| HU-23 | `HU-23-ver-equipos-trivia` | Ver equipos unidos a Trivia publicada | Mariangel |
| HU-24 | `HU-24-iniciar-trivia` | Iniciar manualmente Trivia | Mariangel |
| HU-26 | `HU-26-responder-trivia-individual` | Responder Trivia individual | Mariangel |
| HU-27 | `HU-27-responder-trivia-equipo` | Responder Trivia por equipo | Mariangel |
| HU-28 | `HU-28-resultado-cierre-pregunta-trivia` | Ver resultado al cerrar pregunta de Trivia | Mariangel |
| HU-29 | `HU-29-calcular-puntaje-trivia` | Calcular puntaje de respuesta en Trivia | Mariangel |
| HU-30 | `HU-30-ranking-trivia` | Ver ranking durante Trivia | Mariangel |
| HU-35 | `HU-35-lista-trivias-publicadas` | Ver lista de partidas de Trivia publicadas | Mariangel |

### BDT Game Service

| HU | Spec folder | Feature | Responsible |
|---|---|---|---|
| HU-10 | `HU-10-ver-bdt-publicadas` | Ver partidas de BDT publicadas | Santiago |
| HU-12 | `HU-12-filtrar-bdt-modalidad` | Filtrar partidas de BDT por modalidad | Santiago |
| HU-14 | `HU-14-advertencia-bdt-no-lider` | Advertencia al entrar a BDT por equipo sin ser líder | Santiago |
| HU-34 | `HU-34-crear-partida-bdt` | Crear partida de Búsqueda del Tesoro | Santiago |
| HU-37 | `HU-37-lista-bdt-publicadas` | Ver lista de partidas de BDT publicadas | Santiago |
| HU-39 | `HU-39-unirse-bdt-individual` | Unirse a BDT individual | Santiago |
| HU-40 | `HU-40-unir-equipo-bdt` | Unir equipo a BDT por equipos | Santiago |
| HU-42 | `HU-42-ver-participantes-bdt` | Ver participantes unidos a BDT publicada | Santiago |
| HU-43 | `HU-43-iniciar-bdt` | Iniciar partida BDT | Santiago |
| HU-44 | `HU-44-etapa-activa-subir-tesoro` | Ver etapa activa y opción de subir tesoro | Santiago |
| HU-45 | `HU-45-subir-foto-tesoro-qr` | Subir foto del tesoro QR | Santiago |
| HU-46 | `HU-46-validar-qr-bdt` | Validar automáticamente QR enviado | Santiago |
| HU-47 | `HU-47-cerrar-etapa-bdt` | Cerrar etapa BDT | Santiago |
| HU-49 | `HU-49-enviar-pistas-bdt` | Enviar pistas a participantes durante BDT | Santiago |

## Specs outside first delivery

Do not create or implement specs outside the list above unless the user explicitly changes the delivery scope.

Examples of specs that may exist in the SRS but are outside the current first-delivery list:

- HU-01
- HU-02
- HU-08
- HU-16
- HU-20
- HU-25
- HU-31
- HU-32
- HU-33
- HU-36
- HU-38
- HU-41
- HU-48

These may be technical dependencies or future work, but they are not active first-delivery specs.

## Required folder structure per spec

Every active spec folder must contain:

```txt
docs/04-sdd/specs/<HU-ID>-<short-name>/
  spec.md
  design.md
  tasks.md
  acceptance.md
```

## Rule for old specs

Any existing folder under `docs/04-sdd/specs/` that is not listed above must be moved to:

```txt
docs/04-sdd/specs/_deprecated/
```

before using OpenCode for implementation.
