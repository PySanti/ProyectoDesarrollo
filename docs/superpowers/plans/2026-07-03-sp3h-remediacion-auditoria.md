# SP-3h — Remediación de auditoría · Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cerrar los 7 hallazgos del informe de auditoría 2026-07-02 (I-1..I-4, m-1..m-3) + 3 minors del ledger, dejando el rango SP-3c..3e-4 CONFORME pleno antes de SP-4.

**Architecture:** Sin cambios estructurales. Dos tareas doc-only (contratos, gobernanza), dos de código puntual (dominio, proyección), una de tests, una de cierre. Único cambio de comportamiento: borde de deadline Trivia (1 tick) y preferencia de convocatoria Aceptada en mi-sesión.

**Tech Stack:** .NET 8, xUnit, Markdown (contratos/ADR).

**Spec:** `docs/superpowers/specs/2026-07-03-sp3h-remediacion-auditoria-design.md`

## Global Constraints

- Rama: `feature/sp-3-audit`. Carve-out LEVANTADO: `docs/04-sdd/traceability-matrix.md` y `docs/04-sdd/auditorias/*` se commitean normalmente en este slice.
- Commits terminan SOLO con: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
- `git add` SOLO de archivos exactos (nunca `-A`, `.`, ni `docs/` completo). PROHIBIDO `git checkout/restore/clean/stash/reset` de rango amplio.
- Suites tras cada tarea de código: desde `services/operaciones-sesion/`, los 3 proyectos de test deben quedar verdes. Baseline: Unit 323 / Integration 28 / Contract 48. Esperado final: Unit 327 / Integration 28 / Contract 48.
- Comandos de test (desde `services/operaciones-sesion/`):
  - `dotnet test tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
  - `dotnet test tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj`
  - `dotnet test tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`

---

### Task H1: Contrato de eventos + línea Status HTTP (I-1, I-2, m-2 — doc-only)

**Files:**
- Modify: `contracts/events/operaciones-sesion-events.md`
- Modify: `contracts/http/operaciones-sesion-api.md` (solo línea 5)

**Interfaces:**
- Consumes: records reales en `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/{TriviaRuntimeEvents,BdtRuntimeEvents,ParticipacionEvents}.cs` (fuente de verdad de los shapes — leerlos antes de editar).
- Produces: doc de eventos alineado al código; nada de código.

- [ ] **Step 1: Leer los 3 archivos de records y verificar los shapes**

Leer `TriviaRuntimeEvents.cs`, `BdtRuntimeEvents.cs`, `ParticipacionEvents.cs`. Confirmar (si algo difiere, el código gana y se ajusta el texto de los steps siguientes):
- `RespuestaTriviaValidadaEvent`, `PuntajeTriviaIncrementadoEvent`, `TesoroQRValidadoEvent`, `EtapaBDTGanadaEvent` → tienen `Guid? EquipoId = null` trailing.
- `PreguntaTriviaCerradaEvent`, `EtapaBDTCerradaEvent` → tienen `Guid? GanadorEquipoId = null` trailing.
- `PistaEnviadaEvent(PartidaId, SesionPartidaId, JuegoId, ParticipanteDestinoId?, Texto, Instante, EquipoDestinoId? = null)`.
- `ConvocatoriaCreadaEvent(PartidaId, SesionPartidaId, ConvocatoriaId, EquipoId, UsuarioId)`.
- `ConvocatoriaRespondidaEvent(PartidaId, SesionPartidaId, ConvocatoriaId, UsuarioId, EstadoConvocatoria)`.

- [ ] **Step 2: Actualizar la tabla Event Registry**

En `contracts/events/operaciones-sesion-events.md`, en la tabla `## Event Registry`:

Reemplazar estas 3 filas (shapes inline) por versiones con los campos Equipo:

```markdown
| `TesoroQRValidado` (SP-3d, SP-3e-3) | Cada intento de tesoro registrado | Registered | { partidaId, sesionPartidaId, juegoId, etapaId, participanteId, resultado, instante, equipoId? } |
| `EtapaBDTGanada` (SP-3d, SP-3e-3) | Validación correcta dentro de ventana | Registered | { partidaId, sesionPartidaId, juegoId, etapaId, participanteId, puntaje, tiempoResolucionMs, equipoId? } |
| `EtapaBDTCerrada` (SP-3d, SP-3e-3) | Cierre de etapa (ganador / tiempo / avance operador) | Registered | { partidaId, sesionPartidaId, juegoId, etapaId, motivo, fechaCierre, ganadorParticipanteId?, ganadorEquipoId? } |
```

Añadir al final de la tabla estas 3 filas nuevas:

```markdown
| `PistaEnviada` (SP-3f-4, SP-3e-4) | El operador envía una pista a un participante o equipo durante un juego BDT activo. | Defined by SDD | Payload registered (SP-3f-4 / SP-3e-4) |
| `ConvocatoriaCreada` (SP-3e-1) | Se preinscribe un equipo: cada miembro del snapshot recibe una convocatoria. | Defined by SDD | Payload registered (SP-3e-1) |
| `ConvocatoriaRespondida` (SP-3e-1) | Un convocado acepta o rechaza su convocatoria. | Defined by SDD | Payload registered (SP-3e-1) |
```

- [ ] **Step 3: Actualizar los 6 samples JSON con los campos Equipo**

En la sección `## Payloads (registered)`:

`RespuestaTriviaValidada` — añadir a su bloque JSON, tras `"instante"`:
```json
  "equipoId": "guid | null"
```
`PuntajeTriviaIncrementado` — añadir tras `"tiempoRespuestaMs"`:
```json
  "equipoId": "guid | null"
```
`PreguntaTriviaCerrada` — añadir tras `"ganadorParticipanteId"`:
```json
  "ganadorEquipoId": "guid?"
```
`TesoroQRValidado` — añadir tras `"instante"`:
```json
  "equipoId": "guid | null"
```
`EtapaBDTGanada` — añadir tras `"tiempoResolucionMs"`:
```json
  "equipoId": "guid | null"
```
`EtapaBDTCerrada` — añadir tras `"ganadorParticipanteId"`:
```json
  "ganadorEquipoId": "guid?"
```
(En cada caso mantener JSON válido: coma en la línea anterior.)

Añadir tras el bloque de `EtapaBDTActivada` (antes de la nota SP-3d) esta nota de semántica:

```markdown
> **Slice-E note (SP-3e-1..4):** `equipoId`/`ganadorEquipoId` son la identidad dual de la modalidad Equipo: `null` ⇔ partida `Individual`. En `Equipo`, `participanteId`/`ganadorParticipanteId` siguen llevando el **autor real** de la acción y `equipoId`/`ganadorEquipoId` el equipo al que se acredita.
```

- [ ] **Step 4: Añadir las 3 secciones de payload nuevas**

Al final del documento (tras la nota SP-3d), añadir:

````markdown
### `PistaEnviada` (SP-3f-4 / SP-3e-4)

Emitted when the operator sends a clue during an active BDT stage. Event-only (no persistence — BR-B06 "recorded" deferred). Real-time delivery via SignalR: group `participante:{participanteDestinoId}` **xor** `equipo:{equipoDestinoId}` (exactly one destination per event).

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "juegoId": "guid",
  "participanteDestinoId": "guid | null",
  "texto": "string",
  "instante": "datetime",
  "equipoDestinoId": "guid | null"
}
```

### `ConvocatoriaCreada` (SP-3e-1)

Emitted once per team-member snapshot entry when a team is pre-inscribed. Real-time delivery via SignalR to group `participante:{usuarioId}`; broker delivery deferred to the RabbitMQ backbone slice.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "convocatoriaId": "guid",
  "equipoId": "guid",
  "usuarioId": "guid"
}
```

### `ConvocatoriaRespondida` (SP-3e-1)

Emitted when a summoned member accepts or rejects their convocatoria. Currently **No-Op** on the SignalR side (deliberate — the operator lobby view polls; see SP-3f-2 no-broadcast list); broker delivery deferred.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "convocatoriaId": "guid",
  "usuarioId": "guid",
  "estadoConvocatoria": "Aceptada | Rechazada"
}
```
````

- [ ] **Step 5: Actualizar la línea Status del contrato HTTP**

En `contracts/http/operaciones-sesion-api.md`, reemplazar la línea 5:

```markdown
SP-3a endpoints registered. Remaining capabilities require a current-doctrine SDD before implementation.
```

por:

```markdown
Endpoints SP-3a..SP-3e-4 registered (21). Trivia and BDT runtime operational in `Individual` and `Equipo` modality; clue delivery, geolocation relay and realtime push via SignalR. RabbitMQ broker delivery and clue persistence remain deferred (see SDD specs).
```

- [ ] **Step 6: Verificación cruzada**

Releer los 3 archivos de records y comparar campo a campo contra cada sample editado/añadido (nombre camelCase, orden, nullabilidad). Criterio de done: cero diferencias.

- [ ] **Step 7: Commit**

```bash
git add contracts/events/operaciones-sesion-events.md contracts/http/operaciones-sesion-api.md
git commit -m "SP-3h H1: contrato de eventos alineado a slice-E + status HTTP (I-1, I-2, m-2)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task H2: ADR-0011 + desambiguación SP-3e (I-4, m-3 — doc-only)

**Files:**
- Create: `docs/05-decisions/ADR-0011-hub-signalr-membresia-grupos.md`
- Modify: `docs/superpowers/specs/2026-06-29-sp3e-reconexion-design.md` (nota al tope)
- Modify: `docs/superpowers/specs/2026-07-01-sp3e1-participacion-equipo-design.md` (nota al tope)

**Interfaces:**
- Consumes: hallazgo I-4 del informe `docs/04-sdd/auditorias/2026-07-02-informe-conformidad-sp3c-3e4.md`; spec SP-3f-2 `docs/superpowers/specs/2026-06-30-sp3f2-push-tiempo-real-signalr-design.md`.
- Produces: ADR-0011 Accepted (cierra I-4 sin código).

- [ ] **Step 1: Crear el ADR**

Crear `docs/05-decisions/ADR-0011-hub-signalr-membresia-grupos.md`:

```markdown
# ADR-0011 — Los hubs SignalR resuelven membresía de grupos vía repositorio de lectura

- **Estado:** Accepted
- **Fecha:** 2026-07-03
- **Contexto de origen:** hallazgo I-4 (escalado) del informe de auditoría `docs/04-sdd/auditorias/2026-07-02-informe-conformidad-sp3c-3e4.md`.

## Contexto

CLAUDE.md ("Structure & coding rules") exige que la capa `Api/` despache por MediatR y no contenga lógica de negocio. `SesionHub.SuscribirAPartida` (Operaciones de Sesión) valida la pertenencia del caller a la partida y resuelve su grupo de equipo inyectando `ISesionPartidaRepository` directamente, sin MediatR. El design aprobado SP-3f-2 lo mandó explícitamente ("reutiliza la consulta de participación existente") y SP-3e-4 extendió el patrón al grupo `equipo:{id}`. La auditoría SP-3c..3e-4 escaló la tensión entre ambas autoridades.

## Decisión

Los hubs SignalR de los servicios UMBRAL **pueden resolver la validación de pertenencia y la membresía de grupos en el handshake de suscripción vía repositorio de lectura inyectado**, sin despachar por MediatR.

Racional: es validación de identidad/pertenencia del canal realtime — equivalente funcional a un middleware de autorización, no a un command/query de negocio. La identidad sale siempre del JWT `sub` server-side; el cliente solo aporta `partidaId`.

## Límites (siguen vigentes)

1. **Solo lectura.** Un hub nunca muta estado ni invoca `SaveChanges`/unit-of-work.
2. **Solo handshake.** El patrón aplica a métodos de suscripción/desuscripción (`SuscribirAPartida`, `DesuscribirDePartida`). Cualquier otra operación de hub con reglas de negocio debe despachar por MediatR.
3. **Relay puro permitido.** Métodos como `EnviarUbicacion` (relay a grupo operador, sin persistencia, BR-B07) permanecen sin repositorio ni MediatR.
4. Las excepciones de hub (`HubException`) son el mecanismo de rechazo del canal realtime; el middleware HTTP de excepciones no aplica a hubs.

## Consecuencias

- Cierra el hallazgo I-4 sin cambio de código; el patrón existente en `SesionHub` queda sancionado.
- Futuros hubs (p. ej. Puntuaciones/SignalR en SP-4) heredan esta regla y sus límites.

## Referencias

- Informe de auditoría 2026-07-02, hallazgo I-4.
- Spec SP-3f-2 (`docs/superpowers/specs/2026-06-30-sp3f2-push-tiempo-real-signalr-design.md`), sección del hub.
- CLAUDE.md, "Structure & coding rules (graded)".
```

- [ ] **Step 2: Nota de desambiguación en el spec de reconexión**

Al tope de `docs/superpowers/specs/2026-06-29-sp3e-reconexion-design.md`, inmediatamente después del título (línea 1), insertar:

```markdown
> **Desambiguación:** "SP-3e" en este documento = slice de **reconexión/mi-sesión** (2026-06-29). No confundir con **SP-3e-1..SP-3e-4** (modalidad Equipo, 2026-07-01/02), que son slices independientes posteriores.
```

- [ ] **Step 3: Nota recíproca en el spec SP-3e-1**

Al tope de `docs/superpowers/specs/2026-07-01-sp3e1-participacion-equipo-design.md`, tras el título, insertar:

```markdown
> **Desambiguación:** los slices **SP-3e-1..SP-3e-4** (modalidad Equipo) son independientes del slice "SP-3e" de reconexión/mi-sesión (`2026-06-29-sp3e-reconexion-design.md`), anterior y sin relación con Equipo.
```

- [ ] **Step 4: Commit**

```bash
git add docs/05-decisions/ADR-0011-hub-signalr-membresia-grupos.md docs/superpowers/specs/2026-06-29-sp3e-reconexion-design.md docs/superpowers/specs/2026-07-01-sp3e1-participacion-equipo-design.md
git commit -m "SP-3h H2: ADR-0011 membresía de grupos en hubs + desambiguación SP-3e (I-4, m-3)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task H3: Dominio — borde de deadline Trivia + ParaEquipo (m-1, ledger)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/PreguntaSnapshot.cs:69`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs` (método `CancelarInscripcionEquipo`, `?? throw` ~línea 139)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/PreguntaSnapshotTests.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaEquipoTests.cs`

**Interfaces:**
- Consumes: `PreguntaSnapshot.RegistrarRespuesta(Guid participanteId, Guid? equipoId, Guid opcionId, DateTime now)`; `InscripcionNoEncontradaException.ParaEquipo(Guid equipoId)` (factory ya existente en `Domain/Exceptions/InscripcionNoEncontradaException.cs:10`); helpers de test existentes `Pregunta(limite:)`, `CorrectaId(p)`, `T0` en `PreguntaSnapshotTests`.
- Produces: en el tick exacto del deadline la respuesta Trivia se rechaza (`PreguntaFueraDeTiempoException`); `CancelarInscripcionEquipo` lanza mensaje de equipo. Ningún cambio de firma.

- [ ] **Step 1: Test de borde (RED)**

En `PreguntaSnapshotTests.cs`, junto a `Answer_after_time_limit_throws`, añadir:

```csharp
    [Fact]
    public void Answer_at_exact_deadline_throws()
    {
        var p = Pregunta(limite: 30);
        p.Activar(T0);
        Assert.Throws<PreguntaFueraDeTiempoException>(
            () => p.RegistrarRespuesta(Guid.NewGuid(), null, CorrectaId(p), T0.AddSeconds(30)));
    }
```

- [ ] **Step 2: Correr y verificar que falla**

Run (desde `services/operaciones-sesion/`):
`dotnet test tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "Answer_at_exact_deadline_throws"`
Expected: FAIL — con el `>` actual, `now == deadline` no lanza; la respuesta se registra y la aserción `Throws` falla.

- [ ] **Step 3: Fix del comparador**

En `PreguntaSnapshot.cs` línea 69, cambiar:

```csharp
        if (now > FechaActivacion!.Value.AddSeconds(TiempoLimiteSegundos))
```

por:

```csharp
        if (now >= FechaActivacion!.Value.AddSeconds(TiempoLimiteSegundos))
```

(Alinea con `EtapaSnapshot.cs:51` — `now < deadline` acepta — y con el cierre por barrido `>=` de `SesionPartida`.)

- [ ] **Step 4: Correr y verificar que pasa**

Mismo comando del Step 2. Expected: PASS.

- [ ] **Step 5: Test de mensaje ParaEquipo (RED)**

En `SesionPartidaEquipoTests.cs`, junto a los tests existentes de `CancelarInscripcionEquipo` (~línea 165), añadir (usar los helpers de construcción de sesión Equipo ya presentes en ese archivo — copiar el arrange de `CancelarInscripcionEquipo_lider_cancela` pero SIN preinscribir el equipo objetivo):

```csharp
    [Fact]
    public void CancelarInscripcionEquipo_equipo_no_inscrito_lanza_mensaje_de_equipo()
    {
        var sesion = PartidaEquipo(Guid.NewGuid()); // helper existente del archivo; si se llama distinto, usar el del arrange de los tests vecinos
        var equipoId = Guid.NewGuid();

        var ex = Assert.Throws<InscripcionNoEncontradaException>(
            () => sesion.CancelarInscripcionEquipo(equipoId, callerEsLider: true));

        Assert.Contains("equipo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
```

- [ ] **Step 6: Correr y verificar que falla**

Run: `dotnet test tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "CancelarInscripcionEquipo_equipo_no_inscrito_lanza_mensaje_de_equipo"`
Expected: FAIL — el ctor actual produce "El participante {id} no tiene…" (no contiene "equipo").

- [ ] **Step 7: Fix a factory ParaEquipo**

En `SesionPartida.cs`, método `CancelarInscripcionEquipo`, cambiar:

```csharp
        var inscripcion = _inscripciones.FirstOrDefault(i => i.EquipoId == equipoId && i.EsActiva)
            ?? throw new InscripcionNoEncontradaException(equipoId);
```

por:

```csharp
        var inscripcion = _inscripciones.FirstOrDefault(i => i.EquipoId == equipoId && i.EsActiva)
            ?? throw InscripcionNoEncontradaException.ParaEquipo(equipoId);
```

- [ ] **Step 8: Correr y verificar que pasa + suite Unit completa**

Run: `dotnet test tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS, 325/325 (323 + 2 nuevos). Si algún test existente fijaba el borde `AddSeconds(30)` como aceptado o el mensaje de participante en cancelación de equipo, ajustarlo es parte de esta tarea (justificar en el commit).

- [ ] **Step 9: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/PreguntaSnapshot.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/PreguntaSnapshotTests.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaEquipoTests.cs
git commit -m "SP-3h H3: borde de deadline Trivia inclusivo + ParaEquipo en cancelación (m-1, ledger)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task H4: Mi-sesión prefiere convocatoria Aceptada (ledger)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs:23-26`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ProyeccionesEquipoTests.cs`

**Interfaces:**
- Consumes: `Convocatoria.EstaAceptada` (bool, ya existente — el hub la usa); helpers de `ProyeccionesEquipoTests`: `PartidaEquipo(partidaId)`, `T0`, `FakeSesionPartidaRepository`, patrón `PreinscribirEquipo(equipoId, true, new[]{usuario}, false, ordinal, T0)` + `ResponderConvocatoria(convId, usuario, aceptar, false, T0)`.
- Produces: cuando el usuario tiene convocatorias en más de una inscripción de la misma sesión, `MiSesionDto.Convocatoria` refleja la **Aceptada**. Con 0..1 convocatorias el comportamiento es idéntico al actual.

- [ ] **Step 1: Test (RED)**

En `ProyeccionesEquipoTests.cs` añadir:

```csharp
    [Fact]
    public async Task MiSesion_prefiere_la_convocatoria_aceptada_sobre_la_pendiente()
    {
        var partidaId = Guid.NewGuid();
        var usuario = Guid.NewGuid();
        var equipoA = Guid.NewGuid();
        var equipoB = Guid.NewGuid();
        var sesion = PartidaEquipo(partidaId);

        // Equipo A convoca al usuario (queda Pendiente; inscripción A es la primera de la lista)
        sesion.PreinscribirEquipo(equipoA, true, new[] { usuario, Guid.NewGuid() }, false, 0, T0);

        // Equipo B convoca al usuario y este acepta
        var inscB = sesion.PreinscribirEquipo(equipoB, true, new[] { usuario }, false, 1, T0);
        sesion.ResponderConvocatoria(inscB.Convocatorias[0].Id.Valor, usuario, true, false, T0);

        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerMiSesionQueryHandler(repo);

        var dto = await handler.Handle(new ObtenerMiSesionQuery(usuario), default);

        Assert.NotNull(dto!.Convocatoria);
        Assert.Equal("Aceptada", dto.Convocatoria!.Estado);
        Assert.Equal(equipoB, dto.Convocatoria.EquipoId);
    }
```

Nota: si `PreinscribirEquipo` lanza un guard al convocar a un usuario ya convocado-pendiente por otro equipo (leer el método antes de asumir), invertir el orden: preinscribir B y aceptar primero NO es posible (aceptar bloquea la convocatoria posterior de A por participación activa) — en ese caso el escenario válido es: A convoca → usuario **rechaza** A → B convoca → usuario acepta B; con inscripción A primera en la lista el first-match actual sigue devolviendo la Rechazada de A y el test sigue siendo RED. Ajustar el arrange a ese flujo y las aserciones quedan iguales.

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "MiSesion_prefiere_la_convocatoria_aceptada_sobre_la_pendiente"`
Expected: FAIL — first-match devuelve la convocatoria de la inscripción A (Pendiente/Rechazada), la aserción `Estado == "Aceptada"` falla.

- [ ] **Step 3: Fix — preferencia por Aceptada**

En `ObtenerMiSesionQueryHandler.cs`, reemplazar:

```csharp
        var convocatoria = sesion.Inscripciones
            .Where(i => i.EsActiva)
            .SelectMany(i => i.Convocatorias)
            .FirstOrDefault(c => c.UsuarioId == request.ParticipanteId);
```

por:

```csharp
        var convocatoria = sesion.Inscripciones
            .Where(i => i.EsActiva)
            .SelectMany(i => i.Convocatorias)
            .Where(c => c.UsuarioId == request.ParticipanteId)
            .OrderByDescending(c => c.EstaAceptada)
            .FirstOrDefault();
```

- [ ] **Step 4: Correr y verificar que pasa + suite Unit completa**

Run: `dotnet test tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS, 326/326. Los tests mi-sesión existentes (una sola convocatoria) no cambian.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ProyeccionesEquipoTests.cs
git commit -m "SP-3h H4: mi-sesión prefiere convocatoria Aceptada sobre first-match (ledger SP-3e-2)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task H5: Controller test ObtenerEtapaActual + estilo .Group (I-3, ledger)

**Files:**
- Test (modify): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerBdtTests.cs`
- Test (modify): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs:328`

**Interfaces:**
- Consumes: `SesionesController.ObtenerEtapaActual(Guid partidaId, CancellationToken)` → `Ok(await _mediator.Send(new ObtenerEtapaActualQuery(partidaId)))`; `EtapaActualDto(Guid PartidaId, Guid JuegoId, Guid EtapaId, int Orden, string AreaBusqueda, int TiempoLimiteSegundos, DateTime FechaActivacion)`; helpers existentes `FakeSender`, `WithUser` en `SesionesControllerBdtTests`; `FakeGroupManager.Added` es `List<(string Conn, string Group)>`.
- Produces: cobertura 21/21 acciones del controller; cero `Item2` en tests.

- [ ] **Step 1: Test del controller (cobertura — pasa al escribirse)**

En `SesionesControllerBdtTests.cs` añadir:

```csharp
    [Fact]
    public async Task Obtener_etapa_actual_dispatches_query()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new EtapaActualDto(
            partidaId, Guid.NewGuid(), Guid.NewGuid(), 1, "Plaza central", 60,
            new DateTime(2026, 6, 28, 10, 0, 0, DateTimeKind.Utc)));
        var controller = WithUser(sender, Guid.NewGuid());

        var result = await controller.ObtenerEtapaActual(partidaId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<EtapaActualDto>(ok.Value);
        var query = Assert.IsType<ObtenerEtapaActualQuery>(sender.LastRequest);
        Assert.Equal(partidaId, query.PartidaId);
    }
```

- [ ] **Step 2: Refactor de estilo Item2 → Group**

En `SesionHubTests.cs` línea 328, cambiar:

```csharp
        Assert.DoesNotContain(groups.Added, g => g.Item2.StartsWith("equipo:"));
```

por:

```csharp
        Assert.DoesNotContain(groups.Added, g => g.Group.StartsWith("equipo:"));
```

- [ ] **Step 3: Correr suite Unit completa**

Run: `dotnet test tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS, 327/327.

- [ ] **Step 4: Correr Integration y Contract**

Run:
`dotnet test tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj`
`dotnet test tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`
Expected: 28/28 y 48/48.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerBdtTests.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs
git commit -m "SP-3h H5: controller test ObtenerEtapaActual (21/21) + estilo .Group (I-3, ledger)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task H6: Cierre — traceability, informe, verificación final

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila SP-3h)
- Modify: `docs/04-sdd/auditorias/2026-07-02-informe-conformidad-sp3c-3e4.md` (sección "Remediación aplicada")

**Interfaces:**
- Consumes: hashes reales de los commits H1-H5 (`git log --oneline -6`).
- Produces: rango SP-3c..3e-4 documentado como CONFORME pleno.

- [ ] **Step 1: Fila SP-3h en traceability**

Añadir a `docs/04-sdd/traceability-matrix.md`, siguiendo el formato de las filas existentes, una fila para SP-3h (remediación auditoría): spec `2026-07-03-sp3h-remediacion-auditoria-design.md`, plan homónimo, commits H1..H5 con hashes reales, alcance "I-1..I-4 + m-1..m-3 + 3 minors ledger".

- [ ] **Step 2: Sección de remediación en el informe**

Al final de `docs/04-sdd/auditorias/2026-07-02-informe-conformidad-sp3c-3e4.md`, añadir:

```markdown
## Remediación aplicada (SP-3h, 2026-07-03)

| Hallazgo | Resolución | Commit |
|---|---|---|
| I-1, I-2, m-2 | Contrato de eventos alineado a slice-E (6 samples + 3 eventos registrados) + Status HTTP | <hash H1> |
| I-4, m-3 | ADR-0011 sanciona membresía de grupos vía repositorio en hubs + desambiguación SP-3e | <hash H2> |
| m-1, ledger | Borde de deadline Trivia inclusivo (`>=`) + `ParaEquipo` en cancelación | <hash H3> |
| ledger | Mi-sesión prefiere convocatoria Aceptada | <hash H4> |
| I-3, ledger | Controller test `ObtenerEtapaActual` (21/21) + estilo `.Group` | <hash H5> |

**Estado post-remediación: CONFORME — 0 Critical · 0 Important · 0 Minor** (del alcance de este informe). Suites en HEAD del slice: Unit 327 / Integration 28 / Contract 48.
```

(Sustituir `<hash Hn>` por los hashes cortos reales.)

- [ ] **Step 3: Verificar suites una última vez**

Los 3 comandos de test. Expected: 327 / 28 / 48.

- [ ] **Step 4: Commit**

```bash
git add docs/04-sdd/traceability-matrix.md docs/04-sdd/auditorias/2026-07-02-informe-conformidad-sp3c-3e4.md
git commit -m "SP-3h H6: cierre — traceability + informe post-remediación CONFORME

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```
