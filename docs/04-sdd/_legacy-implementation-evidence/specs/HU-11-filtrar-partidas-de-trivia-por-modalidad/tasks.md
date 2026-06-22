# HU-11 — Tasks: Filtrar partidas de Trivia por modalidad

Implementar **una tarea a la vez**.

## Convenciones

- Servicio: `services/trivia-game-service/`
- Solución .NET 8 con capas: `Domain`, `Application`, `Infrastructure`, `Api`
- Código de producción y pruebas en **inglés**
- Cliente: React Native mobile (congelado; no implementar en este sprint)
- Sin cambios en Domain ni Infrastructure

---

## 1. Application

| ID | Task | Definition of done |
| --- | --- | --- |
| A-01 | Agregar `string? Modalidad` al record `GetPublishedTriviaGamesQuery` | ✅ |
| A-02 | Actualizar `GetPublishedTriviaGamesQueryHandler` para filtrar por modalidad si se provee, usando `TriviaGameMapper.ParseModalidad()` | ✅ |
| A-03 | Agregar método `ParseModalidad(string)` case-insensitive a `TriviaGameMapper` | ✅ |

## 2. API

| ID | Task | Definition of done |
| --- | --- | --- |
| P-01 | Agregar `[FromQuery] string? modalidad` al endpoint `GET /api/trivia-games` en `TriviaGamesPublicController` | ✅ |

## 3. Contracts

| ID | Task | Definition of done |
| --- | --- | --- |
| C-01 | Agregar query parameter `?modalidad` en `contracts/http/trivia-game-api.md` sección HU-09/HU-11 | ✅ |

## 4. Tests

| ID | Task | Definition of done |
| --- | --- | --- |
| T-01 | Handler retorna solo Individual cuando filtro es `"Individual"` | ✅ |
| T-02 | Handler retorna solo Equipo cuando filtro es `"Equipo"` | ✅ |
| T-03 | Handler retorna todas si no se provee filtro (backward compat) | ✅ |
| T-04 | Handler retorna lista vacía si filtro no coincide | ✅ |
| T-05 | `GET /api/trivia-games?modalidad=Individual` retorna 200 con filtro | ✅ |

## 5. Acceptance

| ID | Task | Definition of done |
| --- | --- | --- |
| AT-01 | Completar `acceptance.md` con checklist verificado | ✅ |
| AT-02 | Actualizar `docs/04-sdd/traceability-matrix.md` fila HU-11 | ✅ |
| AT-03 | Marcar tareas completadas en este archivo | ✅ |

---

## Orden de implementación

```txt
A-03 → A-01 → A-02 → P-01 → C-01
  → T-01 → T-02 → T-03 → T-04 → T-05
  → AT-01 → AT-02 → AT-03
```
