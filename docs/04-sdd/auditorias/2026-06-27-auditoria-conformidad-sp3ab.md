# Plan de Auditoría — SP-3a / SP-3b · Conformidad con doctrina y CLAUDE.md

> Estado: **plan** (metodología, sin hallazgos). Fecha: 2026-06-27. Slice auditado: Operaciones de Sesión, sub-slices SP-3a (publicación→Lobby + inscripciones Individual) y SP-3b (inicio manual/automático + secuenciación de juegos).

## 1. Contexto y propósito

SP-3a y SP-3b se implementaron vía subagent-driven-development; ambos quedan marcados *Implemented* con suite verde y revisión final "READY TO MERGE". Antes de continuar a SP-3c+ y antes de que el equipo aplaste cada slice en un commit "Fase N" sobre `develop`, se requiere una **auditoría independiente de conformidad** que verifique —contra las fuentes de autoridad, no contra la memoria del implementador— que ambos slices cumplen:

- la topología de cuatro servicios y los **límites duros** (AGENTS.md / CLAUDE.md);
- la **estructura graduada** no negociable;
- los **invariantes de dominio** del ciclo de vida Partida/Juego;
- **ADR-0010 / R1** (estado runtime sólo en Operaciones);
- la conformidad **código ↔ contratos** (HTTP y eventos);
- la integridad del **flujo SDD** (spec/design/tasks/acceptance, traceability, ledger, diferimientos).

**Salida:** un informe de auditoría con veredicto por dimensión + tabla de hallazgos (Critical/Important/Minor con evidencia y ruta) + veredicto global, y —si aplica— una lista de tareas de remediación. La auditoría es **read-only**; no muta código, no commitea, no mergea.

## 2. Alcance

**Dentro de alcance**
- `services/operaciones-sesion/src/**` (Domain, Application, Infrastructure, Api) y `services/operaciones-sesion/tests/**`.
- `contracts/http/operaciones-sesion-api.md` y `contracts/events/operaciones-sesion-events.md`.
- `docs/04-sdd/traceability-matrix.md`; specs/plans en `docs/superpowers/{specs,plans}/` (SP-3a, SP-3b); ledger `.git/sdd/progress.md`.
- `docs/05-decisions/ADR-0010-runtime-estado-en-operaciones.md` (y ADR-0009 para slugs/puertos/DB).

**Fuera de alcance (diferimientos documentados — NO marcar como huecos).** La auditoría sólo verifica que estén **correctamente diferidos** (puerto No-Op presente y no medio-construido; sin código gameplay parcial), no que existan:
- Equipo + convocatorias → SP-3a-E
- Gameplay Trivia (Pregunta, RespuestaTrivia) → SP-3c
- Gameplay BDT (EtapaBDT, TesoroQR, Pista, geolocalización) → SP-3d
- Reconexión / recuperación de estado transitorio → SP-3e
- SignalR/WebSockets + scheduler de inicio automático → SP-3f
- Backbone real RabbitMQ (exchange, colas, routing, idempotencia) → su slice previo a SP-4

## 3. Fuentes de autoridad (orden de precedencia)

| # | Fuente | Ruta | Qué gobierna |
|---|---|---|---|
| 1 | AGENTS.md | `/AGENTS.md` | Ruleset maestro: ownership, límites, reglas graduadas |
| 2 | CLAUDE.md | `/CLAUDE.md` | Esenciales operativos, modelo de dominio, estructura graduada, ranking |
| 3 | ADR-0010 | `docs/05-decisions/ADR-0010-runtime-estado-en-operaciones.md` | R1: estado runtime en `SesionPartida.EstadoSesion`; `EstadoPartida` de Partidas queda `null` |
| 4 | ADR-0009 | `docs/05-decisions/ADR-0009-*.md` | slug `operaciones-sesion`, namespace `Umbral.OperacionesSesion.*`, puerto 5020, DB `umbral_operaciones_sesion`, ruta gateway |
| 5 | Contratos | `contracts/http/operaciones-sesion-api.md`, `contracts/events/operaciones-sesion-events.md` | Forma canónica de endpoints y eventos |
| 6 | SDD SP-3a/3b | `docs/superpowers/specs/2026-06-26-sp3{a,b}-*-design.md`, `.../plans/2026-06-26-sp3{a,b}-*.md` | Decisiones bloqueadas (§2), modelo (§4), tareas |
| 7 | Traceability | `docs/04-sdd/traceability-matrix.md` | Estado declarado y diferimientos |

Regla de adjudicación: si un hallazgo contradice lo que **el plan/spec exige explícitamente**, no se descarta ni se "arregla" en silencio — se escala como decisión del responsable (cita hallazgo + texto del plan, se pregunta cuál gobierna).

## 4. Dimensiones de auditoría (los checklists)

Cada dimensión: **qué verificar · evidencia (archivos) · método · criterio de aprobación**.

### D1 — Arquitectura y límites de servicio
- Operaciones **nunca** lee/escribe la DB de Identity/Partidas/Puntuaciones. Verificar que el único acceso cruzado es HTTP síncrono a Partidas (`PartidasConfigHttpClient`) y que no hay `DbContext`/connection string ajeno.
- Cross-service async sólo vía puerto de eventos (`ISesionEventsPublisher`); real-time (SignalR) ausente y diferido a 3f, no medio-cableado.
- Evidencia: `Infrastructure/Services/PartidasConfigHttpClient.cs`, `Infrastructure/Persistence/*`, `appsettings*`, `Program.cs`.
- Método: grep de connection strings y `UseNpgsql`/`DbContext`; leer cliente HTTP.
- Aprobación: 0 accesos a DB ajena; handoff config sólo HTTP; sin estado duplicado.

### D2 — Estructura graduada (no negociable)
- `Application/` contiene **exactamente** {Commands, Queries, Interfaces, Validators, DTOs, Handlers, Handlers/Commands, Handlers/Queries, Exceptions} + `DependencyInjection.cs`/`ValidationBehavior.cs` raíz. Sin carpetas por-feature, sin extras.
- `Api/Controllers/` presente; `Program.cs` sólo `MapControllers` (no registra controllers inline, no minimal-API de negocio).
- Controllers heredan `ControllerBase` nativo, despachan vía `_mediator.Send(...)`, **sin lógica de negocio** (sólo extracción de claim de identidad permitida).
- `Domain/` tiene entidades, enums, exceptions e **interfaces de repositorio** (`Abstractions/Persistence`); `Infrastructure/` tiene `Persistence/` + `Services/`.
- Middleware centralizado `ExceptionHandlingMiddleware` registrado.
- Evidencia: árbol de los 4 proyectos; `Api/Program.cs`; `Api/Controllers/*`; `Api/Middleware/ExceptionHandlingMiddleware.cs`.
- Método: listar árbol; leer `Program.cs` y cada controller.
- Aprobación: cero desviaciones de carpeta; controllers limpios; Program.cs sólo enruta.

### D3 — Modelo de dominio e invariantes (máquinas de estado)
- `EstadoSesion ∈ {Lobby, Iniciada, Cancelada, Terminada}`; `EstadoJuego ∈ {Pendiente, Activo, Finalizado}`.
- Publicabilidad: ≥1 juego, `Orden` contiguo desde 1.
- Inscripción: una por partida; **una participación activa a la vez** (consulta a la **propia** DB, sin leer ajenas); guardas de cupo/modalidad/estado-Lobby.
- Inicio: minimos → si no se alcanzan, **auto-cancelación = 200 + `Cancelada`** (valor de retorno, NO excepción 4xx); si se alcanzan, `Iniciada` + primer juego `Activo` por `Orden`.
- `IntentarInicioAutomatico`: idempotente, gated por tiempo (`now ≥ TiempoInicio`), `NoCorresponde` = no-op sin save/evento.
- **Cubrir los tres `ModoInicioPartida`** {Manual, Automatico, ManualYAutomatico}: que `Iniciar` y `IntentarInicioAutomatico` acepten/rechacen según modo correctamente.
- Secuenciación: exactamente un `Activo` mientras `Iniciada`; `FinalizarJuegoActual` finaliza el `Activo`, activa el siguiente `Pendiente` o → `Terminada` + `FechaFin`.
- Estados terminales (`Terminada`/`Cancelada`) bloquean re-inicio (409).
- Evidencia: `Domain/Entities/SesionPartida.cs`, `JuegoResumen.cs`, `Domain/Results/Resultado{Inicio,Avance}.cs`, enums.
- Método: lectura del agregado + cruce con tests de dominio.
- Aprobación: cada invariante tiene código que lo impone **y** un test que lo verifica.

### D4 — Disciplina CQRS / MediatR
- Commands mutan, Queries no. Validación en `ValidationBehavior` (pipeline), no en controllers.
- `TimeProvider` inyectado; `now` fluye como parámetro al dominio; **sin `DateTime.UtcNow` inline** en dominio/handlers.
- Evidencia: `Application/Handlers/**`, `ValidationBehavior.cs`, `Application/Validators/*`.
- Método: grep `DateTime.UtcNow` / `DateTime.Now`; leer handlers.
- Aprobación: sin reloj inline; queries sin mutación; validación en pipeline.

### D5 — Estado runtime / ADR-0010 / R1
- Estado runtime materializado **sólo** en `SesionPartida.EstadoSesion` (Operaciones). Partidas no expone comandos de publish/runtime; su `EstadoPartida` queda `null`.
- **Sutileza a verificar:** CLAUDE.md nombra `EstadoPartida ∈ {Lobby,...}` a nivel conceptual, pero el código usa `EstadoSesion` por ADR-0010. Confirmar que el rename es **intencional y respaldado por ADR-0010**, no una deriva — y que la traceability lo refleja.
- Evidencia: ADR-0010; `Domain/Enums/EstadoSesion.cs`; ausencia de escrituras a Partidas.
- Aprobación: estado single-sourced en Operaciones; rename respaldado por ADR.

### D6 — Conformidad código ↔ contratos (HTTP + eventos)
- 8 endpoints (4 SP-3a + 4 SP-3b): verbo, ruta, rol, código de éxito y conjunto de errores coinciden con `contracts/http/operaciones-sesion-api.md`. Atención: SP-3a publish = **201**; transiciones SP-3b = **200** (no 201).
- DTOs (`LobbyDto`, `InscripcionResponse`, `InicioPartidaResponse`, `AvanceJuegoResponse`, `EstadoSesionDto`) coinciden campo a campo.
- 5 eventos (`PartidaPublicadaEnLobby`, `PartidaIniciada`, `JuegoActivado`, `PartidaCancelada`, `PartidaFinalizada`) coinciden en nombre y payload con `contracts/events/operaciones-sesion-events.md`.
- **Watch-item conocido:** el set `InicioPartidaResponse.estado` documentado puede omitir valores alcanzables (`Terminada`/`Iniciada` en no-op automático). Verificar y, si el contrato está incompleto, marcar **Minor** (código correcto, doc a alinear).
- Método: comparar firmas de controller/DTO/record de evento contra el `.md` línea a línea.
- Aprobación: cero drift que rompa clientes; nits de doc → Minor.

### D7 — Seam de eventos (puerto No-Op)
- `ISesionEventsPublisher` con los 5 métodos; `NoOpSesionEventsPublisher` implementa todos como no-op.
- **Publicación después de `SaveChanges`** (no antes), en todos los handlers que emiten.
- Auto-cancelación emite `PartidaCancelada` (motivo `"MinimosNoAlcanzados"`); `NoCorresponde` no emite ni guarda.
- Evidencia: handlers SP-3b; `NoOpSesionEventsPublisher.cs`.
- Aprobación: orden save→publish correcto; mapeo resultado→evento correcto.

### D8 — Persistencia y migraciones
- Mapeos EF de columnas nuevas (`fechainicio`, `fechafin`, `estadojuego`) correctos; migración `20260627132931_SP3bInicioSecuenciacion` **aditiva** (Up agrega, Down dropea; sin cambios destructivos).
- `ModelSnapshot` consistente con las dos migraciones.
- Evidencia: `Infrastructure/Persistence/OperacionesSesionDbContext.cs`, `Migrations/*`.
- Aprobación: migración aditiva y reversible; snapshot al día.

### D9 — Pruebas (pirámide e invariantes)
- Existen Unit + Integration + Contract. **Controller unit tests presentes** (requisito graduado).
- Cobertura de cada invariante de D3 (auto-cancel, idempotencia, gating temporal, secuenciación, estados terminales, los tres modos de inicio).
- Conteo declarado coincide con realidad (reconciliar el discrepante histórico: `SesionEndpointsTests` = 17 facts = 11 SP-3a + 6 SP-3b; ContractTests = 18 con Health).
- **Re-ejecutar la suite en HEAD** y confirmar verde (no confiar en el ledger).
- Evidencia: `tests/**`; salida de `dotnet test`.
- Aprobación: suite verde en HEAD; cada invariante con test; conteos cuadran.

### D10 — Integridad del flujo SDD
- spec/design/tasks/acceptance de SP-3a y SP-3b **sin secciones TODO**.
- Filas de traceability SP-3a/SP-3b presentes, con estado y **diferimientos** listados.
- Ledger `.git/sdd/progress.md`: una línea por tarea + entrada de revisión final; coincide con `git log`.
- Acceptance refleja criterios verificados.
- Aprobación: SDD completo; traceability y ledger consistentes con git.

### D11 — Postura de seguridad / autorización
- Identidad por JWT (claim de participante) extraída en controller, sin lógica de negocio.
- Sin `[Authorize]` de permiso funcional fino (diferido a SP-5) — verificar que el **diferimiento está documentado**, no olvidado.
- Credencial Postgres local `16102005` es de diseño/dev documentada — **no** marcar como secreto filtrado.
- Aprobación: identidad correcta; authz fino diferido y documentado.

### D12 — Riesgos arrastrados (no descartar en silencio)
- Watch-item de **token de concurrencia** en `SesionPartida` para SP-3f (doble-publish bajo scheduler + broker real) registrado en ledger y memoria (`sp3f-concurrency-token`). Confirmar que sigue trazado hacia SP-3f.
- Incidente `GUIA-USO-AGENTE.md` (edición no-staged perdida por cleanup de subagente): confirmar lección registrada (`subagent-git-cleanup-hazard`) y que no se reintrodujo el riesgo.
- Aprobación: riesgos visibles y asignados a su slice, no perdidos.

## 5. Método de ejecución

- **Read-only.** Usar Explore / grep / `code-review-graph`; nunca Edit/Write sobre el servicio, nunca commit/merge.
- Por dimensión: recolectar evidencia → emitir hallazgos con severidad y ruta `file:line`.
- **Opción recomendada (subagent-driven):** un auditor por clúster de dimensiones, en paralelo, cada uno con su checklist + las fuentes de autoridad como lente:
  - Auditor A → D1, D2, D5 (arquitectura/estructura/R1)
  - Auditor B → D3, D4, D7 (dominio/CQRS/eventos)
  - Auditor C → D6, D8, D9 (contratos/persistencia/tests; este corre `dotnet test`)
  - Auditor D → D10, D11, D12 (SDD/seguridad/riesgos)
  Síntesis final consolida y deduplica. Alternativa inline si se prefiere un solo hilo. (No lanzar subagentes sin que el usuario lo pida; por defecto, inline.)

## 6. Rúbrica de severidad

- **Critical** — violación de doctrina: escritura a DB ajena, invariante de estado roto, mismatch de contrato que rompe clientes, monolitización, reintroducción de servicio prohibido.
- **Important** — desviación de estructura graduada, invariante sin test, `DateTime.UtcNow` inline en dominio, migración no aditiva, diferimiento medio-construido.
- **Minor** — naming, nit de documentación/contrato (p.ej. set de enum incompleto en doc), redundancia inocua.

## 7. Compuerta de aprobación (definición de "conforme")

- **Conforme:** 0 Critical y 0 Important; Minors registrados como follow-up. Suite verde en HEAD.
- **No conforme:** ≥1 Critical o Important → genera lista de remediación; los Critical bloquean el aplastado "Fase N" hasta corregirse.

## 8. Entregable

- Informe `docs/04-sdd/auditorias/2026-06-27-informe-conformidad-sp3ab.md` con: veredicto por dimensión (D1–D12), tabla de hallazgos (severidad · ruta · descripción · fuente · remediación sugerida), veredicto global, y lista de tareas de remediación si aplica.
- Actualizar `docs/04-sdd/traceability-matrix.md` con el resultado de la auditoría (fila/nota).

## 9. Verificación de la propia auditoría

Antes de declarar la auditoría completa:
- Cada una de las 12 dimensiones tiene evidencia citada (rutas concretas), no aseveraciones.
- Cada diferimiento del §2 fue **confirmado como diferido** (no asumido).
- La suite se **re-ejecutó en HEAD** y quedó verde (D9), no se confió en el ledger.
- Cada hallazgo Critical/Important enlaza a su fuente de autoridad (§3).
