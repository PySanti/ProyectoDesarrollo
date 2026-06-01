# HU-26 — Acceptance: Responder Trivia individual

## Metadata

| Campo | Valor |
| --- | --- |
| HU | HU-26 |
| Feature | Responder Trivia individual |
| Actor | Participante |
| Client | React Native mobile (frontend congelado) |
| Service | Trivia Game Service |
| SDD status | Nueva |
| Implementation status | Not started |

---

## Acceptance checklist

### Funcional — Backend

- [ ] **AC-BE-01** POST `/api/trivia-games/{id}/questions/{qid}/answer` con opción correcta → **200** con `esCorrecta: true`, `puntajeObtenido: assignedScore`, `preguntaCerrada: true`.
- [ ] **AC-BE-02** POST con opción incorrecta → **200** con `esCorrecta: false`, `puntajeObtenido: 0`, `preguntaCerrada: false`.
- [ ] **AC-BE-03** POST repetido (mismo usuario, misma pregunta) → **409**.
- [ ] **AC-BE-04** POST después de tiempo límite → **409**.
- [ ] **AC-BE-05** POST con partida en estado Lobby → **409**.
- [ ] **AC-BE-06** POST con partida en estado Cancelada → **409**.
- [ ] **AC-BE-07** POST con partida en estado Terminada → **409**.
- [ ] **AC-BE-08** POST con participante no inscrito → **404**.
- [ ] **AC-BE-09** POST con partida de modalidad Equipo → **409**.
- [ ] **AC-BE-10** POST sin pregunta activa → **409**.
- [ ] **AC-BE-11** Puntaje acumulado = assignedScore de la pregunta, sin ponderación por tiempo (verificar en DB).
- [ ] **AC-BE-12** Respuesta correcta cierra la pregunta (`preguntaCerrada: true`).
- [ ] **AC-BE-13** Respuesta incorrecta NO cierra la pregunta (`preguntaCerrada: false`), a menos que se agote el tiempo.
- [ ] **AC-BE-14** Usuario sin autenticación recibe **401**.
- [ ] **AC-BE-15** Usuario con rol Operador o Administrador recibe **403**.

### Reglas de negocio

- [ ] **AC-RB-01** RF-20/RB-T21: una única respuesta por jugador por pregunta activa (verificado en BE-03).
- [ ] **AC-RB-02** RF-21/RB-T24: rechazar respuestas repetidas/tardías/fuera de estado (verificado en BE-03, BE-04, BE-05).
- [ ] **AC-RB-03** RF-22/RB-T29: puntaje directo sin ponderación por tiempo (verificado en BE-11).
- [ ] **AC-RB-04** RF-21/RB-T25: cerrar pregunta al acertar (verificado en BE-12).
- [ ] **AC-RB-05** TRIVIA-SCORE-001: scoreEarned = question.assignedScore (verificado en BE-11).

### Contratos y trazabilidad

- [ ] **AC-CT-01** Endpoint documentado en `contracts/http/trivia-game-api.md`.
- [ ] **AC-CT-02** Fila HU-26 actualizada en `docs/04-sdd/traceability-matrix.md`.
- [ ] **AC-CT-03** Sin eventos RabbitMQ requeridos para esta HU (verificado).

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
| Domain unit tests | `PartidaTriviaAnswerTests` | Pending | — |
| Application handler tests | `AnswerTriviaQuestionCommandHandlerTests` | Pending | — |
| API integration tests | `TriviaGameControllerAnswerTests` | Pending | — |
| Auth integration tests | `TriviaGameControllerAnswerAuthTests` | Pending | — |

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
| HTTP contract | `contracts/http/trivia-game-api.md` | Pending update |
| Events contract | N/A for HU-26 | N/A |
| Traceability matrix | `docs/04-sdd/traceability-matrix.md` | Pending update |

---

## Sign-off

| Rol | Name | Date | Approved |
| --- | --- | --- | --- |
| Product / SRS owner | | | [ ] |
| Tech lead | | | [ ] |
| SDD reviewer | | | [ ] |
