# Plan de Auditoría — SP-3c → SP-3e-4 · Conformidad con doctrina y CLAUDE.md

> Estado: **plan** (metodología, sin hallazgos). Fecha: 2026-07-02. Alcance: incremental sobre la auditoría SP-3a/3b (2026-06-27, CONFORME 0/0/0). Rama: `feature/code-migration-SP-3`, rango `c23ebf5..81c57f1` (138 commits). Read-only: no muta código, no commitea. Este documento y su informe viven en `docs/04-sdd/auditorias/` y NUNCA se stagean (carve-out vigente).

## 1. Contexto y propósito

Desde el informe SP-3a/3b se implementaron, vía subagent-driven-development con review final APPROVED cada uno: SP-3c (runtime Trivia Individual), SP-3d (runtime BDT Individual), SP-3f-1 (concurrencia xmin + scheduler inicio automático), SP-3f-2 (push SignalR), SP-3f-3 (geolocalización), SP-3f-4 (pistas), SP-3g (hub bajo prefijo gateway), y el slice-E completo de modalidad Equipo (SP-3e-1 participación, SP-3e-2 Trivia, SP-3e-3 BDT, SP-3e-4 pistas). Esta auditoría verifica —contra las fuentes de autoridad, no contra la memoria del implementador— que todo ello conforma con doctrina, documentación y CLAUDE.md.

**Salida:** informe con veredicto por dimensión + tabla de hallazgos (Critical/Important/Minor, con evidencia `archivo:línea` y fuente violada) + veredicto global + lista de remediación propuesta (solo informe; los fixes serían otro slice, decisión del responsable).

## 2. Alcance

**Dentro:**
- `services/operaciones-sesion/src/**` y `tests/**` (todo lo tocado en el rango).
- `contracts/http/operaciones-sesion-api.md`, `contracts/events/operaciones-sesion-events.md`.
- `gateway/**` solo en lo que SP-3g tocó (routing del hub, prefijo `operaciones-sesion`).
- `docs/04-sdd/traceability-matrix.md` (working tree), specs/plans SP-3c..3e-4 en `docs/superpowers/{specs,plans}/`, ledger `.git/sdd/progress.md`.
- Spot-check de no-regresión SP-3a/3b: invariantes de D3 previo siguen impuestos y testeados (sin re-auditar en profundidad).

**Fuera (diferimientos documentados — verificar que estén correctamente diferidos, NO marcarlos como huecos):**
- Broker RabbitMQ real (dual-write) → slice previo a SP-4. Seam events es el sustituto deliberado.
- Scoring/ranking (Puntuaciones) → SP-4. Operaciones solo emite eventos con `Puntaje`/`EquipoId`.
- Persistencia de pistas (BR-B06 "recorded") → diferido desde SP-3f-4 (event-only).
- Replay de `ConvocatoriaCreada`; audit de ubicación (broker).
- Minors diferidos por reviews finales (registrados en ledger): mi-sesión preferir Aceptada; test timeout Equipo BDT; xmin child-only; `CancelarInscripcionEquipo`→`ParaEquipo`; `g.Item2` vs `.Group`; lobby `Guid.Empty` en Equipo; índice `inscripciones.equipoid`.
- Clientes móvil/web para modalidad Equipo (no cableados aún).

## 3. Fuentes de autoridad (orden de precedencia)

| # | Fuente | Ruta | Qué gobierna |
|---|---|---|---|
| 1 | AGENTS.md | `/AGENTS.md` | Ruleset maestro |
| 2 | CLAUDE.md | `/CLAUDE.md` | Topología, límites duros, estructura graduada, dominio esencial, ranking |
| 3 | ADR-0009 / ADR-0010 | `docs/05-decisions/` | Slugs/puertos/DB; estado runtime solo en Operaciones (R1) |
| 4 | Business rules / SRS | `docs/02-project-context/business-rules.md`, `srs-summary.md`, `bdt-ranking-clarification.md` | BR-*/RF-* (BR-B06 pistas, BR-B07 geoloc, BR-G09, RF-22, reglas Equipo) |
| 5 | Contratos | `contracts/http/operaciones-sesion-api.md`, `contracts/events/operaciones-sesion-events.md` | Forma canónica HTTP/eventos/realtime |
| 6 | SDD del rango | `docs/superpowers/specs/2026-06-2[7-9]*.md`, `2026-07-0[1-2]*.md` + plans homónimos | Decisiones aprobadas por slice |
| 7 | Traceability + ledger | `docs/04-sdd/traceability-matrix.md`, `.git/sdd/progress.md` | Estado declarado y diferimientos |

Regla de adjudicación: hallazgo que contradice lo que un spec/plan aprobado exige explícitamente → se escala como decisión del responsable (hallazgo + texto del spec, cuál gobierna), nunca se descarta en silencio.

## 4. Dimensiones

Heredadas de la auditoría SP-3a/3b: **D1** límites de servicio · **D2** estructura graduada · **D3** invariantes de dominio · **D4** CQRS/MediatR/TimeProvider · **D5** ADR-0010/R1 · **D6** código↔contratos · **D7** seam de eventos · **D8** persistencia/migraciones · **D9** pruebas · **D10** integridad SDD · **D11** traceability · **D12** diferimientos.

Nuevas para este rango:
- **D13 — Doctrina modalidad Equipo:** participación única por partida (individual XOR convocatoria aceptada), convocatorias solo afectan la partida (nunca membresía del equipo), snapshot de miembros al preinscribir, BR-G09 intra-partida y cross-partida, 1 respuesta por equipo en Trivia vs reintentos libres en BDT (ambas contra texto SRS/spec aprobado), ganador dual (autor + equipo) consistente en dominio→eventos→persistencia, identidad dual `EquipoId == null ⇔ Individual` sin excepciones.
- **D14 — Realtime/SignalR:** grupos (`partida:`, `operador:partida:`, `participante:`, `equipo:`) con membresía decidida server-side desde JWT; anti-leak (payloads sin texto de preguntas/opciones/QR, sin puntos/ranking); BR-B07 (ubicación SOLO al grupo operador); BR-B06 (pista solo al destino); no-ops deliberados de SP-3f-2 documentados; hub alcanzable vía gateway bajo prefijo `operaciones-sesion` (SP-3g) sin `PathRemovePrefix`.
- **D15 — Concurrencia y scheduler:** token xmin en el aggregate (SP-3f-1), mapeo `DbUpdateConcurrencyException`→409, scheduler de inicio automático idempotente y gated por tiempo, `TimeProvider` end-to-end.

Cada dimensión se verifica con el patrón del plan previo: qué verificar · evidencia (archivos) · método (lectura + grep + cruce con tests) · criterio de aprobación (cada invariante con código que lo impone Y test que lo verifica).

## 5. Método — 5 clústeres subagent-driven (read-only)

| Clúster | Dimensiones | Foco |
|---|---|---|
| **A1** Arquitectura/estructura/CQRS | D1, D2, D4 | Límites con SignalR dentro del servicio; `Application/` exacta tras ~30 archivos nuevos; controllers puros (21 endpoints); sin relojes inline; ValidationBehavior en pipeline |
| **A2** Dominio runtime Individual | D3, D5, D15 | Invariantes Trivia (1 intento, primera correcta cierra global, RF-22 auto-activa, timeout) y BDT (reintentos libres, QR decode↔texto esperado, puntaje por etapa per bdt-ranking-clarification, timeout); `CerrarActividadVencida`/barrido; scheduler; xmin; ADR-0010; spot-check no-regresión SP-3a/3b |
| **A3** Doctrina Equipo | D13 | Slice-E completo contra SRS/BRs/specs aprobados |
| **A4** Eventos y realtime | D6-eventos, D7, D14 | Seam completo (contar métodos, save→publish en TODOS los handlers que emiten), records↔`contracts/events`, payloads↔doc realtime, grupos/anti-leak/BR-B06/B07, SP-3g routing |
| **A5** Contratos HTTP/persistencia/pruebas/SDD | D6-http, D8, D9, D10-D12 | 21 endpoints↔contrato línea a línea (verbo/ruta/rol/códigos/DTOs campo a campo); 6 migraciones nuevas aditivas + Designer≡Snapshot; pirámide + **re-ejecutar las 3 suites en HEAD** (baseline declarado: Unit 323 / Integration 28 / Contract 48 — no confiar en el ledger); specs/plans/traceability/ledger íntegros; diferimientos del §2 correctamente anotados |

Reglas para cada auditor:
- **Read-only HARD:** prohibido editar, stagear, commitear, `git checkout/restore/clean/stash/reset`. Solo lectura, grep y (solo A5) `dotnet test`.
- Todo hallazgo: severidad + evidencia `archivo:línea` + fuente de autoridad violada + dimensión. Sin evidencia → no es hallazgo.
- No confiar en specs/reviews previos como prueba de conformidad: son insumo, la prueba es el código en HEAD.
- Los diferimientos del §2 no son hallazgos; SÍ lo es un diferimiento a medio construir (código gameplay parcial, puerto medio-cableado).

Orquestación: los 5 auditores corren en paralelo (sonnet, general-purpose). El controller adjudica (dedup, severidad final, escalamientos) y sintetiza el informe `2026-07-02-informe-conformidad-sp3c-3e4.md` con: veredicto global, tabla por dimensión (D1-D15, C/I/m), hallazgos con evidencia, suites re-ejecutadas, y lista de remediación propuesta.

## 6. Criterio de veredicto global

- **CONFORME:** 0 Critical y 0 Important (Minors se listan para remediación futura).
- **CONFORME CON RESERVAS:** 0 Critical, ≥1 Important con remediación acotada.
- **NO CONFORME:** ≥1 Critical (violación de límite duro, invariante de dominio sin imponer, drift de contrato que rompe clientes, o regla graduada rota).
