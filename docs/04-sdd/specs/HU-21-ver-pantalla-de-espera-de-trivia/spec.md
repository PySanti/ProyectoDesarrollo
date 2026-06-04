# HU-21 — Ver pantalla de espera de Trivia

## Identificación

| Campo | Valor |
| --- | --- |
| HU ID | HU-21 |
| Nombre | Ver pantalla de espera de Trivia |
| Actor | Participante |
| Cliente objetivo | React Native mobile |
| Servicio dueño | Trivia Game Service |
| Estado SDD | Nueva |

## Referencias fuente

- `docs/01-project-source/historias-de-usuario.md` — HU-21
- `docs/01-project-source/srs.md` — RF-13, RF-18, RB-08, RB-10, RB-11, RB-T10
- `docs/02-project-context/first-delivery-scope.md` — HU-21 activa
- `docs/03-microservices/service-ownership.md` — Trivia Game Service

## Historia de usuario

Como **Participante**, quiero **ver una pantalla de espera después de unirme**, para **saber que estoy inscrito y esperar el inicio de la partida**.

## Alcance

### Incluido

1. Endpoint `GET /api/trivia-games/{gameId}/lobby` para consultar el estado del lobby.
2. Validación de que el usuario autenticado esté inscrito en la partida.
3. Datos devueltos: nombre, estado, modalidad, tiempo de inicio, cantidad de participantes actual y máximo, lista de participantes.
4. Notificación en tiempo real via SignalR cuando la partida inicia o se cancela.
5. Notificación en tiempo real via SignalR cuando un nuevo participante se une al lobby.

### Excluido

1. Pantalla de UI (frontend React Native congelado).
2. Historial de eventos del lobby.
3. Aceptación/rechazo de participantes por el operador (HU-23 lo cubre).

## Requerimientos relacionados

| ID | Descripción |
| --- | --- |
| RF-13 | El sistema debe actualizar en tiempo real los cambios relevantes: lobby, estados, etc. |
| RF-18 | El sistema debe habilitar inscripciones de jugadores según su modalidad. |
| RB-08 | Una partida en estado lobby permite inscripción de jugadores. |
| RB-10 | Una partida en estado cancelada no acepta nuevas acciones. |
| RB-11 | Una partida en estado terminada no acepta nuevas acciones de juego. |
| RB-T10 | Al iniciar el lobby, la partida queda publicada para todos los jugadores. |

## Flujo principal

1. El participante se une a una Trivia individual (HU-18) o por equipo (HU-19).
2. El sistema redirige o el participante navega a la pantalla de espera.
3. La app móvil consulta `GET /api/trivia-games/{id}/lobby` y muestra los datos.
4. La app móvil se suscribe al hub SignalR `/hubs/trivia-lobby` para recibir actualizaciones.
5. Cuando el operador inicia la partida (HU-24), SignalR notifica y la app navega a la pantalla de juego.
6. Si el operador cancela la partida, SignalR notifica y la app muestra el mensaje.

## Reglas de negocio aplicables

| ID | Regla |
| --- | --- |
| RB-08 | Una partida en estado lobby permite inscripción de jugadores. |
| RB-10 | Una partida en estado cancelada no acepta nuevas acciones de juego. |
| RB-11 | Una partida en estado terminada no acepta nuevas acciones de juego. |
| RB-T10 | Al iniciar el lobby, la partida de Trivia queda publicada para todos los jugadores. |

## Criterios de aceptación

### CA-01 — Consultar lobby exitoso
**Dado** un participante autenticado e inscrito en una Trivia en lobby
**Cuando** consulta `GET /api/trivia-games/{id}/lobby`
**Entonces** retorna 200 OK con nombre, estado, modalidad, participantes y tiempo de inicio.

### CA-02 — No inscrito
**Dado** un participante autenticado NO inscrito en la partida
**Cuando** consulta el lobby
**Entonces** retorna 403 Forbidden.

### CA-03 — Partida no existe
**Dado** un gameId inválido
**Cuando** se consulta el lobby
**Entonces** retorna 404 Not Found.

### CA-04 — Partida no está en Lobby
**Dado** una partida en estado Iniciada o Terminada
**Cuando** se consulta el lobby
**Entonces** retorna 200 OK con estado actual (no lobby); la app decide qué mostrar.

### CA-05 — Notificación en tiempo real al iniciar
**Dado** un participante en la pantalla de espera
**Cuando** el operador inicia la partida
**Entonces** el participante recibe notificación SignalR con el nuevo estado.

### CA-06 — Notificación en tiempo real al cancelar
**Dado** un participante en la pantalla de espera
**Cuando** el operador cancela la partida
**Entonces** el participante recibe notificación SignalR.

## Supuestos explícitos

1. `UserId` se obtiene del claim "sub" del JWT autenticado.
2. SignalR hub se integra con los handlers `JoinTriviaGameCommandHandler` y `StartTriviaGameCommandHandler`.
3. El frontend React Native se suscribe al hub para recibir actualizaciones.

## Preguntas abiertas

Ninguna.
