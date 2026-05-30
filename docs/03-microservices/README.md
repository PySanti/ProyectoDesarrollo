# 03-microservices — UMBRAL

> **Regla de generación:** este contenido fue generado exclusivamente a partir de los archivos `diagrama de clases(2).md`, `enunciado-proyecto(1).md`, `historias de usuario(2).md`, `microservicios(2).md`, `modelo de dominio(2).md` y `srs(2).md`.
>
> **No se agregan microservicios, endpoints, colas, eventos, bases de datos, rutas HTTP ni contratos que no estén indicados explícitamente en esas fuentes.** Cuando una responsabilidad aparece en el SRS/modelo pero no está asignada a un microservicio en `microservicios(2).md`, queda marcada como **no asignada / pendiente de decisión**.


## Propósito

Esta carpeta define el contexto operativo de microservicios de UMBRAL para que OpenCode y el flujo SDD puedan identificar:

- qué microservicios están explícitamente definidos;
- qué responsabilidades tiene cada uno;
- qué historias de usuario cubre cada servicio según el documento de microservicios;
- qué conceptos del dominio pertenecen a cada contexto;
- qué comunicaciones están exigidas por el SRS;
- qué responsabilidades transversales aún no tienen ownership explícito en el project-source.

## Archivos

| Archivo | Propósito |
|---|---|
| `source-basis.md` | Resume las fuentes usadas y las reglas de no-asunción. |
| `microservices-map.md` | Mapa de microservicios explícitamente definidos. |
| `service-ownership.md` | Ownership de entidades, conceptos e historias. |
| `communication-map.md` | Comunicación síncrona/asíncrona/tiempo real sin inventar contratos concretos. |
| `api-contracts.md` | Guía para documentar contratos HTTP sin inventar endpoints. |
| `events-catalog.md` | Eventos y categorías de eventos presentes en el project-source. |
| `unresolved-decisions.md` | Inconsistencias o datos no especificados que no deben ser asumidos por OpenCode. |
| `services/identity-service.md` | Contexto operativo del Identity Service. |
| `services/team-service.md` | Contexto operativo del Team Service. |
| `services/trivia-game-service.md` | Contexto operativo del Trivia Game Service. |
| `services/bdt-game-service.md` | Contexto operativo del BDT Game Service. |

## Regla central

El archivo `microservicios(2).md` titula la sección como **"Los 5 Microservicios de Negocio"**, pero solo detalla cuatro microservicios:

1. Identity Service.
2. Team Service.
3. Trivia Game Service.
4. BDT Game Service.

Por lo tanto, esta carpeta **solo crea contexto operativo para esos cuatro microservicios explícitamente descritos**.

No se crea `Scoring Service`, `Audit Service`, `Notification Service`, `Gateway` ni otro servicio adicional porque en el contexto anexado actual no aparecen definidos como microservicios con responsabilidad, historias y base de datos propias.

## Cómo usar esta carpeta con SDD

Antes de crear o implementar una historia de usuario:

1. Leer `microservices-map.md`.
2. Leer `service-ownership.md`.
3. Leer el archivo correspondiente en `services/`.
4. Si la HU requiere eventos, leer `events-catalog.md`.
5. Si la HU requiere API, completar contratos en `contracts/http/` solo después de que el `spec.md` y `design.md` de la HU lo justifiquen.
6. Si la HU toca una responsabilidad marcada como no asignada, resolver primero `unresolved-decisions.md`.
