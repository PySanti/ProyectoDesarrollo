# SP-3d — Runtime BDT (Búsqueda del Tesoro, Individual) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Materializar el runtime en vivo de un `JuegoBDT` modalidad Individual en Operaciones de Sesión: validación automática de QR, secuencia de etapas con ventana de tiempo, cierre por hallazgo/tiempo, avance automático y eventos de dominio por puerto No-Op.

**Architecture:** Espejo estructural de SP-3c (runtime Trivia). `JuegoResumen` se extiende de forma aditiva (gana `_etapas` junto a `_preguntas`, sin tocar la rama Trivia). `EtapaSnapshot`/`TesoroQR` son entidades nuevas. La validación de QR pasa por el puerto `IQrDecoder` (impl real `ZXingQrDecoder` en Infrastructure, fakes en tests). Eventos por `NoOpSesionEventsPublisher`. Estado runtime en la propia DB (`umbral_operaciones_sesion`); las etapas llegan solo por snapshot HTTP `GET /partidas/{id}`.

**Tech Stack:** .NET 8, Clean Architecture + CQRS (MediatR), EF Core 8 (Npgsql + InMemory fallback), xUnit, FluentValidation, ZXing.Net (decodificación QR), WebApplicationFactory (contract tests).

**Spec:** `docs/superpowers/specs/2026-06-28-sp3d-runtime-bdt-design.md`

## Global Constraints

- **Estructura graduada (R1):** Application top-level exactamente {Commands, Queries, Interfaces, Validators, DTOs, Handlers, Handlers/Commands, Handlers/Queries, Exceptions}; interfaces de repo en `Domain/Abstractions/Persistence`; `IQrDecoder` en `Domain/Abstractions`; `ZXingQrDecoder` en `Infrastructure/Services`; `Program.cs` solo `MapControllers` + middleware; cada controller con unit tests.
- **Boundary (ADR-0010):** Operaciones nunca lee/escribe otra DB; las etapas llegan solo por snapshot HTTP `GET /partidas/{id}` (read-only). Estado runtime en `umbral_operaciones_sesion`.
- **Eventos** por `NoOpSesionEventsPublisher` (sin broker real); **save-before-publish** en todos los handlers mutadores.
- **Pureza/clock:** Domain puro (`now` como parámetro `DateTime`); handlers obtienen `now` vía `TimeProvider.GetUtcNow().UtcDateTime`. Sin `DateTime.Now`/`Math.random` inline.
- **No-leak:** los DTOs participantes (`EtapaActualDto`) nunca exponen `CodigoQREsperado`.
- **Migración aditiva:** solo `CreateTable` + índices; cero ALTER destructivo sobre tablas existentes.
- **Higiene git:** los implementers solo hacen `git add <rutas específicas listadas en el Step>`; nunca `git add -A`/`.`/`-u`; jamás `git checkout/restore/clean/reset` amplios.
- **No tocar la lógica Trivia de 3c:** todo lo de BDT es aditivo. Los miembros existentes de `JuegoResumen`/`SesionPartida`/`PreguntaSnapshot` se conservan byte-idénticos.
- **Modalidad:** solo Individual. Equipo BDT, pistas, geolocalización, SignalR, barrido automático de timeout, scoring real → diferidos (SP-3f / slice-E / SP-4).

## File Structure

**Domain** (`services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/`)
- `Enums/EstadoEtapa.cs`, `Enums/ResultadoValidacionQR.cs`, `Enums/MotivoCierreEtapa.cs` (nuevos)
- `Abstractions/IQrDecoder.cs` (nuevo)
- `Entities/TesoroQR.cs`, `Entities/EtapaSnapshot.cs` (nuevos)
- `Entities/JuegoResumen.cs`, `Entities/SesionPartida.cs` (modificar — aditivo)
- `Results/ResultadoRegistroTesoro.cs`, `Results/ResultadoAvanceEtapa.cs` (nuevos)
- `Exceptions/JuegoActivoNoEsBDTException.cs`, `Exceptions/NoHayEtapaActivaException.cs`, `Exceptions/JuegoConEtapasPendientesException.cs` (nuevos)

**Application** (`.../Umbral.OperacionesSesion.Application/`)
- `Interfaces/BdtRuntimeEvents.cs` (nuevo: 4 records), `Interfaces/ISesionEventsPublisher.cs` (modificar)
- `DTOs/ConfiguracionPartidaDto.cs` (modificar: rama BDT), `DTOs/BdtRuntimeDtos.cs` (nuevo: responses + EtapaActualDto + request)
- `Commands/ValidarTesoroCommand.cs`, `Commands/AvanzarEtapaCommand.cs`, `Queries/ObtenerEtapaActualQuery.cs` (nuevos)
- `Validators/ValidarTesoroCommandValidator.cs`, `Validators/AvanzarEtapaCommandValidator.cs` (nuevos)
- `Handlers/Commands/ValidarTesoroCommandHandler.cs`, `Handlers/Commands/AvanzarEtapaCommandHandler.cs`, `Handlers/Queries/ObtenerEtapaActualQueryHandler.cs` (nuevos)
- `Handlers/Commands/PublicarPartidaCommandHandler.cs`, `Handlers/Commands/IniciarPartidaCommandHandler.cs` (modificar)

**Infrastructure** (`.../Umbral.OperacionesSesion.Infrastructure/`)
- `Services/ZXingQrDecoder.cs` (nuevo), `Services/NoOpSesionEventsPublisher.cs` (modificar)
- `Services/PartidasConfigHttpClient.cs` (modificar: deserializar bdt), `DependencyInjection.cs` (modificar: registrar IQrDecoder)
- `Persistence/OperacionesSesionDbContext.cs` (modificar), `Persistence/SesionPartidaRepository.cs` (modificar), `Persistence/Migrations/*` (nuevo)

**Api** (`.../Umbral.OperacionesSesion.Api/`)
- `Controllers/SesionesController.cs` (modificar: +3 endpoints), `Middleware/ExceptionHandlingMiddleware.cs` (modificar: +3 arms 409)

**Tests** (`services/operaciones-sesion/tests/`)
- `UnitTests/Domain/*`, `UnitTests/Application/*` (nuevos), `UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs` (modificar)
- `IntegrationTests/BdtSnapshotPersistenceTests.cs` (nuevo)
- `ContractTests/BdtRuntimeEndpointsTests.cs` (nuevo), `ContractTests/OperacionesSesionWebFactory.cs` (modificar: stub IQrDecoder)

---

### Task 1: Enums BDT + puerto IQrDecoder + TesoroQR + Results

**Files:**
- Create: `.../Domain/Enums/EstadoEtapa.cs`, `.../Domain/Enums/ResultadoValidacionQR.cs`, `.../Domain/Enums/MotivoCierreEtapa.cs`
- Create: `.../Domain/Abstractions/IQrDecoder.cs`
- Create: `.../Domain/Entities/TesoroQR.cs`
- Create: `.../Domain/Results/ResultadoRegistroTesoro.cs`, `.../Domain/Results/ResultadoAvanceEtapa.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Domain/BdtLeafTypesTests.cs`

**Interfaces:**
- Produces: `EstadoEtapa { Pendiente, Activa, Ganada, CerradaPorTiempo, Cerrada }`; `ResultadoValidacionQR { Valido, Invalido, NoLegible, NoCorrespondeEtapaActiva }`; `MotivoCierreEtapa { Ganador, Tiempo, AvanceOperador }`; `IQrDecoder.Decodificar(byte[]) → string?`; `TesoroQR(Guid participanteId, string? qrDecodificado, ResultadoValidacionQR resultado, DateTime fechaEnvio)` (self-gen `Id`); `ResultadoRegistroTesoro` y `ResultadoAvanceEtapa` (firmas abajo).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.UnitTests/Domain/BdtLeafTypesTests.cs
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class BdtLeafTypesTests
{
    [Fact]
    public void TesoroQR_self_generates_id_and_holds_fields()
    {
        var participante = Guid.NewGuid();
        var t = new TesoroQR(participante, "QR-1", ResultadoValidacionQR.Valido, new DateTime(2026, 6, 28));
        Assert.NotEqual(Guid.Empty, t.Id);
        Assert.Equal(participante, t.ParticipanteId);
        Assert.Equal("QR-1", t.QrDecodificado);
        Assert.Equal(ResultadoValidacionQR.Valido, t.Resultado);
    }

    [Fact]
    public void Enums_have_expected_members()
    {
        Assert.Equal(5, Enum.GetValues<EstadoEtapa>().Length);
        Assert.Equal(4, Enum.GetValues<ResultadoValidacionQR>().Length);
        Assert.Equal(3, Enum.GetValues<MotivoCierreEtapa>().Length);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~BdtLeafTypesTests"`
Expected: FAIL (compilación — tipos no existen).

- [ ] **Step 3: Write minimal implementation**

```csharp
// Domain/Enums/EstadoEtapa.cs
namespace Umbral.OperacionesSesion.Domain.Enums;
public enum EstadoEtapa { Pendiente, Activa, Ganada, CerradaPorTiempo, Cerrada }
```
```csharp
// Domain/Enums/ResultadoValidacionQR.cs
namespace Umbral.OperacionesSesion.Domain.Enums;
public enum ResultadoValidacionQR { Valido, Invalido, NoLegible, NoCorrespondeEtapaActiva }
```
```csharp
// Domain/Enums/MotivoCierreEtapa.cs
namespace Umbral.OperacionesSesion.Domain.Enums;
public enum MotivoCierreEtapa { Ganador, Tiempo, AvanceOperador }
```
```csharp
// Domain/Abstractions/IQrDecoder.cs
namespace Umbral.OperacionesSesion.Domain.Abstractions;

/// <summary>Decodifica el contenido textual de un QR contenido en una imagen. Devuelve null si no es legible.</summary>
public interface IQrDecoder
{
    string? Decodificar(byte[] imagen);
}
```
```csharp
// Domain/Entities/TesoroQR.cs
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class TesoroQR
{
    public Guid Id { get; private set; }
    public Guid ParticipanteId { get; private set; }
    public string? QrDecodificado { get; private set; }
    public ResultadoValidacionQR Resultado { get; private set; }
    public DateTime FechaEnvio { get; private set; }

    private TesoroQR() { } // EF

    public TesoroQR(Guid participanteId, string? qrDecodificado, ResultadoValidacionQR resultado, DateTime fechaEnvio)
    {
        Id = Guid.NewGuid();
        ParticipanteId = participanteId;
        QrDecodificado = qrDecodificado;
        Resultado = resultado;
        FechaEnvio = fechaEnvio;
    }
}
```
```csharp
// Domain/Results/ResultadoRegistroTesoro.cs
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Results;

public sealed record ResultadoRegistroTesoro(
    ResultadoValidacionQR Resultado,
    bool CerroEtapa,
    bool Gano,
    int? Puntaje,
    Guid JuegoId,
    Guid EtapaId,
    Guid ParticipanteId,
    Guid? GanadorParticipanteId,
    long? TiempoResolucionMs,
    string? QrDecodificado,
    DateTime Instante);
```
```csharp
// Domain/Results/ResultadoAvanceEtapa.cs
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Results;

public sealed record ResultadoAvanceEtapa(
    Guid JuegoId,
    Guid EtapaCerradaId,
    int EtapaCerradaOrden,
    MotivoCierreEtapa MotivoCierre,
    Guid? EtapaActivadaId,
    int? EtapaActivadaOrden,
    int? TiempoLimiteActivadaSegundos,
    DateTime? FechaActivacionActivada,
    bool SinMasEtapas);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~BdtLeafTypesTests"`
Expected: PASS (2/2).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/EstadoEtapa.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/ResultadoValidacionQR.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/MotivoCierreEtapa.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/IQrDecoder.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/TesoroQR.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoRegistroTesoro.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoAvanceEtapa.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/BdtLeafTypesTests.cs
git commit -m "SP-3d T1: enums BDT + puerto IQrDecoder + TesoroQR + results"
```

---

### Task 2: EtapaSnapshot (state machine)

**Files:**
- Create: `.../Domain/Entities/EtapaSnapshot.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Domain/EtapaSnapshotTests.cs`

**Interfaces:**
- Consumes: `EstadoEtapa`, `ResultadoValidacionQR`, `MotivoCierreEtapa`, `TesoroQR` (Task 1).
- Produces: `EtapaSnapshot(Guid etapaId, int orden, string codigoQREsperado, int puntaje, int tiempoLimiteSegundos)`; props `EtapaId, Orden, CodigoQREsperado, Puntaje, TiempoLimiteSegundos, Estado, FechaActivacion?, FechaCierre?, MotivoCierre?, GanadorParticipanteId?, TiempoResolucionMs?, Tesoros`; mutadores internos `Activar(now)`, `RegistrarTesoro(participanteId, qrDecodificado, resultado, now) → (bool CerroEtapa, bool Gano, int? Puntaje, long? TiempoResolucionMs)` vía tupla, `CerrarPorTiempo(now)`, `CerrarPorOperador(now)`.

> Nota: `EtapaSnapshot` usa mutadores `internal`; el proyecto de tests ya tiene `InternalsVisibleTo` hacia `Umbral.OperacionesSesion.UnitTests` (añadido en SP-3c). No volver a añadirlo.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Domain/EtapaSnapshotTests.cs
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class EtapaSnapshotTests
{
    private static EtapaSnapshot NuevaEtapa(string qr = "QR-1", int limite = 60)
        => new(Guid.NewGuid(), 1, qr, puntaje: 50, tiempoLimiteSegundos: limite);

    [Fact]
    public void Activar_sets_estado_activa_and_fecha()
    {
        var e = NuevaEtapa();
        var now = new DateTime(2026, 6, 28, 10, 0, 0);
        e.Activar(now);
        Assert.Equal(EstadoEtapa.Activa, e.Estado);
        Assert.Equal(now, e.FechaActivacion);
    }

    [Fact]
    public void RegistrarTesoro_valido_dentro_de_ventana_gana_y_cierra()
    {
        var e = NuevaEtapa();
        var t0 = new DateTime(2026, 6, 28, 10, 0, 0);
        e.Activar(t0);
        var participante = Guid.NewGuid();
        var r = e.RegistrarTesoro(participante, "QR-1", ResultadoValidacionQR.Valido, t0.AddSeconds(5));
        Assert.True(r.CerroEtapa);
        Assert.True(r.Gano);
        Assert.Equal(50, r.Puntaje);
        Assert.Equal(5000, r.TiempoResolucionMs);
        Assert.Equal(EstadoEtapa.Ganada, e.Estado);
        Assert.Equal(participante, e.GanadorParticipanteId);
        Assert.Single(e.Tesoros);
    }

    [Fact]
    public void RegistrarTesoro_invalido_registra_pero_no_cierra()
    {
        var e = NuevaEtapa();
        var t0 = new DateTime(2026, 6, 28, 10, 0, 0);
        e.Activar(t0);
        var r = e.RegistrarTesoro(Guid.NewGuid(), null, ResultadoValidacionQR.NoLegible, t0.AddSeconds(5));
        Assert.False(r.CerroEtapa);
        Assert.False(r.Gano);
        Assert.Equal(EstadoEtapa.Activa, e.Estado);
        Assert.Single(e.Tesoros);
    }

    [Fact]
    public void RegistrarTesoro_multiples_intentos_se_acumulan()
    {
        var e = NuevaEtapa();
        var t0 = new DateTime(2026, 6, 28, 10, 0, 0);
        e.Activar(t0);
        e.RegistrarTesoro(Guid.NewGuid(), null, ResultadoValidacionQR.Invalido, t0.AddSeconds(1));
        e.RegistrarTesoro(Guid.NewGuid(), null, ResultadoValidacionQR.Invalido, t0.AddSeconds(2));
        Assert.Equal(2, e.Tesoros.Count);
    }

    [Fact]
    public void RegistrarTesoro_valido_fuera_de_ventana_registra_pero_no_gana()
    {
        var e = NuevaEtapa(limite: 10);
        var t0 = new DateTime(2026, 6, 28, 10, 0, 0);
        e.Activar(t0);
        var r = e.RegistrarTesoro(Guid.NewGuid(), "QR-1", ResultadoValidacionQR.Valido, t0.AddSeconds(20));
        Assert.False(r.Gano);
        Assert.Equal(EstadoEtapa.Activa, e.Estado); // el cierre por tiempo lo decide el agregado
        Assert.Single(e.Tesoros);
    }

    [Fact]
    public void CerrarPorTiempo_y_CerrarPorOperador_set_estados_distintos()
    {
        var t0 = new DateTime(2026, 6, 28, 10, 0, 0);
        var a = NuevaEtapa(); a.Activar(t0); a.CerrarPorTiempo(t0.AddSeconds(99));
        Assert.Equal(EstadoEtapa.CerradaPorTiempo, a.Estado);
        Assert.Equal(MotivoCierreEtapa.Tiempo, a.MotivoCierre);
        var b = NuevaEtapa(); b.Activar(t0); b.CerrarPorOperador(t0.AddSeconds(3));
        Assert.Equal(EstadoEtapa.Cerrada, b.Estado);
        Assert.Equal(MotivoCierreEtapa.AvanceOperador, b.MotivoCierre);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~EtapaSnapshotTests"`
Expected: FAIL (compilación — `EtapaSnapshot` no existe).

- [ ] **Step 3: Write minimal implementation**

```csharp
// Domain/Entities/EtapaSnapshot.cs
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class EtapaSnapshot
{
    private readonly List<TesoroQR> _tesoros = new();

    public Guid EtapaId { get; private set; }
    public int Orden { get; private set; }
    public string CodigoQREsperado { get; private set; } = null!;
    public int Puntaje { get; private set; }
    public int TiempoLimiteSegundos { get; private set; }
    public EstadoEtapa Estado { get; private set; } = EstadoEtapa.Pendiente;
    public DateTime? FechaActivacion { get; private set; }
    public DateTime? FechaCierre { get; private set; }
    public MotivoCierreEtapa? MotivoCierre { get; private set; }
    public Guid? GanadorParticipanteId { get; private set; }
    public long? TiempoResolucionMs { get; private set; }

    public IReadOnlyList<TesoroQR> Tesoros => _tesoros;

    private EtapaSnapshot() { } // EF

    public EtapaSnapshot(Guid etapaId, int orden, string codigoQREsperado, int puntaje, int tiempoLimiteSegundos)
    {
        EtapaId = etapaId;
        Orden = orden;
        CodigoQREsperado = codigoQREsperado;
        Puntaje = puntaje;
        TiempoLimiteSegundos = tiempoLimiteSegundos;
    }

    internal void Activar(DateTime now)
    {
        if (Estado != EstadoEtapa.Pendiente)
            throw new InvalidOperationException($"La etapa {EtapaId} no está pendiente.");
        Estado = EstadoEtapa.Activa;
        FechaActivacion = now;
    }

    internal (bool CerroEtapa, bool Gano, int? Puntaje, long? TiempoResolucionMs) RegistrarTesoro(
        Guid participanteId, string? qrDecodificado, ResultadoValidacionQR resultado, DateTime now)
    {
        if (Estado != EstadoEtapa.Activa)
            throw new InvalidOperationException($"La etapa {EtapaId} no está activa.");

        _tesoros.Add(new TesoroQR(participanteId, qrDecodificado, resultado, now));

        var dentroDeVentana = now < FechaActivacion!.Value.AddSeconds(TiempoLimiteSegundos);
        if (resultado == ResultadoValidacionQR.Valido && dentroDeVentana)
        {
            var tiempoMs = (long)(now - FechaActivacion!.Value).TotalMilliseconds;
            Estado = EstadoEtapa.Ganada;
            FechaCierre = now;
            MotivoCierre = MotivoCierreEtapa.Ganador;
            GanadorParticipanteId = participanteId;
            TiempoResolucionMs = tiempoMs;
            return (true, true, Puntaje, tiempoMs);
        }
        return (false, false, null, null);
    }

    internal void CerrarPorTiempo(DateTime now) => Cerrar(EstadoEtapa.CerradaPorTiempo, MotivoCierreEtapa.Tiempo, now);
    internal void CerrarPorOperador(DateTime now) => Cerrar(EstadoEtapa.Cerrada, MotivoCierreEtapa.AvanceOperador, now);

    private void Cerrar(EstadoEtapa estado, MotivoCierreEtapa motivo, DateTime now)
    {
        if (Estado != EstadoEtapa.Activa)
            throw new InvalidOperationException($"La etapa {EtapaId} no está activa.");
        Estado = estado;
        FechaCierre = now;
        MotivoCierre = motivo;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~EtapaSnapshotTests"`
Expected: PASS (6/6).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/EtapaSnapshot.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/EtapaSnapshotTests.cs
git commit -m "SP-3d T2: EtapaSnapshot state machine (activar/registrar/cerrar)"
```

---

### Task 3: JuegoResumen — capacidad BDT (aditivo)

**Files:**
- Modify: `.../Domain/Entities/JuegoResumen.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Domain/JuegoResumenBdtTests.cs`

**Interfaces:**
- Consumes: `EtapaSnapshot` (Task 2), `EstadoEtapa`, `TipoJuego`.
- Produces (nuevos miembros de `JuegoResumen`): ctor `JuegoResumen(Guid, int, TipoJuego, IEnumerable<EtapaSnapshot> etapas)` (sobrecarga BDT); `IReadOnlyList<EtapaSnapshot> Etapas`; `EtapaSnapshot? EtapaActiva`; `bool TieneEtapasAbiertas`; `internal EtapaSnapshot? ActivarSiguienteEtapa(DateTime now)`; `Activar(now)` activa la primera etapa si `TipoJuego==BusquedaDelTesoro`.

> CRÍTICO: la rama Trivia (campos `_preguntas`, `PreguntaActiva`, `TienePreguntasAbiertas`, `ActivarSiguientePregunta`, ctor de 4 args con preguntas) se conserva intacta. Solo se AÑADE la colección/métodos de etapas y una rama en `Activar`. El ctor BDT no puede colisionar con el ctor Trivia de 4 args (`IEnumerable<PreguntaSnapshot>`): la sobrecarga BDT toma `IEnumerable<EtapaSnapshot>`, distinta firma.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Domain/JuegoResumenBdtTests.cs
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class JuegoResumenBdtTests
{
    private static JuegoResumen JuegoBdt(params int[] ordenes)
    {
        var etapas = ordenes.Select(o => new EtapaSnapshot(Guid.NewGuid(), o, $"QR-{o}", 50, 60)).ToList();
        return new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, etapas);
    }

    [Fact]
    public void Activar_bdt_activa_primera_etapa_por_orden()
    {
        var juego = JuegoBdt(2, 1);
        juego.Activar(new DateTime(2026, 6, 28));
        Assert.Equal(EstadoJuego.Activo, juego.Estado);
        Assert.NotNull(juego.EtapaActiva);
        Assert.Equal(1, juego.EtapaActiva!.Orden);
        Assert.True(juego.TieneEtapasAbiertas);
    }

    [Fact]
    public void ActivarSiguienteEtapa_avanza_a_la_proxima_pendiente()
    {
        var juego = JuegoBdt(1, 2);
        var now = new DateTime(2026, 6, 28);
        juego.Activar(now);
        juego.EtapaActiva!.CerrarPorOperador(now); // cierra la 1
        var siguiente = juego.ActivarSiguienteEtapa(now);
        Assert.NotNull(siguiente);
        Assert.Equal(2, siguiente!.Orden);
        Assert.Equal(2, juego.EtapaActiva!.Orden);
    }

    [Fact]
    public void Bdt_sin_etapas_no_tiene_etapa_activa()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, Array.Empty<EtapaSnapshot>());
        juego.Activar(new DateTime(2026, 6, 28));
        Assert.Null(juego.EtapaActiva);
        Assert.False(juego.TieneEtapasAbiertas);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~JuegoResumenBdtTests"`
Expected: FAIL (miembros BDT no existen).

- [ ] **Step 3: Write minimal implementation**

Modificar `JuegoResumen.cs` — añadir (sin tocar la rama Trivia). Campo nuevo junto a `_preguntas`:
```csharp
    private readonly List<EtapaSnapshot> _etapas = new();
```
Props nuevas (junto a las de Trivia):
```csharp
    public IReadOnlyList<EtapaSnapshot> Etapas => _etapas;
    public EtapaSnapshot? EtapaActiva => _etapas.FirstOrDefault(e => e.Estado == EstadoEtapa.Activa);
    public bool TieneEtapasAbiertas =>
        _etapas.Any(e => e.Estado is EstadoEtapa.Activa or EstadoEtapa.Pendiente);
```
Sobrecarga de ctor BDT (junto a los ctors existentes):
```csharp
    public JuegoResumen(Guid juegoId, int orden, TipoJuego tipoJuego, IEnumerable<EtapaSnapshot> etapas)
    {
        JuegoId = juegoId;
        Orden = orden;
        TipoJuego = tipoJuego;
        _etapas.AddRange(etapas);
    }
```
En `Activar(now)`, añadir la rama BDT (después de la rama Trivia existente):
```csharp
    internal void Activar(DateTime now)
    {
        if (Estado != EstadoJuego.Pendiente)
            throw new InvalidOperationException($"El juego {JuegoId} no está pendiente.");
        Estado = EstadoJuego.Activo;
        if (TipoJuego == TipoJuego.Trivia)
            ActivarSiguientePregunta(now);
        else if (TipoJuego == TipoJuego.BusquedaDelTesoro)
            ActivarSiguienteEtapa(now);
    }
```
Método nuevo (junto a `ActivarSiguientePregunta`):
```csharp
    internal EtapaSnapshot? ActivarSiguienteEtapa(DateTime now)
    {
        var siguiente = _etapas
            .Where(e => e.Estado == EstadoEtapa.Pendiente)
            .OrderBy(e => e.Orden)
            .FirstOrDefault();
        siguiente?.Activar(now);
        return siguiente;
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~JuegoResumenBdtTests"`
Expected: PASS (3/3). Luego corre TODO el proyecto UnitTests para confirmar que la rama Trivia (3c) sigue verde: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests"` → todo verde.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/JuegoResumen.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/JuegoResumenBdtTests.cs
git commit -m "SP-3d T3: JuegoResumen gana capacidad de etapas BDT (aditivo)"
```

---

### Task 4: SesionPartida — ValidarTesoro / AvanzarEtapa / guard finalización

**Files:**
- Modify: `.../Domain/Entities/SesionPartida.cs`
- Create: `.../Domain/Exceptions/JuegoActivoNoEsBDTException.cs`, `.../Domain/Exceptions/NoHayEtapaActivaException.cs`, `.../Domain/Exceptions/JuegoConEtapasPendientesException.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaBdtTests.cs`

**Interfaces:**
- Consumes: `JuegoResumen` BDT (Task 3), `EtapaSnapshot` (Task 2), `IQrDecoder` (Task 1), `ResultadoRegistroTesoro`/`ResultadoAvanceEtapa` (Task 1).
- Produces: `SesionPartida.ValidarTesoro(Guid participanteId, byte[] imagen, DateTime now, IQrDecoder decoder) → ResultadoRegistroTesoro`; `SesionPartida.AvanzarEtapa(DateTime now) → ResultadoAvanceEtapa`; el guard BDT dentro de `FinalizarJuegoActual`.

> Las exceptions siguen el patrón de las de Trivia (`NoHayPreguntaActivaException`, etc.): heredan de la base de dominio existente y reciben `Guid partidaId`/`participanteId`. Revisar `JuegoActivoNoEsTriviaException.cs` como plantilla exacta (constructor + mensaje + clase base).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Domain/SesionPartidaBdtTests.cs
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class SesionPartidaBdtTests
{
    // Decoder fake: interpreta los bytes como el texto UTF-8 del QR.
    private sealed class TextoQrDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) =>
            imagen.Length == 0 ? null : System.Text.Encoding.UTF8.GetString(imagen);
    }

    private static byte[] Img(string texto) => System.Text.Encoding.UTF8.GetBytes(texto);

    // Helper: publica + inscribe + inicia una sesión BDT Individual con las etapas dadas (qr, limite).
    // Reutiliza el ConfiguracionSnapshot del dominio igual que en SesionPartidaTriviaTests (3c).
    private static SesionPartida SesionBdtIniciada(Guid participante, params (string Qr, int Limite)[] etapas)
    {
        var juegoId = Guid.NewGuid();
        var etapasSnap = etapas.Select((e, i) => new EtapaSnapshot(Guid.NewGuid(), i + 1, e.Qr, 50, e.Limite)).ToList();
        var juego = new JuegoResumen(juegoId, 1, TipoJuego.BusquedaDelTesoro, etapasSnap);
        var snapshot = new ConfiguracionSnapshot(
            "Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snapshot);
        var now = new DateTime(2026, 6, 28, 10, 0, 0);
        sesion.Inscribir(participante, false, 0, now);
        sesion.Iniciar(now);
        return sesion;
    }

    [Fact]
    public void ValidarTesoro_correcto_gana_y_auto_avanza()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60), ("QR-2", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        var r = sesion.ValidarTesoro(jugador, Img("QR-1"), now, new TextoQrDecoder());
        Assert.Equal(ResultadoValidacionQR.Valido, r.Resultado);
        Assert.True(r.Gano);
        Assert.Equal(50, r.Puntaje);
        // auto-avance: la etapa activa ahora es la 2
        var juego = sesion.Juegos.Single();
        Assert.Equal(2, juego.EtapaActiva!.Orden);
    }

    [Fact]
    public void ValidarTesoro_incorrecto_registra_sin_ganar()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        var r = sesion.ValidarTesoro(jugador, Img("QR-OTRO"), now, new TextoQrDecoder());
        Assert.Equal(ResultadoValidacionQR.Invalido, r.Resultado);
        Assert.False(r.Gano);
        Assert.Equal(EstadoEtapa.Activa, sesion.Juegos.Single().EtapaActiva!.Estado);
    }

    [Fact]
    public void ValidarTesoro_qr_de_otra_etapa_es_NoCorrespondeEtapaActiva()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60), ("QR-2", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        var r = sesion.ValidarTesoro(jugador, Img("QR-2"), now, new TextoQrDecoder()); // QR de la etapa 2 mientras la activa es la 1
        Assert.Equal(ResultadoValidacionQR.NoCorrespondeEtapaActiva, r.Resultado);
        Assert.False(r.Gano);
    }

    [Fact]
    public void ValidarTesoro_imagen_ilegible_es_NoLegible()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        var r = sesion.ValidarTesoro(jugador, Array.Empty<byte>(), now, new TextoQrDecoder());
        Assert.Equal(ResultadoValidacionQR.NoLegible, r.Resultado);
    }

    [Fact]
    public void ValidarTesoro_sin_inscripcion_lanza_403_antes_que_409()
    {
        var jugador = Guid.NewGuid();
        var intruso = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        Assert.Throws<ParticipanteNoInscritoException>(() =>
            sesion.ValidarTesoro(intruso, Img("QR-1"), now, new TextoQrDecoder()));
    }

    [Fact]
    public void AvanzarEtapa_operador_cierra_sin_ganador_y_activa_siguiente()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60), ("QR-2", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        var r = sesion.AvanzarEtapa(now);
        Assert.Equal(MotivoCierreEtapa.AvanceOperador, r.MotivoCierre);
        Assert.False(r.SinMasEtapas);
        Assert.Equal(2, r.EtapaActivadaOrden);
    }

    [Fact]
    public void FinalizarJuegoActual_con_etapa_abierta_lanza_JuegoConEtapasPendientes()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var now = new DateTime(2026, 6, 28, 10, 0, 5);
        Assert.Throws<JuegoConEtapasPendientesException>(() => sesion.FinalizarJuegoActual(now));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~SesionPartidaBdtTests"`
Expected: FAIL (métodos/excepciones no existen).

- [ ] **Step 3: Write minimal implementation**

Crear las 3 exceptions (copiar el patrón exacto de `JuegoActivoNoEsTriviaException.cs` / `NoHayPreguntaActivaException.cs` / `JuegoConPreguntasPendientesException.cs`):
```csharp
// Domain/Exceptions/JuegoActivoNoEsBDTException.cs
namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class JuegoActivoNoEsBDTException : Exception
{
    public JuegoActivoNoEsBDTException(Guid partidaId)
        : base($"El juego activo de la partida {partidaId} no es de Búsqueda del Tesoro.") { }
}
```
```csharp
// Domain/Exceptions/NoHayEtapaActivaException.cs
namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class NoHayEtapaActivaException : Exception
{
    public NoHayEtapaActivaException(Guid partidaId)
        : base($"No hay una etapa activa en la partida {partidaId}.") { }
}
```
```csharp
// Domain/Exceptions/JuegoConEtapasPendientesException.cs
namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class JuegoConEtapasPendientesException : Exception
{
    public JuegoConEtapasPendientesException(Guid partidaId)
        : base($"El juego BDT de la partida {partidaId} tiene etapas pendientes.") { }
}
```
> Verificar que la base (`Exception` vs una base de dominio común) coincide con la de las exceptions Trivia; si Trivia hereda de una base propia, heredar igual.

En `SesionPartida.cs` añadir (sin tocar lo de Trivia). Añadir el guard BDT en `FinalizarJuegoActual`, junto al de Trivia (líneas 105-106):
```csharp
        var actual = _juegos.Single(j => j.Estado == EstadoJuego.Activo);
        if (actual.TipoJuego == TipoJuego.Trivia && actual.TienePreguntasAbiertas)
            throw new JuegoConPreguntasPendientesException(PartidaId);
        if (actual.TipoJuego == TipoJuego.BusquedaDelTesoro && actual.TieneEtapasAbiertas)
            throw new JuegoConEtapasPendientesException(PartidaId);
        actual.Finalizar();
```
Métodos nuevos (junto a `ResponderPregunta`/`AvanzarPregunta`):
```csharp
    public ResultadoRegistroTesoro ValidarTesoro(Guid participanteId, byte[] imagen, DateTime now, Umbral.OperacionesSesion.Domain.Abstractions.IQrDecoder decoder)
    {
        var juego = JuegoBDTActivo();
        var activa = juego.EtapaActiva ?? throw new NoHayEtapaActivaException(PartidaId);
        if (!_inscripciones.Any(i => i.ParticipanteId == participanteId && i.EsActiva))
            throw new ParticipanteNoInscritoException(participanteId);

        var texto = decoder.Decodificar(imagen);
        var resultado = ClasificarQr(texto, activa, juego);

        var reg = activa.RegistrarTesoro(participanteId, texto, resultado, now);

        if (reg.Gano)
        {
            juego.ActivarSiguienteEtapa(now);
        }
        else if (now >= activa.FechaActivacion!.Value.AddSeconds(activa.TiempoLimiteSegundos))
        {
            activa.CerrarPorTiempo(now);
            juego.ActivarSiguienteEtapa(now);
        }

        return new ResultadoRegistroTesoro(
            resultado, reg.CerroEtapa || (!reg.Gano && activa.Estado == EstadoEtapa.CerradaPorTiempo),
            reg.Gano, reg.Puntaje, juego.JuegoId, activa.EtapaId, participanteId,
            reg.Gano ? participanteId : null, reg.TiempoResolucionMs, texto, now);
    }

    public ResultadoAvanceEtapa AvanzarEtapa(DateTime now)
    {
        var juego = JuegoBDTActivo();
        var activa = juego.EtapaActiva ?? throw new NoHayEtapaActivaException(PartidaId);

        var vencida = now >= activa.FechaActivacion!.Value.AddSeconds(activa.TiempoLimiteSegundos);
        if (vencida) activa.CerrarPorTiempo(now); else activa.CerrarPorOperador(now);
        var motivo = vencida ? MotivoCierreEtapa.Tiempo : MotivoCierreEtapa.AvanceOperador;

        var siguiente = juego.ActivarSiguienteEtapa(now);
        return new ResultadoAvanceEtapa(
            juego.JuegoId, activa.EtapaId, activa.Orden, motivo,
            siguiente?.EtapaId, siguiente?.Orden, siguiente?.TiempoLimiteSegundos, siguiente?.FechaActivacion,
            siguiente is null);
    }

    private JuegoResumen JuegoBDTActivo()
    {
        if (Estado != EstadoSesion.Iniciada)
            throw new SesionNoIniciadaException(PartidaId);
        var juego = _juegos.Single(j => j.Estado == EstadoJuego.Activo);
        if (juego.TipoJuego != TipoJuego.BusquedaDelTesoro)
            throw new JuegoActivoNoEsBDTException(PartidaId);
        return juego;
    }

    private static ResultadoValidacionQR ClasificarQr(string? texto, EtapaSnapshot activa, JuegoResumen juego)
    {
        if (texto is null) return ResultadoValidacionQR.NoLegible;
        if (texto == activa.CodigoQREsperado) return ResultadoValidacionQR.Valido;
        if (juego.Etapas.Any(e => e.CodigoQREsperado == texto)) return ResultadoValidacionQR.NoCorrespondeEtapaActiva;
        return ResultadoValidacionQR.Invalido;
    }
```
> Añadir `using Umbral.OperacionesSesion.Domain.Results;` si no estuviera ya (SesionPartida ya lo usa para `ResultadoRespuesta`).

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~SesionPartidaBdtTests"`
Expected: PASS (7/7). Luego TODO el proyecto UnitTests verde (Trivia intacto).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/JuegoActivoNoEsBDTException.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/NoHayEtapaActivaException.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/JuegoConEtapasPendientesException.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaBdtTests.cs
git commit -m "SP-3d T4: SesionPartida ValidarTesoro/AvanzarEtapa + guard finalización BDT"
```

> === Domain layer (T1–T4) COMPLETO ===

---

### Task 5: Eventos BDT + publisher port

**Files:**
- Create: `.../Application/Interfaces/BdtRuntimeEvents.cs`
- Modify: `.../Application/Interfaces/ISesionEventsPublisher.cs`
- Modify: `.../Infrastructure/Services/NoOpSesionEventsPublisher.cs`
- Modify: `.../tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakePublisherBdtTests.cs`

**Interfaces:**
- Produces: 4 records de evento + 4 métodos en `ISesionEventsPublisher` (`PublicarTesoroQRValidadoAsync`, `PublicarEtapaBDTGanadaAsync`, `PublicarEtapaBDTCerradaAsync`, `PublicarEtapaBDTActivadaAsync`). El `FakeSesionEventsPublisher` gana 4 listas públicas.

> Patrón espejo de `TriviaRuntimeEvents.cs` (4 records) + las 4 líneas Trivia en `ISesionEventsPublisher`/`NoOp`/`Fake`. Todos los miembros existentes se conservan.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Application/Fakes/FakePublisherBdtTests.cs
using Umbral.OperacionesSesion.Application.Interfaces;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public class FakePublisherBdtTests
{
    [Fact]
    public async Task Fake_records_bdt_events()
    {
        var fake = new FakeSesionEventsPublisher();
        var pid = Guid.NewGuid(); var sid = Guid.NewGuid(); var jid = Guid.NewGuid(); var eid = Guid.NewGuid();
        await fake.PublicarTesoroQRValidadoAsync(
            new TesoroQRValidadoEvent(pid, sid, jid, eid, Guid.NewGuid(), "Valido", DateTime.UtcNow), default);
        await fake.PublicarEtapaBDTGanadaAsync(
            new EtapaBDTGanadaEvent(pid, sid, jid, eid, Guid.NewGuid(), 50, 1234), default);
        await fake.PublicarEtapaBDTCerradaAsync(
            new EtapaBDTCerradaEvent(pid, sid, jid, eid, "Ganador", DateTime.UtcNow, Guid.NewGuid()), default);
        await fake.PublicarEtapaBDTActivadaAsync(
            new EtapaBDTActivadaEvent(pid, sid, jid, eid, 1, 60, DateTime.UtcNow), default);
        Assert.Single(fake.TesorosValidados);
        Assert.Single(fake.EtapasGanadas);
        Assert.Single(fake.EtapasCerradas);
        Assert.Single(fake.EtapasActivadas);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~FakePublisherBdtTests"`
Expected: FAIL (tipos/métodos no existen).

- [ ] **Step 3: Write minimal implementation**

```csharp
// Application/Interfaces/BdtRuntimeEvents.cs
namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record TesoroQRValidadoEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
    Guid ParticipanteId, string Resultado, DateTime Instante);

public sealed record EtapaBDTGanadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
    Guid ParticipanteId, int Puntaje, long TiempoResolucionMs);

public sealed record EtapaBDTCerradaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
    string Motivo, DateTime FechaCierre, Guid? GanadorParticipanteId);

public sealed record EtapaBDTActivadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
    int Orden, int TiempoLimiteSegundos, DateTime FechaActivacion);
```
Añadir a `ISesionEventsPublisher` (junto a los métodos Trivia):
```csharp
    Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent evento, CancellationToken cancellationToken);
    Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent evento, CancellationToken cancellationToken);
    Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent evento, CancellationToken cancellationToken);
    Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent evento, CancellationToken cancellationToken);
```
Añadir a `NoOpSesionEventsPublisher` (4 métodos, patrón existente):
```csharp
    public Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
```
Añadir a `FakeSesionEventsPublisher` (4 listas + 4 métodos, patrón existente):
```csharp
    public List<TesoroQRValidadoEvent> TesorosValidados { get; } = new();
    public List<EtapaBDTGanadaEvent> EtapasGanadas { get; } = new();
    public List<EtapaBDTCerradaEvent> EtapasCerradas { get; } = new();
    public List<EtapaBDTActivadaEvent> EtapasActivadas { get; } = new();

    public Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent evento, CancellationToken cancellationToken)
    { TesorosValidados.Add(evento); return Task.CompletedTask; }
    public Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent evento, CancellationToken cancellationToken)
    { EtapasGanadas.Add(evento); return Task.CompletedTask; }
    public Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent evento, CancellationToken cancellationToken)
    { EtapasCerradas.Add(evento); return Task.CompletedTask; }
    public Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent evento, CancellationToken cancellationToken)
    { EtapasActivadas.Add(evento); return Task.CompletedTask; }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~FakePublisherBdtTests"`
Expected: PASS (1/1). Proyecto UnitTests completo verde.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/BdtRuntimeEvents.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakePublisherBdtTests.cs
git commit -m "SP-3d T5: eventos BDT (4 records) + publisher port No-Op/Fake"
```

---

### Task 6: Config snapshot — rama BDT (DTO + http client + MapearJuego)

**Files:**
- Modify: `.../Application/DTOs/ConfiguracionPartidaDto.cs`
- Modify: `.../Application/Handlers/Commands/PublicarPartidaCommandHandler.cs`
- Modify: `.../Infrastructure/Services/PartidasConfigHttpClient.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Application/PublicarPartidaBdtSnapshotTests.cs`

**Interfaces:**
- Consumes: `EtapaSnapshot` (Task 2), `JuegoResumen` BDT ctor (Task 3).
- Produces: `JuegoResumenDto` gana `Bdt` opcional (`BdtConfigDto? Bdt = null`); `BdtConfigDto(string AreaBusqueda, IReadOnlyList<EtapaConfigDto> Etapas)`; `EtapaConfigDto(Guid EtapaBDTId, int Orden, string CodigoQREsperado, int PuntajeAsignado, int TiempoLimiteSegundos)`. `MapearJuego` construye `JuegoResumen` BDT desde `j.Bdt.Etapas`.

> Contrato de config (de SP-2, `contracts/http/partidas-config.md`): `bdt: { areaBusqueda, etapas: [ { etapaBDTId, orden, codigoQREsperado, puntajeAsignado, tiempoLimiteSegundos } ] }`. El http client deserializa esa forma anidada.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Application/PublicarPartidaBdtSnapshotTests.cs
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class PublicarPartidaBdtSnapshotTests
{
    [Fact]
    public void MapearJuego_bdt_construye_etapas_snapshot()
    {
        var juegoId = Guid.NewGuid();
        var e1 = Guid.NewGuid(); var e2 = Guid.NewGuid();
        var dto = new JuegoResumenDto(juegoId, 1, "BusquedaDelTesoro", Trivia: null,
            Bdt: new BdtConfigDto("Plaza central", new List<EtapaConfigDto>
            {
                new(e1, 1, "QR-A", 50, 120),
                new(e2, 2, "QR-B", 70, 90),
            }));

        // MapearJuego es private static; se ejercita vía el snapshot completo en el handler test (abajo),
        // pero aquí validamos la forma del DTO de config para fijar el contrato.
        Assert.Equal("BusquedaDelTesoro", dto.TipoJuego);
        Assert.NotNull(dto.Bdt);
        Assert.Equal(2, dto.Bdt!.Etapas.Count);
        Assert.Equal("QR-A", dto.Bdt.Etapas[0].CodigoQREsperado);
    }
}
```

> Si `MapearJuego` se quiere ejercitar directamente, cambiarlo a `internal static` y verificar `InternalsVisibleTo` (ya presente). En ese caso el test construye un `JuegoResumenDto` BDT y asserta `juego.Etapas.Count==2`, `juego.TipoJuego==BusquedaDelTesoro`. Preferir esto último si el reviewer lo pide; el handler test de la suite contract (T16) ya cubre el camino end-to-end.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~PublicarPartidaBdtSnapshotTests"`
Expected: FAIL (DTO `Bdt`/`BdtConfigDto`/`EtapaConfigDto` no existen).

- [ ] **Step 3: Write minimal implementation**

Modificar `ConfiguracionPartidaDto.cs` — `JuegoResumenDto` gana `Bdt` opcional + 2 records nuevos:
```csharp
public sealed record JuegoResumenDto(Guid JuegoId, int Orden, string TipoJuego, TriviaConfigDto? Trivia = null, BdtConfigDto? Bdt = null);

public sealed record BdtConfigDto(string AreaBusqueda, IReadOnlyList<EtapaConfigDto> Etapas);

public sealed record EtapaConfigDto(
    Guid EtapaBDTId, int Orden, string CodigoQREsperado, int PuntajeAsignado, int TiempoLimiteSegundos);
```
> Los call-sites existentes de `JuegoResumenDto` (3 args, o 4 con Trivia) siguen compilando porque `Bdt` tiene default `null`.

En `PublicarPartidaCommandHandler.MapearJuego`, añadir la rama BDT (el guard Trivia existente ya devuelve vacío para no-Trivia; reemplazarlo por un dispatch por tipo):
```csharp
    private static JuegoResumen MapearJuego(JuegoResumenDto j)
    {
        var tipo = Enum.Parse<TipoJuego>(j.TipoJuego, ignoreCase: true);

        if (tipo == TipoJuego.Trivia && j.Trivia is not null)
        {
            var preguntas = j.Trivia.Preguntas
                .Select((p, idx) => new PreguntaSnapshot(
                    p.PreguntaId, idx + 1, p.Texto, p.PuntajeAsignado, p.TiempoLimiteSegundos,
                    p.Opciones.Select(o => new OpcionSnapshot(o.OpcionId, o.Texto, o.EsCorrecta)).ToList()))
                .ToList();
            return new JuegoResumen(j.JuegoId, j.Orden, tipo, preguntas);
        }

        if (tipo == TipoJuego.BusquedaDelTesoro && j.Bdt is not null)
        {
            var etapas = j.Bdt.Etapas
                .Select((e, idx) => new EtapaSnapshot(
                    e.EtapaBDTId, idx + 1, e.CodigoQREsperado, e.PuntajeAsignado, e.TiempoLimiteSegundos))
                .ToList();
            return new JuegoResumen(j.JuegoId, j.Orden, tipo, etapas);
        }

        return new JuegoResumen(j.JuegoId, j.Orden, tipo); // sin contenido
    }
```
> Verificar imports: `PublicarPartidaCommandHandler` ya usa `Umbral.OperacionesSesion.Domain.Entities` (PreguntaSnapshot/OpcionSnapshot). `EtapaSnapshot` está en el mismo namespace.

En `PartidasConfigHttpClient.cs` — extender la deserialización para incluir `bdt.etapas[]`. Revisar cómo deserializa hoy la rama `trivia` (records privados o System.Text.Json sobre `JuegoResumenDto`). Replicar para `bdt`: añadir los campos/records privados que mapean `areaBusqueda` + `etapas[]{ etapaBDTId, orden, codigoQREsperado, puntajeAsignado, tiempoLimiteSegundos }` y construir el `BdtConfigDto`. Mantener intactos GET/404→null/502.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~PublicarPartidaBdtSnapshotTests"`
Expected: PASS. Proyecto UnitTests completo verde (los publish tests Trivia/sin-contenido de 3a/3c siguen verdes vía las ramas guard).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/ConfiguracionPartidaDto.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PublicarPartidaCommandHandler.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/PartidasConfigHttpClient.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/PublicarPartidaBdtSnapshotTests.cs
git commit -m "SP-3d T6: snapshot de etapas BDT al publicar (DTO + http client + MapearJuego)"
```

---

### Task 7: Commands / Queries / DTOs / Validators BDT

**Files:**
- Create: `.../Application/Commands/ValidarTesoroCommand.cs`, `.../Application/Commands/AvanzarEtapaCommand.cs`, `.../Application/Queries/ObtenerEtapaActualQuery.cs`
- Create: `.../Application/DTOs/BdtRuntimeDtos.cs`
- Create: `.../Application/Validators/ValidarTesoroCommandValidator.cs`, `.../Application/Validators/AvanzarEtapaCommandValidator.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Application/BdtValidatorsTests.cs`

**Interfaces:**
- Produces:
  - `ValidarTesoroCommand(Guid PartidaId, Guid ParticipanteId, string ImagenBase64) : IRequest<ValidacionTesoroResponse>`
  - `AvanzarEtapaCommand(Guid PartidaId) : IRequest<AvanceEtapaResponse>`
  - `ObtenerEtapaActualQuery(Guid PartidaId) : IRequest<EtapaActualDto>`
  - `ValidacionTesoroResponse(Guid PartidaId, Guid EtapaId, string Resultado, bool Gano, bool CerroEtapa, int? Puntaje)`
  - `AvanceEtapaResponse(Guid PartidaId, int EtapaCerradaOrden, int? EtapaActivadaOrden, bool SinMasEtapas)`
  - `EtapaActualDto(Guid PartidaId, Guid JuegoId, Guid EtapaId, int Orden, string AreaBusqueda, int TiempoLimiteSegundos, DateTime FechaActivacion)` — **sin `CodigoQREsperado`**
  - `ValidarTesoroRequest(string ImagenBase64)` (body del endpoint)

> Nota: `AreaBusqueda` no está hoy en `JuegoResumen` (la config la tiene en `JuegoBDT`, pero el snapshot de etapas no la trae). Para SP-3d, `EtapaActualDto.AreaBusqueda` se poblará desde un campo nuevo a nivel de `JuegoResumen`. DECISIÓN: añadir `AreaBusqueda` (string, default "") a `JuegoResumen` en Task 3 NO se hizo — por eso aquí se ajusta: ver Step de implementación. Si el reviewer prefiere no ensanchar `JuegoResumen`, `AreaBusqueda` puede omitirse del DTO; el área textual es contenido de config y no es crítica para el runtime. **Default elegido: incluir `AreaBusqueda` en el snapshot** (ver ajuste abajo).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Application/BdtValidatorsTests.cs
using FluentValidation.TestHelper;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Validators;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class BdtValidatorsTests
{
    [Fact]
    public void ValidarTesoro_requires_partidaId_and_imagen()
    {
        var v = new ValidarTesoroCommandValidator();
        v.TestValidate(new ValidarTesoroCommand(Guid.Empty, Guid.NewGuid(), ""))
            .ShouldHaveValidationErrorFor(c => c.PartidaId);
        v.TestValidate(new ValidarTesoroCommand(Guid.NewGuid(), Guid.NewGuid(), ""))
            .ShouldHaveValidationErrorFor(c => c.ImagenBase64);
        v.TestValidate(new ValidarTesoroCommand(Guid.NewGuid(), Guid.NewGuid(), "Zm9v"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AvanzarEtapa_requires_partidaId()
    {
        var v = new AvanzarEtapaCommandValidator();
        v.TestValidate(new AvanzarEtapaCommand(Guid.Empty)).ShouldHaveValidationErrorFor(c => c.PartidaId);
        v.TestValidate(new AvanzarEtapaCommand(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~BdtValidatorsTests"`
Expected: FAIL (tipos no existen).

- [ ] **Step 3: Write minimal implementation**

```csharp
// Application/Commands/ValidarTesoroCommand.cs
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Commands;
public sealed record ValidarTesoroCommand(Guid PartidaId, Guid ParticipanteId, string ImagenBase64)
    : IRequest<ValidacionTesoroResponse>;
```
```csharp
// Application/Commands/AvanzarEtapaCommand.cs
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Commands;
public sealed record AvanzarEtapaCommand(Guid PartidaId) : IRequest<AvanceEtapaResponse>;
```
```csharp
// Application/Queries/ObtenerEtapaActualQuery.cs
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Queries;
public sealed record ObtenerEtapaActualQuery(Guid PartidaId) : IRequest<EtapaActualDto>;
```
```csharp
// Application/DTOs/BdtRuntimeDtos.cs
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record ValidacionTesoroResponse(
    Guid PartidaId, Guid EtapaId, string Resultado, bool Gano, bool CerroEtapa, int? Puntaje);

public sealed record AvanceEtapaResponse(
    Guid PartidaId, int EtapaCerradaOrden, int? EtapaActivadaOrden, bool SinMasEtapas);

public sealed record EtapaActualDto(
    Guid PartidaId, Guid JuegoId, Guid EtapaId, int Orden, string AreaBusqueda,
    int TiempoLimiteSegundos, DateTime FechaActivacion);

public sealed record ValidarTesoroRequest(string ImagenBase64);
```
```csharp
// Application/Validators/ValidarTesoroCommandValidator.cs
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;
namespace Umbral.OperacionesSesion.Application.Validators;
public sealed class ValidarTesoroCommandValidator : AbstractValidator<ValidarTesoroCommand>
{
    public ValidarTesoroCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.ImagenBase64).NotEmpty();
        // ParticipanteId NO se valida: proviene del claim sub, no del body.
    }
}
```
```csharp
// Application/Validators/AvanzarEtapaCommandValidator.cs
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;
namespace Umbral.OperacionesSesion.Application.Validators;
public sealed class AvanzarEtapaCommandValidator : AbstractValidator<AvanzarEtapaCommand>
{
    public AvanzarEtapaCommandValidator() => RuleFor(c => c.PartidaId).NotEmpty();
}
```
**Ajuste `AreaBusqueda` en el snapshot** (necesario para `EtapaActualDto.AreaBusqueda`): añadir a `JuegoResumen` una prop `public string AreaBusqueda { get; private set; } = string.Empty;`, set en el ctor BDT (`AreaBusqueda = areaBusqueda`), y la sobrecarga BDT pasa a `JuegoResumen(Guid juegoId, int orden, TipoJuego tipoJuego, string areaBusqueda, IEnumerable<EtapaSnapshot> etapas)`. Actualizar: el ctor BDT de Task 3, su test (`JuegoBdt` helper pasa `""`), el `MapearJuego` BDT de Task 6 (`new JuegoResumen(j.JuegoId, j.Orden, tipo, j.Bdt.AreaBusqueda, etapas)`), y el helper `SesionBdtIniciada` de Task 4. Mapear en EF (Task 12) como columna `areabusqueda` nullable→default "". 

> Este ajuste cruza Task 3/4/6/12. El implementer de Task 7 lo aplica en `JuegoResumen` + actualiza los call-sites ya escritos; corre la suite completa para confirmar que nada de lo anterior se rompió.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~BdtValidatorsTests"`
Expected: PASS (2/2). Proyecto UnitTests completo verde tras el ajuste de `AreaBusqueda`.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/ValidarTesoroCommand.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/AvanzarEtapaCommand.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Queries/ObtenerEtapaActualQuery.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/BdtRuntimeDtos.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/ValidarTesoroCommandValidator.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/AvanzarEtapaCommandValidator.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/JuegoResumen.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PublicarPartidaCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/JuegoResumenBdtTests.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaBdtTests.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/BdtValidatorsTests.cs
git commit -m "SP-3d T7: commands/queries/DTOs/validators BDT + AreaBusqueda en snapshot"
```

---

### Task 8: ValidarTesoroCommandHandler

**Files:**
- Create: `.../Application/Handlers/Commands/ValidarTesoroCommandHandler.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Application/ValidarTesoroCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ValidarTesoroCommand`/`ValidacionTesoroResponse` (Task 7), `SesionPartida.ValidarTesoro` (Task 4), eventos BDT (Task 5), `IQrDecoder` (Task 1), `ISesionPartidaRepository`/`IOperacionesSesionUnitOfWork`/`TimeProvider` (existentes).
- Produces: `ValidarTesoroCommandHandler : IRequestHandler<ValidarTesoroCommand, ValidacionTesoroResponse>`. Helper de emisión de `EtapaBDTActivada` reutilizado (de Task 11): `IniciarPartidaCommandHandler.PublicarEtapaActivadaSiBdtAsync`. **Importante:** Task 11 crea ese helper; Task 8 lo invoca. Si se ejecuta T8 antes que T11, definir el helper en T8 e invocarlo aquí; T11 solo lo reutiliza. **Decisión: T8 crea el helper** (igual que en 3c el helper de Trivia vivía en `IniciarPartidaCommandHandler` pero fue introducido junto a los handlers que lo usan).

> El handler decodifica `ImagenBase64 → byte[]` vía `Convert.FromBase64String` y delega en `IQrDecoder` dentro del dominio. Patrón de orden: load → SesionNoEncontrada / now=TimeProvider / `sesion.ValidarTesoro(...)` / SAVE / publish. Save-before-publish.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Application/ValidarTesoroCommandHandlerTests.cs
using System.Text;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ValidarTesoroCommandHandlerTests
{
    private sealed class TextoQrDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) =>
            imagen.Length == 0 ? null : Encoding.UTF8.GetString(imagen);
    }

    private static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

    // Construye sesión BDT iniciada en el repo fake (1 etapa "QR-1", min=1) y devuelve (repo, uow, fake, partidaId, jugadorId).
    // Reusar el patrón de ResponderPreguntaCommandHandlerTests (3c): FakeSesionPartidaRepository + helper de construcción.
    // [El implementer reutiliza el helper de armado de sesión iniciada que ya existe en los tests de 3c, adaptado a BDT.]

    [Fact]
    public async Task Correct_treasure_emits_validado_ganada_cerrada_activada_or_finalize()
    {
        // Arrange: sesión BDT iniciada con 2 etapas (QR-1, QR-2), jugador inscrito.
        var (repo, uow, fake, partidaId, jugador) = BdtBuilder.SesionIniciada(("QR-1", 60), ("QR-2", 60));
        var handler = new ValidarTesoroCommandHandler(repo, uow, fake, FakeTimeProvider.At(new DateTime(2026,6,28,10,0,5)), new TextoQrDecoder());

        // Act
        var resp = await handler.Handle(new ValidarTesoroCommand(partidaId, jugador, B64("QR-1")), default);

        // Assert
        Assert.True(resp.Gano);
        Assert.Equal(50, resp.Puntaje);
        Assert.Single(fake.TesorosValidados);
        Assert.Single(fake.EtapasGanadas);
        Assert.Single(fake.EtapasCerradas);
        Assert.Single(fake.EtapasActivadas);     // auto-avance a la etapa 2
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Wrong_treasure_emits_only_validado()
    {
        var (repo, uow, fake, partidaId, jugador) = BdtBuilder.SesionIniciada(("QR-1", 60));
        var handler = new ValidarTesoroCommandHandler(repo, uow, fake, FakeTimeProvider.At(new DateTime(2026,6,28,10,0,5)), new TextoQrDecoder());
        var resp = await handler.Handle(new ValidarTesoroCommand(partidaId, jugador, B64("QR-X")), default);
        Assert.False(resp.Gano);
        Assert.Single(fake.TesorosValidados);
        Assert.Empty(fake.EtapasGanadas);
        Assert.Empty(fake.EtapasActivadas);
    }
}
```

> `BdtBuilder.SesionIniciada(...)` es un helper de test que el implementer crea (en `tests/.../Application/Fakes/BdtBuilder.cs`) reutilizando `FakeSesionPartidaRepository` + `FakeTimeProvider` ya existentes de 3c. Debe: construir `SesionPartida` BDT vía `ConfiguracionSnapshot`, inscribir al jugador, iniciar, meterla en el repo, devolver la tupla. `FakeTimeProvider.At(...)` ya existe (3c). Si su API difiere, usar la real.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~ValidarTesoroCommandHandlerTests"`
Expected: FAIL (handler no existe).

- [ ] **Step 3: Write minimal implementation**

```csharp
// Application/Handlers/Commands/ValidarTesoroCommandHandler.cs
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class ValidarTesoroCommandHandler : IRequestHandler<ValidarTesoroCommand, ValidacionTesoroResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;
    private readonly IQrDecoder _decoder;

    public ValidarTesoroCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events, TimeProvider timeProvider, IQrDecoder decoder)
    {
        _sesiones = sesiones; _unitOfWork = unitOfWork; _events = events; _timeProvider = timeProvider; _decoder = decoder;
    }

    public async Task<ValidacionTesoroResponse> Handle(ValidarTesoroCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var imagen = Convert.FromBase64String(request.ImagenBase64);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var r = sesion.ValidarTesoro(request.ParticipanteId, imagen, now, _decoder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarTesoroQRValidadoAsync(
            new TesoroQRValidadoEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaId,
                r.ParticipanteId, r.Resultado.ToString(), r.Instante), cancellationToken);

        if (r.Gano)
        {
            await _events.PublicarEtapaBDTGanadaAsync(
                new EtapaBDTGanadaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaId,
                    r.ParticipanteId, r.Puntaje!.Value, r.TiempoResolucionMs!.Value), cancellationToken);
            await _events.PublicarEtapaBDTCerradaAsync(
                new EtapaBDTCerradaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaId,
                    "Ganador", now, r.GanadorParticipanteId), cancellationToken);
            var juego = sesion.Juegos.First(j => j.JuegoId == r.JuegoId);
            await IniciarPartidaCommandHandler.PublicarEtapaActivadaSiBdtAsync(_events, sesion, juego, cancellationToken);
        }
        else if (r.CerroEtapa)
        {
            // cerrada por tiempo en el intento tardío
            await _events.PublicarEtapaBDTCerradaAsync(
                new EtapaBDTCerradaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaId,
                    "Tiempo", now, null), cancellationToken);
            var juego = sesion.Juegos.First(j => j.JuegoId == r.JuegoId);
            await IniciarPartidaCommandHandler.PublicarEtapaActivadaSiBdtAsync(_events, sesion, juego, cancellationToken);
        }

        return new ValidacionTesoroResponse(sesion.PartidaId, r.EtapaId, r.Resultado.ToString(), r.Gano, r.CerroEtapa, r.Puntaje);
    }
}
```
> El helper `IniciarPartidaCommandHandler.PublicarEtapaActivadaSiBdtAsync` se crea aquí (Task 8) si aún no existe — ver Task 11 para su definición exacta. Colocarlo como `internal static` en `IniciarPartidaCommandHandler` (junto a `PublicarPreguntaActivadaSiTriviaAsync`):
```csharp
    internal static async Task PublicarEtapaActivadaSiBdtAsync(
        ISesionEventsPublisher events, SesionPartida sesion, JuegoResumen juego, CancellationToken cancellationToken)
    {
        var etapa = juego.EtapaActiva;
        if (etapa is null) return;
        await events.PublicarEtapaBDTActivadaAsync(
            new EtapaBDTActivadaEvent(sesion.PartidaId, sesion.Id.Valor, juego.JuegoId, etapa.EtapaId,
                etapa.Orden, etapa.TiempoLimiteSegundos, etapa.FechaActivacion!.Value), cancellationToken);
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~ValidarTesoroCommandHandlerTests"`
Expected: PASS (2/2).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/ValidarTesoroCommandHandler.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/IniciarPartidaCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/BdtBuilder.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ValidarTesoroCommandHandlerTests.cs
git commit -m "SP-3d T8: ValidarTesoroCommandHandler (decode + eventos + save-before-publish)"
```

---

### Task 9: AvanzarEtapaCommandHandler

**Files:**
- Create: `.../Application/Handlers/Commands/AvanzarEtapaCommandHandler.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Application/AvanzarEtapaCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `AvanzarEtapaCommand`/`AvanceEtapaResponse` (Task 7), `SesionPartida.AvanzarEtapa` (Task 4), eventos BDT (Task 5).
- Produces: `AvanzarEtapaCommandHandler : IRequestHandler<AvanzarEtapaCommand, AvanceEtapaResponse>`.

> Patrón espejo de `AvanzarPreguntaCommandHandler`: load → SesionNoEncontrada / now / `sesion.AvanzarEtapa(now)` / SAVE / publish `EtapaBDTCerrada` SIEMPRE (motivo `r.MotivoCierre.ToString()`, ganador null) + `EtapaBDTActivada` si `r.EtapaActivadaId is not null`.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Application/AvanzarEtapaCommandHandlerTests.cs
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class AvanzarEtapaCommandHandlerTests
{
    [Fact]
    public async Task Advance_to_next_emits_cerrada_and_activada()
    {
        var (repo, uow, fake, partidaId, _) = BdtBuilder.SesionIniciada(("QR-1", 60), ("QR-2", 60));
        var handler = new AvanzarEtapaCommandHandler(repo, uow, fake, FakeTimeProvider.At(new DateTime(2026,6,28,10,0,5)));
        var resp = await handler.Handle(new AvanzarEtapaCommand(partidaId), default);
        Assert.False(resp.SinMasEtapas);
        Assert.Equal(2, resp.EtapaActivadaOrden);
        Assert.Single(fake.EtapasCerradas);
        Assert.Single(fake.EtapasActivadas);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Advance_on_last_stage_has_no_next()
    {
        var (repo, uow, fake, partidaId, _) = BdtBuilder.SesionIniciada(("QR-1", 60));
        var handler = new AvanzarEtapaCommandHandler(repo, uow, fake, FakeTimeProvider.At(new DateTime(2026,6,28,10,0,5)));
        var resp = await handler.Handle(new AvanzarEtapaCommand(partidaId), default);
        Assert.True(resp.SinMasEtapas);
        Assert.Single(fake.EtapasCerradas);
        Assert.Empty(fake.EtapasActivadas);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~AvanzarEtapaCommandHandlerTests"`
Expected: FAIL (handler no existe).

- [ ] **Step 3: Write minimal implementation**

```csharp
// Application/Handlers/Commands/AvanzarEtapaCommandHandler.cs
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class AvanzarEtapaCommandHandler : IRequestHandler<AvanzarEtapaCommand, AvanceEtapaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public AvanzarEtapaCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events, TimeProvider timeProvider)
    {
        _sesiones = sesiones; _unitOfWork = unitOfWork; _events = events; _timeProvider = timeProvider;
    }

    public async Task<AvanceEtapaResponse> Handle(AvanzarEtapaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var r = sesion.AvanzarEtapa(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarEtapaBDTCerradaAsync(
            new EtapaBDTCerradaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaCerradaId,
                r.MotivoCierre.ToString(), now, null), cancellationToken);

        if (r.EtapaActivadaId is not null)
        {
            await _events.PublicarEtapaBDTActivadaAsync(
                new EtapaBDTActivadaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaActivadaId.Value,
                    r.EtapaActivadaOrden!.Value, r.TiempoLimiteActivadaSegundos!.Value, r.FechaActivacionActivada!.Value),
                cancellationToken);
        }

        return new AvanceEtapaResponse(sesion.PartidaId, r.EtapaCerradaOrden, r.EtapaActivadaOrden, r.SinMasEtapas);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~AvanzarEtapaCommandHandlerTests"`
Expected: PASS (2/2).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/AvanzarEtapaCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/AvanzarEtapaCommandHandlerTests.cs
git commit -m "SP-3d T9: AvanzarEtapaCommandHandler (cierre operador + eventos)"
```

---

### Task 10: ObtenerEtapaActualQueryHandler (read-only, no-leak)

**Files:**
- Create: `.../Application/Handlers/Queries/ObtenerEtapaActualQueryHandler.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerEtapaActualQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `ObtenerEtapaActualQuery`/`EtapaActualDto` (Task 7), `JuegoResumen.EtapaActiva`/`AreaBusqueda` (Task 3/7).
- Produces: `ObtenerEtapaActualQueryHandler : IRequestHandler<ObtenerEtapaActualQuery, EtapaActualDto>`. **No-leak: el DTO no contiene `CodigoQREsperado`.**

> Patrón espejo de `ObtenerPreguntaActualQueryHandler`: load → SesionNoEncontrada / juego activo / `EtapaActiva ?? NoHayEtapaActiva` / map a DTO sin el QR esperado.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Application/ObtenerEtapaActualQueryHandlerTests.cs
using System.Reflection;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ObtenerEtapaActualQueryHandlerTests
{
    [Fact]
    public async Task Returns_active_stage_without_leaking_expected_qr()
    {
        var (repo, _, _, partidaId, _) = BdtBuilder.SesionIniciada(("QR-1", 60));
        var handler = new ObtenerEtapaActualQueryHandler(repo);
        var dto = await handler.Handle(new ObtenerEtapaActualQuery(partidaId), default);
        Assert.Equal(1, dto.Orden);
        // No-leak: el DTO no expone CodigoQREsperado en ninguna propiedad
        Assert.Null(typeof(EtapaActualDto).GetProperty("CodigoQREsperado", BindingFlags.Public | BindingFlags.Instance));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~ObtenerEtapaActualQueryHandlerTests"`
Expected: FAIL (handler no existe).

- [ ] **Step 3: Write minimal implementation**

```csharp
// Application/Handlers/Queries/ObtenerEtapaActualQueryHandler.cs
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerEtapaActualQueryHandler : IRequestHandler<ObtenerEtapaActualQuery, EtapaActualDto>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerEtapaActualQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<EtapaActualDto> Handle(ObtenerEtapaActualQuery request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var juego = sesion.Juegos.FirstOrDefault(j => j.Estado == EstadoJuego.Activo);
        var etapa = juego?.EtapaActiva ?? throw new NoHayEtapaActivaException(request.PartidaId);

        return new EtapaActualDto(
            sesion.PartidaId, juego!.JuegoId, etapa.EtapaId, etapa.Orden, juego.AreaBusqueda,
            etapa.TiempoLimiteSegundos, etapa.FechaActivacion!.Value);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~ObtenerEtapaActualQueryHandlerTests"`
Expected: PASS (1/1).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerEtapaActualQueryHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerEtapaActualQueryHandlerTests.cs
git commit -m "SP-3d T10: ObtenerEtapaActualQueryHandler (read-only, no-leak)"
```

---

### Task 11: Emisión de EtapaBDTActivada en inicio y finalización de juego

**Files:**
- Modify: `.../Application/Handlers/Commands/IniciarPartidaCommandHandler.cs`
- Modify: `.../Application/Handlers/Commands/FinalizarJuegoActualCommandHandler.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Application/EtapaActivadaEnInicioTests.cs`

**Interfaces:**
- Consumes: helper `PublicarEtapaActivadaSiBdtAsync` (creado en Task 8), eventos BDT (Task 5).
- Produces: el helper se invoca tras `JuegoActivado` en `PublicarEventosInicioAsync` (rama Iniciada) y en el handler de finalización (rama Avanzado, cuando el juego activado es BDT).

> En 3c, `PublicarEventosInicioAsync` ya invoca `PublicarPreguntaActivadaSiTriviaAsync` tras `JuegoActivado`. Aquí se añade, en la MISMA posición, la llamada gemela BDT. Ambos helpers hacen short-circuit si no hay pregunta/etapa activa, así que invocar los dos siempre es seguro (un juego es Trivia XOR BDT).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Application/EtapaActivadaEnInicioTests.cs
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class EtapaActivadaEnInicioTests
{
    [Fact]
    public async Task Iniciar_bdt_emits_partida_iniciada_juego_activado_and_etapa_activada()
    {
        // Sesión BDT publicada (en Lobby), jugador inscrito, modo Manual, min=1.
        var (repo, uow, fake, partidaId) = BdtBuilder.SesionEnLobbyConInscrito(("QR-1", 60));
        var handler = new IniciarPartidaCommandHandler(repo, uow, fake, FakeTimeProvider.At(new DateTime(2026,6,28,10,0,0)));
        await handler.Handle(new IniciarPartidaCommand(partidaId), default);
        Assert.Single(fake.PartidasIniciadas);
        Assert.Single(fake.JuegosActivados);
        Assert.Single(fake.EtapasActivadas);
        Assert.Equal(1, fake.EtapasActivadas[0].Orden);
    }
}
```

> `BdtBuilder.SesionEnLobbyConInscrito(...)` = variante del builder que deja la sesión en Lobby (publicada + inscrita) sin iniciar. El implementer lo añade junto a `SesionIniciada`.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~EtapaActivadaEnInicioTests"`
Expected: FAIL (`fake.EtapasActivadas` vacío — el inicio no emite la activación BDT todavía).

- [ ] **Step 3: Write minimal implementation**

En `IniciarPartidaCommandHandler.PublicarEventosInicioAsync`, rama `TipoResultadoInicio.Iniciada`, añadir tras `PublicarPreguntaActivadaSiTriviaAsync`:
```csharp
                await PublicarPreguntaActivadaSiTriviaAsync(events, sesion, juego, cancellationToken);
                await PublicarEtapaActivadaSiBdtAsync(events, sesion, juego, cancellationToken);
```
En `FinalizarJuegoActualCommandHandler` (rama donde `ResultadoAvance` es `Avanzado` y publica `JuegoActivado`), añadir tras la publicación de `JuegoActivado` la llamada gemela:
```csharp
                await IniciarPartidaCommandHandler.PublicarPreguntaActivadaSiTriviaAsync(events, sesion, juegoActivado, cancellationToken);
                await IniciarPartidaCommandHandler.PublicarEtapaActivadaSiBdtAsync(events, sesion, juegoActivado, cancellationToken);
```
> Revisar `FinalizarJuegoActualCommandHandler.cs` para los nombres reales de la variable del juego activado (`resultado.JuegoActivado!`) y si ya invoca el helper de Trivia (3c T11 lo añadió). Si el de Trivia ya está, solo añadir la línea BDT junto a él, misma posición.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~EtapaActivadaEnInicioTests"`
Expected: PASS (1/1). Proyecto UnitTests completo verde (los tests de inicio Trivia de 3b/3c siguen verdes vía el short-circuit del helper BDT).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/IniciarPartidaCommandHandler.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/FinalizarJuegoActualCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/EtapaActivadaEnInicioTests.cs
git commit -m "SP-3d T11: emite EtapaBDTActivada al activarse un juego BDT (inicio + avance)"
```

> === Application layer (T5–T11) COMPLETO ===

---

### Task 12: EF mappings (etapas/tesoros) + migración aditiva

**Files:**
- Modify: `.../Infrastructure/Persistence/OperacionesSesionDbContext.cs`
- Create: `.../Infrastructure/Persistence/Migrations/<timestamp>_SP3dRuntimeBdt.cs` (+ Designer + ModelSnapshot actualizado)
- Test: `.../tests/Umbral.OperacionesSesion.IntegrationTests/BdtSnapshotPersistenceTests.cs`

**Interfaces:**
- Consumes: `EtapaSnapshot`/`TesoroQR` (Task 1/2), `JuegoResumen` con `_etapas`+`AreaBusqueda` (Task 3/7).
- Produces: mapeos EF de `etapas_snapshot`/`tesoros_qr` + `JuegoResumen.AreaBusqueda` columna + `HasMany(Etapas)`; migración aditiva.

> El test de integración será RED hasta Task 13 (falta el `Include` de etapas en el repo). Documentar: T12 crea el esquema; T13 cierra el round-trip. Patrón idéntico a 3c T12→T13.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../IntegrationTests/BdtSnapshotPersistenceTests.cs
// Usa el mismo patrón de TriviaSnapshotPersistenceTests (3c): 2 contextos (InMemory o Npgsql según fixture),
// guarda una SesionPartida BDT con 2 etapas + 1 tesoro, recarga en contexto nuevo y asserta.
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class BdtSnapshotPersistenceTests
{
    private static DbContextOptions<OperacionesSesionDbContext> InMemoryOptions() =>
        new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase($"bdt-{Guid.NewGuid()}").Options;

    [Fact]
    public async Task Roundtrip_persists_bdt_stages_and_treasures()
    {
        var options = InMemoryOptions();
        var partidaId = Guid.NewGuid();

        var etapas = new List<EtapaSnapshot>
        {
            new(Guid.NewGuid(), 1, "QR-1", 50, 60),
            new(Guid.NewGuid(), 2, "QR-2", 70, 90),
        };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Plaza", etapas);
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);

        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            ctx.Sesiones.Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var reloaded = await ctx.Sesiones
                .Include(s => s.Juegos).ThenInclude(j => j.Etapas).ThenInclude(e => e.Tesoros)
                .FirstAsync(s => s.PartidaId == partidaId);
            var j = reloaded.Juegos.Single();
            Assert.Equal("Plaza", j.AreaBusqueda);
            Assert.Equal(2, j.Etapas.Count);
            Assert.Equal("QR-1", j.Etapas.OrderBy(e => e.Orden).First().CodigoQREsperado);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests" --filter "FullyQualifiedName~BdtSnapshotPersistenceTests"`
Expected: FAIL (mapeos no existen — error de modelo EF).

- [ ] **Step 3: Write minimal implementation**

En `OperacionesSesionDbContext.OnModelCreating`, en el bloque `JuegoResumen` añadir la columna AreaBusqueda + HasMany(Etapas) (sin tocar el `HasMany(Preguntas)`):
```csharp
            entity.Property(x => x.AreaBusqueda).HasColumnName("areabusqueda").IsRequired();
            entity.HasMany(x => x.Etapas).WithOne().HasForeignKey("juegoid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Etapas).UsePropertyAccessMode(PropertyAccessMode.Field);
```
Añadir 2 bloques de entidad nuevos (junto a los de Pregunta/Opcion/Respuesta):
```csharp
        modelBuilder.Entity<EtapaSnapshot>(entity =>
        {
            entity.ToTable("etapas_snapshot");
            entity.HasKey(x => x.EtapaId);
            entity.Property(x => x.EtapaId).HasColumnName("etapaid").ValueGeneratedNever();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.CodigoQREsperado).HasColumnName("codigoqresperado").IsRequired();
            entity.Property(x => x.Puntaje).HasColumnName("puntaje").IsRequired();
            entity.Property(x => x.TiempoLimiteSegundos).HasColumnName("tiempolimitesegundos").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estadoetapa").IsRequired();
            entity.Property(x => x.FechaActivacion).HasColumnName("fechaactivacion");
            entity.Property(x => x.FechaCierre).HasColumnName("fechacierre");
            entity.Property(x => x.MotivoCierre).HasColumnName("motivocierre");
            entity.Property(x => x.GanadorParticipanteId).HasColumnName("ganadorparticipanteid");
            entity.Property(x => x.TiempoResolucionMs).HasColumnName("tiemporesolucionms");
            entity.HasMany(x => x.Tesoros).WithOne().HasForeignKey("etapaid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Tesoros).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TesoroQR>(entity =>
        {
            entity.ToTable("tesoros_qr");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.ParticipanteId).HasColumnName("participanteid").IsRequired();
            entity.Property(x => x.QrDecodificado).HasColumnName("qrdecodificado");
            entity.Property(x => x.Resultado).HasColumnName("resultado").IsRequired();
            entity.Property(x => x.FechaEnvio).HasColumnName("fechaenvio").IsRequired();
        });
```
Generar la migración aditiva (desde `services/operaciones-sesion`, usar el binario local por el mismatch de arch del global):
```bash
cd services/operaciones-sesion
dotnet tool run dotnet-ef migrations add SP3dRuntimeBdt \
  --project src/Umbral.OperacionesSesion.Infrastructure \
  --startup-project src/Umbral.OperacionesSesion.Api
```
Verificar que el `Up` solo: `CreateTable etapas_snapshot`, `CreateTable tesoros_qr`, índices FK, y `AddColumn areabusqueda` en `sesion_juegos` (NOT NULL default ""). `Down`: drops inversos. Cero ALTER destructivo a tablas existentes. Si la columna `areabusqueda` sale `NOT NULL` sin default sobre filas existentes, fijar `defaultValue: ""` en el `AddColumn`.

- [ ] **Step 4: Run test to verify it fails (todavía RED — esperado)**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests" --filter "FullyQualifiedName~BdtSnapshotPersistenceTests"`
Expected: el modelo EF ya válida (build OK), pero el round-trip puede fallar por falta del `Include` de etapas en el repo si el test usara el repo. **Aquí el test usa Include explícito**, así que debería PASAR con InMemory. Si el fixture de integración usa el repo real, quedará RED hasta Task 13. Documentar el estado real observado en el reporte.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/ \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/BdtSnapshotPersistenceTests.cs
git commit -m "SP-3d T12: mapeos EF etapas/tesoros + AreaBusqueda + migración aditiva"
```

---

### Task 13: Eager-load del grafo BDT en el repositorio

**Files:**
- Modify: `.../Infrastructure/Persistence/SesionPartidaRepository.cs`
- Test: (cierra el round-trip de Task 12 vía el repo real, si el fixture lo usa)

**Interfaces:**
- Produces: `GetByPartidaIdAsync` carga también `Juegos → Etapas → Tesoros`.

- [ ] **Step 1: Confirm the gap**

El round-trip vía repo (o el contract lifecycle de Task 16) no trae las etapas porque `GetByPartidaIdAsync` no las incluye. Correr el test de integración para ver el grafo BDT vacío si aplica.

- [ ] **Step 2: Write minimal implementation**

En `SesionPartidaRepository.GetByPartidaIdAsync`, añadir las dos ramas Include de BDT junto a las de Trivia (mismo patrón EF8 multi-branch):
```csharp
    public Task<SesionPartida?> GetByPartidaIdAsync(Guid partidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Opciones)
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Respuestas)
            .Include(s => s.Juegos).ThenInclude(j => j.Etapas).ThenInclude(e => e.Tesoros)
            .Include(s => s.Inscripciones)
            .FirstOrDefaultAsync(s => s.PartidaId == partidaId, cancellationToken);
```

- [ ] **Step 3: Run tests to verify green**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests"`
Expected: PASS (incl. `BdtSnapshotPersistenceTests` + los de Trivia 3c). Correr también UnitTests completo: verde.

- [ ] **Step 4: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs
git commit -m "SP-3d T13: eager-load del grafo BDT (juegos→etapas→tesoros)"
```

> === Infrastructure layer (T12–T13) COMPLETO ===

---

### Task 14: ZXingQrDecoder (impl real) + registro DI

**Files:**
- Create: `.../Infrastructure/Services/ZXingQrDecoder.cs`
- Modify: `.../Infrastructure/Umbral.OperacionesSesion.Infrastructure.csproj` (NuGet ZXing.Net)
- Modify: `.../Infrastructure/DependencyInjection.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/ZXingQrDecoderTests.cs`

**Interfaces:**
- Consumes: `IQrDecoder` (Task 1).
- Produces: `ZXingQrDecoder : IQrDecoder` (decodifica PNG/JPEG → texto, null si ilegible), registrado en DI como `IQrDecoder`.

> El unit test genera un QR real con `ZXing.BarcodeWriterPixelData` (round-trip), lo codifica a PNG en memoria y verifica que `ZXingQrDecoder.Decodificar` recupera el texto. Esto prueba la impl real sin depender de assets. Usar el paquete `ZXing.Net` + `System.Drawing.Common` o `ZXing.Net` con `BarcodeReaderGeneric<byte[]>` sobre `RGBLuminanceSource`. Para portabilidad en Linux CI, preferir `ZXing.Net` puro con `RGBLuminanceSource` (sin System.Drawing). El implementer elige el camino que compile y pase en este entorno Linux; si `System.Drawing.Common` da problemas en Linux, usar `RGBLuminanceSource` con un decodificador de PNG manejado (p.ej. `SixLabors.ImageSharp` para leer pixeles → `RGBLuminanceSource`). **Default: ZXing.Net + SixLabors.ImageSharp** (ambos cross-platform, sin dependencias nativas).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Infrastructure/ZXingQrDecoderTests.cs
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Umbral.OperacionesSesion.Infrastructure.Services;
using ZXing;
using ZXing.QrCode;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure;

public class ZXingQrDecoderTests
{
    // Genera un PNG con un QR que codifica `texto`.
    private static byte[] QrPng(string texto)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions { Width = 200, Height = 200, Margin = 1 }
        };
        var pixelData = writer.Write(texto);
        using var image = Image.LoadPixelData<Bgra32>(pixelData.Pixels, pixelData.Width, pixelData.Height);
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    [Fact]
    public void Decodifica_qr_valido()
    {
        var decoder = new ZXingQrDecoder();
        var png = QrPng("ETAPA-UMBRAL-1");
        Assert.Equal("ETAPA-UMBRAL-1", decoder.Decodificar(png));
    }

    [Fact]
    public void Imagen_sin_qr_devuelve_null()
    {
        var decoder = new ZXingQrDecoder();
        using var blank = new Image<Bgra32>(50, 50, new Bgra32(255, 255, 255, 255));
        using var ms = new MemoryStream();
        blank.Save(ms, new PngEncoder());
        Assert.Null(decoder.Decodificar(ms.ToArray()));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~ZXingQrDecoderTests"`
Expected: FAIL (paquetes + clase no existen).

- [ ] **Step 3: Write minimal implementation**

Añadir paquetes al csproj de Infrastructure (versiones compatibles netstandard/net8; el implementer ancla las últimas estables que restauren):
```xml
    <PackageReference Include="ZXing.Net" Version="0.16.9" />
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.5" />
```
El proyecto UnitTests necesita `ZXing.Net` + `SixLabors.ImageSharp` para el test (añadirlos también a su csproj).
```csharp
// Infrastructure/Services/ZXingQrDecoder.cs
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Umbral.OperacionesSesion.Domain.Abstractions;
using ZXing;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

public sealed class ZXingQrDecoder : IQrDecoder
{
    public string? Decodificar(byte[] imagen)
    {
        if (imagen is null || imagen.Length == 0) return null;
        try
        {
            using var image = Image.Load<Rgba32>(imagen);
            var width = image.Width;
            var height = image.Height;
            var rgbBytes = new byte[width * height * 3];
            var idx = 0;
            image.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (var x = 0; x < row.Length; x++)
                    {
                        rgbBytes[idx++] = row[x].R;
                        rgbBytes[idx++] = row[x].G;
                        rgbBytes[idx++] = row[x].B;
                    }
                }
            });
            var source = new RGBLuminanceSource(rgbBytes, width, height, RGBLuminanceSource.BitmapFormat.RGB24);
            var reader = new BarcodeReaderGeneric { AutoRotate = true, Options = { TryHarder = true, PossibleFormats = new[] { BarcodeFormat.QR_CODE } } };
            var result = reader.Decode(source);
            return result?.Text;
        }
        catch
        {
            return null; // imagen corrupta / formato no soportado → ilegible
        }
    }
}
```
> Verificar la API exacta de `RGBLuminanceSource`/`BarcodeReaderGeneric` para la versión de ZXing.Net que restaure; ajustar el constructor/Decode si la firma difiere. La forma "pixeles RGB24 → RGBLuminanceSource → BarcodeReaderGeneric" es cross-platform (no System.Drawing).

Registrar en `DependencyInjection.cs` (junto a los otros AddScoped):
```csharp
        services.AddScoped<Umbral.OperacionesSesion.Domain.Abstractions.IQrDecoder, ZXingQrDecoder>();
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~ZXingQrDecoderTests"`
Expected: PASS (2/2).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/ZXingQrDecoder.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Umbral.OperacionesSesion.Infrastructure.csproj \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/DependencyInjection.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/ZXingQrDecoderTests.cs
git commit -m "SP-3d T14: ZXingQrDecoder (decode real cross-platform) + registro DI"
```

---

### Task 15: Controller (+3 endpoints) + middleware (403/409)

**Files:**
- Modify: `.../Api/Controllers/SesionesController.cs`
- Modify: `.../Api/Middleware/ExceptionHandlingMiddleware.cs`
- Test: `.../tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerBdtTests.cs`, `.../tests/Umbral.OperacionesSesion.UnitTests/Api/ExceptionHandlingMiddlewareBdtTests.cs`

**Interfaces:**
- Consumes: comandos/query BDT (Task 7), `ValidarTesoroRequest` (Task 7), excepciones BDT (Task 4).
- Produces: 3 endpoints REST + 3 arms nuevos de middleware (Conflict).

> Espejo de los 3 endpoints Trivia (`pregunta-actual/...`). El de validar tesoro toma `ParticipanteId` del claim `sub` vía `ObtenerParticipanteId()` (ya existe) y el body `{ imagenBase64 }`. El proyecto NO usa Moq: los controller tests usan el `FakeSender` existente (revisar `SesionesControllerTriviaTests` de 3c para el patrón exacto del doble de `ISender`).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/.../Api/SesionesControllerBdtTests.cs
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Api.Controllers;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerBdtTests
{
    // Reutiliza el FakeSender del proyecto (mismo que SesionesControllerTriviaTests de 3c).
    [Fact]
    public async Task Validar_tesoro_dispatches_command_with_sub_and_imagen()
    {
        var partidaId = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var sender = new FakeSender(new ValidacionTesoroResponse(partidaId, Guid.NewGuid(), "Valido", true, true, 50));
        var controller = ControllerConSub(sender, sub);
        var result = await controller.ValidarTesoro(partidaId, new ValidarTesoroRequest("Zm9v"), default);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ValidacionTesoroResponse>(ok.Value);
        var cmd = Assert.IsType<ValidarTesoroCommand>(sender.LastRequest);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(sub, cmd.ParticipanteId);
        Assert.Equal("Zm9v", cmd.ImagenBase64);
    }

    [Fact]
    public async Task Avanzar_etapa_dispatches_command()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new AvanceEtapaResponse(partidaId, 1, 2, false));
        var controller = ControllerConSub(sender, Guid.NewGuid());
        var result = await controller.AvanzarEtapa(partidaId, default);
        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<AvanzarEtapaCommand>(sender.LastRequest);
    }

    // ControllerConSub: instancia SesionesController con ClaimsPrincipal que tiene sub=<id>.
    // Reusar el helper de SesionesControllerTriviaTests (3c).
}
```
```csharp
// tests/.../Api/ExceptionHandlingMiddlewareBdtTests.cs
// Reusar el patrón de ExceptionHandlingMiddlewareTests (3c): invoca el middleware con cada excepción
// y asserta el status code.
using System.Net;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class ExceptionHandlingMiddlewareBdtTests
{
    [Theory]
    [InlineData(typeof(JuegoActivoNoEsBDTException))]
    [InlineData(typeof(NoHayEtapaActivaException))]
    [InlineData(typeof(JuegoConEtapasPendientesException))]
    public async Task Bdt_conflicts_map_to_409(Type excType)
    {
        var status = await MiddlewareTestHarness.StatusFor(excType, partidaId: Guid.NewGuid());
        Assert.Equal((int)HttpStatusCode.Conflict, status);
    }
}
```
> `MiddlewareTestHarness` / `FakeSender` son los dobles ya existentes en el proyecto de tests (3c). El implementer reutiliza exactamente esos; si los nombres difieren, usar los reales vistos en `SesionesControllerTriviaTests`/`ExceptionHandlingMiddlewareTests`.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~Bdt&FullyQualifiedName~Api"`
Expected: FAIL (endpoints/arms no existen).

- [ ] **Step 3: Write minimal implementation**

En `SesionesController` añadir (después de los endpoints Trivia, antes de `ObtenerParticipanteId`):
```csharp
    [HttpPost("partidas/{partidaId:guid}/etapa-actual/tesoro")]
    public async Task<IActionResult> ValidarTesoro(Guid partidaId, [FromBody] ValidarTesoroRequest request, CancellationToken cancellationToken)
    {
        var participanteId = ObtenerParticipanteId();
        var response = await _mediator.Send(new ValidarTesoroCommand(partidaId, participanteId, request.ImagenBase64), cancellationToken);
        return Ok(response);
    }

    [HttpPost("partidas/{partidaId:guid}/etapa-actual/avance")]
    public async Task<IActionResult> AvanzarEtapa(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new AvanzarEtapaCommand(partidaId), cancellationToken));

    [HttpGet("partidas/{partidaId:guid}/etapa-actual")]
    public async Task<IActionResult> ObtenerEtapaActual(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ObtenerEtapaActualQuery(partidaId), cancellationToken));
```
En `ExceptionHandlingMiddleware.MapStatus`, añadir a la cadena de `Conflict` (junto a las de Trivia):
```csharp
            or JuegoConPreguntasPendientesException
            or JuegoActivoNoEsBDTException
            or NoHayEtapaActivaException
            or JuegoConEtapasPendientesException => HttpStatusCode.Conflict,
```
> `ParticipanteNoInscritoException` ya está mapeada a `Forbidden` (3c). No duplicar.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests"`
Expected: PASS (proyecto completo verde, incl. los nuevos BDT).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Middleware/ExceptionHandlingMiddleware.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerBdtTests.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/ExceptionHandlingMiddlewareBdtTests.cs
git commit -m "SP-3d T15: endpoints etapa-actual (tesoro/avance/get) + arms 403/409"
```

> === Api layer (T15) COMPLETO ===

---

### Task 16: Contract tests end-to-end (WebApplicationFactory)

**Files:**
- Modify: `.../tests/Umbral.OperacionesSesion.ContractTests/OperacionesSesionWebFactory.cs` (stub `IQrDecoder`)
- Create: `.../tests/Umbral.OperacionesSesion.ContractTests/BdtRuntimeEndpointsTests.cs`

**Interfaces:**
- Consumes: todo el slice BDT a través del pipeline HTTP real.
- Produces: cobertura end-to-end del lifecycle BDT + no-leak + 403/409. Stub de `IQrDecoder` que interpreta los bytes de la imagen como el texto UTF-8 del QR (determinista, sin generar PNGs en cada test).

> El `OperacionesSesionWebFactory` ya sustituye `IConfiguracionPartidaClient` por `StubConfigClient`. Añadir la misma técnica para `IQrDecoder`: `services.RemoveAll<IQrDecoder>(); services.AddSingleton<IQrDecoder>(new TextoQrDecoder());` donde `TextoQrDecoder` decodifica `bytes → UTF-8` (null si vacío). Los tests "suben" `Convert.ToBase64String(Encoding.UTF8.GetBytes("QR-1"))`.

- [ ] **Step 1: Modify the factory (stub IQrDecoder)**

En `OperacionesSesionWebFactory.ConfigureWebHost`, dentro de `ConfigureServices`, añadir tras el bloque del StubConfigClient:
```csharp
            services.RemoveAll<Umbral.OperacionesSesion.Domain.Abstractions.IQrDecoder>();
            services.AddSingleton<Umbral.OperacionesSesion.Domain.Abstractions.IQrDecoder>(new ContractTestQrDecoder());
```
Y añadir la clase al archivo (junto a `StubConfigClient`):
```csharp
public sealed class ContractTestQrDecoder : Umbral.OperacionesSesion.Domain.Abstractions.IQrDecoder
{
    // Interpreta los bytes de la "imagen" como el texto del QR (UTF-8). null si vacío.
    public string? Decodificar(byte[] imagen) =>
        imagen is null || imagen.Length == 0 ? null : System.Text.Encoding.UTF8.GetString(imagen);
}
```
> Requiere `using Microsoft.Extensions.DependencyInjection.Extensions;` (ya presente para `RemoveAll`).

- [ ] **Step 2: Write the failing test**

```csharp
// tests/.../ContractTests/BdtRuntimeEndpointsTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.ContractTests;

public class BdtRuntimeEndpointsTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;
    private readonly HttpClient _client; // operador (sin claim participante)

    public BdtRuntimeEndpointsTests(OperacionesSesionWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string Tesoro(string texto) => Convert.ToBase64String(Encoding.UTF8.GetBytes(texto));

    // Config BDT Individual con etapas (qr, puntaje) en orden, tiempo amplio.
    private static ConfiguracionPartidaDto BuildBdtConfig(int minParticipacion, params (string Qr, int Puntaje)[] etapas)
    {
        var juegoId = Guid.NewGuid();
        var etapaConfigs = new List<EtapaConfigDto>();
        for (var i = 0; i < etapas.Length; i++)
            etapaConfigs.Add(new EtapaConfigDto(Guid.NewGuid(), i + 1, etapas[i].Qr, etapas[i].Puntaje, 3600));
        var juego = new JuegoResumenDto(juegoId, 1, "BusquedaDelTesoro", Trivia: null,
            Bdt: new BdtConfigDto("Plaza central", etapaConfigs));
        return new ConfiguracionPartidaDto("Copa", "Individual", "Manual", null, minParticipacion, 10,
            new List<JuegoResumenDto> { juego });
    }

    [Fact]
    public async Task Lifecycle_validate_wins_auto_advances_finalize_terminada()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = BuildBdtConfig(1, ("QR-1", 50), ("QR-2", 70));
        var jugadorClient = _factory.CreateClientAs(jugador);

        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await jugadorClient.PostAsync($"/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/partidas/{partidaId}/inicio", null)).StatusCode);

        // GET etapa-actual → Orden 1, sin filtrar el QR esperado
        var etapa1 = await jugadorClient.GetFromJsonAsync<EtapaActualDto>($"/partidas/{partidaId}/etapa-actual");
        Assert.Equal(1, etapa1!.Orden);

        // Validar tesoro correcto → gana + auto-avance
        var val1 = await jugadorClient.PostAsJsonAsync($"/partidas/{partidaId}/etapa-actual/tesoro",
            new ValidarTesoroRequest(Tesoro("QR-1")));
        Assert.Equal(HttpStatusCode.OK, val1.StatusCode);
        var r1 = await val1.Content.ReadFromJsonAsync<ValidacionTesoroResponse>();
        Assert.True(r1!.Gano);
        Assert.Equal(50, r1.Puntaje);

        // GET etapa-actual → ahora Orden 2 (auto-avance)
        var etapa2 = await jugadorClient.GetFromJsonAsync<EtapaActualDto>($"/partidas/{partidaId}/etapa-actual");
        Assert.Equal(2, etapa2!.Orden);

        // Validar etapa 2 correcto
        var val2 = await jugadorClient.PostAsJsonAsync($"/partidas/{partidaId}/etapa-actual/tesoro",
            new ValidarTesoroRequest(Tesoro("QR-2")));
        Assert.Equal(HttpStatusCode.OK, val2.StatusCode);

        // Finalizar → Terminada (no quedan etapas abiertas)
        var fin = await _client.PostAsync($"/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.OK, fin.StatusCode);
        var avance = await fin.Content.ReadFromJsonAsync<AvanceJuegoResponse>();
        Assert.True(avance!.Terminada);
    }

    [Fact]
    public async Task Invalid_treasure_registers_without_winning()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = BuildBdtConfig(1, ("QR-1", 50));
        var jugadorClient = _factory.CreateClientAs(jugador);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await jugadorClient.PostAsync($"/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/partidas/{partidaId}/inicio", null)).StatusCode);

        var val = await jugadorClient.PostAsJsonAsync($"/partidas/{partidaId}/etapa-actual/tesoro",
            new ValidarTesoroRequest(Tesoro("QR-EQUIVOCADO")));
        Assert.Equal(HttpStatusCode.OK, val.StatusCode);
        var r = await val.Content.ReadFromJsonAsync<ValidacionTesoroResponse>();
        Assert.False(r!.Gano);
        Assert.Equal("Invalido", r.Resultado);
    }

    [Fact]
    public async Task Operator_advance_skips_open_stage()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = BuildBdtConfig(1, ("QR-1", 50), ("QR-2", 70));
        var jugadorClient = _factory.CreateClientAs(jugador);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await jugadorClient.PostAsync($"/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/partidas/{partidaId}/inicio", null)).StatusCode);

        var av1 = await _client.PostAsync($"/partidas/{partidaId}/etapa-actual/avance", null);
        var a1 = await av1.Content.ReadFromJsonAsync<AvanceEtapaResponse>();
        Assert.False(a1!.SinMasEtapas);
        Assert.Equal(2, a1.EtapaActivadaOrden);

        var av2 = await _client.PostAsync($"/partidas/{partidaId}/etapa-actual/avance", null);
        var a2 = await av2.Content.ReadFromJsonAsync<AvanceEtapaResponse>();
        Assert.True(a2!.SinMasEtapas);

        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/partidas/{partidaId}/juego-actual/finalizacion", null)).StatusCode);
    }

    [Fact]
    public async Task Validate_without_inscription_returns_403()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        var intruso = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = BuildBdtConfig(1, ("QR-1", 50));
        var jugadorClient = _factory.CreateClientAs(jugador);
        var intrusoClient = _factory.CreateClientAs(intruso);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await jugadorClient.PostAsync($"/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/partidas/{partidaId}/inicio", null)).StatusCode);

        var resp = await intrusoClient.PostAsJsonAsync($"/partidas/{partidaId}/etapa-actual/tesoro",
            new ValidarTesoroRequest(Tesoro("QR-1")));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Finalize_with_open_stage_returns_409()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = BuildBdtConfig(1, ("QR-1", 50));
        var jugadorClient = _factory.CreateClientAs(jugador);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await jugadorClient.PostAsync($"/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/partidas/{partidaId}/inicio", null)).StatusCode);

        var fin = await _client.PostAsync($"/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.Conflict, fin.StatusCode);
    }
}
```

- [ ] **Step 3: Run test to verify it fails, then implement nothing new**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests" --filter "FullyQualifiedName~BdtRuntimeEndpointsTests"`
Expected: tras añadir el stub `IQrDecoder` (Step 1), estos tests ejercen el pipeline real ya implementado en T1–T15 → PASS (5/5). Si algo falla, es un bug en una tarea previa: depurar ahí, no parchear el test.

- [ ] **Step 4: Run full suite**

Run: `dotnet test "services/operaciones-sesion"`
Expected: TODO verde (UnitTests + IntegrationTests + ContractTests, Trivia 3c + BDT 3d).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/OperacionesSesionWebFactory.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/BdtRuntimeEndpointsTests.cs
git commit -m "SP-3d T16: contract tests end-to-end de runtime BDT (gana/auto-avance/skip/403/409)"
```

---

### Task 17: Contratos HTTP/evento + traceability

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md`
- Modify: `contracts/events/operaciones-sesion-events.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

**Interfaces:** documentación; sin código.

> CARVE-OUT GIT (decisión del usuario, igual que SP-3c T16): `docs/04-sdd/traceability-matrix.md` tiene cambios sin commitear que el usuario reserva para un squash propio. ESCRIBIR la fila SP-3d en traceability-matrix.md pero **NO** stagearla/commitearla; el commit de esta tarea lleva SOLO los 2 archivos de `contracts/`. Confirmar con el usuario al ejecutar T17 si la política sigue vigente.

- [ ] **Step 1: HTTP contract**

En `contracts/http/operaciones-sesion-api.md`, añadir al Endpoint Registry (verbatim):
```
| Validar tesoro | POST | `/operaciones-sesion/partidas/{partidaId}/etapa-actual/tesoro` | Participante | 200 + ValidacionTesoroResponse | 401 sin identidad · 403 no inscrito · 404 sesión no existe · 409 no iniciada / juego no BDT / sin etapa activa |
| Avanzar/cerrar etapa | POST | `/operaciones-sesion/partidas/{partidaId}/etapa-actual/avance` | Operador | 200 + AvanceEtapaResponse | 404 · 409 no iniciada / juego no BDT / sin etapa activa |
| Etapa actual | GET | `/operaciones-sesion/partidas/{partidaId}/etapa-actual` | Operador/Participante | 200 + EtapaActualDto | 404 sesión no existe · 409 sin etapa activa |
```
A la lista de DTOs:
```
- `ValidacionTesoroResponse { partidaId, etapaId, resultado, gano, cerroEtapa, puntaje? }` (resultado ∈ {Valido, Invalido, NoLegible, NoCorrespondeEtapaActiva})
- `AvanceEtapaResponse { partidaId, etapaCerradaOrden, etapaActivadaOrden?, sinMasEtapas }`
- `EtapaActualDto { partidaId, juegoId, etapaId, orden, areaBusqueda, tiempoLimiteSegundos, fechaActivacion }` (participant-safe; nunca `codigoQREsperado`)
```
Nota: request body de validar tesoro es `{ imagenBase64 }`; `participanteId` del JWT `sub`. El backend decodifica la imagen (autoridad backend, RF-29).

- [ ] **Step 2: Events contract**

En `contracts/events/operaciones-sesion-events.md`, registrar (payload + trigger):
```
| TesoroQRValidado (SP-3d) | Cada intento de tesoro registrado | Registered | { partidaId, sesionPartidaId, juegoId, etapaId, participanteId, resultado, instante } |
| EtapaBDTGanada (SP-3d) | Validación correcta dentro de ventana | Registered | { partidaId, sesionPartidaId, juegoId, etapaId, participanteId, puntaje, tiempoResolucionMs } |
| EtapaBDTCerrada (SP-3d) | Cierre de etapa (ganador / tiempo / avance operador) | Registered | { partidaId, sesionPartidaId, juegoId, etapaId, motivo, fechaCierre, ganadorParticipanteId? } |
| EtapaBDTActivada (SP-3d) | Se activa una etapa: inicio del juego BDT, avance del operador, o auto-avance al cerrarse la anterior | Registered | { partidaId, sesionPartidaId, juegoId, etapaId, orden, tiempoLimiteSegundos, fechaActivacion } |
```
Nota: `motivo` ∈ {`Ganador`, `Tiempo`, `AvanceOperador`} (valores `ToString()` del enum `MotivoCierreEtapa`); `resultado` ∈ {`Valido`,`Invalido`,`NoLegible`,`NoCorrespondeEtapaActiva`}. Emitidos vía `NoOpSesionEventsPublisher`; `RankingBDTActualizado` queda en Puntuaciones (SP-4).

- [ ] **Step 3: Traceability (escribir, NO commitear)**

Añadir fila a `docs/04-sdd/traceability-matrix.md`:
```
| Runtime BDT Individual (SP-3d) | Validar tesoros QR en vivo de un JuegoBDT activo (decode backend vs CodigoQREsperado; 1ra validación correcta o timeout cierra la etapa; ventana de tiempo), registro de cada intento (TesoroQR), avance secuencial automático de etapas, agotar→FinalizarJuegoActual; snapshot de etapas al publicar; eventos por puerto No-Op | Operaciones de Sesión | Partidas (snapshot de etapas vía HTTP, solo lectura); Puntuaciones consume eventos en SP-4 | docs/superpowers/specs/2026-06-28-sp3d-runtime-bdt-design.md · docs/superpowers/plans/2026-06-28-sp3d-runtime-bdt.md | contracts/http/operaciones-sesion-api.md · contracts/events/operaciones-sesion-events.md | Implemented — suite verde. **Diferido:** Equipo BDT→slice-E, pistas + geolocalización + barrido automático de timeout + SignalR→SP-3f, scoring/ranking real→SP-4, reconexión→SP-3e. |
```

- [ ] **Step 4: Run the FULL suite**

Run: `dotnet test "services/operaciones-sesion"`
Expected: PASS (docs-only, confirma que nada se rompió).

- [ ] **Step 5: Commit (solo los 2 contratos)**

```bash
git add contracts/http/operaciones-sesion-api.md contracts/events/operaciones-sesion-events.md
git commit -m "SP-3d T17: contratos HTTP/evento de runtime BDT (fila traceability SP-3d escrita, sin commitear)"
```
> traceability-matrix.md queda modificado+unstaged, uniéndose al squash pendiente del usuario.

> === ALL 17 SP-3d TASKS COMPLETE. Next: final whole-branch review (opus) sobre el rango SP-3d, luego finishing-a-development-branch. ===

---

## Self-Review (autor del plan)

**1. Cobertura del spec (§ por §):**
- §1 alcance Individual + diferimientos → respetado (sin Equipo/pistas/geoloc/SignalR/scoring).
- §2 decisiones (scope, IQrDecoder+ZXing, auto-avance RF-32, modelado aditivo, endpoint operador) → T1/T4/T14 (puerto+impl), T3/T4 (aditivo), T15 (endpoint avance).
- §3 dominio (enums, EtapaSnapshot, TesoroQR, IQrDecoder, JuegoResumen, SesionPartida, excepciones, invariantes) → T1/T2/T3/T4.
- §4 Application (config snapshot, comandos/queries/DTOs, eventos, orden de emisión, activación BDT) → T5/T6/T7/T8/T9/T10/T11.
- §5 Infrastructure (mapeos, migración, Include, ZXingQrDecoder, DI) → T12/T13/T14.
- §6 Api (3 endpoints + middleware) → T15.
- §7 testing (dominio/handler/controller/integration/contract + no-leak) → T2/T3/T4/T8/T9/T10/T12/T15/T16.
- §8 doctrina/límites/contratos → respetado por construcción + T17.
- §9 watch-items (concurrencia 3f, git hygiene, NoCorrespondeEtapaActiva) → Global Constraints + ClasificarQr (T4).
- §10 diferimientos → fila traceability T17.

**2. Placeholder scan:** sin TBD/TODO; cada step de código lleva código real; tests con asserts reales. Las referencias a helpers de test reutilizados de 3c (`FakeSender`, `FakeSesionPartidaRepository`, `FakeTimeProvider`, `MiddlewareTestHarness`, `BdtBuilder`) están señaladas con su origen; `BdtBuilder` es nuevo y su contrato está descrito en T8.

**3. Consistencia de tipos:** `ResultadoRegistroTesoro` (11 campos) usado en T1/T4/T8; `ResultadoAvanceEtapa` (9 campos) en T1/T4/T9; nombres de eventos y métodos del publisher idénticos en T5/T8/T9/T11; `JuegoResumen` ctor BDT de 5 args (con `AreaBusqueda`, fijado en T7) usado consistentemente en T3→ajuste-T7/T6/T12; `EtapaActualDto`/`ValidacionTesoroResponse`/`AvanceEtapaResponse` consistentes T7/T10/T15/T16.

**Riesgo cruzado registrado (T7):** `AreaBusqueda` se introduce en `JuegoResumen` recién en T7 (ensanchando el ctor BDT de T3 de 4→5 args). El ejecutor de T7 DEBE actualizar los call-sites de T3/T4/T6 y sus tests, y correr la suite completa. Alternativa si molesta: introducir `AreaBusqueda` ya en T3 (ctor de 5 args desde el inicio) — el ejecutor puede adelantarlo si lo prefiere; el resultado final es idéntico.

**Nota de orden T12→T13:** el round-trip de T12 vía repo real queda RED hasta el Include de T13 (igual que 3c T12→T13); el test de T12 usa Include explícito para aislar el mapeo.
