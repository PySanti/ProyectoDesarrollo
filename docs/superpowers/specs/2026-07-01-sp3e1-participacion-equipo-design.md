# SP-3e-1 — Participación Equipo (fundación) — Design

- **Fecha:** 2026-07-01
- **Rama:** feature/code-migration-SP-3
- **Servicios:** Operaciones de Sesión (principal) + Identity (endpoint read de membresía)
- **Cliente:** móvil (Participante / Líder actuando como participante); operador ve lobby (web, read/operate)
- **Depende de:** SP-3a (inscripción Individual), SP-3f-1 (xmin concurrency), SP-3f-4 (grupo SignalR `participante:{id}`)
- **Habilita:** SP-3e-2 (Trivia Equipo), SP-3e-3 (BDT Equipo), SP-3e-4 (Pistas Equipo)
- **Fuera de alcance:** runtime Trivia/BDT/pistas en modalidad Equipo (2/3/4); scoring/ranking (SP-4); broker RabbitMQ real (bridge diferido)

## 1. Objetivo

Habilitar la modalidad **Equipo** a nivel de **participación** (partida-level). Hoy el runtime es Individual-only: `SesionPartida.Inscribir` lanza `ModalidadNoSoportadaException` si `Modalidad != Individual`. Este slice construye la **fundación** de Equipo: preinscripción del equipo por su líder, generación de convocatorias a los integrantes, aceptación/rechazo, participante-activo = convocatoria aceptada, cupo/mínimos por equipo, no-doble-participación (equipo y participante), y proyecciones de lobby/mi-sesión. Todo lo demás (runtime por equipo) se apoya en esta base.

## 2. Reglas de dominio (fuente: docs/01-project-source, business-rules)

- **Preinscripción por líder.** En partidas por equipo, el **líder** preinscribe **su** equipo activo en la partida; el sistema genera una **Convocatoria** por cada integrante del equipo. (modelo-de-dominio: `PreinscribirEquipo`).
- **Una inscripción por equipo por partida.** En Equipo hay **una** `InscripcionPartida` por equipo (no por integrante, no por juego).
- **Participante activo (Equipo)** = integrante que **aceptó** la convocatoria. Rechazar no lo saca del equipo; solo lo excluye de esa partida.
- **BR-G09 (una participación activa a la vez).** Un **equipo** no puede tener más de una inscripción activa a la vez; un **participante** no puede tener más de una participación activa a la vez = inscripción individual activa **o** convocatoria de equipo aceptada, mientras la partida está en `Lobby`/`Iniciada`.
- **Convocatoria ≠ InvitacionEquipo.** La convocatoria es de partida, se materializa en Operaciones y no cambia la pertenencia al equipo (que vive en Identity vía `InvitacionEquipo`).
- **Modalidad fija.** La modalidad se fija al publicar y aplica a toda la partida (ya capturada en `ConfiguracionSnapshot.Modalidad`).
- **Sin mínimo de jugadores por equipo.** La config target (`ConfiguracionSnapshot`) solo tiene `MinimosParticipacion`/`MaximosParticipacion`; en Equipo cuentan **equipos**, no jugadores. No se introduce `minimoJugadoresPorEquipo` (campo legacy, superseded).

## 3. Arquitectura

Dos servicios, un slice:

### 3.1 Identity — endpoint read de membresía (nuevo)

Operaciones necesita, al preinscribir, la composición del equipo del líder. La membresía vive en Identity (`Equipo {EquipoId, NombreEquipo, Estado, Participantes:[ParticipanteEquipo{UsuarioId, EsLider}]}`). Identity hoy **no** expone lectura de equipo.

- **Query:** `ObtenerMiEquipoQuery(UsuarioId)` + handler → equipo **activo** (`EstadoEquipo.Activo`) del usuario, con integrantes + flag líder. `null` si el usuario no tiene equipo activo.
- **Endpoint:** `GET /api/teams/mine` (auth Registered; `usuarioId` del token).
  - `200` → `{ equipoId, nombreEquipo, estado, participantes: [ { usuarioId, esLider } ] }`
  - `404` → el caller no tiene equipo activo.
- **Contrato:** fila nueva en `contracts/http/identity-api.md` (skill contract-design).
- **Tests:** query handler (equipo activo / sin equipo / equipo eliminado) + controller unit (dispatch + mapeo 200/404).

### 3.2 Operaciones — cliente snapshot (espeja `IConfiguracionPartidaClient`)

- **Puerto** (`Application/Interfaces`): `IEquipoDirectoryClient.ObtenerMiEquipoAsync(string? bearerToken, CancellationToken)` → `EquipoSnapshotDto?`.
  - `EquipoSnapshotDto(Guid EquipoId, string NombreEquipo, IReadOnlyList<MiembroEquipoDto> Miembros)`; `MiembroEquipoDto(Guid UsuarioId, bool EsLider)`.
- **Impl HTTP** (`Infrastructure/Services`): `IdentityEquipoHttpClient` — `GET /api/teams/mine`, reenvía `Authorization` (patrón `PartidasConfigHttpClient`); `404 → null`; red/timeout/no-success → `IdentityInaccesibleException` (nueva app exception, mapeada 502/503 en middleware, análoga a `PartidasConfigInaccesibleException`).
- **Fake** (tests): `FakeEquipoDirectoryClient` con equipo configurable.
- **Registro DI:** `HttpClient` tipado con base address del gateway/Identity (config `IdentityBaseUrl`, análogo a `PartidasBaseUrl`).

## 4. Dominio Operaciones — Convocatoria

- **Enum** `EstadoConvocatoria { Pendiente, Aceptada, Rechazada }`.
- **Entidad hija** `Convocatoria`:
  - Props: `ConvocatoriaId Id`, `Guid InscripcionId`, `Guid PartidaId`, `Guid EquipoId`, `Guid UsuarioId`, `EstadoConvocatoria Estado`, `DateTime FechaEnvio`, `DateTime? FechaRespuesta`.
  - Métodos internos: `Aceptar(now)`, `Rechazar(now)`, prop `EstaAceptada`.
  - VO nuevo `ConvocatoriaId` (espeja `InscripcionId`).
- **`InscripcionPartida` extendida:**
  - `+ Modalidad Modalidad`, `+ Guid? EquipoId`, `+ IReadOnlyList<Convocatoria> Convocatorias`.
  - `CrearIndividual(...)` = ctor actual (Individual, sin equipo/convocatorias).
  - `PreinscribirEquipo(Guid equipoId, IEnumerable<Guid> miembros, DateTime fecha)` → Inscripción `Estado=Activa`, `Modalidad=Equipo`, `EquipoId=equipoId`, una `Convocatoria Pendiente` por miembro.
  - `EstadoInscripcion` se mantiene `{ Activa, Cancelada }` (default simple aprobado; sin two-phase).
  - `ConvocatoriasAceptadas` helper (para conteos de participante-activo).

## 5. Dominio Operaciones — `SesionPartida` (agregado)

- **`PreinscribirEquipo(Guid equipoId, bool callerEsLider, IReadOnlyList<Guid> miembros, bool equipoTieneParticipacionActivaEnOtra, int equiposActivos, DateTime fecha)`**
  - `Estado == Lobby` (si no `SesionNoEnLobbyException`).
  - `Modalidad == Equipo` (si no `ModalidadNoSoportadaException`; el guard actual de `Inscribir` se conserva para el camino Individual — inscribir-individual en partida Equipo y preinscribir-equipo en partida Individual quedan rechazados).
  - `callerEsLider` (si no `NoEsLiderEquipoException`, nueva).
  - equipo no inscrito ya en **esta** partida (si no `EquipoYaInscritoException`, nueva).
  - `!equipoTieneParticipacionActivaEnOtra` (si no `ParticipacionActivaExistenteException`, reuso).
  - `equiposActivos < MaximosParticipacion` (si no `CupoLlenoException`, reuso).
  - Crea la inscripción vía `InscripcionPartida.PreinscribirEquipo` y la agrega; devuelve la inscripción + convocatorias (para emitir eventos).
- **`ResponderConvocatoria(Guid usuarioId, bool aceptar, bool participanteTieneParticipacionActivaEnOtra, DateTime now)`**
  - `Estado == Lobby`.
  - Localiza la `Convocatoria` **Pendiente** del `usuarioId` entre inscripciones activas (si no `ConvocatoriaNoEncontradaException`, nueva).
  - Si `aceptar` y `participanteTieneParticipacionActivaEnOtra` → `ParticipacionActivaExistenteException`.
  - `Aceptar(now)` / `Rechazar(now)`.
- **`CancelarInscripcionEquipo(Guid equipoId, bool callerEsLider)`**
  - `Estado == Lobby`; `callerEsLider`; localiza inscripción activa del equipo (si no `InscripcionNoEncontradaException`, reuso); `Cancelar()` (las convocatorias aceptadas dejan de contar por estar la inscripción `Cancelada`).
- **Participante activo & mínimos (default simple aprobado):**
  - **Participante activo (Equipo)** = integrante con convocatoria **Aceptada** en inscripción **Activa**.
  - **Cupo (`Maximos`)** = nº de inscripciones de equipo **Activas** (chequeo al preinscribir).
  - **Mínimos al iniciar** (`AplicarInicio`): si `Modalidad == Equipo`, cuenta **equipos participantes** = inscripciones activas **con ≥1 convocatoria aceptada**; si `< MinimosParticipacion` → cancela **toda** la sesión (mismo patrón que Individual). Sin `ConfirmarSiCumpleMinimos`/`ExcluirPorMinimos`.

## 6. Eventos (seam `ISesionEventsPublisher`)

Dos métodos nuevos (15 y 16) en el puerto y sus **5 impls** (SignalR / NoOp / Composite / Fake / NoOpBase):

- `PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent { SesionPartidaId, ConvocatoriaId, PartidaId, EquipoId, UsuarioId }, ct)`
- `PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent { SesionPartidaId, ConvocatoriaId, UsuarioId, EstadoConvocatoria }, ct)`

**SignalR:** `ConvocatoriaCreada` → push al grupo `participante:{usuarioId}` (grupo ya existente desde SP-3f-4) para notificar al móvil del convocado. `ConvocatoriaRespondida` no requiere push en este slice (feed de audit/scoring vendrá por broker). Constantes de mensaje en `SesionRealtimeMessages` + payloads en `SesionRealtimePayloads`.

Event-only (sin outbox/broker real; el composite ya combina NoOp + SignalR). El broker real es bridge diferido.

## 7. Application — commands, handlers, DTOs, validators

- `PreinscribirEquipoCommand(Guid PartidaId, Guid LiderId, string? BearerToken)` + handler:
  1. carga sesión (`GetByPartidaIdAsync`, si no `SesionNoEncontradaException`);
  2. `IEquipoDirectoryClient.ObtenerMiEquipoAsync(bearer)` (si `null` → `SinEquipoActivoException`, nueva app exception → 409);
  3. valida que `LiderId` sea el líder del snapshot (`callerEsLider`);
  4. calcula `equipoTieneParticipacionActivaEnOtra` (repo) y `equiposActivos` (de la sesión);
  5. `sesion.PreinscribirEquipo(...)`;
  6. `SaveChangesAsync`;
  7. publica `ConvocatoriaCreada` por cada convocatoria.
- `ResponderConvocatoriaCommand(Guid ConvocatoriaId, Guid UsuarioId, bool Aceptar)` + handler: localiza la sesión por convocatoria, calcula participación-activa-en-otra del usuario, `ResponderConvocatoria`, `SaveChangesAsync`, publica `ConvocatoriaRespondida`.
- `CancelarInscripcionEquipoCommand(Guid PartidaId, Guid LiderId, string? BearerToken)` + handler: valida liderazgo (snapshot), `CancelarInscripcionEquipo`, `SaveChangesAsync`.
- DTOs: `PreinscripcionEquipoResponse(Guid InscripcionId, Guid EquipoId, int Convocados)`, `ConvocatoriaResponse(Guid ConvocatoriaId, string Estado)`.
- Validators: `NotEmpty` en ids.
- Exceptions app nuevas: `IdentityInaccesibleException`, `SinEquipoActivoException`.

## 8. API — endpoints (bajo `[Route("operaciones-sesion")]`)

- `POST partidas/{id}/inscripciones-equipo` — líder; `LiderId` del claim, `BearerToken` del header → `PreinscribirEquipoCommand`. `201` → `PreinscripcionEquipoResponse`.
- `POST convocatorias/{convocatoriaId}/aceptacion` — convocado del claim → `ResponderConvocatoriaCommand(aceptar:true)`. `200`.
- `POST convocatorias/{convocatoriaId}/rechazo` — convocado del claim → `ResponderConvocatoriaCommand(aceptar:false)`. `200`.
- `DELETE partidas/{id}/inscripciones-equipo/mia` — líder → `CancelarInscripcionEquipoCommand`. `204`.
- Proyecciones: `lobby` (`ObtenerLobbyQuery`) y `mi-sesion` (`ObtenerMiSesionQuery`) extendidas para exponer, en Equipo, equipos inscritos y — para el caller — el estado de su convocatoria (`Pendiente/Aceptada/Rechazada/Ninguna`).
- Contrato: `contracts/http/operaciones-sesion-api.md` (endpoints + DTOs + notas de auth y realtime).
- Mapeo de excepciones en `ExceptionHandlingMiddleware`: `NoEsLiderEquipo`→403, `EquipoYaInscrito`/`ParticipacionActivaExistente`/`SinEquipoActivo`→409, `ConvocatoriaNoEncontrada`→404, `IdentityInaccesible`→502/503.

## 9. Persistencia

Migración `SP3eParticipacionEquipo`:

- `InscripcionPartida += Modalidad (int), EquipoId (uuid null)`.
- Tabla `Convocatoria` (`ConvocatoriaId` PK, `InscripcionId` FK, `PartidaId`, `EquipoId`, `UsuarioId`, `Estado`, `FechaEnvio`, `FechaRespuesta` null); relación owned/child en `OperacionesSesionDbContext` (patrón inscripciones).
- Reusa el token de concurrencia `xmin` (SP-3f-1) del agregado `SesionPartida`.

## 10. Repositorio

- `+ Task<bool> EquipoTieneParticipacionActivaAsync(Guid equipoId, Guid exceptPartidaId, CancellationToken)` — existe inscripción de equipo activa en otra partida en `Lobby`/`Iniciada`.
- `+ Task<SesionPartida?> GetByConvocatoriaIdAsync(Guid convocatoriaId, CancellationToken)` — para el handler de respuesta.
- `ParticipanteTieneParticipacionActivaAsync` extendido: participación activa del usuario incluye ahora **convocatoria aceptada** (además de inscripción individual activa).
- Includes de `Convocatorias` en las cargas del agregado.

## 11. Testing

- **Unit dominio** (`Convocatoria`, `InscripcionPartida`, `SesionPartida`): preinscribir feliz + cada guard (no-lobby, no-equipo-modalidad, no-líder, equipo-ya-inscrito, participación-activa-otra, cupo-lleno); aceptar/rechazar convocatoria + guards (no-lobby, convocatoria-inexistente, participación-activa-al-aceptar); cancelar por líder; conteo participante-activo; mínimos Equipo (≥1 aceptado cuenta; cancela toda la sesión bajo mínimos); cupo por equipos.
- **Handler** (`PreinscribirEquipo`, `ResponderConvocatoria`, `CancelarInscripcionEquipo`): con `FakeEquipoDirectoryClient` + repo fake/InMemory; verifica snapshot, publicación de eventos, `SaveChanges`.
- **Controller** unit: dispatch de los 4 endpoints (claim→command, mapeo status).
- **Contract** doc↔constantes: Identity (`GET /api/teams/mine`) + Operaciones (endpoints nuevos + realtime `ConvocatoriaCreada`).
- **Seam fan-out**: composite invoca los 2 métodos nuevos en las impls; SignalR dirige `ConvocatoriaCreada` solo a `participante:{convocado}`.
- **Identity**: query handler + controller test del endpoint read.
- Ejecución: `dotnet test <un-solo-.csproj>` (UnitTests y ContractTests por separado; MSB1008 si se pasan dos rutas).

## 12. Restricciones de proceso

- Commits terminan solo con `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
- Carve-out git: `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md`, `docs/04-sdd/auditorias/` quedan **siempre unstaged**; las tareas escriben la fila de traceability pero **no** la commitean. Stagear solo archivos exactos nombrados; nunca `git add -A/./docs/`.
- Selección de modelo por subagente: haiku = mecánico 1-2 archivos; sonnet = multi-archivo/integración/reviews; opus = review final whole-branch.
- SDD: spec (este doc) → writing-plans → subagent-driven-development (una tarea a la vez, review 2 etapas, commit, ledger) → review final opus → finishing-a-development-branch (decide usuario).

## 13. Follow-ups diferidos (no bloqueantes)

- Runtime Equipo: Trivia (SP-3e-2), BDT (SP-3e-3), pistas targeting equipo (SP-3e-4).
- Broker RabbitMQ real (bridge) para `ConvocatoriaCreada/Respondida` → Puntuaciones/audit.
- Cableado clientes móvil/web (preinscribir, inbox convocatorias, lobby equipos).
- Hardening rol-operador/participante in-service (deuda pre-existente documentada).
