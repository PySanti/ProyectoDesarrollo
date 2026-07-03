# Informe de Auditoría — SP-3c → SP-3e-4 · Conformidad con doctrina y CLAUDE.md

> Fecha: 2026-07-02/03. Método: subagent-driven, 5 auditores read-only por clúster (§5 del plan). Plan: `docs/04-sdd/auditorias/2026-07-02-auditoria-conformidad-sp3c-3e4.md`. Rango auditado: `c23ebf5..81c57f1` (138 commits, rama `feature/code-migration-SP-3`). El código en HEAD al ejecutar la auditoría (`18a43a6`, rama `feature/sp-3-audit`) es **idéntico** al de `81c57f1` en `services/`, `gateway/` y `contracts/` (diff = 0; solo difieren docs de auditoría/traceability commiteados por el responsable).

## Veredicto global

### 🟡 CONFORME CON RESERVAS — **0 Critical · 4 Important · 3 Minor**

Doce de quince dimensiones pasan limpias. El **código** conforma con la doctrina en todo lo sustantivo: límites duros de servicio, invariantes de dominio (Individual y Equipo), CQRS/TimeProvider, seam de eventos save→publish, realtime server-side/anti-leak, concurrencia xmin, migraciones aditivas, y los diferimientos del §2 están todos correctamente diferidos (ninguno a medio construir). Ningún hallazgo es de comportamiento en runtime.

Las reservas son **4 Important de superficie acotada**: dos de drift documental en el contrato canónico de eventos (riesgo directo para SP-4), un gap de cobertura de la regla graduada de controller tests (1 de 21 acciones), y una tensión spec-vs-doctrina escalada (hub SignalR bypasea MediatR **por mandato del spec aprobado SP-3f-2** — decisión del responsable, no defecto unilateral).

**Suites re-ejecutadas en HEAD por A5 (no se confió en el ledger): Unit 323/323 · Integration 28/28 · Contract 48/48 — 100% verdes, baseline confirmado exacto.**

## Veredictos por dimensión

| Dim | Área | Veredicto | C/I/m | Clúster |
|---|---|---|---|---|
| D1 | Límites de servicio | PASS | 0/0/0 | A1 |
| D2 | Estructura graduada | PASS con escalación | 0/1/0 | A1 |
| D3 | Invariantes de dominio (runtime Individual) | PASS | 0/0/1 | A2 |
| D4 | CQRS / MediatR / TimeProvider | PASS | 0/0/0 | A1 |
| D5 | ADR-0010 / R1 | PASS | 0/0/0 | A2 |
| D6 | Código ↔ contratos (eventos) | **FAIL** | 0/2/0 | A4 |
| D6 | Código ↔ contratos (HTTP) | PASS | 0/0/1 | A5 |
| D7 | Seam de eventos (save→publish) | PASS | 0/0/0 | A4 |
| D8 | Persistencia / migraciones | PASS | 0/0/0 | A5 |
| D9 | Pruebas | **FAIL** | 0/1/0 | A5 |
| D10 | Integridad SDD | PASS | 0/0/1 | A5 |
| D11 | Traceability | PASS | 0/0/0 | A5 |
| D12 | Diferimientos | PASS | 0/0/0 | A5 |
| D13 | Doctrina modalidad Equipo | PASS | 0/0/0 | A3 |
| D14 | Realtime / SignalR | PASS | 0/0/0 | A4 |
| D15 | Concurrencia + scheduler | PASS | 0/0/0 | A2 |

## Hallazgos

### Important

**I-1 · D6-eventos · Contrato de eventos sin los campos Equipo del slice-E** — `contracts/events/operaciones-sesion-events.md:81-194` vs `Application/Interfaces/TriviaRuntimeEvents.cs:3-20` y `BdtRuntimeEvents.cs:3-13`. Los 6 eventos que ganaron `EquipoId`/`GanadorEquipoId` en slice-E (`RespuestaTriviaValidada`, `PuntajeTriviaIncrementado`, `PreguntaTriviaCerrada`, `TesoroQRValidado`, `EtapaBDTGanada`, `EtapaBDTCerrada`) siguen con el shape pre-slice-E en el doc canónico: ningún commit E lo tocó (última edición: SP-3c/3d). El plan SP-3e-3 pedía añadir los campos "donde el doc ya describa" los eventos, pero su tarea D4 solo alcanzó el contrato HTTP. Ningún ContractTest referencia este archivo, así que nada guarda contra el drift. **Fuente violada:** CLAUDE.md "`contracts/` — source of truth for … event contracts". **Por qué Important y no Critical:** ningún cliente runtime roto hoy (broker diferido a pre-SP-4), pero SP-4 Puntuaciones es el consumidor nombrado de exactamente esos campos — un implementador que lea solo el doc canónico no vería `EquipoId`.

**I-2 · D6-eventos · Registry de eventos sin 3 eventos del seam** — `contracts/events/operaciones-sesion-events.md:13-27`. `PistaEnviada`, `ConvocatoriaCreada`, `ConvocatoriaRespondida` son miembros de primera clase de `ISesionEventsPublisher` (16 métodos, implementados por NoOp/SignalR/Composite con destino declarado al broker futuro) y no aparecen en la tabla registry. CLAUDE.md §"Events & messaging" nombra explícitamente los "team/invitation/convocatoria events" como parte del set RabbitMQ. Documentados solo en la prosa realtime del contrato HTTP (doc SignalR, no el registry cross-service). **Fuente violada:** CLAUDE.md Events & messaging + designación source-of-truth de `contracts/`.

**I-3 · D9 · `ObtenerEtapaActual` sin controller unit test** — `Api/Controllers/SesionesController.cs:139-141`. 20 de 21 acciones del controller tienen unit test con `ISender` mockeado; `ObtenerEtapaActual` (GET `.../etapa-actual`, introducido en `42d50fe`, SP-3d T15) es la única ausente en los 6 archivos `SesionesController*Tests.cs` — sus hermanos del mismo commit (`AvanzarEtapa`, `ValidarTesoro`) sí los tienen. **Fuente violada:** CLAUDE.md regla graduada "Every controller has unit tests" (non-negotiable). **Mitigante (por eso Important y no Critical):** la ruta está verificada end-to-end por `ContractTests/BdtRuntimeEndpointsTests.cs:74-88`; falta solo la puerta unitaria aislada.

**I-4 · D2 · ⚖️ ESCALADO — Hub SignalR bypasea MediatR, por mandato del spec aprobado** — `Api/Realtime/SesionHub.cs:44-59`. `SuscribirAPartida` inyecta `ISesionPartidaRepository` (puerto de dominio) directo en la capa Api, impone el invariante de pertenencia inline (`HubException`, fuera del middleware centralizado de excepciones) y recorre entidades (`Inscripciones`/`Convocatorias`) para resolver el grupo `equipo:{id}` — duplicando lo que `ObtenerMiSesionQueryHandler` ya expone vía MediatR, y en la misma capa donde `MantenimientoSesionesWorker` demuestra que `ISender` está disponible. **Pero** el design aprobado SP-3f-2 (`docs/superpowers/specs/2026-06-30-sp3f2-push-tiempo-real-signalr-design.md:157-158`) instruye textualmente: *"SuscribirAPartida valida pertenencia: … (reutiliza la consulta de participación existente)"*. **Tensión de autoridades:** CLAUDE.md "Structure & coding rules" (precedencia #2: capa Api despacha por MediatR, sin lógica de negocio) vs spec SDD del rango (#6). Per regla de adjudicación del plan (§3), se reporta como escalación, no se suprime ni se cuenta como defecto unilateral: **decisión del responsable** — (a) sancionar la excepción con nota/ADR (hubs resuelven membresía de grupos vía repositorio de lectura, racional: handshake realtime, no comando de negocio), o (b) refactorizar el hub a despachar una query MediatR. Si se elige (a) y se documenta, este hallazgo se cierra sin cambio de código.

### Minor

**m-1 · D3 · Borde de deadline inconsistente en Trivia** — `Domain/Entities/PreguntaSnapshot.cs:69` usa `>` estricto para rechazar respuesta tardía; `SesionPartida.cs:220,275` usa `>=` para clasificar la pregunta como vencida. En el tick exacto del deadline, `RegistrarRespuesta` aceptaría lo que el barrido considera vencido. Impacto práctico despreciable (coincidencia de tick exacto entre HTTP y scheduler); ningún test ejercita ese borde. Fix: unificar el comparador.

**m-2 · D6-http · Línea "Status" obsoleta** — `contracts/http/operaciones-sesion-api.md:5` aún dice "SP-3a endpoints registered…". El registry inmediatamente debajo está completo y correcto (21 endpoints y 16 DTOs verificados 1:1 contra código, cero drift). Fix: actualizar la línea.

**m-3 · D10 · Colisión de identificador de slice "SP-3e"** — `2026-06-29-sp3e-reconexion-design.md` (reconexión/mi-sesión) vs los `sp3e1..4` del slice-E de Equipo. Ambos plenamente documentados (no hay hueco de cobertura); nada cruza-referencia la colisión. Las filas de traceability sí distinguen (fila 14 "(SP-3e)" vs filas 20-23 "(SP-3e-1..4)"), por eso Minor. Fix: nota de desambiguación en ambos specs o en traceability.

## Evidencia clave por clúster

### A1 — D1/D2/D4 (arquitectura, estructura, CQRS)
- **D1:** única DB propia (`OperacionesSesionDbContext` → `umbral_operaciones_sesion`); los 2 HTTP clients cross-service (`PartidasConfigHttpClient`, `IdentityEquipoHttpClient`) son GET-only y su patrón directo-a-puerto está documentado en contrato (`operaciones-sesion-api.md:63,70`); cero acceso a DB ajena; gateway YARP puro (4 clusters, política por rol, sin `PathRemovePrefix`).
- **D2:** `Application/` con carpetas exactas de la regla graduada, sin extras; `Infrastructure/{Persistence,Services}`; `Program.cs` sin controllers inline; `SesionesController` (21 endpoints) puro `_mediator.Send` + `ObtenerParticipanteId()`; interfaces de repositorio en `Domain/Abstractions/Persistence`.
- **D4:** cero `DateTime.Now/UtcNow` en `src`; 13 handlers con `_timeProvider`; dominio recibe `now` como parámetro; 6 query handlers sin `IUnitOfWork` ni mutación.

### A2 — D3/D5/D15 (dominio Individual, R1, concurrencia)
- **D3:** cada invariante con código **y** test — Trivia: 1 intento por participante, primera-correcta cierra global, RF-22 auto-activación (`SesionPartida.cs:211`), puntaje solo en correcta, rechazos 4xx-familia; BDT: reintentos libres sin sello, QR decode↔texto, `Puntaje` por etapa solo al ganador, cero código que ranquee por conteo de etapas (conforme `bdt-ranking-clarification.md`); exactamente un juego `Activo`; barridos idempotentes gated por `HuboCambio`. Spot-check no-regresión SP-3a/3b: limpio.
- **D5:** estado transient solo; sin escrituras a Partidas.
- **D15:** xmin `IsRowVersion` en el aggregate root, `DbUpdateConcurrencyException`→409 (middleware + tests dedicados), scheduler idempotente/`NoCorresponde` sin save/publish, `TimeProvider` end-to-end. El diferimiento de scope compartido de DbContext por tick verificado como registrado (ledger:460 + spec 3f-1 §61).

### A3 — D13 (doctrina Equipo) — 0/0/0
- Participación única (individual XOR convocatoria aceptada) impuesta en dominio + repos cross-partida (solo DB propia); snapshot de miembros congelado al preinscribir; BR-G09 intra y cross-partida con tests; Trivia Equipo dedup-por-equipo y sella al equipo entero con ganador dual; BDT Equipo reintentos ilimitados por diseño (divergencia spec-aprobada); pistas Equipo con XOR 400 / 409 Individual / 404 sin inscripción y grupo server-side desde JWT; invariante `EquipoId == null ⇔ Individual` estructural en dominio→eventos→migraciones (columnas nullable aditivas). Límite duro con Identity intacto: cero entidades de membresía en Operaciones (grep repo-wide = 0 hits). Los 7 diferimientos del ledger verificados honestos, ninguno a medio construir.

### A4 — D6-eventos/D7/D14 (eventos, seam, realtime)
- **D7:** `ISesionEventsPublisher` = 16 métodos; 3 impls en `src` completas; los 12 handlers que emiten guardan (`SaveChangesAsync`) estrictamente antes de publicar; `EnviarPistaCommandHandler` sin save por diseño documentado; caminos `NoCorresponde`/excepción saltan ambos.
- **D14:** membresía de grupos 100% server-side desde JWT `sub` (`SesionHub.cs:38-59`); `UbicacionActualizada` → solo `operador:partida:{id}` (BR-B07); `PistaEnviada` → destino único participante xor equipo (BR-B06); ningún payload con texto de preguntas/opciones/QR ni puntos salvo `PistaEnviadaPayload.Texto` (excepción documentada); no-ops de SP-3f-2 documentados en código y contrato; hub alcanzable vía gateway bajo prefijo sin strip (`Program.cs:116` + `appsettings.json` del gateway).
- **D6-eventos:** FAIL — hallazgos I-1, I-2 arriba.

### A5 — D6-http/D8/D9/D10-12 (HTTP, persistencia, pruebas, SDD)
- **D6-http:** 21 endpoints verificados 1:1 (verbo/ruta/status/errores) y 16 DTOs campo a campo contra `Application/DTOs/*.cs` — cero drift (solo m-2). La brecha coarse-role del prefijo es diferimiento spec-aprobado de SP-3a a SP-5, reconfirmado en ledger — no hallazgo per charter.
- **D8:** 6 migraciones del rango aditivas-only, `Down` reversibles, `SP3f1ConcurrencyToken` no-op DDL intencional (xmin = columna de sistema); Designer del HEAD ≡ ModelSnapshot (0 drift); `HasColumnName` lowercase consistente.
- **D9:** re-ejecución real en HEAD: **Unit 323/323, Integration 28/28, Contract 48/48**. FAIL solo por I-3.
- **D10-12:** 12 slices del rango, todos con spec+plan+veredicto APPROVED en ledger; 3 filas de traceability spot-checkeadas contra `git log` (rangos reales); todos los diferimientos del §2 con registro durable y referencia de línea.

## Adjudicación (dedup y severidades)

- Sin solapes entre clústeres: los 7 hallazgos son disjuntos.
- **I-3 no asciende a Critical** pese a que §6 lista "regla graduada rota": la regla está cumplida en 20/21 acciones y la ruta faltante tiene verificación e2e por contract test — es gap parcial de cobertura, no regla estructuralmente rota.
- **I-4 se contabiliza como Important mientras no se decida la escalación.** Si el responsable sanciona el spec con nota/ADR, se cierra sin tocar código y el conteo pasa a 0C/3I.
- **I-1 + I-2 son el mismo síntoma** (contrato de eventos desatendido durante slice-E) con una sola remediación doc-only, pero se cuentan separados por violar cláusulas distintas.

## Remediación propuesta (mini-slice, decisión del responsable)

| # | Hallazgo | Acción | Esfuerzo |
|---|---|---|---|
| 1 | I-1 + I-2 | Actualizar `contracts/events/operaciones-sesion-events.md`: añadir `equipoId?`/`ganadorEquipoId?` a los 6 samples + registrar `PistaEnviada`/`ConvocatoriaCreada`/`ConvocatoriaRespondida` en la tabla. **Hacerlo antes de SP-4** (Puntuaciones consume exactamente esto). | Doc-only, ~1 h |
| 2 | I-3 | Controller unit test para `ObtenerEtapaActual` (patrón de sus hermanos en `SesionesControllerBdtTests.cs`). | 1 test |
| 3 | I-4 | Decidir: (a) nota/ADR sancionando el patrón hub-repositorio (cierra sin código), o (b) refactor a query MediatR. | Decisión + nota, o refactor pequeño |
| 4 | m-1..m-3 | Unificar comparador de deadline; actualizar línea Status; nota de desambiguación SP-3e. Candidatos al mini-slice de minors ya acumulados en ledger. | One-liners |

## Próximos pasos (decisión del responsable)

- **Nada bloquea el código:** 0 Critical, suites 100% verdes, invariantes completos. Las reservas son documentales/de cobertura y una decisión de gobernanza (I-4).
- Ruta recomendada: mini-slice de remediación (ítems 1-2 + decisión de 3) **antes de SP-4**, porque I-1/I-2 le mienten al implementador de Puntuaciones justo en los campos que necesita.
- Con ítems 1-3 resueltos, el rango queda CONFORME pleno.

## Remediación aplicada (SP-3h, 2026-07-03)

| Hallazgo | Resolución | Commit |
|---|---|---|
| I-1, I-2, m-2 | Contrato de eventos alineado a slice-E (6 samples + 3 eventos registrados) + Status HTTP | c6ac2a5 |
| I-4, m-3 | ADR-0011 sanciona membresía de grupos vía repositorio en hubs + desambiguación SP-3e | 6baab6c |
| m-1, ledger | Borde de deadline Trivia inclusivo (`>=`) + `ParaEquipo` en cancelación | a64132d |
| ledger | Mi-sesión prefiere convocatoria Aceptada | ed89296 |
| I-3, ledger | Controller test `ObtenerEtapaActual` (21/21) + estilo `.Group` | 9295091 |

**Estado post-remediación: CONFORME — 0 Critical · 0 Important · 0 Minor** (del alcance de este informe). Suites en HEAD del slice: Unit 327 / Integration 28 / Contract 48.
