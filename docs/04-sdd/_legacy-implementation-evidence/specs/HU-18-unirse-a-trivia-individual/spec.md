# HU-18 — Unirse a Trivia individual

## Identificación

| Campo | Valor |
| --- | --- |
| HU ID | HU-18 |
| Nombre | Unirse a Trivia individual |
| Actor | Participante |
| Cliente objetivo | React Native mobile |
| Servicio dueño | Trivia Game Service |
| Estado SDD | Nueva |

## Referencias fuente

- `docs/01-project-source/historias-de-usuario.md` — HU-18
- `docs/01-project-source/srs.md` — RF-05, RF-06, RF-11, RF-18, RB-T11, RB-T12, RB-08, RB-18, RB-20
- `docs/02-project-context/first-delivery-scope.md` — HU-18 activa
- `docs/03-microservices/service-ownership.md` — Trivia Game Service

## Historia de usuario

Como **Participante**, quiero **unirme a una Trivia individual publicada**, para **participar individualmente**.

## Alcance

### Incluido

1. Endpoint `POST /api/trivia-games/{gameId}/join` para inscripción individual.
2. Validación de game existe, estado Lobby, modalidad Individual, cupo disponible, sin duplicado.
3. Creación del agregado `TriviaInscripcion` en el dominio del Trivia Game Service.
4. Persistencia en tabla `TriviaInscripciones`.
5. Actualización de `CountInscripcionesAsync` para contar desde inscripciones reales.

### Fuera de alcance

| Elemento | Motivo |
| --- | --- |
| Inscripción por equipo | HU-19 |
| Convocatorias a equipo | HU-20 |
| UI de unión en móvil | Frontend React Native congelado |
| Inicio automático por inscripción | Será parte de HU-24 |
| Eventos de dominio / RabbitMQ | No requerido para esta HU |

## Precondiciones

1. Usuario autenticado.
2. Partida de Trivia existe y está en estado Lobby.
3. Partida es modalidad Individual.
4. Hay cupo disponible (inscriptos < MaximoJugadores).
5. Usuario no está ya inscrito en la partida.

## Postcondiciones

1. Se crea `TriviaInscripcion` asociada al usuario y la partida.
2. El contador de inscriptos se incrementa.

## Reglas de negocio aplicables

| ID | Regla |
| --- | --- |
| RB-T11 | Cualquier jugador puede intentar entrar a una Trivia publicada. |
| RB-T12 | Si la Trivia es individual, cualquier jugador puede inscribirse mientras la partida esté en lobby y haya cupo. |
| RB-08 | Una partida en estado lobby permite inscripción de jugadores. |
| RB-18 | Los participantes pueden jugar partidas individuales aunque pertenezcan a un equipo. |
| RB-20 | En juegos individuales, el operador define el máximo de jugadores. |
| RF-18 | El sistema debe habilitar inscripciones de jugadores según su modalidad. |

## Criterios de aceptación

### CA-01 — Inscripción exitosa
**Dado** un participante autenticado y una Trivia individual en lobby con cupo
**Cuando** el participante se une
**Entonces** se registra la inscripción y retorna 200 OK.

### CA-02 — Game no existe
**Dado** un gameId inválido
**Cuando** se intenta unir
**Entonces** retorna 404 Not Found.

### CA-03 — Game no está en Lobby
**Dado** una partida en estado Iniciada o Cancelada
**Cuando** se intenta unir
**Entonces** retorna 409 Conflict con mensaje.

### CA-04 — Game es modalidad Equipo
**Dado** una partida de modalidad Equipo
**Cuando** un participante intenta unirse individualmente
**Entonces** retorna 400/409 con mensaje "Debes ser líder de un equipo para entrar en este evento".

### CA-05 — Cupo lleno
**Dado** una partida con máximo de jugadores alcanzado
**Cuando** se intenta unir
**Entonces** retorna 409 Conflict con mensaje.

### CA-06 — Ya inscrito
**Dado** un participante ya inscrito en la partida
**Cuando** intenta unirse nuevamente
**Entonces** retorna 409 Conflict con mensaje.

## Supuestos explícitos

1. `UserId` se obtiene del claim "sub" del JWT autenticado.
2. `TriviaInscripcion` es un nuevo agregado separado de `PartidaTrivia`.
3. `CountInscripcionesAsync` se actualizará para consultar desde `TriviaInscripcion`.

## Preguntas abiertas

Ninguna.
