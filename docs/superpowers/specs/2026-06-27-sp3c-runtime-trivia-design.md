# SP-3c — Runtime Trivia (Individual) · Diseño

> Fecha: 2026-06-27. Servicio: **Operaciones de Sesión**. Cliente: backend (gameplay consumido por mobile en slices posteriores). Predecesores: SP-3a (publicación→Lobby + inscripciones), SP-3b (inicio + secuenciación de juegos). Estado doc: completo (sin TODO).

## §1 — Alcance

Implementa la experiencia en vivo de un `JuegoTrivia` **activo** dentro de una `SesionPartida` ya `Iniciada` (estado runtime de SP-3b): sincronización de la pregunta actual, recepción y **validación** de respuestas, cierre de la pregunta por primera-correcta o por tiempo, avance secuencial de preguntas y, al agotarlas, avance al siguiente juego reutilizando `FinalizarJuegoActual`.

**Modalidad: Individual solamente.** En `Equipo` la inscripción ya se rechaza en SP-3a (`ModalidadNoSoportadaException`), así que un Trivia en partida `Equipo` no alcanza runtime; la regla "primera opción de cualquier miembro activo" se difiere a SP-3a-E (modalidad Equipo).

Scoring/ranking pertenece a **Puntuaciones (SP-4)**. Operaciones **valida y emite eventos**; no computa `PuntajeAcumulado` ni ranking.

### Fuera de alcance (diferido)
- Equipo Trivia → SP-3a-E.
- Barrido automático de timeout (scheduler que cierra preguntas vencidas sin operador) + push SignalR de sincronización → SP-3f.
- Backbone real RabbitMQ → slice propio antes de SP-4 (aquí los eventos van por puerto No-Op).
- Runtime BDT → SP-3d.
- Ranking/scoring nativo Trivia (`PuntajeAcumulado`, tie-break por tiempo, `RankingTriviaActualizado`) → SP-4 (Puntuaciones).
- Reconexión / recuperación de estado transitorio → SP-3e.

## §2 — Decisiones bloqueadas

1. **Contenido de preguntas vía snapshot-at-publish.** `PublicarPartida` (ya hace `GET /partidas/{id}`) captura el set completo de preguntas/opciones como snapshot inmutable en la DB de Operaciones. La sesión es autocontenida: **cero llamadas a Partidas durante el juego en vivo**. (Alternativa fetch-perezoso descartada por acoplar el juego en vivo a Partidas.)
2. **Avance de preguntas con auto-avance al cerrar (SRS RF-22); ventana de tiempo aplicada.** Primera respuesta correcta cierra la pregunta para todos **y auto-activa la siguiente pendiente** (RF-22: "al cerrar una pregunta … avanzar automáticamente a la siguiente si existe") — corrección de doctrina aplicada en FIX-RF22 (la versión inicial dejaba el avance solo al operador y deadlockeaba los juegos multi-pregunta tras un acierto). Respuestas tras el límite se rechazan. `AvanzarPregunta` (operador) cubre el caso de la pregunta **aún abierta** (skip/timeout manual): la cierra (motivo Operador/Tiempo) y activa la siguiente o reporta fin de preguntas. El **barrido automático** de timeout (cierre sin operador) va en 3f.
3. **Eventos por puerto No-Op** (`ISesionEventsPublisher`), publicados **después** de `SaveChanges`, igual que SP-3a/3b.
4. **`now` inyectado por `TimeProvider`** y pasado como parámetro a los métodos de dominio; sin `DateTime.UtcNow` inline.
5. **Resoluciones a/b/c:** (a) participante sin inscripción activa que responde → **403 Forbidden**; (b) se registran los **4 eventos** Trivia (cada uno mapea una transición real); (c) respuesta tardía (fuera de la ventana) → **409 Conflict**.

## §3 — Modelo de dominio

### Enums (nuevos)
- `EstadoPregunta { Pendiente, Activa, Cerrada }`
- `MotivoCierrePregunta { RespuestaCorrecta, AvanceOperador, Tiempo }`

### Entidades / snapshot
- **`PreguntaSnapshot`** — hija de `JuegoResumen` (sólo cuando `TipoJuego == Trivia`):
  - Config (inmutable): `PreguntaId`, `Orden` (1..n asignado por posición en el array de Partidas), `Texto`, `PuntajeAsignado`, `TiempoLimiteSegundos`, `Opciones` (`IReadOnlyList<OpcionSnapshot>`).
  - Runtime: `Estado` (`EstadoPregunta`, default `Pendiente`), `FechaActivacion?`, `FechaCierre?`, `MotivoCierre?`, `GanadorParticipanteId?`, `Respuestas` (`IReadOnlyList<RespuestaTrivia>`).
  - Métodos `internal`: `Activar(now)` (guard `Pendiente`→`Activa`, set `FechaActivacion`), `Cerrar(motivo, now, ganador?)` (guard `Activa`→`Cerrada`).
- **`OpcionSnapshot`** (inmutable): `OpcionId`, `Texto`, `EsCorrecta`.
- **`RespuestaTrivia`** — hija de `PreguntaSnapshot` (runtime): `ParticipanteId`, `OpcionId`, `EsCorrecta`, `Instante`.

### Cambios en entidades existentes
- `JuegoResumen`: contiene la lista `PreguntaSnapshot` (vacía para BDT). `Activar()` para un Trivia activa además su primera pregunta por `Orden`. Helpers de sólo-lectura: `PreguntaActiva` (la `Activa` o null), `SiguientePreguntaPendiente`.
- `ConfiguracionSnapshot` + el record de publicación: llevan las preguntas/opciones por juego Trivia.

### Invariantes
- A lo sumo **una** `PreguntaActiva` por `JuegoTrivia` mientras el juego está `Activo`.
- Una `RespuestaTrivia` por (`participante`, `pregunta`) — no se permite duplicado.
- Una pregunta `Cerrada` no acepta respuestas.
- `GanadorParticipanteId` se fija sólo en cierre por `RespuestaCorrecta` (el primero correcto dentro de la ventana).

## §4 — Transiciones (todo a través del agregado `SesionPartida`)

**`ResponderPregunta(participanteId, opcionId, tieneInscripcionActiva, now) → ResultadoRespuesta`**
1. `Estado != Iniciada` → `SesionNoIniciadaException` (409).
2. Juego activo no es Trivia → `JuegoActivoNoEsTriviaException` (409).
3. No hay pregunta `Activa` → `NoHayPreguntaActivaException` (409).
4. `!tieneInscripcionActiva` → `ParticipanteNoInscritoException` (**403**).
5. Participante ya respondió esta pregunta → `RespuestaDuplicadaException` (409).
6. `now > FechaActivacion + TiempoLimiteSegundos` → `PreguntaFueraDeTiempoException` (**409**).
7. Registra `RespuestaTrivia` (`EsCorrecta` = la opción elegida es la correcta). Si **correcta y pregunta aún abierta** → `Cerrar(RespuestaCorrecta, now, ganador=participante)`.
8. Devuelve `ResultadoRespuesta { EsCorrecta, CerroPregunta, Puntaje? }` (Puntaje = `PuntajeAsignado` sólo si fue la correcta ganadora).

**`AvanzarPregunta(now) → ResultadoAvancePregunta`** (operador)
1. `Estado != Iniciada` → `SesionNoIniciadaException` (409).
2. Juego activo no es Trivia → `JuegoActivoNoEsTriviaException` (409).
3. No hay pregunta `Activa` → `NoHayPreguntaActivaException` (409).
4. Cierra la activa: `motivo = Tiempo` si `now ≥ FechaActivacion + TiempoLimiteSegundos`, si no `AvanceOperador` (sin ganador).
5. Activa la siguiente `Pendiente` por `Orden`; si no hay → `sinMasPreguntas = true` (no finaliza el juego aquí).
6. Devuelve `ResultadoAvancePregunta { PreguntaCerradaOrden, PreguntaActivadaOrden?, SinMasPreguntas }`.

**`FinalizarJuegoActual(now)` (existente SP-3b) — guard añadido para Trivia**
- Si el juego activo es Trivia y le quedan preguntas `Activa` o `Pendiente` → `JuegoConPreguntasPendientesException` (409). El flujo `AvanzarPregunta` hasta `sinMasPreguntas` garantiza que todas queden `Cerrada` antes de finalizar. El resto de la transición (siguiente juego / `Terminada`) no cambia.

Barrido automático de timeout → 3f. La ventana de tiempo se aplica ya en pasos 6/4 anteriores.

## §5 — API (3 endpoints nuevos)

| Capacidad | Método | Ruta gateway | Rol | Éxito | Errores |
|---|---|---|---|---|---|
| Responder pregunta activa | POST | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual/respuesta` | Participante | 200 + `RespuestaTriviaResponse` | 401 sin identidad · 403 no inscrito · 404 sesión no existe · 409 no iniciada / juego no Trivia / sin pregunta activa / duplicada / fuera de tiempo |
| Avanzar pregunta | POST | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual/avance` | Operador | 200 + `AvancePreguntaResponse` | 404 · 409 no iniciada / juego no Trivia / sin pregunta activa |
| Pregunta actual | GET | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual` | Operador/Participante | 200 + `PreguntaActualDto` | 404 sesión no existe · 409 sin pregunta activa |

Body de respuesta: `{ "opcionId": "<guid>" }`. `participanteId` siempre del claim `sub` (nunca del body).

### DTOs
- `RespuestaTriviaResponse { partidaId, preguntaId, esCorrecta, cerroPregunta, puntaje? }`
- `AvancePreguntaResponse { partidaId, preguntaCerradaOrden, preguntaActivadaOrden?, sinMasPreguntas }`
- `PreguntaActualDto { partidaId, juegoId, preguntaId, orden, texto, tiempoLimiteSegundos, fechaActivacion, opciones[]{ opcionId, texto } }` — **participant-safe: nunca incluye `esCorrecta`.**

Enums serializados como string. Transiciones devuelven 200 (no creación de recurso).

## §6 — Eventos (puerto No-Op, registrados en `contracts/events/operaciones-sesion-events.md`)

Publicados después de `SaveChanges`:
- `RespuestaTriviaValidada { partidaId, sesionPartidaId, juegoId, preguntaId, participanteId, opcionId, esCorrecta, instante }` — en cada respuesta registrada.
- `PuntajeTriviaIncrementado { partidaId, sesionPartidaId, juegoId, preguntaId, participanteId, puntaje, tiempoRespuestaMs }` — sólo en primera-correcta. `tiempoRespuestaMs = (instante − fechaActivacion)`. Lo consume Puntuaciones (SP-4) para el ranking nativo (suma + tie-break por tiempo).
- `PreguntaTriviaActivada { partidaId, sesionPartidaId, juegoId, preguntaId, orden, tiempoLimiteSegundos, fechaActivacion }` — al activar la primera pregunta (en `Activar` del juego) y en cada `AvanzarPregunta` que activa una nueva.
- `PreguntaTriviaCerrada { partidaId, sesionPartidaId, juegoId, preguntaId, motivo, fechaCierre, ganadorParticipanteId? }` — al cerrar una pregunta.

Activada/Cerrada son eventos de **sincronización** (los consumirá SignalR en 3f); Validada/Incrementado alimentan scoring (SP-4). Cada uno mapea una transición de estado real.

## §7 — Excepciones (nuevas, Domain salvo nota)

`JuegoActivoNoEsTriviaException`, `NoHayPreguntaActivaException`, `RespuestaDuplicadaException`, `PreguntaFueraDeTiempoException`, `JuegoConPreguntasPendientesException` → 409. `ParticipanteNoInscritoException` → **403**. Todas mapeadas en `ExceptionHandlingMiddleware` (centralizado). El 403 añade un nuevo arm al middleware.

## §8 — Persistencia

- Migración **aditiva**: tablas `preguntas_snapshot` (FK a sesión-juego), `opciones_snapshot` (FK a pregunta), `respuestas_trivia` (FK a pregunta) + columnas de estado/fechas/motivo/ganador en `preguntas_snapshot`. `Down` las dropea. Sin cambios destructivos a lo existente.
- Mapeo EF owned/relacional consistente con el estilo actual (`OperacionesSesionDbContext`). El repositorio carga el grafo completo (`SesionPartida` → juegos → preguntas → opciones/respuestas) para el runtime.

## §9 — Pruebas

- **Dominio:** snapshot con preguntas; activar juego activa primera pregunta; primera-correcta cierra + fija ganador; respuesta incorrecta no cierra; dedup por participante; rechazo fuera de tiempo; `AvanzarPregunta` cierra (motivo Operador/Tiempo) y activa siguiente; `sinMasPreguntas`; guard `JuegoConPreguntasPendientes`; agotar preguntas → `FinalizarJuegoActual` avanza al siguiente juego / `Terminada`; no fuga de `esCorrecta` en el DTO.
- **Handlers:** con fakes (repo, eventos, time) — emisión post-save de los 4 eventos; mapeo resultado→evento.
- **Controller unit:** los 3 endpoints despachan el comando/query correcto, status correcto, `sub` del claim.
- **Contract** (`WebApplicationFactory<Program>`): flujo end-to-end publicar→iniciar→responder (correcta/incorrecta/tardía/duplicada)→avanzar→agotar→finalizar; 403 no inscrito; 409 variantes.
- Reusa `FakeTimeProvider`, `FakeSesionEventsPublisher` (extendido con los nuevos métodos), `TestAuthHandler`.

## §10 — Conformidad / doctrina

- Estructura graduada intacta (carpetas Application exactas, controller nativo + MediatR, repos en Domain/Abstractions, middleware centralizado).
- Límites duros: sin acceso a DB ajena; el contenido de preguntas entra una sola vez por HTTP en publicación; en vivo se lee de la propia DB.
- R1/ADR-0010 sin cambios: estado runtime sólo en Operaciones.
- Eventos por puerto No-Op; SignalR/scheduler/broker diferidos.

## §11 — Watch-items heredados

- Token de concurrencia en `SesionPartida` ([[sp3f-concurrency-token]]) sigue diferido a 3f: dos `ResponderPregunta` concurrentes podrían registrar dos "primeras correctas" bajo last-write-wins. Hoy inocuo (un solo proceso, sin broker); en 3f el token (`rowversion`/`xmin`) o la idempotencia de broker lo resuelve. Anotar en el plan, no resolver aquí.
- Prohibición de cleanups git amplios en dispatches de implementación (lección SP-3b) se mantiene en cada brief.

## §12 — Traceability

Al cerrar el slice: fila SP-3c en `docs/04-sdd/traceability-matrix.md` (servicio Operaciones; contratos http+events actualizados; estado + suite; diferimientos Equipo/3d/3e/3f/SP-4).
