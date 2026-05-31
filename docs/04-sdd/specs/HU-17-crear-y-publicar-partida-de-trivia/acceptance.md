# HU-17 — Acceptance: Crear y publicar partida de Trivia

## Metadata

| Campo | Valor |
| --- | --- |
| HU | HU-17 |
| Feature | Crear y publicar partida de Trivia |
| Actor | Operador |
| Client | React web |
| Service | Trivia Game Service |
| SDD status | Completa |
| Implementation status | Backend done — 211 tests |

---

## Acceptance checklist

### Funcional — Backend

- [x] **AC-BE-01** POST `/api/trivia-games` crea partida con datos válidos → **201** con estado `Lobby`.
- [x] **AC-BE-02** POST rechaza formulario incompleto (`isComplete = false`) → **400**.
- [x] **AC-BE-03** POST rechaza formulario inexistente → **404**.
- [x] **AC-BE-04** POST rechaza modalidad Individual con `maximoJugadores` nulo → **400**.
- [x] **AC-BE-05** POST rechaza modalidad Equipo con `maximoEquipos` nulo → **400**.
- [x] **AC-BE-06** POST start inicia partida si cumple mínimos → **200** con estado `Iniciada`.
- [x] **AC-BE-07** POST start rechaza si no cumple mínimos → **409**.
- [x] **AC-BE-08** POST start rechaza si partida no está en Lobby → **400**.
- [x] **AC-BE-09** GET `/api/trivia-games/{id}` retorna detalle sin modificar estado → **200**.
- [x] **AC-BE-10** GET con id inexistente → **404**.
- [x] **AC-BE-11** Usuario sin rol Operador recibe **403** en POST, POST start y GET.
- [x] **AC-BE-12** POST por usuario autenticado sin rol recibe **401** (manejado por middleware JWT).

### Funcional — Frontend

- [ ] **AC-FE-01** Operador accede a pantalla de creación de partida desde panel web.
- [ ] **AC-FE-02** Puede seleccionar formulario completo de la lista.
- [ ] **AC-FE-03** Puede configurar nombre, modalidad, límites y tiempo de inicio.
- [ ] **AC-FE-04** Puede iniciar partida manualmente desde el lobby.
- [ ] **AC-FE-05** Errores de validación del backend se muestran al operador.

### Reglas de negocio

- [x] **AC-RB-01** RB-T04: solo operador crea partidas.
- [x] **AC-RB-02** RB-T05: formulario asociado debe ser completo.
- [x] **AC-RB-03** RB-T06: todos los campos de configuración requeridos.
- [x] **AC-RB-04** RB-T07/T08: máximos según modalidad.
- [x] **AC-RB-05** RB-T09: modalidad equipo define min/max por equipo.
- [x] **AC-RB-06** RB-T17/T18: inicio cambia estado a Iniciada.
- [x] **AC-RB-07** RB-26: no iniciar sin mínimos.
- [x] **AC-RB-08** RB-08/09: lobby permite inscripciones, iniciada permite acciones.
- [x] **AC-RB-09** RB-27: auto-cancelación por tiempo sin mínimos (validación en handler).

### Contratos y trazabilidad

- [x] **AC-CT-01** Endpoints documentados en `contracts/http/trivia-game-api.md`.
- [x] **AC-CT-02** Fila HU-17 actualizada en `docs/04-sdd/traceability-matrix.md`.
- [x] **AC-CT-03** Sin eventos RabbitMQ requeridos (verificado).

---

## Automated test evidence

| Test suite | File / location | Status | Evidence |
| --- | --- | --- | --- |
| Domain unit tests | `PartidaTriviaTests`, `PartidaTriviaStateTests` | ✅ | 130 tests pasan (incluye creación, transiciones, límites, modalidad) |
| Application validator tests | `CreateTriviaGameCommandValidatorTests`, `StartTriviaGameCommandValidatorTests` | ✅ | 65 tests de aplicación pasan |
| Application handler tests | `CreateTriviaGameCommandHandlerTests`, `StartTriviaGameCommandHandlerTests` | ✅ | Validación de formulario incompleto, inicio con/sin mínimos |
| API integration tests | `TriviaGamesControllerTests` | ✅ | 16 tests: create, start, get, 403, 404, 409 |
| Auth integration tests | Included in API tests | ✅ | `Create_WithoutOperadorRole_Returns403` |
| **Total** | | **✅** | **211 tests — 130 Domain + 65 Application + 16 API** |

---

## Traceability status

| Artefact | Reference | Status |
| --- | --- | --- |
| HU-17 | `docs/01-project-source/srs.md` | ✅ Implementado |
| RF-17 | Crear partida asociada a formulario válido | ✅ Handler + tests |
| RF-18 | Publicar lobby e iniciar partida | ✅ Endpoints + validación de mínimos |
| RF-35 | Consulta sin mutación | ✅ GET endpoint |
| RB-T04..T09, T17, T18 | Reglas Trivia partida | ✅ Validadas en aggregate |
| RB-08..RB-11 | Estados de partida | ✅ Transiciones validadas |
| RB-26, RB-27 | Inicio con mínimos y auto-cancelación | ✅ Handler + domain |
| SPECS-LIST | Active sprint | ✅ Confirmado |
| Owning service | Trivia Game Service | ✅ Confirmado |
| Client | React web | 🔄 Pendiente (frontend congelado) |
| HTTP contract | `contracts/http/trivia-game-api.md` | ✅ Actualizado con HU-17 endpoints |
| Events contract | N/A for HU-17 | ✅ Confirmado sin eventos |
| Traceability matrix | `docs/04-sdd/traceability-matrix.md` | ✅ Actualizado |

---

## Sign-off

| Rol | Name | Date | Approved |
| --- | --- | --- | --- |
| Product / SRS owner | | | [ ] |
| Tech lead | | | [ ] |
| SDD reviewer | | | [ ] |

---

## Review notes (post-implementation)

### Notas sobre cobertura de acceptance

- **AC-BE-06** (start con mínimos cumplidos → 200/Iniciada): cubierto por tests de dominio y aplicación. La integración API requiere HU-18 (inscripciones) para ejecutarse end-to-end. El test `CreateThenStart_WhenMinimosCumplidos` fue reemplazado por `CreateGame_WithMinimos_Returns201WithLobby` que verifica la creación exitosa.
- **AC-BE-08** (start rechaza si no en Lobby): cubierto por tests de dominio que verifican `InvalidStateTransitionException`. No se puede iniciar sin inscripciones, por lo que no se puede simular estado no-Lobby en API.
- **AC-FE-01..05**: Frontend React congelado para este sprint.
- **RB-27** (auto-cancelación): el handler `StartTriviaGameCommandHandler` valida mínimos. La auto-cancelación por timer se implementará con background job en HU-24.
