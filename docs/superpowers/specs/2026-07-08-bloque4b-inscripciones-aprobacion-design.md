# Bloque 4B — Aprobación de inscripciones por el operador (HU-19) — Design

Fecha: 2026-07-08 · Servicio: **Operaciones de Sesión** · Clientes: web (operador) + mobile (participante)
Historia: **HU-19** — *"Como Operador quiero observar y aceptar o rechazar los participantes/equipos que solicitan inscribirse a la partida publicada."*
Origen: `docs/04-sdd/auditorias/2026-07-06-auditoria-cobertura-requisitos.md` (hallazgo Bloque 4, fila HU-19). Slice hermano de 4A (equipos-admin), separado porque cambia el flujo de inscripción y vive en otro servicio.

## Problema

Hoy inscribirse deja la inscripción en `Activa` de inmediato (tanto `SesionPartida.Inscribir` individual como `PreinscribirEquipo`). No hay paso de aprobación. HU-19 exige que el operador **observe** las solicitudes en el lobby y las **acepte o rechace** mientras la partida está en `Lobby`.

## Decisiones de diseño (fijadas en brainstorming)

1. **Aprobación siempre obligatoria.** Toda inscripción/preinscripción en una partida publicada nace `Pendiente`; solo cuenta para mínimos, cupo y juego una vez que el operador la acepta. Aplica por igual a `Individual` y `Equipo`.
2. **Equipo: aprobar → luego convocatorias.** La preinscripción nace `Pendiente` **sin** emitir convocatorias; guarda el snapshot de miembros. Al aceptar, recién ahí se emiten las `Convocatoria` a cada miembro.
3. **Rechazo terminal, re-solicitud permitida.** Rechazar deja la inscripción en `Rechazada` (distinta de `Cancelada`, que la hace el propio participante). El participante/equipo puede enviar una nueva solicitud (nueva inscripción `Pendiente`) mientras la partida siga en `Lobby`.
4. **BR-G09 — `Pendiente` bloquea.** Una solicitud `Pendiente` ya ocupa el cupo de "una sola participación activa a la vez": no se puede solicitar en otra partida teniendo una `Pendiente` o `Activa`. El chequeo cross-partida cuenta `Pendiente + Activa`.
5. **Cupo se valida al ACEPTAR.** Las solicitudes `Pendiente` **no** consumen `MaximosParticipacion`; se pueden acumular. El límite se aplica al aceptar (si `Activos >= Maximos` → 409 `CupoLleno`). Al solicitar solo se rechaza si el cupo de activos ya está lleno.

**Enfoque de modelado elegido:** **A — extender `InscripcionPartida`** con ciclo de vida (frente a una entidad `SolicitudInscripcion` separada o un flag booleano). Mínimo desvío del agregado `SesionPartida`, reusa la proyección del guard BR-E10 y los eventos de equipo, mantiene el lobby por polling.

## Sección 1 — Máquina de estados (dominio)

`EstadoInscripcion`:

```
{ Pendiente, Activa, Rechazada, Cancelada }
```

Transiciones (siempre con partida en `Lobby`):

```
(inscribir / preinscribir)  → Pendiente
Pendiente --operador acepta--> Activa      (terminal-de-aprobación)
Pendiente --operador rechaza-> Rechazada   (terminal)
Pendiente/Activa --participante cancela--> Cancelada (terminal)
```

`InscripcionPartida`:
- Ctor Individual y factory Equipo pasan a nacer `Estado = Pendiente` (hoy `Activa`).
- **Equipo:** la factory `PreinscribirEquipo` deja de crear `Convocatoria` en construcción; en su lugar guarda el snapshot de miembros (`IReadOnlyList<Guid>`) como estado persistido de la inscripción para poder convocar al aceptar.
- Métodos internos nuevos:
  - `internal IReadOnlyList<Convocatoria> Aceptar(DateTime now)` — exige `Estado == Pendiente`; pasa a `Activa`; si es `Equipo`, crea las `Convocatoria` desde el snapshot y las devuelve (para emitir `ConvocatoriaCreada`); si es `Individual`, devuelve lista vacía.
  - `internal void Rechazar()` — exige `Estado == Pendiente`; pasa a `Rechazada`.
- Helpers:
  - `public bool OcupaParticipacion => Estado is EstadoInscripcion.Pendiente or EstadoInscripcion.Activa;` — base de los guards de "ya inscrito" y BR-G09.
  - `public bool EsActiva => Estado == EstadoInscripcion.Activa;` — **sin cambio**; sigue siendo solo `Activa` (mínimos, juego, respuestas dependen de esto y no deben contar Pendiente).
  - `public bool EstaPendiente => Estado == EstadoInscripcion.Pendiente;`

**Persistencia del snapshot de miembros (Equipo):** el snapshot vive en la inscripción hasta que se convierte en convocatorias al aceptar. Se modela como colección de valor (`ICollection<Guid>` mapeada a tabla hija `inscripcion_miembros_snapshot`, o como columna de lista según el patrón EF existente del servicio). Al aceptar, el snapshot ya no es necesario funcionalmente pero se conserva (no se borra) por trazabilidad.

## Sección 2 — Agregado `SesionPartida`

`Inscribir(participanteId, tieneParticipacionActivaEnOtra, inscritosActivos, fecha)`:
- Guard "ya inscrito": `_inscripciones.Any(i => i.ParticipanteId == participanteId && i.OcupaParticipacion)` (antes `EsActiva`). Así una `Rechazada`/`Cancelada` previa **no** bloquea re-solicitar (decisión 3).
- `tieneParticipacionActivaEnOtra` ahora significa "tiene Pendiente **o** Activa en otra sesión" (decisión 4) — el cambio real está en la query del repo que lo calcula, no en el agregado.
- Cupo: `if (inscritosActivos >= MaximosParticipacion) throw CupoLleno` — `inscritosActivos` es el conteo de **`Activa`** (decisión 5), no de pendientes.
- Crea la inscripción `Pendiente` y la devuelve.

`PreinscribirEquipo(equipoId, callerEsLider, miembros, equipoTieneParticipacionActivaEnOtra, equiposActivos, fecha)`:
- Mismos cambios de guard/cupo con `OcupaParticipacion` y `equiposActivos` = conteo de `Activa`.
- Nace `Pendiente`, guarda el snapshot `miembros`, **no** emite convocatorias.

Métodos nuevos:
- `AceptarInscripcion(Guid inscripcionId, int inscritosActivos, DateTime now) : IReadOnlyList<Convocatoria>`
  - `Estado == Lobby` si no → `SesionNoEnLobbyException`.
  - Localiza inscripción `Pendiente` por id; si no existe → `InscripcionNoEncontradaException` (404).
  - Cupo: `if (inscritosActivos >= MaximosParticipacion) throw CupoLleno` (409). `inscritosActivos` = `Activa` actuales del mismo tipo.
  - `insc.Aceptar(now)`; devuelve convocatorias creadas (vacío en Individual).
  - **No** re-chequea BR-G09: el `Pendiente` ya era el lock, ningún otro Pendiente/Activa pudo crearse en otra partida para ese participante/equipo.
- `RechazarInscripcion(Guid inscripcionId, DateTime now) : (Guid inscripcionId, Guid? equipoId)`
  - `Estado == Lobby`; localiza `Pendiente` por id (404 si no); `insc.Rechazar()`.
  - Devuelve `equipoId?` para emitir `InscripcionEquipoCancelada` (guard BR-E10) cuando sea equipo.

**Sin cambios:** `AplicarInicio` (mínimos siguen contando `EsActiva`, y para Equipo `EsActiva && ConvocatoriasAceptadas >= 1`), `ResponderPregunta`, `ValidarTesoro`, `PrepararPista*` (todos filtran `EsActiva`, excluyen `Pendiente` automáticamente).

## Sección 3 — Endpoints y DTOs

Nuevos endpoints de operador (política `GestionarPartidas`), sirven a Individual y Equipo (identificados por `inscripcionId`):

| Método | Ruta | Éxito | Errores |
|---|---|---|---|
| POST | `operaciones-sesion/partidas/{partidaId:guid}/inscripciones/{inscripcionId:guid}/aceptacion` | 200 (lobby actualizado) | 404 no encontrada/partida; 409 cupo lleno; 409 no en lobby |
| POST | `operaciones-sesion/partidas/{partidaId:guid}/inscripciones/{inscripcionId:guid}/rechazo` | 200 (lobby actualizado) | 404; 409 no en lobby |

Comandos: `AceptarInscripcionCommand(partidaId, inscripcionId)` y `RechazarInscripcionCommand(partidaId, inscripcionId)` con sus handlers. Los handlers cargan la sesión, aplican el método de dominio, guardan y emiten eventos (best-effort, ADR-0012).

Endpoints existentes (inscribir/preinscribir/cancelar/convocatoria): **sin cambio de firma**; ahora la inscripción resultante es `Pendiente`.

`LobbyDto` extendido:

```csharp
public sealed record LobbyDto(
    Guid PartidaId, Guid SesionPartidaId, string Estado, string Modalidad,
    int MinimosParticipacion, int MaximosParticipacion, int InscritosActivos,
    IReadOnlyList<Guid> Participantes,                 // ACTIVOS (sin cambio semántico)
    IReadOnlyList<EquipoLobbyDto> Equipos,             // ACTIVOS (sin cambio semántico)
    IReadOnlyList<SolicitudIndividualDto> SolicitudesPendientesIndividual,
    IReadOnlyList<SolicitudEquipoDto> SolicitudesPendientesEquipo);

public sealed record SolicitudIndividualDto(Guid InscripcionId, Guid ParticipanteId, DateTime FechaInscripcion);
public sealed record SolicitudEquipoDto(Guid InscripcionId, Guid EquipoId, int Miembros, DateTime FechaInscripcion);
```

`Participantes`/`Equipos` se calculan sobre `EsActiva`; las nuevas listas sobre `EstaPendiente`.

`mi-sesion`: para que el participante vea su estado `Pendiente`, `GetByParticipanteActivoAsync` (repo) y el filtro del handler pasan de `EsActiva` a `OcupaParticipacion`. `InscripcionResumenDto.Estado` ya transporta el string del enum (`"Pendiente"`/`"Activa"`/`"Rechazada"`). Un participante `Rechazado`/`Cancelado` no aparece (no ocupa participación) → puede re-solicitar.

## Sección 4 — Eventos, guard BR-E10 y tiempo real

Eventos nuevos de Operaciones (exchange `umbral.operaciones-sesion`, best-effort tras `SaveChanges`, ADR-0012):

| Evento | Trigger | Payload | Routing key |
|---|---|---|---|
| `InscripcionSolicitada` | Se inscribe/preinscribe (nace Pendiente) | `{ partidaId, sesionPartidaId, inscripcionId, modalidad, participanteId?, equipoId?, instante }` | `operaciones-sesion.inscripcion-solicitada.v1` |
| `InscripcionAceptada` | El operador acepta | `{ partidaId, sesionPartidaId, inscripcionId, modalidad, participanteId?, equipoId?, instante }` | `operaciones-sesion.inscripcion-aceptada.v1` |
| `InscripcionRechazada` | El operador rechaza | `{ partidaId, sesionPartidaId, inscripcionId, modalidad, participanteId?, equipoId?, instante }` | `operaciones-sesion.inscripcion-rechazada.v1` |

Estos se archivan solos en el historial de Puntuaciones (cola `puntuaciones.operaciones-sesion.historial` ligada a `operaciones-sesion.#`) — **sin consumidor nuevo**.

Cambios en eventos existentes:
- `ConvocatoriaCreada` (`operaciones-sesion.convocatoria-creada.v1`) pasa a emitirse **al aceptar** una preinscripción de equipo, no al preinscribir (decisión 2). Payload sin cambio.
- Guard BR-E10 (proyección `participaciones_activas_equipo` en Identity):
  - `InscripcionEquipoCreada` se sigue emitiendo **al preinscribir** (un equipo `Pendiente` ya "participa" en `Lobby`, coherente con decisión 4 "Pendiente bloquea"). Sin cambio de payload/routing.
  - `InscripcionEquipoCancelada` se emite al **rechazar** (además del cancelar ya existente) para retirar `(equipoId, partidaId)` del guard. Sin cambio de payload/routing.
  - Identity **no** requiere cambios: ya consume ambos eventos; solo cambia *cuándo* se producen desde Operaciones.

Tiempo real: **ninguno nuevo.** El lobby del operador **pollea** `GET …/lobby` (coherente con la decisión SP-3f-2 de que el lobby no difunde por SignalR; `ConvocatoriaRespondida` es No-Op en SignalR por la misma razón).

## Sección 5 — Clientes (DIFERIDO a slice de migración)

**Hallazgo (2026-07-08):** la web (`TriviaOperationsPage.tsx` vía `VITE_TRIVIA_API_BASE_URL`→5015) y el móvil (`EXPO_PUBLIC_*`→5015/5016) corren **100% sobre los servicios legacy** (`trivia-game-service`/`bdt-game-service`), con DTOs propios distintos al `LobbyDto` nuevo; **ningún cliente tiene cableado a Operaciones de Sesión ni al gateway**. Exponer HU-19 en los clientes reales no es "extender un adaptador" sino un mini-proyecto de migración (modificar servicios legacy a retirar, o construir wiring nuevo cliente→gateway→Operaciones).

**Decisión:** este slice 4B cubre **solo el backend de Operaciones** (núcleo gradeable, testeable e independiente del enredo legacy). La UI de operador (aceptar/rechazar + secciones de pendientes) y el estado `Pendiente`/`Rechazada` del participante se implementan en un **slice cliente aparte**, una vez migrado el lobby a gateway/Operaciones — mismo criterio que 4A (backend sólido primero). Cuando se haga: web = dos secciones de pendientes con Aceptar/Rechazar + refetch, preservando `label`/`id`/`data-testid`/ARIA; mobile = estado "Pendiente de aprobación" → unido/"Solicitud rechazada" con re-solicitud, y convocatoria de miembro que solo llega tras la aprobación.

## Sección 6 — Testing

- **Dominio (unit):** máquina de estados (Pendiente→Activa/Rechazada); convocatorias diferidas y creadas solo al aceptar (Equipo); cupo validado al aceptar (409 si `Activos == Maximos`); `OcupaParticipacion` en guards de "ya inscrito"; mínimos al iniciar excluyen `Pendiente`; rechazo→re-solicitud permitida; cancelar Pendiente.
- **Handlers/controllers (unit):** `AceptarInscripcion`/`RechazarInscripcion` (authz `GestionarPartidas`; 200/404/409); `LobbyDto` con listas de pendientes; `mi-sesion` con `OcupaParticipacion`.
- **Contract tests:** payloads de `InscripcionSolicitada`/`Aceptada`/`Rechazada`; timing de `ConvocatoriaCreada` (al aceptar) e `InscripcionEquipoCancelada` (al rechazar).
- **Regresión:** actualizar todos los tests existentes de Operaciones que asumían "inscribir/preinscribir = `Activa`" (dominio, handlers, contract) al nuevo `Pendiente`.
- **Clientes (web/mobile):** diferido junto con la UI al slice de migración (ver Sección 5).

## Fuera de alcance

- **UI de clientes (web operador + mobile participante)** — diferida a un slice de migración (Sección 5): los clientes corren sobre servicios legacy sin cableado a Operaciones.
- Configurabilidad de la aprobación por partida (descartada: HU-19 no la pide; aprobación siempre obligatoria).
- SignalR para el lobby (el operador pollea, coherente con SP-3f-2).
- Cambios en Identity (solo cambia el *momento* de emisión de eventos ya consumidos).
- Notificación al participante por correo del resultado de la aprobación (no pedido por HU-19; el estado se ve por `mi-sesion`).

## Trazabilidad

- Historia: **HU-19**. Reglas tocadas: **BR-G09** (una participación activa a la vez, ahora Pendiente+Activa), **BR-E10** (guard de equipo, timing de eventos). Actualizar `docs/04-sdd/traceability-matrix.md` y `acceptance.md` al cerrar.
- Contratos a actualizar: `contracts/events/operaciones-sesion-events.md` (3 eventos nuevos + nota de timing de `ConvocatoriaCreada`/`InscripcionEquipoCancelada`), `contracts/http/operaciones-sesion-api.md` (2 endpoints + `LobbyDto`).
