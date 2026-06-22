# HU-26 — Responder Trivia individual

## Identificación

| Campo | Valor |
| --- | --- |
| HU ID | HU-26 |
| Nombre | Responder Trivia individual |
| Actor | Participante |
| Cliente objetivo | React Native mobile (frontend congelado — solo backend) |
| Servicio dueño | Trivia Game Service |
| Estado SDD | Nueva |

## Referencias fuente

- `docs/01-project-source/srs.md` — HU-26, RF-20, RF-21, RF-22, RB-T11, RB-T21, RB-T24, RB-T25, RB-T26, RB-T27, RB-T28, RB-T29
- `docs/01-project-source/historias-de-usuario.md` — HU-26
- `docs/01-project-source/modelo-de-dominio.md` — agregado `PartidaTrivia`, `RespuestaTrivia`
- `docs/01-project-source/diagrama-de-clases.md` — `RespuestaTrivia`, `PartidaTrivia.RegistrarRespuestaDefinitiva()`
- `docs/02-project-context/business-rules.md` — BR-T11, BR-T13, BR-T14, BR-T15, BR-T16, BR-T17
- `docs/02-project-context/design/domain-business-rules.md` — TRIVIA-ANSWER-001, TRIVIA-ANSWER-002, TRIVIA-SCORE-001, TRIVIA-SCORE-003, TRIVIA-SCORE-004
- `docs/02-project-context/known-ambiguities-and-decisions.md` — decisión de puntaje Trivia sin ponderación por tiempo
- `docs/03-microservices/service-ownership.md`
- `docs/03-microservices/services/trivia-game-service.md`
- `services/trivia-game-service/service-context.md`
- `contracts/http/trivia-game-api.md`

## Historia de usuario

Como **Participante**, quiero **seleccionar una única respuesta por pregunta en una trivia individual**, para **que el sistema registre mi respuesta y evalúe si es correcta**.

## Objetivo de negocio

Permitir que un participante inscrito en una partida de Trivia individual en estado `Iniciada` envíe una única respuesta por pregunta activa. El sistema debe validar que la respuesta sea oportuna (dentro del tiempo límite), no repetida (mismo participante no puede responder la misma pregunta dos veces) y contra la opción correcta configurada. Si es correcta, se acumula el puntaje de la pregunta.

## Alcance

### Incluido

1. **Enviar respuesta** a la pregunta activa de una partida de Trivia individual en estado `Iniciada`.
2. **Validar** que el participante esté inscrito en la partida.
3. **Validar** que la partida esté en estado `Iniciada`.
4. **Validar** que la pregunta esté activa (no cerrada).
5. **Validar** que no exista una respuesta previa del mismo participante para la misma pregunta.
6. **Validar** respuesta contra la opción correcta de la pregunta (`IsCorrect`).
7. **Registrar** la respuesta persistida con el resultado de validación.
8. **Acumular puntaje** si la respuesta es correcta (score = `AssignedScore` de la pregunta).
9. **Cerrar la pregunta** para todos los participantes si se respondió correctamente o si se agotó el tiempo límite (lógica de cierre compartida con HU-28).
10. **Emitir evento** de dominio `RespuestaTriviaRegistradaDomainEvent` con datos de validación.

### Fuera de alcance

| Elemento | Motivo |
| --- | --- |
| Modalidad por equipos | Corresponde a HU-27 |
| Mostrar resultado al participante luego del cierre | Corresponde a HU-28 |
| Ranking en tiempo real | Corresponde a HU-30 |
| Frontend React Native mobile | Congelado para este sprint |
| Eventos RabbitMQ | Solo eventos de dominio in-process + SignalR |
| Inicio automático o cancelación por tiempo | Corresponde a HU-24 o tarea separada |

## Precondiciones

1. Participante autenticado vía Keycloak con rol **Participante**.
2. Participante inscrito en la partida de Trivia individual (existe `TriviaInscripcion`).
3. Partida en estado `Iniciada`.
4. Partida tiene modalidad `Individual`.
5. Pregunta activa (no cerrada).
6. Participante no ha respondido esta pregunta aún.

## Postcondiciones

1. `RespuestaTrivia` persistida con resultado de validación.
2. Si respuesta correcta: puntaje de la pregunta acumulado al participante.
3. Si respuesta correcta o tiempo agotado: pregunta marcada como cerrada.
4. Evento `RespuestaTriviaRegistradaDomainEvent` publicado (in-process).
5. SignalR notifica a suscriptores del cambio.

## Reglas de negocio aplicables

| ID | Regla |
| --- | --- |
| RF-20 | Una única respuesta por jugador por pregunta activa (individual) |
| RF-21 | Rechazar respuestas repetidas, tardías o fuera de estado; validar automáticamente contra opción correcta; cerrar pregunta al acertar o agotar tiempo |
| RF-22 | Puntaje directo sin ponderación por tiempo |
| RB-T11 | 1 respuesta por participante por pregunta activa |
| RB-T21 | Modalidad individual: cada jugador solo puede enviar una respuesta por pregunta |
| RB-T24 | Rechazar respuestas repetidas, tardías o fuera de pregunta activa. Si responde incorrectamente, no puede reintentar |
| RB-T25 | Pregunta se cierra cuando alguien acierta o se agota el tiempo |
| RB-T28 | Puntaje solo si respuesta es correcta |
| RB-T29 | Puntaje = assignedScore de la pregunta, sin ponderación por tiempo |
| TRIVIA-ANSWER-001 | Una respuesta definitiva por participante por pregunta activa |
| TRIVIA-ANSWER-002 | Respuesta tardía debe rechazarse |
| TRIVIA-SCORE-001 | scoreEarned = question.assignedScore |
| TRIVIA-SCORE-003 | Tiempo no afecta puntaje |

## Criterios de aceptación

### CA-01 — Respuesta correcta aceptada

**Dado** un participante autenticado e inscrito en una partida de Trivia individual en estado `Iniciada` con una pregunta activa
**Cuando** envía una respuesta con la opción correcta dentro del tiempo límite
**Entonces** el sistema registra la respuesta como correcta, acumula el puntaje de la pregunta y responde 200 OK.

### CA-02 — Respuesta incorrecta aceptada (sin puntaje)

**Dado** un participante autenticado e inscrito en una partida de Trivia individual en estado `Iniciada` con una pregunta activa
**Cuando** envía una respuesta con una opción incorrecta dentro del tiempo límite
**Entonces** el sistema registra la respuesta como incorrecta, no acumula puntaje y responde 200 OK.

### CA-03 — Rechazar respuesta repetida

**Dado** un participante que ya respondió la pregunta activa
**Cuando** intenta enviar otra respuesta para la misma pregunta
**Entonces** el sistema responde 409 Conflict.

### CA-04 — Rechazar respuesta tardía

**Dado** un participante en una partida con pregunta activa cuyo tiempo límite ya expiró
**Cuando** intenta enviar una respuesta
**Entonces** el sistema responde 409 Conflict.

### CA-05 — Rechazar respuesta si partida no está iniciada

**Dado** un participante en una partida en estado `Lobby`, `Cancelada` o `Terminada`
**Cuando** intenta enviar una respuesta
**Entonces** el sistema responde 409 Conflict.

### CA-06 — Rechazar respuesta si participante no está inscrito

**Dado** un usuario autenticado que no está inscrito en la partida
**Cuando** intenta enviar una respuesta
**Entonces** el sistema responde 404 o 403.

### CA-07 — Rechazar respuesta si modalidad es Equipo

**Dado** un participante en una partida de Trivia por equipos
**Cuando** intenta enviar una respuesta individual
**Entonces** el sistema responde 409 Conflict (esta HU solo cubre individual; equipo es HU-27).

### CA-08 — Rechazar respuesta si pregunta no está activa

**Dado** un participante en una partida iniciada donde no hay pregunta activa (aún no se abrió primera pregunta, o todas están cerradas)
**Cuando** intenta enviar una respuesta
**Entonces** el sistema responde 409 Conflict.

### CA-09 — Puntaje correcto sin ponderación por tiempo

**Dado** una respuesta correcta donde la pregunta tiene `assignedScore = 100` y `timeLimitSeconds = 30`
**Cuando** el participante responde correctamente
**Entonces** el score acumulado es exactamente 100, sin importar el tiempo empleado.

## Supuestos explícitos

1. **Pregunta activa**: la partida gestiona internamente `PreguntaActualId` y `EstadoPregunta` (activa/cerrada). La primera pregunta se activa al iniciar la partida (HU-24) o mediante un mecanismo de apertura sincronizada (HU-25).
2. **Cierre por acierto**: cuando un participante acierta, la pregunta se cierra para *todos* los participantes (RF-21). Esto aplica para individual.
3. **Cierre por tiempo**: el handler debe verificar si el tiempo límite de la pregunta ha expirado antes de aceptar la respuesta.
4. **Sin autocalificación de cierre**: el endpoint solo acepta respuestas. El cierre automático por tiempo se manejará con un mecanismo externo (background job o validación en el handler).
5. **El participante se identifica** por el `sub` del JWT o claim equivalente.

## Preguntas abiertas

1. **¿Cómo se determina la pregunta activa?** El agregado `PartidaTrivia` debe exponer `PreguntaActualId` y `EstadoPregunta`. Se asume que al iniciar la partida (HU-24) la primera pregunta se activa automáticamente.
2. **¿Quién cierra la pregunta por tiempo?** Inicialmente el handler de respuesta valida tiempo. En una iteración futura se agregará un background job. Se documenta para HU-28 o HU-25.
