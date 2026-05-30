# Events Catalog

> **Regla de generación:** este contenido fue generado exclusivamente a partir de los archivos `diagrama de clases(2).md`, `enunciado-proyecto(1).md`, `historias de usuario(2).md`, `microservicios(2).md`, `modelo de dominio(2).md` y `srs(2).md`.
>
> **No se agregan microservicios, endpoints, colas, eventos, bases de datos, rutas HTTP ni contratos que no estén indicados explícitamente en esas fuentes.** Cuando una responsabilidad aparece en el SRS/modelo pero no está asignada a un microservicio en `microservicios(2).md`, queda marcada como **no asignada / pendiente de decisión**.


## Estado del project-source

El SRS exige eventos relevantes del dominio para auditoría, historial, notificaciones internas, actualización de ranking, trazabilidad de puntajes y comunicación en tiempo real.

El modelo de dominio nombra algunos eventos concretos.

El diagrama de clases define tipos de evento histórico.

El project-source no especifica:

- formato exacto de payload;
- exchange;
- queue;
- routing key;
- versión;
- esquema JSON;
- publicador técnico;
- consumidor técnico.

Por tanto, este catálogo no inventa contratos de mensajería completos.

## Eventos de dominio nombrados explícitamente en `modelo de dominio(2).md`

| Evento | Contexto | Descripción derivada de fuente |
|---|---|---|
| `RespuestaTriviaValidada` | Trivia | Lleva `ParticipanteId`, si la respuesta fue correcta o incorrecta, y el tiempo empleado. |
| `PuntajeTriviaIncrementado` | Trivia | Se dispara cuando el participante suma puntos. |
| `HitoBDTEncontrado` | BDT | Lleva el `ParticipanteId` que escaneó exitosamente el QR. |
| `PuntajeBDTIncrementado` | BDT | Notifica que un competidor sumó los puntos de la etapa. |
| `PartidaTriviaFinalizada` | Trivia | Lleva el estado final de participantes con puntajes consolidados. |
| `PartidaBDTFinalizada` | BDT | Lleva el estado final de participantes con puntajes consolidados. |

## Tipos de evento histórico nombrados en `diagrama de clases(2).md`

| TipoEventoHistorial |
|---|
| `CambioEstado` |
| `Inscripcion` |
| `Convocatoria` |
| `RespuestaTrivia` |
| `TesoroSubido` |
| `ValidacionQR` |
| `PistaEnviada` |
| `Ubicacion` |
| `Puntaje` |
| `Cancelacion` |
| `Resultado` |

## Categorías de eventos exigidas por el SRS

| Categoría | Fuente funcional |
|---|---|
| Cambios de estado | RF-12, RF-37, reglas generales de partida. |
| Inscripciones | RF-10, RF-12, RF-37. |
| Convocatorias | RF-10, RF-12, RF-37. |
| Respuestas de Trivia | RF-12, RF-20, RF-21, RF-37. |
| Tesoros subidos | RF-12, RF-28, RF-30, RF-37. |
| Validaciones de QR | RF-12, RF-29, RF-30, RF-37. |
| Pistas enviadas | RF-12, RF-33, RF-37. |
| Ubicaciones relevantes | RF-12, RF-34, RF-37. |
| Variaciones de puntaje | RF-12, RF-22, RF-37. |
| Cancelaciones | RF-12, RF-23, RF-37. |
| Resultados de partida | RF-12, RF-31, RF-32, RF-37. |
| Ranking | RF-13, RF-22, RF-37. |

## Publicadores explícitos o deducidos sin crear servicios nuevos

| Evento / categoría | Publicador según contexto | Estado |
|---|---|---|
| `RespuestaTriviaValidada` | Trivia Game Service. | Confirmado por contexto de Trivia. |
| `PuntajeTriviaIncrementado` | Trivia Game Service. | Confirmado por modelo. |
| `HitoBDTEncontrado` | BDT Game Service. | Confirmado por contexto BDT. |
| `PuntajeBDTIncrementado` | BDT Game Service. | Confirmado por modelo. |
| `PartidaTriviaFinalizada` | Trivia Game Service. | Confirmado por nombre/contexto. |
| `PartidaBDTFinalizada` | BDT Game Service. | Confirmado por nombre/contexto. |
| Eventos históricos de auditoría | No asignado a microservicio explícito. | Pendiente. |
| Notificaciones internas | No asignado a microservicio explícito. | Pendiente. |
| Actualización de ranking global | No asignado a microservicio explícito; el modelo ubica `ClasificadorRankingService` dentro de Trivia y BDT. | Pendiente para cada HU. |

## Regla para contratos de eventos

Antes de implementar un evento en una HU, el SDD debe completar:

```md
| Campo | Valor |
|---|---|
| Evento | <nombre> |
| Productor | <microservicio> |
| Consumidor | No especificado / especificar en SDD |
| Motivo | <RF/RB/HU> |
| Payload | Pendiente de definir |
| Efecto en tiempo real | Sí / No |
| Se registra en historial | Sí / No |
```
