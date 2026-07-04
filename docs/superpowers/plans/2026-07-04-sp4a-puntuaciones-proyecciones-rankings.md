# SP-4a — Puntuaciones: consumidor real, proyecciones y rankings nativos — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reemplazar el consumidor de humo de Puntuaciones por el consumidor real de proyecciones: 7 eventos de Operaciones de Sesión se materializan en `umbral_puntuaciones` y dos endpoints HTTP exponen el ranking nativo por juego y el marcador propio.

**Architecture:** Modelo de lectura (enfoque A del spec): upserts idempotentes por evento (dedup por `eventId` en la misma transacción) sobre 4 tablas de proyección; el ranking se calcula al leer (ORDER BY puntos DESC, tiempo ASC). El worker RabbitMQ despacha comandos MediatR; toda la lógica vive en Application/Domain.

**Tech Stack:** .NET 8, MediatR 12.2.0, EF Core 8.0.7 (Npgsql / InMemory), RabbitMQ.Client 6.8.1, xUnit.

**Spec:** `docs/superpowers/specs/2026-07-04-sp4a-puntuaciones-proyecciones-rankings-design.md`

## Global Constraints

- Servicio: `services/puntuaciones` (namespace raíz `Umbral.Puntuaciones`, puerto local 5030, DB `umbral_puntuaciones`, conn-string key `PuntuacionesDatabase` — ADR-0009).
- Estructura graduada obligatoria: controllers en `Api/Controllers` heredando `ControllerBase`, despacho por MediatR, sin lógica de negocio, **cada controller con unit tests**; `Application/` solo con las carpetas mandadas (`Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `DTOs/`, `Handlers/Commands/`, `Handlers/Queries/`, `Exceptions/`); interfaces de repositorio en `Domain/`; implementaciones EF en `Infrastructure/Persistence/`.
- Doctrina de ranking: Trivia y BDT ordenan por **puntos acumulados DESC, tiempo acumulado ASC**; `unidadesGanadas` (etapas/preguntas ganadas) es **solo informativo**, nunca clave de orden.
- Identidad dual slice-E: `CompetidorId = equipoId ?? participanteId`; `equipoId == null` ⇔ modalidad Individual.
- Dedup obligatorio por `eventId` (contrato de transporte SP-3i); consumo best-effort, ack-siempre, sin poison-loop (ADR-0012).
- Exchange `umbral.operaciones-sesion` (topic, durable); envelope camelCase `{eventId, eventType, version, occurredAt, payload}`.
- Cola nueva `puntuaciones.operaciones-sesion.proyecciones` (durable) con 7 bindings; la cola de humo `puntuaciones.operaciones-sesion.all` se elimina.
- TDD por tarea; commits frecuentes; cada tarea deja `dotnet test services/puntuaciones/Umbral.Puntuaciones.sln` verde.
- Todos los comandos `dotnet` se corren desde `services/puntuaciones/` salvo indicación contraria.

---

### Task 0: Rama de trabajo

**Files:** ninguno (git).

- [ ] **Step 1: Crear la rama desde develop**

```bash
git -C . checkout develop && git pull --ff-only 2>/dev/null; git checkout -b feature/sp-4a-puntuaciones
```

Expected: rama `feature/sp-4a-puntuaciones` activa, working tree limpio (`git status`).

---

### Task 1: Dominio — enums, entidades de proyección e invariantes

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Enums/Modalidad.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Enums/TipoJuego.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Enums/EstadoPartidaProyectada.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Enums/TipoCompetidor.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Exceptions/PuntuacionInvalidaException.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Entities/PartidaProyectada.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Entities/JuegoProyectado.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Entities/Marcador.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Entities/EventoProcesado.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Domain/MarcadorTests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Domain/PartidaProyectadaTests.cs`

**Interfaces:**
- Consumes: nada (primer task de código).
- Produces: `PartidaProyectada` (`DesdePublicacion(Guid, Guid, Modalidad)`, `Stub(Guid, Guid)`, `RegistrarPublicacion(Modalidad)`, `MarcarIniciada(DateTime)`, `MarcarCancelada(DateTime)`, `MarcarTerminada(DateTime)`); `JuegoProyectado.Desde(Guid, Guid, int, TipoJuego)`; `Marcador.Nuevo(Guid, Guid, Guid, TipoCompetidor)` + `Acreditar(int, long)`; `EventoProcesado.Registrar(Guid, string, DateTime, DateTime)`; enums `Modalidad`, `TipoJuego`, `EstadoPartidaProyectada`, `TipoCompetidor`.

- [ ] **Step 1: Escribir los tests de dominio (fallan por compilación)**

`tests/Umbral.Puntuaciones.UnitTests/Domain/MarcadorTests.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.Domain.Exceptions;

namespace Umbral.Puntuaciones.UnitTests.Domain;

public class MarcadorTests
{
    private static Marcador NuevoMarcador() =>
        Marcador.Nuevo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoCompetidor.Participante);

    [Fact]
    public void Nuevo_inicia_en_cero()
    {
        var m = NuevoMarcador();

        Assert.Equal(0, m.PuntosAcumulados);
        Assert.Equal(0, m.TiempoAcumuladoMs);
        Assert.Equal(0, m.UnidadesGanadas);
    }

    [Fact]
    public void Acreditar_acumula_puntos_tiempo_y_unidades()
    {
        var m = NuevoMarcador();

        m.Acreditar(10, 1500);
        m.Acreditar(5, 500);

        Assert.Equal(15, m.PuntosAcumulados);
        Assert.Equal(2000, m.TiempoAcumuladoMs);
        Assert.Equal(2, m.UnidadesGanadas);
    }

    [Fact]
    public void Acreditar_puntos_negativos_lanza()
    {
        var m = NuevoMarcador();

        Assert.Throws<PuntuacionInvalidaException>(() => m.Acreditar(-1, 100));
    }

    [Fact]
    public void Acreditar_tiempo_negativo_lanza()
    {
        var m = NuevoMarcador();

        Assert.Throws<PuntuacionInvalidaException>(() => m.Acreditar(1, -100));
    }
}
```

`tests/Umbral.Puntuaciones.UnitTests/Domain/PartidaProyectadaTests.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.UnitTests.Domain;

public class PartidaProyectadaTests
{
    [Fact]
    public void DesdePublicacion_queda_en_lobby_con_modalidad()
    {
        var p = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Equipo);

        Assert.Equal(EstadoPartidaProyectada.Lobby, p.Estado);
        Assert.Equal(Modalidad.Equipo, p.Modalidad);
        Assert.Null(p.FechaInicio);
        Assert.Null(p.FechaFin);
    }

    [Fact]
    public void Transiciones_normales_avanzan_estado_y_fechas()
    {
        var p = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Individual);
        var inicio = new DateTime(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc);
        var fin = inicio.AddMinutes(30);

        p.MarcarIniciada(inicio);
        p.MarcarTerminada(fin);

        Assert.Equal(EstadoPartidaProyectada.Terminada, p.Estado);
        Assert.Equal(inicio, p.FechaInicio);
        Assert.Equal(fin, p.FechaFin);
    }

    [Fact]
    public void El_estado_nunca_retrocede_ante_eventos_desordenados()
    {
        // PartidaFinalizada llegó primero (stub), luego llegan Iniciada y la publicación.
        var p = PartidaProyectada.Stub(Guid.NewGuid(), Guid.NewGuid());
        var fin = new DateTime(2026, 7, 4, 11, 0, 0, DateTimeKind.Utc);

        p.MarcarTerminada(fin);
        p.MarcarIniciada(fin.AddMinutes(-30));
        p.RegistrarPublicacion(Modalidad.Individual);

        Assert.Equal(EstadoPartidaProyectada.Terminada, p.Estado);
        Assert.Equal(Modalidad.Individual, p.Modalidad);
        Assert.Equal(fin, p.FechaFin);
        Assert.Equal(fin.AddMinutes(-30), p.FechaInicio);
    }

    [Fact]
    public void Stub_no_tiene_modalidad_hasta_registrar_publicacion()
    {
        var p = PartidaProyectada.Stub(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(p.Modalidad);
        Assert.Equal(EstadoPartidaProyectada.Lobby, p.Estado);
    }

    [Fact]
    public void Cancelada_prevalece_sobre_iniciada_tardia()
    {
        var p = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Individual);
        var t = new DateTime(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc);

        p.MarcarCancelada(t);
        p.MarcarIniciada(t.AddMinutes(-1));

        Assert.Equal(EstadoPartidaProyectada.Cancelada, p.Estado);
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test services/puntuaciones/Umbral.Puntuaciones.sln --filter "FullyQualifiedName~UnitTests.Domain"` (desde la raíz del repo)
Expected: FAIL de compilación (`Marcador`, `PartidaProyectada` no existen).

- [ ] **Step 3: Implementar enums, excepción y entidades**

`src/Umbral.Puntuaciones.Domain/Enums/Modalidad.cs`:

```csharp
namespace Umbral.Puntuaciones.Domain.Enums;

public enum Modalidad
{
    Individual,
    Equipo
}
```

`src/Umbral.Puntuaciones.Domain/Enums/TipoJuego.cs`:

```csharp
namespace Umbral.Puntuaciones.Domain.Enums;

public enum TipoJuego
{
    Trivia,
    BusquedaDelTesoro
}
```

`src/Umbral.Puntuaciones.Domain/Enums/EstadoPartidaProyectada.cs`:

```csharp
namespace Umbral.Puntuaciones.Domain.Enums;

// Espejo de EstadoPartida de la doctrina, visto desde la proyección.
public enum EstadoPartidaProyectada
{
    Lobby,
    Iniciada,
    Cancelada,
    Terminada
}
```

`src/Umbral.Puntuaciones.Domain/Enums/TipoCompetidor.cs`:

```csharp
namespace Umbral.Puntuaciones.Domain.Enums;

public enum TipoCompetidor
{
    Participante,
    Equipo
}
```

`src/Umbral.Puntuaciones.Domain/Exceptions/PuntuacionInvalidaException.cs`:

```csharp
namespace Umbral.Puntuaciones.Domain.Exceptions;

public sealed class PuntuacionInvalidaException : Exception
{
    public PuntuacionInvalidaException(string message) : base(message)
    {
    }
}
```

`src/Umbral.Puntuaciones.Domain/Entities/PartidaProyectada.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Domain.Entities;

// Proyección del ciclo de vida de una partida (fuente: eventos de Operaciones de Sesión).
// Tolerante al desorden de llegada: el estado nunca retrocede y las fechas no se pisan.
public sealed class PartidaProyectada
{
    private PartidaProyectada(Guid partidaId, Guid sesionPartidaId, Modalidad? modalidad, EstadoPartidaProyectada estado)
    {
        PartidaId = partidaId;
        SesionPartidaId = sesionPartidaId;
        Modalidad = modalidad;
        Estado = estado;
    }

    public Guid PartidaId { get; private set; }
    public Guid SesionPartidaId { get; private set; }
    public Modalidad? Modalidad { get; private set; }
    public EstadoPartidaProyectada Estado { get; private set; }
    public DateTime? FechaInicio { get; private set; }
    public DateTime? FechaFin { get; private set; }

    public static PartidaProyectada DesdePublicacion(Guid partidaId, Guid sesionPartidaId, Modalidad modalidad)
        => new(partidaId, sesionPartidaId, modalidad, EstadoPartidaProyectada.Lobby);

    // Un evento posterior llegó antes que la publicación (best-effort, sin garantía de orden).
    public static PartidaProyectada Stub(Guid partidaId, Guid sesionPartidaId)
        => new(partidaId, sesionPartidaId, null, EstadoPartidaProyectada.Lobby);

    public void RegistrarPublicacion(Modalidad modalidad) => Modalidad ??= modalidad;

    public void MarcarIniciada(DateTime fechaInicio)
    {
        AvanzarEstado(EstadoPartidaProyectada.Iniciada);
        FechaInicio ??= fechaInicio;
    }

    public void MarcarCancelada(DateTime fechaCancelacion)
    {
        AvanzarEstado(EstadoPartidaProyectada.Cancelada);
        FechaFin ??= fechaCancelacion;
    }

    public void MarcarTerminada(DateTime fechaFin)
    {
        AvanzarEstado(EstadoPartidaProyectada.Terminada);
        FechaFin ??= fechaFin;
    }

    private void AvanzarEstado(EstadoPartidaProyectada nuevo)
    {
        if (Rango(nuevo) > Rango(Estado))
        {
            Estado = nuevo;
        }
    }

    private static int Rango(EstadoPartidaProyectada estado) => estado switch
    {
        EstadoPartidaProyectada.Lobby => 0,
        EstadoPartidaProyectada.Iniciada => 1,
        _ => 2 // Cancelada y Terminada son terminales.
    };
}
```

`src/Umbral.Puntuaciones.Domain/Entities/JuegoProyectado.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Domain.Entities;

// Registro informativo de un juego activado (fuente: JuegoActivado).
public sealed class JuegoProyectado
{
    private JuegoProyectado(Guid juegoId, Guid partidaId, int orden, TipoJuego tipoJuego)
    {
        JuegoId = juegoId;
        PartidaId = partidaId;
        Orden = orden;
        TipoJuego = tipoJuego;
    }

    public Guid JuegoId { get; private set; }
    public Guid PartidaId { get; private set; }
    public int Orden { get; private set; }
    public TipoJuego TipoJuego { get; private set; }

    public static JuegoProyectado Desde(Guid juegoId, Guid partidaId, int orden, TipoJuego tipoJuego)
        => new(juegoId, partidaId, orden, tipoJuego);
}
```

`src/Umbral.Puntuaciones.Domain/Entities/Marcador.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.Domain.Exceptions;

namespace Umbral.Puntuaciones.Domain.Entities;

// Acumulado de un competidor (participante o equipo) en un juego.
// La acumulación es conmutativa: el orden de llegada de eventos no altera el total.
public sealed class Marcador
{
    private Marcador(Guid juegoId, Guid competidorId, Guid partidaId, TipoCompetidor tipoCompetidor)
    {
        JuegoId = juegoId;
        CompetidorId = competidorId;
        PartidaId = partidaId;
        TipoCompetidor = tipoCompetidor;
    }

    public Guid JuegoId { get; private set; }
    public Guid CompetidorId { get; private set; }
    public Guid PartidaId { get; private set; }
    public TipoCompetidor TipoCompetidor { get; private set; }
    public int PuntosAcumulados { get; private set; }
    public long TiempoAcumuladoMs { get; private set; }
    public int UnidadesGanadas { get; private set; }

    public static Marcador Nuevo(Guid juegoId, Guid competidorId, Guid partidaId, TipoCompetidor tipoCompetidor)
        => new(juegoId, competidorId, partidaId, tipoCompetidor);

    public void Acreditar(int puntos, long tiempoMs)
    {
        if (puntos < 0)
        {
            throw new PuntuacionInvalidaException("El puntaje acreditado no puede ser negativo.");
        }
        if (tiempoMs < 0)
        {
            throw new PuntuacionInvalidaException("El tiempo acreditado no puede ser negativo.");
        }

        PuntosAcumulados += puntos;
        TiempoAcumuladoMs += tiempoMs;
        UnidadesGanadas += 1;
    }
}
```

`src/Umbral.Puntuaciones.Domain/Entities/EventoProcesado.cs`:

```csharp
namespace Umbral.Puntuaciones.Domain.Entities;

// Dedup por eventId exigido por el contrato de transporte (SP-3i).
public sealed class EventoProcesado
{
    private EventoProcesado(Guid eventId, string eventType, DateTime occurredAt, DateTime procesadoAt)
    {
        EventId = eventId;
        EventType = eventType;
        OccurredAt = occurredAt;
        ProcesadoAt = procesadoAt;
    }

    public Guid EventId { get; private set; }
    public string EventType { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime ProcesadoAt { get; private set; }

    public static EventoProcesado Registrar(Guid eventId, string eventType, DateTime occurredAt, DateTime procesadoAt)
        => new(eventId, eventType, occurredAt, procesadoAt);
}
```

- [ ] **Step 4: Correr los tests y verificar que pasan**

Run: `dotnet test services/puntuaciones/Umbral.Puntuaciones.sln --filter "FullyQualifiedName~UnitTests.Domain"`
Expected: PASS (9 tests).

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): entidades de proyeccion SP-4a con invariantes de dominio"
```

---

### Task 2: Persistencia — repositorio, UnitOfWork, DbContext y migración

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/IProyeccionesRepository.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/IPuntuacionesUnitOfWork.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/PuntuacionesDbContext.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/ProyeccionesRepository.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/PuntuacionesUnitOfWork.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/PuntuacionesDbContextDesignTimeFactory.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/DependencyInjection.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Umbral.Puntuaciones.Infrastructure.csproj`
- Create: migración EF `SP4aProyecciones` en `Infrastructure/Persistence/Migrations/`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/ProyeccionesRepositoryTests.cs`

**Interfaces:**
- Consumes: entidades y enums de Task 1.
- Produces: `IProyeccionesRepository` con `Task<bool> EventoYaProcesadoAsync(Guid, CancellationToken)`, `void RegistrarEventoProcesado(EventoProcesado)`, `Task<PartidaProyectada?> GetPartidaAsync(Guid, CancellationToken)`, `void AddPartida(PartidaProyectada)`, `Task<JuegoProyectado?> GetJuegoAsync(Guid, CancellationToken)`, `void AddJuego(JuegoProyectado)`, `Task<Marcador?> GetMarcadorAsync(Guid juegoId, Guid competidorId, CancellationToken)`, `void AddMarcador(Marcador)`, `Task<IReadOnlyList<Marcador>> GetMarcadoresDeJuegoAsync(Guid juegoId, CancellationToken)`; `IPuntuacionesUnitOfWork.SaveChangesAsync(CancellationToken)`. Ambos registrados scoped en DI.

- [ ] **Step 1: Escribir el test de integración (falla por compilación)**

`tests/Umbral.Puntuaciones.IntegrationTests/ProyeccionesRepositoryTests.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class ProyeccionesRepositoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProyeccionesRepositoryTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Repositorio_persiste_y_recupera_proyecciones()
    {
        var juegoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var competidorId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProyeccionesRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IPuntuacionesUnitOfWork>();

            repo.AddPartida(PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual));
            repo.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 1, TipoJuego.Trivia));
            var marcador = Marcador.Nuevo(juegoId, competidorId, partidaId, TipoCompetidor.Participante);
            marcador.Acreditar(10, 1200);
            repo.AddMarcador(marcador);
            repo.RegistrarEventoProcesado(EventoProcesado.Registrar(eventId, "PuntajeTriviaIncrementado", DateTime.UtcNow, DateTime.UtcNow));
            await uow.SaveChangesAsync(CancellationToken.None);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProyeccionesRepository>();

            var partida = await repo.GetPartidaAsync(partidaId, CancellationToken.None);
            var juego = await repo.GetJuegoAsync(juegoId, CancellationToken.None);
            var marcador = await repo.GetMarcadorAsync(juegoId, competidorId, CancellationToken.None);
            var lista = await repo.GetMarcadoresDeJuegoAsync(juegoId, CancellationToken.None);

            Assert.NotNull(partida);
            Assert.Equal(TipoJuego.Trivia, juego!.TipoJuego);
            Assert.Equal(10, marcador!.PuntosAcumulados);
            Assert.Single(lista);
            Assert.True(await repo.EventoYaProcesadoAsync(eventId, CancellationToken.None));
            Assert.False(await repo.EventoYaProcesadoAsync(Guid.NewGuid(), CancellationToken.None));
        }
    }
}
```

- [ ] **Step 2: Correr el test para verificar que falla**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj`
Expected: FAIL de compilación (`IProyeccionesRepository` no existe).

- [ ] **Step 3: Implementar interfaces, DbContext, repositorio y DI**

`src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/IProyeccionesRepository.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Domain.Abstractions.Persistence;

public interface IProyeccionesRepository
{
    Task<bool> EventoYaProcesadoAsync(Guid eventId, CancellationToken cancellationToken);
    void RegistrarEventoProcesado(EventoProcesado evento);
    Task<PartidaProyectada?> GetPartidaAsync(Guid partidaId, CancellationToken cancellationToken);
    void AddPartida(PartidaProyectada partida);
    Task<JuegoProyectado?> GetJuegoAsync(Guid juegoId, CancellationToken cancellationToken);
    void AddJuego(JuegoProyectado juego);
    Task<Marcador?> GetMarcadorAsync(Guid juegoId, Guid competidorId, CancellationToken cancellationToken);
    void AddMarcador(Marcador marcador);
    Task<IReadOnlyList<Marcador>> GetMarcadoresDeJuegoAsync(Guid juegoId, CancellationToken cancellationToken);
}
```

`src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/IPuntuacionesUnitOfWork.cs`:

```csharp
namespace Umbral.Puntuaciones.Domain.Abstractions.Persistence;

public interface IPuntuacionesUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
```

`src/Umbral.Puntuaciones.Infrastructure/Persistence/PuntuacionesDbContext.cs` (reemplazo completo):

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Infrastructure.Persistence;

public sealed class PuntuacionesDbContext : DbContext
{
    public PuntuacionesDbContext(DbContextOptions<PuntuacionesDbContext> options) : base(options)
    {
    }

    public DbSet<PartidaProyectada> Partidas => Set<PartidaProyectada>();
    public DbSet<JuegoProyectado> Juegos => Set<JuegoProyectado>();
    public DbSet<Marcador> Marcadores => Set<Marcador>();
    public DbSet<EventoProcesado> EventosProcesados => Set<EventoProcesado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PartidaProyectada>(entity =>
        {
            entity.ToTable("partidas_proyectadas");
            entity.HasKey(x => x.PartidaId);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").ValueGeneratedNever();
            entity.Property(x => x.SesionPartidaId).HasColumnName("sesionpartidaid").IsRequired();
            entity.Property(x => x.Modalidad).HasColumnName("modalidad");
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.FechaInicio).HasColumnName("fechainicio");
            entity.Property(x => x.FechaFin).HasColumnName("fechafin");
        });

        modelBuilder.Entity<JuegoProyectado>(entity =>
        {
            entity.ToTable("juegos_proyectados");
            entity.HasKey(x => x.JuegoId);
            entity.Property(x => x.JuegoId).HasColumnName("juegoid").ValueGeneratedNever();
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.TipoJuego).HasColumnName("tipojuego").IsRequired();
            entity.HasIndex(x => x.PartidaId).HasDatabaseName("ix_juegos_proyectados_partidaid");
        });

        modelBuilder.Entity<Marcador>(entity =>
        {
            entity.ToTable("marcadores");
            entity.HasKey(x => new { x.JuegoId, x.CompetidorId });
            entity.Property(x => x.JuegoId).HasColumnName("juegoid");
            entity.Property(x => x.CompetidorId).HasColumnName("competidorid");
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.TipoCompetidor).HasColumnName("tipocompetidor").IsRequired();
            entity.Property(x => x.PuntosAcumulados).HasColumnName("puntosacumulados").IsRequired();
            entity.Property(x => x.TiempoAcumuladoMs).HasColumnName("tiempoacumuladoms").IsRequired();
            entity.Property(x => x.UnidadesGanadas).HasColumnName("unidadesganadas").IsRequired();
            entity.HasIndex(x => x.JuegoId).HasDatabaseName("ix_marcadores_juegoid");
        });

        modelBuilder.Entity<EventoProcesado>(entity =>
        {
            entity.ToTable("eventos_procesados");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).HasColumnName("eventid").ValueGeneratedNever();
            entity.Property(x => x.EventType).HasColumnName("eventtype").IsRequired();
            entity.Property(x => x.OccurredAt).HasColumnName("occurredat").IsRequired();
            entity.Property(x => x.ProcesadoAt).HasColumnName("procesadoat").IsRequired();
        });
    }
}
```

`src/Umbral.Puntuaciones.Infrastructure/Persistence/ProyeccionesRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Infrastructure.Persistence;

public sealed class ProyeccionesRepository : IProyeccionesRepository
{
    private readonly PuntuacionesDbContext _db;

    public ProyeccionesRepository(PuntuacionesDbContext db) => _db = db;

    public Task<bool> EventoYaProcesadoAsync(Guid eventId, CancellationToken cancellationToken)
        => _db.EventosProcesados.AsNoTracking().AnyAsync(e => e.EventId == eventId, cancellationToken);

    public void RegistrarEventoProcesado(EventoProcesado evento) => _db.EventosProcesados.Add(evento);

    public Task<PartidaProyectada?> GetPartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => _db.Partidas.FirstOrDefaultAsync(p => p.PartidaId == partidaId, cancellationToken);

    public void AddPartida(PartidaProyectada partida) => _db.Partidas.Add(partida);

    public Task<JuegoProyectado?> GetJuegoAsync(Guid juegoId, CancellationToken cancellationToken)
        => _db.Juegos.FirstOrDefaultAsync(j => j.JuegoId == juegoId, cancellationToken);

    public void AddJuego(JuegoProyectado juego) => _db.Juegos.Add(juego);

    public Task<Marcador?> GetMarcadorAsync(Guid juegoId, Guid competidorId, CancellationToken cancellationToken)
        => _db.Marcadores.FirstOrDefaultAsync(
            m => m.JuegoId == juegoId && m.CompetidorId == competidorId, cancellationToken);

    public void AddMarcador(Marcador marcador) => _db.Marcadores.Add(marcador);

    public async Task<IReadOnlyList<Marcador>> GetMarcadoresDeJuegoAsync(Guid juegoId, CancellationToken cancellationToken)
        => await _db.Marcadores.AsNoTracking()
            .Where(m => m.JuegoId == juegoId)
            .ToListAsync(cancellationToken);
}
```

`src/Umbral.Puntuaciones.Infrastructure/Persistence/PuntuacionesUnitOfWork.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Infrastructure.Persistence;

public sealed class PuntuacionesUnitOfWork : IPuntuacionesUnitOfWork
{
    private readonly PuntuacionesDbContext _db;

    public PuntuacionesUnitOfWork(PuntuacionesDbContext db) => _db = db;

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
```

`src/Umbral.Puntuaciones.Infrastructure/Persistence/PuntuacionesDbContextDesignTimeFactory.cs` (patrón exacto de Operaciones):

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbral.Puntuaciones.Infrastructure.Persistence;

public sealed class PuntuacionesDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PuntuacionesDbContext>
{
    public PuntuacionesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PuntuacionesDbContext>()
            .UseNpgsql("Host=localhost;Port=55432;Database=umbral_puntuaciones;Username=umbral;Password=16102005")
            .Options;
        return new PuntuacionesDbContext(options);
    }
}
```

En `src/Umbral.Puntuaciones.Infrastructure/DependencyInjection.cs`, después del bloque `AddDbContext` y antes de `return services;`, añadir:

```csharp
        services.AddScoped<IProyeccionesRepository, ProyeccionesRepository>();
        services.AddScoped<IPuntuacionesUnitOfWork, PuntuacionesUnitOfWork>();
```

con el using nuevo `using Umbral.Puntuaciones.Domain.Abstractions.Persistence;`.

En `src/Umbral.Puntuaciones.Infrastructure/Umbral.Puntuaciones.Infrastructure.csproj`, añadir al ItemGroup de paquetes:

```xml
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.7">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
```

- [ ] **Step 4: Correr el test y verificar que pasa**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj`
Expected: PASS (3 tests: los 2 de Health + el nuevo). La factory usa InMemory (sin conn string en tests).

- [ ] **Step 5: Generar la migración EF**

Run (desde `services/puntuaciones/`):

```bash
dotnet ef migrations add SP4aProyecciones --project src/Umbral.Puntuaciones.Infrastructure --output-dir Persistence/Migrations
```

Expected: carpeta `Persistence/Migrations/` con `*_SP4aProyecciones.cs` creando las 4 tablas. No requiere DB viva (usa la DesignTimeFactory). Si `dotnet ef` no está instalado: `dotnet tool install --global dotnet-ef --version 8.*`.

- [ ] **Step 6: Verificar build + suite completa**

Run: `dotnet test services/puntuaciones/Umbral.Puntuaciones.sln`
Expected: PASS (todas las suites verdes).

- [ ] **Step 7: Commit**

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): persistencia de proyecciones (repositorio, UoW, migracion SP4a)"
```

---

### Task 3: Comandos de proyección de ciclo de vida (5) + fakes de test

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarPartidaPublicadaCommand.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarPartidaIniciadaCommand.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarJuegoActivadoCommand.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarPartidaCanceladaCommand.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarPartidaFinalizadaCommand.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarPartidaPublicadaCommandHandler.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarPartidaIniciadaCommandHandler.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarJuegoActivadoCommandHandler.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarPartidaCanceladaCommandHandler.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarPartidaFinalizadaCommandHandler.cs`
- Create: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/Fakes/FakeProyeccionesRepository.cs`
- Create: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/Fakes/FakePuntuacionesUnitOfWork.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ProyectarCicloDeVidaHandlersTests.cs`

**Interfaces:**
- Consumes: `IProyeccionesRepository`, `IPuntuacionesUnitOfWork`, entidades/enums de Task 1.
- Produces: los 5 records de comando (abajo, con sus firmas exactas) — Task 7 los construye desde el mapper; los fakes `FakeProyeccionesRepository` (listas públicas `Partidas`, `Juegos`, `Marcadores`, `EventosProcesados`) y `FakePuntuacionesUnitOfWork` (contador `Saves`) — Task 4 y 5 los reutilizan.

- [ ] **Step 1: Escribir fakes y tests (fallan por compilación)**

`tests/Umbral.Puntuaciones.UnitTests/Application/Fakes/FakeProyeccionesRepository.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.UnitTests.Application.Fakes;

public sealed class FakeProyeccionesRepository : IProyeccionesRepository
{
    public List<PartidaProyectada> Partidas { get; } = new();
    public List<JuegoProyectado> Juegos { get; } = new();
    public List<Marcador> Marcadores { get; } = new();
    public List<EventoProcesado> EventosProcesados { get; } = new();

    public Task<bool> EventoYaProcesadoAsync(Guid eventId, CancellationToken cancellationToken)
        => Task.FromResult(EventosProcesados.Any(e => e.EventId == eventId));

    public void RegistrarEventoProcesado(EventoProcesado evento) => EventosProcesados.Add(evento);

    public Task<PartidaProyectada?> GetPartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult(Partidas.FirstOrDefault(p => p.PartidaId == partidaId));

    public void AddPartida(PartidaProyectada partida) => Partidas.Add(partida);

    public Task<JuegoProyectado?> GetJuegoAsync(Guid juegoId, CancellationToken cancellationToken)
        => Task.FromResult(Juegos.FirstOrDefault(j => j.JuegoId == juegoId));

    public void AddJuego(JuegoProyectado juego) => Juegos.Add(juego);

    public Task<Marcador?> GetMarcadorAsync(Guid juegoId, Guid competidorId, CancellationToken cancellationToken)
        => Task.FromResult(Marcadores.FirstOrDefault(m => m.JuegoId == juegoId && m.CompetidorId == competidorId));

    public void AddMarcador(Marcador marcador) => Marcadores.Add(marcador);

    public Task<IReadOnlyList<Marcador>> GetMarcadoresDeJuegoAsync(Guid juegoId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Marcador>>(Marcadores.Where(m => m.JuegoId == juegoId).ToList());
}
```

`tests/Umbral.Puntuaciones.UnitTests/Application/Fakes/FakePuntuacionesUnitOfWork.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.UnitTests.Application.Fakes;

public sealed class FakePuntuacionesUnitOfWork : IPuntuacionesUnitOfWork
{
    public int Saves { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saves++;
        return Task.CompletedTask;
    }
}
```

`tests/Umbral.Puntuaciones.UnitTests/Application/ProyectarCicloDeVidaHandlersTests.cs`:

```csharp
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.Handlers.Commands;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ProyectarCicloDeVidaHandlersTests
{
    private readonly FakeProyeccionesRepository _repo = new();
    private readonly FakePuntuacionesUnitOfWork _uow = new();
    private static readonly DateTime Ahora = new(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PartidaPublicada_crea_la_proyeccion_en_lobby()
    {
        var cmd = new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Modalidad.Equipo);

        await new ProyectarPartidaPublicadaCommandHandler(_repo, _uow).Handle(cmd, CancellationToken.None);

        var partida = Assert.Single(_repo.Partidas);
        Assert.Equal(cmd.PartidaId, partida.PartidaId);
        Assert.Equal(Modalidad.Equipo, partida.Modalidad);
        Assert.Equal(EstadoPartidaProyectada.Lobby, partida.Estado);
        Assert.Single(_repo.EventosProcesados);
        Assert.Equal(1, _uow.Saves);
    }

    [Fact]
    public async Task Evento_duplicado_no_tiene_efecto()
    {
        var cmd = new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Modalidad.Individual);
        var handler = new ProyectarPartidaPublicadaCommandHandler(_repo, _uow);

        await handler.Handle(cmd, CancellationToken.None);
        await handler.Handle(cmd, CancellationToken.None);

        Assert.Single(_repo.Partidas);
        Assert.Single(_repo.EventosProcesados);
        Assert.Equal(1, _uow.Saves);
    }

    [Fact]
    public async Task PartidaIniciada_sin_publicacion_previa_crea_stub_iniciada()
    {
        var cmd = new ProyectarPartidaIniciadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Ahora);

        await new ProyectarPartidaIniciadaCommandHandler(_repo, _uow).Handle(cmd, CancellationToken.None);

        var partida = Assert.Single(_repo.Partidas);
        Assert.Equal(EstadoPartidaProyectada.Iniciada, partida.Estado);
        Assert.Null(partida.Modalidad);
        Assert.Equal(Ahora, partida.FechaInicio);
    }

    [Fact]
    public async Task Publicacion_tardia_completa_modalidad_sin_retroceder_estado()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        await new ProyectarPartidaFinalizadaCommandHandler(_repo, _uow).Handle(
            new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora), CancellationToken.None);

        await new ProyectarPartidaPublicadaCommandHandler(_repo, _uow).Handle(
            new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual), CancellationToken.None);

        var partida = Assert.Single(_repo.Partidas);
        Assert.Equal(EstadoPartidaProyectada.Terminada, partida.Estado);
        Assert.Equal(Modalidad.Individual, partida.Modalidad);
    }

    [Fact]
    public async Task PartidaCancelada_marca_cancelada_con_fecha_fin()
    {
        var cmd = new ProyectarPartidaCanceladaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Ahora);

        await new ProyectarPartidaCanceladaCommandHandler(_repo, _uow).Handle(cmd, CancellationToken.None);

        var partida = Assert.Single(_repo.Partidas);
        Assert.Equal(EstadoPartidaProyectada.Cancelada, partida.Estado);
        Assert.Equal(Ahora, partida.FechaFin);
    }

    [Fact]
    public async Task JuegoActivado_registra_el_juego_una_sola_vez()
    {
        var juegoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var handler = new ProyectarJuegoActivadoCommandHandler(_repo, _uow);

        await handler.Handle(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), juegoId, 1, TipoJuego.BusquedaDelTesoro), CancellationToken.None);
        await handler.Handle(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), juegoId, 1, TipoJuego.BusquedaDelTesoro), CancellationToken.None);

        var juego = Assert.Single(_repo.Juegos);
        Assert.Equal(TipoJuego.BusquedaDelTesoro, juego.TipoJuego);
        Assert.Equal(partidaId, juego.PartidaId);
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj --filter "FullyQualifiedName~ProyectarCicloDeVida"`
Expected: FAIL de compilación (comandos/handlers no existen).

- [ ] **Step 3: Implementar los 5 comandos y handlers**

Los 5 records de comando (un archivo cada uno bajo `Application/Commands/`, namespace `Umbral.Puntuaciones.Application.Commands`, usings `MediatR` y `Umbral.Puntuaciones.Domain.Enums` donde aplique):

```csharp
public sealed record ProyectarPartidaPublicadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, Modalidad Modalidad) : IRequest;

public sealed record ProyectarPartidaIniciadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, DateTime FechaInicio) : IRequest;

public sealed record ProyectarJuegoActivadoCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, int Orden, TipoJuego TipoJuego) : IRequest;

public sealed record ProyectarPartidaCanceladaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, DateTime FechaCancelacion) : IRequest;

public sealed record ProyectarPartidaFinalizadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, DateTime FechaFin) : IRequest;
```

`Application/Handlers/Commands/ProyectarPartidaPublicadaCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarPartidaPublicadaCommandHandler : IRequestHandler<ProyectarPartidaPublicadaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarPartidaPublicadaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarPartidaPublicadaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        var partida = await _repo.GetPartidaAsync(request.PartidaId, cancellationToken);
        if (partida is null)
        {
            _repo.AddPartida(PartidaProyectada.DesdePublicacion(request.PartidaId, request.SesionPartidaId, request.Modalidad));
        }
        else
        {
            partida.RegistrarPublicacion(request.Modalidad);
        }

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "PartidaPublicadaEnLobby", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
```

`ProyectarPartidaIniciadaCommandHandler.cs` (mismos usings y ctor de 2 dependencias; cuerpo del `Handle`):

```csharp
    public async Task Handle(ProyectarPartidaIniciadaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        var partida = await _repo.GetPartidaAsync(request.PartidaId, cancellationToken);
        if (partida is null)
        {
            partida = PartidaProyectada.Stub(request.PartidaId, request.SesionPartidaId);
            _repo.AddPartida(partida);
        }
        partida.MarcarIniciada(request.FechaInicio);

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "PartidaIniciada", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
```

`ProyectarPartidaCanceladaCommandHandler.cs`: idéntico al anterior sustituyendo `MarcarIniciada(request.FechaInicio)` por `MarcarCancelada(request.FechaCancelacion)` y el eventType por `"PartidaCancelada"`.

`ProyectarPartidaFinalizadaCommandHandler.cs`: idéntico sustituyendo por `MarcarTerminada(request.FechaFin)` y `"PartidaFinalizada"`.

`ProyectarJuegoActivadoCommandHandler.cs` (cuerpo del `Handle`):

```csharp
    public async Task Handle(ProyectarJuegoActivadoCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        var juego = await _repo.GetJuegoAsync(request.JuegoId, cancellationToken);
        if (juego is null)
        {
            _repo.AddJuego(JuegoProyectado.Desde(request.JuegoId, request.PartidaId, request.Orden, request.TipoJuego));
        }

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "JuegoActivado", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
```

- [ ] **Step 4: Correr los tests y verificar que pasan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj --filter "FullyQualifiedName~ProyectarCicloDeVida"`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): comandos de proyeccion de ciclo de vida con dedup por eventId"
```

---

### Task 4: Comandos de proyección de scoring (2) — acumulación e identidad dual

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarPuntajeTriviaCommand.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarEtapaBdtGanadaCommand.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarPuntajeTriviaCommandHandler.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarEtapaBdtGanadaCommandHandler.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ProyectarScoringHandlersTests.cs`

**Interfaces:**
- Consumes: fakes y patrón de Task 3; entidades de Task 1.
- Produces: `ProyectarPuntajeTriviaCommand(Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId, Guid ParticipanteId, int Puntaje, long TiempoRespuestaMs, Guid? EquipoId)` y `ProyectarEtapaBdtGanadaCommand(Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId, Guid ParticipanteId, int Puntaje, long TiempoResolucionMs, Guid? EquipoId)` — Task 7 los construye desde el mapper.

- [ ] **Step 1: Escribir los tests (fallan por compilación)**

`tests/Umbral.Puntuaciones.UnitTests/Application/ProyectarScoringHandlersTests.cs`:

```csharp
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.Handlers.Commands;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ProyectarScoringHandlersTests
{
    private readonly FakeProyeccionesRepository _repo = new();
    private readonly FakePuntuacionesUnitOfWork _uow = new();
    private static readonly DateTime Ahora = new(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc);

    private static ProyectarPuntajeTriviaCommand Trivia(Guid juegoId, Guid participanteId, int puntaje, long tiempoMs, Guid? equipoId = null)
        => new(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), juegoId, Guid.NewGuid(), participanteId, puntaje, tiempoMs, equipoId);

    [Fact]
    public async Task PuntajeTrivia_individual_acredita_al_participante()
    {
        var juegoId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var handler = new ProyectarPuntajeTriviaCommandHandler(_repo, _uow);

        await handler.Handle(Trivia(juegoId, participanteId, 10, 1500), CancellationToken.None);
        await handler.Handle(Trivia(juegoId, participanteId, 5, 500), CancellationToken.None);

        var marcador = Assert.Single(_repo.Marcadores);
        Assert.Equal(participanteId, marcador.CompetidorId);
        Assert.Equal(TipoCompetidor.Participante, marcador.TipoCompetidor);
        Assert.Equal(15, marcador.PuntosAcumulados);
        Assert.Equal(2000, marcador.TiempoAcumuladoMs);
        Assert.Equal(2, marcador.UnidadesGanadas);
    }

    [Fact]
    public async Task PuntajeTrivia_equipo_acredita_al_equipo_no_al_autor()
    {
        var juegoId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var handler = new ProyectarPuntajeTriviaCommandHandler(_repo, _uow);

        // Dos autores distintos del mismo equipo.
        await handler.Handle(Trivia(juegoId, Guid.NewGuid(), 10, 1000, equipoId), CancellationToken.None);
        await handler.Handle(Trivia(juegoId, Guid.NewGuid(), 20, 2000, equipoId), CancellationToken.None);

        var marcador = Assert.Single(_repo.Marcadores);
        Assert.Equal(equipoId, marcador.CompetidorId);
        Assert.Equal(TipoCompetidor.Equipo, marcador.TipoCompetidor);
        Assert.Equal(30, marcador.PuntosAcumulados);
    }

    [Fact]
    public async Task PuntajeTrivia_duplicado_no_acredita_dos_veces()
    {
        var cmd = Trivia(Guid.NewGuid(), Guid.NewGuid(), 10, 1000);
        var handler = new ProyectarPuntajeTriviaCommandHandler(_repo, _uow);

        await handler.Handle(cmd, CancellationToken.None);
        await handler.Handle(cmd, CancellationToken.None);

        var marcador = Assert.Single(_repo.Marcadores);
        Assert.Equal(10, marcador.PuntosAcumulados);
        Assert.Equal(1, marcador.UnidadesGanadas);
    }

    [Fact]
    public async Task EtapaBdtGanada_acredita_puntaje_de_etapa()
    {
        var juegoId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var cmd = new ProyectarEtapaBdtGanadaCommand(
            Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), juegoId, Guid.NewGuid(), participanteId, 25, 4000, null);

        await new ProyectarEtapaBdtGanadaCommandHandler(_repo, _uow).Handle(cmd, CancellationToken.None);

        var marcador = Assert.Single(_repo.Marcadores);
        Assert.Equal(25, marcador.PuntosAcumulados);
        Assert.Equal(4000, marcador.TiempoAcumuladoMs);
        Assert.Equal(1, marcador.UnidadesGanadas);
    }

    [Fact]
    public async Task Competidores_distintos_del_mismo_juego_tienen_marcadores_separados()
    {
        var juegoId = Guid.NewGuid();
        var handler = new ProyectarPuntajeTriviaCommandHandler(_repo, _uow);

        await handler.Handle(Trivia(juegoId, Guid.NewGuid(), 10, 1000), CancellationToken.None);
        await handler.Handle(Trivia(juegoId, Guid.NewGuid(), 20, 2000), CancellationToken.None);

        Assert.Equal(2, _repo.Marcadores.Count);
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj --filter "FullyQualifiedName~ProyectarScoring"`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar comandos y handlers de scoring**

`Application/Commands/ProyectarPuntajeTriviaCommand.cs`:

```csharp
using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarPuntajeTriviaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, Guid JuegoId,
    Guid PreguntaId, Guid ParticipanteId, int Puntaje, long TiempoRespuestaMs, Guid? EquipoId) : IRequest;
```

`Application/Commands/ProyectarEtapaBdtGanadaCommand.cs`:

```csharp
using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarEtapaBdtGanadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid SesionPartidaId, Guid JuegoId,
    Guid EtapaId, Guid ParticipanteId, int Puntaje, long TiempoResolucionMs, Guid? EquipoId) : IRequest;
```

`Application/Handlers/Commands/ProyectarPuntajeTriviaCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarPuntajeTriviaCommandHandler : IRequestHandler<ProyectarPuntajeTriviaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarPuntajeTriviaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarPuntajeTriviaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        // Identidad dual slice-E: en Equipo se acredita al equipo; en Individual, al participante.
        var competidorId = request.EquipoId ?? request.ParticipanteId;
        var tipo = request.EquipoId is null ? TipoCompetidor.Participante : TipoCompetidor.Equipo;

        var marcador = await _repo.GetMarcadorAsync(request.JuegoId, competidorId, cancellationToken);
        if (marcador is null)
        {
            marcador = Marcador.Nuevo(request.JuegoId, competidorId, request.PartidaId, tipo);
            _repo.AddMarcador(marcador);
        }
        marcador.Acreditar(request.Puntaje, request.TiempoRespuestaMs);

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "PuntajeTriviaIncrementado", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
```

`Application/Handlers/Commands/ProyectarEtapaBdtGanadaCommandHandler.cs`: mismo cuerpo que el de Trivia sustituyendo el tipo del request por `ProyectarEtapaBdtGanadaCommand`, `request.TiempoRespuestaMs` por `request.TiempoResolucionMs` y el eventType por `"EtapaBDTGanada"`.

- [ ] **Step 4: Correr los tests y verificar que pasan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj --filter "FullyQualifiedName~ProyectarScoring"`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): proyeccion de scoring Trivia/BDT con identidad dual y dedup"
```

---

### Task 5: Queries de ranking y marcador — cálculo on-read, empates, 404

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Queries/ObtenerRankingJuegoQuery.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Queries/ObtenerMarcadorQuery.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/RankingJuegoResponse.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/MarcadorResponse.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Exceptions/JuegoNoEncontradoException.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Exceptions/MarcadorNoEncontradoException.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/RankingCalculator.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerRankingJuegoQueryHandler.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerMarcadorQueryHandler.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerRankingJuegoQueryHandlerTests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerMarcadorQueryHandlerTests.cs`

**Interfaces:**
- Consumes: fakes de Task 3; `IProyeccionesRepository`; entidades/enums.
- Produces: `ObtenerRankingJuegoQuery(Guid PartidaId, Guid JuegoId) : IRequest<RankingJuegoResponse>`; `ObtenerMarcadorQuery(Guid PartidaId, Guid JuegoId, Guid CompetidorId) : IRequest<MarcadorResponse>`; DTOs `RankingJuegoResponse(Guid JuegoId, TipoJuego TipoJuego, DateTime GeneradoEn, IReadOnlyList<EntradaRankingDto> Entradas)`, `EntradaRankingDto(int Posicion, Guid CompetidorId, TipoCompetidor TipoCompetidor, int Puntos, long TiempoAcumuladoMs, int UnidadesGanadas)`, `MarcadorResponse(Guid CompetidorId, TipoCompetidor TipoCompetidor, int Puntos, long TiempoAcumuladoMs, int UnidadesGanadas, int Posicion)`; excepciones `JuegoNoEncontradoException(Guid)`, `MarcadorNoEncontradoException(Guid, Guid)` — Task 6 las mapea a 404.

- [ ] **Step 1: Escribir los tests (fallan por compilación)**

`tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerRankingJuegoQueryHandlerTests.cs`:

```csharp
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ObtenerRankingJuegoQueryHandlerTests
{
    private readonly FakeProyeccionesRepository _repo = new();

    private (Guid partidaId, Guid juegoId) SembrarJuego(TipoJuego tipo = TipoJuego.Trivia)
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        _repo.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 1, tipo));
        return (partidaId, juegoId);
    }

    private void SembrarMarcador(Guid juegoId, Guid competidorId, int puntos, long tiempoMs, int unidades)
    {
        var m = Marcador.Nuevo(juegoId, competidorId, Guid.NewGuid(), TipoCompetidor.Participante);
        for (var i = 0; i < unidades; i++)
        {
            m.Acreditar(i == 0 ? puntos : 0, i == 0 ? tiempoMs : 0);
        }
        _repo.AddMarcador(m);
    }

    [Fact]
    public async Task Ordena_por_puntos_desc_y_tiempo_asc()
    {
        var (partidaId, juegoId) = SembrarJuego();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        SembrarMarcador(juegoId, a, 10, 5000, 1); // 2do: menos puntos que c
        SembrarMarcador(juegoId, b, 10, 9000, 1); // 3ro: mismos puntos que a, mas tiempo
        SembrarMarcador(juegoId, c, 20, 9999, 1); // 1ro: mas puntos, el tiempo no lo baja

        var r = await new ObtenerRankingJuegoQueryHandler(_repo).Handle(
            new ObtenerRankingJuegoQuery(partidaId, juegoId), CancellationToken.None);

        Assert.Equal(new[] { c, a, b }, r.Entradas.Select(e => e.CompetidorId).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, r.Entradas.Select(e => e.Posicion).ToArray());
        Assert.Equal(TipoJuego.Trivia, r.TipoJuego);
    }

    [Fact]
    public async Task Empate_exacto_comparte_posicion_y_la_siguiente_salta()
    {
        var (partidaId, juegoId) = SembrarJuego(TipoJuego.BusquedaDelTesoro);
        SembrarMarcador(juegoId, Guid.NewGuid(), 20, 1000, 2);
        SembrarMarcador(juegoId, Guid.NewGuid(), 10, 3000, 1); // empatado
        SembrarMarcador(juegoId, Guid.NewGuid(), 10, 3000, 1); // empatado
        SembrarMarcador(juegoId, Guid.NewGuid(), 5, 100, 1);

        var r = await new ObtenerRankingJuegoQueryHandler(_repo).Handle(
            new ObtenerRankingJuegoQuery(partidaId, juegoId), CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 2, 4 }, r.Entradas.Select(e => e.Posicion).ToArray());
    }

    [Fact]
    public async Task Muchas_unidades_ganadas_no_ordenan_solo_los_puntos()
    {
        // Doctrina BDT: EtapasGanadas es informativo; gana quien acumula mas puntos.
        var (partidaId, juegoId) = SembrarJuego(TipoJuego.BusquedaDelTesoro);
        var muchasEtapas = Guid.NewGuid();
        var pocasEtapasMasPuntos = Guid.NewGuid();
        SembrarMarcador(juegoId, muchasEtapas, 10, 1000, 3);
        SembrarMarcador(juegoId, pocasEtapasMasPuntos, 50, 9000, 1);

        var r = await new ObtenerRankingJuegoQueryHandler(_repo).Handle(
            new ObtenerRankingJuegoQuery(partidaId, juegoId), CancellationToken.None);

        Assert.Equal(pocasEtapasMasPuntos, r.Entradas[0].CompetidorId);
        Assert.Equal(3, r.Entradas[1].UnidadesGanadas);
    }

    [Fact]
    public async Task Juego_sin_marcadores_devuelve_lista_vacia()
    {
        var (partidaId, juegoId) = SembrarJuego();

        var r = await new ObtenerRankingJuegoQueryHandler(_repo).Handle(
            new ObtenerRankingJuegoQuery(partidaId, juegoId), CancellationToken.None);

        Assert.Empty(r.Entradas);
        Assert.Equal(juegoId, r.JuegoId);
    }

    [Fact]
    public async Task Juego_desconocido_lanza_404()
    {
        await Assert.ThrowsAsync<JuegoNoEncontradoException>(() =>
            new ObtenerRankingJuegoQueryHandler(_repo).Handle(
                new ObtenerRankingJuegoQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Juego_de_otra_partida_lanza_404()
    {
        var (_, juegoId) = SembrarJuego();

        await Assert.ThrowsAsync<JuegoNoEncontradoException>(() =>
            new ObtenerRankingJuegoQueryHandler(_repo).Handle(
                new ObtenerRankingJuegoQuery(Guid.NewGuid(), juegoId), CancellationToken.None));
    }
}
```

`tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerMarcadorQueryHandlerTests.cs`:

```csharp
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ObtenerMarcadorQueryHandlerTests
{
    private readonly FakeProyeccionesRepository _repo = new();

    [Fact]
    public async Task Devuelve_marcador_con_posicion_actual()
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        _repo.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 1, TipoJuego.Trivia));
        var lider = Marcador.Nuevo(juegoId, Guid.NewGuid(), partidaId, TipoCompetidor.Participante);
        lider.Acreditar(30, 1000);
        _repo.AddMarcador(lider);
        var consultado = Marcador.Nuevo(juegoId, Guid.NewGuid(), partidaId, TipoCompetidor.Participante);
        consultado.Acreditar(10, 2000);
        _repo.AddMarcador(consultado);

        var r = await new ObtenerMarcadorQueryHandler(_repo).Handle(
            new ObtenerMarcadorQuery(partidaId, juegoId, consultado.CompetidorId), CancellationToken.None);

        Assert.Equal(consultado.CompetidorId, r.CompetidorId);
        Assert.Equal(10, r.Puntos);
        Assert.Equal(2, r.Posicion);
    }

    [Fact]
    public async Task Competidor_sin_marcador_lanza_404()
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        _repo.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 1, TipoJuego.Trivia));

        await Assert.ThrowsAsync<MarcadorNoEncontradoException>(() =>
            new ObtenerMarcadorQueryHandler(_repo).Handle(
                new ObtenerMarcadorQuery(partidaId, juegoId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Juego_desconocido_lanza_404_de_juego()
    {
        await Assert.ThrowsAsync<JuegoNoEncontradoException>(() =>
            new ObtenerMarcadorQueryHandler(_repo).Handle(
                new ObtenerMarcadorQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj --filter "FullyQualifiedName~ObtenerRankingJuego|FullyQualifiedName~ObtenerMarcador"`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar queries, DTOs, excepciones, calculator y handlers**

`Application/Queries/ObtenerRankingJuegoQuery.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerRankingJuegoQuery(Guid PartidaId, Guid JuegoId) : IRequest<RankingJuegoResponse>;
```

`Application/Queries/ObtenerMarcadorQuery.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerMarcadorQuery(Guid PartidaId, Guid JuegoId, Guid CompetidorId) : IRequest<MarcadorResponse>;
```

`Application/DTOs/RankingJuegoResponse.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record EntradaRankingDto(
    int Posicion, Guid CompetidorId, TipoCompetidor TipoCompetidor,
    int Puntos, long TiempoAcumuladoMs, int UnidadesGanadas);

public sealed record RankingJuegoResponse(
    Guid JuegoId, TipoJuego TipoJuego, DateTime GeneradoEn, IReadOnlyList<EntradaRankingDto> Entradas);
```

`Application/DTOs/MarcadorResponse.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record MarcadorResponse(
    Guid CompetidorId, TipoCompetidor TipoCompetidor,
    int Puntos, long TiempoAcumuladoMs, int UnidadesGanadas, int Posicion);
```

`Application/Exceptions/JuegoNoEncontradoException.cs`:

```csharp
namespace Umbral.Puntuaciones.Application.Exceptions;

public sealed class JuegoNoEncontradoException : Exception
{
    public JuegoNoEncontradoException(Guid juegoId)
        : base($"No se encontró el juego {juegoId} en las proyecciones de Puntuaciones.")
    {
    }
}
```

`Application/Exceptions/MarcadorNoEncontradoException.cs`:

```csharp
namespace Umbral.Puntuaciones.Application.Exceptions;

public sealed class MarcadorNoEncontradoException : Exception
{
    public MarcadorNoEncontradoException(Guid juegoId, Guid competidorId)
        : base($"No existe marcador del competidor {competidorId} en el juego {juegoId}.")
    {
    }
}
```

`Application/Handlers/Queries/RankingCalculator.cs` (regla de orden compartida por ambas queries — puntos DESC, tiempo ASC; empate exacto comparte posición):

```csharp
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

public static class RankingCalculator
{
    public static IReadOnlyList<EntradaRankingDto> Calcular(IEnumerable<Marcador> marcadores)
    {
        var ordenados = marcadores
            .OrderByDescending(m => m.PuntosAcumulados)
            .ThenBy(m => m.TiempoAcumuladoMs)
            .ToList();

        var entradas = new List<EntradaRankingDto>(ordenados.Count);
        for (var i = 0; i < ordenados.Count; i++)
        {
            var actual = ordenados[i];
            var posicion = i + 1;
            if (i > 0)
            {
                var previo = ordenados[i - 1];
                var empateExacto = previo.PuntosAcumulados == actual.PuntosAcumulados
                    && previo.TiempoAcumuladoMs == actual.TiempoAcumuladoMs;
                if (empateExacto)
                {
                    posicion = entradas[i - 1].Posicion;
                }
            }

            entradas.Add(new EntradaRankingDto(
                posicion, actual.CompetidorId, actual.TipoCompetidor,
                actual.PuntosAcumulados, actual.TiempoAcumuladoMs, actual.UnidadesGanadas));
        }

        return entradas;
    }
}
```

`Application/Handlers/Queries/ObtenerRankingJuegoQueryHandler.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

public sealed class ObtenerRankingJuegoQueryHandler : IRequestHandler<ObtenerRankingJuegoQuery, RankingJuegoResponse>
{
    private readonly IProyeccionesRepository _repo;

    public ObtenerRankingJuegoQueryHandler(IProyeccionesRepository repo) => _repo = repo;

    public async Task<RankingJuegoResponse> Handle(ObtenerRankingJuegoQuery request, CancellationToken cancellationToken)
    {
        var juego = await _repo.GetJuegoAsync(request.JuegoId, cancellationToken);
        if (juego is null || juego.PartidaId != request.PartidaId)
        {
            throw new JuegoNoEncontradoException(request.JuegoId);
        }

        var marcadores = await _repo.GetMarcadoresDeJuegoAsync(request.JuegoId, cancellationToken);
        return new RankingJuegoResponse(juego.JuegoId, juego.TipoJuego, DateTime.UtcNow, RankingCalculator.Calcular(marcadores));
    }
}
```

`Application/Handlers/Queries/ObtenerMarcadorQueryHandler.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

public sealed class ObtenerMarcadorQueryHandler : IRequestHandler<ObtenerMarcadorQuery, MarcadorResponse>
{
    private readonly IProyeccionesRepository _repo;

    public ObtenerMarcadorQueryHandler(IProyeccionesRepository repo) => _repo = repo;

    public async Task<MarcadorResponse> Handle(ObtenerMarcadorQuery request, CancellationToken cancellationToken)
    {
        var juego = await _repo.GetJuegoAsync(request.JuegoId, cancellationToken);
        if (juego is null || juego.PartidaId != request.PartidaId)
        {
            throw new JuegoNoEncontradoException(request.JuegoId);
        }

        var marcadores = await _repo.GetMarcadoresDeJuegoAsync(request.JuegoId, cancellationToken);
        var entradas = RankingCalculator.Calcular(marcadores);
        var propia = entradas.FirstOrDefault(e => e.CompetidorId == request.CompetidorId)
            ?? throw new MarcadorNoEncontradoException(request.JuegoId, request.CompetidorId);

        return new MarcadorResponse(
            propia.CompetidorId, propia.TipoCompetidor, propia.Puntos,
            propia.TiempoAcumuladoMs, propia.UnidadesGanadas, propia.Posicion);
    }
}
```

- [ ] **Step 4: Correr los tests y verificar que pasan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj --filter "FullyQualifiedName~ObtenerRankingJuego|FullyQualifiedName~ObtenerMarcador"`
Expected: PASS (9 tests).

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): queries de ranking por juego y marcador propio (on-read, empates compartidos)"
```

---

### Task 6: Controller, middleware 404, JSON enums y JWT del servicio

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/RankingsController.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs`
- Create: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/FakeSender.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/RankingsControllerTests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/ExceptionHandlingMiddlewareTests.cs`

**Interfaces:**
- Consumes: queries/DTOs/excepciones de Task 5.
- Produces: `GET /puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking` y `GET /puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{competidorId}` (rutas que Task 8/9 ejercitan); `FakeSender` reutilizable.

- [ ] **Step 1: Crear FakeSender y escribir los tests (fallan por compilación)**

`tests/Umbral.Puntuaciones.UnitTests/Api/FakeSender.cs` — **copiar tal cual** `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/FakeSender.cs` cambiando el namespace a `Umbral.Puntuaciones.UnitTests.Api` (implementa `MediatR.ISender` registrando el último request enviado y devolviendo una respuesta configurable; MediatR es 12.2.0 en ambos servicios, las firmas coinciden).

`tests/Umbral.Puntuaciones.UnitTests/Api/RankingsControllerTests.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Api.Controllers;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.UnitTests.Api;

public class RankingsControllerTests
{
    [Fact]
    public async Task ObtenerRanking_despacha_query_y_devuelve_ok()
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var respuesta = new RankingJuegoResponse(juegoId, TipoJuego.Trivia, DateTime.UtcNow, Array.Empty<EntradaRankingDto>());
        var sender = new FakeSender { Respuesta = respuesta };
        var controller = new RankingsController(sender);

        var result = await controller.ObtenerRanking(partidaId, juegoId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(respuesta, ok.Value);
        var query = Assert.IsType<ObtenerRankingJuegoQuery>(sender.UltimoRequest);
        Assert.Equal(partidaId, query.PartidaId);
        Assert.Equal(juegoId, query.JuegoId);
    }

    [Fact]
    public async Task ObtenerMarcador_despacha_query_y_devuelve_ok()
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidorId = Guid.NewGuid();
        var respuesta = new MarcadorResponse(competidorId, TipoCompetidor.Participante, 10, 1000, 1, 1);
        var sender = new FakeSender { Respuesta = respuesta };
        var controller = new RankingsController(sender);

        var result = await controller.ObtenerMarcador(partidaId, juegoId, competidorId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(respuesta, ok.Value);
        var query = Assert.IsType<ObtenerMarcadorQuery>(sender.UltimoRequest);
        Assert.Equal(partidaId, query.PartidaId);
        Assert.Equal(juegoId, query.JuegoId);
        Assert.Equal(competidorId, query.CompetidorId);
    }
}
```

> Si el `FakeSender` copiado expone otra propiedad para la respuesta o el último request (p. ej. nombres distintos), adaptar los asserts a esa API en lugar de duplicar el fake.

`tests/Umbral.Puntuaciones.UnitTests/Api/ExceptionHandlingMiddlewareTests.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.Puntuaciones.Api.Middleware;
using Umbral.Puntuaciones.Application.Exceptions;

namespace Umbral.Puntuaciones.UnitTests.Api;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<int> StatusDe(Exception ex)
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw ex, NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        return context.Response.StatusCode;
    }

    [Fact]
    public async Task JuegoNoEncontrado_mapea_404()
        => Assert.Equal(StatusCodes.Status404NotFound, await StatusDe(new JuegoNoEncontradoException(Guid.NewGuid())));

    [Fact]
    public async Task MarcadorNoEncontrado_mapea_404()
        => Assert.Equal(StatusCodes.Status404NotFound, await StatusDe(new MarcadorNoEncontradoException(Guid.NewGuid(), Guid.NewGuid())));

    [Fact]
    public async Task Excepcion_generica_mapea_500()
        => Assert.Equal(StatusCodes.Status500InternalServerError, await StatusDe(new InvalidOperationException("x")));
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj --filter "FullyQualifiedName~RankingsController|FullyQualifiedName~ExceptionHandlingMiddleware"`
Expected: FAIL de compilación (`RankingsController` no existe; el middleware aún no mapea 404).

- [ ] **Step 3: Implementar controller, mapping del middleware y Program.cs**

`src/Umbral.Puntuaciones.Api/Controllers/RankingsController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.Api.Controllers;

[ApiController]
[Route("puntuaciones")]
public sealed class RankingsController : ControllerBase
{
    private readonly ISender _mediator;

    public RankingsController(ISender mediator) => _mediator = mediator;

    [HttpGet("partidas/{partidaId:guid}/juegos/{juegoId:guid}/ranking")]
    public async Task<IActionResult> ObtenerRanking(Guid partidaId, Guid juegoId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ObtenerRankingJuegoQuery(partidaId, juegoId), cancellationToken);
        return Ok(response);
    }

    [HttpGet("partidas/{partidaId:guid}/juegos/{juegoId:guid}/marcadores/{competidorId:guid}")]
    public async Task<IActionResult> ObtenerMarcador(Guid partidaId, Guid juegoId, Guid competidorId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ObtenerMarcadorQuery(partidaId, juegoId, competidorId), cancellationToken);
        return Ok(response);
    }
}
```

En `src/Umbral.Puntuaciones.Api/Middleware/ExceptionHandlingMiddleware.cs`, dentro del `catch`, reemplazar la asignación fija de status por mapeo (patrón exacto de Operaciones):

```csharp
        catch (Exception ex)
        {
            var status = MapStatus(ex);
            if (status == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception.");
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
        }
```

y añadir al final de la clase (+ using `Umbral.Puntuaciones.Application.Exceptions;` y `FluentValidation;`):

```csharp
    private static HttpStatusCode MapStatus(Exception ex) => ex switch
    {
        JuegoNoEncontradoException or MarcadorNoEncontradoException => HttpStatusCode.NotFound,
        ValidationException or ArgumentException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
    };
```

En `src/Umbral.Puntuaciones.Api/Program.cs`:

1. `AddControllers()` gana el converter de enums (los DTOs serializan `tipoJuego`/`tipoCompetidor` como string):

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
```

2. Después del bloque del hosted service y antes de `var app = builder.Build();`, añadir la validación JWT Keycloak condicional (defensa en profundidad, espejo de `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs:53-129`): **copiar de ahí** el helper `ResolveSetting`, la resolución de las 5 variables `Keycloak:*`, el `if/else` con `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` **omitiendo `options.Events`/`OnMessageReceived`** (no hay hub SignalR en 4a) y el `builder.Services.AddAuthorization();`. Usings nuevos: `Microsoft.AspNetCore.Authentication.JwtBearer;` y `Microsoft.IdentityModel.Tokens;`.

3. Tras `app.UseMiddleware<ExceptionHandlingMiddleware>();`, añadir:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Sin variables Keycloak (tests, dev sin auth) la rama `else` (`AddAuthentication()` a secas) mantiene el comportamiento actual: los GET son de lectura para cualquier rol autenticado — la puerta por rol la pone el gateway, igual que en los servicios hermanos.

- [ ] **Step 4: Correr los tests y verificar que pasan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj --filter "FullyQualifiedName~RankingsController|FullyQualifiedName~ExceptionHandlingMiddleware"`
Expected: PASS (5 tests). Además `dotnet test services/puntuaciones/Umbral.Puntuaciones.sln` completo verde (health intacto).

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): endpoints de ranking y marcador con middleware 404 y JWT condicional"
```

---

### Task 7: Consumidor real — payload en envelope, mapper y worker

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/EnvelopeReader.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/ProyeccionEventMapper.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/RabbitMqConsumerOptions.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/OperacionesSesionEventsConsumer.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/EnvelopeReaderTests.cs` (modify — los fixtures válidos ganan `payload`)
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/ProyeccionEventMapperTests.cs`

**Interfaces:**
- Consumes: los 7 comandos de Tasks 3-4 (firmas exactas en sus bloques "Produces").
- Produces: `EnvelopeResumen` gana `JsonElement Payload`; `ProyeccionEventMapper.Map(EnvelopeResumen) : IBaseRequest?` (null = evento sin proyección en 4a o payload no deserializable); `RabbitMqConsumerOptions.Queue` default `puntuaciones.operaciones-sesion.proyecciones`, `Bindings` (string[], 7 keys), const `ColaDeHumoLegacy`.

- [ ] **Step 1: Actualizar/escribir los tests (fallan)**

En `tests/Umbral.Puntuaciones.UnitTests/Workers/EnvelopeReaderTests.cs`: añadir `"payload": {...}` a todos los JSON válidos existentes, añadir un caso nuevo `Envelope_sin_payload_es_malformado` (JSON con los 4 campos pero sin `payload` → `TryRead` devuelve `false`) y un assert en el caso válido de que `envelope.Payload.ValueKind == JsonValueKind.Object`.

`tests/Umbral.Puntuaciones.UnitTests/Workers/ProyeccionEventMapperTests.cs`:

```csharp
using System.Text.Json;
using Umbral.Puntuaciones.Api.Workers;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.UnitTests.Workers;

public class ProyeccionEventMapperTests
{
    private static EnvelopeResumen Envelope(string eventType, string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        return new EnvelopeResumen(Guid.NewGuid(), eventType, 1, DateTime.UtcNow, doc.RootElement.Clone());
    }

    [Fact]
    public void Mapea_PartidaPublicadaEnLobby()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var envelope = Envelope("PartidaPublicadaEnLobby",
            $$"""{"partidaId":"{{partidaId}}","sesionPartidaId":"{{sesionId}}","modalidad":"Equipo","minimosParticipacion":1,"maximosParticipacion":10}""");

        var cmd = Assert.IsType<ProyectarPartidaPublicadaCommand>(ProyeccionEventMapper.Map(envelope));

        Assert.Equal(envelope.EventId, cmd.EventId);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(Modalidad.Equipo, cmd.Modalidad);
    }

    [Fact]
    public void Mapea_PuntajeTriviaIncrementado_individual_y_equipo()
    {
        var juegoId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var individual = Envelope("PuntajeTriviaIncrementado",
            $$"""{"partidaId":"{{Guid.NewGuid()}}","sesionPartidaId":"{{Guid.NewGuid()}}","juegoId":"{{juegoId}}","preguntaId":"{{Guid.NewGuid()}}","participanteId":"{{Guid.NewGuid()}}","puntaje":10,"tiempoRespuestaMs":1234,"equipoId":null}""");
        var equipo = Envelope("PuntajeTriviaIncrementado",
            $$"""{"partidaId":"{{Guid.NewGuid()}}","sesionPartidaId":"{{Guid.NewGuid()}}","juegoId":"{{juegoId}}","preguntaId":"{{Guid.NewGuid()}}","participanteId":"{{Guid.NewGuid()}}","puntaje":10,"tiempoRespuestaMs":1234,"equipoId":"{{equipoId}}"}""");

        var cmdIndividual = Assert.IsType<ProyectarPuntajeTriviaCommand>(ProyeccionEventMapper.Map(individual));
        var cmdEquipo = Assert.IsType<ProyectarPuntajeTriviaCommand>(ProyeccionEventMapper.Map(equipo));

        Assert.Null(cmdIndividual.EquipoId);
        Assert.Equal(equipoId, cmdEquipo.EquipoId);
        Assert.Equal(10, cmdEquipo.Puntaje);
        Assert.Equal(1234, cmdEquipo.TiempoRespuestaMs);
    }

    [Fact]
    public void Mapea_EtapaBDTGanada()
    {
        var envelope = Envelope("EtapaBDTGanada",
            $$"""{"partidaId":"{{Guid.NewGuid()}}","sesionPartidaId":"{{Guid.NewGuid()}}","juegoId":"{{Guid.NewGuid()}}","etapaId":"{{Guid.NewGuid()}}","participanteId":"{{Guid.NewGuid()}}","puntaje":25,"tiempoResolucionMs":4000,"equipoId":null}""");

        var cmd = Assert.IsType<ProyectarEtapaBdtGanadaCommand>(ProyeccionEventMapper.Map(envelope));

        Assert.Equal(25, cmd.Puntaje);
        Assert.Equal(4000, cmd.TiempoResolucionMs);
    }

    [Fact]
    public void Mapea_JuegoActivado_con_tipo_enum_string()
    {
        var envelope = Envelope("JuegoActivado",
            $$"""{"partidaId":"{{Guid.NewGuid()}}","sesionPartidaId":"{{Guid.NewGuid()}}","juegoId":"{{Guid.NewGuid()}}","orden":2,"tipoJuego":"BusquedaDelTesoro"}""");

        var cmd = Assert.IsType<ProyectarJuegoActivadoCommand>(ProyeccionEventMapper.Map(envelope));

        Assert.Equal(2, cmd.Orden);
        Assert.Equal(TipoJuego.BusquedaDelTesoro, cmd.TipoJuego);
    }

    [Theory]
    [InlineData("PartidaIniciada", """{"partidaId":"0e6bd10a-9088-4a4e-8b1a-111111111111","sesionPartidaId":"0e6bd10a-9088-4a4e-8b1a-222222222222","fechaInicio":"2026-07-04T10:00:00Z","primerJuegoId":"0e6bd10a-9088-4a4e-8b1a-333333333333","primerJuegoOrden":1}""", typeof(ProyectarPartidaIniciadaCommand))]
    [InlineData("PartidaCancelada", """{"partidaId":"0e6bd10a-9088-4a4e-8b1a-111111111111","sesionPartidaId":"0e6bd10a-9088-4a4e-8b1a-222222222222","motivo":"MinimosNoAlcanzados","fechaCancelacion":"2026-07-04T10:00:00Z"}""", typeof(ProyectarPartidaCanceladaCommand))]
    [InlineData("PartidaFinalizada", """{"partidaId":"0e6bd10a-9088-4a4e-8b1a-111111111111","sesionPartidaId":"0e6bd10a-9088-4a4e-8b1a-222222222222","fechaFin":"2026-07-04T10:30:00Z"}""", typeof(ProyectarPartidaFinalizadaCommand))]
    public void Mapea_los_eventos_de_ciclo_de_vida(string eventType, string payload, Type esperado)
    {
        var cmd = ProyeccionEventMapper.Map(Envelope(eventType, payload));

        Assert.NotNull(cmd);
        Assert.IsType(esperado, cmd);
    }

    [Theory]
    [InlineData("RespuestaTriviaValidada")] // evento real sin proyección en 4a
    [InlineData("UbicacionActualizada")]
    [InlineData("EventoInventado")]
    public void Eventos_sin_proyeccion_devuelven_null(string eventType)
    {
        Assert.Null(ProyeccionEventMapper.Map(Envelope(eventType, """{"x":1}""")));
    }

    [Fact]
    public void Payload_no_deserializable_devuelve_null()
    {
        var envelope = Envelope("JuegoActivado", """{"tipoJuego":"NoExisteEsteTipo"}""");

        Assert.Null(ProyeccionEventMapper.Map(envelope));
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj --filter "FullyQualifiedName~Workers"`
Expected: FAIL de compilación (`EnvelopeResumen` sin `Payload`, mapper no existe).

- [ ] **Step 3: Implementar EnvelopeReader+payload, mapper, options y worker**

`Api/Workers/EnvelopeReader.cs` — el record pasa a:

```csharp
public sealed record EnvelopeResumen(Guid EventId, string EventType, int Version, DateTime OccurredAt, JsonElement Payload);
```

y en `TryRead`, al bloque de validaciones se suma (antes del `return false` conjunto):

```csharp
                !root.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object
```

con el constructor final `envelope = new EnvelopeResumen(eventId, eventType, version, occurredAt, payload.Clone());` (el `Clone()` es obligatorio: el `JsonDocument` se dispone al salir).

`Api/Workers/ProyeccionEventMapper.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Api.Workers;

// Traduce el envelope del broker al comando de proyección (SP-4a).
// Devuelve null para eventos sin proyección en este slice o payloads no deserializables (warn + ack en el worker).
public static class ProyeccionEventMapper
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static IBaseRequest? Map(EnvelopeResumen envelope)
    {
        try
        {
            return envelope.EventType switch
            {
                "PartidaPublicadaEnLobby" => MapPartidaPublicada(envelope),
                "PartidaIniciada" => MapPartidaIniciada(envelope),
                "JuegoActivado" => MapJuegoActivado(envelope),
                "PartidaCancelada" => MapPartidaCancelada(envelope),
                "PartidaFinalizada" => MapPartidaFinalizada(envelope),
                "PuntajeTriviaIncrementado" => MapPuntajeTrivia(envelope),
                "EtapaBDTGanada" => MapEtapaBdtGanada(envelope),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record PartidaPublicadaPayload(Guid PartidaId, Guid SesionPartidaId, Modalidad Modalidad);
    private sealed record PartidaIniciadaPayload(Guid PartidaId, Guid SesionPartidaId, DateTime FechaInicio);
    private sealed record JuegoActivadoPayload(Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, int Orden, TipoJuego TipoJuego);
    private sealed record PartidaCanceladaPayload(Guid PartidaId, Guid SesionPartidaId, DateTime FechaCancelacion);
    private sealed record PartidaFinalizadaPayload(Guid PartidaId, Guid SesionPartidaId, DateTime FechaFin);
    private sealed record PuntajeTriviaPayload(
        Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
        Guid ParticipanteId, int Puntaje, long TiempoRespuestaMs, Guid? EquipoId);
    private sealed record EtapaBdtGanadaPayload(
        Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
        Guid ParticipanteId, int Puntaje, long TiempoResolucionMs, Guid? EquipoId);

    private static T? Deserializar<T>(EnvelopeResumen envelope) where T : class
        => envelope.Payload.Deserialize<T>(JsonOpts);

    private static IBaseRequest? MapPartidaPublicada(EnvelopeResumen e)
        => Deserializar<PartidaPublicadaPayload>(e) is { } p
            ? new ProyectarPartidaPublicadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.Modalidad)
            : null;

    private static IBaseRequest? MapPartidaIniciada(EnvelopeResumen e)
        => Deserializar<PartidaIniciadaPayload>(e) is { } p
            ? new ProyectarPartidaIniciadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.FechaInicio)
            : null;

    private static IBaseRequest? MapJuegoActivado(EnvelopeResumen e)
        => Deserializar<JuegoActivadoPayload>(e) is { } p
            ? new ProyectarJuegoActivadoCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.JuegoId, p.Orden, p.TipoJuego)
            : null;

    private static IBaseRequest? MapPartidaCancelada(EnvelopeResumen e)
        => Deserializar<PartidaCanceladaPayload>(e) is { } p
            ? new ProyectarPartidaCanceladaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.FechaCancelacion)
            : null;

    private static IBaseRequest? MapPartidaFinalizada(EnvelopeResumen e)
        => Deserializar<PartidaFinalizadaPayload>(e) is { } p
            ? new ProyectarPartidaFinalizadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.FechaFin)
            : null;

    private static IBaseRequest? MapPuntajeTrivia(EnvelopeResumen e)
        => Deserializar<PuntajeTriviaPayload>(e) is { } p
            ? new ProyectarPuntajeTriviaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.JuegoId, p.PreguntaId, p.ParticipanteId, p.Puntaje, p.TiempoRespuestaMs, p.EquipoId)
            : null;

    private static IBaseRequest? MapEtapaBdtGanada(EnvelopeResumen e)
        => Deserializar<EtapaBdtGanadaPayload>(e) is { } p
            ? new ProyectarEtapaBdtGanadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.SesionPartidaId, p.JuegoId, p.EtapaId, p.ParticipanteId, p.Puntaje, p.TiempoResolucionMs, p.EquipoId)
            : null;
}
```

`Api/Workers/RabbitMqConsumerOptions.cs` — reemplazo completo:

```csharp
namespace Umbral.Puntuaciones.Api.Workers;

public sealed class RabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMq";

    // Cola de humo de SP-3i: se elimina al arrancar (su binding # acumularía ubicaciones sin consumidor).
    public const string ColaDeHumoLegacy = "puntuaciones.operaciones-sesion.all";

    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "umbral.operaciones-sesion";
    public string Queue { get; set; } = "puntuaciones.operaciones-sesion.proyecciones";
    public string[] Bindings { get; set; } =
    {
        "operaciones-sesion.partida-publicada-en-lobby.v1",
        "operaciones-sesion.partida-iniciada.v1",
        "operaciones-sesion.juego-activado.v1",
        "operaciones-sesion.partida-cancelada.v1",
        "operaciones-sesion.partida-finalizada.v1",
        "operaciones-sesion.puntaje-trivia-incrementado.v1",
        "operaciones-sesion.etapa-bdt-ganada.v1",
    };
}
```

`Api/Workers/OperacionesSesionEventsConsumer.cs` — reemplazo completo:

```csharp
using MediatR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Umbral.Puntuaciones.Api.Workers;

// Consumidor real de proyecciones (SP-4a): mapea cada evento a su comando MediatR y lo despacha
// con un scope por mensaje. Best-effort (ADR-0012): ack-siempre, sin poison-loop; la proyección
// es reconstruible. Reemplaza al consumidor de humo de SP-3i.
public sealed class OperacionesSesionEventsConsumer : BackgroundService
{
    private readonly RabbitMqConsumerOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OperacionesSesionEventsConsumer> _logger;

    public OperacionesSesionEventsConsumer(
        RabbitMqConsumerOptions options,
        IServiceScopeFactory scopeFactory,
        ILogger<OperacionesSesionEventsConsumer> logger)
    {
        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("RabbitMQ deshabilitado o sin host: el consumidor de proyecciones no arranca.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.Host,
                    Port = _options.Port,
                    UserName = _options.User,
                    Password = _options.Password,
                    DispatchConsumersAsync = true
                };
                using var connection = factory.CreateConnection("umbral-puntuaciones-consumer");
                EliminarColaDeHumoLegacy(connection);

                using var channel = connection.CreateModel();
                channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
                channel.QueueDeclare(_options.Queue, durable: true, exclusive: false, autoDelete: false);
                foreach (var binding in _options.Bindings)
                {
                    channel.QueueBind(_options.Queue, _options.Exchange, binding);
                }

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += (_, ea) => ProcesarMensajeAsync(channel, ea, stoppingToken);
                channel.BasicConsume(_options.Queue, autoAck: false, consumer);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Conexión RabbitMQ caída; reintento en 30 s.");
                try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }

    private async Task ProcesarMensajeAsync(IModel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        if (!EnvelopeReader.TryRead(ea.Body.Span, out var envelope))
        {
            _logger.LogWarning("Envelope malformado en {RoutingKey}; se descarta (ack).", ea.RoutingKey);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        var command = ProyeccionEventMapper.Map(envelope!);
        if (command is null)
        {
            _logger.LogWarning(
                "Evento {EventType} {EventId} sin proyección en SP-4a; se descarta (ack).",
                envelope!.EventType, envelope.EventId);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(command, ct);
            _logger.LogInformation(
                "Evento proyectado {EventType} {EventId} (rk {RoutingKey}).",
                envelope!.EventType, envelope.EventId, ea.RoutingKey);
        }
        catch (Exception ex)
        {
            // Best-effort (ADR-0012): la proyección es reconstruible; sin requeue para evitar poison-loop.
            _logger.LogError(ex, "Fallo proyectando {EventType} {EventId}; se descarta (ack).",
                envelope!.EventType, envelope.EventId);
        }
        finally
        {
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
    }

    private void EliminarColaDeHumoLegacy(IConnection connection)
    {
        // Canal desechable propio: si la operación falla, no tumba el canal de consumo.
        try
        {
            using var channel = connection.CreateModel();
            channel.QueueDelete(RabbitMqConsumerOptions.ColaDeHumoLegacy, ifUnused: false, ifEmpty: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo eliminar la cola de humo legacy {Queue}.", RabbitMqConsumerOptions.ColaDeHumoLegacy);
        }
    }
}
```

- [ ] **Step 4: Correr los tests y verificar que pasan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj --filter "FullyQualifiedName~Workers"`
Expected: PASS (EnvelopeReader actualizado + mapper). Suite completa de la solución también verde.

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): consumidor real de proyecciones (cola fina, mapper, dispatch MediatR)"
```

---

### Task 8: Tests de integración — proyección E2E y round-trip RabbitMQ opt-in

**Files:**
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/ProyeccionYRankingE2ETests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/RabbitMqProyeccionRoundTripTests.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj` (añadir `<PackageReference Include="RabbitMQ.Client" Version="6.8.1" />`)

**Interfaces:**
- Consumes: comandos (Tasks 3-4), endpoints (Task 6), consumer/options (Task 7).
- Produces: nada nuevo (verificación).

- [ ] **Step 1: Escribir el E2E de proyección (InMemory, sin broker)**

`tests/Umbral.Puntuaciones.IntegrationTests/ProyeccionYRankingE2ETests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class ProyeccionYRankingE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly DateTime Ahora = DateTime.UtcNow;

    public ProyeccionYRankingE2ETests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task Proyectar(IBaseRequest comando)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando);
    }

    [Fact]
    public async Task Flujo_completo_de_eventos_produce_ranking_consultable()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var ganador = Guid.NewGuid();
        var segundo = Guid.NewGuid();

        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await Proyectar(new ProyectarPartidaIniciadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), ganador, 20, 1000, null));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), segundo, 10, 2000, null));

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking");
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entradas = json.RootElement.GetProperty("entradas");
        Assert.Equal(2, entradas.GetArrayLength());
        Assert.Equal(ganador, entradas[0].GetProperty("competidorId").GetGuid());
        Assert.Equal(1, entradas[0].GetProperty("posicion").GetInt32());
        Assert.Equal(20, entradas[0].GetProperty("puntos").GetInt32());
        Assert.Equal("Trivia", json.RootElement.GetProperty("tipoJuego").GetString());
    }

    [Fact]
    public async Task Marcador_propio_devuelve_posicion_y_404_para_desconocido()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.BusquedaDelTesoro));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), Guid.NewGuid(), 25, 4000, equipoId));

        var client = _factory.CreateClient();
        var ok = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{equipoId}");
        var notFound = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{Guid.NewGuid()}");
        using var json = JsonDocument.Parse(await ok.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Equal(25, json.RootElement.GetProperty("puntos").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("posicion").GetInt32());
        Assert.Equal("Equipo", json.RootElement.GetProperty("tipoCompetidor").GetString());
    }

    [Fact]
    public async Task Ranking_de_juego_desconocido_devuelve_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/juegos/{Guid.NewGuid()}/ranking");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Evento_duplicado_no_duplica_puntos_e2e()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        var duplicado = new ProyectarPuntajeTriviaCommand(eventId, Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), participanteId, 10, 1000, null);
        await Proyectar(duplicado);
        await Proyectar(duplicado);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{participanteId}");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(10, json.RootElement.GetProperty("puntos").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("unidadesGanadas").GetInt32());
    }
}
```

- [ ] **Step 2: Correr el E2E y verificar que pasa**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj`
Expected: PASS. (Si algo falla aquí es un bug real de integración de las tasks previas: diagnosticar, no maquillar el test.)

- [ ] **Step 3: Escribir el round-trip RabbitMQ opt-in (patrón skip-suave de SP-3i)**

Añadir primero el paquete al csproj de IntegrationTests (`RabbitMQ.Client` 6.8.1, mismo ItemGroup que los demás).

`tests/Umbral.Puntuaciones.IntegrationTests/RabbitMqProyeccionRoundTripTests.cs`:

```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RabbitMQ.Client;

namespace Umbral.Puntuaciones.IntegrationTests;

// Round-trip real broker → consumidor → proyección → HTTP. Opt-in:
//   docker compose -f infra/docker-compose.yml up -d rabbitmq
//   RABBITMQ_TEST_HOST=localhost dotnet test tests/Umbral.Puntuaciones.IntegrationTests/... --filter RabbitMqProyeccionRoundTripTests
// Sin RABBITMQ_TEST_HOST el test retorna sin assertar (skip suave, sin dependencia de paquetes extra).
public class RabbitMqProyeccionRoundTripTests
{
    private const string Exchange = "umbral.operaciones-sesion";

    private static string EnvelopeJson(string eventType, object payload) => JsonSerializer.Serialize(new
    {
        eventId = Guid.NewGuid(),
        eventType,
        version = 1,
        occurredAt = DateTime.UtcNow,
        payload
    }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public async Task Evento_publicado_al_broker_termina_en_el_ranking()
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_TEST_HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            return; // skip suave: sin broker configurado no se asserta nada.
        }

        var testQueue = $"puntuaciones.proyecciones.it-{Guid.NewGuid():N}";
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RabbitMq:Enabled", "true");
            builder.UseSetting("RabbitMq:Host", host);
            builder.UseSetting("RabbitMq:Queue", testQueue);
        });
        var client = factory.CreateClient(); // arranca el host y el consumidor

        var connectionFactory = new ConnectionFactory { HostName = host };
        using var connection = connectionFactory.CreateConnection("umbral-puntuaciones-it");
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        // Esperar a que el consumidor haya declarado su cola (arranque asíncrono del BackgroundService).
        var declarada = false;
        for (var i = 0; i < 50 && !declarada; i++)
        {
            try
            {
                using var probe = connection.CreateModel();
                probe.QueueDeclarePassive(testQueue);
                declarada = true;
            }
            catch (Exception)
            {
                await Task.Delay(200);
            }
        }
        Assert.True(declarada, "El consumidor no declaró su cola a tiempo.");

        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();

        void Publicar(string routingKey, string json)
            => channel.BasicPublish(Exchange, routingKey, basicProperties: null, body: Encoding.UTF8.GetBytes(json));

        Publicar("operaciones-sesion.juego-activado.v1", EnvelopeJson("JuegoActivado",
            new { partidaId, sesionPartidaId = sesionId, juegoId, orden = 1, tipoJuego = "Trivia" }));
        Publicar("operaciones-sesion.puntaje-trivia-incrementado.v1", EnvelopeJson("PuntajeTriviaIncrementado",
            new { partidaId, sesionPartidaId = sesionId, juegoId, preguntaId = Guid.NewGuid(), participanteId, puntaje = 10, tiempoRespuestaMs = 1234, equipoId = (Guid?)null }));

        var proyectado = false;
        for (var i = 0; i < 50 && !proyectado; i++)
        {
            var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                proyectado = json.RootElement.GetProperty("entradas").GetArrayLength() == 1;
            }
            if (!proyectado)
            {
                await Task.Delay(200);
            }
        }

        channel.QueueDelete(testQueue, ifUnused: false, ifEmpty: false);
        Assert.True(proyectado, "El evento publicado al broker no llegó al ranking en 10 s.");
    }
}
```

- [ ] **Step 4: Correr la suite de integración (sin broker) y verificar verde**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj`
Expected: PASS — el round-trip retorna vacío sin `RABBITMQ_TEST_HOST`.

- [ ] **Step 5 (opcional, si hay docker disponible): humo real**

```bash
docker compose -f infra/docker-compose.yml up -d rabbitmq
RABBITMQ_TEST_HOST=localhost dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj --filter RabbitMqProyeccionRoundTripTests
```

Expected: PASS (1/1). Si no hay docker en el entorno, anotarlo en el reporte de la tarea (no bloquea).

- [ ] **Step 6: Commit**

```bash
git add services/puntuaciones
git commit -m "test(puntuaciones): e2e de proyeccion y round-trip RabbitMQ opt-in"
```

---

### Task 9: Contract tests de los 2 endpoints

**Files:**
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/RankingContractTests.cs`

**Interfaces:**
- Consumes: endpoints (Task 6), comandos (Tasks 3-4).
- Produces: nada nuevo (fija el shape del contrato HTTP que Task 10 documenta).

- [ ] **Step 1: Escribir los contract tests**

`tests/Umbral.Puntuaciones.ContractTests/RankingContractTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.ContractTests;

public class RankingContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RankingContractTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<(Guid partidaId, Guid juegoId, Guid competidorId)> Sembrar()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidorId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        await sender.Send(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, Guid.NewGuid(), competidorId, 10, 1500, null));
        return (partidaId, juegoId, competidorId);
    }

    [Fact]
    public async Task Ranking_body_matches_contract()
    {
        var (partidaId, juegoId, _) = await Sembrar();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = json.RootElement;
        Assert.Equal(juegoId, root.GetProperty("juegoId").GetGuid());
        Assert.Equal("Trivia", root.GetProperty("tipoJuego").GetString());
        Assert.True(root.TryGetProperty("generadoEn", out _));
        var entrada = root.GetProperty("entradas")[0];
        Assert.Equal(1, entrada.GetProperty("posicion").GetInt32());
        Assert.True(entrada.TryGetProperty("competidorId", out _));
        Assert.Equal("Participante", entrada.GetProperty("tipoCompetidor").GetString());
        Assert.Equal(10, entrada.GetProperty("puntos").GetInt32());
        Assert.Equal(1500, entrada.GetProperty("tiempoAcumuladoMs").GetInt64());
        Assert.Equal(1, entrada.GetProperty("unidadesGanadas").GetInt32());
    }

    [Fact]
    public async Task Marcador_body_matches_contract()
    {
        var (partidaId, juegoId, competidorId) = await Sembrar();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{competidorId}");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = json.RootElement;
        Assert.Equal(competidorId, root.GetProperty("competidorId").GetGuid());
        Assert.Equal("Participante", root.GetProperty("tipoCompetidor").GetString());
        Assert.Equal(10, root.GetProperty("puntos").GetInt32());
        Assert.Equal(1500, root.GetProperty("tiempoAcumuladoMs").GetInt64());
        Assert.Equal(1, root.GetProperty("unidadesGanadas").GetInt32());
        Assert.Equal(1, root.GetProperty("posicion").GetInt32());
    }

    [Fact]
    public async Task Errores_404_devuelven_message_json()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/juegos/{Guid.NewGuid()}/ranking");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(json.RootElement.TryGetProperty("message", out _));
    }
}
```

- [ ] **Step 2: Correr los contract tests y verificar que pasan**

Run: `dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/Umbral.Puntuaciones.ContractTests.csproj`
Expected: PASS (4 tests: health + los 3 nuevos).

- [ ] **Step 3: Commit**

```bash
git add services/puntuaciones
git commit -m "test(puntuaciones): contract tests de ranking y marcador"
```

---

### Task 10: Contratos, service-context, traceability y verificación final

**Files:**
- Modify: `contracts/http/puntuaciones-api.md` (reemplazo completo)
- Modify: `contracts/events/operaciones-sesion-events.md` (sección Transport, bala "Smoke queue")
- Modify: `services/puntuaciones/service-context.md` (reemplazo completo)
- Modify: `docs/04-sdd/traceability-matrix.md` (fila nueva)

**Interfaces:**
- Consumes: todo lo anterior implementado y verde.
- Produces: cierre documental del slice.

- [ ] **Step 1: Reescribir `contracts/http/puntuaciones-api.md`**

Contenido completo:

```markdown
# Puntuaciones HTTP Contract

## Status

Endpoints SP-4a registrados (2): ranking nativo por juego y marcador propio, servidos por las
proyecciones alimentadas por RabbitMQ (best-effort, ADR-0012). Consolidado/team-performance (SP-4b),
SignalR de ranking (SP-4c) y auditoría/historial (SP-4d) pendientes.

## Access Path

Requests enter through the YARP gateway (`/puntuaciones/*` → servicio Puntuaciones, reenvío puro).

## Endpoint Registry

| Capability | Method | Gateway path | Owning service | Status |
|---|---|---|---|---|
| Ranking nativo de un juego | GET | `/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking` | Puntuaciones | Registered (SP-4a) |
| Marcador propio en un juego | GET | `/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{competidorId}` | Puntuaciones | Registered (SP-4a) |

## `GET /puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking`

Ranking nativo del juego (Trivia y BDT usan la misma regla: puntos acumulados DESC, tiempo
acumulado ASC; `unidadesGanadas` — preguntas/etapas ganadas — es informativo, nunca clave de
orden). Empate exacto en ambas claves comparte `posicion` (1, 2, 2, 4). Calculado al leer.

- `200`:

```json
{
  "juegoId": "guid",
  "tipoJuego": "Trivia | BusquedaDelTesoro",
  "generadoEn": "datetime (UTC)",
  "entradas": [
    {
      "posicion": 1,
      "competidorId": "guid",
      "tipoCompetidor": "Participante | Equipo",
      "puntos": 30,
      "tiempoAcumuladoMs": 12345,
      "unidadesGanadas": 3
    }
  ]
}
```

- `404` `{ "message": "..." }`: el juego no existe en la proyección o no pertenece a la partida.
- Juego conocido sin marcadores → `200` con `entradas: []`.
- `competidorId` sigue la identidad dual slice-E: participante en `Individual`, equipo en `Equipo`.

## `GET /puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{competidorId}`

Marcador propio de un competidor con su posición actual (misma regla de orden/empates).

- `200`:

```json
{
  "competidorId": "guid",
  "tipoCompetidor": "Participante | Equipo",
  "puntos": 10,
  "tiempoAcumuladoMs": 1500,
  "unidadesGanadas": 1,
  "posicion": 2
}
```

- `404` `{ "message": "..." }`: juego desconocido, o el competidor no tiene marcador en el juego.

## Autorización

JWT validado (gateway + servicio, defensa en profundidad). Lectura para cualquier rol
autenticado — el ranking es visible para operador y participantes; sin permiso funcional
específico (queries de lectura).
```

- [ ] **Step 2: Actualizar la sección Transport del contrato de eventos**

En `contracts/events/operaciones-sesion-events.md`, reemplazar la bala:

```markdown
- **Smoke queue (SP-3i):** `puntuaciones.operaciones-sesion.all`, durable, binding `operaciones-sesion.#` (Puntuaciones; replaced by finer queues in SP-4).
```

por:

```markdown
- **Projection queue (SP-4a):** `puntuaciones.operaciones-sesion.proyecciones`, durable, bound to the 7 routing keys consumed by the SP-4a projections (`partida-publicada-en-lobby`, `partida-iniciada`, `juego-activado`, `partida-cancelada`, `partida-finalizada`, `puntaje-trivia-incrementado`, `etapa-bdt-ganada`, all `.v1`). Consumed by the real Puntuaciones projection consumer (dedup by `eventId`, ack-always best-effort per ADR-0012). The SP-3i smoke queue `puntuaciones.operaciones-sesion.all` is deleted at consumer startup. Remaining events have no Puntuaciones consumer until SP-4b/4d.
```

- [ ] **Step 3: Reescribir `services/puntuaciones/service-context.md`**

```markdown
# Puntuaciones — service context

Tracks scores and won stages, computes each game's native ranking and the consolidated partida
ranking, team-performance queries, and materializes audit/history. A read/projection model fed by
RabbitMQ domain events, broadcasting via SignalR. Owns neither configuration nor runtime.

Status: SP-4a — real projection consumer (queue `puntuaciones.operaciones-sesion.proyecciones`,
7 bindings, dedup by `eventId`, ADR-0012 best-effort) + projections (`partidas_proyectadas`,
`juegos_proyectados`, `marcadores`, `eventos_procesados` → `umbral_puntuaciones`) + native
per-game ranking and own-marcador HTTP queries (points DESC, time ASC; `unidadesGanadas`
informative only). Pending: consolidated ranking + team performance (SP-4b), live ranking
SignalR (SP-4c), audit/history projection (SP-4d).
```

- [ ] **Step 4: Añadir la fila SP-4a a `docs/04-sdd/traceability-matrix.md`**

Añadir al final de la tabla (una sola fila, mismas 7 columnas que las existentes):

- **Feature:** `Puntuaciones — consumidor real de proyecciones + rankings nativos (SP-4a)`
- **Requirement:** resumen: consumidor RabbitMQ real (cola `puntuaciones.operaciones-sesion.proyecciones`, 7 bindings, dedup por `eventId` transaccional, ack-siempre best-effort ADR-0012, cola de humo SP-3i eliminada); proyecciones `partidas_proyectadas`/`juegos_proyectados`/`marcadores`/`eventos_procesados` con identidad dual (`CompetidorId = equipoId ?? participanteId`) y tolerancia al desorden (stubs + estado monotónico + acumulación conmutativa); ranking nativo on-read (puntos DESC, tiempo ASC, empate exacto comparte posición; `unidadesGanadas` informativo — doctrina BDT por puntos) expuesto en `GET .../ranking` y `GET .../marcadores/{competidorId}` (404 vía middleware).
- **Owning service:** `Puntuaciones`
- **Supporting services:** `Operaciones de Sesión (productor de los 7 eventos consumidos)`
- **SDD folder:** `docs/superpowers/specs/2026-07-04-sp4a-puntuaciones-proyecciones-rankings-design.md · docs/superpowers/plans/2026-07-04-sp4a-puntuaciones-proyecciones-rankings.md`
- **Contracts:** `contracts/http/puntuaciones-api.md · contracts/events/operaciones-sesion-events.md`
- **Status:** `Implemented — <conteos reales de la suite tras Task 9, formato "N unit + N integration + N contract">. Diferido: consolidado+team-performance→SP-4b, SignalR ranking→SP-4c, auditoría/historial→SP-4d, publisher propio de Puntuaciones y outbox→ADR-0012, cableado clientes→SP-5.` (rellenar los conteos con la salida real de `dotnet test`; si el round-trip corrió con broker, anotarlo)

- [ ] **Step 5: Verificación final completa**

```bash
dotnet test services/puntuaciones/Umbral.Puntuaciones.sln
dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln
```

Expected: ambas soluciones verdes (Operaciones no se tocó — es regresión de seguridad; sus conteos de referencia: 356 unit + 29 integration + 48 contract).

Verificar además la ruta del gateway (el spec la da por existente, ADR-0009 — solo confirmar):

```bash
grep -n "puntuaciones" gateway/src/Umbral.Gateway/appsettings.json
```

Expected: route `Match.Path = /puntuaciones/{**catch-all}` y cluster con destino `http://localhost:5030/`. Si no aparece, detenerse y reportar (no improvisar cambios de gateway en este slice).

- [ ] **Step 6: Commit de cierre**

```bash
git add contracts docs services/puntuaciones/service-context.md
git commit -m "docs(puntuaciones): contratos SP-4a, service-context y traceability"
```

---

## Cierre del slice (post-plan)

- Review final whole-branch del rango de commits SP-4a (`superpowers:requesting-code-review`).
- El merge/PR se decide con `superpowers:finishing-a-development-branch`.
- Post-slice: SP-4b (consolidado + team-performance) arranca con partidas/juegos/marcadores proyectados y las queries nativas operativas.






