# Bloque 4A — Slice equipos-admin / ciclo de vida de equipos (design)

Fecha: 2026-07-08
Origen: auditoría de cobertura `docs/04-sdd/auditorias/2026-07-06-auditoria-cobertura-requisitos.md`, Bloque 4 (hallazgo sin dueño previo).

## Alcance

Cierra **end-to-end (backend + clientes)**: HU-06, HU-09, HU-48 · BR-E06 (borrado explícito con
limpieza de invitaciones) · BR-E10 · BR-E11.

**Fuera de alcance:** HU-19 (aprobación de inscripciones por el operador) se separa como slice 4B
propio en Operaciones de Sesión, con su propio ciclo spec → plan. El paso de clientes por el
gateway sigue siendo Bloque 2 (RNF-21); este slice cablea web/mobile directo a Identity, como el
resto de las HU de equipos ya end-to-end.

## Decisiones tomadas en brainstorming

1. **Bloque 4 se divide en dos slices**: 4A equipos-admin (este documento) y 4B aprobación de
   inscripciones (HU-19).
2. **Backend + clientes**: las HU se cierran con el criterio "cumplido pleno" de la auditoría
   (actor documentado, cliente documentado, backend nuevo).
3. **"Notificar" = evento RabbitMQ + correo SMTP** con el sender existente de Identity. El Bloque 5
   moverá el correo a consumidor RabbitMQ sin cambiar los eventos de este slice.
4. **El admin no toca membresía** (BR-E05 intacta): crear = nombre + líder válido; editar =
   renombrar y reasignar liderazgo. Los integrantes siguen entrando solo por invitación del líder.
5. **Guard BR-E10 por proyección local de eventos RabbitMQ** (elección explícita del equipo sobre
   la alternativa HTTP síncrona): Identity mantiene una tabla de participaciones activas por equipo
   alimentada por eventos de Operaciones. Caveat aceptado: el guard es eventualmente consistente —
   una inscripción hecha instantes antes del borrado puede no estar proyectada aún.

## Arquitectura

### Servicios tocados

- **Identity** (grueso): eliminación por líder y por admin, CRUD admin, historial de nombres,
  eventos + correos, consumidor RabbitMQ + proyección `participaciones_activas_equipo`.
- **Operaciones de Sesión** (mínimo): publicar `InscripcionEquipoCreada` e
  `InscripcionEquipoCancelada` desde los handlers existentes de preinscripción/cancelación de
  equipo. Sin cambios de comportamiento ni de API HTTP.
- **Web**: página admin "Equipos" (listar, crear, renombrar, reasignar líder,
  desactivar/reactivar, eliminar).
- **Mobile**: acción "Eliminar equipo" para el líder (aunque tenga integrantes) y vista
  "Historial de equipos".

### BR-E10 primera mitad — ya cubierta sin código nuevo

`GET /identity/teams/mine` solo devuelve equipos `Activo`
(`EquipoRepository.ObtenerEquipoActivoDe` filtra `Estado == EstadoEquipo.Activo`), y la
preinscripción de equipo en Operaciones rechaza con 409 "sin equipo activo" cuando ese endpoint
devuelve 404. Un equipo `Desactivado` por tanto no puede inscribirse en partidas nuevas. Se
documenta en contratos y se cubre con un test; no requiere cambios.

## Contratos HTTP (Identity)

### Participante / líder — `TeamsController`, policy `GestionarEquipos`

| Acción | Verbo/Ruta | Respuestas |
|---|---|---|
| Líder elimina su equipo (HU-06) | `DELETE /identity/teams/mine` | 204 · 401 sin identidad · 403 no es líder · 404 sin equipo activo · 409 participación activa en partida Lobby/Iniciada (BR-E10) |
| Historial de nombres propio (HU-48) | `GET /identity/teams/mine/history` | 200 `{ historial: [{ nombreEquipo, equipoId, fechaRegistro }] }` — siempre 200, lista vacía si no hay |

### Admin — `AdminTeamsController` nuevo, ruta `identity/admin/teams`, policy `AdminOnly` (la misma de `GovernanceController`)

| Acción | Verbo/Ruta | Notas |
|---|---|---|
| Listar equipos | `GET /identity/admin/teams` | Todos los estados, con integrantes, líder y estado |
| Detalle | `GET /identity/admin/teams/{id}` | 404 si no existe |
| Crear (HU-09) | `POST /identity/admin/teams` | `{ nombreEquipo, liderUserId }`; el líder debe ser usuario válido sin equipo activo y queda como único integrante inicial; 201 |
| Renombrar | `PATCH /identity/admin/teams/{id}/name` | `{ nombreEquipo }`; alimenta historial de nombres |
| Reasignar liderazgo | `PATCH /identity/admin/teams/{id}/leadership` | `{ nuevoLiderUserId }` entre integrantes existentes; notifica a líder anterior y nuevo |
| Desactivar / reactivar | `PATCH /identity/admin/teams/{id}/estado` | `{ estado: "Desactivado" \| "Activo" }`; solo transiciones Activo↔Desactivado (nunca desde/hacia `Eliminado`) |
| Eliminar | `DELETE /identity/admin/teams/{id}` | Mismo guard 409 de BR-E10 que el borrado por líder |

Errores mapeados por el manejo centralizado de excepciones existente en Identity.

## Dominio (`Equipo`)

Métodos nuevos:

- `EliminarPorLider(actorUserId)` — valida que el actor sea el líder; **no** exige equipo vacío
  (a diferencia de `Salir`); transiciona a `Eliminado`.
- `EliminarPorAdmin()` — transiciona a `Eliminado` sin validación de actor.
- `Desactivar()` / `Reactivar()` — Activo↔Desactivado; rechazan si el equipo está `Eliminado`.
- `Renombrar(nuevoNombre)` — mismas reglas de nombre que la creación.
- `ReasignarLiderazgo(nuevoLiderUserId)` — variante admin de `TransferirLiderazgo`, sin exigir que
  el actor sea el líder; el nuevo líder debe ser integrante.
- `CrearPorAdmin(nombreEquipo, liderUserId)` — factoría; el líder es el primer y único integrante.

**Semántica de eliminación (soft delete):** `Estado = Eliminado`. Como todas las queries de
membresía filtran por `Activo`, los integrantes quedan libres automáticamente para unirse a otro
equipo, y las filas del equipo se conservan (historial, HU-06/BR-E11). Al eliminar se borran las
invitaciones **pendientes** del equipo (BR-E06); las respondidas se conservan como historia.
El guard BR-E10 aplica a ambos caminos de eliminación (líder y admin). Desactivar no tiene guard
(BR-E10 solo restringe la eliminación).

## Datos nuevos (`umbral_identity`)

- **`historial_nombre_equipo`** `{ id, usuarioId, equipoId, nombreEquipo, fechaRegistro }`.
  Se inserta una fila por integrante cuando: (a) alguien entra al equipo — creador, invitación
  aceptada, líder asignado por admin —, y (b) el equipo se renombra — una fila por integrante
  actual con el nombre nuevo. Nunca se borra (sobrevive a la eliminación del equipo, BR-E11).
  La migración hace **backfill** desde las membresías activas actuales.
  HU-48 lee: filas del `usuarioId` del caller ordenadas por `fechaRegistro`.
- **`participaciones_activas_equipo`** `{ equipoId, partidaId, fechaRegistro }` (PK compuesta
  equipoId+partidaId). Proyección del guard: upsert con `InscripcionEquipoCreada`; delete con
  `InscripcionEquipoCancelada`, `PartidaFinalizada`, `PartidaCancelada`. Guard de borrado:
  `EXISTS(equipoId)` → 409.

## Eventos RabbitMQ

### Operaciones de Sesión publica (nuevos; registrar en `contracts/events/operaciones-sesion-events.md`)

| Evento | Routing key | Payload |
|---|---|---|
| `InscripcionEquipoCreada` | `operaciones-sesion.inscripcion-equipo-creada.v1` | `{ partidaId, sesionPartidaId, inscripcionId, equipoId, instante }` |
| `InscripcionEquipoCancelada` | `operaciones-sesion.inscripcion-equipo-cancelada.v1` | `{ partidaId, inscripcionId, equipoId, instante }` |

### Identity publica (nuevos; registrar en `contracts/events/identity-events.md`)

| Evento | Routing key | Payload |
|---|---|---|
| `EquipoEliminado` | `identity.equipo-eliminado.v1` | `{ equipoId, nombreEquipo, origen: "Lider"\|"Admin", miembros: [{ usuarioId }], instante }` |
| `LiderazgoEquipoModificado` | `identity.liderazgo-equipo-modificado.v1` | `{ equipoId, liderAnteriorUserId, nuevoLiderUserId, origen: "Admin", instante }` |
| `EquipoDesactivado` | `identity.equipo-desactivado.v1` | `{ equipoId, instante }` |
| `EquipoReactivado` | `identity.equipo-reactivado.v1` | `{ equipoId, instante }` |

### Identity consume

`InscripcionEquipoCreada`, `InscripcionEquipoCancelada`, `PartidaFinalizada`, `PartidaCancelada`
(consumidor sobre el backbone RabbitMQ de Identity de SP-5b) para mantener la proyección del
guard. El consumidor es **idempotente** (upsert/delete por clave compuesta) y tolera duplicados y
desorden.

## Notificaciones

- Eliminación de equipo → correo a **todos** los integrantes (HU-06), sender SMTP existente.
- Reasignación de liderazgo por admin → correo al líder anterior y al nuevo (HU-09).
- Best-effort, no transaccional: un fallo SMTP se loggea y no revierte la operación de dominio.
  El Bloque 5 (RNF-23) moverá el envío a consumidor RabbitMQ sin cambiar los eventos.

## Clientes

- **Web (admin):** página "Equipos" junto al panel de gobernanza: tabla
  nombre/estado/integrantes/líder y acciones crear, renombrar, reasignar líder,
  desactivar/reactivar, eliminar (con confirmación destructiva). Página nueva con el design system
  implementado (`docs/02-project-context/design/design-system.md`); no se alteran
  `label`/`id`/`data-testid`/ARIA existentes.
- **Mobile (participante/líder):** en la pantalla de equipo, el líder ve "Eliminar equipo" con
  confirmación y mensaje claro ante 409 (equipo en partida activa); vista nueva "Historial de
  equipos" con los nombres y fechas.

## Testing

- **Dominio:** unit tests de `Equipo` — eliminar por líder con integrantes, eliminar por admin,
  guards de estado, renombrar, reasignar liderazgo, crear por admin, invariantes.
- **Application:** handlers con repos falsos — limpieza de invitaciones pendientes al eliminar,
  escritura de historial en alta/renombre, guard 409 contra la proyección, publicación de eventos.
- **Controllers (obligatorio):** unit tests de las acciones nuevas de `TeamsController` y de
  `AdminTeamsController` completo.
- **Consumidor:** idempotencia de la proyección (duplicados, cancelación sin alta previa, fin de
  partida sin inscripción).
- **Operaciones:** preinscribir/cancelar publica los eventos nuevos con el payload registrado.
- **Web (vitest) / mobile (node --test):** pantallas y flujos nuevos, incluidos los caminos 409.
- **Cierre:** actualizar `acceptance.md` del spec SDD, `docs/04-sdd/traceability-matrix.md`,
  `contracts/http/identity-api.md` y `contracts/events/*.md`.

## Riesgos y caveats

- **Consistencia eventual del guard** (decisión aceptada): ventana de carrera entre inscripción y
  borrado. Mitigación parcial: la proyección se actualiza en el mismo flujo del consumidor sin
  batching; la ventana es de milisegundos-segundos en operación normal.
- **Cold start de la proyección:** al desplegar, la tabla arranca vacía; inscripciones de equipo
  activas previas al despliegue no estarían proyectadas. Aceptable en el estado actual (no hay
  producción), se anota en el plan como verificación de despliegue.
- **Equipo desactivado con inscripción ya activa:** BR-E10 solo impide inscripciones *nuevas*;
  una desactivación con partida en curso no expulsa al equipo. Comportamiento aceptado y
  documentado.
