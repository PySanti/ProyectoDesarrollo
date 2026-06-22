# HU-26 — Tasks: Responder Trivia individual

Implementar **una tarea a la vez** en el orden sugerido. No iniciar implementación hasta aprobación del SDD.

## Convenciones

- Servicio: `services/trivia-game-service/`
- Solución .NET 8 con capas: `Domain`, `Application`, `Infrastructure`, `Api`
- Frontend: React Native mobile (congelado — solo backend en este sprint)

---

## 1. Domain

| ID | Task | Definition of done |
| --- | --- | --- |
| D-01 | Crear value object `RespuestaTriviaId` con validación y equality | ✅ Done |
| D-02 | Crear entidad `RespuestaTrivia` con factory method, propiedades y validaciones | ✅ Done |
| D-03 | Extender `PartidaTrivia`: agregar `PreguntaActualId`, `_respuestas`, `_puntajesAcumulados`, métodos `AbrirPrimeraPregunta()`, `RegistrarRespuestaDefinitiva()`, `CerrarPreguntaActual()` | ✅ Done |
| D-04 | Crear evento `RespuestaTriviaRegistradaDomainEvent` | ✅ Done |
| D-05 | Crear excepciones de dominio: `EstadoPartidaInvalidoException`, `PreguntaNoActivaException`, `RespuestaDuplicadaException`, `RespuestaTardiaException`, `ModalidadInvalidaException` (reutilizar existentes si aplica) | ✅ Done |

---

## 2. Application

| ID | Task | Definition of done |
| --- | --- | --- |
| A-01 | Create `AnswerTriviaQuestionCommand` record | ✅ Done |
| A-02 | Create `AnswerTriviaResultDto` response | ✅ Done |
| A-03 | Create `AnswerTriviaQuestionCommandValidator` (FluentValidation) | ✅ Done |
| A-04 | Create `AnswerTriviaQuestionCommandHandler` with full validation flow | ✅ Done |
| A-05 | Create `IRespuestaTriviaRepository` port | ✅ Done |
| A-06 | Implement access to `PartidaTrivia` with questions loaded (extend `IPartidaTriviaRepository` if needed) | ✅ Done |

---

## 3. Infrastructure

| ID | Task | Definition of done |
| --- | --- | --- |
| I-01 | Create `RespuestaTriviaConfiguration` EF Core mapping | ✅ Done |
| I-02 | Add `trivia_respuestas` DbSet to `TriviaGameDbContext` | ✅ Done |
| I-03 | Implement `RespuestaTriviaRepository` | ✅ Done |
| I-04 | Update `PartidaTrivia` EF mapping for `PreguntaActualId`, `_respuestas` collection | ✅ Done |
| I-05 | Generate and apply EF Core migration | ✅ Done |
| I-06 | Create stub repository for tests | ✅ Done |

---

## 4. API

| ID | Task | Definition of done |
| --- | --- | --- |
| P-01 | Create `POST /api/trivia-games/{partidaId}/questions/{preguntaId}/answer` endpoint in `TriviaGameController` | ✅ Done |
| P-02 | Map `sub` claim from JWT to `UsuarioId` for handler | ✅ Done |
| P-03 | Map domain exceptions to HTTP status codes (409, 404, 400) | ✅ Done |
| P-04 | Register new services in `Program.cs` / DI | ✅ Done |

---

## 5. Contracts

| ID | Task | Definition of done |
| --- | --- | --- |
| C-01 | Document answer endpoint in `contracts/http/trivia-game-api.md` | ✅ Done |
| C-02 | Update `docs/04-sdd/traceability-matrix.md` row HU-26 | ✅ Done |

---

## 6. Tests

| ID | Task | Definition of done |
| --- | --- | --- |
| T-01 | Domain unit tests for `RespuestaTrivia.Create()` and `PartidaTrivia.RegistrarRespuestaDefinitiva()` | ✅ Done |
| T-02 | Application unit tests for `AnswerTriviaQuestionCommandValidator` | ✅ Done |
| T-03 | Application handler tests with mocked repository | ✅ Done |
| T-04 | API integration tests (success, duplicate, late, not registered, wrong modality) | ✅ Done |

---

## 7. Frontend (React Native mobile)

| ID | Task | Definition of done |
| --- | --- | --- |
| F-01 | Pending — frontend frozen | ⏳ Pending |

---

## 8. Acceptance and traceability

| ID | Task | Definition of done |
| --- | --- | --- |
| AT-01 | Execute `acceptance.md` checklist | ✅ Done |
| AT-02 | Update `docs/04-sdd/traceability-matrix.md` row HU-26 | ✅ Done |
| AT-03 | Mark tasks completed in this file | ✅ Done |

---

## Orden de implementación recomendado

```txt
D-01 → D-02 → D-03 → D-04 → D-05 ✅
  → T-01 ✅
  → A-01 → A-02 → A-03 → A-04 → A-05 → A-06 ✅
  → T-02 → T-03 ✅
  → I-01 → I-02 → I-03 → I-04 → I-05 → I-06 ✅
  → P-01 → P-02 → P-03 → P-04 → T-04 ✅
  → C-01 → C-02 ✅
  → AT-01 → AT-02 → AT-03 ✅
```

## Estimación orientativa

| Capa | Esfuerzo relativo |
| --- | --- |
| Domain + tests | 1 d |
| Application | 0.75 d |
| Infrastructure + API | 1 d |
| Contracts | 0.25 d |
| **Total** | **~3 d** |

## Bloqueos conocidos

- `PartidaTrivia` necesita cargar `TriviaForm` + `Questions` para validar respuesta. Depende de HU-15 (formularios ya implementados).
- Se necesita determinar cómo abrir la primera pregunta al iniciar partida (HU-24). Se asume mecanismo simple: al iniciar, `AbrirPrimeraPregunta()` se invoca en el handler de inicio.
