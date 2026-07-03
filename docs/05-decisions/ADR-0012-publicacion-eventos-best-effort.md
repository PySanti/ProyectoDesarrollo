# ADR-0012 — Publicación de eventos best-effort post-save; outbox transaccional diferido

- **Estado:** Accepted
- **Fecha:** 2026-07-03
- **Contexto de origen:** slice SP-3i (backbone RabbitMQ), spec `docs/superpowers/specs/2026-07-03-sp3i-backbone-rabbitmq-design.md`.

## Contexto

Los handlers de Operaciones de Sesión persisten con `SaveChanges` y luego publican al seam de eventos (patrón save→publish verificado por la auditoría 2026-07-02, dimensión D7). Con RabbitMQ real en el Composite, la publicación al broker ocurre fuera de la transacción de base de datos: un crash del proceso entre el save y el publish pierde el evento; un fallo del broker lo pierde también (se loguea y se continúa).

## Decisión

Se acepta la publicación **best-effort**: el publisher RabbitMQ captura toda excepción, la loguea (`LogError`) y nunca falla el request ni el scheduler. No se implementa outbox transaccional en SP-3i.

## Justificación

1. Puntuaciones (SP-4) es un modelo de proyección **reconstruible**; la pérdida puntual de un evento no corrompe estado de negocio irrecuperable.
2. El Composite ya aísla fallos por delegado; la semántica user-facing (SignalR) no depende del broker.
3. El outbox (tabla + dispatcher + idempotencia de despacho) duplica el tamaño del slice sin necesidad presente.

## Criterio de activación del outbox (cuándo revisar esta decisión)

- SP-4 materializa datos **no reconstruibles** desde el estado de Operaciones, o
- la pérdida observada de eventos afecta rankings/auditoría de forma visible, o
- se añade un consumidor con requisitos de completitud (p. ej. auditoría normativa).

## Referencias

- Spec SP-3i §7; contrato `contracts/events/operaciones-sesion-events.md` §Transport.
- Informe de auditoría 2026-07-02, D7 (save→publish).
