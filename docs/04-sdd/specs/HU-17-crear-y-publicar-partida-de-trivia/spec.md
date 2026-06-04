# HU-17 — Crear y publicar partida de Trivia

## Identificación

| Campo | Valor |
| --- | --- |
| HU ID | HU-17 |
| Nombre | Crear y publicar partida de Trivia |
| Actor | Operador |
| Cliente objetivo | React web |
| Servicio dueño | Trivia Game Service |
| Estado SDD | Nueva |

## Referencias fuente

- `docs/01-project-source/srs.md` — HU-17, RF-17, RF-18, RB-T04, RB-T05, RB-T06, RB-T07, RB-T08, RB-T09
- `docs/01-project-source/historias-de-usuario.md` — HU-17
- `docs/01-project-source/modelo-de-dominio.md` — agregado `PartidaTrivia`
- `docs/01-project-source/diagrama-de-clases.md` — `PartidaTrivia`, `FormularioTrivia`
- `docs/02-project-context/business-rules.md` — BR-T04, BR-T05, BR-T06, BR-T07, BR-T08, BR-T09
- `docs/02-project-context/design/domain-business-rules.md` — TRIVIA-FORM-001
- `docs/02-project-context/known-ambiguities-and-decisions.md`
- `docs/03-microservices/service-ownership.md`
- `docs/03-microservices/services/trivia-game-service.md`
- `services/trivia-game-service/service-context.md`

## Historia de usuario

Como **Operador**, quiero **crear una partida de Trivia asociada a un formulario existente y publicarla**, para **que los participantes puedan inscribirse desde sus dispositivos móviles**.

## Objetivo de negocio

Permitir al operador crear una partida de Trivia configurando nombre, modalidad (individual/equipo), formulario asociado, límites de participación y tiempo de inicio. Al crearse, la partida queda publicada en estado `Lobby`, visible para los participantes en sus paneles móviles.

## Alcance

### Incluido

1. **Crear partida** desde el endpoint `POST /api/trivia-games`.
2. Validar que el formulario referenciado exista y esté completo.
3. Validar modalidad: valores permitidos `Individual` y `Equipo`.
4. Validar `ModoInicio`: valores permitidos `Manual`, `Automatico`, `ManualYAutomatico`.
5. Validar campos según modalidad:
   - Si `Individual`: `MaximoJugadores` es requerido (1-1000).
   - Si `Equipo`: `MaximoEquipos` es requerido (1-1000), y opcionalmente `MinimoJugadoresPorEquipo` y `MaximoJugadoresPorEquipo`.
6. Publicar la partida automáticamente en estado `Lobby`.
7. Registrar `CreatedAtUtc` y `OperadorId`.
8. Publicar evento de dominio `TriviaGameCreatedDomainEvent`.
9. Autorización: solo Operador.

### Fuera de alcance

| Elemento | Motivo |
| --- | --- |
| Frontend React web | Se implementa en rama separada |
| Editar partida después de creada | No especificado en SRS para esta HU |
| Inicio automático por tiempo | Es responsabilidad de HU-24 |
| Unión de participantes | Es responsabilidad de HU-18 y HU-19 |
| SignalR lobby updates | El endpoint crea la partida; los updates en tiempo real se manejan en HU-21 |

## Precondiciones

1. Operador autenticado con rol **Operador**.
2. Existe un formulario de Trivia completo (`IsComplete = true`).
3. Trivia Game Service disponible con PostgreSQL.

## Postcondiciones

1. Partida creada en estado `Lobby`.
2. `CreatedAtUtc` registrado.
3. Evento `TriviaGameCreatedDomainEvent` despachado.
4. Partida visible para participantes en `GET /api/trivia-games`.

## Reglas de negocio aplicables

| ID | Regla |
| --- | --- |
| RB-T04 | Solo el operador puede crear partidas de Trivia |
| RB-T05 | Toda partida de Trivia debe estar asociada a un formulario válido |
| RB-T06 | Operador debe definir nombre, modalidad, formulario, mínimos, máximos y tiempo de inicio |
| RB-T07 | Si es individual, el máximo corresponde a jugadores |
| RB-T08 | Si es por equipo, el máximo corresponde a equipos |
| RB-T09 | Si es por equipo, operador define mínimo y máximo de jugadores por equipo |
| RF-17 | Crear partida asociada a formulario válido |

## Criterios de aceptación

### CA-01 — Crear partida individual exitosamente

**Dado** un operador autenticado y un formulario completo  
**Cuando** envía `POST /api/trivia-games` con modalidad `Individual`, nombre válido, formularioId válido, minimoParticipantes, maximoJugadores y tiempoInicio futuro  
**Entonces** el sistema responde `201 Created` con la partida en estado `Lobby`.

### CA-02 — Crear partida por equipos exitosamente

**Dado** un operador autenticado y un formulario completo  
**Cuando** envía `POST /api/trivia-games` con modalidad `Equipo`, nombre válido, formularioId válido, minimoParticipantes, maximoEquipos, minimoJugadoresPorEquipo, maximoJugadoresPorEquipo y tiempoInicio futuro  
**Entonces** el sistema responde `201 Created` con la partida en estado `Lobby`.

### CA-03 — Rechazar si formulario no existe

**Dado** un operador autenticado  
**Cuando** envía `POST /api/trivia-games` con un `formularioId` inexistente  
**Entonces** el sistema responde `404 Not Found`.

### CA-04 — Rechazar si formulario está incompleto

**Dado** un operador autenticado y un formulario incompleto  
**Cuando** envía `POST /api/trivia-games` con ese `formularioId`  
**Entonces** el sistema responde `400 Bad Request` con error de formulario incompleto.

### CA-05 — Rechazar si modalidad inválida

**Dado** un operador autenticado  
**Cuando** envía `POST /api/trivia-games` con `modalidad` distinta de `Individual` o `Equipo`  
**Entonces** el sistema responde `400 Bad Request`.

### CA-06 — Rechazar si campos requeridos faltan según modalidad

**Dado** un operador autenticado  
**Cuando** envía `POST /api/trivia-games` con modalidad `Individual` sin `maximoJugadores`  
**Entonces** el sistema responde `400 Bad Request`.

### CA-07 — Acceso no autorizado

**Dado** un usuario autenticado sin rol Operador  
**Cuando** intenta crear una partida  
**Entonces** el sistema responde `403 Forbidden`.

## Requisitos relacionados

| ID | Descripción |
| --- | --- |
| RF-17 | Crear partidas de Trivia asociadas a formulario válido |
| RF-18 | Publicar lobby, habilitar inscripciones |
| RB-T04 | Solo operador puede crear partidas |
| RB-T05 | Partida debe tener formulario válido |
| RB-T06 | Definir nombre, modalidad, formulario, mínimos, máximos y tiempo de inicio |
| RB-T07 | Máximo individual = jugadores |
| RB-T08 | Máximo por equipo = equipos |
| RB-T09 | Mínimo y máximo de jugadores por equipo |

## Supuestos explícitos

1. La partida se crea directamente en estado `Lobby` — no hay estado intermedio de borrador.
2. El evento `TriviaGameCreatedDomainEvent` es in-process; no se publica por RabbitMQ en esta HU.
3. `MinimoParticipantes` no puede ser 0 ni negativo.
4. `TiempoInicio` debe ser una fecha futura.
