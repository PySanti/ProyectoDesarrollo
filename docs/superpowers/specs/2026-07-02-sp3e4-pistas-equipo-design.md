# SP-3e-4 — Pistas BDT en modalidad Equipo (destino equipo, grupo SignalR `equipo:{id}`)

- **Slice:** SP-3e-4 (cuarto y último de slice-E). Base: SP-3e-3 (runtime BDT Equipo, commits `c7f5c52..9d3170e`, APPROVED).
- **Servicio:** Operaciones de Sesión (único servicio tocado).
- **Cierra:** el hueco de pistas en Equipo — hoy `PrepararPista` exige inscripción individual, así que el operador recibe 403 al enviar pista a cualquier convocado de una partida Equipo.
- **Fuera de alcance:** persistencia de pistas (BR-B06 "clues are recorded" sigue diferido desde SP-3f-4 — event-only), pista a miembro específico dentro de un equipo (decisión: solo equipo entero en modalidad Equipo), minors diferidos de SP-3e-2/3, clientes móvil/web.

## 1. Objetivo

BR-B06: el operador puede enviar pistas a participantes/**equipos** específicos durante un juego BDT activo. En Individual funciona (SP-3f-4: `POST partidas/{id}/pistas` → evento seam → SignalR a `participante:{destino}`). Este slice habilita el destino **equipo**: la pista llega a todos los miembros conectados del equipo vía un grupo SignalR nuevo `equipo:{equipoId}` (decisión aprobada: enfoque B grupo-de-equipo; A fan-out-por-miembro descartado por el usuario).

## 2. Reglas de dominio

- **Destino en modalidad Equipo = el equipo entero.** No hay pista a miembro individual en sesión Equipo (YAGNI, decisión confirmada).
- **Destino válido** = equipo con inscripción activa en la sesión. Sin ella → `InscripcionNoEncontradaException` (404).
- **Coherencia destino↔modalidad:** destino-equipo en sesión Individual → `ModalidadNoSoportadaException` (409). Destino-participante en sesión Equipo → 403 actual (`PrepararPista` no encuentra inscripción individual — comportamiento existente, sin cambios).
- **Mismos guards de juego que Individual:** juego activo debe ser BDT (`JuegoActivoNoEsBDT`, 409) y con etapa activa (`NoHayEtapaActiva`, 409).
- **Exactamente un destino por request** (`participanteDestinoId` XOR `equipoDestinoId`) → si no, 400 (FluentValidation).
- **Quién ve la pista:** los miembros del equipo destino conectados y suscritos a la partida (grupo `equipo:{id}`). La membresía del grupo la decide el servidor con la identidad del JWT — un cliente no puede unirse al grupo de otro equipo.

## 3. Diseño

### 3.1 HTTP (endpoint existente `POST /operaciones-sesion/partidas/{partidaId}/pistas`, operador)

- `EnviarPistaRequest`: `ParticipanteDestinoId` pasa a `Guid?` + nuevo `Guid? EquipoDestinoId` (default null). JSON de clientes actuales (solo `participanteDestinoId`) sigue funcionando.
- `EnviarPistaCommandValidator`: `Texto` no vacío (actual) + exactamente uno de los dos destinos.
- `PistaEnviadaResponse`: `ParticipanteDestinoId` → `Guid?` + `Guid? EquipoDestinoId = null` trailing.
- Controller: dispatch MediatR puro, sin cambios de lógica (pasa ambos campos al command).

### 3.2 Dominio

- **Nuevo `SesionPartida.PrepararPistaEquipo(Guid equipoDestinoId)`**: `Modalidad != Equipo` → `ModalidadNoSoportadaException`; sin inscripción activa de ese equipo (`_inscripciones.Any(i => i.EquipoId == equipoDestinoId && i.EsActiva)`) → `InscripcionNoEncontradaException`; guards de juego idénticos a `PrepararPista` (BDT activo + etapa activa); devuelve `juegoId`.
- `PrepararPista` (individual) **sin cambios**.

### 3.3 Aplicación

- `EnviarPistaCommand` += `Guid? EquipoDestinoId = null`; `ParticipanteDestinoId` → `Guid?`.
- `EnviarPistaCommandHandler`: rama por destino — participante → flujo actual; equipo → `PrepararPistaEquipo` + evento con `EquipoDestinoId`.
- `PistaEnviadaEvent`: `ParticipanteDestinoId` → `Guid?` + `Guid? EquipoDestinoId = null` trailing (call sites existentes compilan: `Guid` → `Guid?` implícito + default).

### 3.4 SignalR

- **Grupo nuevo:** `SesionRealtimeMessages.GrupoEquipo(Guid equipoId)` → `"equipo:{equipoId}"`.
- **`SesionHub.SuscribirAPartida`** (rama participante): tras el lookup existente `GetByParticipanteActivoAsync`, si la sesión es Equipo resuelve la inscripción activa con convocatoria Aceptada del caller → `AddToGroupAsync(GrupoEquipo(equipoId))` + guarda `equipoId` en `Context.Items`. `DesuscribirDePartida` lo remueve. Identidad siempre del JWT `sub` (server-side).
- **`SignalRSesionEventsPublisher.PublicarPistaEnviadaAsync`**: `EquipoDestinoId != null` → `Group(GrupoEquipo(...))`; si no → `Group(GrupoParticipante(...))` (Individual intacto).
- **`PistaEnviadaPayload`**: `ParticipanteDestinoId` → `Guid?` + `Guid? EquipoDestinoId = null` trailing — único payload que cambia en el slice (necesario: el destino es el routing). Resto de impls del seam (NoOp/Composite/Fake) sin cambios de lógica (la interface no cambia, solo el record del evento).

### 3.5 Persistencia y proyecciones

- **Ninguna.** Event-only (igual SP-3f-4). Sin migración. Proyecciones sin cambios.

## 4. Testing

- **Dominio:** `PrepararPistaEquipo` happy (devuelve juegoId); sesión Individual → `ModalidadNoSoportada`; equipo sin inscripción activa → `InscripcionNoEncontrada`; sin etapa activa → `NoHayEtapaActiva`; regresión `PrepararPista` individual intacto.
- **Validator:** ningún destino → 400; ambos destinos → 400; solo uno (cualquiera) → válido.
- **Handler:** destino equipo → evento con `EquipoDestinoId` y `ParticipanteDestinoId` null + response espejo; destino participante → flujo actual con `EquipoDestinoId` null (regresión).
- **Publisher unit** (patrón `SignalRSesionEventsPublisherTests`): evento con `EquipoDestinoId` → mensaje al grupo `equipo:{id}`; sin él → grupo `participante:{id}`.
- **Hub** (patrón tests hub existentes): suscripción de convocado aceptado en sesión Equipo une a `equipo:{id}`; desuscripción lo remueve; Individual no une a grupo de equipo.
- **Regresión:** 3 suites completas verdes (Unit/Integration/Contract); baseline 308/28/48; cambios en tests existentes solo por arity/nullabilidad de records extendidos.
- **Contrato:** fila pistas actualizada (request/response nuevos campos, 400/404/409 nuevos casos, semántica Equipo) + payload documentado si el doc lista payloads.

## 5. Riesgos y mitigaciones

- **Nullabilidad de `ParticipanteDestinoId`:** pasa de `Guid` a `Guid?` en request/command/evento/payload/response — la conversión implícita cubre los call sites, pero el plan debe listar la búsqueda repo-wide de sitios (lección B13) y los tests que asserten el valor deben seguir compilando (`Assert.Equal(guid, nullable)` compila y compara correcto).
- **Hub y reconexión:** `Context.Items` es por conexión — reconectar re-ejecuta `SuscribirAPartida` y re-une al grupo; sin estado que migrar.
- **Miembro que se une tarde** (acepta convocatoria después de conectar): se suscribe tras aceptar → el hub resuelve su equipo en ese momento. Pistas enviadas antes no se re-entregan (sin persistencia — mismo trade-off aceptado en SP-3f-4 y el inbox HTTP no cubre pistas; diferido).

## 6. Follow-ups diferidos (no bloqueantes)

- Persistencia de pistas (BR-B06 "recorded") — diferido desde SP-3f-4.
- Pista a miembro específico en Equipo (si el operador lo pide como UX del mapa de geolocalización).
- Minors SP-3e-2/3: mi-sesión preferir convocatoria Aceptada; test timeout Equipo BDT; xmin child-only.
