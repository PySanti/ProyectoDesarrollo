# SP-3h — Remediación de auditoría SP-3c..3e-4 (contratos, ADR-0011, minors)

- **Slice:** SP-3h (remediación). Base: informe de auditoría `docs/04-sdd/auditorias/2026-07-02-informe-conformidad-sp3c-3e4.md` (CONFORME CON RESERVAS, 0C/4I/3m).
- **Servicio:** Operaciones de Sesión (código/tests) + `contracts/` + `docs/05-decisions/` (docs).
- **Rama:** `feature/sp-3-audit` (HEAD `18a43a6`). **Carve-out levantado por decisión del usuario:** el informe de auditoría, los specs y `docs/04-sdd/traceability-matrix.md` se commitean normalmente con este slice.
- **Objetivo de cierre:** resolver I-1..I-4 y m-1..m-3 del informe + 3 minors baratos del ledger → el rango SP-3c..3e-4 queda CONFORME pleno antes de SP-4.
- **Fuera de alcance:** broker RabbitMQ real, persistencia de pistas (BR-B06 "recorded"), scoring SP-4, minors del ledger no listados aquí (test timeout BDT Equipo, xmin child-only writes, lobby `Guid.Empty` en Equipo, índice `inscripciones.equipoid`, assert `FechaEnvio`, test Rechazado-responde-Trivia), clientes web/móvil.

## 1. Ítems del slice (10)

| # | Origen | Tipo | Qué |
|---|---|---|---|
| 1 | I-1 | doc | Contrato de eventos: añadir campos Equipo a los 6 eventos slice-E |
| 2 | I-2 | doc | Contrato de eventos: registrar `PistaEnviada`, `ConvocatoriaCreada`, `ConvocatoriaRespondida` |
| 3 | m-2 | doc | Contrato HTTP: línea "Status" actualizada |
| 4 | I-4 | doc | ADR-0011: hubs SignalR resuelven membresía de grupos vía repositorio de lectura |
| 5 | m-3 | doc | Desambiguación "SP-3e" (reconexión) vs "SP-3e-1..4" (Equipo) en los 2 specs |
| 6 | m-1 | código | Borde de deadline Trivia: rechazar en el tick exacto (`>=`), alineado con BDT/scheduler |
| 7 | ledger | código | `CancelarInscripcionEquipo` → `InscripcionNoEncontradaException.ParaEquipo` |
| 8 | ledger | código | `ObtenerMiSesionQueryHandler`: preferir convocatoria `EstaAceptada` sobre first-match |
| 9 | I-3 | test | Controller unit test para `ObtenerEtapaActual` |
| 10 | ledger | test | Test hub Individual: `g.Item2` → accessor `.Group` |

## 2. Diseño por grupo

### Grupo A — Contratos (ítems 1-3, doc-only)

**`contracts/events/operaciones-sesion-events.md`:**
- Los 6 samples JSON de los eventos que ganaron campos en slice-E se actualizan **campo a campo contra los records reales** en `Application/Interfaces/TriviaRuntimeEvents.cs` y `BdtRuntimeEvents.cs` (fuente: el código, no este spec): `RespuestaTriviaValidada`, `PuntajeTriviaIncrementado`, `TesoroQRValidado`, `EtapaBDTGanada` ganan `equipoId` (nullable, autor/dueño); `PreguntaTriviaCerrada`, `EtapaBDTCerrada` ganan `ganadorEquipoId` (nullable). Semántica documentada: `null` ⇔ modalidad Individual; en Equipo, identidad dual autor + equipo.
- Tabla registry + sección de sample para los 3 eventos ausentes del seam: `PistaEnviadaEvent` (`BdtRuntimeEvents.cs`), `ConvocatoriaCreadaEvent`/`ConvocatoriaRespondidaEvent` (`ParticipacionEvents.cs`), shapes campo a campo contra los records. Nota por evento de su estado de transporte actual (seam/SignalR hoy; broker diferido — igual que el resto del doc).

**`contracts/http/operaciones-sesion-api.md`:** línea "Status" (línea 5) reemplazada por el estado real: endpoints SP-3a..SP-3e-4 registrados (21), runtime Trivia/BDT Individual y Equipo operativos, broker/persistencia-de-pistas diferidos. Sin tocar el registry (verificado 1:1 sin drift por la auditoría).

### Grupo B — Gobernanza (ítems 4-5, doc-only)

**`docs/05-decisions/ADR-0011-hub-signalr-membresia-grupos.md`** (Accepted):
- **Decisión:** los hubs SignalR de los servicios UMBRAL resuelven la validación de pertenencia y la membresía de grupos en el handshake de suscripción **vía repositorio de lectura inyectado**, sin despachar por MediatR.
- **Racional:** es validación de identidad/pertenencia del canal realtime (equivalente a middleware de autorización), no un comando/query de negocio; la identidad sale del JWT `sub` server-side; el patrón fue mandado por el design aprobado SP-3f-2 ("reutiliza la consulta de participación existente") y confirmado en SP-3e-4 para el grupo `equipo:{id}`.
- **Límites:** solo lectura; solo en métodos de suscripción/handshake (`SuscribirAPartida`/`DesuscribirDePartida`); cualquier mutación o regla de negocio en un hub sigue prohibida y debe despachar por MediatR (`EnviarUbicacion` sigue siendo relay puro).
- **Referencias:** hallazgo I-4 del informe 2026-07-02, spec SP-3f-2 §hub, CLAUDE.md "Structure & coding rules".
- Cierra I-4 sin cambio de código.

**Ítem 5:** una línea de nota al tope de `docs/superpowers/specs/2026-06-29-sp3e-reconexion-design.md` ("SP-3e" aquí = slice de reconexión/mi-sesión; no confundir con SP-3e-1..4, modalidad Equipo) y su recíproca en `2026-07-01-sp3e1-participacion-equipo-design.md`.

### Grupo C — Código (ítems 6-8)

**Ítem 6 (m-1):** `Domain/Entities/PreguntaSnapshot.cs` — el guard de respuesta tardía pasa de aceptar en el tick exacto (`now > deadline` rechaza) a rechazar en el tick exacto (acepta solo si `now < FechaActivacion + TiempoLimiteSegundos`), espejo de `EtapaSnapshot.cs:51` y del comparador `>=` del cierre por barrido (`SesionPartida.cs:220,275`). Comportamiento: en el instante exacto del deadline la respuesta se rechaza como tardía. Test de borde nuevo: `now == deadline` → rechazada (y `now == deadline - 1s` → aceptada, si no existe ya).

**Ítem 7:** `Domain/Entities/SesionPartida.cs` (`CancelarInscripcionEquipo`, ~línea 139) — sustituir el ctor genérico de `InscripcionNoEncontradaException` (mensaje de participante) por el factory existente `InscripcionNoEncontradaException.ParaEquipo(equipoId)`. Ajustar asserts de mensaje en tests si los hay. Sin cambio de status HTTP (sigue 404).

**Ítem 8:** `Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs:23-26` — la resolución de la convocatoria del participante deja de ser first-match: prefiere la convocatoria con `EstaAceptada == true` y cae al comportamiento actual si no hay ninguna aceptada. Corrige la proyección cuando el usuario tiene convocatorias en más de una inscripción de la misma sesión (p. ej. rechazó equipo A, aceptó equipo B). Test nuevo: usuario con convocatoria rechazada en inscripción A y aceptada en B → mi-sesión refleja la aceptada (B). Raíz del minor: SP-3e-1; el hub ya es inmune (filtra `EstaAceptada` directo) — esto alinea la proyección HTTP con el hub.

### Grupo D — Tests (ítems 9-10)

**Ítem 9 (I-3):** controller unit test para `ObtenerEtapaActual` en `SesionesControllerBdtTests.cs`, patrón exacto de sus hermanos (`AvanzarEtapa`): mock `ISender`, assert de dispatch de la query con `partidaId` correcto y de mapeo `Ok(response)`. Cierra la única acción sin puerta unitaria (21/21).

**Ítem 10:** test hub Individual que usa `g.Item2` pasa al accessor nombrado `.Group` como sus siblings (estilo, sin cambio de aserción).

## 3. Sin cambios

- Endpoints (forma), DTOs, payloads SignalR (shape), migraciones/persistencia (ningún cambio de esquema), gateway, eventos (records C# — solo el doc se alinea a ellos).
- `PrepararPista`/`PrepararPistaEquipo`, publisher, hub: intactos (I-4 se cierra por ADR).

## 4. Testing y verificación

- Baseline: Unit 323 / Integration 28 / Contract 48. Esperado: **Unit ~326-327** (+1 borde m-1, +1..2 mi-sesión, +1 controller) / Integration 28 / Contract 48 — todas verdes.
- Regresión clave ítem 6: suite Trivia existente no debe requerir cambios (ningún test actual ejercita el tick exacto — verificado por la auditoría); si alguno fija el borde, ajustarlo es parte del ítem con justificación.
- Regresión clave ítem 8: tests mi-sesión existentes (first-match con una sola convocatoria) siguen verdes sin cambios.
- Verificación doc (grupo A): cross-check campo a campo records ↔ samples como criterio de done del implementer y del reviewer.

## 5. Cierre del slice

- Actualizar `docs/04-sdd/traceability-matrix.md` (fila SP-3h) — ahora committeable.
- Anotar en el informe de auditoría una sección "Remediación aplicada (SP-3h)" con el mapeo hallazgo→commit, dejando el conteo post-remediación en 0C/0I/0m del alcance elegido.
- Ledger `.git/sdd/progress.md` por tarea, como siempre.
- Review final whole-branch del slice (opus) sobre el rango de commits SP-3h.

## 6. Riesgos

- **Ítem 6 es un cambio de comportamiento real** (ventana efectiva 1 tick más corta). Mitigación: es la semántica que el scheduler ya aplica — el cambio elimina la incoherencia, no crea una nueva; test de borde lo fija.
- **Ítem 8 toca una proyección usada por móvil.** Mitigación: solo reordena la preferencia de resolución; con 0 o 1 convocatorias el resultado es idéntico al actual.
- **Grupo A sin test que lo guarde** (no hay ContractTest sobre el doc de eventos — señalado por la auditoría). Guardarlo con tests queda fuera de alcance; el cross-check manual del reviewer es la puerta en este slice.
