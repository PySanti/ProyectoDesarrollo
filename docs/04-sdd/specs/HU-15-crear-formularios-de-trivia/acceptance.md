# HU-15 — Acceptance: Crear formularios de Trivia

## Metadata

| Campo | Valor |
| --- | --- |
| HU | HU-15 |
| Feature | Crear formularios de Trivia |
| Actor | Operador |
| Client | React web |
| Service | Trivia Game Service |
| SDD status | Ready for review |
| Implementation status | Backend complete / React web minimal creation UI integrated |

---

## Acceptance checklist

### Funcional — Backend

- [x] **AC-BE-01** POST `/api/trivia-forms` crea formulario con título y ≥1 pregunta válida → **201** con `id` e `isComplete: true`.
- [x] **AC-BE-02** POST rechaza formulario sin preguntas → **400**.
- [x] **AC-BE-03** POST rechaza pregunta con ≠4 opciones → **400**.
- [x] **AC-BE-04** POST rechaza pregunta con ≠1 opción correcta → **400**.
- [x] **AC-BE-05** POST rechaza `assignedScore` ≤ 0 o `timeLimitSeconds` ≤ 0 → **400**.
- [x] **AC-BE-06** PUT `/api/trivia-forms/{id}` actualiza formulario existente → **200** con datos actualizados.
- [x] **AC-BE-07** PUT con id inexistente → **404**.
- [x] **AC-BE-08** GET `/api/trivia-forms/{id}` retorna detalle completo sin modificar estado → **200**.
- [x] **AC-BE-09** GET con id inexistente → **404**.
- [x] **AC-BE-10** Usuario sin rol Operador recibe **403** en POST, PUT y GET.
- [ ] **AC-BE-11** Request sin autenticación recibe **401**.
- [x] **AC-BE-12** Respuesta incluye `isComplete` e `incompleteReasons` coherentes con reglas RF-16.
- [x] **AC-BE-13** Modelo persistido no contiene campos de ponderación de puntaje por tiempo.

### Funcional — Frontend

- [x] **AC-FE-01** Operador accede a pantalla de creación de formulario desde panel web.
- [ ] **AC-FE-02** Puede ingresar título y agregar múltiples preguntas.
- [x] **AC-FE-03** Cada pregunta muestra exactamente 4 campos de opción.
- [x] **AC-FE-04** Solo una opción puede marcarse como correcta por pregunta (select único en UI mínima).
- [x] **AC-FE-05** Puede configurar puntaje y temporizador por pregunta.
- [x] **AC-FE-06** Errores de validación del backend se muestran al operador.
- [x] **AC-FE-07** Tras guardar, se visualiza mensaje de éxito con formulario creado.
- [ ] **AC-FE-08** Operador puede abrir formulario existente en modo edición y persistir cambios.

### Reglas de negocio

- [ ] **AC-RB-01** BR-T01: solo operador gestiona formularios.
- [ ] **AC-RB-02** BR-T02 / TRIVIA-FORM-001: estructura mínima cumplida en formularios completos.
- [ ] **AC-RB-03** BR-T03: formulario incompleto expone `isComplete: false` (bloqueo en partida delegado a HU-17).
- [ ] **AC-RB-04** BR-T15 / TRIVIA-SCORE-003: puntaje fijo por pregunta; tiempo no altera puntaje en el modelo.
- [ ] **AC-RB-05** BR-T16 / BR-T17: diseño refleja score en pregunta correcta e implícito 0 en incorrectas.
- [ ] **AC-RB-06** HU-15-FORM-001..004: 4 opciones, 1 correcta, puntaje a nivel pregunta, sin ponderación temporal.

### Contratos y trazabilidad

- [ ] **AC-CT-01** Endpoints documentados en `contracts/http/trivia-game-api.md`.
- [x] **AC-CT-02** Fila HU-15 actualizada en `docs/04-sdd/traceability-matrix.md`.
- [ ] **AC-CT-03** Sin eventos RabbitMQ requeridos para esta HU (verificado).

---

## Manual verification steps

### Escenario 1 — Crear formulario válido

1. Iniciar sesión como **Operador** en la app web.
2. Navegar a **Nuevo formulario de Trivia**.
3. Ingresar título: `Sprint Demo Trivia`.
4. Agregar pregunta:
   - Texto: `2 + 2 = ?`
   - Puntaje: `10`
   - Temporizador: `30` segundos
   - Opciones: `4` (correcta), `3`, `5`, `22` — marcar `4` como correcta.
5. Guardar.
6. **Esperado:** mensaje de éxito; formulario con `isComplete: true`; id asignado.

### Escenario 2 — Validación de 4 opciones

1. En creación de formulario, intentar guardar pregunta con 3 opciones (vía API o UI si permite).
2. **Esperado:** error 400 / mensaje indicando que se requieren exactamente 4 opciones.

### Escenario 3 — Validación de respuesta correcta única

1. Enviar pregunta con 4 opciones pero ninguna o dos marcadas como correctas.
2. **Esperado:** error 400.

### Escenario 4 — Consultar y editar

1. GET del formulario creado en Escenario 1.
2. Cambiar título a `Sprint Demo Trivia v2` y agregar segunda pregunta válida.
3. PUT y GET nuevamente.
4. **Esperado:** datos actualizados; orden de preguntas preservado.

### Escenario 5 — Autorización

1. Repetir GET con token de **Participante** o **Administrador** (si admin no debe acceder).
2. **Esperado:** 403.

### Escenario 6 — Puntaje sin ponderación temporal

1. Inspeccionar payload GET de formulario con pregunta score=10, timer=30.
2. **Esperado:** solo `assignedScore: 10` y `timeLimitSeconds: 30`; sin campos de multiplicador temporal.

---

## Automated test evidence

| Test suite | File / location | Status | Evidence |
| --- | --- | --- | --- |
| Domain unit tests | `TriviaFormTests`, `QuestionTests`, `TriviaFormCompletenessValidatorTests` | Done | 101 tests green |
| Application validator tests | `CreateTriviaFormCommandValidatorTests` | Done | 32 tests green |
| API integration tests | `TriviaFormsControllerTests` | Done | 6 tests green |
| Auth integration tests | `TriviaFormsControllerTests` (403 test) | Done | Included in 6 tests |
| Frontend component tests | `frontend/src/features/trivia/TriviaOperationsPage.test.tsx` | Done | `npm test --prefix frontend` → 43 passed |

## Integration pass evidence

- React web added `TriviaOperationsPage` for a minimal HU-15 creation flow with exactly four options, one selected correct answer, score and time limit.
- Validation run: `npm test --prefix frontend` → 43 passed.
- Validation run: `npm run build --prefix frontend` → passed.

> Completar columna **Evidence** con enlace a CI run, captura o comando local al finalizar implementación.

---

## Traceability status

| Artefact | Reference | Status |
| --- | --- | --- |
| HU-15 | `docs/01-project-source/srs.md` | Mapped |
| RF-15 | Crear, editar, consultar formularios | Covered by spec + design |
| RF-16 | Validación de completitud | Covered by domain validator + `isComplete` |
| RF-35 | Consulta sin mutación | GET endpoint |
| RB-T01..T03, T15..T17, T20 | Reglas Trivia formulario | Mapped in spec.md |
| SPECS-LIST | Active sprint | Confirmed |
| Owning service | Trivia Game Service | Confirmed |
| Client | React web | Confirmed |
| HTTP contract | `contracts/http/trivia-game-api.md` | Complete (C-01) |
| Events contract | N/A for HU-15 | N/A |
| Traceability matrix | `docs/04-sdd/traceability-matrix.md` | Updated at SDD creation |

---

## Sign-off

| Rol | Name | Date | Approved |
| --- | --- | --- | --- |
| Product / SRS owner | | | [ ] |
| Tech lead | | | [ ] |
| SDD reviewer | | | [ ] |

---

## Review notes (post-implementation)

_Espacio para observaciones durante QA._
