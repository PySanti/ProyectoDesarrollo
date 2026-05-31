# HU-17 — Tasks: Crear y publicar partida de Trivia

Implementar **una tarea a la vez** en el orden sugerido. No iniciar implementación hasta aprobación del SDD.

## Convenciones

- Servicio: `services/trivia-game-service/` (misma solución que HU-15)
- Solución .NET 8 con capas: `Domain`, `Application`, `Infrastructure`, `Api`
- Código de producción y pruebas en **inglés**; comentarios explicativos en **español**
- Cliente: React web (congelado; no implementar en este sprint)

---

## 1. Domain

| ID | Task | Definition of done |
| --- | --- | --- |
| D-01 | Value objects: `PartidaId`, `NombrePartida`, `TiempoInicio`, `CantidadMinima`, `CantidadMaximaJugadores`, `CantidadMaximaEquipos`, `JugadoresPorEquipoMin`, `JugadoresPorEquipoMax` con validación y equality | ✅ VOs creados |
| D-02 | Enums `PartidaEstado` (Lobby, Iniciada, Cancelada, Terminada), `Modalidad` (Individual, Equipo), `ModoInicio` (Manual, Automatico) | ✅ Enums definidos |
| D-03 | Aggregate root `PartidaTrivia` con factory `Create`, métodos `PublicarLobby`, `Iniciar`, `Cancelar`, validación de transiciones de estado y reglas de modalidad/límites | ✅ Invariantes protegidas |
| D-04 | Excepciones de dominio: `PartidaTriviaNotFoundException`, `InvalidStateTransitionException`, `MinimosNoCumplidosException`, `FormularioIncompletoException`, `ModalidadInvalidaException` | ✅ Excepciones creadas |
| D-05 | Eventos de dominio in-process: `PartidaTriviaCreadaDomainEvent`, `PartidaTriviaPublicadaDomainEvent`, `PartidaTriviaIniciadaDomainEvent`, `PartidaTriviaCanceladaDomainEvent` | ✅ Eventos publicados desde aggregate |

---

## 2. Application

| ID | Task | Definition of done |
| --- | --- | --- |
| A-01 | Puerto `IPartidaTriviaRepository` con AddAsync, GetByIdAsync, UpdateAsync | ✅ Interface definida |
| A-02 | DTOs: `TriviaGameDetailDto`, `CreateTriviaGameRequest` | ✅ Alineados con contrato |
| A-03 | Command `CreateTriviaGameCommand` + handler (valida formulario completo, crea PartidaTrivia en Lobby) | ✅ Crea y persiste |
| A-04 | Command `StartTriviaGameCommand` + handler (valida mínimos, cambia a Iniciada o lanza MinimosNoCumplidosException) | ✅ Inicia o rechaza |
| A-05 | Query `GetTriviaGameByIdQuery` + handler | ✅ Solo lectura |
| A-06 | Validators FluentValidation para CreateTriviaGameCommand y StartTriviaGameCommand | ✅ Reglas de negocio reflejadas |
| A-07 | Mapper PartidaTrivia ↔ TriviaGameDetailDto | ✅ Sin reglas en mapper |

---

## 3. Infrastructure

| ID | Task | Definition of done |
| --- | --- | --- |
| I-01 | Config EF Core `PartidaTriviaConfiguration` + DbSet en `TriviaGameDbContext` | ✅ Mapping correcto con ValueConverters |
| I-02 | Implementar `PartidaTriviaRepository` | ✅ Add/Get/Update funcionando |
| I-03 | Migración nueva para tabla `partidas_trivia` | ✅ Migración generada |

---

## 4. API

| ID | Task | Definition of done |
| --- | --- | --- |
| P-01 | Endpoint `POST /api/trivia-games` (crear partida) | ✅ 201 con Location |
| P-02 | Endpoint `POST /api/trivia-games/{id}/start` (iniciar partida) | ✅ 200 o 409 según reglas |
| P-03 | Endpoint `GET /api/trivia-games/{id}` (detalle) | ✅ 200 / 404 |
| P-04 | Reutilizar policy `Operador` del controller existente | ✅ 403 si no es operador |
| P-05 | Mapeo de nuevas excepciones de dominio a 400/404/409 | ✅ Middleware extendido |

---

## 5. Contracts

| ID | Task | Definition of done |
| --- | --- | --- |
| C-01 | Documentar endpoints nuevos en `contracts/http/trivia-game-api.md` (sección HU-17) | ✅ Contrato actualizado |
| C-02 | Verificar si `contracts/events/trivia-game-events.md` requiere actualización (no aplica para esta HU) | ✅ Confirmado sin eventos |

---

## 6. Tests

| ID | Task | Definition of done |
| --- | --- | --- |
| T-01 | Tests unitarios de dominio (D-03, D-04, D-05) — creación, transiciones, validación de límites, rechazo de estados inválidos | ✅ |
| T-02 | Tests unitarios de validators Application | ✅ |
| T-03 | Tests de integración API: create → start → get; create → start sin mínimos → 409 | ✅ InMemory DB |
| T-04 | Tests de integración autorización 403 | ✅ |
| T-05 | Tests de integración auto-cancelación (simulación de timer) | ✅ |

---

## 7. Acceptance and traceability

| ID | Task | Definition of done |
| --- | --- | --- |
| AT-01 | Ejecutar checklist de `acceptance.md` | ✅ Backend items verificados |
| AT-02 | Actualizar `docs/04-sdd/traceability-matrix.md` fila HU-17 | ✅ Status actualizado |
| AT-03 | Marcar tareas completadas en este archivo | ✅ Checkboxes actualizados |

---

## Orden de implementación recomendado

```txt
D-01 → D-02 → D-03 → D-04 → D-05
  → T-01
  → A-01 → A-02 → A-06 → A-03 → A-04 → A-05 → A-07 → T-02
  → I-01 → I-02 → I-03
  → P-01 → P-02 → P-03 → P-04 → P-05 → T-03 → T-04 → T-05
  → C-01 → C-02
  → AT-01 → AT-02 → AT-03
```

## Estimación orientativa

| Capa | Esfuerzo relativo |
| --- | --- |
| Domain + tests | 1 d |
| Application + Infrastructure + API | 1.5 d |
| Contracts | 0.25 d |
| Tests de integración | 0.5 d |
| Acceptance | 0.25 d |
| **Total** | **~3.5 d** |

## Bloqueos conocidos

Ninguno. El microservicio Trivia Game Service ya tiene solución base con Domain, Application, Infrastructure y API del HU-15. Se reutilizan `TriviaGameDbContext`, `ValueConverters`, `DomainEventDispatcher`, `ExceptionHandlingMiddleware`, `PolicyNames.Operador` y configuraciones de DI.
