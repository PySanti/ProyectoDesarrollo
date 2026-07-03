# ADR-0010 — El estado runtime de la partida vive en Operaciones de Sesión

- **Estado:** Aceptado
- **Fecha:** 2026-06-26
- **Contexto de slice:** SP-3a (`docs/superpowers/specs/2026-06-26-sp3a-publicacion-lobby-inscripcion-design.md`)
- **Relacionado:** ADR-0009 (topología de servicios), SP-2 (SEAM `EstadoPartida` nullable)

## Contexto

El diagrama de clases ubica `EstadoPartida ∈ {Lobby, Iniciada, Cancelada, Terminada}` en el agregado `Partida`. SP-2 dejó esa propiedad **nullable** (`null` = configurada, no publicada) con la nota "SP-3 pone Lobby". Pero publicar/runtime pertenece a **Operaciones de Sesión**, y un servicio **nunca** escribe la BD de otro (frontera dura). Operaciones no puede mover el `EstadoPartida` de Partidas.

## Decisión

El ciclo de vida runtime de la partida se materializa en el agregado **`SesionPartida.EstadoSesion`** dentro de **Operaciones de Sesión**. El `EstadoPartida` de **Partidas permanece `null`** para siempre: Partidas es config-only y no expone ningún command de publicación/runtime.

## Alternativas rechazadas

1. **Partidas expone un command de publicación** que voltea su propio `EstadoPartida` → re-aloja runtime en un servicio config-only; viola el ownership de ADR-0009.
2. **Un evento hace que Partidas actualice su estado** → exige el backbone de mensajería (diferido) y **duplica** el estado en dos servicios (fuente de verdad ambigua).

## Consecuencias

- El estado runtime es **single-sourced** en Operaciones.
- Partidas no gana superficie de publicación/runtime; su `EstadoPartida` nullable queda como marcador de "configurada" y no avanza.
- El backbone de RabbitMQ posterior **no** duplica el estado; solo transporta eventos.
- Lectores que esperaban "SP-3 pone Lobby en Partidas" deben mirar `SesionPartida` en Operaciones (este ADR es el registro durable; referénciese desde la nota SEAM de SP-2 si hay confusión).
