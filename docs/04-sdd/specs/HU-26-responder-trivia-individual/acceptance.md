# HU-26 — Acceptance: Responder Trivia individual

## Metadata

| Campo | Valor |
| --- | --- |
| HU | HU-26 |
| Feature | Responder Trivia individual |
| Actor | Participante |
| Client | React Native mobile |
| Service | Trivia Game Service |
| SDD status | Backend implemented |
| Implementation status | Backend done / mobile response UI integrated with parametrized `preguntaId` |

---

## Acceptance checklist

### Funcional — Backend

- [x] **AC-BE-01** POST `/api/trivia-games/{id}/questions/{qid}/answer` con opción correcta → **200** con `esCorrecta: true` y `puntajeObtenido: assignedScore`.
- [x] **AC-BE-02** POST con opción incorrecta → **200** con `esCorrecta: false` y `puntajeObtenido: 0`.
- [x] **AC-BE-03** POST repetido (mismo usuario, misma pregunta) → rechazo de negocio.
- [x] **AC-BE-04** POST después de tiempo límite → rechazo de negocio.
- [x] **AC-BE-05** POST con partida fuera de estado válido → rechazo de negocio.
- [x] **AC-BE-06** POST con participante no inscrito → rechazo de negocio.
- [x] **AC-BE-07** POST con partida de modalidad Equipo → rechazo de negocio para HU-26 individual.
- [x] **AC-BE-08** POST sin pregunta activa → rechazo de negocio.
- [x] **AC-BE-09** Puntaje acumulado = assignedScore de la pregunta, sin ponderación por tiempo.
- [x] **AC-BE-10** Respuesta correcta cierra la pregunta.
- [x] **AC-BE-11** Respuesta incorrecta no cierra la pregunta por sí sola.
- [x] **AC-BE-12** Usuario sin autenticación recibe **401**.

### Reglas de negocio

- [x] **AC-RB-01** RF-20/RB-T21: una única respuesta por jugador por pregunta activa.
- [x] **AC-RB-02** RF-21/RB-T24: rechazar respuestas repetidas/tardías/fuera de estado.
- [x] **AC-RB-03** RF-22/RB-T29: puntaje directo sin ponderación por tiempo.
- [x] **AC-RB-04** RF-21/RB-T25: cerrar pregunta al acertar.
- [x] **AC-RB-05** TRIVIA-SCORE-001: scoreEarned = question.assignedScore.

### Contratos y trazabilidad

- [x] **AC-CT-01** Endpoint documentado en `contracts/http/trivia-game-api.md`.
- [x] **AC-CT-02** Fila HU-26 actualizada en `docs/04-sdd/traceability-matrix.md`.
- [x] **AC-CT-03** Sin eventos RabbitMQ requeridos para esta HU (verificado).

---

## Manual verification steps

### Escenario 1 — Respuesta correcta

1. Crear partida de Trivia individual (HU-17) con formulario que tenga pregunta con score=100, timer=30.
2. Iniciar partida (HU-24).
3. Como participante inscrito, enviar `POST /api/trivia-games/{id}/questions/{qid}/answer` con opción correcta.
4. **Esperado:** `200`, `esCorrecta: true`, `puntajeObtenido: 100`, `preguntaCerrada: true`.

### Escenario 2 — Respuesta incorrecta

1. Repetir escenario 1 pero con opción incorrecta.
2. **Esperado:** `200`, `esCorrecta: false`, `puntajeObtenido: 0`, `preguntaCerrada: false`.

### Escenario 3 — Respuesta duplicada

1. Enviar respuesta a la misma pregunta dos veces.
2. **Esperado:** segunda respuesta → `409`.

### Escenario 4 — Puntaje sin tiempo

1. Verificar en DB que `puntaje_obtenido = assignedScore` sin factores de tiempo.
2. Verificar que no existe columna de multiplicador temporal.

---

## Automated test evidence

| Test suite | File / location | Status | Evidence |
| --- | --- | --- | --- |
| Domain unit tests | `PartidaTriviaTests` answer/scoring cases | Passed in Trivia backend suite | 13 domain tests added for HU-26/HU-28/HU-29 area |
| Application handler tests | `AnswerTriviaQuestionCommandHandlerTests` | Passed in Trivia backend suite | 10 application tests added |
| API integration tests | `TriviaGamesAnswerControllerTests` | Passed in Trivia backend suite | 8 API tests added |
| Mobile UI/typecheck | Implemented | `TriviaAnswerScreen` consumes documented HTTP contract; `npm run typecheck --prefix mobile` passed | `preguntaId` is entered/received as route data because no active-question query contract exists |

---

## Traceability status

| Artefact | Reference | Status |
| --- | --- | --- |
| HU-26 | `docs/01-project-source/srs.md` | Mapped |
| RF-20 | Unanswered | Covered by spec |
| RF-21 | Unanswered validation | Covered by spec + design |
| RF-22 | Direct score | Covered by spec + design |
| RB-T21, RB-T24, RB-T25, RB-T28, RB-T29 | Domain rules | Covered |
| SPECS-LIST | Active sprint | Confirmed |
| Owning service | Trivia Game Service | Confirmed |
| Client | React Native mobile (frozen) | Confirmed |
| HTTP contract | `contracts/http/trivia-game-api.md` | Updated |
| Events contract | N/A for HU-26 | N/A |
| Traceability matrix | `docs/04-sdd/traceability-matrix.md` | Updated |

## Current integration status

Backend command, domain behavior and HTTP endpoint are implemented and tested. React Native answer UI is integrated against the documented answer endpoint. The app does not invent an active-question endpoint; `preguntaId` is currently provided as screen input/route data until HU-25 or a future SDD defines synchronized active-question delivery.

## Integration pass evidence

- `mobile/src/features/trivia/screens/TriviaAnswerScreen.tsx` posts `{ opcionIndex }` to the documented answer endpoint.
- Validation run: `npm test --prefix mobile` → 77 passed.
- Validation run: `npm run typecheck --prefix mobile` → passed.

---

## Sign-off

| Rol | Name | Date | Approved |
| --- | --- | --- | --- |
| Product / SRS owner | | | [ ] |
| Tech lead | | | [ ] |
| SDD reviewer | | | [ ] |
