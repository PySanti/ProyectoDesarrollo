# Communication Map

> **Regla de generación:** este contenido fue generado exclusivamente a partir de los archivos `diagrama de clases(2).md`, `enunciado-proyecto(1).md`, `historias de usuario(2).md`, `microservicios(2).md`, `modelo de dominio(2).md` y `srs(2).md`.
>
> **No se agregan microservicios, endpoints, colas, eventos, bases de datos, rutas HTTP ni contratos que no estén indicados explícitamente en esas fuentes.** Cuando una responsabilidad aparece en el SRS/modelo pero no está asignada a un microservicio en `microservicios(2).md`, queda marcada como **no asignada / pendiente de decisión**.


## Comunicación explícitamente exigida por el project-source

| Tipo de comunicación | Fuente funcional | Alcance definido |
|---|---|---|
| WebSockets / tiempo real | SRS y enunciado. | Actualización de publicación de partidas, lobby, estados, preguntas, ranking, etapas, temporizadores, pistas, geolocalización, resultados y sincronización. |
| RabbitMQ / mensajería asíncrona | SRS y enunciado. | Eventos relacionados con auditoría, historial, notificaciones internas, ranking, trazabilidad de puntajes y comunicación en tiempo real. |
| Keycloak / autenticación y autorización base | SRS. | Autenticación de administradores, operadores y participantes; roles base; UMBRAL almacena referencia local al identificador de Keycloak. |
| PostgreSQL / persistencia | SRS y microservicios. | Persistencia relacional mediante EF Core; cada microservicio descrito indica su propia base o tabla principal. |

## Comunicación síncrona entre microservicios

El contexto anexado **no especifica contratos HTTP concretos entre microservicios**.

Por lo tanto, este archivo no define:

- rutas HTTP;
- métodos;
- request bodies;
- response bodies;
- clientes internos entre servicios;
- reglas de timeout/retry;
- API Gateway.

### Dependencias conceptuales existentes

Las siguientes dependencias sí existen a nivel de dominio, pero su mecanismo técnico no está especificado:

| Dependencia conceptual | Motivo | Estado |
|---|---|---|
| Trivia Game Service necesita conocer si un participante puede inscribir un equipo. | HU-13/HU-19 exigen validar liderazgo para Trivia por equipo. | Mecanismo no especificado. |
| BDT Game Service necesita conocer si un participante puede inscribir un equipo. | HU-14/HU-40 exigen validar liderazgo para BDT por equipo. | Mecanismo no especificado. |
| Trivia/BDT necesitan usar equipos globales. | SRS indica que los equipos son comunes para ambos modos. | Mecanismo no especificado. |
| Todo servicio con autorización necesita identidad/rol del usuario. | SRS exige roles y Keycloak. | Mecanismo exacto de propagación no especificado. |

## Comunicación asíncrona

El SRS exige publicar eventos relevantes del dominio para:

- auditoría;
- historial;
- notificaciones internas;
- actualización de ranking;
- trazabilidad de puntajes;
- comunicación en tiempo real.

El project-source no define:

- exchange names;
- queue names;
- routing keys;
- payload exacto;
- versionado de eventos;
- política de idempotencia;
- mecanismo outbox.

Estos aspectos deben definirse por HU en SDD antes de implementar.

## Comunicación en tiempo real

El SRS exige canal de tiempo real para:

- publicación de partidas;
- cambios de lobby;
- estados de partida;
- preguntas de Trivia;
- ranking;
- etapas BDT;
- temporizadores;
- pistas;
- geolocalización;
- resultados;
- sincronización entre dispositivos autorizados de participantes de un mismo equipo.

El project-source no especifica si los hubs/canales viven en Trivia, BDT, frontend, gateway u otro componente. Por tanto, no se asigna ownership técnico del hub en esta carpeta.

## Regla para SDD

Cada `design.md` de HU que requiera comunicación debe especificar:

| Pregunta | Respuesta requerida |
|---|---|
| ¿La comunicación es visible para el usuario en tiempo real? | Si sí, usar WebSockets según SRS. |
| ¿La comunicación es efecto secundario no bloqueante? | Si sí, usar RabbitMQ según SRS. |
| ¿La comunicación requiere consultar datos de otro contexto? | Definir contrato explícito; el project-source actual no lo define. |
| ¿La comunicación modifica estado? | Debe ser comando/caso de uso del microservicio dueño. |
