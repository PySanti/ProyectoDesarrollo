# HU-06 — Transferir liderazgo antes de salir del equipo

## User story

Como **Lider de equipo**, quiero **transferir el liderazgo antes de salir del equipo**, para **que el equipo pueda seguir existiendo**.

## Source references

- HU: `HU-06` en `docs/01-project-source/srs.md` y `docs/01-project-source/historias-de-usuario.md`.
- RF: `RF-08`, `RF-35`, `RF-36` en `docs/01-project-source/srs.md`.
- RB: `RB-E07`, `RB-E08`, `RB-E10`, `RB-E17`, `RB-U10` en `docs/01-project-source/srs.md`.
- RNF: `RNF-01`, `RNF-02`, `RNF-04`, `RNF-06`, `RNF-13`, `RNF-14` en `docs/01-project-source/srs.md`.
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`.
- Service context: `services/team-service/service-context.md`.
- Business rules: `docs/02-project-context/business-rules.md`, `docs/02-project-context/design/domain-business-rules.md`.
- Resolved decisions: `docs/02-project-context/known-ambiguities-and-decisions.md`.
- Contract base: `contracts/http/team-api.md`, `contracts/events/team-events.md`.

## Actor

- `Participante lider`.

## User goal

Permitir que el lider actual de un equipo activo designe como nuevo lider a otro integrante del mismo equipo antes de ejecutar la salida del equipo por HU-07.

## Scope

Included:
- Transferencia de liderazgo desde la app movil de participante (`React Native mobile`).
- Validar que el actor autenticado pertenece a un equipo activo.
- Validar que el actor autenticado es el lider actual del equipo.
- Validar que el nuevo lider indicado pertenece al mismo equipo activo.
- Validar que el nuevo lider indicado es diferente al lider actual.
- Actualizar la membresia para que exista exactamente un lider activo.
- Retornar un resultado minimo para que la UI movil muestre el nuevo lider y habilite la salida posterior por HU-07.

Out of scope:
- Salir del equipo despues de transferir liderazgo; eso pertenece a HU-07.
- Eliminar equipo creado; eso pertenece a HU-05.
- Crear equipo; eso pertenece a HU-03.
- Unirse a equipo usando codigo; eso pertenece a HU-04.
- Gestion administrativa de equipos por Administrador; eso pertenece a HU-08.
- Notificaciones en tiempo real a integrantes.
- Validar inscripciones en partidas `lobby` o `iniciada`; esa restriccion aplica a eliminacion de equipo y futuras integraciones, no a transferencia interna de liderazgo.

## Preconditions

- Usuario autenticado con rol base `Participante`.
- Usuario activo segun autenticacion/autorizacion base.
- Team Service puede identificar al participante mediante claims del token.
- El participante autenticado pertenece a un equipo activo.
- El participante autenticado es lider actual del equipo.
- El equipo tiene al menos otro integrante elegible como nuevo lider.

## Postconditions

- El lider actual queda como integrante no lider.
- El integrante seleccionado queda como unico lider del equipo.
- El equipo permanece en estado `Activo`.
- La cardinalidad del equipo no cambia.
- No se elimina ni modifica historial de partidas o participaciones previas.
- El lider anterior puede ejecutar HU-07 para salir del equipo como no lider.

## Business rules

- `RF-08`: si el lider desea salir y existen otros integrantes, debe transferir el liderazgo antes de salir.
- `RB-E07`: un equipo puede existir con minimo 1 integrante y maximo 5 integrantes.
- `RB-E08`: los jugadores pueden salir de su equipo.
- `RB-E10`: si el lider desea salir y existen otros integrantes, debe transferir el liderazgo a otro jugador antes de salir.
- `RB-E17`: la eliminacion de un equipo no elimina ni modifica historial; HU-06 no elimina el equipo, pero mantiene esta restriccion de no tocar historial.
- `RB-U10`: el liderazgo de equipo no constituye rol de Keycloak; es una condicion de negocio dentro de UMBRAL.
- Decision resuelta: `1 <= Equipo.Participantes.Count <= 5`; un equipo de un solo integrante no puede transferir liderazgo porque no tiene otro integrante elegible.

## Related requirements

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

1. Un participante autenticado que es lider de un equipo activo puede seleccionar a otro integrante del mismo equipo como nuevo lider.
2. Al transferir liderazgo exitosamente, el lider anterior deja de tener `EsLider = true` y el nuevo integrante queda con `EsLider = true`.
3. Despues de la transferencia exitosa, existe exactamente un lider en el equipo.
4. El equipo permanece activo y conserva sus integrantes.
5. Un participante que no pertenece a ningun equipo activo recibe `404` o error equivalente de recurso no encontrado, sin modificar estado.
6. Un participante que pertenece al equipo pero no es lider recibe conflicto de negocio (`409`) al intentar transferir liderazgo.
7. Si el nuevo lider indicado no pertenece al equipo activo, la operacion se rechaza con conflicto de negocio (`409`).
8. Si el nuevo lider indicado es el mismo lider actual, la operacion se rechaza con conflicto de negocio (`409`).
9. Si el equipo tiene un solo integrante, la operacion se rechaza porque no existe otro integrante elegible.
10. El endpoint exige autenticacion; sin token valido responde `401`.
11. Un actor autenticado sin condicion valida de participante recibe `403` cuando aplique por politica.
12. La respuesta exitosa permite a la app movil actualizar la vista del equipo y mostrar el nuevo lider.

## Open questions

- Ninguna bloqueante para completar el SDD de HU-06.

## Assumptions

- Team Service es la fuente de verdad para membresia, liderazgo y estado del equipo.
- HU-06 solo transfiere liderazgo; no ejecuta automaticamente la salida del lider anterior.
- La seleccion del nuevo lider se hace por `nuevoLiderUserId` de un integrante ya existente del equipo.
- No se publica evento cross-service obligatorio para cerrar HU-06; cualquier notificacion futura debera documentarse en contratos antes de implementarse.
