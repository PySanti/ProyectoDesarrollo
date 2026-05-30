# Design Patterns Catalog — UMBRAL

Este catálogo define cómo evidenciar patrones de diseño sin sobreingeniería.

## Regla general

No usar patrones por decoración. Un patrón debe resolver un problema real de diseño, reducir acoplamiento, encapsular variación o evidenciar una decisión académica defendible.

## Patrones arquitectónicos obligatorios

| Patrón | Uso en UMBRAL | Evidencia |
|---|---|---|
| CQRS | Separar escritura y lectura. | Commands, Queries, Handlers. |
| Mediator | Orquestar casos de uso. | MediatR handlers invocados por controllers. |
| Repository | Abstraer persistencia. | Interfaces en Application/Domain, implementaciones EF Core en Infrastructure. |
| Adapter / Ports | Aislar infraestructura. | Adaptadores PostgreSQL, RabbitMQ, SignalR, Keycloak, QR decoder. |
| Dependency Injection | Invertir dependencias. | Registro de servicios e interfaces por capa. |

## Patrones tácticos recomendados

| Patrón | Cuándo usarlo | Ejemplo |
|---|---|---|
| Factory Method | Creación de agregados con invariantes. | `Equipo.Crear(...)`, `PartidaTrivia.Crear(...)`. |
| Strategy | Algoritmos intercambiables. | Política de puntaje directo vs ponderado por tiempo. |
| State | Transiciones complejas de partida/etapa. | Estados de Partida o Etapa si la lógica crece. |
| Specification | Validaciones combinables. | Validar inscripción: estado lobby + cupo + liderazgo + equipo activo. |
| Domain Event | Hechos del dominio. | `RespuestaTriviaValidada`, `HitoBDTEncontrado`. |
| Observer / PubSub | Actualizaciones en tiempo real. | SignalR hubs para ranking/lobby/etapas. |
| Outbox | Publicación confiable de eventos. | Guardar evento + publicar RabbitMQ tras commit. |
| Unit of Work | Persistir cambios de agregado y eventos. | EF Core DbContext. |
| Result Pattern | Respuestas de dominio sin excepciones para flujo esperado. | QR inválido, respuesta tardía, cupo lleno. |

## Patrones por tipo de HU

### Equipos

| HU | Patrones sugeridos |
|---|---|
| Crear equipo | Factory Method, Repository, CQRS/Mediator. |
| Unirse a equipo | Specification o validaciones en agregado, Repository, CQRS/Mediator. |
| Salir/transferir liderazgo | State simple o métodos de agregado, Domain Event si notifica. |
| Eliminar equipo | Domain Event para notificación, Repository. |

### Trivia

| HU | Patrones sugeridos |
|---|---|
| Crear formulario | Composite simple Formulario-Pregunta-Opción, Factory Method. |
| Crear partida | Factory Method, Specification para formulario válido. |
| Responder pregunta | Command Handler, Domain Event, Strategy para puntaje si hay variantes. |
| Ranking | Domain Service, Strategy si el criterio cambia. |
| Tiempo real | Observer/PubSub vía SignalR. |

### BDT

| HU | Patrones sugeridos |
|---|---|
| Crear BDT | Factory Method, Composite Partida-Etapa. |
| Validar QR | Adapter para decodificador QR, Strategy si hay varios métodos. |
| Cerrar etapa | State si la lógica crece; Domain Event. |
| Enviar pista | Command Handler, PubSub/SignalR, Event para auditoría. |

## Sección obligatoria en cada `design.md`

Cada feature SDD debe incluir:

```md
## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
```

Si no se introduce patrón táctico adicional:

```md
No additional tactical pattern is introduced. The feature uses the mandatory architectural patterns: CQRS, Mediator, Repository, Adapter and Dependency Injection.
```

## Antipatrones a evitar

| Antipatrón | Evitar porque |
|---|---|
| God Service | Mezcla lógica de varios contextos. |
| Anemic Domain sin reglas | Lleva reglas a handlers/controllers y debilita DDD. |
| Controller con lógica de negocio | Rompe Clean Architecture. |
| Shared database entre servicios | Rompe límites de microservicios. |
| Patrón innecesario | Aumenta complejidad sin beneficio. |
| DTO como entidad de dominio | Mezcla API con dominio. |
