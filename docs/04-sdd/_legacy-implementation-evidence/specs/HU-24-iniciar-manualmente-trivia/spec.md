# HU-24 — Iniciar manualmente Trivia

## Identificación

| Campo | Valor |
| --- | --- |
| HU ID | HU-24 |
| Nombre | Iniciar manualmente Trivia |
| Actor | Operador |
| Cliente objetivo | React web (frontend congelado — solo backend) |
| Servicio dueño | Trivia Game Service |
| Estado SDD | Nueva |

## Referencias fuente

- `docs/01-project-source/srs.md` — HU-24, RF-18, RB-26, RB-27, RB-T17, RB-T18
- `docs/01-project-source/historias-de-usuario.md` — HU-24
- `docs/01-project-source/modelo-de-dominio.md` — agregado `PartidaTrivia`
- `docs/01-project-source/diagrama-de-clases.md` — `EstadoPartida`, `PartidaTrivia.IniciarPartida()`
- `docs/02-project-context/business-rules.md` — BR-T17, BR-T18, BR-G02
- `docs/02-project-context/design/domain-business-rules.md` — RB-26, RB-27
- `docs/02-project-context/known-ambiguities-and-decisions.md`
- `docs/03-microservices/service-ownership.md`
- `docs/03-microservices/services/trivia-game-service.md`
- `services/trivia-game-service/service-context.md`

## Historia de usuario

Como **Operador**, quiero **iniciar manualmente una partida de Trivia desde el lobby**, para **comenzar la partida cuando se cumplan las condiciones de participación**.

## Objetivo de negocio

Permitir al operador iniciar una partida de Trivia que se encuentra en estado `Lobby` cuando se haya cumplido el mínimo de participantes configurado. El sistema debe validar que la partida permita inicio manual según su `ModoInicio`, que esté en estado `Lobby` y que alcance los mínimos de participación.

## Alcance

### Incluido

1. **Iniciar partida manualmente** desde el endpoint `POST /api/trivia-games/{id}/start`.
2. Validar que la partida exista.
3. Validar que la partida esté en estado `Lobby`.
4. Validar que `ModoInicio` permita inicio manual (`Manual` o `ManualYAutomatico`).
5. Validar que se cumpla el mínimo de participantes configurado.
6. Transicionar la partida a estado `Iniciada` y registrar `StartedAtUtc`.
7. Publicar evento de dominio `PartidaTriviaIniciadaDomainEvent`.
8. Notificar por SignalR a los suscriptores del lobby.
9. Autorización: solo Operador.

### Fuera de alcance

| Elemento | Motivo |
| --- | --- |
| Inicio automático por tiempo | Se implementará con background job en iteración futura |
| Cancelación automática por mínimos | Depende del timer de inicio automático |
| Frontend React web | Congelado para este sprint |
| Eventos RabbitMQ | El inicio solo requiere evento de dominio in-process + SignalR |

## Precondiciones

1. Operador autenticado con rol **Operador**.
2. Existe una partida en estado `Lobby`.
3. La partida tiene `ModoInicio = Manual` o `ManualYAutomatico`.
4. La partida tiene al menos `MinimoParticipantes` inscripciones registradas.
5. Trivia Game Service disponible con PostgreSQL.

## Postcondiciones

1. Partida en estado `Iniciada`.
2. `StartedAtUtc` registrado.
3. Evento `PartidaTriviaIniciadaDomainEvent` publicado.
4. SignalR notifica a suscriptores del lobby.

## Reglas de negocio aplicables

| ID | Regla |
| --- | --- |
| RB-T17 | La Trivia inicia cuando se cumple el tiempo o el operador la inicia manualmente |
| RB-T18 | Al iniciar, la partida cambia a estado iniciada |
| RB-26 | Operador no puede iniciar manualmente si no se cumplen mínimos |
| RB-12 | Toda transición de estado debe ser validada antes de aplicarse |
| RB-09 | Estado iniciada permite acciones de juego |
| HU-24-R01 | Si `ModoInicio = Automatico`, el inicio manual debe rechazarse con 409 |
| HU-24-R02 | Si `ModoInicio = ManualYAutomatico`, el inicio manual está permitido |

## Criterios de aceptación

### CA-01 — Iniciar partida manualmente cumpliendo condiciones

**Dado** un operador autenticado, una partida en `Lobby` con `ModoInicio = Manual` y suficientes inscripciones
**Cuando** solicita iniciar manualmente
**Entonces** el sistema cambia el estado a `Iniciada`, registra `StartedAtUtc` y responde 200.

### CA-02 — Rechazar inicio si partida no existe

**Dado** un operador autenticado
**Cuando** solicita iniciar una partida con ID inexistente
**Entonces** el sistema responde 404.

### CA-03 — Rechazar inicio si no está en Lobby

**Dado** un operador autenticado y una partida en estado `Iniciada`, `Cancelada` o `Terminada`
**Cuando** solicita iniciar
**Entonces** el sistema responde 409.

### CA-04 — Rechazar inicio si no se cumplen mínimos

**Dado** un operador autenticado y una partida en `Lobby` sin suficientes inscripciones
**Cuando** solicita iniciar
**Entonces** el sistema responde 409.

### CA-05 — Rechazar inicio si ModoInicio = Automatico

**Dado** un operador autenticado y una partida en `Lobby` con `ModoInicio = Automatico`
**Cuando** solicita iniciar manualmente
**Entonces** el sistema responde 409.

### CA-06 — Permitir inicio si ModoInicio = ManualYAutomatico

**Dado** un operador autenticado y una partida en `Lobby` con `ModoInicio = ManualYAutomatico` y suficientes inscripciones
**Cuando** solicita iniciar manualmente
**Entonces** el sistema cambia el estado a `Iniciada` y responde 200.

### CA-07 — Acceso no autorizado

**Dado** un usuario autenticado sin rol Operador
**Cuando** intenta iniciar una partida
**Entonces** el sistema responde 403.

## Requisitos relacionados

| ID | Descripción |
| --- | --- |
| RF-18 | Iniciar partida manualmente o automáticamente |
| RB-26 | Mínimos de participación |
| RB-27 | Cancelación automática por tiempo sin mínimos |
| RB-T17/T18 | Inicio cambia estado a Iniciada |

## Supuestos explícitos

1. El handler recibe `esInicioManual = true` porque el endpoint HTTP es una acción manual del operador.
2. El conteo de inscripciones se realiza sobre `TriviaInscripcion` para la partida.
3. No se requiere validar convocatorias aceptadas en esta HU; eso será parte de HU-19 cuando esté implementada.
