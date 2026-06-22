# HU-29 — Calcular puntaje de respuesta en Trivia

## User story

Como **Participante**, quiero que mi respuesta correcta sume el puntaje configurado para la pregunta, para conocer mi avance en la partida.

## Source

| Campo | Valor |
|---|---|
| HU | HU-29 |
| Actor | Sistema / Participante |
| Requerimientos | RF-22 |
| Reglas de negocio | RB-T28, RB-T29, RB-T34, RB-T35 |
| Microservicio | Trivia Game Service |
| Servicios de apoyo | Identity Service (JWT / autenticación) |
| Cliente objetivo | Backend / React Native mobile (frontend congelado) |

## Scope

### Incluido

- Cálculo del puntaje por respuesta correcta: suma directa del `assignedScore` de la pregunta
- Cálculo del puntaje acumulado del participante en la partida
- Exposición del puntaje acumulado vía endpoint GET
- Verificación explícita de que el tiempo no modifica el puntaje
- Verificación de que respuestas incorrectas no suman puntos

### Excluido

- Ranking general (corresponde a HU-30)
- Visualización mobile (frontend congelado)
- Modalidad por equipos (HU-27)

## Business rules

| ID | Regla |
|---|---|
| RB-T28 | El puntaje se otorga únicamente cuando la respuesta es correcta. |
| RB-T29 | El puntaje de una respuesta correcta debe ser igual al puntaje asignado a la pregunta por el operador. |
| RB-T34 | El tiempo no forma parte del cálculo de puntaje. |
| RB-T35 | En caso de empate en puntaje, el ranking se ordena por menor tiempo acumulado de respuesta. |

## API / Events

### HTTP endpoints

| Método | Ruta | Auth | Propósito |
|---|---|---|---|
| GET | `/api/trivia-games/{id}/score` | Participante | Obtener puntaje acumulado del participante en la partida |

### Response (200 OK)

```json
{
  "partidaId": "uuid",
  "puntajeAcumulado": 300,
  "tiempoAcumuladoSegundos": 45.5,
  "respuestasCorrectas": 3,
  "totalRespuestas": 5
}
```

### Events

Ninguno.

## Criterios de aceptación

- [ ] CA-01: Una respuesta correcta suma exactamente el `assignedScore` de la pregunta.
- [ ] CA-02: Una respuesta incorrecta suma 0 puntos.
- [ ] CA-03: El tiempo empleado no modifica el puntaje (score = assignedScore, no ponderado).
- [ ] CA-04: El endpoint `GET /api/trivia-games/{id}/score` retorna el puntaje acumulado, tiempo acumulado y conteo de respuestas.
- [ ] CA-05: El endpoint retorna 404 si la partida no existe.
- [ ] CA-06: El endpoint retorna 401 si no hay autenticación.

## Tests

| Tipo | Cantidad | Descripción |
|---|---|---|
| Unit (dominio) | 3 | Correcta suma assignedScore exacto; incorrecta suma 0; tiempo no afecta puntaje |
| Unit (handler) | 4 | Handler retorna score correcto; partida no encontrada; sin respuestas; formulario no encontrado |
| API (integración) | 4 | GET score válido; partida no existe 404; no autenticado 401 |
