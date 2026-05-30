# Known Ambiguities and Required Decisions — UMBRAL

Este archivo registra puntos del project-source que deben resolverse antes de implementar ciertas features.

## 1. Topología de microservicios

### Estado

Resuelto por `docs/05-decisions/ADR-0006-four-service-topology.md`.

### Decisión vigente

UMBRAL se implementa con cuatro microservicios físicos:

1. Identity Service
2. Team Service
3. Trivia Game Service
4. BDT Game Service

No son microservicios físicos en la arquitectura vigente:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

### Regla para OpenCode

OpenCode no debe crear, modificar, documentar ni referenciar esos nombres como servicios físicos activos.

Las responsabilidades de puntaje, ranking, auditoría e historial se ubican dentro del servicio dueño del flujo:

- Trivia Game Service para puntaje, ranking e historial de Trivia.
- BDT Game Service para puntaje, ranking e historial de BDT.
- Team Service para historial relacionado con equipos.
- Identity Service para historial relacionado con usuarios.

## 2. Puntaje de Trivia: fórmula del SRS vs acumulación directa

### Observación

El SRS indica que el puntaje puede calcularse con fórmula ponderada por tiempo restante:

```txt
puntaje_obtenido = puntaje_pregunta * (tiempo_restante / tiempo_total)
```

El modelo/diagrama de clases menciona acumulación directa del `PuntajeAsignado` al `PuntajeAcumulado`.

### Decisión requerida

Antes de implementar HU-29, confirmar una de estas opciones:

- **Opción A — SRS estricto:** usar fórmula ponderada por tiempo restante.
- **Opción B — Modelo de dominio confirmado:** sumar directamente el puntaje asignado si la respuesta es correcta.

### Regla temporal

OpenCode no debe implementar HU-29 hasta que el `spec.md` de esa HU resuelva explícitamente esta decisión.

## 3. Mínimo de integrantes de equipo

### Observación

Las historias de usuario permiten que un participante cree un equipo y quede como líder. El diagrama indica que un equipo contiene `1..5` participantes, pero una versión del modelo menciona una invariante `≥2 y ≤5`.

### Decisión requerida

Antes de implementar reglas de equipo, confirmar si el equipo puede existir con:

- **1 a 5 integrantes**, o
- **2 a 5 integrantes**.

### Regla temporal

Para HU-03, HU-04, HU-05, HU-06 y HU-07, el SDD debe fijar explícitamente esta regla antes de implementar.
