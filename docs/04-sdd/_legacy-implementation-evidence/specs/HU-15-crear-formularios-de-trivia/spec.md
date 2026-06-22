# HU-15 — Crear formularios de Trivia

## Identificación

| Campo | Valor |
| --- | --- |
| HU ID | HU-15 |
| Nombre | Crear formularios de Trivia |
| Actor | Operador |
| Cliente objetivo | React web |
| Servicio dueño | Trivia Game Service |
| Estado SDD | Listo para revisión |

## Referencias fuente

- `docs/01-project-source/srs.md` — HU-15, RF-15, RF-16, RF-35, RB-T01, RB-T02, RB-T03, RB-T15, RB-T16, RB-T17, RB-T20
- `docs/01-project-source/historias-de-usuario.md` — HU-15
- `docs/01-project-source/modelo-de-dominio.md` — agregado `FormularioTrivia`
- `docs/01-project-source/diagrama-de-clases.md` — agregado `FormularioTrivia`
- `docs/02-project-context/business-rules.md` — BR-T01, BR-T02, BR-T03, BR-T14, BR-T15, BR-T16, BR-T17
- `docs/02-project-context/design/domain-business-rules.md` — TRIVIA-FORM-001, TRIVIA-SCORE-003, TRIVIA-SCORE-004
- `docs/02-project-context/known-ambiguities-and-decisions.md` — decisión de puntaje Trivia sin ponderación por tiempo
- `docs/03-microservices/service-ownership.md`
- `docs/03-microservices/services/trivia-game-service.md`
- `services/trivia-game-service/service-context.md`

## Historia de usuario

Como **Operador**, quiero **crear y configurar formularios de Trivia**, para **preparar el contenido que luego será usado en partidas de Trivia**.

## Objetivo de negocio

Permitir al operador diseñar plantillas reutilizables de Trivia compuestas por un título y una cantidad libre de preguntas. Cada pregunta define su temporizador, cuatro opciones de respuesta, cuál es la correcta y el puntaje fijo otorgado al acertar. El formulario debe quedar persistido y consultable para su uso posterior en la creación de partidas (HU-17).

## Alcance

### Incluido

1. **Crear formulario** con título y al menos una pregunta válida.
2. **Editar formulario** existente (título, preguntas, opciones, puntaje y temporizador).
3. **Consultar formulario por identificador** con detalle completo de preguntas y opciones.
4. **Validar completitud del formulario** en dominio y exponer el resultado (`isComplete`) para consumo del operador y de HU-17.
5. **Autorización**: solo usuarios con rol Operador pueden crear, editar o consultar formularios.
6. **Frontend React web**: pantalla(s) de creación/edición y consulta de detalle del formulario.
7. **Persistencia** en PostgreSQL vía Trivia Game Service.

### Estructura funcional del formulario

| Elemento | Regla |
| --- | --- |
| Formulario | Debe tener un **título** no vacío y **una o más preguntas**. |
| Pregunta | Debe tener **texto**, **temporizador** (`timeLimitSeconds`), **puntaje asignado** (`assignedScore`) y **exactamente 4 opciones**. |
| Opciones | **Exactamente 1** marcada como correcta; las **3 restantes** son incorrectas. |
| Puntaje correcto | La opción correcta otorga el `assignedScore` de la pregunta cuando se responde acertadamente en partida. |
| Puntaje incorrecto | Las opciones incorrectas otorgan **0 puntos** (no se configura puntaje por opción). |
| Temporizador | Limita el tiempo de respuesta en partida; **no modifica el puntaje** en la creación del formulario ni en el cálculo de puntaje (decisión resuelta del proyecto). |

### Fuera de alcance

| Elemento | Motivo |
| --- | --- |
| Listado o navegación de formularios creados | Corresponde a HU-16 (no está en el sprint activo). |
| Eliminar formularios | No está definido en RF-15 ni en el sprint activo. |
| Crear, publicar o ejecutar partidas de Trivia | HU-17 y HUs de ejecución. |
| Consumo del formulario durante partida en vivo | HUs de gameplay (HU-24, HU-26, etc.). |
| Notificaciones push o actualizaciones SignalR | No aplican a la gestión estática de plantillas. |
| Eventos RabbitMQ cross-service | El formulario es dato maestro interno de Trivia Game Service; no requiere integración asíncrona externa en esta HU. |
| Cliente móvil React Native | Actor Operador → React web exclusivamente. |

## Precondiciones

1. El operador está autenticado vía Keycloak con rol **Operador**.
2. Trivia Game Service está disponible y conectado a su base de datos PostgreSQL.
3. El operador accede al panel web de operador.

## Postcondiciones

### Creación exitosa

1. Existe un formulario persistido con identificador único.
2. El formulario contiene el título y las preguntas enviadas que cumplen las invariantes.
3. La respuesta HTTP incluye el identificador del formulario y el estado de completitud.
4. El operador puede consultar el formulario creado por su identificador.

### Edición exitosa

1. El formulario existente refleja los cambios persistidos.
2. Se recalcula y expone `isComplete` según las reglas de completitud.

### Consulta exitosa

1. El operador obtiene título, preguntas, opciones, puntaje, temporizador y `isComplete` sin modificar estado.

## Reglas de negocio aplicables

| ID | Regla |
| --- | --- |
| BR-T01 / RB-T01 | Solo el operador puede crear formularios de Trivia. |
| BR-T02 / RB-T02 | Un formulario debe contener preguntas, opciones, respuesta correcta, puntaje y tiempo por pregunta. |
| BR-T03 / RB-T03 | Un formulario incompleto no puede usarse para crear partida (validación expuesta; enforcement completo en HU-17). |
| BR-T14 / RB-T15 | El temporizador controla disponibilidad y cierre; no modifica puntaje. |
| BR-T15 | El temporizador no modifica puntaje. |
| BR-T16 / RB-T16 | Una respuesta correcta suma directamente el puntaje asignado de la pregunta. |
| BR-T17 / RB-T17 | Una respuesta incorrecta suma 0 puntos. |
| BR-T20 / RB-T20 | Cada pregunta tiene tiempo límite propio definido en el formulario. |
| TRIVIA-FORM-001 | Formulario completo: ≥1 pregunta; cada pregunta con opciones, una correcta, puntaje y tiempo. |
| TRIVIA-SCORE-003 | El tiempo no entra en la fórmula de puntaje. |
| HU-15-FORM-001 | Cada pregunta debe tener **exactamente 4 opciones**. |
| HU-15-FORM-002 | Cada pregunta debe tener **exactamente 1 opción correcta**. |
| HU-15-FORM-003 | El puntaje se define **a nivel de pregunta** (`assignedScore`); las opciones incorrectas implican 0 puntos. |
| HU-15-FORM-004 | El temporizador se configura solo como límite de respuesta; **no existe ponderación de puntaje por tiempo** al crear el formulario. |
| HU-15-FORM-005 | Un formulario debe tener **al menos una pregunta** para considerarse completo. |
| HU-15-FORM-006 | El orden de las preguntas se conserva mediante `displayOrder` (relevante para sincronización futura en partida). |

## Requisitos relacionados

| ID | Descripción | Cobertura en HU-15 |
| --- | --- | --- |
| RF-15 | Crear, editar y consultar formularios de Trivia | Completa |
| RF-16 | Validar completitud antes de usar en partida | Validación de dominio + flag `isComplete`; uso en partida en HU-17 |
| RF-35 | Consultar formularios sin modificar estado | GET por id |
| RB-T01 | Solo operador crea formularios | Autorización |
| RB-T02 | Estructura mínima del formulario | Invariantes de dominio |
| RB-T03 | Formulario incompleto no usable en partida | `isComplete = false` |
| RNF (transversal) | Persistencia PostgreSQL + EF Core | Infraestructura |
| RNF (transversal) | Clean Architecture + CQRS + MediatR | Diseño técnico |

## Criterios de aceptación

### CA-01 — Crear formulario válido

**Dado** un operador autenticado
**Cuando** envía un formulario con título, al menos una pregunta, cada pregunta con texto, temporizador > 0, puntaje > 0 y exactamente 4 opciones (1 correcta)
**Entonces** el sistema persiste el formulario, responde 201 con el identificador y `isComplete = true`.

### CA-02 — Rechazar formulario sin preguntas

**Dado** un operador autenticado
**Cuando** intenta guardar un formulario sin preguntas
**Entonces** el sistema responde 400 indicando que se requiere al menos una pregunta.

### CA-03 — Rechazar pregunta con cantidad incorrecta de opciones

**Dado** un operador autenticado
**Cuando** envía una pregunta con un número de opciones distinto de 4
**Entonces** el sistema responde 400 indicando que cada pregunta debe tener exactamente 4 opciones.

### CA-04 — Rechazar pregunta sin exactamente una opción correcta

**Dado** un operador autenticado
**Cuando** envía una pregunta con 0 o más de 1 opción marcada como correcta
**Entonces** el sistema responde 400 indicando que debe existir exactamente una respuesta correcta.

### CA-05 — Rechazar puntaje o temporizador inválido

**Dado** un operador autenticado
**Cuando** envía una pregunta con `assignedScore` ≤ 0 o `timeLimitSeconds` ≤ 0
**Entonces** el sistema responde 400 con el detalle de validación correspondiente.

### CA-06 — Editar formulario existente

**Dado** un operador autenticado y un formulario existente
**Cuando** envía una actualización válida del formulario
**Entonces** el sistema persiste los cambios y responde 200 con el detalle actualizado y `isComplete` recalculado.

### CA-07 — Consultar formulario por id

**Dado** un operador autenticado y un formulario existente
**Cuando** solicita el detalle por identificador
**Entonces** el sistema responde 200 con título, preguntas ordenadas, opciones, puntajes, temporizadores e `isComplete` sin modificar datos.

### CA-08 — Formulario no encontrado

**Dado** un operador autenticado
**Cuando** consulta o edita un identificador inexistente
**Entonces** el sistema responde 404.

### CA-09 — Acceso no autorizado

**Dado** un usuario autenticado sin rol Operador
**Cuando** intenta crear, editar o consultar formularios
**Entonces** el sistema responde 403.

### CA-10 — Puntaje fijo sin ponderación por tiempo

**Dado** un formulario válido con pregunta de `assignedScore = 10` y `timeLimitSeconds = 30`
**Cuando** el operador consulta el formulario
**Entonces** el puntaje mostrado es 10 fijo por pregunta y no existe campo ni cálculo de ponderación temporal en el modelo del formulario.

### CA-11 — UI web de operador

**Dado** un operador en el panel web
**Cuando** crea o edita un formulario desde la interfaz
**Entonces** puede definir título, agregar múltiples preguntas, configurar 4 opciones por pregunta, marcar una como correcta, asignar puntaje y temporizador, y guardar con retroalimentación de errores de validación.

## Supuestos explícitos

1. **Identificador del operador**: se obtiene del token Keycloak (`sub` o claim acordado con Identity Service) y se almacena como referencia local `CreatedByOperatorId` en el formulario.
2. **Edición con partidas asociadas**: HU-15 permite editar cualquier formulario existente. Las restricciones por partidas ya publicadas o en curso, si fueran necesarias, se definirán en HU-17.
3. **Límites de longitud**: título máximo 200 caracteres; texto de pregunta máximo 1000 caracteres; texto de opción máximo 500 caracteres (definidos en diseño).
4. **Rango de temporizador**: entre 5 y 300 segundos inclusive (definidos en diseño).
5. **Rango de puntaje**: entero positivo entre 1 y 1000 inclusive (definidos en diseño).

## Preguntas abiertas

Ninguna. Las reglas de estructura (4 opciones, 1 correcta, puntaje fijo) están definidas por el SRS, el dominio del proyecto y las instrucciones de alcance de esta HU.
