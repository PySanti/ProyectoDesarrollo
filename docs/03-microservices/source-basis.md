# Source Basis

> **Regla de generación:** este contenido fue generado exclusivamente a partir de los archivos `diagrama de clases(2).md`, `enunciado-proyecto(1).md`, `historias de usuario(2).md`, `microservicios(2).md`, `modelo de dominio(2).md` y `srs(2).md`.
>
> **No se agregan microservicios, endpoints, colas, eventos, bases de datos, rutas HTTP ni contratos que no estén indicados explícitamente en esas fuentes.** Cuando una responsabilidad aparece en el SRS/modelo pero no está asignada a un microservicio en `microservicios(2).md`, queda marcada como **no asignada / pendiente de decisión**.


## Fuentes consideradas

| Fuente | Uso en esta carpeta |
|---|---|
| `microservicios(2).md` | Fuente principal para definir microservicios, responsabilidades, contexto DDD, historias cubiertas y persistencia. |
| `srs(2).md` | Fuente principal para requerimientos funcionales, no funcionales, reglas de negocio, tiempo real, RabbitMQ, Keycloak y alcance. |
| `modelo de dominio(2).md` | Fuente para contextos acotados, subdominios, conceptos, comandos, eventos y servicios de dominio/aplicación nombrados. |
| `diagrama de clases(2).md` | Fuente para agregados, entidades, value objects, relaciones, clases transversales y contexto de auditoría. |
| `historias de usuario(2).md` | Fuente para alcance de primera entrega y asignación de historias por responsable. |
| `enunciado-proyecto(1).md` | Fuente para lineamientos técnicos generales: React, WebSockets, .NET Core, EF Core, MediatR, CQRS, PostgreSQL y RabbitMQ. |

## Reglas de no-asunción aplicadas

- No se agregan microservicios no detallados en `microservicios(2).md`.
- No se inventan endpoints HTTP.
- No se inventan nombres de colas, exchanges, topics o routing keys.
- No se asigna ownership a responsabilidades transversales cuando el project-source no lo especifica.
- No se decide si `RegistroAuditoria`, `Ranking Final`, `InscripcionPartida` o `Convocatoria` pertenecen a un servicio concreto si las fuentes no lo asignan de forma explícita.
- No se define gateway ni API Gateway porque el contexto anexado no lo especifica como microservicio.
- No se define una política de base de datos por servicio más allá de la persistencia indicada por `microservicios(2).md`.
- No se resuelven inconsistencias internas del project-source; se documentan en `unresolved-decisions.md`.

## Jerarquía usada para esta carpeta

1. Para nombres y cantidad de microservicios: `microservicios(2).md`.
2. Para reglas funcionales y restricciones técnicas: `srs(2).md`.
3. Para conceptos, agregados y eventos de dominio: `modelo de dominio(2).md` y `diagrama de clases(2).md`.
4. Para alcance de primera entrega: `historias de usuario(2).md`.

## Criterio ante contradicciones

Si una fuente menciona una responsabilidad del dominio pero `microservicios(2).md` no la asigna a un microservicio, se marca como:

```txt
No asignado en microservicios(2).md / pendiente de decisión
```
