# SP-3e-1b — Operaciones: Participación Equipo (preinscripción + convocatorias) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Habilitar la modalidad **Equipo** a nivel participación en Operaciones: el líder preinscribe su equipo (snapshot HTTP a Identity) generando convocatorias a los integrantes; cada convocado acepta/rechaza; participante-activo = convocatoria aceptada; cupo/mínimos por equipo; no-doble-participación (equipo y participante); proyecciones lobby/mi-sesión.

**Architecture:** Nuevo puerto `IEquipoDirectoryClient` (impl HTTP `IdentityEquipoHttpClient` → `GET /api/teams/mine` de SP-3e-1a; Fake en tests) para tomar un **snapshot** de miembros al preinscribir. Entidad hija `Convocatoria` bajo `InscripcionPartida` (extendida con `Modalidad`/`EquipoId`). El agregado `SesionPartida` gana `PreinscribirEquipo`/`ResponderConvocatoria`/`CancelarInscripcionEquipo` y cuenta mínimos Equipo (equipos con ≥1 aceptado). Eventos `ConvocatoriaCreada`/`ConvocatoriaRespondida` por el seam `ISesionEventsPublisher`; SignalR empuja `ConvocatoriaCreada` solo a `participante:{convocado}` (grupo de SP-3f-4).

**Tech Stack:** .NET 8, Clean Architecture + CQRS/MediatR, FluentValidation, SignalR, EF Core 8 (con migración), xUnit con fakes a mano (sin Moq); integración con provider InMemory.

## Global Constraints

- **Servicio:** todo el trabajo vive en `services/operaciones-sesion/`. Backend-only. Depende del contrato `GET /api/teams/mine` que entrega SP-3e-1a (consumido vía HTTP; en tests se usa `FakeEquipoDirectoryClient`).
- **Default simple de mínimos (aprobado):** en Equipo, al iniciar, cuenta como participante el equipo con inscripción activa **y ≥1 convocatoria aceptada**; si el conteo `< MinimosParticipacion` → cancela **toda** la sesión (mismo patrón que Individual). Sin two-phase `Confirmar/ExcluirPorMinimos`. `EstadoInscripcion` sigue `{ Activa, Cancelada }`.
- **Participante activo (Equipo)** = integrante con convocatoria **Aceptada** en inscripción **Activa**. **Cupo (`Maximos`)** = nº de inscripciones de equipo activas.
- **Snapshot congelado:** los miembros a convocar se toman de Identity al preinscribir; altas/bajas posteriores del equipo no afectan esa partida.
- **`ParticipanteTieneParticipacionActivaAsync` se extiende** para contar convocatoria aceptada como participación activa (afecta también inscripción Individual: BR-G09).
- **Fakes a mano, sin Moq.** TDD estricto: test que falla → correr rojo → implementar mínimo → correr verde → commit.
- **`TimeProvider` inyectado**; server-stampea timestamps. IDs value-object vía `.Valor`; enums vía `.ToString()`.
- **Métodos 15 y 16 de `ISesionEventsPublisher`** rompen compilación en 4 implementadores si faltan: `SignalRSesionEventsPublisher`, `NoOpSesionEventsPublisher`, `CompositeSesionEventsPublisher`, `FakeSesionEventsPublisher` (test), y la clase abstracta `NoOpBase` dentro de `CompositeSesionEventsPublisherTests`. Los 5 se actualizan en la Tarea B6.
- **Carve-out git (NO commitear):** `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md`, `docs/04-sdd/auditorias/`. La Tarea B14 **escribe** la fila de traceability pero **no la commitea**. `git add` SOLO archivos nombrados exactos — nunca `git add -A`/`.`/`docs/`. Prohibido `git checkout`/`restore`/`clean`/`stash`/`reset` de rango amplio.
- **Trailer de commit:** cada commit termina con exactamente `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` (sin línea de sesión).
- **Comando de test:** `dotnet test <ruta-de-un-solo-.csproj>`. NO pasar dos rutas en un comando (falla `MSB1008`). Correr Unit / Contract / Integration por separado.

**Rutas de proyecto (para los comandos):**
- Unit: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
- Contract: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`
- Integration: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj`

**Firmas clave (contrato entre tareas):**
- B1: `IEquipoDirectoryClient.ObtenerMiEquipoAsync(string? bearerToken, CancellationToken) → Task<EquipoSnapshotDto?>`; `EquipoSnapshotDto(Guid EquipoId, string NombreEquipo, IReadOnlyList<MiembroEquipoDto> Miembros)`; `MiembroEquipoDto(Guid UsuarioId, bool EsLider)`; `FakeEquipoDirectoryClient { EquipoSnapshotDto? Equipo }`.
- B2: `EstadoConvocatoria { Pendiente, Aceptada, Rechazada }`; `ConvocatoriaId(Guid Valor)` (New/From/EsValido); `Convocatoria { ConvocatoriaId Id, Guid PartidaId, Guid EquipoId, Guid UsuarioId, EstadoConvocatoria Estado, DateTime FechaEnvio, DateTime? FechaRespuesta; Aceptar(now); Rechazar(now); bool EstaAceptada; bool EstaPendiente }`.
- B3: `InscripcionPartida += Modalidad, Guid? EquipoId, IReadOnlyList<Convocatoria> Convocatorias, int ConvocatoriasAceptadas`; `static InscripcionPartida.PreinscribirEquipo(Guid equipoId, IEnumerable<Guid> miembros, Guid partidaId, DateTime fecha)`.
- B4: `SesionPartida.PreinscribirEquipo(Guid equipoId, bool callerEsLider, IReadOnlyList<Guid> miembros, bool equipoTieneParticipacionActivaEnOtra, int equiposActivos, DateTime fecha) → InscripcionPartida`; `NoEsLiderEquipoException(Guid)`, `EquipoYaInscritoException(Guid)`.
- B5: `SesionPartida.ResponderConvocatoria(Guid convocatoriaId, Guid usuarioId, bool aceptar, bool participanteTieneParticipacionActivaEnOtra, DateTime now) → Convocatoria`; `SesionPartida.CancelarInscripcionEquipo(Guid equipoId, bool callerEsLider)`; `ConvocatoriaNoEncontradaException(Guid)`.
- B6: `ConvocatoriaCreadaEvent(Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid EquipoId, Guid UsuarioId)`; `ConvocatoriaRespondidaEvent(Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid UsuarioId, string EstadoConvocatoria)`; `ISesionEventsPublisher.PublicarConvocatoriaCreadaAsync/PublicarConvocatoriaRespondidaAsync`; `SesionRealtimeMessages.ConvocatoriaCreada`; `ConvocatoriaCreadaPayload`.
- B7: `PreinscribirEquipoCommand(Guid PartidaId, Guid LiderId, string? BearerToken) : IRequest<PreinscripcionEquipoResponse>`; `PreinscripcionEquipoResponse(Guid InscripcionId, Guid EquipoId, int Convocados)`; `SinEquipoActivoException(Guid)`; `IdentityInaccesibleException`.
- B8: `ResponderConvocatoriaCommand(Guid ConvocatoriaId, Guid UsuarioId, bool Aceptar) : IRequest<ConvocatoriaResponse>`; `ConvocatoriaResponse(Guid ConvocatoriaId, string Estado)`; `CancelarInscripcionEquipoCommand(Guid PartidaId, Guid LiderId, string? BearerToken) : IRequest`.
- B9: `IdentityEquipoHttpClient : IEquipoDirectoryClient` (GET `/api/teams/mine`) + DI `AddHttpClient`.
- B11: `ISesionPartidaRepository.EquipoTieneParticipacionActivaAsync(Guid equipoId, Guid exceptPartidaId, CancellationToken) → Task<bool>`; `GetByConvocatoriaIdAsync(Guid convocatoriaId, CancellationToken) → Task<SesionPartida?>`.
- B12: endpoints `POST partidas/{id}/inscripciones-equipo`, `POST convocatorias/{convocatoriaId}/aceptacion`, `POST convocatorias/{convocatoriaId}/rechazo`, `DELETE partidas/{id}/inscripciones-equipo/mia`.

---

### Task B1: Puerto `IEquipoDirectoryClient` + DTOs + Fake

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/IEquipoDirectoryClient.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/EquipoSnapshotDto.cs`
- Create: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeEquipoDirectoryClient.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeEquipoDirectoryClientTests.cs`

**Interfaces:**
- Produces: `IEquipoDirectoryClient`, `EquipoSnapshotDto`, `MiembroEquipoDto`, `FakeEquipoDirectoryClient`.

- [ ] **Step 1: Escribir el test del fake (rojo)**

Crear `FakeEquipoDirectoryClientTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public class FakeEquipoDirectoryClientTests
{
    [Fact]
    public async Task Devuelve_el_equipo_configurado()
    {
        var equipoId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var fake = new FakeEquipoDirectoryClient
        {
            Equipo = new EquipoSnapshotDto(equipoId, "Halcones",
                new List<MiembroEquipoDto> { new(lider, true) })
        };

        var r = await fake.ObtenerMiEquipoAsync("Bearer x", CancellationToken.None);

        Assert.NotNull(r);
        Assert.Equal(equipoId, r!.EquipoId);
        Assert.True(r.Miembros[0].EsLider);
    }

    [Fact]
    public async Task Sin_configurar_devuelve_null()
    {
        var fake = new FakeEquipoDirectoryClient();
        Assert.Null(await fake.ObtenerMiEquipoAsync(null, CancellationToken.None));
    }
}
```

- [ ] **Step 2: Correr y verificar que falla (a compilar)**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila (`IEquipoDirectoryClient`, `EquipoSnapshotDto`, `FakeEquipoDirectoryClient` no existen).

- [ ] **Step 3: Crear el DTO**

Crear `EquipoSnapshotDto.cs`:

```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record EquipoSnapshotDto(
    Guid EquipoId, string NombreEquipo, IReadOnlyList<MiembroEquipoDto> Miembros);

public sealed record MiembroEquipoDto(Guid UsuarioId, bool EsLider);
```

- [ ] **Step 4: Crear el puerto**

Crear `IEquipoDirectoryClient.cs`:

```csharp
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Interfaces;

public interface IEquipoDirectoryClient
{
    Task<EquipoSnapshotDto?> ObtenerMiEquipoAsync(string? bearerToken, CancellationToken cancellationToken);
}
```

- [ ] **Step 5: Crear el fake**

Crear `FakeEquipoDirectoryClient.cs`:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeEquipoDirectoryClient : IEquipoDirectoryClient
{
    public EquipoSnapshotDto? Equipo { get; set; }

    public Task<EquipoSnapshotDto?> ObtenerMiEquipoAsync(string? bearerToken, CancellationToken cancellationToken)
        => Task.FromResult(Equipo);
}
```

- [ ] **Step 6: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/IEquipoDirectoryClient.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/EquipoSnapshotDto.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeEquipoDirectoryClient.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeEquipoDirectoryClientTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B1: puerto IEquipoDirectoryClient + EquipoSnapshotDto + Fake (snapshot membresía)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B2: Dominio — `Convocatoria` (enum + VO + entidad)

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/EstadoConvocatoria.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/ValueObjects/ConvocatoriaId.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/Convocatoria.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/ConvocatoriaTests.cs`

**Interfaces:**
- Produces: `EstadoConvocatoria`, `ConvocatoriaId`, `Convocatoria`.

- [ ] **Step 1: Escribir los tests (rojo)**

Crear `ConvocatoriaTests.cs`:

```csharp
using System;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class ConvocatoriaTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Nace_pendiente()
    {
        var c = new Convocatoria(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0);
        Assert.Equal(EstadoConvocatoria.Pendiente, c.Estado);
        Assert.True(c.EstaPendiente);
        Assert.False(c.EstaAceptada);
        Assert.Null(c.FechaRespuesta);
    }

    [Fact]
    public void Aceptar_marca_aceptada_y_sella_fecha()
    {
        var c = new Convocatoria(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0);
        c.Aceptar(T0.AddMinutes(1));
        Assert.Equal(EstadoConvocatoria.Aceptada, c.Estado);
        Assert.True(c.EstaAceptada);
        Assert.Equal(T0.AddMinutes(1), c.FechaRespuesta);
    }

    [Fact]
    public void Rechazar_marca_rechazada_y_sella_fecha()
    {
        var c = new Convocatoria(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0);
        c.Rechazar(T0.AddMinutes(2));
        Assert.Equal(EstadoConvocatoria.Rechazada, c.Estado);
        Assert.False(c.EstaPendiente);
        Assert.Equal(T0.AddMinutes(2), c.FechaRespuesta);
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila.

- [ ] **Step 3: Crear el enum**

Crear `EstadoConvocatoria.cs`:

```csharp
namespace Umbral.OperacionesSesion.Domain.Enums;

public enum EstadoConvocatoria { Pendiente, Aceptada, Rechazada }
```

- [ ] **Step 4: Crear el VO**

Crear `ConvocatoriaId.cs`:

```csharp
namespace Umbral.OperacionesSesion.Domain.ValueObjects;

public readonly record struct ConvocatoriaId(Guid Valor)
{
    public static ConvocatoriaId New() => new(Guid.NewGuid());
    public static ConvocatoriaId From(Guid valor) => new(valor);
    public bool EsValido() => Valor != Guid.Empty;
}
```

- [ ] **Step 5: Crear la entidad**

Crear `Convocatoria.cs`:

```csharp
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class Convocatoria
{
    public ConvocatoriaId Id { get; private set; }
    public Guid PartidaId { get; private set; }
    public Guid EquipoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public EstadoConvocatoria Estado { get; private set; }
    public DateTime FechaEnvio { get; private set; }
    public DateTime? FechaRespuesta { get; private set; }

    private Convocatoria() { } // EF

    internal Convocatoria(Guid partidaId, Guid equipoId, Guid usuarioId, DateTime fechaEnvio)
    {
        Id = ConvocatoriaId.New();
        PartidaId = partidaId;
        EquipoId = equipoId;
        UsuarioId = usuarioId;
        Estado = EstadoConvocatoria.Pendiente;
        FechaEnvio = fechaEnvio;
    }

    internal void Aceptar(DateTime now) { Estado = EstadoConvocatoria.Aceptada; FechaRespuesta = now; }
    internal void Rechazar(DateTime now) { Estado = EstadoConvocatoria.Rechazada; FechaRespuesta = now; }

    public bool EstaAceptada => Estado == EstadoConvocatoria.Aceptada;
    public bool EstaPendiente => Estado == EstadoConvocatoria.Pendiente;
}
```

- [ ] **Step 6: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/EstadoConvocatoria.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/ValueObjects/ConvocatoriaId.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/Convocatoria.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/ConvocatoriaTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B2: dominio Convocatoria (EstadoConvocatoria + ConvocatoriaId VO + entidad hija)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B3: Dominio — `InscripcionPartida` extendida (Modalidad, EquipoId, PreinscribirEquipo)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/InscripcionPartida.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/InscripcionPartidaTests.cs`

**Interfaces:**
- Consumes: de B2 → `Convocatoria`. Existentes: `InscripcionId`, `EstadoInscripcion`, `Modalidad`.
- Produces: `InscripcionPartida += Modalidad, Guid? EquipoId, IReadOnlyList<Convocatoria> Convocatorias, int ConvocatoriasAceptadas`; `static PreinscribirEquipo(...)`. El ctor Individual existente `InscripcionPartida(Guid participanteId, DateTime fecha)` se conserva (setea `Modalidad.Individual`).

- [ ] **Step 1: Escribir los tests (rojo)**

Crear `InscripcionPartidaTests.cs`:

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
    public void Individual_es_activa_sin_equipo_ni_convocatorias()
    {
        var insc = new InscripcionPartida(Guid.NewGuid(), T0);
        Assert.Equal(Modalidad.Individual, insc.Modalidad);
        Assert.Null(insc.EquipoId);
        Assert.Empty(insc.Convocatorias);
        Assert.True(insc.EsActiva);
    }

    [Fact]
    public void PreinscribirEquipo_genera_una_convocatoria_pendiente_por_miembro()
    {
        var equipoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        var insc = InscripcionPartida.PreinscribirEquipo(equipoId, new[] { m1, m2 }, partidaId, T0);

        Assert.Equal(Modalidad.Equipo, insc.Modalidad);
        Assert.Equal(equipoId, insc.EquipoId);
        Assert.True(insc.EsActiva);
        Assert.Equal(2, insc.Convocatorias.Count);
        Assert.All(insc.Convocatorias, c => Assert.True(c.EstaPendiente));
        Assert.All(insc.Convocatorias, c => Assert.Equal(equipoId, c.EquipoId));
        Assert.All(insc.Convocatorias, c => Assert.Equal(partidaId, c.PartidaId));
        Assert.Contains(insc.Convocatorias, c => c.UsuarioId == m1);
        Assert.Contains(insc.Convocatorias, c => c.UsuarioId == m2);
        Assert.Equal(0, insc.ConvocatoriasAceptadas);
    }

    [Fact]
    public void ConvocatoriasAceptadas_cuenta_solo_aceptadas()
    {
        var insc = InscripcionPartida.PreinscribirEquipo(
            Guid.NewGuid(), new[] { Guid.NewGuid(), Guid.NewGuid() }, Guid.NewGuid(), T0);
        insc.Convocatorias[0].Aceptar(T0);

        Assert.Equal(1, insc.ConvocatoriasAceptadas);
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila (`PreinscribirEquipo`, `Modalidad`, `EquipoId`, `Convocatorias` no existen en `InscripcionPartida`).

- [ ] **Step 3: Reescribir `InscripcionPartida.cs`**

Reemplazar el contenido completo por:

```csharp
using System.Collections.Generic;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class InscripcionPartida
{
    private readonly List<Convocatoria> _convocatorias = new();

    public InscripcionId Id { get; private set; }
    public Guid ParticipanteId { get; private set; } // Guid.Empty en modalidad Equipo
    public Modalidad Modalidad { get; private set; }
    public Guid? EquipoId { get; private set; }
    public EstadoInscripcion Estado { get; private set; }
    public DateTime FechaInscripcion { get; private set; }

    public IReadOnlyList<Convocatoria> Convocatorias => _convocatorias;

    private InscripcionPartida() { } // EF

    // Individual (ctor existente; conservado para SesionPartida.Inscribir)
    internal InscripcionPartida(Guid participanteId, DateTime fecha)
    {
        Id = InscripcionId.New();
        ParticipanteId = participanteId;
        Modalidad = Modalidad.Individual;
        Estado = EstadoInscripcion.Activa;
        FechaInscripcion = fecha;
    }

    // Equipo
    private InscripcionPartida(Guid equipoId, IEnumerable<Guid> miembros, Guid partidaId, DateTime fecha)
    {
        Id = InscripcionId.New();
        ParticipanteId = Guid.Empty;
        Modalidad = Modalidad.Equipo;
        EquipoId = equipoId;
        Estado = EstadoInscripcion.Activa;
        FechaInscripcion = fecha;
        foreach (var m in miembros)
            _convocatorias.Add(new Convocatoria(partidaId, equipoId, m, fecha));
    }

    internal static InscripcionPartida PreinscribirEquipo(
        Guid equipoId, IEnumerable<Guid> miembros, Guid partidaId, DateTime fecha)
        => new(equipoId, miembros, partidaId, fecha);

    internal void Cancelar() => Estado = EstadoInscripcion.Cancelada;

    public bool EsActiva => Estado == EstadoInscripcion.Activa;
    public int ConvocatoriasAceptadas => _convocatorias.Count(c => c.EstaAceptada);
}
```

- [ ] **Step 4: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS (los 3 nuevos + suite existente, incluida la de inscripción Individual — el ctor `(Guid, DateTime)` sigue intacto).

> ⚠️ Ventana conocida B3→B10: la navegación `Convocatorias` queda descubierta por EF pero `ConvocatoriaId` no tiene converter hasta B10 → el model-building de `OperacionesSesionDbContext` lanza si se construye. Entre B3 y B9 correr SOLO UnitTests (ningún step de esas tareas corre Integration/Contract). B10 cierra la ventana.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/InscripcionPartida.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/InscripcionPartidaTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B3: InscripcionPartida += Modalidad/EquipoId/Convocatorias + factory PreinscribirEquipo

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B4: Dominio — `SesionPartida.PreinscribirEquipo` + excepciones

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/NoEsLiderEquipoException.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/EquipoYaInscritoException.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaEquipoTests.cs`

**Interfaces:**
- Consumes: de B3 → `InscripcionPartida.PreinscribirEquipo`, `.EquipoId`, `.EsActiva`, `.Convocatorias`. Existentes: `_inscripciones`, `Estado`, `Modalidad`, `MaximosParticipacion`, `SesionNoEnLobbyException`, `ModalidadNoSoportadaException`, `ParticipacionActivaExistenteException`, `CupoLlenoException`.
- Produces: `SesionPartida.PreinscribirEquipo(...)`, `NoEsLiderEquipoException`, `EquipoYaInscritoException`.

- [ ] **Step 1: Escribir los tests (rojo)**

Crear `SesionPartidaEquipoTests.cs`:

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

public class SesionPartidaEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida PartidaEquipoEnLobby(int minimos = 1, int maximos = 5)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot(
            "Copa Equipos", Modalidad.Equipo, ModoInicioPartida.Manual, null, minimos, maximos,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public void PreinscribirEquipo_feliz_crea_inscripcion_y_convocatorias()
    {
        var sesion = PartidaEquipoEnLobby();
        var equipoId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var miembros = new List<Guid> { lider, Guid.NewGuid() };

        var insc = sesion.PreinscribirEquipo(equipoId, callerEsLider: true, miembros, false, 0, T0);

        Assert.Equal(Modalidad.Equipo, insc.Modalidad);
        Assert.Equal(equipoId, insc.EquipoId);
        Assert.Equal(2, insc.Convocatorias.Count);
        Assert.Single(sesion.Inscripciones);
    }

    [Fact]
    public void PreinscribirEquipo_no_lider_lanza()
    {
        var sesion = PartidaEquipoEnLobby();
        Assert.Throws<NoEsLiderEquipoException>(() =>
            sesion.PreinscribirEquipo(Guid.NewGuid(), callerEsLider: false, new[] { Guid.NewGuid() }, false, 0, T0));
    }

    [Fact]
    public void PreinscribirEquipo_equipo_ya_inscrito_lanza()
    {
        var sesion = PartidaEquipoEnLobby();
        var equipoId = Guid.NewGuid();
        sesion.PreinscribirEquipo(equipoId, true, new[] { Guid.NewGuid() }, false, 0, T0);

        Assert.Throws<EquipoYaInscritoException>(() =>
            sesion.PreinscribirEquipo(equipoId, true, new[] { Guid.NewGuid() }, false, 1, T0));
    }

    [Fact]
    public void PreinscribirEquipo_participacion_activa_en_otra_lanza()
    {
        var sesion = PartidaEquipoEnLobby();
        Assert.Throws<ParticipacionActivaExistenteException>(() =>
            sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { Guid.NewGuid() },
                equipoTieneParticipacionActivaEnOtra: true, 0, T0));
    }

    [Fact]
    public void PreinscribirEquipo_cupo_lleno_lanza()
    {
        var sesion = PartidaEquipoEnLobby(minimos: 1, maximos: 2);
        Assert.Throws<CupoLlenoException>(() =>
            sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { Guid.NewGuid() }, false, equiposActivos: 2, T0));
    }

    [Fact]
    public void PreinscribirEquipo_en_partida_individual_lanza_modalidad()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("Individual", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);

        Assert.Throws<ModalidadNoSoportadaException>(() =>
            sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { Guid.NewGuid() }, false, 0, T0));
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila.

- [ ] **Step 3: Crear las excepciones**

Crear `NoEsLiderEquipoException.cs`:

```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class NoEsLiderEquipoException : Exception
{
    public NoEsLiderEquipoException(Guid equipoId)
        : base($"El usuario no es líder del equipo {equipoId}.") { }
}
```

Crear `EquipoYaInscritoException.cs`:

```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class EquipoYaInscritoException : Exception
{
    public EquipoYaInscritoException(Guid equipoId)
        : base($"El equipo {equipoId} ya está inscrito en esta partida.") { }
}
```

- [ ] **Step 4: Implementar `PreinscribirEquipo` en `SesionPartida.cs`**

Añadir el método público (p. ej. tras `CancelarInscripcion`):

```csharp
    public InscripcionPartida PreinscribirEquipo(
        Guid equipoId, bool callerEsLider, IReadOnlyList<Guid> miembros,
        bool equipoTieneParticipacionActivaEnOtra, int equiposActivos, DateTime fecha)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);
        if (Modalidad != Modalidad.Equipo)
            throw new ModalidadNoSoportadaException(PartidaId);
        if (!callerEsLider)
            throw new NoEsLiderEquipoException(equipoId);
        if (_inscripciones.Any(i => i.EquipoId == equipoId && i.EsActiva))
            throw new EquipoYaInscritoException(equipoId);
        if (equipoTieneParticipacionActivaEnOtra)
            throw new ParticipacionActivaExistenteException(equipoId);
        if (equiposActivos >= MaximosParticipacion)
            throw new CupoLlenoException(PartidaId);

        var inscripcion = InscripcionPartida.PreinscribirEquipo(equipoId, miembros, PartidaId, fecha);
        _inscripciones.Add(inscripcion);
        return inscripcion;
    }
```

- [ ] **Step 5: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS (los 6 nuevos + suite).

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/NoEsLiderEquipoException.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/EquipoYaInscritoException.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaEquipoTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B4: SesionPartida.PreinscribirEquipo (líder + cupo + no-doble-participación) + 2 excepciones

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B5: Dominio — `ResponderConvocatoria` + `CancelarInscripcionEquipo` + mínimos Equipo

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/ConvocatoriaNoEncontradaException.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaEquipoTests.cs` (añadir)

**Interfaces:**
- Consumes: de B4 → `SesionPartida.PreinscribirEquipo`; de B2 → `Convocatoria.Aceptar/Rechazar/EstaPendiente`. Existentes: `AplicarInicio`, `Iniciar`, `InscripcionNoEncontradaException`, `ResultadoInicio`.
- Produces: `SesionPartida.ResponderConvocatoria(...)`, `SesionPartida.CancelarInscripcionEquipo(...)`, `ConvocatoriaNoEncontradaException`; `AplicarInicio` cuenta mínimos Equipo.

- [ ] **Step 1: Añadir los tests (rojo)**

En `SesionPartidaEquipoTests.cs`, añadir dentro de la clase:

```csharp
    private static (SesionPartida sesion, Guid convocatoriaId, Guid usuario) EquipoConUnaConvocatoria()
    {
        var sesion = PartidaEquipoEnLobby();
        var usuario = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { usuario }, false, 0, T0);
        return (sesion, insc.Convocatorias[0].Id.Valor, usuario);
    }

    [Fact]
    public void ResponderConvocatoria_aceptar_marca_aceptada()
    {
        var (sesion, convocatoriaId, usuario) = EquipoConUnaConvocatoria();

        var c = sesion.ResponderConvocatoria(convocatoriaId, usuario, aceptar: true, false, T0.AddMinutes(1));

        Assert.True(c.EstaAceptada);
    }

    [Fact]
    public void ResponderConvocatoria_rechazar_marca_rechazada()
    {
        var (sesion, convocatoriaId, usuario) = EquipoConUnaConvocatoria();

        var c = sesion.ResponderConvocatoria(convocatoriaId, usuario, aceptar: false, false, T0.AddMinutes(1));

        Assert.Equal(EstadoConvocatoria.Rechazada, c.Estado);
    }

    [Fact]
    public void ResponderConvocatoria_aceptar_con_participacion_activa_en_otra_lanza()
    {
        var (sesion, convocatoriaId, usuario) = EquipoConUnaConvocatoria();

        Assert.Throws<ParticipacionActivaExistenteException>(() =>
            sesion.ResponderConvocatoria(convocatoriaId, usuario, aceptar: true,
                participanteTieneParticipacionActivaEnOtra: true, T0));
    }

    [Fact]
    public void ResponderConvocatoria_id_inexistente_lanza()
    {
        var (sesion, _, usuario) = EquipoConUnaConvocatoria();

        Assert.Throws<ConvocatoriaNoEncontradaException>(() =>
            sesion.ResponderConvocatoria(Guid.NewGuid(), usuario, true, false, T0));
    }

    [Fact]
    public void ResponderConvocatoria_usuario_distinto_lanza()
    {
        var (sesion, convocatoriaId, _) = EquipoConUnaConvocatoria();

        Assert.Throws<ConvocatoriaNoEncontradaException>(() =>
            sesion.ResponderConvocatoria(convocatoriaId, Guid.NewGuid(), true, false, T0));
    }

    [Fact]
    public void ResponderConvocatoria_ya_respondida_lanza()
    {
        var (sesion, convocatoriaId, usuario) = EquipoConUnaConvocatoria();
        sesion.ResponderConvocatoria(convocatoriaId, usuario, true, false, T0);

        Assert.Throws<ConvocatoriaNoEncontradaException>(() =>
            sesion.ResponderConvocatoria(convocatoriaId, usuario, false, false, T0));
    }

    [Fact]
    public void CancelarInscripcionEquipo_lider_cancela()
    {
        var sesion = PartidaEquipoEnLobby();
        var equipoId = Guid.NewGuid();
        sesion.PreinscribirEquipo(equipoId, true, new[] { Guid.NewGuid() }, false, 0, T0);

        sesion.CancelarInscripcionEquipo(equipoId, callerEsLider: true);

        Assert.DoesNotContain(sesion.Inscripciones, i => i.EsActiva);
    }

    [Fact]
    public void CancelarInscripcionEquipo_no_lider_lanza()
    {
        var sesion = PartidaEquipoEnLobby();
        var equipoId = Guid.NewGuid();
        sesion.PreinscribirEquipo(equipoId, true, new[] { Guid.NewGuid() }, false, 0, T0);

        Assert.Throws<NoEsLiderEquipoException>(() =>
            sesion.CancelarInscripcionEquipo(equipoId, callerEsLider: false));
    }

    [Fact]
    public void Iniciar_equipo_sin_aceptados_cancela_por_minimos()
    {
        var sesion = PartidaEquipoEnLobby(minimos: 1);
        sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { Guid.NewGuid() }, false, 0, T0);
        // nadie aceptó → 0 equipos participantes < mínimo 1

        var r = sesion.Iniciar(T0);

        Assert.Equal(ResultadoInicio.Cancelada, r);
        Assert.Equal(EstadoSesion.Cancelada, sesion.Estado);
    }

    [Fact]
    public void Iniciar_equipo_con_un_aceptado_inicia()
    {
        var sesion = PartidaEquipoEnLobby(minimos: 1);
        var usuario = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { usuario }, false, 0, T0);
        sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);

        var r = sesion.Iniciar(T0);

        Assert.Equal(EstadoSesion.Iniciada, sesion.Estado);
        Assert.NotEqual(ResultadoInicio.Cancelada, r);
    }
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila (`ResponderConvocatoria`, `CancelarInscripcionEquipo`, `ConvocatoriaNoEncontradaException` no existen); `Iniciar_equipo_sin_aceptados...` falla porque el conteo actual usa inscripciones activas (contaría 1) hasta implementar el mínimo Equipo.

- [ ] **Step 3: Crear la excepción**

Crear `ConvocatoriaNoEncontradaException.cs`:

```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class ConvocatoriaNoEncontradaException : Exception
{
    public ConvocatoriaNoEncontradaException(Guid convocatoriaId)
        : base($"No existe una convocatoria pendiente {convocatoriaId} para este usuario.") { }
}
```

- [ ] **Step 4: Implementar `ResponderConvocatoria` y `CancelarInscripcionEquipo` en `SesionPartida.cs`**

Añadir los métodos públicos (junto a `PreinscribirEquipo`):

```csharp
    public Convocatoria ResponderConvocatoria(
        Guid convocatoriaId, Guid usuarioId, bool aceptar,
        bool participanteTieneParticipacionActivaEnOtra, DateTime now)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);

        var convocatoria = _inscripciones
            .Where(i => i.EsActiva)
            .SelectMany(i => i.Convocatorias)
            .FirstOrDefault(c => c.Id.Valor == convocatoriaId && c.UsuarioId == usuarioId && c.EstaPendiente)
            ?? throw new ConvocatoriaNoEncontradaException(convocatoriaId);

        if (aceptar)
        {
            if (participanteTieneParticipacionActivaEnOtra)
                throw new ParticipacionActivaExistenteException(usuarioId);
            convocatoria.Aceptar(now);
        }
        else
        {
            convocatoria.Rechazar(now);
        }

        return convocatoria;
    }

    public void CancelarInscripcionEquipo(Guid equipoId, bool callerEsLider)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);
        if (!callerEsLider)
            throw new NoEsLiderEquipoException(equipoId);
        var inscripcion = _inscripciones.FirstOrDefault(i => i.EquipoId == equipoId && i.EsActiva)
            ?? throw new InscripcionNoEncontradaException(equipoId);
        inscripcion.Cancelar();
    }
```

- [ ] **Step 5: Ajustar `AplicarInicio` para contar mínimos por modalidad**

En `AplicarInicio`, reemplazar la línea `var inscritosActivos = _inscripciones.Count(i => i.EsActiva);` y su condición por el conteo dependiente de modalidad. El método queda:

```csharp
    private ResultadoInicio AplicarInicio(DateTime now)
    {
        var participantes = Modalidad == Modalidad.Equipo
            ? _inscripciones.Count(i => i.EsActiva && i.ConvocatoriasAceptadas >= 1)
            : _inscripciones.Count(i => i.EsActiva);
        if (participantes < MinimosParticipacion)
        {
            Estado = EstadoSesion.Cancelada;
            FechaFin = now;
            return ResultadoInicio.Cancelada;
        }

        Estado = EstadoSesion.Iniciada;
        FechaInicio = now;
        var primero = _juegos.OrderBy(j => j.Orden).First();
        primero.Activar(now);
        return ResultadoInicio.Iniciada(primero);
    }
```

- [ ] **Step 6: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS (nuevos + suite; los tests Individual existentes siguen verdes — el conteo Individual no cambió).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/ConvocatoriaNoEncontradaException.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaEquipoTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B5: ResponderConvocatoria + CancelarInscripcionEquipo + mínimos Equipo (≥1 aceptado)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B6: Seam — eventos `ConvocatoriaCreada`/`ConvocatoriaRespondida` (métodos 15/16) + SignalR

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ParticipacionEvents.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SignalRSesionEventsPublisherTests.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/CompositeSesionEventsPublisherTests.cs`

**Interfaces:**
- Consumes: existentes `SesionRealtimeMessages.GrupoParticipante`, patrón `Difundir`, helper `Build()`/`T0`/`clients.LastGroup` en SignalR tests, `RecordingPublisher`/`NoOpBase`/`T0` en Composite tests.
- Produces: `ConvocatoriaCreadaEvent`, `ConvocatoriaRespondidaEvent`, métodos 15/16 en el seam, `SesionRealtimeMessages.ConvocatoriaCreada`, `ConvocatoriaCreadaPayload`, `FakeSesionEventsPublisher.ConvocatoriasCreadas`/`.ConvocatoriasRespondidas`.

- [ ] **Step 1: Escribir los tests SignalR y Composite (rojo)**

En `SignalRSesionEventsPublisherTests.cs`, añadir dentro de la clase:

```csharp
    [Fact]
    public async Task ConvocatoriaCreada_difunde_solo_al_grupo_del_convocado()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var convocado = Guid.NewGuid();

        await pub.PublicarConvocatoriaCreadaAsync(
            new ConvocatoriaCreadaEvent(partidaId, Guid.NewGuid(), convocatoriaId, equipoId, convocado),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.GrupoParticipante(convocado), clients.LastGroup); // NO GrupoPartida
        Assert.Equal(SesionRealtimeMessages.ConvocatoriaCreada, clients.Proxy.Method);
        var payload = Assert.IsType<ConvocatoriaCreadaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(partidaId, payload.PartidaId);
        Assert.Equal(equipoId, payload.EquipoId);
        Assert.Equal(convocatoriaId, payload.ConvocatoriaId);
        Assert.Equal(convocado, payload.UsuarioId);
    }

    [Fact]
    public async Task ConvocatoriaRespondida_no_difunde()
    {
        var (pub, clients) = Build();

        await pub.PublicarConvocatoriaRespondidaAsync(
            new ConvocatoriaRespondidaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Aceptada"),
            CancellationToken.None);

        Assert.Null(clients.LastGroup); // sin difusión
    }
```

En `CompositeSesionEventsPublisherTests.cs`:
1. En `RecordingPublisher`, añadir:

```csharp
        public int ConvocatoriasCreadas;
        public int ConvocatoriasRespondidas;
        public override Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent e, CancellationToken ct)
        { ConvocatoriasCreadas++; return Task.CompletedTask; }
        public override Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent e, CancellationToken ct)
        { ConvocatoriasRespondidas++; return Task.CompletedTask; }
```

2. En la clase abstracta `NoOpBase`, añadir:

```csharp
        public virtual Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent e, CancellationToken ct) => Task.CompletedTask;
```

3. Añadir el test de fan-out:

```csharp
    [Fact]
    public async Task Convocatoria_fan_out_invoca_a_todos()
    {
        var a = new RecordingPublisher();
        var b = new RecordingPublisher();
        var sut = new CompositeSesionEventsPublisher(new ISesionEventsPublisher[] { a, b }, NullLogger<CompositeSesionEventsPublisher>.Instance);

        await sut.PublicarConvocatoriaCreadaAsync(
            new ConvocatoriaCreadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);
        await sut.PublicarConvocatoriaRespondidaAsync(
            new ConvocatoriaRespondidaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Rechazada"),
            CancellationToken.None);

        Assert.Equal(1, a.ConvocatoriasCreadas);
        Assert.Equal(1, b.ConvocatoriasCreadas);
        Assert.Equal(1, a.ConvocatoriasRespondidas);
        Assert.Equal(1, b.ConvocatoriasRespondidas);
    }
```

- [ ] **Step 2: Correr y verificar que falla (a compilar)**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila.

- [ ] **Step 3: Crear los eventos**

Crear `ParticipacionEvents.cs`:

```csharp
namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record ConvocatoriaCreadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid EquipoId, Guid UsuarioId);

public sealed record ConvocatoriaRespondidaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid UsuarioId, string EstadoConvocatoria);
```

- [ ] **Step 4: Añadir los métodos 15/16 a `ISesionEventsPublisher.cs`**

Añadir dentro de la interfaz (tras `PublicarPistaEnviadaAsync`):

```csharp
    Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken);
    Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken);
```

- [ ] **Step 5: Implementar en No-Op y Composite**

En `NoOpSesionEventsPublisher.cs`, añadir:

```csharp
    public Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;
```

En `CompositeSesionEventsPublisher.cs`, añadir:

```csharp
    public Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarConvocatoriaCreadaAsync(evento, cancellationToken));
    public Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarConvocatoriaRespondidaAsync(evento, cancellationToken));
```

- [ ] **Step 6: Añadir const + payload realtime**

En `SesionRealtimeMessages.cs`, añadir la const tras `PistaEnviada`:

```csharp
    public const string ConvocatoriaCreada = nameof(ConvocatoriaCreada);
```

En `SesionRealtimePayloads.cs`, añadir al final:

```csharp
public sealed record ConvocatoriaCreadaPayload(Guid PartidaId, Guid EquipoId, Guid ConvocatoriaId, Guid UsuarioId);
```

- [ ] **Step 7: Implementar en `SignalRSesionEventsPublisher.cs`**

Añadir (push al grupo del convocado, NO vía `Difundir`; `ConvocatoriaRespondida` no difunde):

```csharp
    public Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken) =>
        _hub.Clients.Group(SesionRealtimeMessages.GrupoParticipante(evento.UsuarioId))
            .SendAsync(
                SesionRealtimeMessages.ConvocatoriaCreada,
                new ConvocatoriaCreadaPayload(evento.PartidaId, evento.EquipoId, evento.ConvocatoriaId, evento.UsuarioId),
                cancellationToken);

    public Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken) =>
        Task.CompletedTask;
```

- [ ] **Step 8: Implementar en `FakeSesionEventsPublisher.cs`**

Añadir al final de la clase:

```csharp
    public List<ConvocatoriaCreadaEvent> ConvocatoriasCreadas { get; } = new();
    public List<ConvocatoriaRespondidaEvent> ConvocatoriasRespondidas { get; } = new();

    public Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken)
    { ConvocatoriasCreadas.Add(evento); return Task.CompletedTask; }
    public Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken)
    { ConvocatoriasRespondidas.Add(evento); return Task.CompletedTask; }
```

- [ ] **Step 9: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS. El proyecto de test compila (las 5 impls tienen 15/16) y los tests nuevos pasan.

- [ ] **Step 10: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ParticipacionEvents.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SignalRSesionEventsPublisherTests.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/CompositeSesionEventsPublisherTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B6: eventos ConvocatoriaCreada/Respondida (seam 15/16, 5 impls) + SignalR a participante:{convocado}

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B7: Application — `PreinscribirEquipoCommand` + handler + DTOs + validator

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/PreinscribirEquipoCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/ParticipacionEquipoDtos.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/PreinscribirEquipoCommandValidator.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PreinscribirEquipoCommandHandler.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Exceptions/SinEquipoActivoException.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Exceptions/IdentityInaccesibleException.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/PreinscribirEquipoCommandHandlerTests.cs`

**Interfaces:**
- Consumes: de B1 → `IEquipoDirectoryClient`, `EquipoSnapshotDto`, `MiembroEquipoDto`, `FakeEquipoDirectoryClient`; de B4 → `SesionPartida.PreinscribirEquipo`; de B6 → `ConvocatoriaCreadaEvent`, `ISesionEventsPublisher.PublicarConvocatoriaCreadaAsync`, `FakeSesionEventsPublisher.ConvocatoriasCreadas`; de B11 (interfaz) → `ISesionPartidaRepository.EquipoTieneParticipacionActivaAsync`. Existentes: `ISesionPartidaRepository.GetByPartidaIdAsync`, `IOperacionesSesionUnitOfWork`, `TimeProvider`, `SesionNoEncontradaException`, `FakeSesionPartidaRepository`, `FakeTimeProvider`.
- Produces: `PreinscribirEquipoCommand`, `PreinscripcionEquipoResponse`, `ConvocatoriaResponse`, `PreinscribirEquipoCommandValidator`, `PreinscribirEquipoCommandHandler`, `SinEquipoActivoException`, `IdentityInaccesibleException`.

> Nota: los 2 métodos nuevos de `ISesionPartidaRepository` se agregan en esta tarea **con firma en la interfaz E implementación en `SesionPartidaRepository`** (si solo se agregara la firma, Infrastructure dejaría de compilar hasta B11). B11 los cubre con integration tests y extiende los predicados de los métodos existentes. `FakeSesionPartidaRepository` (test) también gana los métodos en esta tarea para que el handler compile y se pueda testear con fake.

- [ ] **Step 1: Escribir el test del handler (rojo)**

Crear `PreinscribirEquipoCommandHandlerTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class PreinscribirEquipoCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida PartidaEquipoEnLobby(Guid partidaId)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(partidaId, snap);
    }

    [Fact]
    public async Task Preinscribe_y_publica_una_convocatoria_creada_por_miembro()
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
        Assert.Equal(2, resp.Convocados);
        Assert.Equal(2, events.ConvocatoriasCreadas.Count);
        Assert.All(events.ConvocatoriasCreadas, e => Assert.Equal(equipoId, e.EquipoId));
        Assert.Contains(events.ConvocatoriasCreadas, e => e.UsuarioId == miembro);
    }

    [Fact]
    public async Task Caller_no_es_lider_lanza_sin_publicar()
    {
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var otro = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(PartidaEquipoEnLobby(partidaId));
        var directory = new FakeEquipoDirectoryClient
        {
            Equipo = new EquipoSnapshotDto(Guid.NewGuid(), "Halcones",
                new List<MiembroEquipoDto> { new(lider, true), new(otro, false) })
        };
        var events = new FakeSesionEventsPublisher();
        var handler = new PreinscribirEquipoCommandHandler(
            repo, directory, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        // el caller es 'otro' (no líder según el snapshot)
        await Assert.ThrowsAsync<NoEsLiderEquipoException>(
            () => handler.Handle(new PreinscribirEquipoCommand(partidaId, otro, "Bearer x"), default));
        Assert.Empty(events.ConvocatoriasCreadas);
    }

    [Fact]
    public async Task Sin_equipo_activo_lanza()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(PartidaEquipoEnLobby(partidaId));
        var directory = new FakeEquipoDirectoryClient { Equipo = null };
        var handler = new PreinscribirEquipoCommandHandler(
            repo, directory, new FakeSesionEventsPublisher(), new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<SinEquipoActivoException>(
            () => handler.Handle(new PreinscribirEquipoCommand(partidaId, Guid.NewGuid(), "Bearer x"), default));
    }

    [Fact]
    public async Task Sesion_inexistente_lanza()
    {
        var repo = new FakeSesionPartidaRepository();
        var directory = new FakeEquipoDirectoryClient
        {
            Equipo = new EquipoSnapshotDto(Guid.NewGuid(), "H", new List<MiembroEquipoDto> { new(Guid.NewGuid(), true) })
        };
        var handler = new PreinscribirEquipoCommandHandler(
            repo, directory, new FakeSesionEventsPublisher(), new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new PreinscribirEquipoCommand(Guid.NewGuid(), Guid.NewGuid(), "Bearer x"), default));
    }
}
```

> `FakeSesionPartidaRepository` y `FakeUnitOfWork` ya existen en `UnitTests/Application/Fakes` (usados por handlers previos). Si `FakeSesionPartidaRepository` no implementa aún `EquipoTieneParticipacionActivaAsync`, se agrega en el Step 4 (devuelve `false` por defecto).

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila.

- [ ] **Step 3: Declarar los métodos de repo en la interfaz E implementarlos en `SesionPartidaRepository`**

En `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs`, añadir dentro de la interfaz:

```csharp
    Task<bool> EquipoTieneParticipacionActivaAsync(
        Guid equipoId, Guid exceptPartidaId, CancellationToken cancellationToken);
    Task<SesionPartida?> GetByConvocatoriaIdAsync(Guid convocatoriaId, CancellationToken cancellationToken);
```

En `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs` (para que Infrastructure siga compilando; B11 los cubre con integration tests), añadir `using Umbral.OperacionesSesion.Domain.ValueObjects;` al inicio y los dos métodos:

```csharp
    public Task<bool> EquipoTieneParticipacionActivaAsync(
        Guid equipoId, Guid exceptPartidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Where(s => s.PartidaId != exceptPartidaId
                && (s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada))
            .SelectMany(s => s.Inscripciones)
            .AnyAsync(i => i.EquipoId == equipoId && i.Estado == EstadoInscripcion.Activa, cancellationToken);

    public Task<SesionPartida?> GetByConvocatoriaIdAsync(Guid convocatoriaId, CancellationToken cancellationToken)
    {
        var id = ConvocatoriaId.From(convocatoriaId);
        return _dbContext.Sesiones
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Opciones)
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Respuestas)
            .Include(s => s.Juegos).ThenInclude(j => j.Etapas).ThenInclude(e => e.Tesoros)
            .Include(s => s.Inscripciones).ThenInclude(i => i.Convocatorias)
            .FirstOrDefaultAsync(
                s => s.Inscripciones.Any(i => i.Convocatorias.Any(c => c.Id == id)),
                cancellationToken);
    }
```

> `GetByConvocatoriaIdAsync` requiere que la relación `Convocatorias` esté mapeada; B10 (migración/config EF) corre antes que cualquier uso runtime real — para B7 solo importa que compile, y compila porque la navegación existe desde B3.

- [ ] **Step 4: Actualizar el `FakeSesionPartidaRepository` de tests**

En `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs`, añadir los dos métodos (defaults simples para unit tests):

```csharp
    public bool EquipoActivaEnOtra { get; set; }
    public Task<bool> EquipoTieneParticipacionActivaAsync(Guid equipoId, Guid exceptPartidaId, CancellationToken cancellationToken)
        => Task.FromResult(EquipoActivaEnOtra);

    public Task<SesionPartida?> GetByConvocatoriaIdAsync(Guid convocatoriaId, CancellationToken cancellationToken)
        => Task.FromResult(_sesiones.FirstOrDefault(s =>
            s.Inscripciones.Any(i => i.Convocatorias.Any(c => c.Id.Valor == convocatoriaId))));
```

(Usa la lista interna del fake — ajustar el nombre `_sesiones` al campo real del fake si difiere; el fake ya expone `Add` y una colección de sesiones. Requiere `using System.Linq;`.)

- [ ] **Step 5: Crear excepciones de aplicación**

Crear `SinEquipoActivoException.cs`:

```csharp
namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class SinEquipoActivoException : Exception
{
    public SinEquipoActivoException(Guid usuarioId)
        : base($"El usuario {usuarioId} no tiene un equipo activo.") { }
}
```

Crear `IdentityInaccesibleException.cs`:

```csharp
namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class IdentityInaccesibleException : Exception
{
    public IdentityInaccesibleException() : base("El servicio Identity no está accesible.") { }
    public IdentityInaccesibleException(Exception inner) : base("El servicio Identity no está accesible.", inner) { }
}
```

- [ ] **Step 6: Crear el command + DTOs + validator**

Crear `PreinscribirEquipoCommand.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record PreinscribirEquipoCommand(Guid PartidaId, Guid LiderId, string? BearerToken)
    : IRequest<PreinscripcionEquipoResponse>;
```

Crear `ParticipacionEquipoDtos.cs`:

```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record PreinscripcionEquipoResponse(Guid InscripcionId, Guid EquipoId, int Convocados);
public sealed record ConvocatoriaResponse(Guid ConvocatoriaId, string Estado);
```

Crear `PreinscribirEquipoCommandValidator.cs`:

```csharp
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class PreinscribirEquipoCommandValidator : AbstractValidator<PreinscribirEquipoCommand>
{
    public PreinscribirEquipoCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.LiderId).NotEmpty();
    }
}
```

- [ ] **Step 7: Crear el handler**

Crear `PreinscribirEquipoCommandHandler.cs`:

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

public sealed class PreinscribirEquipoCommandHandler
    : IRequestHandler<PreinscribirEquipoCommand, PreinscripcionEquipoResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IEquipoDirectoryClient _directory;
    private readonly ISesionEventsPublisher _events;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public PreinscribirEquipoCommandHandler(
        ISesionPartidaRepository sesiones, IEquipoDirectoryClient directory, ISesionEventsPublisher events,
        IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _directory = directory;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PreinscripcionEquipoResponse> Handle(
        PreinscribirEquipoCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var equipo = await _directory.ObtenerMiEquipoAsync(request.BearerToken, cancellationToken)
            ?? throw new SinEquipoActivoException(request.LiderId);

        var callerEsLider = equipo.Miembros.Any(m => m.UsuarioId == request.LiderId && m.EsLider);
        var miembros = equipo.Miembros.Select(m => m.UsuarioId).ToList();

        var equipoActivaEnOtra = await _sesiones.EquipoTieneParticipacionActivaAsync(
            equipo.EquipoId, request.PartidaId, cancellationToken);
        var equiposActivos = sesion.Inscripciones.Count(i => i.Modalidad == Modalidad.Equipo && i.EsActiva);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var inscripcion = sesion.PreinscribirEquipo(
            equipo.EquipoId, callerEsLider, miembros, equipoActivaEnOtra, equiposActivos, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var c in inscripcion.Convocatorias)
        {
            await _events.PublicarConvocatoriaCreadaAsync(
                new ConvocatoriaCreadaEvent(sesion.PartidaId, sesion.Id.Valor, c.Id.Valor, c.EquipoId, c.UsuarioId),
                cancellationToken);
        }

        return new PreinscripcionEquipoResponse(inscripcion.Id.Valor, equipo.EquipoId, inscripcion.Convocatorias.Count);
    }
}
```

- [ ] **Step 8: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS. (`SesionNoEncontradaException` está en `Application.Exceptions`; el handler la usa vía el `using`.)

- [ ] **Step 9: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/PreinscribirEquipoCommand.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/ParticipacionEquipoDtos.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/PreinscribirEquipoCommandValidator.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PreinscribirEquipoCommandHandler.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Exceptions/SinEquipoActivoException.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Exceptions/IdentityInaccesibleException.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/PreinscribirEquipoCommandHandlerTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B7: PreinscribirEquipoCommand + handler (snapshot Identity → convocatorias + eventos) + DTOs/validator

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B8: Application — commands de convocatoria + cancelación

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/ResponderConvocatoriaCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/CancelarInscripcionEquipoCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/ResponderConvocatoriaCommandValidator.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/CancelarInscripcionEquipoCommandValidator.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/ResponderConvocatoriaCommandHandler.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/CancelarInscripcionEquipoCommandHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ResponderConvocatoriaCommandHandlerTests.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/CancelarInscripcionEquipoCommandHandlerTests.cs`

**Interfaces:**
- Consumes: de B5 → `SesionPartida.ResponderConvocatoria`, `CancelarInscripcionEquipo`; de B6 → `ConvocatoriaRespondidaEvent`, `PublicarConvocatoriaRespondidaAsync`, `FakeSesionEventsPublisher.ConvocatoriasRespondidas`; de B7 → `ConvocatoriaResponse`, `IEquipoDirectoryClient` (para cancelar por líder), `SinEquipoActivoException`; repo `GetByConvocatoriaIdAsync` (fake de B7), `ParticipanteTieneParticipacionActivaAsync`.
- Produces: `ResponderConvocatoriaCommand`, `CancelarInscripcionEquipoCommand`, validators, handlers.

- [ ] **Step 1: Escribir los tests (rojo)**

Crear `ResponderConvocatoriaCommandHandlerTests.cs`:

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
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ResponderConvocatoriaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static (FakeSesionPartidaRepository repo, Guid convocatoriaId, Guid usuario) SesionConConvocatoria()
    {
        var partidaId = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var usuario = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { usuario }, false, 0, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        return (repo, insc.Convocatorias[0].Id.Valor, usuario);
    }

    [Fact]
    public async Task Aceptar_publica_convocatoria_respondida_aceptada()
    {
        var (repo, convocatoriaId, usuario) = SesionConConvocatoria();
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderConvocatoriaCommandHandler(repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        var resp = await handler.Handle(new ResponderConvocatoriaCommand(convocatoriaId, usuario, true), default);

        Assert.Equal("Aceptada", resp.Estado);
        var evt = Assert.Single(events.ConvocatoriasRespondidas);
        Assert.Equal("Aceptada", evt.EstadoConvocatoria);
        Assert.Equal(usuario, evt.UsuarioId);
    }

    [Fact]
    public async Task Convocatoria_inexistente_lanza()
    {
        var (repo, _, usuario) = SesionConConvocatoria();
        var handler = new ResponderConvocatoriaCommandHandler(
            repo, new FakeSesionEventsPublisher(), new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<ConvocatoriaNoEncontradaException>(
            () => handler.Handle(new ResponderConvocatoriaCommand(Guid.NewGuid(), usuario, true), default));
    }
}
```

Crear `CancelarInscripcionEquipoCommandHandlerTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class CancelarInscripcionEquipoCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Lider_cancela_inscripcion_de_equipo()
    {
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        sesion.PreinscribirEquipo(equipoId, true, new[] { lider }, false, 0, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var directory = new FakeEquipoDirectoryClient
        {
            Equipo = new EquipoSnapshotDto(equipoId, "H", new List<MiembroEquipoDto> { new(lider, true) })
        };
        var handler = new CancelarInscripcionEquipoCommandHandler(repo, directory, new FakeOperacionesSesionUnitOfWork());

        await handler.Handle(new CancelarInscripcionEquipoCommand(partidaId, lider, "Bearer x"), default);

        Assert.DoesNotContain(sesion.Inscripciones, i => i.EsActiva);
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila.

- [ ] **Step 3: Crear los commands**

Crear `ResponderConvocatoriaCommand.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record ResponderConvocatoriaCommand(Guid ConvocatoriaId, Guid UsuarioId, bool Aceptar)
    : IRequest<ConvocatoriaResponse>;
```

Crear `CancelarInscripcionEquipoCommand.cs`:

```csharp
using MediatR;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record CancelarInscripcionEquipoCommand(Guid PartidaId, Guid LiderId, string? BearerToken)
    : IRequest;
```

- [ ] **Step 4: Crear los validators**

Crear `ResponderConvocatoriaCommandValidator.cs`:

```csharp
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class ResponderConvocatoriaCommandValidator : AbstractValidator<ResponderConvocatoriaCommand>
{
    public ResponderConvocatoriaCommandValidator()
    {
        RuleFor(x => x.ConvocatoriaId).NotEmpty();
        RuleFor(x => x.UsuarioId).NotEmpty();
    }
}
```

Crear `CancelarInscripcionEquipoCommandValidator.cs`:

```csharp
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class CancelarInscripcionEquipoCommandValidator : AbstractValidator<CancelarInscripcionEquipoCommand>
{
    public CancelarInscripcionEquipoCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.LiderId).NotEmpty();
    }
}
```

- [ ] **Step 5: Crear los handlers**

Crear `ResponderConvocatoriaCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class ResponderConvocatoriaCommandHandler
    : IRequestHandler<ResponderConvocatoriaCommand, ConvocatoriaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly ISesionEventsPublisher _events;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ResponderConvocatoriaCommandHandler(
        ISesionPartidaRepository sesiones, ISesionEventsPublisher events,
        IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ConvocatoriaResponse> Handle(
        ResponderConvocatoriaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByConvocatoriaIdAsync(request.ConvocatoriaId, cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException(request.ConvocatoriaId);

        var activaEnOtra = await _sesiones.ParticipanteTieneParticipacionActivaAsync(
            request.UsuarioId, sesion.PartidaId, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var convocatoria = sesion.ResponderConvocatoria(
            request.ConvocatoriaId, request.UsuarioId, request.Aceptar, activaEnOtra, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarConvocatoriaRespondidaAsync(
            new ConvocatoriaRespondidaEvent(
                sesion.PartidaId, sesion.Id.Valor, convocatoria.Id.Valor,
                convocatoria.UsuarioId, convocatoria.Estado.ToString()),
            cancellationToken);

        return new ConvocatoriaResponse(convocatoria.Id.Valor, convocatoria.Estado.ToString());
    }
}
```

Crear `CancelarInscripcionEquipoCommandHandler.cs`:

```csharp
using MediatR;
using System.Linq;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class CancelarInscripcionEquipoCommandHandler : IRequestHandler<CancelarInscripcionEquipoCommand>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IEquipoDirectoryClient _directory;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;

    public CancelarInscripcionEquipoCommandHandler(
        ISesionPartidaRepository sesiones, IEquipoDirectoryClient directory, IOperacionesSesionUnitOfWork unitOfWork)
    {
        _sesiones = sesiones;
        _directory = directory;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CancelarInscripcionEquipoCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var equipo = await _directory.ObtenerMiEquipoAsync(request.BearerToken, cancellationToken)
            ?? throw new SinEquipoActivoException(request.LiderId);

        var callerEsLider = equipo.Miembros.Any(m => m.UsuarioId == request.LiderId && m.EsLider);
        sesion.CancelarInscripcionEquipo(equipo.EquipoId, callerEsLider);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS. (`IRequestHandler<CancelarInscripcionEquipoCommand>` sin tipo de retorno — MediatR 12 soporta comandos `IRequest` sin respuesta con `Task Handle(...)`.)

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/ResponderConvocatoriaCommand.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/CancelarInscripcionEquipoCommand.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/ResponderConvocatoriaCommandValidator.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/CancelarInscripcionEquipoCommandValidator.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/ResponderConvocatoriaCommandHandler.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/CancelarInscripcionEquipoCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ResponderConvocatoriaCommandHandlerTests.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/CancelarInscripcionEquipoCommandHandlerTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B8: commands ResponderConvocatoria + CancelarInscripcionEquipo (handlers + validators)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B9: Infrastructure — `IdentityEquipoHttpClient` + DI

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/IdentityEquipoHttpClient.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/DependencyInjection.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/IdentityEquipoHttpClientTests.cs`

**Interfaces:**
- Consumes: de B1 → `IEquipoDirectoryClient`, `EquipoSnapshotDto`, `MiembroEquipoDto`; de B7 → `IdentityInaccesibleException`. Patrón `PartidasConfigHttpClient`.
- Produces: `IdentityEquipoHttpClient`; registro DI `AddHttpClient<IEquipoDirectoryClient, IdentityEquipoHttpClient>`.

- [ ] **Step 1: Escribir el test (rojo)**

Crear `IdentityEquipoHttpClientTests.cs`:

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Infrastructure.Services;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure;

public class IdentityEquipoHttpClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        public StubHandler(HttpStatusCode status, string body = "") { _status = status; _body = body; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(_status)
            { Content = new StringContent(_body, Encoding.UTF8, "application/json") });
    }

    private static IdentityEquipoHttpClient Build(HttpStatusCode status, string body = "")
        => new(new HttpClient(new StubHandler(status, body)) { BaseAddress = new Uri("http://identity.test") });

    [Fact]
    public async Task Ok_mapea_snapshot()
    {
        var lider = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var body = $$"""
        {"equipoId":"{{equipoId}}","nombreEquipo":"Halcones","estado":"Activo",
         "participantes":[{"usuarioId":"{{lider}}","esLider":true}]}
        """;
        var client = Build(HttpStatusCode.OK, body);

        var r = await client.ObtenerMiEquipoAsync("Bearer x", CancellationToken.None);

        Assert.NotNull(r);
        Assert.Equal(equipoId, r!.EquipoId);
        Assert.Equal("Halcones", r.NombreEquipo);
        Assert.True(r.Miembros[0].EsLider);
        Assert.Equal(lider, r.Miembros[0].UsuarioId);
    }

    [Fact]
    public async Task NotFound_devuelve_null()
    {
        var client = Build(HttpStatusCode.NotFound);
        Assert.Null(await client.ObtenerMiEquipoAsync("Bearer x", CancellationToken.None));
    }

    [Fact]
    public async Task ServerError_lanza_identity_inaccesible()
    {
        var client = Build(HttpStatusCode.InternalServerError);
        await Assert.ThrowsAsync<IdentityInaccesibleException>(
            () => client.ObtenerMiEquipoAsync("Bearer x", CancellationToken.None));
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila.

- [ ] **Step 3: Crear el cliente HTTP**

Crear `IdentityEquipoHttpClient.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

// GET /api/teams/mine en Identity → snapshot de membresía del líder autenticado.
// 404 → null (sin equipo activo); red/timeout/non-success → IdentityInaccesible (Identity caído ≠ sin equipo).
public sealed class IdentityEquipoHttpClient : IEquipoDirectoryClient
{
    private readonly HttpClient _http;

    public IdentityEquipoHttpClient(HttpClient http) => _http = http;

    public async Task<EquipoSnapshotDto?> ObtenerMiEquipoAsync(string? bearerToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/teams/mine");
        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.TryAddWithoutValidation("Authorization", bearerToken);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new IdentityInaccesibleException(ex);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
            throw new IdentityInaccesibleException();

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<EquipoMineResponse>(cancellationToken: cancellationToken)
                ?? throw new IdentityInaccesibleException();

            return new EquipoSnapshotDto(
                payload.EquipoId,
                payload.NombreEquipo,
                payload.Participantes.Select(p => new MiembroEquipoDto(p.UsuarioId, p.EsLider)).ToList());
        }
        catch (JsonException ex)
        {
            throw new IdentityInaccesibleException(ex);
        }
    }

    // Deserialización local del contrato identity-api GET /api/teams/mine (camelCase; binding case-insensitive).
    private sealed record EquipoMineResponse(Guid EquipoId, string NombreEquipo, string Estado, List<MiembroResponse> Participantes);
    private sealed record MiembroResponse(Guid UsuarioId, bool EsLider);
}
```

- [ ] **Step 4: Registrar en DI**

En `DependencyInjection.cs`, tras el bloque `AddHttpClient<IConfiguracionPartidaClient, ...>`, añadir:

```csharp
        var identityBaseUrl = configuration["IdentityApi:BaseUrl"] ?? "http://localhost:5000";
        services.AddHttpClient<IEquipoDirectoryClient, IdentityEquipoHttpClient>(client =>
        {
            client.BaseAddress = new Uri(identityBaseUrl);
        });
```

- [ ] **Step 5: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/IdentityEquipoHttpClient.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/DependencyInjection.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/IdentityEquipoHttpClientTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B9: IdentityEquipoHttpClient (GET /api/teams/mine, 404→null, error→IdentityInaccesible) + DI

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B10: Persistencia — migración + config EF de Convocatoria

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs`
- Create (generado): `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/<timestamp>_SP3eParticipacionEquipo.cs` (+ `.Designer.cs`)
- Modify (generado): `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/OperacionesSesionDbContextModelSnapshot.cs`

**Interfaces:**
- Consumes: de B2 → `Convocatoria`, `ConvocatoriaId`; de B3 → `InscripcionPartida.Modalidad/EquipoId/Convocatorias`.
- Produces: tablas/columnas `inscripciones.modalidad`, `inscripciones.equipoid`, tabla `convocatorias`.

- [ ] **Step 1: Añadir el converter y la config en `OperacionesSesionDbContext.cs`**

Junto a los otros `ValueConverter` (tras `InscripcionIdConverter`), añadir:

```csharp
    private static readonly ValueConverter<ConvocatoriaId, Guid> ConvocatoriaIdConverter =
        new(v => v.Valor, v => ConvocatoriaId.From(v));
```

En el bloque `modelBuilder.Entity<InscripcionPartida>(...)`, tras la línea de `FechaInscripcion`, añadir la config de las columnas nuevas y la relación con convocatorias:

```csharp
            entity.Property(x => x.Modalidad).HasColumnName("modalidad").IsRequired();
            entity.Property(x => x.EquipoId).HasColumnName("equipoid");
            entity.HasMany(x => x.Convocatorias).WithOne().HasForeignKey("inscripcionid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Convocatorias).UsePropertyAccessMode(PropertyAccessMode.Field);
```

Añadir un nuevo bloque de entidad (tras el de `InscripcionPartida`):

```csharp
        modelBuilder.Entity<Convocatoria>(entity =>
        {
            entity.ToTable("convocatorias");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").HasConversion(ConvocatoriaIdConverter);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.EquipoId).HasColumnName("equipoid").IsRequired();
            entity.Property(x => x.UsuarioId).HasColumnName("usuarioid").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.FechaEnvio).HasColumnName("fechaenvio").IsRequired();
            entity.Property(x => x.FechaRespuesta).HasColumnName("fecharespuesta");
        });
```

- [ ] **Step 2: Compilar Infrastructure (verificar que el modelo compila antes de generar la migración)**

Run: `dotnet build services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Umbral.OperacionesSesion.Infrastructure.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Generar la migración**

Run:
```bash
dotnet ef migrations add SP3eParticipacionEquipo \
  --project services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure \
  --startup-project services/operaciones-sesion/src/Umbral.OperacionesSesion.Api
```
Expected: se crean `<timestamp>_SP3eParticipacionEquipo.cs` + `.Designer.cs` y se actualiza `OperacionesSesionDbContextModelSnapshot.cs`.

- [ ] **Step 4: Verificar el `Up()` generado**

Abrir `<timestamp>_SP3eParticipacionEquipo.cs` y confirmar que `Up()`:
- `AddColumn<int>("modalidad", "inscripciones", nullable: false, defaultValue: 0)` (Individual=0 para filas existentes),
- `AddColumn<Guid>("equipoid", "inscripciones", nullable: true)`,
- `CreateTable("convocatorias", ...)` con columnas `id, partidaid, equipoid, usuarioid, estado, fechaenvio, fecharespuesta(null)` y FK `inscripcionid` → `inscripciones(id)` `OnDelete: Cascade`.

Si `modalidad` se generó **sin** `defaultValue`, editar el `AddColumn<int>` para incluir `defaultValue: 0` (necesario si hay filas Individual preexistentes).

- [ ] **Step 5: Compilar para confirmar que la migración compila**

Run: `dotnet build services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Umbral.OperacionesSesion.Api.csproj`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/
git commit -m "$(cat <<'EOF'
SP-3e-1b B10: migración SP3eParticipacionEquipo (inscripciones.modalidad/equipoid + tabla convocatorias)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B11: Repositorio — métodos Equipo + includes + participación activa extendida

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/SesionPartidaRepositoryEquipoTests.cs`

**Interfaces:**
- Consumes: de B7 (interfaz) → firmas `EquipoTieneParticipacionActivaAsync`, `GetByConvocatoriaIdAsync` (declaradas en `ISesionPartidaRepository`); de B2/B3 → `Convocatoria`, `EstadoConvocatoria`, `InscripcionPartida.EquipoId/Convocatorias`.
- Produces: implementación de los 2 métodos; `ParticipanteTieneParticipacionActivaAsync` y `GetByParticipanteActivoAsync` extendidos con convocatoria aceptada; `GetByPartidaIdAsync` incluye `Convocatorias`.

- [ ] **Step 1: Escribir los integration tests (rojo)**

Crear `SesionPartidaRepositoryEquipoTests.cs`:

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class SesionPartidaRepositoryEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private static OperacionesSesionDbContext NewCtx(string name) =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options);

    private static SesionPartida PartidaEquipo(Guid partidaId)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        return SesionPartida.Publicar(partidaId, snap);
    }

    [Fact]
    public async Task GetByConvocatoriaId_encuentra_la_sesion()
    {
        await using var ctx = NewCtx("equipo-conv-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);
        var sesion = PartidaEquipo(Guid.NewGuid());
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { Guid.NewGuid() }, false, 0, T0);
        var convocatoriaId = insc.Convocatorias[0].Id.Valor;
        repo.Add(sesion);
        await ctx.SaveChangesAsync();

        var r = await repo.GetByConvocatoriaIdAsync(convocatoriaId, CancellationToken.None);

        Assert.NotNull(r);
        Assert.Equal(sesion.PartidaId, r!.PartidaId);
        Assert.Contains(r.Inscripciones.SelectMany(i => i.Convocatorias), c => c.Id.Valor == convocatoriaId);
    }

    [Fact]
    public async Task EquipoTieneParticipacionActiva_detecta_en_otra_partida()
    {
        await using var ctx = NewCtx("equipo-act-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);
        var equipoId = Guid.NewGuid();
        var otra = PartidaEquipo(Guid.NewGuid());
        otra.PreinscribirEquipo(equipoId, true, new[] { Guid.NewGuid() }, false, 0, T0);
        repo.Add(otra);
        await ctx.SaveChangesAsync();

        var r = await repo.EquipoTieneParticipacionActivaAsync(equipoId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(r);
    }

    [Fact]
    public async Task ParticipanteConConvocatoriaAceptada_cuenta_como_participacion_activa()
    {
        await using var ctx = NewCtx("equipo-partact-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);
        var usuario = Guid.NewGuid();
        var sesion = PartidaEquipo(Guid.NewGuid());
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { usuario }, false, 0, T0);
        sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);
        repo.Add(sesion);
        await ctx.SaveChangesAsync();

        var r = await repo.ParticipanteTieneParticipacionActivaAsync(usuario, Guid.NewGuid(), CancellationToken.None);

        Assert.True(r);
    }

    [Fact]
    public async Task MiSesion_encuentra_sesion_por_convocatoria_aceptada()
    {
        await using var ctx = NewCtx("equipo-misesion-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);
        var usuario = Guid.NewGuid();
        var sesion = PartidaEquipo(Guid.NewGuid());
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { usuario }, false, 0, T0);
        sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);
        repo.Add(sesion);
        await ctx.SaveChangesAsync();

        var r = await repo.GetByParticipanteActivoAsync(usuario, CancellationToken.None);

        Assert.NotNull(r);
        Assert.Equal(sesion.PartidaId, r!.PartidaId);
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj`
Expected: FAIL — los tests 3 y 4 (`ParticipanteConConvocatoriaAceptada_...` y `MiSesion_encuentra_...`) fallan en rojo: los predicados de `ParticipanteTieneParticipacionActivaAsync`/`GetByParticipanteActivoAsync` aún no cuentan convocatoria aceptada. Los tests 1 y 2 pasan (sus métodos se implementaron en B7).

- [ ] **Step 3: Extender los métodos existentes en `SesionPartidaRepository.cs`**

(Los 2 métodos nuevos `EquipoTieneParticipacionActivaAsync`/`GetByConvocatoriaIdAsync` ya se implementaron en B7 — esta tarea NO los toca; los cubre con los integration tests 1-2 y extiende los métodos preexistentes.)

En `GetByPartidaIdAsync`, cambiar el include de inscripciones para traer convocatorias:

```csharp
            .Include(s => s.Inscripciones).ThenInclude(i => i.Convocatorias)
```
(reemplaza `.Include(s => s.Inscripciones)`).

En `GetByParticipanteActivoAsync`, cambiar el include de inscripciones igual y extender el predicado para aceptar convocatoria aceptada:

```csharp
            .Include(s => s.Inscripciones).ThenInclude(i => i.Convocatorias)
```
y el `FirstOrDefaultAsync`:

```csharp
            .FirstOrDefaultAsync(
                s => s.Inscripciones.Any(i => i.Estado == EstadoInscripcion.Activa
                    && (i.ParticipanteId == participanteId
                        || i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.Estado == EstadoConvocatoria.Aceptada))),
                cancellationToken);
```

En `ParticipanteTieneParticipacionActivaAsync`, extender el predicado:

```csharp
    public Task<bool> ParticipanteTieneParticipacionActivaAsync(
        Guid participanteId, Guid exceptPartidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Where(s => s.PartidaId != exceptPartidaId
                && (s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada))
            .SelectMany(s => s.Inscripciones)
            .AnyAsync(i => i.Estado == EstadoInscripcion.Activa
                && (i.ParticipanteId == participanteId
                    || i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.Estado == EstadoConvocatoria.Aceptada)),
                cancellationToken);
```

(`using Umbral.OperacionesSesion.Domain.Enums;` ya está presente — usado por `EstadoSesion`; `EstadoConvocatoria` vive en el mismo namespace. `using ...Domain.ValueObjects;` lo añadió B7.)

- [ ] **Step 4: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj`
Expected: PASS (los 4 nuevos + suite de integración existente).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/SesionPartidaRepositoryEquipoTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B11: repo Equipo (EquipoTieneParticipacionActiva + GetByConvocatoriaId) + participación activa por convocatoria aceptada

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B12: API — endpoints Equipo + mapeo de excepciones

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerEquipoTests.cs`

**Interfaces:**
- Consumes: de B7 → `PreinscribirEquipoCommand`, `PreinscripcionEquipoResponse`; de B8 → `ResponderConvocatoriaCommand`, `ConvocatoriaResponse`, `CancelarInscripcionEquipoCommand`; de B4/B5/B7 → excepciones para el mapeo. Existentes: `ISender _mediator`, `ObtenerParticipanteId()`, patrón `FakeSender`/`WithUser`, `Request.Headers.Authorization`.
- Produces: acciones `PreinscribirEquipo`, `AceptarConvocatoria`, `RechazarConvocatoria`, `CancelarInscripcionEquipo`; mapeos de status.

- [ ] **Step 1: Escribir los tests del controller (rojo)**

Crear `SesionesControllerEquipoTests.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerEquipoTests
{
    [Fact]
    public async Task PreinscribirEquipo_dispatches_command_con_lider_del_claim()
    {
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var sender = new FakeSender(new PreinscripcionEquipoResponse(Guid.NewGuid(), Guid.NewGuid(), 3));
        var controller = WithUser(sender, lider);

        var result = await controller.PreinscribirEquipo(partidaId, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        var cmd = Assert.IsType<PreinscribirEquipoCommand>(sender.LastRequest);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(lider, cmd.LiderId);
    }

    [Fact]
    public async Task AceptarConvocatoria_dispatches_con_aceptar_true()
    {
        var convocatoriaId = Guid.NewGuid();
        var usuario = Guid.NewGuid();
        var sender = new FakeSender(new ConvocatoriaResponse(convocatoriaId, "Aceptada"));
        var controller = WithUser(sender, usuario);

        var result = await controller.AceptarConvocatoria(convocatoriaId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var cmd = Assert.IsType<ResponderConvocatoriaCommand>(sender.LastRequest);
        Assert.Equal(convocatoriaId, cmd.ConvocatoriaId);
        Assert.Equal(usuario, cmd.UsuarioId);
        Assert.True(cmd.Aceptar);
    }

    [Fact]
    public async Task RechazarConvocatoria_dispatches_con_aceptar_false()
    {
        var convocatoriaId = Guid.NewGuid();
        var usuario = Guid.NewGuid();
        var sender = new FakeSender(new ConvocatoriaResponse(convocatoriaId, "Rechazada"));
        var controller = WithUser(sender, usuario);

        var result = await controller.RechazarConvocatoria(convocatoriaId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var cmd = Assert.IsType<ResponderConvocatoriaCommand>(sender.LastRequest);
        Assert.False(cmd.Aceptar);
    }

    [Fact]
    public async Task CancelarInscripcionEquipo_dispatches_y_devuelve_204()
    {
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var sender = new FakeSender(MediatR.Unit.Value);
        var controller = WithUser(sender, lider);

        var result = await controller.CancelarInscripcionEquipo(partidaId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var cmd = Assert.IsType<CancelarInscripcionEquipoCommand>(sender.LastRequest);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(lider, cmd.LiderId);
    }
}
```

> Reutiliza los helpers `FakeSender` y `WithUser(sender, userId)` ya usados por los tests del controller (p. ej. `SesionesControllerBdtTests`). Si `WithUser` está en una clase auxiliar compartida, no hace falta redefinirlo; si es privado por archivo, replicar el mismo patrón mínimo en este archivo.

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila (las acciones no existen).

- [ ] **Step 3: Añadir las acciones en `SesionesController.cs`**

Añadir junto a las acciones de inscripción (tras `CancelarInscripcion`):

```csharp
    [HttpPost("partidas/{partidaId:guid}/inscripciones-equipo")]
    public async Task<IActionResult> PreinscribirEquipo(Guid partidaId, CancellationToken cancellationToken)
    {
        var liderId = ObtenerParticipanteId();
        var bearer = Request.Headers.Authorization.ToString();
        var response = await _mediator.Send(
            new PreinscribirEquipoCommand(partidaId, liderId, string.IsNullOrWhiteSpace(bearer) ? null : bearer),
            cancellationToken);
        return CreatedAtAction(nameof(ObtenerLobby), new { partidaId }, response);
    }

    [HttpDelete("partidas/{partidaId:guid}/inscripciones-equipo/mia")]
    public async Task<IActionResult> CancelarInscripcionEquipo(Guid partidaId, CancellationToken cancellationToken)
    {
        var liderId = ObtenerParticipanteId();
        var bearer = Request.Headers.Authorization.ToString();
        await _mediator.Send(
            new CancelarInscripcionEquipoCommand(partidaId, liderId, string.IsNullOrWhiteSpace(bearer) ? null : bearer),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("convocatorias/{convocatoriaId:guid}/aceptacion")]
    public async Task<IActionResult> AceptarConvocatoria(Guid convocatoriaId, CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerParticipanteId();
        var response = await _mediator.Send(
            new ResponderConvocatoriaCommand(convocatoriaId, usuarioId, true), cancellationToken);
        return Ok(response);
    }

    [HttpPost("convocatorias/{convocatoriaId:guid}/rechazo")]
    public async Task<IActionResult> RechazarConvocatoria(Guid convocatoriaId, CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerParticipanteId();
        var response = await _mediator.Send(
            new ResponderConvocatoriaCommand(convocatoriaId, usuarioId, false), cancellationToken);
        return Ok(response);
    }
```

- [ ] **Step 4: Mapear las excepciones en `ExceptionHandlingMiddleware.cs`**

Añadir `using Umbral.OperacionesSesion.Domain.Exceptions;` ya está presente. En `MapStatus`:

Añadir a la rama `NotFound` `ConvocatoriaNoEncontradaException`:

```csharp
        PartidaConfigNoEncontradaException
            or SesionNoEncontradaException
            or InscripcionNoEncontradaException
            or ConvocatoriaNoEncontradaException => HttpStatusCode.NotFound,
```

Añadir una rama `Forbidden` para `NoEsLiderEquipoException` (junto a `ParticipanteNoInscritoException`):

```csharp
        ParticipanteNoInscritoException
            or NoEsLiderEquipoException => HttpStatusCode.Forbidden,
```

Añadir a la rama `BadGateway` `IdentityInaccesibleException`:

```csharp
        PartidasConfigInaccesibleException
            or IdentityInaccesibleException => HttpStatusCode.BadGateway,
```

Añadir a la rama `Conflict` (409) `EquipoYaInscritoException` y `SinEquipoActivoException`:

```csharp
            or EquipoYaInscritoException
            or SinEquipoActivoException
```
(insertar dentro del grupo `or ... => HttpStatusCode.Conflict`).

(`IdentityInaccesibleException` y `SinEquipoActivoException` son `Application.Exceptions`; añadir `using Umbral.OperacionesSesion.Application.Exceptions;` — ya presente porque el middleware referencia `PartidasConfigInaccesibleException`.)

- [ ] **Step 5: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Middleware/ExceptionHandlingMiddleware.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerEquipoTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B12: endpoints Equipo (preinscripción/convocatoria aceptar-rechazar/cancelar) + mapeo excepciones

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B13: Proyecciones — lobby (equipos) + mi-sesión (convocatoria propia)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/LobbyDto.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/MiSesionDto.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PublicarPartidaCommandHandler.cs` (método estático `MapearLobby`)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ProyeccionesEquipoTests.cs`

**Interfaces:**
- Consumes: de B3 → `InscripcionPartida.Modalidad/EquipoId/Convocatorias/ConvocatoriasAceptadas`; de B2 → `Convocatoria`. Existentes: `MapearLobby`, `ObtenerMiSesionQueryHandler`, `GetByParticipanteActivoAsync` (extendido en B11).
- Produces: `LobbyDto += IReadOnlyList<EquipoLobbyDto> Equipos`; `EquipoLobbyDto(Guid EquipoId, int Convocados, int Aceptados)`; `MiSesionDto += MiConvocatoriaDto? Convocatoria`; `MiConvocatoriaDto(Guid ConvocatoriaId, Guid EquipoId, string Estado)`.

- [ ] **Step 1: Escribir los tests (rojo)**

Crear `ProyeccionesEquipoTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ProyeccionesEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida PartidaEquipo(Guid partidaId)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        return SesionPartida.Publicar(partidaId, snap);
    }

    [Fact]
    public void MapearLobby_expone_equipos_con_convocados_y_aceptados()
    {
        var sesion = PartidaEquipo(Guid.NewGuid());
        var usuario = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { usuario, Guid.NewGuid() }, false, 0, T0);
        sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);

        var lobby = PublicarPartidaCommandHandler.MapearLobby(sesion);

        var equipo = Assert.Single(lobby.Equipos);
        Assert.Equal(2, equipo.Convocados);
        Assert.Equal(1, equipo.Aceptados);
    }

    [Fact]
    public async Task MiSesion_expone_convocatoria_del_convocado()
    {
        var partidaId = Guid.NewGuid();
        var usuario = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var sesion = PartidaEquipo(partidaId);
        var insc = sesion.PreinscribirEquipo(equipoId, true, new[] { usuario }, false, 0, T0);
        sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerMiSesionQueryHandler(repo);

        var dto = await handler.Handle(new ObtenerMiSesionQuery(usuario), default);

        Assert.NotNull(dto);
        Assert.NotNull(dto!.Convocatoria);
        Assert.Equal("Aceptada", dto.Convocatoria!.Estado);
        Assert.Equal(equipoId, dto.Convocatoria.EquipoId);
        Assert.Equal(insc.Convocatorias[0].Id.Valor, dto.Convocatoria.ConvocatoriaId);
    }
}
```

> `FakeSesionPartidaRepository.GetByParticipanteActivoAsync` debe devolver la sesión cuando el usuario tiene convocatoria aceptada. Ajustar el fake (Step 3) para replicar el predicado extendido de B11 si aún filtra solo por `ParticipanteId`.

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila (`lobby.Equipos`, `dto.Convocatoria` no existen).

- [ ] **Step 3: Extender los DTOs**

En `LobbyDto.cs`, reemplazar el record por:

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
    IReadOnlyList<EquipoLobbyDto> Equipos);

public sealed record EquipoLobbyDto(Guid EquipoId, int Convocados, int Aceptados);
```

En `MiSesionDto.cs`, añadir el campo `Convocatoria` al final del record y el DTO auxiliar:

```csharp
public sealed record MiSesionDto(
    Guid PartidaId,
    Guid SesionPartidaId,
    string EstadoPartida,
    string Modalidad,
    InscripcionResumenDto Inscripcion,
    JuegoActivoResumenDto? JuegoActivo,
    PreguntaActualDto? PreguntaActual,
    EtapaActualDto? EtapaActual,
    bool? YaRespondioPreguntaActual,
    MiConvocatoriaDto? Convocatoria);

public sealed record MiConvocatoriaDto(Guid ConvocatoriaId, Guid EquipoId, string Estado);
```

(`InscripcionResumenDto` y `JuegoActivoResumenDto` se conservan tal cual.)

- [ ] **Step 4: Actualizar `MapearLobby` en `PublicarPartidaCommandHandler.cs`**

En el `return new LobbyDto(...)` de `MapearLobby`, añadir el argumento `Equipos` al final:

```csharp
            sesion.Inscripciones
                .Where(i => i.Modalidad == Modalidad.Equipo && i.EsActiva && i.EquipoId is not null)
                .Select(i => new EquipoLobbyDto(i.EquipoId!.Value, i.Convocatorias.Count, i.ConvocatoriasAceptadas))
                .ToList());
```

(Requiere `using Umbral.OperacionesSesion.Domain.Enums;` y `using System.Linq;` — verificar que estén; el archivo ya usa `sesion.Inscripciones`. El resto de los argumentos existentes de `LobbyDto` no cambian: `Participantes` sigue siendo la lista de `ParticipanteId` de inscripciones Individual activas.)

- [ ] **Step 5: Actualizar `ObtenerMiSesionQueryHandler.cs` para la ruta Equipo**

Reemplazar el bloque que obtiene `inscripcion`/`inscDto` (que hoy asume Individual con `First(i => i.ParticipanteId == ...)`) por una resolución tolerante a Equipo, y computar `Convocatoria`:

```csharp
        var inscripcion = sesion.Inscripciones.FirstOrDefault(
            i => i.EsActiva && i.ParticipanteId == request.ParticipanteId);

        var convocatoria = sesion.Inscripciones
            .Where(i => i.EsActiva)
            .SelectMany(i => i.Convocatorias)
            .FirstOrDefault(c => c.UsuarioId == request.ParticipanteId);

        var inscId = inscripcion?.Id.Valor ?? Guid.Empty;
        var inscEstado = inscripcion?.Estado.ToString() ?? "Equipo";
        var inscDto = new InscripcionResumenDto(inscId, inscEstado);

        MiConvocatoriaDto? convocatoriaDto = convocatoria is null
            ? null
            : new MiConvocatoriaDto(convocatoria.Id.Valor, convocatoria.EquipoId, convocatoria.Estado.ToString());
```

Y en el `return new MiSesionDto(...)`, añadir `convocatoriaDto` como último argumento:

```csharp
        return new MiSesionDto(
            sesion.PartidaId, sesion.Id.Valor, sesion.Estado.ToString(), sesion.Modalidad.ToString(),
            inscDto, juegoDto, preguntaDto, etapaDto, yaRespondio, convocatoriaDto);
```

- [ ] **Step 6: Ajustar el `FakeSesionPartidaRepository.GetByParticipanteActivoAsync` (si aplica)**

Si el fake filtra solo por `ParticipanteId`, extenderlo para incluir convocatoria aceptada:

```csharp
    public Task<SesionPartida?> GetByParticipanteActivoAsync(Guid participanteId, CancellationToken cancellationToken)
        => Task.FromResult(_sesiones.FirstOrDefault(s =>
            (s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada)
            && s.Inscripciones.Any(i => i.EsActiva
                && (i.ParticipanteId == participanteId
                    || i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.EstaAceptada)))));
```

(ajustar `_sesiones` al campo real del fake; `using System.Linq;` + `Domain.Enums`.)

- [ ] **Step 7: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS. Verificar que los tests Individual de lobby/mi-sesión siguen verdes (Individual: `Equipos` vacío, `Convocatoria` null).

- [ ] **Step 8: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/LobbyDto.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/MiSesionDto.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PublicarPartidaCommandHandler.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ProyeccionesEquipoTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B13: proyecciones Equipo (lobby.Equipos + mi-sesion.Convocatoria del convocado)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task B14: Contrato HTTP + Realtime + traceability (carve-out)

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeContractTests.cs`
- Modify (WRITE, **NO commitear** — carve-out): `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: de B6 → `SesionRealtimeMessages.ConvocatoriaCreada`; firmas de endpoints/DTOs de B7/B8/B12.
- Produces: contrato documentado; `RealtimeContractTests` verifica `ConvocatoriaCreada`.

- [ ] **Step 1: Añadir el InlineData al test de contrato (rojo)**

En `RealtimeContractTests.cs`, dentro del `[Theory]` que verifica que cada mensaje del código está documentado, añadir tras la línea de `PistaEnviada`:

```csharp
    [InlineData(nameof(SesionRealtimeMessages.ConvocatoriaCreada))]
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`
Expected: FAIL — `Assert.Contains("ConvocatoriaCreada", contrato)` falla (aún no documentado).

- [ ] **Step 3: Documentar los endpoints HTTP en `operaciones-sesion-api.md`**

En la tabla **Endpoint Registry**, añadir tras la fila de inscripciones (Individual):

```markdown
| Preinscribir equipo (líder) | POST | `/operaciones-sesion/partidas/{partidaId}/inscripciones-equipo` | Participante (líder) | 201 + PreinscripcionEquipoResponse | 404 sesión no existe · 403 no es líder · 409 equipo ya inscrito / participación activa en otra / cupo lleno / sin equipo activo · 502 Identity inaccesible |
| Cancelar preinscripción de equipo (líder) | DELETE | `/operaciones-sesion/partidas/{partidaId}/inscripciones-equipo/mia` | Participante (líder) | 204 | 404 sesión/inscripción no existe · 403 no es líder · 409 no en lobby · 502 Identity inaccesible |
| Aceptar convocatoria | POST | `/operaciones-sesion/convocatorias/{convocatoriaId}/aceptacion` | Participante (convocado) | 200 + ConvocatoriaResponse | 404 convocatoria no encontrada · 409 no en lobby / participación activa en otra |
| Rechazar convocatoria | POST | `/operaciones-sesion/convocatorias/{convocatoriaId}/rechazo` | Participante (convocado) | 200 + ConvocatoriaResponse | 404 convocatoria no encontrada · 409 no en lobby |
```

En la sección **### DTOs**, añadir:

```markdown
- `PreinscripcionEquipoResponse { inscripcionId, equipoId, convocados }` (líder preinscribe su equipo; el equipo y miembros se toman por snapshot de `GET /api/teams/mine` en Identity; genera una convocatoria por integrante)
- `ConvocatoriaResponse { convocatoriaId, estado }` (`estado` ∈ `Pendiente|Aceptada|Rechazada`)
- `LobbyDto.equipos: [{ equipoId, convocados, aceptados }]` (solo modalidad Equipo)
- `MiSesionDto.convocatoria: { convocatoriaId, equipoId, estado } | null` (estado de la convocatoria del caller en modalidad Equipo)
```

- [ ] **Step 4: Documentar el mensaje Realtime**

En la sección **## Realtime / SignalR**, tabla *Servidor → cliente*, añadir tras `PistaEnviada`:

```markdown
| `ConvocatoriaCreada` *(convocado-destino only)* | `{ partidaId, equipoId, convocatoriaId, usuarioId }` |
```

En el párrafo **Notas**, añadir:

```markdown
`ConvocatoriaCreada` (SP-3e-1) se difunde SOLO al grupo `participante:{usuarioId}` del convocado (para que el móvil muestre la convocatoria entrante); no llega al grupo de partida ni al operador. `ConvocatoriaRespondida` no tiene push en tiempo real en este slice (alimenta audit/scoring vía broker RabbitMQ, diferido). Snapshot de membresía: los miembros a convocar se congelan al preinscribir (altas/bajas del equipo posteriores no afectan esa partida).
```

- [ ] **Step 5: Correr el test de contrato y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`
Expected: PASS.

- [ ] **Step 6: Escribir la fila de traceability (NO commitear — carve-out)**

Editar `docs/04-sdd/traceability-matrix.md` y añadir la fila SP-3e-1 (participación Equipo: HU de inscripción/convocatoria por equipo → endpoints `inscripciones-equipo` + `convocatorias/{id}/aceptacion|rechazo`, eventos `ConvocatoriaCreada`/`ConvocatoriaRespondida`, dominio `PreinscribirEquipo`/`ResponderConvocatoria`, snapshot Identity `GET /api/teams/mine`), siguiendo el formato de las filas SP-3f/SP-3g existentes. **Este archivo NO se incluye en el commit.**

- [ ] **Step 7: Commit (SOLO contrato + test; traceability queda unstaged)**

```bash
git add contracts/http/operaciones-sesion-api.md \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeContractTests.cs
git commit -m "$(cat <<'EOF'
SP-3e-1b B14: contrato HTTP+Realtime de participación Equipo (endpoints + ConvocatoriaCreada) + test doc↔constantes

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 8: Verificar el carve-out intacto**

Run: `git status --short`
Expected: `docs/04-sdd/traceability-matrix.md` aparece como `M` (sin commitear); `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md` y `docs/04-sdd/auditorias/` siguen unstaged. Ningún archivo de docs commiteado en B14.

---

## Self-Review

**1. Spec coverage (spec SP-3e-1):**
- Snapshot HTTP a Identity (puerto + cliente + fake) → B1, B9. ✅
- Dominio Convocatoria (enum/VO/entidad) → B2. ✅
- InscripcionPartida extendida (Modalidad/EquipoId/convocatorias) → B3. ✅
- SesionPartida.PreinscribirEquipo + guards (lobby/modalidad/líder/ya-inscrito/participación-activa/cupo) → B4. ✅
- ResponderConvocatoria + CancelarInscripcionEquipo + mínimos Equipo (≥1 aceptado, cancela sesión) → B5. ✅
- Eventos ConvocatoriaCreada/Respondida por seam (15/16, 5 impls) + SignalR a `participante:{convocado}` → B6. ✅
- Commands/handlers/validators/DTOs (preinscribir, responder, cancelar) + app exceptions → B7, B8. ✅
- Cliente HTTP Identity + DI → B9. ✅
- Migración + config EF (Modalidad/EquipoId + tabla convocatorias) → B10. ✅
- Repo (EquipoTieneParticipacionActiva + GetByConvocatoriaId + participación activa por convocatoria + includes) → B11. ✅
- Endpoints API (4) + mapeo excepciones (403/404/409/502) → B12. ✅
- Proyecciones lobby (equipos) + mi-sesión (convocatoria) → B13. ✅
- Contrato HTTP+Realtime + traceability carve-out → B14. ✅
- Participante activo (Equipo) = convocatoria aceptada → usado en B5 (mínimos), B11 (participación activa), B13 (proyección). ✅
- Fuera de alcance (runtime Trivia/BDT/pistas Equipo, broker real, scoring, clientes) → sin tareas (correcto). ✅

**2. Placeholder scan:** sin TBD/TODO. Único punto con instrucción condicional (no placeholder): ajustar nombre del campo interno del fake (`_sesiones`) al real — es una nota de adaptación al fake existente, con el código completo provisto. ✅

**3. Type consistency:**
- `EquipoSnapshotDto(EquipoId, NombreEquipo, Miembros)` + `MiembroEquipoDto(UsuarioId, EsLider)` idénticos en B1 (def), B7/B8 (uso), B9 (mapeo). ✅
- `Convocatoria` / `ConvocatoriaId` / `EstadoConvocatoria` producidos en B2, consumidos en B3–B13. ✅
- `SesionPartida.PreinscribirEquipo(equipoId, callerEsLider, miembros, equipoTieneParticipacionActivaEnOtra, equiposActivos, fecha)` idéntico en B4 (def), B7 (uso). ✅
- `ResponderConvocatoria(convocatoriaId, usuarioId, aceptar, participanteTieneParticipacionActivaEnOtra, now)` idéntico en B5 (def), B8 (uso). ✅
- `ConvocatoriaCreadaEvent`/`ConvocatoriaRespondidaEvent` idénticos en B6 (def), B7/B8 (uso), fan-out/fake. ✅
- `ISesionPartidaRepository.EquipoTieneParticipacionActivaAsync`/`GetByConvocatoriaIdAsync` declarados en B7 (interfaz), implementados en B11 (impl) y en fakes (B7). ✅
- `PreinscripcionEquipoResponse(InscripcionId, EquipoId, Convocados)` / `ConvocatoriaResponse(ConvocatoriaId, Estado)` idénticos en B7 (def), B8/B12 (uso), B14 (contrato JSON camelCase). ✅
- `LobbyDto.Equipos` / `EquipoLobbyDto` / `MiSesionDto.Convocatoria` / `MiConvocatoriaDto` producidos en B13, documentados en B14. ✅

## Execution Handoff

Plan guardado en `docs/superpowers/plans/2026-07-01-sp3e1b-operaciones-participacion-equipo.md`.
