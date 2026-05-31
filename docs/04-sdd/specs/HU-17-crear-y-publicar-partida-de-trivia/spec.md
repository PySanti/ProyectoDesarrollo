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

- `docs/01-project-source/srs.md` — HU-17, RF-17, RF-18, RB-T04..T09, RB-T17, RB-T18, RB-08..RB-11, RB-26, RB-27, RB-28
- `docs/01-project-source/historias-de-usuario.md` — HU-17
- `docs/01-project-source/modelo-de-dominio.md` — agregado `PartidaTrivia`, `FormularioTrivia`
- `docs/01-project-source/diagrama-de-clases.md` — `PartidaTrivia`, `EstadoPartida`, `Modalidad`, `CompetidorTrivia`
- `docs/02-project-context/business-rules.md` — BR-T04..T09, BR-T17, BR-T18, BR-G02, BR-G08..BR-G11
- `docs/02-project-context/design/domain-business-rules.md` — TRIVIA-FORM-001, TRIVIA-SCORE-001..004
- `docs/02-project-context/known-ambiguities-and-decisions.md` — team cardinality 1..5, scoring sin ponderación
- `docs/03-microservices/service-ownership.md`
- `docs/03-microservices/services/trivia-game-service.md`
- `services/trivia-game-service/service-context.md`

## Historia de usuario

Como **Operador**, quiero **crear una partida de Trivia asociada a un formulario existente y publicarla**, para **iniciar la dinámica de juego con los participantes**.

## Objetivo de negocio

Permitir al operador configurar y publicar una partida de Trivia basada en un formulario previamente creado y completo. La partida define nombre, modalidad (individual o equipo), límites de participación y tiempo de inicio. Una vez creada, el operador puede publicar el lobby para habilitar inscripciones. El inicio puede ser manual o automático al cumplirse el tiempo configurado, validando siempre los mínimos de participación.

## Alcance

### Incluido

1. **Crear partida de Trivia** asociada a un formulario válido y completo.
2. **Publicar lobby** para hacer visible la partida a los participantes.
3. **Iniciar partida manualmente** desde el panel del operador.
4. **Validar mínimos de participación** antes de iniciar.
5. **Cancelación automática** al cumplirse el tiempo de inicio si no se alcanzan los mínimos.
6. **Consulta de detalle de partida** por identificador.
7. **Autorización**: solo Operador puede crear, publicar, iniciar o cancelar partidas.
8. **Persistencia** en PostgreSQL vía Trivia Game Service.

### Fuera de alcance

| Elemento | Motivo |
| --- | --- |
| Inscripción de participantes o equipos | HU-18, HU-19 |
| Pantalla de espera / lobby de participantes | HU-21 |
| Panel de monitoreo de lobby para operador | HU-22, HU-23 |
| Ejecución de preguntas, respuestas, ranking | HUs de gameplay (HU-24..HU-30) |
| Frontend web (React) | Congelado para este sprint |
| Eventos RabbitMQ cross-service | No se requieren en esta HU |
| Notificaciones SignalR en tiempo real | Estado de lobby/pub en HUs posteriores |

## Precondiciones

1. El operador está autenticado vía Keycloak con rol **Operador**.
2. Existe al menos un formulario de Trivia con `isComplete = true`.
3. Trivia Game Service está disponible con PostgreSQL.

## Postcondiciones

### Creación exitosa

1. Partida persistida en estado **Lobby** con configuración completa.
2. Partida visible en listado de partidas Trivia publicadas.
3. Operador puede iniciar manualmente o esperar inicio automático.

### Inicio exitoso

1. Partida cambia a estado **Iniciada**.
2. Se activa la primera pregunta del formulario asociado.
3. Participantes/equipos inscritos pueden comenzar a responder.

### Cancelación por mínimos

1. Partida cambia a estado **Cancelada** (automática o manual).
2. Historial conserva evento de cancelación.

## Reglas de negocio aplicables

| ID | Regla |
| --- | --- |
| RB-T04 | Solo el operador puede crear partidas de Trivia. |
| RB-T05 | Toda partida debe estar asociada a un formulario válido y completo. |
| RB-T06 | El operador define nombre, modalidad, formulario, mínimos, máximos y tiempo de inicio. |
| RB-T07 | Modalidad individual → máximo = cantidad máxima de jugadores. |
| RB-T08 | Modalidad equipo → máximo = cantidad máxima de equipos. |
| RB-T09 | Modalidad equipo → operador define mínimo y máximo de jugadores por equipo. |
| RB-T10 | Al iniciar el lobby, la partida queda publicada para todos los jugadores. |
| RB-T17 | La Trivia inicia cuando se cumple el tiempo o el operador la inicia manualmente. |
| RB-T18 | Al iniciar, la partida cambia a estado iniciada. |
| RB-08 | Estado lobby permite inscripciones. |
| RB-09 | Estado iniciada permite acciones de juego. |
| RB-26 | Operador no puede iniciar manualmente si no se cumplen mínimos. |
| RB-27 | Si el inicio automático llega y no cumple mínimos, se cancela automáticamente. |

## Criterios de aceptación

### CA-01 — Crear partida válida

**Dado** un operador autenticado y un formulario de Trivia completo existente  
**Cuando** envía nombre, formularioId, modalidad, límites de participación y tiempo de inicio  
**Entonces** el sistema crea la partida en estado **Lobby**, responde 201 con el detalle.

### CA-02 — Rechazar formulario incompleto

**Dado** un operador autenticado  
**Cuando** intenta crear una partida con un formulario que tiene `isComplete = false`  
**Entonces** el sistema responde 400 indicando que el formulario debe estar completo.

### CA-03 — Rechazar formulario inexistente

**Dado** un operador autenticado  
**Cuando** intenta crear una partida con un formularioId que no existe  
**Entonces** el sistema responde 404.

### CA-04 — Iniciar partida manualmente cumpliendo mínimos

**Dado** un operador autenticado y una partida en estado Lobby con suficientes inscripciones  
**Cuando** solicita iniciar la partida manualmente  
**Entonces** el sistema cambia el estado a **Iniciada** y responde 200.

### CA-05 — Rechazar inicio manual sin mínimos

**Dado** un operador autenticado y una partida en Lobby sin suficientes inscripciones  
**Cuando** solicita iniciar manualmente  
**Entonces** el sistema responde 409 indicando que no se cumplen los mínimos.

### CA-06 — Acceso no autorizado

**Dado** un usuario autenticado sin rol Operador  
**Cuando** intenta crear, iniciar o consultar partidas  
**Entonces** el sistema responde 403.

### CA-07 — Partida no encontrada

**Dado** un operador autenticado  
**Cuando** consulta un id inexistente  
**Entonces** el sistema responde 404.

## Supuestos explícitos

1. Los límites mínimos/máximos se almacenan en la partida para validación futura.
2. La lógica de inscripción (HU-18/19) modificará el estado de participantes; HU-17 solo valida existencia de mínimos al iniciar.
3. El tiempo de inicio se almacena como `DateTimeOffset` en UTC.
4. El inicio automático se implementará como un background job o timer programado dentro del servicio.

## Preguntas abiertas

Ninguna para el alcance actual.
