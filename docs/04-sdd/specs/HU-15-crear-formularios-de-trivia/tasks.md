# HU-15 — Tasks: Crear formularios de Trivia

Implementar **una tarea a la vez** en el orden sugerido. No iniciar implementación hasta aprobación del SDD.

## Convenciones

- Servicio: `services/trivia-game-service/`
- Solución .NET 8 con capas: `Domain`, `Application`, `Infrastructure`, `Api`
- Cliente: `web/` (React) — rutas bajo panel operador

---

## 1. Domain

| ID | Task | Definition of done |
| --- | --- | --- |
| D-01 | Crear value objects (`FormTitle`, `QuestionText`, `OptionText`, `AssignedScore`, `TimeLimit`, `TriviaFormId`, `QuestionId`, `OperatorId`) con validación y equality | ✅ Done — `services/trivia-game-service/` |
| D-02 | Implementar value object `AnswerOption` y drafts `QuestionDraft` / `AnswerOptionDraft` para construcción | ✅ Done |
| D-03 | Implementar entidad `Question` con invariantes: 4 opciones, 1 correcta, score y timer en rango | Tests cubren casos inválidos |
| D-04 | Implementar aggregate root `TriviaForm` con `Create`, `UpdateTitle`, `ReplaceQuestions` | Rechaza conjunto vacío de preguntas |
| D-05 | Implementar `TriviaFormCompletenessValidator` + excepciones de dominio | `IsComplete` y `GetIncompleteReasons` testeados |
| D-06 | Definir eventos de dominio in-process `TriviaFormCreatedDomainEvent`, `TriviaFormUpdatedDomainEvent` | Publicados desde aggregate si el proyecto ya usa patrón de eventos de dominio |

---

## 2. Application

| ID | Task | Definition of done |
| --- | --- | --- |
| A-01 | Registrar MediatR y FluentValidation en capa Application | DI configurado |
| A-02 | Crear DTOs de entrada/salida (`QuestionInputDto`, `TriviaFormDetailDto`, etc.) | Alineados con contrato HTTP del design |
| A-03 | Implementar `CreateTriviaFormCommand` + handler | Persiste formulario y retorna DTO |
| A-04 | Implementar `UpdateTriviaFormCommand` + handler | 404 si no existe |
| A-05 | Implementar `GetTriviaFormByIdQuery` + handler | Solo lectura; retorna DTO o null |
| A-06 | Implementar validators FluentValidation para create/update | Espejan reglas HU-15-FORM-001..006 |
| A-07 | Definir puerto `ITriviaFormRepository` | Interface en Application |

---

## 3. Infrastructure

| ID | Task | Definition of done |
| --- | --- | --- |
| I-01 | Crear `TriviaGameDbContext` con sets para forms, questions, options | PostgreSQL connection string desde configuración |
| I-02 | Configurar EF Core mappings (`TriviaFormConfiguration`, etc.) | Tablas `trivia_forms`, `trivia_questions`, `trivia_answer_options` |
| I-03 | Implementar `TriviaFormRepository` | Add/Get/Update funcionando |
| I-04 | Implementar mappers dominio ↔ persistencia | Sin reglas de negocio en mappers |
| I-05 | Generar y aplicar migración inicial para tablas de formulario | Migración versionada en repo |

---

## 4. API

| ID | Task | Definition of done |
| --- | --- | --- |
| P-01 | Crear `TriviaFormsController` con POST, PUT, GET | Delega a MediatR; sin lógica de negocio |
| P-02 | Configurar policy de autorización `Operador` | 403 para roles no permitidos |
| P-03 | Mapear excepciones de dominio y null query a 400/404 | Respuestas de error consistentes |
| P-04 | Registrar servicios en `Program.cs` / pipeline del microservicio | Swagger muestra endpoints (si aplica) |

---

## 5. Contracts

| ID | Task | Definition of done |
| --- | --- | --- |
| C-01 | Documentar POST/PUT/GET en `contracts/http/trivia-game-api.md` | Según plantilla del contrato |
| C-02 | Actualizar `docs/03-microservices/api-contracts.md` si requiere índice de endpoints HU-15 | Referencia cruzada a trivia-game-api |

---

## 6. Tests

| ID | Task | Definition of done |
| --- | --- | --- |
| T-01 | Tests unitarios de dominio (D-03, D-04, D-05) | CI verde |
| T-02 | Tests unitarios de validators Application | Casos límite 4 opciones / 1 correcta |
| T-03 | Tests de integración API: create → get → update → get | Usa Testcontainers PostgreSQL o DB de test |
| T-04 | Tests de integración autorización 403 | Token sin rol Operador |
| T-05 | Tests frontend: editor de preguntas (opcional mínimo con Testing Library) | Radio única correcta; 4 slots de opción |

---

## 7. Frontend (React web)

| ID | Task | Definition of done |
| --- | --- | --- |
| F-01 | Crear cliente API `triviaFormsApi` | Tipado TypeScript alineado con DTO |
| F-02 | Implementar `TriviaFormEditorPage` (create) | POST exitoso navega a detalle o muestra éxito |
| F-03 | Implementar ruta edit `:formId` | PUT con carga inicial GET |
| F-04 | Implementar `QuestionEditor` con 4 opciones y selector de correcta | Validación UX antes de submit |
| F-05 | Mostrar badge `isComplete` / errores `incompleteReasons` | Operador entiende si el formulario es usable en partida |
| F-06 | Integrar rutas en navegación del panel operador | Enlace accesible para rol Operador |

---

## 8. Acceptance and traceability

| ID | Task | Definition of done |
| --- | --- | --- |
| AT-01 | Ejecutar checklist de `acceptance.md` manualmente | Evidencia registrada en acceptance.md |
| AT-02 | Actualizar `docs/04-sdd/traceability-matrix.md` fila HU-15 | Status → In progress / Done según avance |
| AT-03 | Marcar tareas completadas en este archivo | Checkboxes actualizados por implementador |

---

## Orden de implementación recomendado

```txt
D-01 → D-02 → D-03 → D-04 → D-05 → D-06
  → T-01
  → A-07 → A-02 → A-06 → A-03 → A-04 → A-05 → T-02
  → I-01 → I-02 → I-03 → I-04 → I-05
  → P-01 → P-02 → P-03 → P-04 → T-03 → T-04
  → C-01 → C-02
  → F-01 → F-04 → F-02 → F-03 → F-05 → F-06 → T-05
  → AT-01 → AT-02 → AT-03
```

## Estimación orientativa

| Capa | Esfuerzo relativo |
| --- | --- |
| Domain + tests | 1.5 d |
| Application + Infrastructure + API | 2 d |
| Contracts | 0.25 d |
| Frontend | 1.5 d |
| Acceptance | 0.5 d |
| **Total** | **~5.75 d** |

## Bloqueos conocidos

Ninguno. El microservicio Trivia Game Service aún no tiene código base en el repositorio; la tarea I-01 puede incluir bootstrap mínimo de solución si no existe.
