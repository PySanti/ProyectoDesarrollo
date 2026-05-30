# Guía de uso de OpenCode para crear una historia de usuario en UMBRAL

## Objetivo

Esta guía explica cómo usar OpenCode para crear, planificar, implementar y revisar una historia de usuario dentro del proyecto UMBRAL siguiendo el flujo SDD.

El objetivo no es pedirle a OpenCode que “implemente una historia” directamente, sino obligarlo a trabajar por etapas:

```txt
1. Crear/refinar SDD
2. Revisar SDD
3. Planificar implementación
4. Implementar una tarea a la vez
5. Probar
6. Revisar
7. Actualizar trazabilidad
```

---

# 1. Regla base

Nunca empieces con un prompt como:

```txt
Implementa HU-03
```

Primero debes crear el SDD de la historia.

El flujo correcto es:

```txt
/create-feature-sdd HU-03 Crear equipo
```

Luego:

```txt
/plan-feature HU-03
```

Y solo después:

```txt
/implement-task HU-03 task 1
```

---

# 2. Antes de empezar

Verifica que la HU esté en:

```txt
docs/04-sdd/SPECS-LIST.md
```

Si la historia no está en esa lista, OpenCode debe detenerse y avisar que está fuera del alcance activo.

También verifica que la HU tenga un único servicio dueño:

```txt
Identity Service
Team Service
Trivia Game Service
BDT Game Service
```

No son servicios válidos:

```txt
Audit Service
Scoring Service
Trivia Service
Treasure Hunt Service
Notification Service
```

---

# 3. Flujo completo recomendado

## Paso 1 — Crear el SDD de la HU

Usa:

```txt
/create-feature-sdd HU-XX Nombre de historia
```

Ejemplo:

```txt
/create-feature-sdd HU-03 Crear equipo
```

OpenCode debe crear:

```txt
docs/04-sdd/specs/HU-03-crear-equipo/
  spec.md
  design.md
  tasks.md
  acceptance.md
```

## Qué debe contener `spec.md`

Debe definir:

```txt
HU ID
Nombre de historia
Actor principal
Objetivo del usuario
Alcance
Fuera de alcance
Precondiciones
Postcondiciones
Reglas de negocio
RF/RB/RNF relacionados
Criterios de aceptación
Preguntas abiertas, solo si son inevitables
```

## Qué debe contener `design.md`

Debe definir:

```txt
Servicio dueño
Servicios de apoyo
Entidades involucradas
Value objects
Comandos
Queries
Eventos
Endpoints HTTP
Actualizaciones en tiempo real, si aplican
Patrones de diseño usados
Pruebas necesarias
```

## Qué debe contener `tasks.md`

Debe dividir el trabajo por capas:

```txt
Domain
Application
Infrastructure
API
Contracts
Tests
Frontend, si aplica
Acceptance / Traceability
```

Las tareas deben ser pequeñas y ejecutables una por una.

## Qué debe contener `acceptance.md`

Debe tener:

```txt
Checklist de aceptación
Pasos de verificación manual
Evidencia esperada de pruebas
Estado de trazabilidad
```

---

# 4. Prompt recomendado para crear el SDD

Usa este formato:

```txt
/create-feature-sdd HU-03 Crear equipo

Crea el SDD completo para esta historia siguiendo el setup del proyecto UMBRAL.

Reglas:
- Valida que HU-03 aparece en docs/04-sdd/SPECS-LIST.md.
- Identifica el servicio dueño.
- Usa docs/01-project-source, docs/02-project-context, docs/03-microservices y docs/05-decisions.
- Aplica las decisiones resueltas:
  - equipos de 1 a 5 integrantes;
  - puntaje de Trivia sin tiempo, si aplica.
- No implementes código.
- No inventes requisitos.
- Deja spec.md, design.md, tasks.md y acceptance.md listos para revisión.
```

---

# 5. Revisar el SDD antes de implementar

Después de crear el SDD, usa:

```txt
/review-feature HU-03
```

O:

```txt
/review-feature docs/04-sdd/specs/HU-03-crear-equipo
```

OpenCode debe revisar:

```txt
SDD completo
Servicio dueño correcto
Reglas de negocio correctas
Contratos necesarios
Separación Clean/Hexagonal
CQRS/MediatR
Pruebas necesarias
Criterios de aceptación
Trazabilidad
```

Si detecta problemas, corrige el SDD antes de avanzar.

---

# 6. Planificar la implementación

Cuando el SDD esté limpio:

```txt
/plan-feature HU-03
```

OpenCode debe devolver:

```txt
Feature / HU
Carpeta SDD
Servicio dueño
Servicios de apoyo
Archivos a modificar
Contratos a modificar
Pruebas a escribir
Primera tarea a implementar
Riesgos o blockers
Si se puede implementar ahora
```

Todavía no debe escribir código.

---

# 7. Implementar una tarea a la vez

Cuando el plan esté aprobado, implementa solo una tarea:

```txt
/implement-task HU-03 task 1
```

O:

```txt
/implement-task docs/04-sdd/specs/HU-03-crear-equipo task 1
```

Reglas:

```txt
No implementar varias tareas a la vez.
No modificar microservicios no relacionados.
No inventar endpoints.
No inventar eventos.
No poner reglas de negocio en controllers, hubs o EF configs.
No acceder a la base de datos de otro servicio.
```

---

# 8. Orden ideal de implementación por tarea

Para una HU backend, el orden recomendado es:

```txt
1. Domain
2. Application
3. Contracts
4. Infrastructure
5. API
6. Tests
7. Acceptance
8. Traceability
```

Para una HU frontend:

```txt
1. Revisar contrato HTTP/eventos
2. Crear API client
3. Crear hook/caso de uso frontend
4. Crear componentes
5. Crear página/route
6. Agregar validaciones UI
7. Agregar tests
8. Actualizar acceptance.md
```

---

# 9. Ejemplo completo: HU-03 Crear equipo

## 9.1 Crear SDD

```txt
/create-feature-sdd HU-03 Crear equipo
```

Debe identificar:

```txt
Owning service: Team Service
Supporting services: Identity Service / Keycloak, si aplica
```

Debe aplicar:

```txt
Equipo válido con 1 a 5 integrantes.
El creador es el primer integrante.
El creador queda como líder.
Se genera código único.
Un participante solo puede pertenecer a un equipo activo.
```

## 9.2 Revisar SDD

```txt
/review-feature HU-03
```

## 9.3 Planificar

```txt
/plan-feature HU-03
```

## 9.4 Implementar primera tarea

Ejemplo:

```txt
/implement-task HU-03 task 1
```

La tarea 1 podría ser:

```txt
Crear aggregate Equipo con invariantes de creación.
```

## 9.5 Implementar siguiente tarea

```txt
/implement-task HU-03 task 2
```

Y así sucesivamente.

---

# 10. Cuándo usar cada comando

## `/create-feature-sdd`

Úsalo cuando:

```txt
La HU todavía no tiene carpeta SDD.
La HU tiene SDD incompleto.
Quieres regenerar la especificación antes de implementar.
```

## `/plan-feature`

Úsalo cuando:

```txt
El SDD ya existe.
Quieres saber qué archivos tocar.
Quieres validar si la implementación puede iniciar.
```

## `/implement-task`

Úsalo cuando:

```txt
El SDD está completo.
La tarea está clara en tasks.md.
Quieres implementar una sola tarea.
```

## `/review-boundaries`

Úsalo cuando:

```txt
La HU toca más de un microservicio.
No está claro si se debe usar HTTP, RabbitMQ o SignalR.
Hay riesgo de mezclar reglas de Team Service con Trivia/BDT.
```

Ejemplo:

```txt
/review-boundaries HU-19 Unir equipo a Trivia por equipos
```

## `/review-feature`

Úsalo cuando:

```txt
Quieres auditar una HU antes o después de implementarla.
Quieres revisar arquitectura, tests, contracts y SDD.
```

## `/update-traceability`

Úsalo cuando:

```txt
Una tarea cambió de estado.
Una HU quedó parcialmente implementada.
Una HU quedó completada.
Se agregaron endpoints, eventos o tests.
```

---

# 11. Uso de agentes

## Architect Agent

Úsalo para:

```txt
Validar boundaries.
Validar microservicios.
Validar ADRs.
Validar Clean/Hexagonal.
Validar que no se cree Scoring/Audit como servicio.
```

Prompt útil:

```txt
@architect Revisa el SDD de HU-03 y dime si respeta boundaries, microservicios, Clean Architecture y decisiones del proyecto.
```

## Backend Agent

Úsalo para:

```txt
Implementar tareas backend.
Crear dominio.
Crear handlers.
Crear endpoints.
Crear repositorios.
Crear tests backend.
```

Prompt útil:

```txt
@backend Implementa únicamente la task 1 de HU-03. No modifiques otras tareas.
```

## Frontend Agent

Úsalo para:

```txt
Crear pantallas.
Crear componentes.
Crear API clients.
Crear hooks.
Integrar SignalR si el SDD lo pide.
```

Prompt útil:

```txt
@frontend Planifica la UI necesaria para HU-09 usando el contrato de Trivia Game Service. No implementes todavía.
```

## DevOps Agent

Úsalo para:

```txt
Docker Compose.
Variables de entorno.
PostgreSQL.
RabbitMQ.
Keycloak.
CI.
```

Prompt útil:

```txt
@devops Revisa si el nuevo microservicio Team Service queda correctamente configurado en docker-compose.
```

## QA Agent

Úsalo para:

```txt
Revisar SDD.
Revisar acceptance.
Revisar tests.
Revisar trazabilidad.
```

Prompt útil:

```txt
@qa Revisa HU-03 y dime si está lista para marcarse como Done.
```

---

# 12. Uso de skills

## `umbral-context`

Siempre debe usarse al inicio.

Carga:

```txt
producto
SRS
modelo de dominio
microservicios
scope
contracts
decisiones
```

## `sdd-workflow`

Debe usarse antes de toda HU.

Evita implementar sin:

```txt
spec.md
design.md
tasks.md
acceptance.md
```

## `ddd-modeling`

Úsala para:

```txt
agregados
entidades
value objects
invariantes
servicios de dominio
eventos de dominio
```

## `cqrs-mediatr`

Úsala para:

```txt
commands
queries
handlers
validators
DTOs
MediatR
```

## `efcore-postgres`

Úsala para:

```txt
DbContext
migrations
EF configurations
repositories
PostgreSQL
```

## `contract-design`

Úsala para:

```txt
HTTP endpoints
eventos RabbitMQ
DTOs
errores
versionado
publisher/consumer
```

## `rabbitmq-events`

Úsala cuando:

```txt
La HU emite o consume eventos asíncronos.
```

## `websocket-signalr`

Úsala cuando:

```txt
La HU necesita actualizaciones visibles en tiempo real.
```

## `testing`

Úsala siempre que implementes reglas de negocio.

---

# 13. Checklist antes de implementar una HU

Antes de `/implement-task`, verifica:

```txt
[ ] La HU aparece en SPECS-LIST.md.
[ ] La carpeta SDD existe.
[ ] No está en _deprecated.
[ ] spec.md no tiene TODO.
[ ] design.md no tiene TODO.
[ ] tasks.md no tiene TODO.
[ ] acceptance.md no tiene TODO.
[ ] El owning service es correcto.
[ ] Los contracts requeridos existen.
[ ] Las reglas de negocio están claras.
[ ] Las pruebas están definidas.
```

---

# 14. Checklist después de implementar una tarea

Después de `/implement-task`, verifica:

```txt
[ ] Solo se implementó una tarea.
[ ] No se modificaron servicios no relacionados.
[ ] Las reglas están en Domain/Application, no en controllers.
[ ] Se agregaron o actualizaron tests.
[ ] acceptance.md fue actualizado.
[ ] traceability-matrix.md fue actualizado si corresponde.
[ ] No se inventaron endpoints/eventos.
[ ] No se violaron boundaries.
```

---

# 15. Validaciones rápidas

## Buscar servicios prohibidos

```bash
grep -RniE "Audit Service|Scoring Service|Trivia Service|Treasure Hunt Service|Notification Service|audit-service|scoring-service|trivia-service|treasure-hunt-service" .
```

Solo deben aparecer como servicios prohibidos, deprecated o non-services.

## Buscar referencias viejas de professor-source

```bash
grep -RniE "docs/00-professor-source|professor-source" .
```

Debe devolver cero resultados.

## Buscar conflicto de equipos

```bash
grep -RniE "≥2|2 a 5|2 y 5|minimo 2|mínimo 2|minimum 2|at least 2" docs .opencode contracts services
```

No debe aparecer como regla activa.

## Buscar conflicto de puntaje con tiempo

```bash
grep -RniE "tiempo_restante|tiempo_total|remainingTime / totalTime|timeMultiplier|ponderad|tiempo.*puntaje|time.*score" docs .opencode contracts services
```

No debe aparecer como regla activa.

---

# 16. Flujo resumido para copiar y usar

```txt
/create-feature-sdd HU-03 Crear equipo
/review-feature HU-03
/plan-feature HU-03
/implement-task HU-03 task 1
/review-feature HU-03
/update-traceability HU-03
```

Para cada tarea siguiente:

```txt
/implement-task HU-03 task 2
/review-feature HU-03
/update-traceability HU-03
```

---

# 17. Regla final

OpenCode debe trabajar como un desarrollador disciplinado, no como un generador libre de código.

La secuencia correcta siempre es:

```txt
Contexto → SDD → Revisión → Plan → Una tarea → Tests → Acceptance → Traceability
```

Nunca:

```txt
Prompt vago → Código directo
```
