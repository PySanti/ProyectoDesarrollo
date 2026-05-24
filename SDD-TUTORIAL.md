# SDD + OpenCode Tutorial — Nueva Feature

Guía rápida para agregar una nueva feature en UMBRAL usando **SDD + OpenCode**.

---

## 1. Elegir la historia o requerimiento

Identifica la feature a trabajar:

```txt
HU-02-team-management
HU-13-treasure-missions
HU-21-trivia-questions
```

Ubicación:

```txt
docs/04-sdd/specs/<feature>/
```

Cada feature debe tener:

```txt
spec.md
design.md
tasks.md
acceptance.md
```

---

## 2. Generar o completar el `spec.md`

Primero se define **qué se va a construir**, no se programa todavía.

Prompt recomendado:

```txt
@backend Usa las skills sdd-workflow y umbral-context.

Trabaja únicamente sobre <FEATURE>.

Completa solo:
docs/04-sdd/specs/<FEATURE>/spec.md

Usa como fuente:
- docs/01-project-source/
- docs/02-project-context/
- docs/03-microservices/
- docs/04-sdd/
- services/<service>/service-context.md

No escribas código.
No modifiques otros microservicios.
```

El `spec.md` debe dejar claro:

```txt
- Historia de usuario
- Requerimiento relacionado
- Microservicio dueño
- Servicios de apoyo
- Alcance incluido
- Alcance excluido
- Reglas de negocio
- Criterios de aceptación
```

---

## 3. Revisar el spec

Antes de diseñar o programar, revisa el spec.

Prompt recomendado:

```txt
@architect Revisa el spec.md de <FEATURE>.
Verifica microservicios, ownership, alcance, reglas de negocio y SDD.
No modifiques archivos.
```

Si está bien, continúa.

---

## 4. Generar `design.md` y `tasks.md`

Ahora se define **cómo se va a construir**.

Prompt recomendado:

```txt
@backend Usa ddd-modeling, cqrs-mediatr, efcore-postgres, rabbitmq-events y websocket-signalr según aplique.

Con base en el spec aprobado de <FEATURE>, completa:
- design.md
- tasks.md

No escribas código.
```

El `design.md` debe incluir:

```txt
- Entidades / agregados
- Value objects
- Commands
- Queries
- Handlers
- Endpoints
- Eventos
- Persistencia
- Pruebas necesarias
```

El `tasks.md` debe dividir el trabajo en tareas pequeñas.

---

## 5. Revisar Definition of Ready

Antes de implementar, la feature debe cumplir:

```txt
- spec.md sin TODO
- design.md sin TODO
- tasks.md con tareas concretas
- acceptance.md inicializado
- contratos HTTP/eventos definidos si aplican
- microservicio dueño claro
- pruebas planificadas
```

Prompt recomendado:

```txt
@qa Revisa <FEATURE> contra Definition of Ready.
No escribas código.
```

---

## 6. Implementar una tarea a la vez

Nunca pidas “implementa toda la feature”.

Prompt recomendado:

```txt
@backend Implementa únicamente la primera tarea pendiente de:
docs/04-sdd/specs/<FEATURE>/tasks.md

Respeta:
- spec.md
- design.md
- microservicio dueño
- contratos
- reglas de negocio

Actualiza tests, acceptance.md y traceability-matrix.md si aplica.
```

---

## 7. Revisar la feature

Cuando termines una tarea o feature:

```txt
@qa Revisa <FEATURE>.
Verifica:
- pruebas
- aceptación
- trazabilidad
- contratos
- ownership del microservicio
- Definition of Done
```

---

## 8. Actualizar trazabilidad

Toda feature debe quedar conectada:

```txt
Historia → Requerimiento → Microservicio → Spec → Código → Pruebas
```

Archivo:

```txt
docs/04-sdd/traceability-matrix.md
```

---

## Regla de oro

```txt
No código sin SDD.
No SDD sin contexto.
No feature sin microservicio dueño.
No cambios entre microservicios sin contrato.
```

---

## Flujo resumido

```txt
1. Elegir feature
2. Generar spec.md
3. Revisar spec
4. Generar design.md + tasks.md
5. Revisar Definition of Ready
6. Implementar una tarea
7. Probar
8. Actualizar acceptance + trazabilidad
```
