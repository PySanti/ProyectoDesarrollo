# Operaciones de Sesion Events

## Status

Current event contract index. Concrete payloads require a current-doctrine SDD before implementation.

## Publisher

`Operaciones de Sesion`

## Event Registry

| Event | Trigger | Consumers | Status |
|---|---|---|---|
| `PartidaPublicadaEnLobby` | A partida is published and moved to lobby. | Defined by SDD | Payload registered (SP-3a) |
| `PartidaIniciada` | A partida starts manually or automatically. | Defined by SDD | Payload registered (SP-3b) |
| `JuegoActivado` | The next sequential game becomes active. | Defined by SDD | Payload registered (SP-3b) |
| `PartidaCancelada` | A partida is auto-cancelled when minimums are not met at start. | Defined by SDD | Payload registered (SP-3b) |
| `RespuestaTriviaValidada` | Cada respuesta registrada. | Defined by SDD | Payload registered (SP-3c) |
| `TesoroQRValidado` (SP-3d, SP-3e-3) | Cada intento de tesoro registrado | Registered | { partidaId, sesionPartidaId, juegoId, etapaId, participanteId, resultado, instante, equipoId? } |
| `EtapaBDTGanada` (SP-3d, SP-3e-3) | Validación correcta dentro de ventana | Registered | { partidaId, sesionPartidaId, juegoId, etapaId, participanteId, puntaje, tiempoResolucionMs, equipoId? } |
| `EtapaBDTCerrada` (SP-3d, SP-3e-3) | Cierre de etapa (ganador / tiempo / avance operador) | Registered | { partidaId, sesionPartidaId, juegoId, etapaId, motivo, fechaCierre, ganadorParticipanteId?, ganadorEquipoId? } |
| `EtapaBDTActivada` (SP-3d) | Se activa una etapa: inicio del juego BDT, avance del operador, o auto-avance al cerrarse la anterior | Registered | { partidaId, sesionPartidaId, juegoId, etapaId, orden, tiempoLimiteSegundos, fechaActivacion } |
| `PuntajeTriviaIncrementado` | Primera respuesta correcta en una pregunta Trivia. | Defined by SDD | Payload registered (SP-3c) |
| `PreguntaTriviaActivada` | Se activa una pregunta: al iniciar el juego, por avance del operador, o al auto-avanzar tras cerrarse la anterior por respuesta correcta (RF-22). | Defined by SDD | Payload registered (SP-3c) |
| `PreguntaTriviaCerrada` | Se cierra una pregunta Trivia. | Defined by SDD | Payload registered (SP-3c) |
| `PartidaFinalizada` | A partida finishes. | Defined by SDD | Payload registered (SP-3b) |
| `PistaEnviada` (SP-3f-4, SP-3e-4) | El operador envía una pista a un participante o equipo durante un juego BDT activo. | Defined by SDD | Payload registered (SP-3f-4 / SP-3e-4) |
| `ConvocatoriaCreada` (SP-3e-1) | Se preinscribe un equipo: cada miembro del snapshot recibe una convocatoria. | Defined by SDD | Payload registered (SP-3e-1) |
| `ConvocatoriaRespondida` (SP-3e-1) | Un convocado acepta o rechaza su convocatoria. | Defined by SDD | Payload registered (SP-3e-1) |

## Rule

Concrete exchange names, queue names, routing keys, payloads, versions and idempotency rules are documented only after a current-doctrine SDD defines them.

## Payloads (registered)

### `PartidaPublicadaEnLobby` (SP-3a)

Emitted after a partida is published to Lobby. In SP-3a it is published through a **No-Op** port (no broker delivery yet); the exchange/queue/routing-key/idempotency are defined by the RabbitMQ backbone slice.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "modalidad": "Individual | Equipo",
  "minimosParticipacion": 1,
  "maximosParticipacion": 10
}
```

### `PartidaIniciada` (SP-3b)

Emitted after a partida starts (manual or automatic). No-Op port in SP-3b.

```json
{ "partidaId": "guid", "sesionPartidaId": "guid", "fechaInicio": "datetime", "primerJuegoId": "guid", "primerJuegoOrden": 1 }
```

### `JuegoActivado` (SP-3b)

Emitted when a game becomes active — at start (first game) and on each sequential advance.

```json
{ "partidaId": "guid", "sesionPartidaId": "guid", "juegoId": "guid", "orden": 1, "tipoJuego": "Trivia | BusquedaDelTesoro" }
```

### `PartidaFinalizada` (SP-3b)

Emitted when the last game finishes and the partida reaches Terminada. The consolidated ranking is computed by Puntuaciones in SP-4; this payload only signals finish.

```json
{ "partidaId": "guid", "sesionPartidaId": "guid", "fechaFin": "datetime" }
```

### `PartidaCancelada` (SP-3b)

Emitted when a partida is auto-cancelled at start because participation minimums were not met.

```json
{ "partidaId": "guid", "sesionPartidaId": "guid", "motivo": "MinimosNoAlcanzados", "fechaCancelacion": "datetime" }
```

### `RespuestaTriviaValidada` (SP-3c)

Emitted for every registered answer in a Trivia game. Emitted via **No-Op** port (no broker delivery yet); the exchange/queue/routing-key/idempotency are defined by the RabbitMQ backbone slice.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "juegoId": "guid",
  "preguntaId": "guid",
  "participanteId": "guid",
  "opcionId": "guid",
  "esCorrecta": true,
  "instante": "datetime",
  "equipoId": "guid | null"
}
```

### `PuntajeTriviaIncrementado` (SP-3c)

Emitted only on the first correct answer for a Trivia question (the one that closes it). No-Op port in SP-3c.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "juegoId": "guid",
  "preguntaId": "guid",
  "participanteId": "guid",
  "puntaje": 10,
  "tiempoRespuestaMs": 1234,
  "equipoId": "guid | null"
}
```

### `PreguntaTriviaActivada` (SP-3c)

Emitted when a Trivia question becomes active: at game start (first question), on operator advance, or on auto-advance after the previous question closed by correct answer (RF-22). No-Op port in SP-3c.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "juegoId": "guid",
  "preguntaId": "guid",
  "orden": 1,
  "tiempoLimiteSegundos": 30,
  "fechaActivacion": "datetime"
}
```

### `PreguntaTriviaCerrada` (SP-3c)

Emitted when a Trivia question closes (by correct answer, by timeout, or by operator advance). `ganadorParticipanteId` is present only when the question closed by a correct answer. No-Op port in SP-3c.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "juegoId": "guid",
  "preguntaId": "guid",
  "motivo": "RespuestaCorrecta | AvanceOperador | Tiempo",
  "fechaCierre": "datetime",
  "ganadorParticipanteId": "guid?",
  "ganadorEquipoId": "guid?"
}
```

> **SP-3c note:** all four events above are emitted via `NoOpSesionEventsPublisher` (no broker delivery in this slice). `RankingTriviaActualizado` is deferred to Puntuaciones (SP-4).

### `TesoroQRValidado` (SP-3d)

Emitted for every QR treasure validation attempt in a BDT game, regardless of result. Emitted via **No-Op** port (no broker delivery in this slice).

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "juegoId": "guid",
  "etapaId": "guid",
  "participanteId": "guid",
  "resultado": "Valido | Invalido | NoLegible | NoCorrespondeEtapaActiva",
  "instante": "datetime",
  "equipoId": "guid | null"
}
```

### `EtapaBDTGanada` (SP-3d)

Emitted only when a QR validation is correct and within the active stage window (the validation that closes the stage by winner). Carries the configured stage score. No-Op port in SP-3d.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "juegoId": "guid",
  "etapaId": "guid",
  "participanteId": "guid",
  "puntaje": 10,
  "tiempoResolucionMs": 1234,
  "equipoId": "guid | null"
}
```

### `EtapaBDTCerrada` (SP-3d)

Emitted when a BDT stage closes, regardless of reason. `ganadorParticipanteId` is present only when `motivo` is `Ganador`. No-Op port in SP-3d.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "juegoId": "guid",
  "etapaId": "guid",
  "motivo": "Ganador | Tiempo | AvanceOperador",
  "fechaCierre": "datetime",
  "ganadorParticipanteId": "guid?",
  "ganadorEquipoId": "guid?"
}
```

### `EtapaBDTActivada` (SP-3d)

Emitted when a BDT stage becomes active: at BDT game start (first stage), on operator advance, or on auto-advance after the previous stage closed. No-Op port in SP-3d.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "juegoId": "guid",
  "etapaId": "guid",
  "orden": 1,
  "tiempoLimiteSegundos": 60,
  "fechaActivacion": "datetime"
}
```

> **Slice-E note (SP-3e-1..4):** `equipoId`/`ganadorEquipoId` son la identidad dual de la modalidad Equipo: `null` ⇔ partida `Individual`. En `Equipo`, `participanteId`/`ganadorParticipanteId` siguen llevando el **autor real** de la acción y `equipoId`/`ganadorEquipoId` el equipo al que se acredita.

> **SP-3d note:** all four BDT events above are emitted via `NoOpSesionEventsPublisher` (no broker delivery in this slice). `RankingBDTActualizado` is deferred to Puntuaciones (SP-4). `motivo` values are the `ToString()` of the `MotivoCierreEtapa` enum.

### `PistaEnviada` (SP-3f-4 / SP-3e-4)

Emitted when the operator sends a clue during an active BDT stage. Event-only (no persistence — BR-B06 "recorded" deferred). Real-time delivery via SignalR: group `participante:{participanteDestinoId}` **xor** `equipo:{equipoDestinoId}` (exactly one destination per event).

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "juegoId": "guid",
  "participanteDestinoId": "guid | null",
  "texto": "string",
  "instante": "datetime",
  "equipoDestinoId": "guid | null"
}
```

### `ConvocatoriaCreada` (SP-3e-1)

Emitted once per team-member snapshot entry when a team is pre-inscribed. Real-time delivery via SignalR to group `participante:{usuarioId}`; broker delivery deferred to the RabbitMQ backbone slice.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "convocatoriaId": "guid",
  "equipoId": "guid",
  "usuarioId": "guid"
}
```

### `ConvocatoriaRespondida` (SP-3e-1)

Emitted when a summoned member accepts or rejects their convocatoria. Currently **No-Op** on the SignalR side (deliberate — the operator lobby view polls; see SP-3f-2 no-broadcast list); broker delivery deferred.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "convocatoriaId": "guid",
  "usuarioId": "guid",
  "estadoConvocatoria": "Aceptada | Rechazada"
}
```
