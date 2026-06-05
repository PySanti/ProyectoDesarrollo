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
| D-01 | Crear value objects (`FormTitle`, `QuestionText`, `OptionText`, `AssignedScore`, `TimeLimit`, `TriviaFormId`, `QuestionId`, `OperatorId`) con validación y equality | ✅ Done |
| D-02 | Implementar value object `AnswerOption` y drafts | ✅ Done |
| D-03 | Implementar entidad `Question` con invariantes | ✅ Done |
| D-04 | Implementar aggregate root `TriviaForm` | ✅ Done |
| D-05 | Implementar `TriviaFormCompletenessValidator` | ✅ Done |
| D-06 | Definir eventos de dominio in-process | ✅ Done |

---

## 2. Application

| ID | Task | Definition of done |
| --- | --- | --- |
| ID | Task | Definition of done |
| --- | --- | --- |
| A-01 | Registrar MediatR y FluentValidation en capa Application | ✅ Done |
| A-02 | Crear DTOs de entrada/salida | ✅ Done |
| A-03 | Implementar `CreateTriviaFormCommand` + handler | ✅ Done |
| A-04 | Implementar `UpdateTriviaFormCommand` + handler | ✅ Done |
| A-05 | Implementar `GetTriviaFormByIdQuery` + handler | ✅ Done |
| A-06 | Implementar validators FluentValidation | ✅ Done |
| A-07 | Definir puerto `ITriviaFormRepository` | ✅ Done |

---

## 3. Infrastructure

| ID | Task | Definition of done |
| --- | --- | --- |
| I-01 | Crear `TriviaGameDbContext` con sets para forms, questions, options | ✅ Done |
| I-02 | Configurar EF Core mappings | ✅ Done |
| I-03 | Implementar `TriviaFormRepository` | ✅ Done |
| I-04 | Implementar mappers dominio ↔ persistencia | ✅ Done |
| I-05 | Generar y aplicar migración inicial | ✅ Done |

---

## 4. API

| ID | Task | Definition of done |
| --- | --- | --- |
| P-01 | Crear `TriviaFormsController` con POST, PUT, GET | ✅ Done |
| P-02 | Configurar policy de autorización `Operador` | ✅ Done |
| P-03 | Mapear excepciones de dominio y null query a 400/404 | ✅ Done |
| P-04 | Registrar servicios en `Program.cs` / pipeline del microservicio | ✅ Done |

---

## 5. Contracts

| ID | Task | Definition of done |
| --- | --- | --- |
| C-01 | Documentar POST/PUT/GET en `contracts/http/trivia-game-api.md` | ✅ Done |
| C-02 | Actualizar `docs/03-microservices/api-contracts.md` | ✅ Done |

---

## 6. Tests

| ID | Task | Definition of done |
| --- | --- | --- |
| T-01 | Tests unitarios de dominio (D-03, D-04, D-05) | ✅ Done — 101 tests |
| T-02 | Tests unitarios de validators Application | ✅ Done — 32 tests |
| T-03 | Tests de integración API: create → get → update → get | ✅ Done — InMemory DB |
| T-04 | Tests de integración autorización 403 | ✅ Done — 6 API tests total |
| T-05 | Tests frontend: editor de preguntas (opcional mínimo con Testing Library) | ✅ Done — 5 tests en `TriviaOperationsPage.test.tsx`, incluye formulario con múltiples preguntas |

---

## 7. Frontend (React web)

| ID | Task | Definition of done |
| --- | --- | --- |
| F-01 | Crear cliente API `triviaFormsApi` | Tipado TypeScript alineado con DTO |
| F-02 | Implementar `TriviaFormEditorPage` (create) | ✅ Done — creación muestra éxito y soporta una o varias preguntas |
| F-03 | Implementar ruta edit `:formId` | PUT con carga inicial GET |
| F-04 | Implementar `QuestionEditor` con 4 opciones y selector de correcta | ✅ Done — cada pregunta renderiza 4 opciones, selector único, puntaje y tiempo |
| F-05 | Mostrar badge `isComplete` / errores `incompleteReasons` | ⏳ Pending — respuesta exitosa visible; detalle `isComplete` no se muestra en UI mínima |
| F-06 | Integrar rutas en navegación del panel operador | ✅ Done — pantalla accesible desde `Operar Trivia` |

---

## 8. Acceptance and traceability

| ID | Task | Definition of done |
| --- | --- | --- |
| AT-01 | Ejecutar checklist de `acceptance.md` | ✅ Backend items verified |
| AT-02 | Actualizar `docs/04-sdd/traceability-matrix.md` fila HU-15 | ✅ Status → Backend done — 139 tests |
| AT-03 | Marcar tareas completadas en este archivo | ✅ Done (this edit) |

---

## Orden de implementación recomendado

```txt
D-01 → D-02 → D-03 → D-04 → D-05 → D-06 ✅
  → T-01 ✅
  → A-07 → A-02 → A-06 → A-03 → A-04 → A-05 → T-02 ✅
  → I-01 → I-02 → I-03 → I-04 → I-05 ✅
  → P-01 → P-02 → P-03 → P-04 → T-03 → T-04 ✅
  → C-01 → C-02 ✅
  → F-01 → F-04 ✅ → F-02 ✅ → F-03 ⏳ → F-05 ⏳ → F-06 ✅ → T-05 ✅
  → AT-01 → AT-02 → AT-03 ✅
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

Ninguno. El microservicio Trivia Game Service ya tiene solución completa con Domain, Application, Infrastructure, API y pruebas. El frontend web ya soporta creación con múltiples preguntas; edición de formularios existentes y visualización detallada de `isComplete` permanecen pendientes si se exige cierre completo de HU-15.
