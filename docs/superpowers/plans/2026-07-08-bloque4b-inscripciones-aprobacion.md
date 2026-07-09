# Bloque 4B — Aprobación de inscripciones por el operador (HU-19) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Insertar un paso de aprobación del operador en el flujo de inscripción de Operaciones de Sesión: toda inscripción/preinscripción nace `Pendiente` y solo cuenta (mínimos, cupo, juego, convocatorias) tras que el operador la acepte; rechazar la deja `Rechazada` (terminal, re-solicitable).

**Architecture:** Slice **backend-only** del servicio Operaciones de Sesión (Clean Architecture + CQRS/MediatR + EF Core 8 sobre PostgreSQL, eventos RabbitMQ best-effort ADR-0012). Se extiende el ciclo de vida de `InscripcionPartida` (enfoque A del spec), se añaden dos comandos de operador (aceptar/rechazar), tres eventos de dominio, y una migración EF para el snapshot de miembros. La UI de clientes está **fuera de alcance** (diferida a un slice de migración; ver spec §5).

**Tech Stack:** .NET 8, C#, MediatR, EF Core 8.0.7 (Npgsql + InMemory en tests), xUnit, RabbitMQ, SignalR.

**Spec:** `docs/superpowers/specs/2026-07-08-bloque4b-inscripciones-aprobacion-design.md`

## Global Constraints

- Servicio único: **Operaciones de Sesión**. Respetar capas `Domain → Application → Infrastructure → Api`; el dominio no depende de infraestructura; los controladores no contienen lógica de negocio y despachan por MediatR; **cada controlador tiene tests unitarios**.
- **Aprobación siempre obligatoria** (decisión 1): inscribir/preinscribir → `Pendiente`, para `Individual` y `Equipo`.
- **Equipo: aprobar → luego convocatorias** (decisión 2): la preinscripción guarda el snapshot de miembros y NO crea convocatorias; se crean al aceptar.
- **Rechazo terminal + re-solicitable** (decisión 3): enum `{ Pendiente, Activa, Rechazada, Cancelada }`.
- **BR-G09 — `Pendiente` bloquea** (decisión 4): "una participación activa a la vez" cuenta `Pendiente + Activa`.
- **Cupo se valida al ACEPTAR** (decisión 5): `Pendiente` no consume `MaximosParticipacion`; al aceptar, si `Activos >= Maximos` → 409 `CupoLleno`; al inscribir se rechaza solo si `Activos >= Maximos`.
- **Valores numéricos del enum:** EF persiste `EstadoInscripcion` como **int** (sin `HasConversion<string>()`). Preservar los valores existentes `Activa=0`, `Cancelada=1` y **anexar** `Pendiente=2`, `Rechazada=3` — nunca renumerar (corrompería filas existentes).
- Eventos: best-effort tras `SaveChanges` (ADR-0012), mapa de routing **explícito** en `SesionEventRouting`, envelope camelCase existente. Nada rompe el flujo HTTP si el broker falla.
- **El operador pollea el lobby** — no se añade SignalR (coherente con SP-3f-2). Los 3 eventos nuevos solo alimentan el historial de Puntuaciones (cola ligada a `operaciones-sesion.#`), sin consumidor nuevo.
- Endpoints de operador con policy `GestionarPartidas`; endpoints de participante con `ParticiparEnPartidas`.
- Comando de test de la suite: `dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln`. Proyectos: `Umbral.OperacionesSesion.UnitTests`, `.IntegrationTests`, `.ContractTests`.

---

## File Structure

**Domain**
- `Domain/Enums/EstadoInscripcion.cs` — anexar `Pendiente`, `Rechazada` con valores explícitos.
- `Domain/Entities/InscripcionPartida.cs` — nacer `Pendiente`; snapshot de miembros; `Aceptar`/`Rechazar`; `OcupaParticipacion`/`EstaPendiente`.
- `Domain/Entities/SesionPartida.cs` — guards `OcupaParticipacion`; nacer `Pendiente` sin convocatorias; `AceptarInscripcion`/`RechazarInscripcion`.
- `Domain/Exceptions/` — reutiliza `SesionNoEnLobbyException`, `InscripcionNoEncontradaException`, `CupoLlenoException`, `InscripcionNoPendienteException` (nueva).

**Application**
- `Application/Commands/AceptarInscripcionCommand.cs`, `RechazarInscripcionCommand.cs` (crear).
- `Application/Handlers/Commands/AceptarInscripcionCommandHandler.cs`, `RechazarInscripcionCommandHandler.cs` (crear).
- `Application/Handlers/Commands/InscribirParticipanteCommandHandler.cs`, `PreinscribirEquipoCommandHandler.cs` (modificar: emitir `InscripcionSolicitada`; preinscribir deja de emitir `ConvocatoriaCreada`).
- `Application/Handlers/Commands/PublicarPartidaCommandHandler.cs` — `MapearLobby` con listas de pendientes.
- `Application/DTOs/LobbyDto.cs` — `SolicitudIndividualDto`, `SolicitudEquipoDto`.
- `Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs` — filtro `OcupaParticipacion`.
- `Application/Interfaces/ParticipacionEvents.cs` — 3 records nuevos.
- `Application/Interfaces/ISesionEventsPublisher.cs` — 3 métodos nuevos.

**Infrastructure**
- `Infrastructure/Persistence/OperacionesSesionDbContext.cs` — mapping del snapshot (colección primitiva).
- `Infrastructure/Persistence/Migrations/` — nueva migración EF.
- `Infrastructure/Persistence/SesionPartidaRepository.cs` — `ParticipanteTieneParticipacionActivaAsync`/`EquipoTieneParticipacionActivaAsync`/`GetByParticipanteActivoAsync` cuentan `Pendiente+Activa`.
- `Infrastructure/Services/CompositeSesionEventsPublisher.cs`, `RabbitMqSesionEventsPublisher.cs`, `NoOpSesionEventsPublisher.cs` — 3 métodos.
- `Infrastructure/Services/Messaging/SesionEventRouting.cs` — 3 routing keys.

**Api**
- `Api/Controllers/SesionesController.cs` — endpoints `aceptacion`/`rechazo`.
- `Api/Realtime/SignalRSesionEventsPublisher.cs` — 3 métodos No-Op.

**Contracts/Docs**
- `contracts/events/operaciones-sesion-events.md`, `contracts/http/operaciones-sesion-api.md`, `docs/04-sdd/traceability-matrix.md`.

**Tests** (bajo `services/operaciones-sesion/tests/`): `UnitTests/Domain/`, `UnitTests/Application/`, `UnitTests/Api/`, `UnitTests/Infrastructure/Messaging/`, `IntegrationTests/`, `ContractTests/`.

---

## Task 1: Dominio — enum de estado + ciclo de vida de `InscripcionPartida`

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/EstadoInscripcion.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/InscripcionPartida.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/InscripcionPartidaTests.cs`

**Interfaces:**
- Consumes: `Convocatoria` (ctor `new Convocatoria(partidaId, equipoId, usuarioId, fecha)`, método `Aceptar(now)`, prop `EstaAceptada`, `EstaPendiente`).
- Produces:
  - `enum EstadoInscripcion { Activa = 0, Cancelada = 1, Pendiente = 2, Rechazada = 3 }`
  - `InscripcionPartida`: ctor `internal InscripcionPartida(Guid participanteId, DateTime fecha)` (nace `Pendiente`); factory `internal static InscripcionPartida PreinscribirEquipo(Guid equipoId, IEnumerable<Guid> miembros, Guid partidaId, DateTime fecha)` (nace `Pendiente`, sin convocatorias, guarda snapshot); `internal IReadOnlyList<Convocatoria> Aceptar(DateTime now)`; `internal void Rechazar()`; props `bool OcupaParticipacion`, `bool EstaPendiente`, `bool EsActiva`, `IReadOnlyList<Guid> MiembrosSnapshot`.

- [ ] **Step 1: Reescribir los tests de dominio de `InscripcionPartida`**

Reemplaza el contenido de `InscripcionPartidaTests.cs` por (los tests viejos asumían nacer `Activa` y convocatorias en preinscripción — ya no aplican):

```csharp
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class InscripcionPartidaTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Individual_nace_pendiente_sin_equipo_ni_convocatorias()
    {
        var insc = new InscripcionPartida(Guid.NewGuid(), T0);

        Assert.Equal(Modalidad.Individual, insc.Modalidad);
        Assert.Null(insc.EquipoId);
        Assert.Empty(insc.Convocatorias);
        Assert.Equal(EstadoInscripcion.Pendiente, insc.Estado);
        Assert.True(insc.EstaPendiente);
        Assert.True(insc.OcupaParticipacion);
        Assert.False(insc.EsActiva);
    }

    [Fact]
    public void PreinscribirEquipo_nace_pendiente_guarda_snapshot_sin_convocatorias()
    {
        var equipoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        var insc = InscripcionPartida.PreinscribirEquipo(equipoId, new[] { m1, m2 }, partidaId, T0);

        Assert.Equal(Modalidad.Equipo, insc.Modalidad);
        Assert.Equal(equipoId, insc.EquipoId);
        Assert.Equal(EstadoInscripcion.Pendiente, insc.Estado);
        Assert.Empty(insc.Convocatorias);
        Assert.Equal(new[] { m1, m2 }, insc.MiembrosSnapshot);
    }

    [Fact]
    public void Aceptar_individual_pasa_a_activa_y_no_crea_convocatorias()
    {
        var insc = new InscripcionPartida(Guid.NewGuid(), T0);

        var creadas = insc.Aceptar(T0);

        Assert.Equal(EstadoInscripcion.Activa, insc.Estado);
        Assert.True(insc.EsActiva);
        Assert.Empty(creadas);
        Assert.Empty(insc.Convocatorias);
    }

    [Fact]
    public void Aceptar_equipo_crea_una_convocatoria_pendiente_por_miembro()
    {
        var equipoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();
        var insc = InscripcionPartida.PreinscribirEquipo(equipoId, new[] { m1, m2 }, partidaId, T0);

        var creadas = insc.Aceptar(T0);

        Assert.Equal(EstadoInscripcion.Activa, insc.Estado);
        Assert.Equal(2, creadas.Count);
        Assert.Equal(2, insc.Convocatorias.Count);
        Assert.All(insc.Convocatorias, c => Assert.True(c.EstaPendiente));
        Assert.All(insc.Convocatorias, c => Assert.Equal(equipoId, c.EquipoId));
        Assert.All(insc.Convocatorias, c => Assert.Equal(partidaId, c.PartidaId));
        Assert.Contains(insc.Convocatorias, c => c.UsuarioId == m1);
        Assert.Contains(insc.Convocatorias, c => c.UsuarioId == m2);
    }

    [Fact]
    public void Rechazar_pasa_a_rechazada_y_deja_de_ocupar_participacion()
    {
        var insc = new InscripcionPartida(Guid.NewGuid(), T0);

        insc.Rechazar();

        Assert.Equal(EstadoInscripcion.Rechazada, insc.Estado);
        Assert.False(insc.OcupaParticipacion);
        Assert.False(insc.EsActiva);
        Assert.False(insc.EstaPendiente);
    }

    [Fact]
    public void ConvocatoriasAceptadas_cuenta_solo_aceptadas_tras_aceptar_equipo()
    {
        var insc = InscripcionPartida.PreinscribirEquipo(
            Guid.NewGuid(), new[] { Guid.NewGuid(), Guid.NewGuid() }, Guid.NewGuid(), T0);
        insc.Aceptar(T0);
        insc.Convocatorias[0].Aceptar(T0);

        Assert.Equal(1, insc.ConvocatoriasAceptadas);
    }
}
```

- [ ] **Step 2: Correr los tests para verlos fallar**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~InscripcionPartidaTests"`
Expected: FALLA de compilación (no existen `EstadoInscripcion.Pendiente/Rechazada`, `Aceptar`, `Rechazar`, `OcupaParticipacion`, `MiembrosSnapshot`).

- [ ] **Step 3: Anexar valores al enum**

Reemplaza `EstadoInscripcion.cs` por:

```csharp
namespace Umbral.OperacionesSesion.Domain.Enums;

// Valores explícitos: EF persiste como int. Activa/Cancelada mantienen sus valores
// históricos (0/1) para no corromper filas existentes; los nuevos se anexan.
public enum EstadoInscripcion
{
    Activa = 0,
    Cancelada = 1,
    Pendiente = 2,
    Rechazada = 3
}
```

- [ ] **Step 4: Implementar el ciclo de vida en `InscripcionPartida`**

Reemplaza `InscripcionPartida.cs` por:

```csharp
using System.Collections.Generic;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class InscripcionPartida
{
    private readonly List<Convocatoria> _convocatorias = new();
    private readonly List<Guid> _miembrosSnapshot = new();

    public InscripcionId Id { get; private set; }
    public Guid ParticipanteId { get; private set; } // Guid.Empty en modalidad Equipo
    public Modalidad Modalidad { get; private set; }
    public Guid? EquipoId { get; private set; }
    public EstadoInscripcion Estado { get; private set; }
    public DateTime FechaInscripcion { get; private set; }

    public IReadOnlyList<Convocatoria> Convocatorias => _convocatorias;
    public IReadOnlyList<Guid> MiembrosSnapshot => _miembrosSnapshot;

    private InscripcionPartida() { } // EF

    // Individual: nace Pendiente (requiere aprobación del operador — HU-19).
    internal InscripcionPartida(Guid participanteId, DateTime fecha)
    {
        Id = InscripcionId.New();
        ParticipanteId = participanteId;
        Modalidad = Modalidad.Individual;
        Estado = EstadoInscripcion.Pendiente;
        FechaInscripcion = fecha;
    }

    // Equipo: nace Pendiente, guarda el snapshot de miembros; las convocatorias se
    // emiten al aceptar (decisión 2 del spec).
    private InscripcionPartida(Guid equipoId, IEnumerable<Guid> miembros, DateTime fecha)
    {
        Id = InscripcionId.New();
        ParticipanteId = Guid.Empty;
        Modalidad = Modalidad.Equipo;
        EquipoId = equipoId;
        Estado = EstadoInscripcion.Pendiente;
        FechaInscripcion = fecha;
        _miembrosSnapshot.AddRange(miembros);
    }

    internal static InscripcionPartida PreinscribirEquipo(
        Guid equipoId, IEnumerable<Guid> miembros, Guid partidaId, DateTime fecha)
        => new(equipoId, miembros, fecha);

    // El operador acepta: pasa a Activa. En Equipo crea las convocatorias desde el
    // snapshot y las devuelve (para emitir ConvocatoriaCreada). Individual → lista vacía.
    internal IReadOnlyList<Convocatoria> Aceptar(DateTime now)
    {
        Estado = EstadoInscripcion.Activa;
        if (Modalidad != Modalidad.Equipo)
            return System.Array.Empty<Convocatoria>();

        var creadas = _miembrosSnapshot
            .Select(m => new Convocatoria(FechaInscripcionPartidaId(), EquipoId!.Value, m, now))
            .ToList();
        _convocatorias.AddRange(creadas);
        return creadas;
    }

    internal void Rechazar() => Estado = EstadoInscripcion.Rechazada;

    public bool EsActiva => Estado == EstadoInscripcion.Activa;
    public bool EstaPendiente => Estado == EstadoInscripcion.Pendiente;
    public bool OcupaParticipacion =>
        Estado is EstadoInscripcion.Pendiente or EstadoInscripcion.Activa;
    public int ConvocatoriasAceptadas => _convocatorias.Count(c => c.EstaAceptada);

    // La Convocatoria requiere el partidaId; no se persiste en la inscripción (deriva de
    // la sesión), así que el agregado lo inyecta al aceptar. Ver nota en Task 2.
    private Guid _partidaIdParaConvocar;
    internal void FijarPartidaIdParaConvocar(Guid partidaId) => _partidaIdParaConvocar = partidaId;
    private Guid FechaInscripcionPartidaId() => _partidaIdParaConvocar;
}
```

> **Nota de diseño (importante):** `Convocatoria` necesita el `partidaId`, que la inscripción no almacena. Para no añadir una columna redundante, el agregado `SesionPartida` inyecta el `partidaId` con `FijarPartidaIdParaConvocar(PartidaId)` **inmediatamente antes** de llamar `Aceptar(now)` (Task 2). El campo `_partidaIdParaConvocar` es transitorio (no mapeado por EF).

- [ ] **Step 5: Correr los tests hasta verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~InscripcionPartidaTests"`
Expected: PASS (6/6).

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/EstadoInscripcion.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/InscripcionPartida.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/InscripcionPartidaTests.cs
git commit -m "feat(operaciones): InscripcionPartida nace Pendiente con ciclo Aceptar/Rechazar (HU-19)"
```

---

## Task 2: Dominio — guards y transiciones en `SesionPartida`

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/InscripcionNoPendienteException.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaAprobacionTests.cs`
- Modify (regresión): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaEquipoTests.cs`, `.../SesionPartidaTests.cs` (ver Step 6)

**Interfaces:**
- Consumes: `InscripcionPartida` (Task 1): `OcupaParticipacion`, `EstaPendiente`, `Aceptar(now)`, `Rechazar()`, `FijarPartidaIdParaConvocar(partidaId)`, `MiembrosSnapshot`.
- Produces:
  - `InscripcionPartida Inscribir(Guid participanteId, bool tieneParticipacionActivaEnOtra, int inscritosActivos, DateTime fecha)` (nace `Pendiente`).
  - `InscripcionPartida PreinscribirEquipo(Guid equipoId, bool callerEsLider, IReadOnlyList<Guid> miembros, bool equipoTieneParticipacionActivaEnOtra, int equiposActivos, DateTime fecha)` (nace `Pendiente`, sin convocatorias).
  - `IReadOnlyList<Convocatoria> AceptarInscripcion(Guid inscripcionId, int inscritosActivos, DateTime now)`.
  - `(Guid InscripcionId, Guid? EquipoId) RechazarInscripcion(Guid inscripcionId, DateTime now)`.

- [ ] **Step 1: Escribir los tests de aprobación del agregado**

Crea `SesionPartidaAprobacionTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class SesionPartidaAprobacionTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Individual(int min = 1, int max = 5)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Individual, ModoInicioPartida.Manual, null, min, max,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    private static SesionPartida Equipo(int min = 1, int max = 5)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Equipo, ModoInicioPartida.Manual, null, min, max,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public void Inscribir_nace_pendiente_y_no_cuenta_como_activa()
    {
        var s = Individual();
        var insc = s.Inscribir(Guid.NewGuid(), tieneParticipacionActivaEnOtra: false, inscritosActivos: 0, T0);

        Assert.Equal(EstadoInscripcion.Pendiente, insc.Estado);
        Assert.Equal(0, s.Inscripciones.Count(i => i.EsActiva));
    }

    [Fact]
    public void Aceptar_pendiente_individual_pasa_a_activa()
    {
        var s = Individual();
        var insc = s.Inscribir(Guid.NewGuid(), false, 0, T0);

        var creadas = s.AceptarInscripcion(insc.Id.Valor, inscritosActivos: 0, T0);

        Assert.True(insc.EsActiva);
        Assert.Empty(creadas);
    }

    [Fact]
    public void Aceptar_pendiente_equipo_crea_convocatorias_con_partidaId_correcto()
    {
        var s = Equipo();
        var m1 = Guid.NewGuid();
        var insc = s.PreinscribirEquipo(Guid.NewGuid(), callerEsLider: true, new[] { m1 },
            equipoTieneParticipacionActivaEnOtra: false, equiposActivos: 0, T0);

        var creadas = s.AceptarInscripcion(insc.Id.Valor, inscritosActivos: 0, T0);

        var c = Assert.Single(creadas);
        Assert.Equal(m1, c.UsuarioId);
        Assert.Equal(s.PartidaId, c.PartidaId);
        Assert.True(insc.EsActiva);
    }

    [Fact]
    public void Aceptar_con_cupo_de_activos_lleno_lanza_CupoLleno()
    {
        var s = Individual(max: 1);
        var insc = s.Inscribir(Guid.NewGuid(), false, 0, T0);

        Assert.Throws<CupoLlenoException>(
            () => s.AceptarInscripcion(insc.Id.Valor, inscritosActivos: 1, T0));
        Assert.True(insc.EstaPendiente); // sin efecto
    }

    [Fact]
    public void Aceptar_inscripcion_inexistente_lanza_NoEncontrada()
    {
        var s = Individual();
        Assert.Throws<InscripcionNoEncontradaException>(
            () => s.AceptarInscripcion(Guid.NewGuid(), 0, T0));
    }

    [Fact]
    public void Aceptar_una_ya_activa_lanza_NoPendiente()
    {
        var s = Individual();
        var insc = s.Inscribir(Guid.NewGuid(), false, 0, T0);
        s.AceptarInscripcion(insc.Id.Valor, 0, T0);

        Assert.Throws<InscripcionNoPendienteException>(
            () => s.AceptarInscripcion(insc.Id.Valor, 0, T0));
    }

    [Fact]
    public void Rechazar_pendiente_pasa_a_rechazada_y_devuelve_equipoId()
    {
        var s = Equipo();
        var equipoId = Guid.NewGuid();
        var insc = s.PreinscribirEquipo(equipoId, true, new[] { Guid.NewGuid() }, false, 0, T0);

        var (inscId, equipo) = s.RechazarInscripcion(insc.Id.Valor, T0);

        Assert.Equal(insc.Id.Valor, inscId);
        Assert.Equal(equipoId, equipo);
        Assert.Equal(EstadoInscripcion.Rechazada, insc.Estado);
    }

    [Fact]
    public void Rechazada_no_bloquea_reinscribir_al_mismo_participante()
    {
        var s = Individual();
        var participante = Guid.NewGuid();
        var insc1 = s.Inscribir(participante, false, 0, T0);
        s.RechazarInscripcion(insc1.Id.Valor, T0);

        var insc2 = s.Inscribir(participante, false, 0, T0); // no lanza ParticipanteYaInscrito
        Assert.Equal(EstadoInscripcion.Pendiente, insc2.Estado);
    }

    [Fact]
    public void Pendiente_bloquea_reinscribir_al_mismo_participante()
    {
        var s = Individual();
        var participante = Guid.NewGuid();
        s.Inscribir(participante, false, 0, T0);

        Assert.Throws<ParticipanteYaInscritoException>(
            () => s.Inscribir(participante, false, 0, T0));
    }

    [Fact]
    public void Inscribir_con_cupo_de_activos_lleno_lanza_CupoLleno()
    {
        var s = Individual(max: 1);
        Assert.Throws<CupoLlenoException>(
            () => s.Inscribir(Guid.NewGuid(), false, inscritosActivos: 1, T0));
    }
}
```

- [ ] **Step 2: Correr para ver fallo de compilación**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~SesionPartidaAprobacionTests"`
Expected: FALLA de compilación (`AceptarInscripcion`, `RechazarInscripcion`, `InscripcionNoPendienteException` no existen).

- [ ] **Step 3: Crear la excepción `InscripcionNoPendienteException`**

Crea `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/InscripcionNoPendienteException.cs`:

```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class InscripcionNoPendienteException : Exception
{
    public InscripcionNoPendienteException(Guid inscripcionId)
        : base($"La inscripción {inscripcionId} no está pendiente de aprobación.") { }
}
```

- [ ] **Step 4: Modificar `SesionPartida`**

En `SesionPartida.cs`, cambia el guard de `Inscribir` (línea con `i.ParticipanteId == participanteId && i.EsActiva`) por `OcupaParticipacion`:

```csharp
        if (_inscripciones.Any(i => i.ParticipanteId == participanteId && i.OcupaParticipacion))
            throw new ParticipanteYaInscritoException(participanteId);
```

En `PreinscribirEquipo`, cambia el guard `i.EquipoId == equipoId && i.EsActiva` por `OcupaParticipacion`:

```csharp
        if (_inscripciones.Any(i => i.EquipoId == equipoId && i.OcupaParticipacion))
            throw new EquipoYaInscritoException(equipoId);
```

(Los ctors/factory de `InscripcionPartida` ya nacen `Pendiente` — Task 1 — así que `Inscribir`/`PreinscribirEquipo` no cambian su cuerpo de creación. El cupo sigue comparando contra `inscritosActivos`/`equiposActivos` que el handler calcula sobre `EsActiva`.)

Añade los dos métodos nuevos (después de `PreinscribirEquipo`):

```csharp
    public IReadOnlyList<Convocatoria> AceptarInscripcion(Guid inscripcionId, int inscritosActivos, DateTime now)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);

        var inscripcion = _inscripciones.FirstOrDefault(i => i.Id.Valor == inscripcionId)
            ?? throw new InscripcionNoEncontradaException(inscripcionId);
        if (!inscripcion.EstaPendiente)
            throw new InscripcionNoPendienteException(inscripcionId);
        if (inscritosActivos >= MaximosParticipacion)
            throw new CupoLlenoException(PartidaId);

        inscripcion.FijarPartidaIdParaConvocar(PartidaId);
        return inscripcion.Aceptar(now);
    }

    public (Guid InscripcionId, Guid? EquipoId) RechazarInscripcion(Guid inscripcionId, DateTime now)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);

        var inscripcion = _inscripciones.FirstOrDefault(i => i.Id.Valor == inscripcionId)
            ?? throw new InscripcionNoEncontradaException(inscripcionId);
        if (!inscripcion.EstaPendiente)
            throw new InscripcionNoPendienteException(inscripcionId);

        inscripcion.Rechazar();
        return (inscripcion.Id.Valor, inscripcion.EquipoId);
    }
```

> **Nota:** `InscripcionNoEncontradaException` ya existe con ctor `(Guid participanteId)`; se reutiliza tal cual pasándole el `inscripcionId` (el mensaje es genérico). No cambiar su firma.

- [ ] **Step 5: Correr los tests de aprobación hasta verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~SesionPartidaAprobacionTests"`
Expected: PASS.

- [ ] **Step 6: Arreglar tests de regresión que asumían nacer `Activa`**

Corre toda la suite de dominio y arregla los que rompan:

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~Domain"`

Patrón de arreglo: cualquier test que hacía `s.Inscribir(...)` o `s.PreinscribirEquipo(...)` y luego esperaba mínimos alcanzados / inicio exitoso / `EsActiva`, ahora debe **aceptar** la inscripción antes: tras `var insc = s.Inscribir(p, false, 0, T0);` añade `s.AceptarInscripcion(insc.Id.Valor, 0, T0);`. Para equipos: tras preinscribir, `s.AceptarInscripcion(insc.Id.Valor, 0, T0);` y luego aceptar convocatorias sobre `insc.Convocatorias`. Repite hasta que la suite de dominio quede verde. No cambies aserciones de reglas ajenas a la aprobación.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/InscripcionNoPendienteException.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/
git commit -m "feat(operaciones): AceptarInscripcion/RechazarInscripcion + guards Pendiente en SesionPartida (HU-19)"
```

---

## Task 3: Persistencia — mapping del snapshot, migración y queries de participación

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs`
- Create (generado): migración EF bajo `.../Infrastructure/Persistence/Migrations/`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/InscripcionAprobacionPersistenceTests.cs`

**Interfaces:**
- Consumes: `InscripcionPartida.MiembrosSnapshot` (`IReadOnlyList<Guid>` con backing field `_miembrosSnapshot`), `EstadoInscripcion` con `Pendiente`/`Activa`.
- Produces: columna `miembrossnapshot` (jsonb) en tabla `inscripciones`; queries de participación que cuentan `Pendiente + Activa`.

- [ ] **Step 1: Escribir test de persistencia (round-trip del snapshot + estado Pendiente)**

Crea `InscripcionAprobacionPersistenceTests.cs`. Sigue el patrón de los `IntegrationTests` existentes (usan Npgsql con Testcontainers **o** InMemory según el harness del proyecto — inspecciona `SesionPartidaRepositoryEquipoTests.cs` para el `CrearDbContext()`/fixture y reutilízalo). Test:

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class InscripcionAprobacionPersistenceTests // : usar el mismo fixture que SesionPartidaRepositoryEquipoTests
{
    [Fact]
    public async Task Preinscripcion_pendiente_persiste_snapshot_de_miembros()
    {
        // Arrange: crear una SesionPartida Equipo en Lobby, preinscribir un equipo (nace Pendiente),
        // guardar con el DbContext del fixture.
        // Act: recargar con GetByPartidaIdAsync.
        // Assert: la inscripción recargada está Pendiente, Convocatorias vacío, y MiembrosSnapshot
        // conserva los Guid guardados en orden.
        // (Reutilizar los helpers de arranque de SesionPartidaRepositoryEquipoTests.)
    }
}
```

> Implementa el cuerpo replicando el arranque de `SesionPartidaRepositoryEquipoTests.cs` (mismo fixture/DbContext). El aserto clave: `Assert.Equal(EstadoInscripcion.Pendiente, insc.Estado)` y `Assert.Equal(miembros, insc.MiembrosSnapshot)` tras round-trip.

- [ ] **Step 2: Correr para ver fallo**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj --filter "FullyQualifiedName~InscripcionAprobacionPersistence"`
Expected: FALLA (EF no conoce `MiembrosSnapshot` → no se persiste / excepción de modelo).

- [ ] **Step 3: Mapear el snapshot como colección primitiva**

En `OperacionesSesionDbContext.cs`, dentro de `modelBuilder.Entity<InscripcionPartida>(...)`, añade tras el mapeo de `EquipoId`:

```csharp
            entity.PrimitiveCollection<IReadOnlyList<Guid>>("MiembrosSnapshot")
                .HasColumnName("miembrossnapshot");
            entity.Navigation(x => x.Convocatorias).UsePropertyAccessMode(PropertyAccessMode.Field);
```

> Si `PrimitiveCollection` no resuelve por el tipo de la propiedad de solo-lectura, mapea el backing field: `entity.PrimitiveCollection("_miembrosSnapshot").HasColumnName("miembrossnapshot");` y añade `entity.Metadata.FindProperty("_miembrosSnapshot")` con acceso de campo. EF Core 8.0.7 soporta colecciones primitivas (jsonb en Npgsql). Mantén la línea existente de `Convocatorias` si ya estaba; no la dupliques.

- [ ] **Step 4: Generar la migración**

Run (desde la raíz):
```bash
dotnet ef migrations add SP4bAprobacionInscripciones \
  --project services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Umbral.OperacionesSesion.Infrastructure.csproj \
  --startup-project services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Umbral.OperacionesSesion.Api.csproj
```
Expected: crea una migración que añade la columna `miembrossnapshot` (jsonb, nullable) a `inscripciones`. Revisa el archivo generado: **no debe** alterar la columna `estado` (el enum sigue siendo int; anexar valores no cambia el esquema). Si la migración incluye cambios no relacionados, elimínala, corrige el modelo y regénerala.

- [ ] **Step 5: Actualizar las queries de participación del repositorio**

En `SesionPartidaRepository.cs`:

`ParticipanteTieneParticipacionActivaAsync` — cambia `i.Estado == EstadoInscripcion.Activa` por el conjunto que ocupa participación (BR-G09, decisión 4). La convocatoria aceptada sigue contando solo sobre inscripciones ya activas:

```csharp
    public Task<bool> ParticipanteTieneParticipacionActivaAsync(
        Guid participanteId, Guid exceptPartidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Where(s => s.PartidaId != exceptPartidaId
                && (s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada))
            .SelectMany(s => s.Inscripciones)
            .AnyAsync(i =>
                ((i.Estado == EstadoInscripcion.Pendiente || i.Estado == EstadoInscripcion.Activa)
                    && i.ParticipanteId == participanteId)
                || (i.Estado == EstadoInscripcion.Activa
                    && i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.Estado == EstadoConvocatoria.Aceptada)),
                cancellationToken);
```

`EquipoTieneParticipacionActivaAsync` — cuenta `Pendiente + Activa`:

```csharp
    public Task<bool> EquipoTieneParticipacionActivaAsync(
        Guid equipoId, Guid exceptPartidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Where(s => s.PartidaId != exceptPartidaId
                && (s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada))
            .SelectMany(s => s.Inscripciones)
            .AnyAsync(i => i.EquipoId == equipoId
                && (i.Estado == EstadoInscripcion.Pendiente || i.Estado == EstadoInscripcion.Activa),
                cancellationToken);
```

`GetByParticipanteActivoAsync` — para que `mi-sesion` muestre el estado `Pendiente`, incluye Pendiente del propio participante (la convocatoria aceptada sigue exigiendo inscripción activa):

```csharp
            .FirstOrDefaultAsync(
                s => s.Inscripciones.Any(i =>
                    ((i.Estado == EstadoInscripcion.Pendiente || i.Estado == EstadoInscripcion.Activa)
                        && i.ParticipanteId == participanteId)
                    || (i.Estado == EstadoInscripcion.Activa
                        && i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.Estado == EstadoConvocatoria.Aceptada))),
                cancellationToken);
```

- [ ] **Step 6: Correr integración + regresión del repo hasta verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj`
Expected: PASS (incluye el nuevo test y los `SesionPartidaRepository*Tests` existentes). Ajusta los tests de repo existentes que asumían nacer `Activa` (p.ej. escenarios de scan) aceptando la inscripción cuando el escenario requiera actividad real.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/ \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/
git commit -m "feat(operaciones): persiste snapshot de miembros + queries de participación cuentan Pendiente (HU-19)"
```

---

## Task 4: Eventos — records, publisher y routing (seam sin cablear handlers)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ParticipacionEvents.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/RabbitMqSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/SesionEventRouting.cs`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/` → `FakeSesionEventsPublisher` (añadir captura de los 3 eventos)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/Messaging/SesionEventRoutingTests.cs` (extender si existe; si no, crear)

**Interfaces:**
- Produces:
  - `record InscripcionSolicitadaEvent(Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, string Modalidad, Guid? ParticipanteId, Guid? EquipoId, DateTime Instante)`
  - `record InscripcionAceptadaEvent(...)` y `record InscripcionRechazadaEvent(...)` con la **misma** forma.
  - `ISesionEventsPublisher`: `PublicarInscripcionSolicitadaAsync`, `PublicarInscripcionAceptadaAsync`, `PublicarInscripcionRechazadaAsync`.
  - Routing keys: `operaciones-sesion.inscripcion-solicitada.v1`, `...-aceptada.v1`, `...-rechazada.v1`.

- [ ] **Step 1: Escribir/extender el test de routing**

En `SesionEventRoutingTests.cs` (crea si no existe, en `UnitTests/Infrastructure/Messaging/`) añade:

```csharp
using Umbral.OperacionesSesion.Infrastructure.Services.Messaging;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure.Messaging;

public class SesionEventRoutingInscripcionTests
{
    [Theory]
    [InlineData("InscripcionSolicitada", "operaciones-sesion.inscripcion-solicitada.v1")]
    [InlineData("InscripcionAceptada", "operaciones-sesion.inscripcion-aceptada.v1")]
    [InlineData("InscripcionRechazada", "operaciones-sesion.inscripcion-rechazada.v1")]
    public void RoutingKeyFor_mapea_los_eventos_de_aprobacion(string eventType, string expected)
        => Assert.Equal(expected, SesionEventRouting.RoutingKeyFor(eventType));
}
```

- [ ] **Step 2: Correr para ver fallo**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~SesionEventRoutingInscripcion"`
Expected: FALLA (`KeyNotFoundException` — claves no registradas).

- [ ] **Step 3: Añadir los records de evento**

En `ParticipacionEvents.cs`, añade al final:

```csharp
public sealed record InscripcionSolicitadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, string Modalidad,
    Guid? ParticipanteId, Guid? EquipoId, DateTime Instante);

public sealed record InscripcionAceptadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, string Modalidad,
    Guid? ParticipanteId, Guid? EquipoId, DateTime Instante);

public sealed record InscripcionRechazadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, string Modalidad,
    Guid? ParticipanteId, Guid? EquipoId, DateTime Instante);
```

- [ ] **Step 4: Añadir los 3 métodos a la interfaz y a los 4 publishers**

En `ISesionEventsPublisher.cs` añade:

```csharp
    Task PublicarInscripcionSolicitadaAsync(InscripcionSolicitadaEvent evento, CancellationToken cancellationToken);
    Task PublicarInscripcionAceptadaAsync(InscripcionAceptadaEvent evento, CancellationToken cancellationToken);
    Task PublicarInscripcionRechazadaAsync(InscripcionRechazadaEvent evento, CancellationToken cancellationToken);
```

En `CompositeSesionEventsPublisher.cs` (patrón `FanOut`):

```csharp
    public Task PublicarInscripcionSolicitadaAsync(InscripcionSolicitadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarInscripcionSolicitadaAsync(evento, cancellationToken));
    public Task PublicarInscripcionAceptadaAsync(InscripcionAceptadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarInscripcionAceptadaAsync(evento, cancellationToken));
    public Task PublicarInscripcionRechazadaAsync(InscripcionRechazadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarInscripcionRechazadaAsync(evento, cancellationToken));
```

En `RabbitMqSesionEventsPublisher.cs` (patrón `Publicar`):

```csharp
    public Task PublicarInscripcionSolicitadaAsync(InscripcionSolicitadaEvent evento, CancellationToken cancellationToken) => Publicar("InscripcionSolicitada", evento);
    public Task PublicarInscripcionAceptadaAsync(InscripcionAceptadaEvent evento, CancellationToken cancellationToken) => Publicar("InscripcionAceptada", evento);
    public Task PublicarInscripcionRechazadaAsync(InscripcionRechazadaEvent evento, CancellationToken cancellationToken) => Publicar("InscripcionRechazada", evento);
```

En `NoOpSesionEventsPublisher.cs`:

```csharp
    public Task PublicarInscripcionSolicitadaAsync(InscripcionSolicitadaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublicarInscripcionAceptadaAsync(InscripcionAceptadaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublicarInscripcionRechazadaAsync(InscripcionRechazadaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
```

En `SignalRSesionEventsPublisher.cs` (No difunden — el operador pollea, coherente con SP-3f-2):

```csharp
    // No difunden: el lobby del operador se refresca por polling (SP-3f-2). Feed solo historial vía RabbitMQ.
    public Task PublicarInscripcionSolicitadaAsync(InscripcionSolicitadaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublicarInscripcionAceptadaAsync(InscripcionAceptadaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublicarInscripcionRechazadaAsync(InscripcionRechazadaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
```

- [ ] **Step 5: Registrar las 3 routing keys**

En `SesionEventRouting.cs`, dentro del diccionario `Keys`, añade:

```csharp
        ["InscripcionSolicitada"] = "operaciones-sesion.inscripcion-solicitada.v1",
        ["InscripcionAceptada"] = "operaciones-sesion.inscripcion-aceptada.v1",
        ["InscripcionRechazada"] = "operaciones-sesion.inscripcion-rechazada.v1",
```

- [ ] **Step 6: Extender el `FakeSesionEventsPublisher` de tests**

En `tests/.../Application/Fakes/` (archivo del fake; inspecciona su nombre — está junto a `FakeSesionPartidaRepository`), añade listas de captura y las 3 implementaciones:

```csharp
    public List<InscripcionSolicitadaEvent> InscripcionesSolicitadas { get; } = new();
    public List<InscripcionAceptadaEvent> InscripcionesAceptadas { get; } = new();
    public List<InscripcionRechazadaEvent> InscripcionesRechazadas { get; } = new();

    public Task PublicarInscripcionSolicitadaAsync(InscripcionSolicitadaEvent evento, CancellationToken cancellationToken)
    { InscripcionesSolicitadas.Add(evento); return Task.CompletedTask; }
    public Task PublicarInscripcionAceptadaAsync(InscripcionAceptadaEvent evento, CancellationToken cancellationToken)
    { InscripcionesAceptadas.Add(evento); return Task.CompletedTask; }
    public Task PublicarInscripcionRechazadaAsync(InscripcionRechazadaEvent evento, CancellationToken cancellationToken)
    { InscripcionesRechazadas.Add(evento); return Task.CompletedTask; }
```

- [ ] **Step 7: Compilar y correr routing + suite unit**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~SesionEventRoutingInscripcion"`
Expected: PASS. Luego compila el resto: `dotnet build services/operaciones-sesion/Umbral.OperacionesSesion.sln` → sin errores (todas las implementaciones de la interfaz completas).

- [ ] **Step 8: Commit**

```bash
git add services/operaciones-sesion/src services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests
git commit -m "feat(operaciones): eventos InscripcionSolicitada/Aceptada/Rechazada + routing (HU-19)"
```

---

## Task 5: Application — comandos y handlers Aceptar/Rechazar

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/AceptarInscripcionCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/RechazarInscripcionCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/AceptarInscripcionCommandHandler.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/RechazarInscripcionCommandHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/AceptarInscripcionCommandHandlerTests.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/RechazarInscripcionCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ISesionPartidaRepository.GetByPartidaIdAsync`, `IOperacionesSesionUnitOfWork.SaveChangesAsync`, `ISesionEventsPublisher` (Task 4), `SesionPartida.AceptarInscripcion/RechazarInscripcion` (Task 2), `LobbyDto`+`MapearLobby` (Task 7 lo amplía; aquí se usa la firma actual — ver nota).
- Produces:
  - `record AceptarInscripcionCommand(Guid PartidaId, Guid InscripcionId) : IRequest<LobbyDto>`
  - `record RechazarInscripcionCommand(Guid PartidaId, Guid InscripcionId) : IRequest<LobbyDto>`

> **Orden con Task 7:** este handler devuelve `LobbyDto` vía `PublicarPartidaCommandHandler.MapearLobby(sesion)`. Task 7 amplía `LobbyDto`/`MapearLobby` con las listas de pendientes. Para no acoplar el orden, este handler **solo** llama `MapearLobby(sesion)` (sea cual sea su forma en ese momento); los tests de este handler no afirman sobre las listas de pendientes (eso lo cubre Task 7).

- [ ] **Step 1: Escribir los tests de handler**

Crea `AceptarInscripcionCommandHandlerTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class AceptarInscripcionCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida IndividualEnLobby(Guid partidaId)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(partidaId, snap);
    }

    [Fact]
    public async Task Acepta_pendiente_y_publica_InscripcionAceptada()
    {
        var partidaId = Guid.NewGuid();
        var sesion = IndividualEnLobby(partidaId);
        var insc = sesion.Inscribir(Guid.NewGuid(), false, 0, T0); // Pendiente
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new AceptarInscripcionCommandHandler(
            repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await handler.Handle(new AceptarInscripcionCommand(partidaId, insc.Id.Valor), default);

        Assert.True(insc.EsActiva);
        var e = Assert.Single(events.InscripcionesAceptadas);
        Assert.Equal(insc.Id.Valor, e.InscripcionId);
        Assert.Equal("Individual", e.Modalidad);
        Assert.Empty(events.ConvocatoriasCreadas); // individual no convoca
    }

    [Fact]
    public async Task Acepta_equipo_publica_una_ConvocatoriaCreada_por_miembro()
    {
        var partidaId = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { m1, m2 }, false, 0, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new AceptarInscripcionCommandHandler(
            repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await handler.Handle(new AceptarInscripcionCommand(partidaId, insc.Id.Valor), default);

        Assert.Equal(2, events.ConvocatoriasCreadas.Count);
        Assert.Contains(events.ConvocatoriasCreadas, c => c.UsuarioId == m1);
        Assert.Single(events.InscripcionesAceptadas);
    }

    [Fact]
    public async Task Sesion_inexistente_lanza()
    {
        var repo = new FakeSesionPartidaRepository();
        var handler = new AceptarInscripcionCommandHandler(
            repo, new FakeSesionEventsPublisher(), new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new AceptarInscripcionCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }
}
```

Crea `RechazarInscripcionCommandHandlerTests.cs` (análogo):

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class RechazarInscripcionCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Rechaza_equipo_publica_InscripcionRechazada_y_InscripcionEquipoCancelada()
    {
        var partidaId = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var equipoId = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(equipoId, true, new[] { Guid.NewGuid() }, false, 0, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new RechazarInscripcionCommandHandler(
            repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await handler.Handle(new RechazarInscripcionCommand(partidaId, insc.Id.Valor), default);

        Assert.Equal(EstadoInscripcion.Rechazada, insc.Estado);
        var rech = Assert.Single(events.InscripcionesRechazadas);
        Assert.Equal(equipoId, rech.EquipoId);
        var cancel = Assert.Single(events.InscripcionesEquipoCanceladas);
        Assert.Equal(equipoId, cancel.EquipoId);
    }

    [Fact]
    public async Task Rechaza_individual_no_publica_InscripcionEquipoCancelada()
    {
        var partidaId = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var insc = sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new RechazarInscripcionCommandHandler(
            repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await handler.Handle(new RechazarInscripcionCommand(partidaId, insc.Id.Valor), default);

        Assert.Single(events.InscripcionesRechazadas);
        Assert.Empty(events.InscripcionesEquipoCanceladas);
    }
}
```

- [ ] **Step 2: Correr para ver fallo de compilación**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~AceptarInscripcionCommandHandlerTests|FullyQualifiedName~RechazarInscripcionCommandHandlerTests"`
Expected: FALLA (comandos y handlers no existen).

- [ ] **Step 3: Crear los comandos**

`AceptarInscripcionCommand.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record AceptarInscripcionCommand(Guid PartidaId, Guid InscripcionId) : IRequest<LobbyDto>;
```

`RechazarInscripcionCommand.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record RechazarInscripcionCommand(Guid PartidaId, Guid InscripcionId) : IRequest<LobbyDto>;
```

- [ ] **Step 4: Crear el handler de aceptar**

`AceptarInscripcionCommandHandler.cs`:

```csharp
using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class AceptarInscripcionCommandHandler : IRequestHandler<AceptarInscripcionCommand, LobbyDto>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly ISesionEventsPublisher _events;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public AceptarInscripcionCommandHandler(
        ISesionPartidaRepository sesiones, ISesionEventsPublisher events,
        IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<LobbyDto> Handle(AceptarInscripcionCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var inscripcion = sesion.Inscripciones.FirstOrDefault(i => i.Id.Valor == request.InscripcionId);
        var inscritosActivos = inscripcion is { Modalidad: Modalidad.Equipo }
            ? sesion.Inscripciones.Count(i => i.Modalidad == Modalidad.Equipo && i.EsActiva)
            : sesion.Inscripciones.Count(i => i.Modalidad == Modalidad.Individual && i.EsActiva);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var convocatorias = sesion.AceptarInscripcion(request.InscripcionId, inscritosActivos, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var aceptada = sesion.Inscripciones.First(i => i.Id.Valor == request.InscripcionId);
        var esEquipo = aceptada.Modalidad == Modalidad.Equipo;

        foreach (var c in convocatorias)
        {
            await _events.PublicarConvocatoriaCreadaAsync(
                new ConvocatoriaCreadaEvent(sesion.PartidaId, sesion.Id.Valor, c.Id.Valor, c.EquipoId, c.UsuarioId),
                cancellationToken);
        }

        await _events.PublicarInscripcionAceptadaAsync(
            new InscripcionAceptadaEvent(
                sesion.PartidaId, sesion.Id.Valor, aceptada.Id.Valor, aceptada.Modalidad.ToString(),
                esEquipo ? null : aceptada.ParticipanteId, esEquipo ? aceptada.EquipoId : null, now),
            cancellationToken);

        return PublicarPartidaCommandHandler.MapearLobby(sesion);
    }
}
```

- [ ] **Step 5: Crear el handler de rechazar**

`RechazarInscripcionCommandHandler.cs`:

```csharp
using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class RechazarInscripcionCommandHandler : IRequestHandler<RechazarInscripcionCommand, LobbyDto>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly ISesionEventsPublisher _events;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RechazarInscripcionCommandHandler(
        ISesionPartidaRepository sesiones, ISesionEventsPublisher events,
        IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<LobbyDto> Handle(RechazarInscripcionCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var inscripcion = sesion.Inscripciones.FirstOrDefault(i => i.Id.Valor == request.InscripcionId);
        var esEquipo = inscripcion is { Modalidad: Modalidad.Equipo };
        var participanteId = inscripcion?.ParticipanteId;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var (inscId, equipoId) = sesion.RechazarInscripcion(request.InscripcionId, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarInscripcionRechazadaAsync(
            new InscripcionRechazadaEvent(
                sesion.PartidaId, sesion.Id.Valor, inscId, esEquipo ? "Equipo" : "Individual",
                esEquipo ? null : participanteId, equipoId, now),
            cancellationToken);

        if (esEquipo && equipoId is { } eq)
        {
            await _events.PublicarInscripcionEquipoCanceladaAsync(
                new InscripcionEquipoCanceladaEvent(sesion.PartidaId, inscId, eq, now),
                cancellationToken);
        }

        return PublicarPartidaCommandHandler.MapearLobby(sesion);
    }
}
```

- [ ] **Step 6: Correr los tests de handler hasta verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~AceptarInscripcionCommandHandlerTests|FullyQualifiedName~RechazarInscripcionCommandHandlerTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/AceptarInscripcionCommandHandler.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/RechazarInscripcionCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application
git commit -m "feat(operaciones): handlers Aceptar/Rechazar inscripción con eventos (HU-19)"
```

---

## Task 6: Application — inscribir/preinscribir emiten `InscripcionSolicitada`; preinscribir difiere convocatorias

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/InscribirParticipanteCommandHandler.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PreinscribirEquipoCommandHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/PreinscribirEquipoCommandHandlerTests.cs` (actualizar)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/InscribirParticipanteCommandHandlerTests.cs` (crear o actualizar si existe)

**Interfaces:**
- Consumes: `ISesionEventsPublisher.PublicarInscripcionSolicitadaAsync` (Task 4). `InscribirParticipanteCommandHandler` gana dependencias `ISesionEventsPublisher` + `TimeProvider` (hoy no las tiene).

- [ ] **Step 1: Actualizar el test de preinscripción**

En `PreinscribirEquipoCommandHandlerTests.cs`, cambia el test `Preinscribe_y_publica_una_convocatoria_creada_por_miembro` para el nuevo contrato: preinscribir **ya NO** emite `ConvocatoriaCreada` (se difieren al aceptar), sí emite `InscripcionSolicitada` e `InscripcionEquipoCreada`, y `resp.Convocados == 0`:

```csharp
    [Fact]
    public async Task Preinscribe_pendiente_publica_solicitada_y_equipo_creada_sin_convocatorias()
    {
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        var repo = new FakeSesionPartidaRepository();
        repo.Add(PartidaEquipoEnLobby(partidaId));
        var directory = new FakeEquipoDirectoryClient
        {
            Equipo = new EquipoSnapshotDto(equipoId, "Halcones",
                new List<MiembroEquipoDto> { new(lider, true), new(miembro, false) })
        };
        var events = new FakeSesionEventsPublisher();
        var handler = new PreinscribirEquipoCommandHandler(
            repo, directory, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        var resp = await handler.Handle(new PreinscribirEquipoCommand(partidaId, lider, "Bearer x"), default);

        Assert.Equal(equipoId, resp.EquipoId);
        Assert.Equal(0, resp.Convocados);                 // convocatorias diferidas a la aceptación
        Assert.Empty(events.ConvocatoriasCreadas);        // no se convoca aún
        var solicitada = Assert.Single(events.InscripcionesSolicitadas);
        Assert.Equal(equipoId, solicitada.EquipoId);
        Assert.Equal("Equipo", solicitada.Modalidad);
        var creada = Assert.Single(events.InscripcionesEquipoCreadas);
        Assert.Equal(equipoId, creada.EquipoId);
    }
```

Mantén `Caller_no_es_lider_lanza_sin_publicar`, `Sin_equipo_activo_lanza`, `Sesion_inexistente_lanza` (ajusta el aserto `Assert.Empty(events.ConvocatoriasCreadas)` que ya es correcto).

- [ ] **Step 2: Correr para ver fallo**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~PreinscribirEquipoCommandHandlerTests"`
Expected: FALLA (el handler aún emite convocatorias y no emite `InscripcionSolicitada`).

- [ ] **Step 3: Modificar `PreinscribirEquipoCommandHandler`**

Reemplaza el bloque desde `await _unitOfWork.SaveChangesAsync(...)` hasta el `return` por (quita el `foreach` de convocatorias, añade `InscripcionSolicitada`, conserva `InscripcionEquipoCreada`):

```csharp
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarInscripcionSolicitadaAsync(
            new InscripcionSolicitadaEvent(
                sesion.PartidaId, sesion.Id.Valor, inscripcion.Id.Valor, Modalidad.Equipo.ToString(),
                null, equipo.EquipoId, now),
            cancellationToken);

        await _events.PublicarInscripcionEquipoCreadaAsync(
            new InscripcionEquipoCreadaEvent(
                sesion.PartidaId, sesion.Id.Valor, inscripcion.Id.Valor, equipo.EquipoId, now),
            cancellationToken);

        return new PreinscripcionEquipoResponse(inscripcion.Id.Valor, equipo.EquipoId, inscripcion.Convocatorias.Count);
```

(`inscripcion.Convocatorias.Count` es 0 ahora — la preinscripción no convoca. `now` ya está declarado arriba en el handler.)

- [ ] **Step 4: Añadir emisión de `InscripcionSolicitada` en el flujo individual**

Modifica `InscribirParticipanteCommandHandler.cs` para inyectar `ISesionEventsPublisher` y `TimeProvider` y emitir el evento. Reemplaza la clase por:

```csharp
using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class InscribirParticipanteCommandHandler : IRequestHandler<InscribirParticipanteCommand, InscripcionResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly ISesionEventsPublisher _events;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public InscribirParticipanteCommandHandler(
        ISesionPartidaRepository sesiones, ISesionEventsPublisher events,
        IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<InscripcionResponse> Handle(InscribirParticipanteCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var activaEnOtra = await _sesiones.ParticipanteTieneParticipacionActivaAsync(
            request.ParticipanteId, request.PartidaId, cancellationToken);
        var inscritosActivos = sesion.Inscripciones.Count(i => i.EsActiva);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var inscripcion = sesion.Inscribir(request.ParticipanteId, activaEnOtra, inscritosActivos, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarInscripcionSolicitadaAsync(
            new InscripcionSolicitadaEvent(
                sesion.PartidaId, sesion.Id.Valor, inscripcion.Id.Valor, Modalidad.Individual.ToString(),
                request.ParticipanteId, null, now),
            cancellationToken);

        return new InscripcionResponse(inscripcion.Id.Valor, request.PartidaId, request.ParticipanteId);
    }
}
```

- [ ] **Step 5: Actualizar/crear el test del handler individual**

Si existe `InscribirParticipanteCommandHandlerTests.cs`, actualiza los ctors del handler a la nueva firma `(repo, events, uow, timeProvider)`. Si no existe, créalo con un test:

```csharp
    [Fact]
    public async Task Inscribe_pendiente_y_publica_InscripcionSolicitada()
    {
        var partidaId = Guid.NewGuid();
        var sesion = /* SesionPartida Individual en Lobby, ver helper de otros tests */;
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var participante = Guid.NewGuid();
        var handler = new InscribirParticipanteCommandHandler(
            repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        var resp = await handler.Handle(new InscribirParticipanteCommand(partidaId, participante), default);

        var e = Assert.Single(events.InscripcionesSolicitadas);
        Assert.Equal("Individual", e.Modalidad);
        Assert.Equal(participante, e.ParticipanteId);
    }
```

- [ ] **Step 6: Verificar el registro DI del handler individual**

Los handlers se registran por MediatR assembly-scan (no requieren registro manual). Verifica que `InscribirParticipanteCommandHandler` resuelva sus nuevas dependencias: `ISesionEventsPublisher` y `TimeProvider` ya están registrados en el `Program.cs`/DI del Api (los usan otros handlers). No hace falta cambio de DI. Confírmalo con el build.

- [ ] **Step 7: Correr suite unit completa hasta verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS (arregla cualquier test que instancie `InscribirParticipanteCommandHandler` con la firma vieja).

- [ ] **Step 8: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/InscribirParticipanteCommandHandler.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PreinscribirEquipoCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application
git commit -m "feat(operaciones): inscribir/preinscribir emiten InscripcionSolicitada; convocatorias diferidas al aceptar (HU-19)"
```

---

## Task 7: Application — `LobbyDto` con pendientes + `mi-sesion` con `OcupaParticipacion`

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/LobbyDto.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PublicarPartidaCommandHandler.cs` (`MapearLobby`)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerLobbyQueryHandlerTests.cs` (extender)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerMiSesionQueryHandlerPendienteTests.cs` (crear)

**Interfaces:**
- Produces: `LobbyDto` con `IReadOnlyList<SolicitudIndividualDto> SolicitudesPendientesIndividual` y `IReadOnlyList<SolicitudEquipoDto> SolicitudesPendientesEquipo`; records `SolicitudIndividualDto(Guid InscripcionId, Guid ParticipanteId, DateTime FechaInscripcion)`, `SolicitudEquipoDto(Guid InscripcionId, Guid EquipoId, int Miembros, DateTime FechaInscripcion)`.

- [ ] **Step 1: Escribir el test de lobby con pendientes**

Añade a `ObtenerLobbyQueryHandlerTests.cs` un test: una sesión con una inscripción individual `Pendiente`, una individual `Activa`, y una preinscripción de equipo `Pendiente` debe reportar `InscritosActivos==1`, `SolicitudesPendientesIndividual` con 1 elemento (el pendiente), y `SolicitudesPendientesEquipo` con 1 (con `Miembros` = tamaño del snapshot):

```csharp
    [Fact]
    public async Task Lobby_separa_activos_de_solicitudes_pendientes()
    {
        var partidaId = Guid.NewGuid();
        var sesion = /* SesionPartida Individual en Lobby con min1/max5, ver helpers */;
        var pAct = sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.AceptarInscripcion(pAct.Id.Valor, 0, T0);         // Activa
        var pPend = sesion.Inscribir(Guid.NewGuid(), false, 1, T0); // Pendiente
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerLobbyQueryHandler(repo);

        var lobby = await handler.Handle(new ObtenerLobbyQuery(partidaId), default);

        Assert.Equal(1, lobby.InscritosActivos);
        var pendiente = Assert.Single(lobby.SolicitudesPendientesIndividual);
        Assert.Equal(pPend.Id.Valor, pendiente.InscripcionId);
    }
```

> Para el caso equipo, usa una sesión Equipo, `PreinscribirEquipo(...)` (queda `Pendiente`) y afirma `Assert.Single(lobby.SolicitudesPendientesEquipo)` con `Miembros == <n>`.

- [ ] **Step 2: Correr para ver fallo de compilación**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~ObtenerLobbyQueryHandlerTests"`
Expected: FALLA (`SolicitudesPendientesIndividual` no existe).

- [ ] **Step 3: Ampliar `LobbyDto`**

Reemplaza `LobbyDto.cs` por:

```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record LobbyDto(
    Guid PartidaId,
    Guid SesionPartidaId,
    string Estado,
    string Modalidad,
    int MinimosParticipacion,
    int MaximosParticipacion,
    int InscritosActivos,
    IReadOnlyList<Guid> Participantes,
    IReadOnlyList<EquipoLobbyDto> Equipos,
    IReadOnlyList<SolicitudIndividualDto> SolicitudesPendientesIndividual,
    IReadOnlyList<SolicitudEquipoDto> SolicitudesPendientesEquipo);

public sealed record EquipoLobbyDto(Guid EquipoId, int Convocados, int Aceptados);

public sealed record SolicitudIndividualDto(Guid InscripcionId, Guid ParticipanteId, DateTime FechaInscripcion);

public sealed record SolicitudEquipoDto(Guid InscripcionId, Guid EquipoId, int Miembros, DateTime FechaInscripcion);
```

- [ ] **Step 4: Actualizar `MapearLobby`**

Reemplaza el cuerpo de `MapearLobby` en `PublicarPartidaCommandHandler.cs` por:

```csharp
    internal static LobbyDto MapearLobby(SesionPartida sesion)
    {
        var activas = sesion.Inscripciones.Where(i => i.EsActiva).ToList();
        return new LobbyDto(
            sesion.PartidaId,
            sesion.Id.Valor,
            sesion.Estado.ToString(),
            sesion.Modalidad.ToString(),
            sesion.MinimosParticipacion,
            sesion.MaximosParticipacion,
            activas.Count,
            activas.Where(i => i.Modalidad == Modalidad.Individual).Select(i => i.ParticipanteId).ToList(),
            sesion.Inscripciones
                .Where(i => i.Modalidad == Modalidad.Equipo && i.EsActiva && i.EquipoId is not null)
                .Select(i => new EquipoLobbyDto(i.EquipoId!.Value, i.Convocatorias.Count, i.ConvocatoriasAceptadas))
                .ToList(),
            sesion.Inscripciones
                .Where(i => i.Modalidad == Modalidad.Individual && i.EstaPendiente)
                .Select(i => new SolicitudIndividualDto(i.Id.Valor, i.ParticipanteId, i.FechaInscripcion))
                .ToList(),
            sesion.Inscripciones
                .Where(i => i.Modalidad == Modalidad.Equipo && i.EstaPendiente && i.EquipoId is not null)
                .Select(i => new SolicitudEquipoDto(i.Id.Valor, i.EquipoId!.Value, i.MiembrosSnapshot.Count, i.FechaInscripcion))
                .ToList());
    }
```

(Añadí `.Where(i => i.Modalidad == Modalidad.Individual)` al listado de `Participantes` activos: antes tomaba `ParticipanteId` de todas las activas; en Equipo `ParticipanteId` es `Guid.Empty`, así que filtrar evita colar ceros. Verifica que ningún test dependa del comportamiento previo.)

- [ ] **Step 5: `mi-sesion` con `OcupaParticipacion`**

En `ObtenerMiSesionQueryHandler.cs`, cambia el filtro de la inscripción propia (línea `i => i.EsActiva && i.ParticipanteId == request.ParticipanteId`) por:

```csharp
        var inscripcion = sesion.Inscripciones.FirstOrDefault(
            i => i.OcupaParticipacion && i.ParticipanteId == request.ParticipanteId);
```

(El repo `GetByParticipanteActivoAsync` ya devuelve la sesión con la inscripción `Pendiente` — Task 3. `InscripcionResumenDto.Estado` transportará `"Pendiente"`.)

- [ ] **Step 6: Test de `mi-sesion` en estado Pendiente**

Crea `ObtenerMiSesionQueryHandlerPendienteTests.cs`: preinscribe/inscribe un participante individual (queda `Pendiente`), configura el `FakeSesionPartidaRepository` para que `GetByParticipanteActivoAsync` devuelva esa sesión, invoca el handler y afirma `dto.Inscripcion.Estado == "Pendiente"`.

> Nota: el `FakeSesionPartidaRepository` debe implementar `GetByParticipanteActivoAsync` incluyendo Pendiente (mismo criterio `OcupaParticipacion`). Si el fake filtra por `EsActiva`, actualízalo a `OcupaParticipacion` para reflejar el repo real.

- [ ] **Step 7: Correr suite unit hasta verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS (ajusta `ObtenerLobbyQueryHandlerTests` y cualquier test que construya `LobbyDto` con la firma vieja — ahora requiere las 2 listas nuevas).

- [ ] **Step 8: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/LobbyDto.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PublicarPartidaCommandHandler.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application
git commit -m "feat(operaciones): LobbyDto expone solicitudes pendientes; mi-sesion muestra estado Pendiente (HU-19)"
```

---

## Task 8: Api — endpoints de operador aceptar/rechazar + tests de controlador

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Middleware/ExceptionHandlingMiddleware.cs` (mapear `InscripcionNoPendienteException`)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerAprobacionTests.cs`

**Interfaces:**
- Consumes: `AceptarInscripcionCommand`, `RechazarInscripcionCommand` (Task 5), `LobbyDto`.

- [ ] **Step 1: Escribir los tests de controlador**

Crea `SesionesControllerAprobacionTests.cs`. Sigue el patrón de `SesionesControllerEquipoTests.cs` (mock/fake de `ISender`, se construye el controller y se verifica que despacha el comando correcto y devuelve `OkObjectResult`). Ejemplo:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Umbral.OperacionesSesion.Api.Controllers;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerAprobacionTests
{
    private static LobbyDto LobbyVacio(Guid partidaId) => new(
        partidaId, Guid.NewGuid(), "Lobby", "Individual", 1, 5, 0,
        Array.Empty<Guid>(), Array.Empty<EquipoLobbyDto>(),
        Array.Empty<SolicitudIndividualDto>(), Array.Empty<SolicitudEquipoDto>());

    [Fact]
    public async Task Aceptar_despacha_comando_y_devuelve_lobby()
    {
        var partidaId = Guid.NewGuid();
        var inscripcionId = Guid.NewGuid();
        var mediator = new Mock<ISender>();
        mediator.Setup(m => m.Send(It.IsAny<AceptarInscripcionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LobbyVacio(partidaId));
        var controller = new SesionesController(mediator.Object);

        var result = await controller.AceptarInscripcion(partidaId, inscripcionId, default);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<LobbyDto>(ok.Value);
        mediator.Verify(m => m.Send(
            It.Is<AceptarInscripcionCommand>(c => c.PartidaId == partidaId && c.InscripcionId == inscripcionId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rechazar_despacha_comando_y_devuelve_lobby()
    {
        var partidaId = Guid.NewGuid();
        var inscripcionId = Guid.NewGuid();
        var mediator = new Mock<ISender>();
        mediator.Setup(m => m.Send(It.IsAny<RechazarInscripcionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LobbyVacio(partidaId));
        var controller = new SesionesController(mediator.Object);

        var result = await controller.RechazarInscripcion(partidaId, inscripcionId, default);

        var ok = Assert.IsType<OkObjectResult>(result);
        mediator.Verify(m => m.Send(
            It.Is<RechazarInscripcionCommand>(c => c.PartidaId == partidaId && c.InscripcionId == inscripcionId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

> Si los tests de controlador existentes no usan Moq sino un fake de `ISender`, replica ese estilo (inspecciona `SesionesControllerEquipoTests.cs`). Mantén consistencia con la suite.

- [ ] **Step 2: Correr para ver fallo de compilación**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~SesionesControllerAprobacionTests"`
Expected: FALLA (métodos `AceptarInscripcion`/`RechazarInscripcion` no existen en el controller).

- [ ] **Step 3: Añadir los endpoints al controller**

En `SesionesController.cs`, tras el endpoint `CancelarInscripcionEquipo`, añade:

```csharp
    [Authorize(Policy = "GestionarPartidas")]
    [HttpPost("partidas/{partidaId:guid}/inscripciones/{inscripcionId:guid}/aceptacion")]
    public async Task<IActionResult> AceptarInscripcion(Guid partidaId, Guid inscripcionId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new AceptarInscripcionCommand(partidaId, inscripcionId), cancellationToken));

    [Authorize(Policy = "GestionarPartidas")]
    [HttpPost("partidas/{partidaId:guid}/inscripciones/{inscripcionId:guid}/rechazo")]
    public async Task<IActionResult> RechazarInscripcion(Guid partidaId, Guid inscripcionId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new RechazarInscripcionCommand(partidaId, inscripcionId), cancellationToken));
```

- [ ] **Step 4: Mapear la nueva excepción a 409**

En `ExceptionHandlingMiddleware.cs`, añade `InscripcionNoPendienteException` a la rama de `HttpStatusCode.Conflict` (junto a `CupoLlenoException`):

```csharp
            or CupoLlenoException
            or InscripcionNoPendienteException
```

Y añade el `using Umbral.OperacionesSesion.Domain.Exceptions;` si no está (ya está — `SesionNoEnLobbyException` vive ahí). `InscripcionNoEncontradaException` ya mapea a 404 (rama existente).

- [ ] **Step 5: Correr los tests de controlador hasta verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~SesionesControllerAprobacionTests"`
Expected: PASS.

- [ ] **Step 6: Correr la solución completa**

Run: `dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln`
Expected: PASS (Unit + Integration + Contract). Arregla cualquier residuo de firmas cambiadas.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api
git commit -m "feat(operaciones): endpoints operador aceptar/rechazar inscripción (HU-19)"
```

---

## Task 9: Contratos y trazabilidad

**Files:**
- Modify: `contracts/events/operaciones-sesion-events.md`
- Modify: `contracts/http/operaciones-sesion-api.md`
- Modify: `docs/04-sdd/traceability-matrix.md`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/` (extender si hay tests de forma de evento; ver Step 3)

- [ ] **Step 1: Registrar los 3 eventos en el contrato de eventos**

En `contracts/events/operaciones-sesion-events.md`, en la tabla **Event Registry** añade 3 filas (`InscripcionSolicitada`, `InscripcionAceptada`, `InscripcionRechazada`), en la tabla de **Routing keys** añade las 3 claves (`operaciones-sesion.inscripcion-solicitada.v1`, `...-aceptada.v1`, `...-rechazada.v1`), y en **Payloads** añade la forma común:

```json
{ "partidaId": "guid", "sesionPartidaId": "guid", "inscripcionId": "guid", "modalidad": "Individual | Equipo", "participanteId": "guid | null", "equipoId": "guid | null", "instante": "datetime" }
```

Añade una **nota de timing** (HU-19): `ConvocatoriaCreada` ahora se emite **al aceptar** una preinscripción de equipo (no al preinscribir); `InscripcionEquipoCreada` se sigue emitiendo al preinscribir (equipo `Pendiente` ya participa, BR-G09/BR-E10); `InscripcionEquipoCancelada` se emite también al **rechazar**. Los 3 eventos nuevos no difunden por SignalR (el operador pollea) y se archivan en el historial de Puntuaciones vía la cola ligada a `operaciones-sesion.#` (sin consumidor nuevo).

- [ ] **Step 2: Documentar los endpoints y el `LobbyDto` en el contrato HTTP**

En `contracts/http/operaciones-sesion-api.md` añade:
- `POST /operaciones-sesion/partidas/{partidaId}/inscripciones/{inscripcionId}/aceptacion` — `GestionarPartidas`, 200 `LobbyDto`; 404 (sesión/inscripción), 409 (`CupoLleno`, `InscripcionNoPendiente`, `SesionNoEnLobby`).
- `POST /operaciones-sesion/partidas/{partidaId}/inscripciones/{inscripcionId}/rechazo` — `GestionarPartidas`, 200 `LobbyDto`; 404, 409.
- Actualiza la forma de `LobbyDto` con `solicitudesPendientesIndividual[]` (`{inscripcionId, participanteId, fechaInscripcion}`) y `solicitudesPendientesEquipo[]` (`{inscripcionId, equipoId, miembros, fechaInscripcion}`).
- Nota: inscribir/preinscribir ahora devuelven una inscripción en estado `Pendiente` (requiere aprobación del operador para contar en mínimos/cupo/juego).

- [ ] **Step 3: Verificar/añadir contract tests de forma de evento**

Inspecciona `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/`. Si hay un patrón que valida el envelope/routing de eventos (p.ej. serializa un evento y compara la routing key + campos), añade casos para los 3 eventos nuevos siguiendo ese patrón. Si no existe tal patrón, omite este paso (el routing ya está cubierto por Task 4).

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`
Expected: PASS.

- [ ] **Step 4: Actualizar la matriz de trazabilidad**

En `docs/04-sdd/traceability-matrix.md`, marca **HU-19** como cubierta (backend) por el slice 4B, referenciando el spec y este plan; nota reglas tocadas **BR-G09** (Pendiente+Activa) y **BR-E10** (timing de eventos de equipo). Deja constancia de que la **UI de clientes** queda diferida al slice de migración.

- [ ] **Step 5: Commit**

```bash
git add contracts/events/operaciones-sesion-events.md contracts/http/operaciones-sesion-api.md \
        docs/04-sdd/traceability-matrix.md \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests
git commit -m "docs(4B): contratos de eventos/HTTP y trazabilidad de HU-19 (aprobación de inscripciones)"
```

---

## Self-Review (rellenado)

**1. Cobertura del spec:**
- §1 máquina de estados → Task 1 (enum + entidad) + Task 2 (agregado).
- §2 agregado (guards, Aceptar/Rechazar, cupo al aceptar, mínimos sin cambio) → Task 2.
- §3 endpoints → Task 8; `LobbyDto` pendientes → Task 7; `mi-sesion` OcupaParticipacion → Task 7 (+ repo en Task 3).
- §4 eventos + guard BR-E10 timing + sin SignalR → Task 4 (seam) + Task 5/6 (emisión) + Task 9 (contrato).
- §5 clientes → **fuera de alcance** (diferido), documentado en spec y Task 9.
- §6 testing → tests en cada task; regresión en Task 2/3/6/7.
- Persistencia (enum int, snapshot, migración) → Task 3.

**2. Placeholders:** el único cuerpo de test no literal es Task 3 Step 1 (round-trip de persistencia) y Task 7 Step 6, deliberadamente descritos porque dependen del fixture de integración del proyecto (Testcontainers vs InMemory) que el implementador debe reutilizar del archivo hermano citado; el aserto exacto está dado. Todo lo demás es código completo.

**3. Consistencia de tipos:** `AceptarInscripcion(Guid, int, DateTime) : IReadOnlyList<Convocatoria>` y `RechazarInscripcion(Guid, DateTime) : (Guid, Guid?)` idénticos en Task 2 (dominio), Task 5 (handlers) y tests. Records de evento con forma común de 7 campos en Task 4/5/6/9. `LobbyDto` ampliado en Task 7 y usado con esa firma en Task 5 (vía `MapearLobby`) y Task 8 (constructor en tests) — consistente.

**Riesgo señalado (pre-flight):** el orden de Task 5 (handlers devuelven `MapearLobby`) antes de Task 7 (amplía `LobbyDto`) implica que al terminar Task 5 el código compila con el `LobbyDto` de 9 campos y Task 7 lo lleva a 11; los tests de Task 5 no afirman sobre pendientes, así que no rompen. Si el implementador ejecuta estrictamente en orden, entre Task 5 y Task 7 el `LobbyDto` es el viejo y todo compila. OK.
