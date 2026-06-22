# Design Context Index — UMBRAL

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

This directory contains design documents derived from the project source, mapped to the four target services: **Identity**, **Partidas**, **Operaciones de Sesion**, **Puntuaciones** (behind the mandatory YARP gateway). The central structure is a **`Partida`** of **sequential `Juego`s**, each a **`JuegoTrivia`** or **`JuegoBDT`**.

## Files

| File | Use |
|---|---|
| `domain-business-rules.md` | Place rules inside aggregates, entities, and domain services. |
| `domain-entities-by-context.md` | Look up entities, aggregates, value objects, and enums by context, mapped to the target services. |
| `class-design-by-layer.md` | Translate classes to Clean / Hexagonal layers per service. |
| `service-model-impact.md` | Determine which target service owns each feature. |
| `design-patterns-catalog.md` | Choose design patterns in a justified way. |

## Use within SDD

When creating `docs/04-sdd/specs/<HU>/design.md`, consult these documents and answer:

1. Which bounded context does the HU touch?
2. Which aggregate protects the rules?
3. Which target service owns it (Identity / Partidas / Operaciones de Sesion / Puntuaciones)?
4. Which commands/queries are needed?
5. Which events must be published (RabbitMQ) and which real-time updates broadcast (SignalR through the gateway)?
6. Which rules require tests?
7. Which design pattern is justified and where?
