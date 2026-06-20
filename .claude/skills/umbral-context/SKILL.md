---
name: umbral-context
description: Load UMBRAL product, SRS, domain, class design, microservices, contracts and first-delivery scope before planning or implementation.
compatibility: opencode
---

# UMBRAL Context

Use this skill before planning, SDD writing, implementation or review.

## Read first

- `AGENTS.md`
- `docs/05-decisions/ADR-0006-four-service-topology.md`
- `docs/02-project-context/project-brief.md`
- `docs/02-project-context/srs-summary.md`
- `docs/02-project-context/business-rules.md`
- `docs/02-project-context/first-delivery-scope.md`
- `docs/02-project-context/glossary.md`
- `docs/02-project-context/design/design-index.md`
- `docs/02-project-context/design/domain-entities-by-context.md`
- `docs/02-project-context/design/service-model-impact.md`
- `docs/03-microservices/microservices-map.md`
- `docs/03-microservices/service-ownership.md`
- `docs/03-microservices/communication-map.md`

## Read when extra detail is needed

Use the actual filenames present in `docs/01-project-source/`:

- `docs/01-project-source/srs.md`
- `docs/01-project-source/historias de usuario.md`
- `docs/01-project-source/modelo de dominio.md`
- `docs/01-project-source/diagrama de clases.md`
- `docs/01-project-source/microservicios.md`
- `docs/01-project-source/enunciado-proyecto.md`

## Core constraints

- Only Trivia and Búsqueda del Tesoro exist.
- Physical microservices are mandatory in this setup.
- The only valid backend services are Identity Service, Team Service, Trivia Game Service and BDT Game Service.
- Do not create Audit Service, Scoring Service, Trivia Service or Treasure Hunt Service as physical services.
- SDD is mandatory.
- The first delivery is limited to the selected user stories.
- Keycloak handles authentication and base roles.
- Leadership is a business condition managed by UMBRAL, not a Keycloak role.
