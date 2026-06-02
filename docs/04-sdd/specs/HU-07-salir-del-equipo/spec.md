# HU-07 — Salir del equipo

## User story

Como **Participante**, quiero **salir de mi equipo**, para **dejar de participar en el**.

## Source references

- HU: `HU-07` en `docs/01-project-source/srs.md` y `docs/01-project-source/historias-de-usuario.md`.
- RF: `RF-07`, `RF-08`, `RF-35`, `RF-36` en `docs/01-project-source/srs.md`.
- RB: `RB-E07`, `RB-E08`, `RB-E09`, `RB-E10`, `RB-E11`, `RB-E17` en `docs/01-project-source/srs.md`.
- RNF: `RNF-01`, `RNF-02`, `RNF-04`, `RNF-06`, `RNF-13`, `RNF-14` en `docs/01-project-source/srs.md`.
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`.
- Service context: `services/team-service/service-context.md`.
- Business rules: `docs/02-project-context/business-rules.md`, `docs/02-project-context/design/domain-business-rules.md`.
- Resolved decisions: `docs/02-project-context/known-ambiguities-and-decisions.md`.
- Contract base: `contracts/http/team-api.md`, `contracts/events/team-events.md`.

## Actor

- `Participante`.

## User goal

Permitir que un participante autenticado abandone su equipo activo, respetando las reglas de liderazgo y manteniendo la consistencia del agregado `Equipo`.

## Scope

Included:
- Salida de equipo desde la app movil de participante (`React Native mobile`).
- Validar que el participante pertenezca a un equipo activo.
- Permitir salida directa cuando el participante no es lider.
- Impedir que un lider con otros integrantes salga sin transferir liderazgo previamente.
- Eliminar el equipo cuando el lider es el unico integrante y decide salir.
- Persistir el cambio de membresia o estado del equipo en Team Service.
- Retornar un resultado minimo para que la UI movil refleje que el participante ya no pertenece al equipo.

Out of scope:
- Crear equipo (HU-03).
- Unirse a equipo por codigo (HU-04).
- Transferir liderazgo a otro integrante (HU-06).
- Eliminar equipo por decision directa del lider cuando tiene integrantes (HU-05).
- Validar si el equipo esta inscrito en partidas en `lobby` o `iniciada`; esa regla pertenece a HU-05 y futuras integraciones con servicios de juego.
- Gestion administrativa de equipos por Administrador (HU-08).
- Notificaciones en tiempo real a otros integrantes.

## Preconditions

- Usuario autenticado con rol base `Participante`.
- Usuario activo segun autenticacion/autorizacion base.
- Team Service puede identificar al participante mediante claims del token.
- El participante pertenece a un equipo activo para ejecutar una salida exitosa.

## Postconditions

- Si el participante no era lider, deja de pertenecer al equipo activo.
- Si el participante era lider y era el unico integrante, el equipo cambia a estado `Eliminado` y el participante deja de pertenecer al equipo activo.
- Si el participante era lider y existen otros integrantes, no se modifica el estado del equipo ni la membresia.
- En el modelo de persistencia actual, una salida exitosa elimina la fila de `ParticipanteEquipo` para no conservar una membresia activa que bloquee futuras uniones por HU-04.
- El historial de partidas o participaciones previas no se elimina ni modifica.

## Business rules

- `RF-08`: el sistema permite que un participante salga de su equipo; si no es lider sale directamente; si es lider y existen otros integrantes debe transferir liderazgo; si no existen otros integrantes el equipo debe eliminarse.
- `RB-E07`: un equipo puede existir con minimo 1 integrante y maximo 5 integrantes.
- `RB-E08`: los jugadores pueden salir de su equipo.
- `RB-E09`: si un jugador no lider sale del equipo, simplemente deja de pertenecer al equipo.
- `RB-E10`: si el lider desea salir y existen otros integrantes, debe transferir el liderazgo antes de salir.
- `RB-E11`: si el lider desea salir y no existen otros integrantes, el equipo se elimina.
- `RB-E17`: la eliminacion de un equipo no elimina ni modifica el historial de partidas, participaciones, puntajes o eventos ya registrados.
- Decision resuelta: `1 <= Equipo.Participantes.Count <= 5`; un equipo de un solo integrante es valido hasta que su unico lider sale, momento en que se elimina.

## Related requirements

- `RF-07`
- `RF-08`
- `RF-35`
- `RF-36`
- `RNF-01`
- `RNF-02`
- `RNF-04`
- `RNF-06`
- `RNF-13`
- `RNF-14`

## Acceptance criteria

1. Un participante autenticado no lider puede salir de su equipo activo exitosamente.
2. Al salir exitosamente como no lider, el participante deja de aparecer como integrante del equipo.
3. Un participante que no pertenece a ningun equipo activo recibe `404` o error equivalente de recurso no encontrado, sin modificar estado.
4. Un lider con otros integrantes no puede salir directamente; recibe conflicto de negocio (`409`) indicando que debe transferir liderazgo primero.
5. Un lider que es el unico integrante puede salir y el equipo queda eliminado.
6. La salida del lider unico no elimina historial ni referencias historicas de participacion.
7. El endpoint exige autenticacion; sin token valido responde `401`.
8. Un actor autenticado sin condicion valida de participante recibe `403` cuando aplique por politica.
9. La respuesta exitosa permite a la app movil actualizar la pantalla de equipo o volver al estado “sin equipo”.

## Open questions

- Ninguna bloqueante para completar el SDD de HU-07.

## Assumptions

- Team Service es la fuente de verdad para membresia, liderazgo y estado del equipo.
- HU-07 no consulta directamente Trivia Game Service ni BDT Game Service para validar inscripciones activas; esa restriccion aplica a HU-05 y futuras historias de inscripcion/cancelacion.
- La eliminacion por salida del lider unico se implementa como cambio de estado `Eliminado` sobre `Equipo`, no como borrado fisico del equipo, para preservar trazabilidad.
- La membresia del participante que sale se remueve fisicamente de `equipos_participantes` porque `ParticipanteEquipo` no tiene estado activo/inactivo y existe un indice unico sobre `usuarioid`.
