# SP-4d — Historial/auditoría de partida + historial del participante — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Materializar en Puntuaciones el historial/auditoría de partida (HU-43) consumiendo los 17 eventos de Operaciones de Sesión por una segunda cola dedicada, exponer el historial del operador y el historial de partidas jugadas del participante (HU-27), y saldar dos deudas (retención de `eventos_procesados` y `ArgumentException`→400 sin log).

**Architecture:** Según el design (`docs/superpowers/specs/2026-07-06-sp4d-historial-auditoria-design.md`): tabla genérica `eventos_historial` (fila = registro de dedup, índice único por `EventId`), segundo `BackgroundService` `HistorialEventsConsumer` con cola `puntuaciones.operaciones-sesion.historial` y binding `operaciones-sesion.#` (la cola de proyecciones y su pipeline SP-4c **no se tocan**), un único comando genérico `ProyectarEventoHistorialCommand`, muestreo de `UbicacionActualizada` (1 por participante/partida por minuto) al escribir, y dos GETs: historial de partida (solo `Operador`/`Administrador`) e historial-partidas del participante (reusa `CalculadorRankingConsolidado`).

**Tech Stack:** .NET 8, EF Core + Npgsql (columna `jsonb`), RabbitMQ.Client (patrón del consumidor SP-4a), MediatR, xUnit con fakes a mano (sin Moq).

## Global Constraints

- Rama: `feature/sp-4d-historial` (ya creada desde `feature/sp-4c-signalr-ranking`; la serie SP-4 no se integra a develop).
- Estructura graduada CLAUDE.md: entidad e interfaces de repositorio en `Domain/`, comando/handler/queries/DTOs/excepciones en las carpetas mandadas de `Application/`, EF en `Infrastructure/Persistence/`, consumidor/mapper/purga en `Api/Workers/`; controllers heredan `ControllerBase`, despachan por `ISender`, sin lógica de negocio, **todo controller con unit tests**.
- **Sin cambios** en: gateway, eventos/payloads publicados por Operaciones de Sesión, cola/bindings de proyecciones (`RabbitMqConsumerOptions` intacto), pipeline SP-4c.
- Application **no** referencia EF Core: la `DbUpdateException` por violación de unicidad la trata el consumidor en `Api/Workers/` (paridad con el patrón SP-4a/4b).
- Nombres exactos del contrato: cola `puntuaciones.operaciones-sesion.historial`, binding `operaciones-sesion.#`, exchange `umbral.operaciones-sesion`; rutas `GET /puntuaciones/partidas/{partidaId}/historial` y `GET /puntuaciones/participantes/{participanteId}/historial-partidas`.
- Muestreo de ubicaciones: ventana de **60 segundos** por `(PartidaId, ParticipanteId)`, solo tipo `UbicacionActualizada`; best-effort (carrera entre consumidores aceptable).
- Autorización: historial de partida `[Authorize(Roles = "Operador,Administrador")]` (participante → 403); historial-partidas cualquier rol autenticado.
- Retención: `Retencion:EventosProcesadosDias` default **30**; la purga **jamás** toca `eventos_historial` (RB-31).
- Comandos de test: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"` (base actual **119/119**). Los tests opt-in de RabbitMQ solo corren con Docker disponible.
- Vocabulario en español del dominio; comentarios de código en español, estilo del servicio.

---

### Task 1: Entidad `EventoHistorial` + `IHistorialRepository` + migración `SP4dHistorial`

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Entities/EventoHistorial.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/IHistorialRepository.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/ParticipacionEquipoHistorial.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/HistorialRepository.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/PuntuacionesDbContext.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/DependencyInjection.cs`
- Create (generada): `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/Migrations/*_SP4dHistorial.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/HistorialRepositoryTests.cs`

**Interfaces:**
- Consumes: `PuntuacionesDbContext` y `EventoProcesado` existentes.
- Produces: `EventoHistorial.Registrar(Guid eventId, Guid partidaId, Guid? juegoId, string tipoEvento, DateTime occurredAt, Guid? participanteId, Guid? equipoId, string detalleJson)`; `IHistorialRepository` con `ExisteEventoAsync(Guid, CancellationToken)`, `ExisteUbicacionCercanaAsync(Guid partidaId, Guid participanteId, DateTime occurredAt, TimeSpan ventana, CancellationToken)`, `AddEvento(EventoHistorial)`, `ContarHistorialDePartidaAsync(Guid, string?, CancellationToken)`, `GetHistorialDePartidaAsync(Guid, string?, int limit, int offset, CancellationToken)`, `GetEquiposDelParticipanteAsync(Guid, CancellationToken)`; record `ParticipacionEquipoHistorial(Guid PartidaId, Guid EquipoId)`. Índice único `eventid` (dedup del historial) e índice `(partidaid, occurredat)`; índice nuevo `procesadoat` en `eventos_procesados` (lo usa la Task 9).

- [ ] **Step 1: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/HistorialRepositoryTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Infrastructure.Persistence;

namespace Umbral.Puntuaciones.IntegrationTests;

public class HistorialRepositoryTests
{
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    private static DbContextOptions<PuntuacionesDbContext> Opciones(string db)
        => new DbContextOptionsBuilder<PuntuacionesDbContext>().UseInMemoryDatabase(db).Options;

    private static EventoHistorial Evento(
        Guid partidaId, string tipo, DateTime occurredAt,
        Guid? participanteId = null, Guid? equipoId = null, Guid? juegoId = null)
        => EventoHistorial.Registrar(
            Guid.NewGuid(), partidaId, juegoId, tipo, occurredAt, participanteId, equipoId, "{}");

    [Fact]
    public async Task Inserta_y_lee_historial_de_partida_en_orden_cronologico()
    {
        var opciones = Opciones($"historial-{Guid.NewGuid()}");
        var partidaId = Guid.NewGuid();
        await using (var db = new PuntuacionesDbContext(opciones))
        {
            var repo = new HistorialRepository(db);
            repo.AddEvento(Evento(partidaId, "PartidaIniciada", Ahora.AddMinutes(1)));
            repo.AddEvento(Evento(partidaId, "PartidaPublicadaEnLobby", Ahora));
            repo.AddEvento(Evento(Guid.NewGuid(), "PartidaIniciada", Ahora));
            await db.SaveChangesAsync();
        }

        await using var lectura = new PuntuacionesDbContext(opciones);
        var repoLectura = new HistorialRepository(lectura);
        var entradas = await repoLectura.GetHistorialDePartidaAsync(partidaId, null, 100, 0, CancellationToken.None);
        var total = await repoLectura.ContarHistorialDePartidaAsync(partidaId, null, CancellationToken.None);

        Assert.Equal(2, total);
        Assert.Equal(new[] { "PartidaPublicadaEnLobby", "PartidaIniciada" },
            entradas.Select(e => e.TipoEvento).ToArray());
    }

    [Fact]
    public async Task Paginacion_y_filtro_por_tipo()
    {
        var opciones = Opciones($"historial-{Guid.NewGuid()}");
        var partidaId = Guid.NewGuid();
        await using (var db = new PuntuacionesDbContext(opciones))
        {
            var repo = new HistorialRepository(db);
            for (var i = 0; i < 5; i++)
            {
                repo.AddEvento(Evento(partidaId, "UbicacionActualizada", Ahora.AddMinutes(i)));
            }
            repo.AddEvento(Evento(partidaId, "EtapaBDTGanada", Ahora.AddMinutes(9)));
            await db.SaveChangesAsync();
        }

        await using var lectura = new PuntuacionesDbContext(opciones);
        var repoLectura = new HistorialRepository(lectura);

        var pagina = await repoLectura.GetHistorialDePartidaAsync(partidaId, null, 2, 2, CancellationToken.None);
        Assert.Equal(2, pagina.Count);
        Assert.Equal(Ahora.AddMinutes(2), pagina[0].OccurredAt);

        var filtrado = await repoLectura.GetHistorialDePartidaAsync(partidaId, "EtapaBDTGanada", 100, 0, CancellationToken.None);
        Assert.Single(filtrado);
        Assert.Equal(1, await repoLectura.ContarHistorialDePartidaAsync(partidaId, "EtapaBDTGanada", CancellationToken.None));
    }

    [Fact]
    public void El_modelo_define_indice_unico_por_EventId()
    {
        // El proveedor InMemory no aplica índices únicos no-PK entre contextos: la aplicación real
        // la garantiza PostgreSQL con el DDL de la migración (misma doctrina que xmin, solo-Npgsql).
        // Aquí se protege la configuración del modelo contra regresiones.
        using var db = new PuntuacionesDbContext(Opciones($"historial-{Guid.NewGuid()}"));

        var indice = db.Model.FindEntityType(typeof(EventoHistorial))!
            .GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(EventoHistorial.EventId));

        Assert.True(indice.IsUnique);
    }

    [Fact]
    public async Task ExisteUbicacionCercana_detecta_solo_misma_partida_y_participante_dentro_de_la_ventana()
    {
        var opciones = Opciones($"historial-{Guid.NewGuid()}");
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        await using (var db = new PuntuacionesDbContext(opciones))
        {
            new HistorialRepository(db).AddEvento(
                Evento(partidaId, "UbicacionActualizada", Ahora, participanteId));
            await db.SaveChangesAsync();
        }

        await using var lectura = new PuntuacionesDbContext(opciones);
        var repo = new HistorialRepository(lectura);
        var ventana = TimeSpan.FromSeconds(60);

        Assert.True(await repo.ExisteUbicacionCercanaAsync(partidaId, participanteId, Ahora.AddSeconds(30), ventana, CancellationToken.None));
        Assert.False(await repo.ExisteUbicacionCercanaAsync(partidaId, participanteId, Ahora.AddSeconds(90), ventana, CancellationToken.None));
        Assert.False(await repo.ExisteUbicacionCercanaAsync(partidaId, Guid.NewGuid(), Ahora.AddSeconds(30), ventana, CancellationToken.None));
        Assert.False(await repo.ExisteUbicacionCercanaAsync(Guid.NewGuid(), participanteId, Ahora.AddSeconds(30), ventana, CancellationToken.None));
    }

    [Fact]
    public async Task GetEquiposDelParticipante_resuelve_membresia_excluyendo_ConvocatoriaCreada()
    {
        var opciones = Opciones($"historial-{Guid.NewGuid()}");
        var participanteId = Guid.NewGuid();
        var partidaJugada = Guid.NewGuid();
        var partidaRechazada = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        await using (var db = new PuntuacionesDbContext(opciones))
        {
            var repo = new HistorialRepository(db);
            // Dos acciones de juego en la misma partida → una sola participación (Distinct).
            repo.AddEvento(Evento(partidaJugada, "RespuestaTriviaValidada", Ahora, participanteId, equipoId));
            repo.AddEvento(Evento(partidaJugada, "TesoroQRValidado", Ahora.AddMinutes(1), participanteId, equipoId));
            // Convocatoria sin acción de juego → no cuenta como participación.
            repo.AddEvento(Evento(partidaRechazada, "ConvocatoriaCreada", Ahora, participanteId, equipoId));
            // Evento sin equipo → no cuenta.
            repo.AddEvento(Evento(partidaJugada, "UbicacionActualizada", Ahora, participanteId));
            await db.SaveChangesAsync();
        }

        await using var lectura = new PuntuacionesDbContext(opciones);
        var participaciones = await new HistorialRepository(lectura)
            .GetEquiposDelParticipanteAsync(participanteId, CancellationToken.None);

        var participacion = Assert.Single(participaciones);
        Assert.Equal(partidaJugada, participacion.PartidaId);
        Assert.Equal(equipoId, participacion.EquipoId);
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj" --filter HistorialRepositoryTests`
Expected: FAIL de compilación (`EventoHistorial`, `HistorialRepository`, `EventosHistorial` no existen).

- [ ] **Step 3: Implementar entidad, record, interfaz, repositorio y mapeo EF**

`services/puntuaciones/src/Umbral.Puntuaciones.Domain/Entities/EventoHistorial.cs`:

```csharp
namespace Umbral.Puntuaciones.Domain.Entities;

// Registro de auditoría/historial de partida (RB-15, RB-31): una fila por evento de dominio
// recibido de Operaciones de Sesión. La propia fila es el registro de dedup del historial
// (índice único por EventId); jamás se purga.
public sealed class EventoHistorial
{
    private EventoHistorial(
        Guid eventId, Guid partidaId, Guid? juegoId, string tipoEvento,
        DateTime occurredAt, Guid? participanteId, Guid? equipoId, string detalleJson)
    {
        EventId = eventId;
        PartidaId = partidaId;
        JuegoId = juegoId;
        TipoEvento = tipoEvento;
        OccurredAt = occurredAt;
        ParticipanteId = participanteId;
        EquipoId = equipoId;
        DetalleJson = detalleJson;
    }

    public long Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid PartidaId { get; private set; }
    public Guid? JuegoId { get; private set; }
    public string TipoEvento { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public Guid? ParticipanteId { get; private set; }
    public Guid? EquipoId { get; private set; }
    public string DetalleJson { get; private set; }

    public static EventoHistorial Registrar(
        Guid eventId, Guid partidaId, Guid? juegoId, string tipoEvento,
        DateTime occurredAt, Guid? participanteId, Guid? equipoId, string detalleJson)
        => new(eventId, partidaId, juegoId, tipoEvento, occurredAt, participanteId, equipoId, detalleJson);
}
```

`services/puntuaciones/src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/ParticipacionEquipoHistorial.cs`:

```csharp
namespace Umbral.Puntuaciones.Domain.Abstractions.Persistence;

// Membresía de equipo resuelta del historial (HU-27): partida donde el participante
// autoró una acción de juego acreditada a un equipo.
public sealed record ParticipacionEquipoHistorial(Guid PartidaId, Guid EquipoId);
```

`services/puntuaciones/src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/IHistorialRepository.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Domain.Abstractions.Persistence;

public interface IHistorialRepository
{
    Task<bool> ExisteEventoAsync(Guid eventId, CancellationToken cancellationToken);
    Task<bool> ExisteUbicacionCercanaAsync(
        Guid partidaId, Guid participanteId, DateTime occurredAt, TimeSpan ventana, CancellationToken cancellationToken);
    void AddEvento(EventoHistorial evento);
    Task<int> ContarHistorialDePartidaAsync(Guid partidaId, string? tipoEvento, CancellationToken cancellationToken);
    Task<IReadOnlyList<EventoHistorial>> GetHistorialDePartidaAsync(
        Guid partidaId, string? tipoEvento, int limit, int offset, CancellationToken cancellationToken);
    Task<IReadOnlyList<ParticipacionEquipoHistorial>> GetEquiposDelParticipanteAsync(
        Guid participanteId, CancellationToken cancellationToken);
}
```

`services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/HistorialRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Infrastructure.Persistence;

public sealed class HistorialRepository : IHistorialRepository
{
    private const string TipoUbicacion = "UbicacionActualizada";
    private const string TipoConvocatoriaCreada = "ConvocatoriaCreada";

    private readonly PuntuacionesDbContext _db;

    public HistorialRepository(PuntuacionesDbContext db) => _db = db;

    public Task<bool> ExisteEventoAsync(Guid eventId, CancellationToken cancellationToken)
        => _db.EventosHistorial.AsNoTracking().AnyAsync(e => e.EventId == eventId, cancellationToken);

    public Task<bool> ExisteUbicacionCercanaAsync(
        Guid partidaId, Guid participanteId, DateTime occurredAt, TimeSpan ventana, CancellationToken cancellationToken)
    {
        var desde = occurredAt - ventana;
        var hasta = occurredAt + ventana;
        return _db.EventosHistorial.AsNoTracking().AnyAsync(
            e => e.TipoEvento == TipoUbicacion
                && e.PartidaId == partidaId
                && e.ParticipanteId == participanteId
                && e.OccurredAt > desde
                && e.OccurredAt < hasta,
            cancellationToken);
    }

    public void AddEvento(EventoHistorial evento) => _db.EventosHistorial.Add(evento);

    public Task<int> ContarHistorialDePartidaAsync(Guid partidaId, string? tipoEvento, CancellationToken cancellationToken)
        => FiltrarPorPartida(partidaId, tipoEvento).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<EventoHistorial>> GetHistorialDePartidaAsync(
        Guid partidaId, string? tipoEvento, int limit, int offset, CancellationToken cancellationToken)
        => await FiltrarPorPartida(partidaId, tipoEvento)
            .OrderBy(e => e.OccurredAt)
            .ThenBy(e => e.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

    // Membresía HU-27: acciones de juego autoradas acreditadas a un equipo. ConvocatoriaCreada se
    // excluye para no listar convocados que rechazaron (design SP-4d §4).
    public async Task<IReadOnlyList<ParticipacionEquipoHistorial>> GetEquiposDelParticipanteAsync(
        Guid participanteId, CancellationToken cancellationToken)
    {
        var filas = await _db.EventosHistorial.AsNoTracking()
            .Where(e => e.ParticipanteId == participanteId
                && e.EquipoId != null
                && e.TipoEvento != TipoConvocatoriaCreada)
            .Select(e => new { e.PartidaId, e.EquipoId })
            .Distinct()
            .ToListAsync(cancellationToken);
        return filas.Select(f => new ParticipacionEquipoHistorial(f.PartidaId, f.EquipoId!.Value)).ToList();
    }

    private IQueryable<EventoHistorial> FiltrarPorPartida(Guid partidaId, string? tipoEvento)
    {
        var query = _db.EventosHistorial.AsNoTracking().Where(e => e.PartidaId == partidaId);
        return tipoEvento is null ? query : query.Where(e => e.TipoEvento == tipoEvento);
    }
}
```

En `PuntuacionesDbContext.cs`, agregar el DbSet debajo de `EventosProcesados`:

```csharp
    public DbSet<EventoHistorial> EventosHistorial => Set<EventoHistorial>();
```

y dentro de `OnModelCreating`, después del bloque de `EventoProcesado`: (1) el índice de retención en `eventos_procesados` (dentro del bloque existente de `EventoProcesado`):

```csharp
            entity.HasIndex(x => x.ProcesadoAt).HasDatabaseName("ix_eventos_procesados_procesadoat");
```

y (2) el bloque nuevo de `EventoHistorial`:

```csharp
        modelBuilder.Entity<EventoHistorial>(entity =>
        {
            entity.ToTable("eventos_historial");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(x => x.EventId).HasColumnName("eventid").IsRequired();
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.JuegoId).HasColumnName("juegoid");
            entity.Property(x => x.TipoEvento).HasColumnName("tipoevento").HasMaxLength(64).IsRequired();
            entity.Property(x => x.OccurredAt).HasColumnName("occurredat").IsRequired();
            entity.Property(x => x.ParticipanteId).HasColumnName("participanteid");
            entity.Property(x => x.EquipoId).HasColumnName("equipoid");
            // jsonb es anotación relacional: Npgsql la aplica, InMemory la ignora.
            entity.Property(x => x.DetalleJson).HasColumnName("detalle").HasColumnType("jsonb").IsRequired();
            entity.HasIndex(x => x.EventId).IsUnique().HasDatabaseName("ix_eventos_historial_eventid");
            entity.HasIndex(x => new { x.PartidaId, x.OccurredAt }).HasDatabaseName("ix_eventos_historial_partidaid_occurredat");
        });
```

En `Infrastructure/DependencyInjection.cs`, registrar el repositorio junto a los existentes:

```csharp
        services.AddScoped<IHistorialRepository, HistorialRepository>();
```

- [ ] **Step 4: Correr los tests para verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj" --filter HistorialRepositoryTests`
Expected: PASS (5 tests).

- [ ] **Step 5: Generar la migración `SP4dHistorial`**

Run (desde la raíz del repo; no necesita Postgres corriendo — usa el design-time factory):

```powershell
dotnet ef migrations add SP4dHistorial --project services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure --output-dir Persistence/Migrations
```

Inspeccionar la migración generada: debe crear `eventos_historial` (con `id` identity, `detalle` de tipo `jsonb`, índice único `ix_eventos_historial_eventid`, índice `ix_eventos_historial_partidaid_occurredat`) y el índice `ix_eventos_procesados_procesadoat` sobre `eventos_procesados`. Nada más.

- [ ] **Step 6: Correr la suite completa y commitear**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: todos verdes (119 previos + 5 nuevos).

```bash
git add services/puntuaciones docs/superpowers
git commit -m "feat(puntuaciones): entidad y repositorio de eventos_historial + migracion SP4dHistorial (SP-4d)"
```

---

### Task 2: `ProyectarEventoHistorialCommand` + handler (dedup + muestreo)

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarEventoHistorialCommand.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarEventoHistorialCommandHandler.cs`
- Create (fake): `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/Fakes/FakeHistorialRepository.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ProyectarEventoHistorialCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IHistorialRepository`, `IPuntuacionesUnitOfWork`, `EventoHistorial.Registrar(...)` (Task 1); `FakePuntuacionesUnitOfWork` existente.
- Produces: `ProyectarEventoHistorialCommand(Guid EventId, string TipoEvento, DateTime OccurredAt, Guid PartidaId, Guid? JuegoId, Guid? ParticipanteId, Guid? EquipoId, string DetalleJson) : IRequest` — lo construye el mapper (Task 3) y lo despacha el consumidor (Task 4). `FakeHistorialRepository` lo reusan las Tasks 5 y 7.

- [ ] **Step 1: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/Fakes/FakeHistorialRepository.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.UnitTests.Application.Fakes;

public sealed class FakeHistorialRepository : IHistorialRepository
{
    public List<EventoHistorial> Eventos { get; } = new();

    public Task<bool> ExisteEventoAsync(Guid eventId, CancellationToken cancellationToken)
        => Task.FromResult(Eventos.Any(e => e.EventId == eventId));

    public Task<bool> ExisteUbicacionCercanaAsync(
        Guid partidaId, Guid participanteId, DateTime occurredAt, TimeSpan ventana, CancellationToken cancellationToken)
        => Task.FromResult(Eventos.Any(e => e.TipoEvento == "UbicacionActualizada"
            && e.PartidaId == partidaId
            && e.ParticipanteId == participanteId
            && e.OccurredAt > occurredAt - ventana
            && e.OccurredAt < occurredAt + ventana));

    public void AddEvento(EventoHistorial evento) => Eventos.Add(evento);

    public Task<int> ContarHistorialDePartidaAsync(Guid partidaId, string? tipoEvento, CancellationToken cancellationToken)
        => Task.FromResult(Filtrar(partidaId, tipoEvento).Count());

    public Task<IReadOnlyList<EventoHistorial>> GetHistorialDePartidaAsync(
        Guid partidaId, string? tipoEvento, int limit, int offset, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<EventoHistorial>>(Filtrar(partidaId, tipoEvento)
            .OrderBy(e => e.OccurredAt)
            .Skip(offset)
            .Take(limit)
            .ToList());

    public Task<IReadOnlyList<ParticipacionEquipoHistorial>> GetEquiposDelParticipanteAsync(
        Guid participanteId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ParticipacionEquipoHistorial>>(Eventos
            .Where(e => e.ParticipanteId == participanteId
                && e.EquipoId != null
                && e.TipoEvento != "ConvocatoriaCreada")
            .Select(e => new ParticipacionEquipoHistorial(e.PartidaId, e.EquipoId!.Value))
            .Distinct()
            .ToList());

    private IEnumerable<EventoHistorial> Filtrar(Guid partidaId, string? tipoEvento)
        => Eventos.Where(e => e.PartidaId == partidaId && (tipoEvento == null || e.TipoEvento == tipoEvento));
}
```

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ProyectarEventoHistorialCommandHandlerTests.cs`:

```csharp
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.Handlers.Commands;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ProyectarEventoHistorialCommandHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeHistorialRepository _repo = new();
    private readonly FakePuntuacionesUnitOfWork _uow = new();

    private ProyectarEventoHistorialCommandHandler Handler() => new(_repo, _uow);

    private static ProyectarEventoHistorialCommand Comando(
        string tipo = "EtapaBDTGanada",
        Guid? eventId = null, Guid? partidaId = null, Guid? juegoId = null,
        Guid? participanteId = null, Guid? equipoId = null,
        DateTime? occurredAt = null, string detalle = """{"puntaje":10}""")
        => new(eventId ?? Guid.NewGuid(), tipo, occurredAt ?? Ahora, partidaId ?? Guid.NewGuid(),
            juegoId, participanteId, equipoId, detalle);

    [Fact]
    public async Task Inserta_la_fila_con_todos_los_campos()
    {
        var comando = Comando(
            juegoId: Guid.NewGuid(), participanteId: Guid.NewGuid(), equipoId: Guid.NewGuid());

        await Handler().Handle(comando, CancellationToken.None);

        var evento = Assert.Single(_repo.Eventos);
        Assert.Equal(comando.EventId, evento.EventId);
        Assert.Equal(comando.PartidaId, evento.PartidaId);
        Assert.Equal(comando.JuegoId, evento.JuegoId);
        Assert.Equal("EtapaBDTGanada", evento.TipoEvento);
        Assert.Equal(Ahora, evento.OccurredAt);
        Assert.Equal(comando.ParticipanteId, evento.ParticipanteId);
        Assert.Equal(comando.EquipoId, evento.EquipoId);
        Assert.Equal("""{"puntaje":10}""", evento.DetalleJson);
        Assert.Equal(1, _uow.Saves);
    }

    [Fact]
    public async Task EventId_duplicado_no_inserta_segunda_fila()
    {
        var eventId = Guid.NewGuid();
        await Handler().Handle(Comando(eventId: eventId), CancellationToken.None);

        await Handler().Handle(Comando(eventId: eventId), CancellationToken.None);

        Assert.Single(_repo.Eventos);
        Assert.Equal(1, _uow.Saves);
    }

    [Fact]
    public async Task Ubicacion_a_menos_de_60s_del_mismo_participante_y_partida_se_descarta()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora), CancellationToken.None);

        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora.AddSeconds(30)), CancellationToken.None);

        Assert.Single(_repo.Eventos);
    }

    [Fact]
    public async Task Ubicacion_a_60s_o_mas_se_guarda()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora), CancellationToken.None);

        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora.AddSeconds(60)), CancellationToken.None);

        Assert.Equal(2, _repo.Eventos.Count);
    }

    [Fact]
    public async Task Ubicacion_de_otro_participante_o_partida_se_guarda()
    {
        var partidaId = Guid.NewGuid();
        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: Guid.NewGuid(), occurredAt: Ahora), CancellationToken.None);

        await Handler().Handle(Comando("UbicacionActualizada",
            partidaId: partidaId, participanteId: Guid.NewGuid(), occurredAt: Ahora.AddSeconds(10)), CancellationToken.None);

        Assert.Equal(2, _repo.Eventos.Count);
    }

    [Fact]
    public async Task Otros_tipos_no_se_muestrean()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        await Handler().Handle(Comando("RespuestaTriviaValidada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora), CancellationToken.None);

        await Handler().Handle(Comando("RespuestaTriviaValidada",
            partidaId: partidaId, participanteId: participanteId, occurredAt: Ahora.AddSeconds(5)), CancellationToken.None);

        Assert.Equal(2, _repo.Eventos.Count);
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter ProyectarEventoHistorialCommandHandlerTests`
Expected: FAIL de compilación (`ProyectarEventoHistorialCommand` no existe).

- [ ] **Step 3: Implementar comando y handler**

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarEventoHistorialCommand.cs`:

```csharp
using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

// Comando genérico del historial (SP-4d): una fila por evento del contrato, ids extraídos por el
// mapper y el resto del payload resumido en DetalleJson.
public sealed record ProyectarEventoHistorialCommand(
    Guid EventId, string TipoEvento, DateTime OccurredAt, Guid PartidaId,
    Guid? JuegoId, Guid? ParticipanteId, Guid? EquipoId, string DetalleJson) : IRequest;
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarEventoHistorialCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

// Dedup por EventId contra la propia tabla (sin tocar eventos_procesados, que pertenece al
// consumidor de proyecciones). Muestreo de ubicaciones: cota de volumen best-effort, no invariante
// (la carrera residual del check-then-insert la cubre el índice único; la trata el consumidor).
public sealed class ProyectarEventoHistorialCommandHandler : IRequestHandler<ProyectarEventoHistorialCommand>
{
    private const string TipoUbicacion = "UbicacionActualizada";
    private static readonly TimeSpan VentanaMuestreoUbicacion = TimeSpan.FromSeconds(60);

    private readonly IHistorialRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarEventoHistorialCommandHandler(IHistorialRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarEventoHistorialCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.ExisteEventoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        if (request.TipoEvento == TipoUbicacion
            && request.ParticipanteId is { } participanteId
            && await _repo.ExisteUbicacionCercanaAsync(
                request.PartidaId, participanteId, request.OccurredAt, VentanaMuestreoUbicacion, cancellationToken))
        {
            return;
        }

        _repo.AddEvento(EventoHistorial.Registrar(
            request.EventId, request.PartidaId, request.JuegoId, request.TipoEvento,
            request.OccurredAt, request.ParticipanteId, request.EquipoId, request.DetalleJson));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Correr los tests para verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter ProyectarEventoHistorialCommandHandlerTests`
Expected: PASS (6 tests).

- [ ] **Step 5: Suite completa y commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: todos verdes.

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): comando generico de historial con dedup y muestreo de ubicaciones (SP-4d)"
```

---

### Task 3: `HistorialEventMapper` — los 17 tipos del contrato

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/HistorialEventMapper.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/HistorialEventMapperTests.cs`

**Interfaces:**
- Consumes: `EnvelopeResumen(Guid EventId, string EventType, int Version, DateTime OccurredAt, JsonElement Payload)` existente; `ProyectarEventoHistorialCommand` (Task 2).
- Produces: `HistorialEventMapper.Map(EnvelopeResumen) : ProyectarEventoHistorialCommand?` — tipo desconocido o payload sin `partidaId` válido → `null` (warn + ack en el consumidor, Task 4). Extracción por tipo: `juegoId` cuando el payload lo trae; autor real desde `participanteId`/`usuarioId`/`ganadorParticipanteId`/`participanteDestinoId`; equipo desde `equipoId`/`ganadorEquipoId`/`equipoDestinoId`; `DetalleJson` = resto del payload sin `partidaId`/`sesionPartidaId`/`juegoId` ni los ids extraídos.

- [ ] **Step 1: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/HistorialEventMapperTests.cs`:

```csharp
using System.Text.Json;
using Umbral.Puntuaciones.Api.Workers;

namespace Umbral.Puntuaciones.UnitTests.Workers;

public class HistorialEventMapperTests
{
    private static readonly Guid PartidaId = Guid.NewGuid();
    private static readonly Guid JuegoId = Guid.NewGuid();
    private static readonly Guid PersonaId = Guid.NewGuid();
    private static readonly Guid EquipoId = Guid.NewGuid();

    private static EnvelopeResumen Envelope(string tipo, string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        return new EnvelopeResumen(
            Guid.NewGuid(), tipo, 1, new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc),
            doc.RootElement.Clone());
    }

    // Un caso por tipo del contrato: payload de ejemplo + ids esperados + claves esperadas del detalle.
    public static TheoryData<string, string, Guid?, Guid?, Guid?, string[]> Casos()
    {
        var sesion = Guid.NewGuid();
        var data = new TheoryData<string, string, Guid?, Guid?, Guid?, string[]>
        {
            { "PartidaPublicadaEnLobby",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","modalidad":"Equipo","minimosParticipacion":1,"maximosParticipacion":10}""",
              null, null, null, new[] { "modalidad", "minimosParticipacion", "maximosParticipacion" } },
            { "PartidaIniciada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","fechaInicio":"2026-07-06T12:00:00Z","primerJuegoId":"{{JuegoId}}","primerJuegoOrden":1}""",
              null, null, null, new[] { "fechaInicio", "primerJuegoId", "primerJuegoOrden" } },
            { "JuegoActivado",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","orden":1,"tipoJuego":"Trivia"}""",
              JuegoId, null, null, new[] { "orden", "tipoJuego" } },
            { "PartidaCancelada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","motivo":"MinimosNoAlcanzados","fechaCancelacion":"2026-07-06T12:00:00Z"}""",
              null, null, null, new[] { "motivo", "fechaCancelacion" } },
            { "PartidaFinalizada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","fechaFin":"2026-07-06T12:00:00Z"}""",
              null, null, null, new[] { "fechaFin" } },
            { "RespuestaTriviaValidada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","preguntaId":"{{Guid.NewGuid()}}","participanteId":"{{PersonaId}}","opcionId":"{{Guid.NewGuid()}}","esCorrecta":true,"instante":"2026-07-06T12:00:00Z","equipoId":"{{EquipoId}}"}""",
              JuegoId, PersonaId, EquipoId, new[] { "preguntaId", "opcionId", "esCorrecta", "instante" } },
            { "PuntajeTriviaIncrementado",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","preguntaId":"{{Guid.NewGuid()}}","participanteId":"{{PersonaId}}","puntaje":10,"tiempoRespuestaMs":1234,"equipoId":null}""",
              JuegoId, PersonaId, null, new[] { "preguntaId", "puntaje", "tiempoRespuestaMs" } },
            { "PreguntaTriviaActivada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","preguntaId":"{{Guid.NewGuid()}}","orden":1,"tiempoLimiteSegundos":30,"fechaActivacion":"2026-07-06T12:00:00Z"}""",
              JuegoId, null, null, new[] { "preguntaId", "orden", "tiempoLimiteSegundos", "fechaActivacion" } },
            { "PreguntaTriviaCerrada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","preguntaId":"{{Guid.NewGuid()}}","motivo":"RespuestaCorrecta","fechaCierre":"2026-07-06T12:00:00Z","ganadorParticipanteId":"{{PersonaId}}","ganadorEquipoId":"{{EquipoId}}"}""",
              JuegoId, PersonaId, EquipoId, new[] { "preguntaId", "motivo", "fechaCierre" } },
            { "TesoroQRValidado",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","etapaId":"{{Guid.NewGuid()}}","participanteId":"{{PersonaId}}","resultado":"Valido","instante":"2026-07-06T12:00:00Z","equipoId":null}""",
              JuegoId, PersonaId, null, new[] { "etapaId", "resultado", "instante" } },
            { "EtapaBDTGanada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","etapaId":"{{Guid.NewGuid()}}","participanteId":"{{PersonaId}}","puntaje":10,"tiempoResolucionMs":1234,"equipoId":"{{EquipoId}}"}""",
              JuegoId, PersonaId, EquipoId, new[] { "etapaId", "puntaje", "tiempoResolucionMs" } },
            { "EtapaBDTCerrada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","etapaId":"{{Guid.NewGuid()}}","motivo":"Tiempo","fechaCierre":"2026-07-06T12:00:00Z"}""",
              JuegoId, null, null, new[] { "etapaId", "motivo", "fechaCierre" } },
            { "EtapaBDTActivada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","etapaId":"{{Guid.NewGuid()}}","orden":1,"tiempoLimiteSegundos":60,"fechaActivacion":"2026-07-06T12:00:00Z"}""",
              JuegoId, null, null, new[] { "etapaId", "orden", "tiempoLimiteSegundos", "fechaActivacion" } },
            { "PistaEnviada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","juegoId":"{{JuegoId}}","participanteDestinoId":"{{PersonaId}}","texto":"cerca de la fuente","instante":"2026-07-06T12:00:00Z","equipoDestinoId":null}""",
              JuegoId, PersonaId, null, new[] { "texto", "instante" } },
            { "ConvocatoriaCreada",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","convocatoriaId":"{{Guid.NewGuid()}}","equipoId":"{{EquipoId}}","usuarioId":"{{PersonaId}}"}""",
              null, PersonaId, EquipoId, new[] { "convocatoriaId" } },
            { "ConvocatoriaRespondida",
              $$"""{"partidaId":"{{PartidaId}}","sesionPartidaId":"{{sesion}}","convocatoriaId":"{{Guid.NewGuid()}}","usuarioId":"{{PersonaId}}","estadoConvocatoria":"Aceptada"}""",
              null, PersonaId, null, new[] { "convocatoriaId", "estadoConvocatoria" } },
            { "UbicacionActualizada",
              $$"""{"partidaId":"{{PartidaId}}","participanteId":"{{PersonaId}}","latitud":10.5,"longitud":-66.9,"instante":"2026-07-06T12:00:00Z"}""",
              null, PersonaId, null, new[] { "latitud", "longitud", "instante" } },
        };
        return data;
    }

    [Theory]
    [MemberData(nameof(Casos))]
    public void Mapea_cada_tipo_con_ids_y_detalle(
        string tipo, string payload, Guid? juegoId, Guid? participanteId, Guid? equipoId, string[] clavesDetalle)
    {
        var envelope = Envelope(tipo, payload);

        var comando = HistorialEventMapper.Map(envelope);

        Assert.NotNull(comando);
        Assert.Equal(envelope.EventId, comando!.EventId);
        Assert.Equal(tipo, comando.TipoEvento);
        Assert.Equal(envelope.OccurredAt, comando.OccurredAt);
        Assert.Equal(PartidaId, comando.PartidaId);
        Assert.Equal(juegoId, comando.JuegoId);
        Assert.Equal(participanteId, comando.ParticipanteId);
        Assert.Equal(equipoId, comando.EquipoId);
        using var detalle = JsonDocument.Parse(comando.DetalleJson);
        var claves = detalle.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();
        Assert.Equal(clavesDetalle.OrderBy(n => n).ToArray(), claves);
    }

    [Fact]
    public void Tipo_desconocido_devuelve_null()
        => Assert.Null(HistorialEventMapper.Map(Envelope("EventoInventado", $$"""{"partidaId":"{{PartidaId}}"}""")));

    [Fact]
    public void Payload_sin_partidaId_devuelve_null()
        => Assert.Null(HistorialEventMapper.Map(Envelope("PartidaIniciada", """{"fechaInicio":"2026-07-06T12:00:00Z"}""")));

    [Fact]
    public void PartidaId_no_guid_devuelve_null()
        => Assert.Null(HistorialEventMapper.Map(Envelope("PartidaIniciada", """{"partidaId":"no-es-guid"}""")));
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter HistorialEventMapperTests`
Expected: FAIL de compilación (`HistorialEventMapper` no existe).

- [ ] **Step 3: Implementar el mapper**

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/HistorialEventMapper.cs`:

```csharp
using System.Text.Json;
using Umbral.Puntuaciones.Application.Commands;

namespace Umbral.Puntuaciones.Api.Workers;

// Traduce cualquier evento del contrato al comando genérico de historial (SP-4d).
// Extracción declarativa por tipo: autor real y equipo acreditado/destino según el payload
// documentado; el resto del payload (sin partidaId/sesionPartidaId/juegoId ni los ids extraídos)
// queda resumido en DetalleJson. Tipo desconocido o partidaId inválido → null (warn + ack).
public static class HistorialEventMapper
{
    private sealed record Extraccion(string? ParticipanteProp, string? EquipoProp);

    private static readonly IReadOnlyDictionary<string, Extraccion> Tipos = new Dictionary<string, Extraccion>
    {
        ["PartidaPublicadaEnLobby"] = new(null, null),
        ["PartidaIniciada"] = new(null, null),
        ["JuegoActivado"] = new(null, null),
        ["PartidaCancelada"] = new(null, null),
        ["PartidaFinalizada"] = new(null, null),
        ["RespuestaTriviaValidada"] = new("participanteId", "equipoId"),
        ["PuntajeTriviaIncrementado"] = new("participanteId", "equipoId"),
        ["PreguntaTriviaActivada"] = new(null, null),
        ["PreguntaTriviaCerrada"] = new("ganadorParticipanteId", "ganadorEquipoId"),
        ["TesoroQRValidado"] = new("participanteId", "equipoId"),
        ["EtapaBDTGanada"] = new("participanteId", "equipoId"),
        ["EtapaBDTCerrada"] = new("ganadorParticipanteId", "ganadorEquipoId"),
        ["EtapaBDTActivada"] = new(null, null),
        ["PistaEnviada"] = new("participanteDestinoId", "equipoDestinoId"),
        ["ConvocatoriaCreada"] = new("usuarioId", "equipoId"),
        ["ConvocatoriaRespondida"] = new("usuarioId", null),
        ["UbicacionActualizada"] = new("participanteId", null),
    };

    public static ProyectarEventoHistorialCommand? Map(EnvelopeResumen envelope)
    {
        if (!Tipos.TryGetValue(envelope.EventType, out var extraccion))
        {
            return null;
        }

        var payload = envelope.Payload;
        if (GetGuidOpcional(payload, "partidaId") is not { } partidaId)
        {
            return null;
        }

        var participanteProp = extraccion.ParticipanteProp;
        var equipoProp = extraccion.EquipoProp;
        var excluidas = new HashSet<string> { "partidaId", "sesionPartidaId", "juegoId" };
        if (participanteProp is not null)
        {
            excluidas.Add(participanteProp);
        }
        if (equipoProp is not null)
        {
            excluidas.Add(equipoProp);
        }

        var detalle = new Dictionary<string, JsonElement>();
        foreach (var prop in payload.EnumerateObject())
        {
            if (!excluidas.Contains(prop.Name))
            {
                detalle[prop.Name] = prop.Value.Clone();
            }
        }

        return new ProyectarEventoHistorialCommand(
            envelope.EventId,
            envelope.EventType,
            envelope.OccurredAt,
            partidaId,
            GetGuidOpcional(payload, "juegoId"),
            participanteProp is null ? null : GetGuidOpcional(payload, participanteProp),
            equipoProp is null ? null : GetGuidOpcional(payload, equipoProp),
            JsonSerializer.Serialize(detalle));
    }

    private static Guid? GetGuidOpcional(JsonElement payload, string nombre)
        => payload.TryGetProperty(nombre, out var prop)
            && prop.ValueKind == JsonValueKind.String
            && prop.TryGetGuid(out var valor)
                ? valor
                : null;
}
```

- [ ] **Step 4: Correr los tests para verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter HistorialEventMapperTests`
Expected: PASS (17 casos del theory + 3 facts).

- [ ] **Step 5: Suite completa y commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: todos verdes.

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): mapper de historial para los 17 eventos del contrato (SP-4d)"
```

---

### Task 4: `RabbitMqHistorialOptions` + `HistorialEventsConsumer` + registro en `Program.cs`

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/RabbitMqHistorialOptions.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/HistorialEventsConsumer.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/RabbitMqHistorialOptionsTests.cs`

**Interfaces:**
- Consumes: `RabbitMqConsumerOptions` (conexión/credenciales/exchange/Enabled — **sin tocar**), `EnvelopeReader.TryRead`, `HistorialEventMapper.Map` (Task 3), `ISender` por scope.
- Produces: cola durable `puntuaciones.operaciones-sesion.historial` con binding `operaciones-sesion.#`; `RabbitMqHistorialOptions { SectionName = "RabbitMqHistorial", Queue, Binding }`. El consumidor trata `DbUpdateException` como "duplicado ya registrado" (éxito) y todo otro fallo como best-effort (`LogError` + ack).

- [ ] **Step 1: Escribir el test que falla**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/RabbitMqHistorialOptionsTests.cs`:

```csharp
using Umbral.Puntuaciones.Api.Workers;

namespace Umbral.Puntuaciones.UnitTests.Workers;

public class RabbitMqHistorialOptionsTests
{
    [Fact]
    public void Defaults_del_contrato_de_transporte()
    {
        var options = new RabbitMqHistorialOptions();

        Assert.Equal("puntuaciones.operaciones-sesion.historial", options.Queue);
        Assert.Equal("operaciones-sesion.#", options.Binding);
        Assert.Equal("RabbitMqHistorial", RabbitMqHistorialOptions.SectionName);
    }
}
```

- [ ] **Step 2: Correr el test para verificar que falla**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter RabbitMqHistorialOptionsTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar opciones y consumidor**

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/RabbitMqHistorialOptions.cs`:

```csharp
namespace Umbral.Puntuaciones.Api.Workers;

// Segunda cola dedicada al historial (SP-4d). La conexión (host/credenciales/exchange/Enabled)
// se reusa de RabbitMqConsumerOptions; aquí solo viven la cola y su binding catch-all.
public sealed class RabbitMqHistorialOptions
{
    public const string SectionName = "RabbitMqHistorial";

    public string Queue { get; set; } = "puntuaciones.operaciones-sesion.historial";
    public string Binding { get; set; } = "operaciones-sesion.#";
}
```

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/HistorialEventsConsumer.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Umbral.Puntuaciones.Api.Workers;

// Consumidor del historial (SP-4d): segunda cola con binding # al exchange existente; cada evento
// se traduce al comando genérico y se despacha con scope propio (sin pipeline de difusión — el
// historial no difunde). Best-effort (ADR-0012): ack-siempre, sin poison-loop; el historial es
// reconstruible reprocesando eventos. Mismo esqueleto que OperacionesSesionEventsConsumer.
public sealed class HistorialEventsConsumer : BackgroundService
{
    private readonly RabbitMqConsumerOptions _conexion;
    private readonly RabbitMqHistorialOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HistorialEventsConsumer> _logger;

    public HistorialEventsConsumer(
        RabbitMqConsumerOptions conexion,
        RabbitMqHistorialOptions options,
        IServiceScopeFactory scopeFactory,
        ILogger<HistorialEventsConsumer> logger)
    {
        _conexion = conexion;
        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_conexion.Enabled || string.IsNullOrWhiteSpace(_conexion.Host))
        {
            _logger.LogWarning("RabbitMQ deshabilitado o sin host: el consumidor de historial no arranca.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _conexion.Host,
                    Port = _conexion.Port,
                    UserName = _conexion.User,
                    Password = _conexion.Password,
                    DispatchConsumersAsync = true
                };
                using var connection = factory.CreateConnection("umbral-puntuaciones-historial");
                using var channel = connection.CreateModel();
                channel.ExchangeDeclare(_conexion.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
                channel.QueueDeclare(_options.Queue, durable: true, exclusive: false, autoDelete: false);
                channel.QueueBind(_options.Queue, _conexion.Exchange, _options.Binding);

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
                _logger.LogWarning(ex, "Conexión RabbitMQ del historial caída; reintento en 30 s.");
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

        var command = HistorialEventMapper.Map(envelope!);
        if (command is null)
        {
            _logger.LogWarning(
                "Evento {EventType} {EventId} sin registro de historial; se descarta (ack).",
                envelope!.EventType, envelope.EventId);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(command, ct);
        }
        catch (DbUpdateException)
        {
            // Carrera del check-then-insert entre consumidores: el índice único de EventId ya
            // registró la fila — el duplicado ES el resultado correcto (design SP-4d §3).
            _logger.LogInformation(
                "Evento de historial {EventType} {EventId} ya registrado por otro consumidor.",
                envelope!.EventType, envelope.EventId);
        }
        catch (Exception ex)
        {
            // Best-effort (ADR-0012): el historial es reconstruible; sin requeue para evitar poison-loop.
            _logger.LogError(ex, "Fallo registrando historial {EventType} {EventId}; se descarta (ack).",
                envelope!.EventType, envelope.EventId);
        }
        finally
        {
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
    }
}
```

En `Program.cs`, después del bloque existente de `rabbitOptions`/`AddHostedService<OperacionesSesionEventsConsumer>()`:

```csharp
var rabbitHistorialOptions = builder.Configuration
    .GetSection(Umbral.Puntuaciones.Api.Workers.RabbitMqHistorialOptions.SectionName)
    .Get<Umbral.Puntuaciones.Api.Workers.RabbitMqHistorialOptions>()
    ?? new Umbral.Puntuaciones.Api.Workers.RabbitMqHistorialOptions();
builder.Services.AddSingleton(rabbitHistorialOptions);
builder.Services.AddHostedService<Umbral.Puntuaciones.Api.Workers.HistorialEventsConsumer>();
```

- [ ] **Step 4: Correr los tests para verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter RabbitMqHistorialOptionsTests`
Expected: PASS. Además la suite de integración sigue verde (el consumidor no arranca con RabbitMQ deshabilitado, igual que el de proyecciones).

- [ ] **Step 5: Suite completa y commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: todos verdes.

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): consumidor dedicado de historial con cola propia y binding # (SP-4d)"
```

---

### Task 5: Query `ObtenerHistorialPartidaQuery` + DTOs (HU-43, lado Application)

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Queries/ObtenerHistorialPartidaQuery.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/HistorialPartidaResponse.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerHistorialPartidaQueryHandler.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerHistorialPartidaQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `IHistorialRepository` (Task 1), `IProyeccionesRepository.GetPartidaAsync`, `PartidaNoEncontradaException` (SP-4b), `FakeHistorialRepository`/`FakeProyeccionesRepository`.
- Produces: `ObtenerHistorialPartidaQuery(Guid PartidaId, int Limit, int Offset, string? TipoEvento) : IRequest<HistorialPartidaResponse>`; `HistorialPartidaResponse(Guid PartidaId, int Total, IReadOnlyList<EntradaHistorialDto> Entradas)`; `EntradaHistorialDto(DateTime OccurredAt, string TipoEvento, Guid? JuegoId, Guid? ParticipanteId, Guid? EquipoId, JsonElement Detalle)`. Reglas: `limit` ∈ [1, 500] y `offset` ≥ 0 (`ArgumentException` → 400), partida no proyectada → `PartidaNoEncontradaException` (404), orden `OccurredAt ASC`.

- [ ] **Step 1: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerHistorialPartidaQueryHandlerTests.cs`:

```csharp
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ObtenerHistorialPartidaQueryHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProyeccionesRepository _proyecciones = new();
    private readonly FakeHistorialRepository _historial = new();

    private ObtenerHistorialPartidaQueryHandler Handler() => new(_proyecciones, _historial);

    private Guid SembrarPartida()
    {
        var partidaId = Guid.NewGuid();
        _proyecciones.AddPartida(PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual));
        return partidaId;
    }

    private void SembrarEvento(Guid partidaId, string tipo, DateTime occurredAt, string detalle = """{"orden":1}""")
        => _historial.AddEvento(EventoHistorial.Registrar(
            Guid.NewGuid(), partidaId, null, tipo, occurredAt, null, null, detalle));

    [Fact]
    public async Task Devuelve_entradas_en_orden_cronologico_con_total_y_detalle()
    {
        var partidaId = SembrarPartida();
        SembrarEvento(partidaId, "PartidaIniciada", Ahora.AddMinutes(1));
        SembrarEvento(partidaId, "PartidaPublicadaEnLobby", Ahora);

        var response = await Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, 100, 0, null), CancellationToken.None);

        Assert.Equal(partidaId, response.PartidaId);
        Assert.Equal(2, response.Total);
        Assert.Equal(new[] { "PartidaPublicadaEnLobby", "PartidaIniciada" },
            response.Entradas.Select(e => e.TipoEvento).ToArray());
        Assert.Equal(1, response.Entradas[0].Detalle.GetProperty("orden").GetInt32());
    }

    [Fact]
    public async Task Paginacion_respeta_limit_y_offset_con_total_completo()
    {
        var partidaId = SembrarPartida();
        for (var i = 0; i < 5; i++)
        {
            SembrarEvento(partidaId, "UbicacionActualizada", Ahora.AddMinutes(i));
        }

        var response = await Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, 2, 3, null), CancellationToken.None);

        Assert.Equal(5, response.Total);
        Assert.Equal(2, response.Entradas.Count);
        Assert.Equal(Ahora.AddMinutes(3), response.Entradas[0].OccurredAt);
    }

    [Fact]
    public async Task Filtro_por_tipo_afecta_entradas_y_total()
    {
        var partidaId = SembrarPartida();
        SembrarEvento(partidaId, "UbicacionActualizada", Ahora);
        SembrarEvento(partidaId, "EtapaBDTGanada", Ahora.AddMinutes(1));

        var response = await Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, 100, 0, "EtapaBDTGanada"), CancellationToken.None);

        Assert.Equal(1, response.Total);
        Assert.Equal("EtapaBDTGanada", Assert.Single(response.Entradas).TipoEvento);
    }

    [Fact]
    public async Task Partida_desconocida_lanza_PartidaNoEncontrada()
        => await Assert.ThrowsAsync<PartidaNoEncontradaException>(() => Handler().Handle(
            new ObtenerHistorialPartidaQuery(Guid.NewGuid(), 100, 0, null), CancellationToken.None));

    [Fact]
    public async Task Partida_conocida_sin_eventos_devuelve_lista_vacia()
    {
        var partidaId = SembrarPartida();

        var response = await Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, 100, 0, null), CancellationToken.None);

        Assert.Equal(0, response.Total);
        Assert.Empty(response.Entradas);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(501, 0)]
    [InlineData(100, -1)]
    public async Task Limit_u_offset_invalidos_lanzan_ArgumentException(int limit, int offset)
    {
        var partidaId = SembrarPartida();

        await Assert.ThrowsAsync<ArgumentException>(() => Handler().Handle(
            new ObtenerHistorialPartidaQuery(partidaId, limit, offset, null), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter ObtenerHistorialPartidaQueryHandlerTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar query, DTOs y handler**

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Queries/ObtenerHistorialPartidaQuery.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerHistorialPartidaQuery(
    Guid PartidaId, int Limit, int Offset, string? TipoEvento) : IRequest<HistorialPartidaResponse>;
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/HistorialPartidaResponse.cs`:

```csharp
using System.Text.Json;

namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record EntradaHistorialDto(
    DateTime OccurredAt, string TipoEvento, Guid? JuegoId, Guid? ParticipanteId, Guid? EquipoId, JsonElement Detalle);

public sealed record HistorialPartidaResponse(
    Guid PartidaId, int Total, IReadOnlyList<EntradaHistorialDto> Entradas);
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerHistorialPartidaQueryHandler.cs`:

```csharp
using System.Text.Json;
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

// HU-43: relato cronológico de la partida para el operador. La partida debe estar proyectada
// (404 si no); el historial en sí no depende de la proyección para escribirse.
public sealed class ObtenerHistorialPartidaQueryHandler
    : IRequestHandler<ObtenerHistorialPartidaQuery, HistorialPartidaResponse>
{
    private const int LimitMaximo = 500;

    private readonly IProyeccionesRepository _proyecciones;
    private readonly IHistorialRepository _historial;

    public ObtenerHistorialPartidaQueryHandler(IProyeccionesRepository proyecciones, IHistorialRepository historial)
    {
        _proyecciones = proyecciones;
        _historial = historial;
    }

    public async Task<HistorialPartidaResponse> Handle(
        ObtenerHistorialPartidaQuery request, CancellationToken cancellationToken)
    {
        if (request.Limit < 1 || request.Limit > LimitMaximo)
        {
            throw new ArgumentException($"limit debe estar entre 1 y {LimitMaximo}.");
        }
        if (request.Offset < 0)
        {
            throw new ArgumentException("offset no puede ser negativo.");
        }

        _ = await _proyecciones.GetPartidaAsync(request.PartidaId, cancellationToken)
            ?? throw new PartidaNoEncontradaException(request.PartidaId);

        var total = await _historial.ContarHistorialDePartidaAsync(request.PartidaId, request.TipoEvento, cancellationToken);
        var eventos = await _historial.GetHistorialDePartidaAsync(
            request.PartidaId, request.TipoEvento, request.Limit, request.Offset, cancellationToken);

        var entradas = eventos
            .Select(e => new EntradaHistorialDto(
                e.OccurredAt, e.TipoEvento, e.JuegoId, e.ParticipanteId, e.EquipoId, ParseDetalle(e.DetalleJson)))
            .ToList();
        return new HistorialPartidaResponse(request.PartidaId, total, entradas);
    }

    private static JsonElement ParseDetalle(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
```

- [ ] **Step 4: Correr los tests para verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter ObtenerHistorialPartidaQueryHandlerTests`
Expected: PASS (8 tests contando el theory).

- [ ] **Step 5: Suite completa y commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: todos verdes.

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): query de historial de partida con paginacion y filtro por tipo (SP-4d)"
```

---

### Task 6: `HistorialController` + roles en `TestAuthHandler` + contract e integración (HU-43 completa)

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/HistorialController.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/TestAuthHandler.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/PuntuacionesWebFactory.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/TestAuthHandler.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/PuntuacionesWebFactory.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/HistorialControllerTests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/HistorialContractTests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/HistorialE2ETests.cs`

**Interfaces:**
- Consumes: `ObtenerHistorialPartidaQuery` (Task 5), `ProyectarEventoHistorialCommand` (Task 2, para sembrar E2E), `FakeSender` existente en `UnitTests/Api/`.
- Produces: `GET /puntuaciones/partidas/{partidaId}/historial?limit=&offset=&tipo=` con `[Authorize(Roles = "Operador,Administrador")]`. `TestAuthHandler` (ambos proyectos) emite claims de rol `"roles"` desde el header `X-Test-Roles` (CSV) y la identidad usa `roleType: "roles"` (paridad con `RoleClaimType` de Program.cs); `PuntuacionesWebFactory.CreateClientConRoles(params string[] roles)` en ambos proyectos.

- [ ] **Step 1: Extender `TestAuthHandler` y `PuntuacionesWebFactory` en ambos proyectos de test**

En `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/TestAuthHandler.cs` **y** `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/TestAuthHandler.cs` (idéntico salvo el namespace), reemplazar el cuerpo de `HandleAuthenticateAsync` desde la construcción de claims:

```csharp
        var sub = subValue.ToString();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sub),
            new("sub", sub)
        };
        // SP-4d: roles opcionales para endpoints con [Authorize(Roles = ...)] — el claim type "roles"
        // replica el RoleClaimType de la config JWT real del servicio.
        if (Request.Headers.TryGetValue("X-Test-Roles", out var rolesValue))
        {
            foreach (var role in rolesValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim("roles", role));
            }
        }
        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.NameIdentifier, "roles");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
```

En ambos `PuntuacionesWebFactory.cs`, agregar debajo de `CreateClientAutenticado()`:

```csharp
    public HttpClient CreateClientConRoles(params string[] roles)
    {
        var client = CreateClientAutenticado();
        client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(",", roles));
        return client;
    }
```

- [ ] **Step 2: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/HistorialControllerTests.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Api.Controllers;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.UnitTests.Api;

public class HistorialControllerTests
{
    [Fact]
    public void Exige_rol_Operador_o_Administrador()
    {
        var attribute = typeof(HistorialController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal("Operador,Administrador", attribute.Roles);
    }

    [Fact]
    public async Task ObtenerHistorial_despacha_la_query_con_los_parametros()
    {
        var partidaId = Guid.NewGuid();
        var esperado = new HistorialPartidaResponse(partidaId, 0, Array.Empty<EntradaHistorialDto>());
        var sender = new FakeSender(esperado);
        var controller = new HistorialController(sender);

        var resultado = await controller.ObtenerHistorial(partidaId, 50, 10, "EtapaBDTGanada", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(esperado, ok.Value);
        var query = Assert.IsType<ObtenerHistorialPartidaQuery>(sender.UltimoRequest);
        Assert.Equal(partidaId, query.PartidaId);
        Assert.Equal(50, query.Limit);
        Assert.Equal(10, query.Offset);
        Assert.Equal("EtapaBDTGanada", query.TipoEvento);
    }
}
```

> Nota: `FakeSender` ya existe en `UnitTests/Api/FakeSender.cs` (lo usan `RankingsControllerTests`/`EquiposControllerTests`). Si su constructor o propiedad de captura difiere de lo anterior (`FakeSender(object respuesta)` / `UltimoRequest`), adaptar el test al patrón real del fake — no cambiar el fake.

`services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/HistorialContractTests.cs`:

```csharp
using System.Net;
using System.Text.Json;

namespace Umbral.Puntuaciones.ContractTests;

public class HistorialContractTests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;

    public HistorialContractTests(PuntuacionesWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Con_rol_Participante_devuelve_403()
    {
        var client = _factory.CreateClientConRoles("Participante");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Autenticado_sin_roles_devuelve_403()
    {
        var client = _factory.CreateClientAutenticado();

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Con_rol_Operador_y_partida_desconocida_devuelve_404_con_message()
    {
        var client = _factory.CreateClientConRoles("Operador");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(json.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Limit_fuera_de_rango_devuelve_400()
    {
        var client = _factory.CreateClientConRoles("Administrador");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial?limit=501");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

`services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/HistorialE2ETests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class HistorialE2ETests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    public HistorialE2ETests(PuntuacionesWebFactory factory) => _factory = factory;

    private async Task Proyectar(IBaseRequest comando)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando);
    }

    private Task RegistrarHistorial(
        Guid partidaId, string tipo, DateTime occurredAt,
        Guid? participanteId = null, string detalle = "{}")
        => Proyectar(new ProyectarEventoHistorialCommand(
            Guid.NewGuid(), tipo, occurredAt, partidaId, null, participanteId, null, detalle));

    [Fact]
    public async Task Historial_de_partida_ordenado_paginado_y_filtrado()
    {
        var partidaId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(
            Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), Modalidad.Individual));
        await RegistrarHistorial(partidaId, "PartidaIniciada", Ahora.AddMinutes(1), detalle: """{"primerJuegoOrden":1}""");
        await RegistrarHistorial(partidaId, "PartidaPublicadaEnLobby", Ahora);
        await RegistrarHistorial(partidaId, "EtapaBDTGanada", Ahora.AddMinutes(2), detalle: """{"puntaje":10}""");

        var client = _factory.CreateClientConRoles("Operador");

        var completo = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/historial");
        using var jsonCompleto = JsonDocument.Parse(await completo.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, completo.StatusCode);
        Assert.Equal(3, jsonCompleto.RootElement.GetProperty("total").GetInt32());
        var entradas = jsonCompleto.RootElement.GetProperty("entradas");
        Assert.Equal("PartidaPublicadaEnLobby", entradas[0].GetProperty("tipoEvento").GetString());
        Assert.Equal("EtapaBDTGanada", entradas[2].GetProperty("tipoEvento").GetString());
        Assert.Equal(10, entradas[2].GetProperty("detalle").GetProperty("puntaje").GetInt32());

        var paginado = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/historial?limit=1&offset=1");
        using var jsonPaginado = JsonDocument.Parse(await paginado.Content.ReadAsStringAsync());
        Assert.Equal(3, jsonPaginado.RootElement.GetProperty("total").GetInt32());
        Assert.Equal("PartidaIniciada",
            jsonPaginado.RootElement.GetProperty("entradas")[0].GetProperty("tipoEvento").GetString());

        var filtrado = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/historial?tipo=EtapaBDTGanada");
        using var jsonFiltrado = JsonDocument.Parse(await filtrado.Content.ReadAsStringAsync());
        Assert.Equal(1, jsonFiltrado.RootElement.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Muestreo_de_ubicaciones_de_punta_a_punta()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(
            Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), Modalidad.Individual));
        await RegistrarHistorial(partidaId, "UbicacionActualizada", Ahora, participanteId);
        await RegistrarHistorial(partidaId, "UbicacionActualizada", Ahora.AddSeconds(30), participanteId);   // descartada
        await RegistrarHistorial(partidaId, "UbicacionActualizada", Ahora.AddSeconds(90), participanteId);   // guardada

        var client = _factory.CreateClientConRoles("Administrador");
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/historial?tipo=UbicacionActualizada");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(2, json.RootElement.GetProperty("total").GetInt32());
    }
}
```

- [ ] **Step 3: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln" --filter "HistorialControllerTests|HistorialContractTests|HistorialE2ETests"`
Expected: FAIL de compilación (`HistorialController` no existe).

- [ ] **Step 4: Implementar el controller**

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/HistorialController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.Api.Controllers;

// HU-43: el historial expone respuestas, pistas y ubicaciones de todos los participantes —
// primer endpoint de Puntuaciones con autorización por rol (solo operador/administrador).
[ApiController]
[Route("puntuaciones")]
[Authorize(Roles = "Operador,Administrador")]
public sealed class HistorialController : ControllerBase
{
    private readonly ISender _mediator;

    public HistorialController(ISender mediator) => _mediator = mediator;

    [HttpGet("partidas/{partidaId:guid}/historial")]
    public async Task<IActionResult> ObtenerHistorial(
        Guid partidaId,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        [FromQuery] string? tipo = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new ObtenerHistorialPartidaQuery(partidaId, limit, offset, tipo), cancellationToken);
        return Ok(response);
    }
}
```

- [ ] **Step 5: Correr los tests para verificar que pasan**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln" --filter "HistorialControllerTests|HistorialContractTests|HistorialE2ETests"`
Expected: PASS (2 unit + 5 contract + 2 integration).

- [ ] **Step 6: Suite completa y commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: todos verdes (los contract/integration existentes no se ven afectados: sin `X-Test-Roles` el comportamiento del TestAuthHandler es idéntico).

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): endpoint de historial de partida con autorizacion por rol (SP-4d, HU-43)"
```

---

### Task 7: Query `ObtenerHistorialPartidasQuery` (HU-27, lado Application)

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/IProyeccionesRepository.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/ProyeccionesRepository.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/Fakes/FakeProyeccionesRepository.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Queries/ObtenerHistorialPartidasQuery.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/HistorialPartidasResponse.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerHistorialPartidasQueryHandler.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerHistorialPartidasQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `CalculadorRankingConsolidado.Calcular(IEnumerable<Marcador>)` (RF-44 sin duplicar cálculo), `IHistorialRepository.GetEquiposDelParticipanteAsync` (Task 1), repos/fakes existentes.
- Produces: en `IProyeccionesRepository`: `GetPartidasTerminadasConMarcadorDeParticipanteAsync(Guid participanteId, CancellationToken)` (espejo del método de equipo, con `Modalidad.Individual` + `TipoCompetidor.Participante`) y `GetJuegosDePartidaAsync(Guid partidaId, CancellationToken)` (ordenados por `Orden`). Query `ObtenerHistorialPartidasQuery(Guid ParticipanteId) : IRequest<HistorialPartidasResponse>`; DTOs `HistorialPartidasResponse(Guid ParticipanteId, IReadOnlyList<PartidaJugadaDto> Partidas)`, `PartidaJugadaDto(Guid PartidaId, Modalidad? Modalidad, DateTime? FechaFin, Guid? EquipoId, int PuntosTotales, int Posicion, bool Gano, IReadOnlyList<JuegoJugadoDto> Juegos)`, `JuegoJugadoDto(Guid JuegoId, int Orden, TipoJuego TipoJuego, int Puntos)`.

- [ ] **Step 1: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerHistorialPartidasQueryHandlerTests.cs`:

```csharp
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ObtenerHistorialPartidasQueryHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProyeccionesRepository _proyecciones = new();
    private readonly FakeHistorialRepository _historial = new();

    private ObtenerHistorialPartidasQueryHandler Handler() => new(_proyecciones, _historial);

    private Guid SembrarPartidaTerminada(Modalidad modalidad, DateTime fechaFin, out Guid juegoId)
    {
        var partidaId = Guid.NewGuid();
        juegoId = Guid.NewGuid();
        var partida = PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), modalidad);
        partida.MarcarTerminada(fechaFin);
        _proyecciones.AddPartida(partida);
        _proyecciones.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 1, TipoJuego.Trivia));
        return partidaId;
    }

    private void SembrarMarcador(Guid partidaId, Guid juegoId, Guid competidorId, TipoCompetidor tipo, int puntos, long tiempoMs)
    {
        var marcador = Marcador.Nuevo(juegoId, competidorId, partidaId, tipo);
        marcador.Acreditar(puntos, tiempoMs);
        _proyecciones.AddMarcador(marcador);
    }

    [Fact]
    public async Task Individual_lista_partida_con_posicion_puntos_y_juegos()
    {
        var participanteId = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Individual, Ahora, out var juegoId);
        SembrarMarcador(partidaId, juegoId, participanteId, TipoCompetidor.Participante, 10, 1000);
        SembrarMarcador(partidaId, juegoId, rival, TipoCompetidor.Participante, 20, 900);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        Assert.Equal(participanteId, response.ParticipanteId);
        var partida = Assert.Single(response.Partidas);
        Assert.Equal(partidaId, partida.PartidaId);
        Assert.Equal(Modalidad.Individual, partida.Modalidad);
        Assert.Null(partida.EquipoId);
        Assert.Equal(10, partida.PuntosTotales);
        Assert.Equal(2, partida.Posicion);
        Assert.False(partida.Gano);
        var juego = Assert.Single(partida.Juegos);
        Assert.Equal(juegoId, juego.JuegoId);
        Assert.Equal(1, juego.Orden);
        Assert.Equal(TipoJuego.Trivia, juego.TipoJuego);
        Assert.Equal(10, juego.Puntos);
    }

    [Fact]
    public async Task Equipo_resuelto_del_historial_muestra_puntuacion_y_posicion_del_equipo()
    {
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Equipo, Ahora, out var juegoId);
        SembrarMarcador(partidaId, juegoId, equipoId, TipoCompetidor.Equipo, 30, 1000);
        SembrarMarcador(partidaId, juegoId, rival, TipoCompetidor.Equipo, 20, 900);
        _historial.AddEvento(EventoHistorial.Registrar(
            Guid.NewGuid(), partidaId, juegoId, "EtapaBDTGanada", Ahora, participanteId, equipoId, "{}"));

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        var partida = Assert.Single(response.Partidas);
        Assert.Equal(equipoId, partida.EquipoId);
        Assert.Equal(30, partida.PuntosTotales);
        Assert.Equal(1, partida.Posicion);
        Assert.True(partida.Gano);
    }

    [Fact]
    public async Task Membresia_por_ConvocatoriaCreada_sola_no_lista_la_partida()
    {
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Equipo, Ahora, out var juegoId);
        SembrarMarcador(partidaId, juegoId, equipoId, TipoCompetidor.Equipo, 30, 1000);
        _historial.AddEvento(EventoHistorial.Registrar(
            Guid.NewGuid(), partidaId, null, "ConvocatoriaCreada", Ahora, participanteId, equipoId, "{}"));

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        Assert.Empty(response.Partidas);
    }

    [Fact]
    public async Task Equipo_sin_marcador_en_la_partida_no_se_lista()
    {
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Equipo, Ahora, out _);
        _historial.AddEvento(EventoHistorial.Registrar(
            Guid.NewGuid(), partidaId, null, "RespuestaTriviaValidada", Ahora, participanteId, equipoId, "{}"));

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        Assert.Empty(response.Partidas);
    }

    [Fact]
    public async Task Partida_no_terminada_o_cancelada_no_se_lista()
    {
        var participanteId = Guid.NewGuid();
        var enCurso = Guid.NewGuid();
        var juegoEnCurso = Guid.NewGuid();
        var partidaEnCurso = PartidaProyectada.DesdePublicacion(enCurso, Guid.NewGuid(), Modalidad.Individual);
        partidaEnCurso.MarcarIniciada(Ahora);
        _proyecciones.AddPartida(partidaEnCurso);
        SembrarMarcador(enCurso, juegoEnCurso, participanteId, TipoCompetidor.Participante, 10, 1000);

        var cancelada = Guid.NewGuid();
        var juegoCancelado = Guid.NewGuid();
        var partidaCancelada = PartidaProyectada.DesdePublicacion(cancelada, Guid.NewGuid(), Modalidad.Individual);
        partidaCancelada.MarcarCancelada(Ahora);
        _proyecciones.AddPartida(partidaCancelada);
        SembrarMarcador(cancelada, juegoCancelado, participanteId, TipoCompetidor.Participante, 5, 500);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        Assert.Empty(response.Partidas);
    }

    [Fact]
    public async Task Ordena_por_fechaFin_descendente()
    {
        var participanteId = Guid.NewGuid();
        var vieja = SembrarPartidaTerminada(Modalidad.Individual, Ahora.AddDays(-2), out var juegoViejo);
        var reciente = SembrarPartidaTerminada(Modalidad.Individual, Ahora, out var juegoReciente);
        SembrarMarcador(vieja, juegoViejo, participanteId, TipoCompetidor.Participante, 10, 1000);
        SembrarMarcador(reciente, juegoReciente, participanteId, TipoCompetidor.Participante, 10, 1000);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        Assert.Equal(new[] { reciente, vieja }, response.Partidas.Select(p => p.PartidaId).ToArray());
    }

    [Fact]
    public async Task Participante_sin_partidas_devuelve_lista_vacia()
    {
        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(response.Partidas);
    }

    [Fact]
    public async Task Juego_sin_marcador_propio_aparece_con_cero_puntos()
    {
        var participanteId = Guid.NewGuid();
        var partidaId = SembrarPartidaTerminada(Modalidad.Individual, Ahora, out var juego1);
        var juego2 = Guid.NewGuid();
        _proyecciones.AddJuego(JuegoProyectado.Desde(juego2, partidaId, 2, TipoJuego.BusquedaDelTesoro));
        SembrarMarcador(partidaId, juego1, participanteId, TipoCompetidor.Participante, 10, 1000);

        var response = await Handler().Handle(new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        var partida = Assert.Single(response.Partidas);
        Assert.Equal(2, partida.Juegos.Count);
        Assert.Equal(0, partida.Juegos.Single(j => j.JuegoId == juego2).Puntos);
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter ObtenerHistorialPartidasQueryHandlerTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar repos, query, DTOs y handler**

En `IProyeccionesRepository.cs`, agregar al final de la interfaz:

```csharp
    Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeParticipanteAsync(Guid participanteId, CancellationToken cancellationToken);
    Task<IReadOnlyList<JuegoProyectado>> GetJuegosDePartidaAsync(Guid partidaId, CancellationToken cancellationToken);
```

En `ProyeccionesRepository.cs`, agregar al final de la clase:

```csharp
    // HU-27 (a): participación individual = tener ≥1 marcador propio en una partida Individual terminada.
    public async Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeParticipanteAsync(Guid participanteId, CancellationToken cancellationToken)
        => await _db.Partidas.AsNoTracking()
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada
                && p.Modalidad == Modalidad.Individual
                && _db.Marcadores.Any(m => m.PartidaId == p.PartidaId
                    && m.CompetidorId == participanteId
                    && m.TipoCompetidor == TipoCompetidor.Participante))
            .OrderByDescending(p => p.FechaFin)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<JuegoProyectado>> GetJuegosDePartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => await _db.Juegos.AsNoTracking()
            .Where(j => j.PartidaId == partidaId)
            .OrderBy(j => j.Orden)
            .ToListAsync(cancellationToken);
```

En `FakeProyeccionesRepository.cs`, agregar al final de la clase:

```csharp
    public Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeParticipanteAsync(Guid participanteId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PartidaProyectada>>(Partidas
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada
                && p.Modalidad == Modalidad.Individual
                && Marcadores.Any(m => m.PartidaId == p.PartidaId
                    && m.CompetidorId == participanteId
                    && m.TipoCompetidor == TipoCompetidor.Participante))
            .OrderByDescending(p => p.FechaFin)
            .ToList());

    public Task<IReadOnlyList<JuegoProyectado>> GetJuegosDePartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<JuegoProyectado>>(Juegos
            .Where(j => j.PartidaId == partidaId)
            .OrderBy(j => j.Orden)
            .ToList());
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Queries/ObtenerHistorialPartidasQuery.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Queries;

public sealed record ObtenerHistorialPartidasQuery(Guid ParticipanteId) : IRequest<HistorialPartidasResponse>;
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/DTOs/HistorialPartidasResponse.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record JuegoJugadoDto(Guid JuegoId, int Orden, TipoJuego TipoJuego, int Puntos);

public sealed record PartidaJugadaDto(
    Guid PartidaId, Modalidad? Modalidad, DateTime? FechaFin, Guid? EquipoId,
    int PuntosTotales, int Posicion, bool Gano, IReadOnlyList<JuegoJugadoDto> Juegos);

public sealed record HistorialPartidasResponse(Guid ParticipanteId, IReadOnlyList<PartidaJugadaDto> Partidas);
```

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerHistorialPartidasQueryHandler.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

// HU-27 (RF-24): historial único de partidas jugadas con puntuación y posición. Participación:
// (a) marcador propio en Individual; (b) equipo resuelto del historial (acciones de juego
// autoradas, sin ConvocatoriaCreada) con ≥1 marcador del equipo. Posición/gano del mismo
// CalculadorRankingConsolidado de SP-4b (RF-44: sin duplicar el cálculo). Limitación documentada:
// el integrante que jamás autoró una acción de juego no ve la partida; canceladas excluidas (RB-30).
public sealed class ObtenerHistorialPartidasQueryHandler
    : IRequestHandler<ObtenerHistorialPartidasQuery, HistorialPartidasResponse>
{
    private readonly IProyeccionesRepository _proyecciones;
    private readonly IHistorialRepository _historial;

    public ObtenerHistorialPartidasQueryHandler(IProyeccionesRepository proyecciones, IHistorialRepository historial)
    {
        _proyecciones = proyecciones;
        _historial = historial;
    }

    public async Task<HistorialPartidasResponse> Handle(
        ObtenerHistorialPartidasQuery request, CancellationToken cancellationToken)
    {
        var partidas = new List<PartidaJugadaDto>();

        var individuales = await _proyecciones.GetPartidasTerminadasConMarcadorDeParticipanteAsync(
            request.ParticipanteId, cancellationToken);
        foreach (var partida in individuales)
        {
            partidas.Add(await ConstruirPartidaJugadaAsync(
                partida, competidorId: request.ParticipanteId, equipoId: null, cancellationToken));
        }

        foreach (var participacion in await _historial.GetEquiposDelParticipanteAsync(request.ParticipanteId, cancellationToken))
        {
            var partida = await _proyecciones.GetPartidaAsync(participacion.PartidaId, cancellationToken);
            if (partida is null || partida.Estado != EstadoPartidaProyectada.Terminada)
            {
                continue;
            }
            var marcadores = await _proyecciones.GetMarcadoresDePartidaAsync(partida.PartidaId, cancellationToken);
            if (!marcadores.Any(m => m.CompetidorId == participacion.EquipoId && m.TipoCompetidor == TipoCompetidor.Equipo))
            {
                continue;
            }
            partidas.Add(await ConstruirPartidaJugadaAsync(
                partida, competidorId: participacion.EquipoId, equipoId: participacion.EquipoId, cancellationToken));
        }

        return new HistorialPartidasResponse(
            request.ParticipanteId,
            partidas.OrderByDescending(p => p.FechaFin).ToList());
    }

    private async Task<PartidaJugadaDto> ConstruirPartidaJugadaAsync(
        PartidaProyectada partida, Guid competidorId, Guid? equipoId, CancellationToken cancellationToken)
    {
        var marcadores = await _proyecciones.GetMarcadoresDePartidaAsync(partida.PartidaId, cancellationToken);
        var entradas = CalculadorRankingConsolidado.Calcular(marcadores);
        // La participación exige ≥1 marcador del competidor, así que la entrada siempre existe.
        var propia = entradas.First(e => e.CompetidorId == competidorId);

        var juegos = (await _proyecciones.GetJuegosDePartidaAsync(partida.PartidaId, cancellationToken))
            .Select(j => new JuegoJugadoDto(
                j.JuegoId, j.Orden, j.TipoJuego,
                marcadores.FirstOrDefault(m => m.JuegoId == j.JuegoId && m.CompetidorId == competidorId)
                    ?.PuntosAcumulados ?? 0))
            .ToList();

        return new PartidaJugadaDto(
            partida.PartidaId, partida.Modalidad, partida.FechaFin, equipoId,
            propia.PuntosTotales, propia.Posicion, propia.Posicion == 1, juegos);
    }
}
```

- [ ] **Step 4: Correr los tests para verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter ObtenerHistorialPartidasQueryHandlerTests`
Expected: PASS (8 tests).

- [ ] **Step 5: Suite completa y commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: todos verdes.

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): query de historial de partidas jugadas del participante (SP-4d, HU-27)"
```

---

### Task 8: `ParticipantesController` + contract e integración (HU-27 completa)

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/ParticipantesController.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/ParticipantesControllerTests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/HistorialPartidasContractTests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/HistorialPartidasE2ETests.cs`

**Interfaces:**
- Consumes: `ObtenerHistorialPartidasQuery`/`HistorialPartidasResponse` (Task 7), `ProyectarEventoHistorialCommand` y comandos de proyección (para sembrar E2E), `FakeSender`, `CreateClientAutenticado()`.
- Produces: `GET /puntuaciones/participantes/{participanteId:guid}/historial-partidas` con `[Authorize]` simple (cualquier rol autenticado; paridad con marcador propio y rendimiento de equipo).

- [ ] **Step 1: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/ParticipantesControllerTests.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Api.Controllers;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.UnitTests.Api;

public class ParticipantesControllerTests
{
    [Fact]
    public void Exige_autenticacion_sin_restriccion_de_rol()
    {
        var attribute = typeof(ParticipantesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Null(attribute.Roles);
    }

    [Fact]
    public async Task ObtenerHistorialPartidas_despacha_la_query()
    {
        var participanteId = Guid.NewGuid();
        var esperado = new HistorialPartidasResponse(participanteId, Array.Empty<PartidaJugadaDto>());
        var sender = new FakeSender(esperado);
        var controller = new ParticipantesController(sender);

        var resultado = await controller.ObtenerHistorialPartidas(participanteId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(esperado, ok.Value);
        var query = Assert.IsType<ObtenerHistorialPartidasQuery>(sender.UltimoRequest);
        Assert.Equal(participanteId, query.ParticipanteId);
    }
}
```

(Misma nota de la Task 6 sobre el patrón real de `FakeSender`.)

`services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/HistorialPartidasContractTests.cs`:

```csharp
using System.Net;
using System.Text.Json;

namespace Umbral.Puntuaciones.ContractTests;

public class HistorialPartidasContractTests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;

    public HistorialPartidasContractTests(PuntuacionesWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/puntuaciones/participantes/{Guid.NewGuid()}/historial-partidas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Autenticado_sin_partidas_devuelve_200_con_shape_del_contrato()
    {
        var client = _factory.CreateClientAutenticado();
        var participanteId = Guid.NewGuid();

        var response = await client.GetAsync($"/puntuaciones/participantes/{participanteId}/historial-partidas");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(participanteId, json.RootElement.GetProperty("participanteId").GetGuid());
        Assert.Equal(0, json.RootElement.GetProperty("partidas").GetArrayLength());
    }
}
```

`services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/HistorialPartidasE2ETests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class HistorialPartidasE2ETests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    public HistorialPartidasE2ETests(PuntuacionesWebFactory factory) => _factory = factory;

    private async Task Proyectar(IBaseRequest comando)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando);
    }

    [Fact]
    public async Task Individual_de_punta_a_punta_con_puntos_posicion_y_juegos()
    {
        var participanteId = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();

        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), participanteId, 20, 1000, null));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), rival, 10, 900, null));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));

        var client = _factory.CreateClientAutenticado();
        var response = await client.GetAsync($"/puntuaciones/participantes/{participanteId}/historial-partidas");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var partida = json.RootElement.GetProperty("partidas")[0];
        Assert.Equal(partidaId, partida.GetProperty("partidaId").GetGuid());
        Assert.Equal("Individual", partida.GetProperty("modalidad").GetString());
        Assert.Equal(20, partida.GetProperty("puntosTotales").GetInt32());
        Assert.Equal(1, partida.GetProperty("posicion").GetInt32());
        Assert.True(partida.GetProperty("gano").GetBoolean());
        Assert.Equal(JsonValueKind.Null, partida.GetProperty("equipoId").ValueKind);
        var juego = partida.GetProperty("juegos")[0];
        Assert.Equal("Trivia", juego.GetProperty("tipoJuego").GetString());
        Assert.Equal(20, juego.GetProperty("puntos").GetInt32());
    }

    [Fact]
    public async Task Equipo_de_punta_a_punta_resuelto_del_historial()
    {
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();

        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Equipo));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.BusquedaDelTesoro));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), participanteId, 30, 1000, equipoId));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), Guid.NewGuid(), 10, 900, rival));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));
        // La membresía HU-27 sale del historial, no de los marcadores: registrar la acción autorada.
        await Proyectar(new ProyectarEventoHistorialCommand(
            Guid.NewGuid(), "EtapaBDTGanada", Ahora, partidaId, juegoId, participanteId, equipoId, """{"puntaje":30}"""));

        var client = _factory.CreateClientAutenticado();
        var response = await client.GetAsync($"/puntuaciones/participantes/{participanteId}/historial-partidas");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var partida = json.RootElement.GetProperty("partidas")[0];
        Assert.Equal("Equipo", partida.GetProperty("modalidad").GetString());
        Assert.Equal(equipoId, partida.GetProperty("equipoId").GetGuid());
        Assert.Equal(30, partida.GetProperty("puntosTotales").GetInt32());
        Assert.Equal(1, partida.GetProperty("posicion").GetInt32());
        Assert.True(partida.GetProperty("gano").GetBoolean());
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln" --filter "ParticipantesControllerTests|HistorialPartidasContractTests|HistorialPartidasE2ETests"`
Expected: FAIL de compilación (`ParticipantesController` no existe).

- [ ] **Step 3: Implementar el controller**

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/ParticipantesController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.Api.Controllers;

[ApiController]
[Route("puntuaciones")]
[Authorize]
public sealed class ParticipantesController : ControllerBase
{
    private readonly ISender _mediator;

    public ParticipantesController(ISender mediator) => _mediator = mediator;

    [HttpGet("participantes/{participanteId:guid}/historial-partidas")]
    public async Task<IActionResult> ObtenerHistorialPartidas(Guid participanteId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ObtenerHistorialPartidasQuery(participanteId), cancellationToken);
        return Ok(response);
    }
}
```

- [ ] **Step 4: Correr los tests para verificar que pasan**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln" --filter "ParticipantesControllerTests|HistorialPartidasContractTests|HistorialPartidasE2ETests"`
Expected: PASS (2 unit + 2 contract + 2 integration).

- [ ] **Step 5: Suite completa y commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: todos verdes.

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): endpoint de historial de partidas jugadas del participante (SP-4d, HU-27)"
```

---

### Task 9: Purga de `eventos_procesados` (deuda SP-4a)

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/RetencionOptions.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/PurgaEventosProcesadosService.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/IProyeccionesRepository.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/ProyeccionesRepository.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/Fakes/FakeProyeccionesRepository.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/PurgaEventosProcesadosServiceTests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/PurgaEventosProcesadosTests.cs`

**Interfaces:**
- Consumes: `EventoProcesado.Registrar`, `IPuntuacionesUnitOfWork`, índice `ix_eventos_procesados_procesadoat` (Task 1).
- Produces: `IProyeccionesRepository.EliminarEventosProcesadosAnterioresAsync(DateTime limite, CancellationToken) : Task<int>`; `RetencionOptions { SectionName = "Retencion", EventosProcesadosDias = 30 }`; `PurgaEventosProcesadosService.EjecutarPasadaAsync(CancellationToken)` público (lo ejercitan los tests). Timer cada 24 h, primera pasada ~1 min tras arrancar. **No** toca `eventos_historial`.

- [ ] **Step 1: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/PurgaEventosProcesadosServiceTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.Puntuaciones.Api.Workers;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Workers;

public class PurgaEventosProcesadosServiceTests
{
    [Fact]
    public async Task Pasada_elimina_lo_anterior_a_la_retencion_y_conserva_lo_reciente()
    {
        var repo = new FakeProyeccionesRepository();
        var uow = new FakePuntuacionesUnitOfWork();
        var viejo = EventoProcesado.Registrar(Guid.NewGuid(), "PartidaIniciada",
            DateTime.UtcNow.AddDays(-40), DateTime.UtcNow.AddDays(-40));
        var reciente = EventoProcesado.Registrar(Guid.NewGuid(), "PartidaIniciada",
            DateTime.UtcNow, DateTime.UtcNow);
        repo.RegistrarEventoProcesado(viejo);
        repo.RegistrarEventoProcesado(reciente);

        var services = new ServiceCollection();
        services.AddSingleton<IProyeccionesRepository>(repo);
        services.AddSingleton<IPuntuacionesUnitOfWork>(uow);
        using var provider = services.BuildServiceProvider();

        var purga = new PurgaEventosProcesadosService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new RetencionOptions { EventosProcesadosDias = 30 },
            NullLogger<PurgaEventosProcesadosService>.Instance);

        await purga.EjecutarPasadaAsync(CancellationToken.None);

        var restante = Assert.Single(repo.EventosProcesados);
        Assert.Equal(reciente.EventId, restante.EventId);
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public void Retencion_default_es_30_dias()
    {
        Assert.Equal(30, new RetencionOptions().EventosProcesadosDias);
        Assert.Equal("Retencion", RetencionOptions.SectionName);
    }
}
```

`services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/PurgaEventosProcesadosTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Infrastructure.Persistence;

namespace Umbral.Puntuaciones.IntegrationTests;

public class PurgaEventosProcesadosTests
{
    [Fact]
    public async Task Elimina_solo_los_eventos_procesados_antes_del_limite()
    {
        var opciones = new DbContextOptionsBuilder<PuntuacionesDbContext>()
            .UseInMemoryDatabase($"purga-{Guid.NewGuid()}").Options;
        var limite = new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc);
        var viejoId = Guid.NewGuid();
        var recienteId = Guid.NewGuid();

        await using (var db = new PuntuacionesDbContext(opciones))
        {
            db.EventosProcesados.Add(EventoProcesado.Registrar(viejoId, "PartidaIniciada", limite.AddDays(-10), limite.AddDays(-10)));
            db.EventosProcesados.Add(EventoProcesado.Registrar(recienteId, "PartidaIniciada", limite.AddDays(1), limite.AddDays(1)));
            await db.SaveChangesAsync();
        }

        await using (var db = new PuntuacionesDbContext(opciones))
        {
            var repo = new ProyeccionesRepository(db);
            var eliminados = await repo.EliminarEventosProcesadosAnterioresAsync(limite, CancellationToken.None);
            await db.SaveChangesAsync();
            Assert.Equal(1, eliminados);
        }

        await using var lectura = new PuntuacionesDbContext(opciones);
        var restante = Assert.Single(await lectura.EventosProcesados.ToListAsync());
        Assert.Equal(recienteId, restante.EventId);
    }
}
```

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln" --filter "PurgaEventosProcesadosServiceTests|PurgaEventosProcesadosTests"`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar repo, opciones y servicio**

En `IProyeccionesRepository.cs`, agregar:

```csharp
    Task<int> EliminarEventosProcesadosAnterioresAsync(DateTime limite, CancellationToken cancellationToken);
```

En `ProyeccionesRepository.cs`, agregar al final de la clase:

```csharp
    // Retención SP-4d: el dedup solo necesita cubrir la ventana de redelivery del broker.
    // RemoveRange (no ExecuteDelete) para mantener compatibilidad con el proveedor InMemory.
    public async Task<int> EliminarEventosProcesadosAnterioresAsync(DateTime limite, CancellationToken cancellationToken)
    {
        var viejos = await _db.EventosProcesados
            .Where(e => e.ProcesadoAt < limite)
            .ToListAsync(cancellationToken);
        _db.EventosProcesados.RemoveRange(viejos);
        return viejos.Count;
    }
```

En `FakeProyeccionesRepository.cs`, agregar:

```csharp
    public Task<int> EliminarEventosProcesadosAnterioresAsync(DateTime limite, CancellationToken cancellationToken)
    {
        var eliminados = EventosProcesados.RemoveAll(e => e.ProcesadoAt < limite);
        return Task.FromResult(eliminados);
    }
```

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/RetencionOptions.cs`:

```csharp
namespace Umbral.Puntuaciones.Api.Workers;

public sealed class RetencionOptions
{
    public const string SectionName = "Retencion";

    public int EventosProcesadosDias { get; set; } = 30;
}
```

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/PurgaEventosProcesadosService.cs`:

```csharp
namespace Umbral.Puntuaciones.Api.Workers;

// Retención de eventos_procesados (deuda SP-4a): el dedup solo necesita cubrir la ventana de
// redelivery del broker; 30 días sobra. Jamás toca eventos_historial (RB-31 exige el historial
// visible) ni su dedup propio (índice único de EventId).
public sealed class PurgaEventosProcesadosService : BackgroundService
{
    private static readonly TimeSpan PrimeraPasada = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RetencionOptions _options;
    private readonly ILogger<PurgaEventosProcesadosService> _logger;

    public PurgaEventosProcesadosService(
        IServiceScopeFactory scopeFactory,
        RetencionOptions options,
        ILogger<PurgaEventosProcesadosService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(PrimeraPasada, stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EjecutarPasadaAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo en la purga de eventos_procesados; se reintenta en la próxima pasada.");
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    public async Task EjecutarPasadaAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Domain.Abstractions.Persistence.IProyeccionesRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<Domain.Abstractions.Persistence.IPuntuacionesUnitOfWork>();

        var limite = DateTime.UtcNow.AddDays(-_options.EventosProcesadosDias);
        var eliminados = await repo.EliminarEventosProcesadosAnterioresAsync(limite, ct);
        await uow.SaveChangesAsync(ct);

        if (eliminados > 0)
        {
            _logger.LogInformation(
                "Purga de eventos_procesados: {Eliminados} filas anteriores a {Limite:o} eliminadas.",
                eliminados, limite);
        }
    }
}
```

(Usar `using Umbral.Puntuaciones.Domain.Abstractions.Persistence;` arriba y tipos sin calificar si se prefiere — mantener el estilo del archivo.)

En `Program.cs`, después del registro del consumidor de historial (Task 4):

```csharp
var retencionOptions = builder.Configuration
    .GetSection(Umbral.Puntuaciones.Api.Workers.RetencionOptions.SectionName)
    .Get<Umbral.Puntuaciones.Api.Workers.RetencionOptions>()
    ?? new Umbral.Puntuaciones.Api.Workers.RetencionOptions();
builder.Services.AddSingleton(retencionOptions);
builder.Services.AddHostedService<Umbral.Puntuaciones.Api.Workers.PurgaEventosProcesadosService>();
```

- [ ] **Step 4: Correr los tests para verificar que pasan**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln" --filter "PurgaEventosProcesadosServiceTests|PurgaEventosProcesadosTests"`
Expected: PASS (2 unit + 1 integration).

> Ojo: la primera pasada corre ~1 min después de arrancar; los hosts de test (WebApplicationFactory) suelen vivir menos que eso, así que no interfiere con las suites. Verificarlo con la suite completa.

- [ ] **Step 5: Suite completa y commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: todos verdes.

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): purga con retencion configurable de eventos_procesados (SP-4d, deuda SP-4a)"
```

---

### Task 10: Middleware — `LogWarning` en 400 (deuda SP-4a)

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/ExceptionHandlingMiddlewareTests.cs`

**Interfaces:**
- Consumes: mapeo existente `ValidationException or ArgumentException => BadRequest`.
- Produces: mismo contrato HTTP (sin cambio de shape); ahora todo 400 emite `LogWarning` con mensaje y path.

- [ ] **Step 1: Escribir los tests que fallan**

En `ExceptionHandlingMiddlewareTests.cs`, agregar un logger que graba y dos tests (mantener los existentes intactos):

```csharp
    private sealed class RecordingLogger : ILogger<ExceptionHandlingMiddleware>
    {
        public List<(LogLevel Nivel, string Mensaje)> Entradas { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entradas.Add((logLevel, formatter(state, exception)));
    }

    [Fact]
    public async Task ArgumentException_mapea_400_y_emite_warning_con_path()
    {
        var logger = new RecordingLogger();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ArgumentException("limit debe estar entre 1 y 500."), logger);
        var context = new DefaultHttpContext();
        context.Request.Path = "/puntuaciones/partidas/x/historial";

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var entrada = Assert.Single(logger.Entradas);
        Assert.Equal(LogLevel.Warning, entrada.Nivel);
        Assert.Contains("limit debe estar entre 1 y 500.", entrada.Mensaje);
        Assert.Contains("/puntuaciones/partidas/x/historial", entrada.Mensaje);
    }

    [Fact]
    public async Task NotFound_no_emite_warning()
    {
        var logger = new RecordingLogger();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new PartidaNoEncontradaException(Guid.NewGuid()), logger);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Empty(logger.Entradas);
    }
```

(Agregar `using Microsoft.Extensions.Logging;` al archivo de tests.)

- [ ] **Step 2: Correr los tests para verificar que fallan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter ExceptionHandlingMiddlewareTests`
Expected: FAIL (`ArgumentException_mapea_400_y_emite_warning_con_path` — no se emite el warning).

- [ ] **Step 3: Implementar el log**

En `ExceptionHandlingMiddleware.InvokeAsync`, reemplazar el `if` del log:

```csharp
            var status = MapStatus(ex);
            if (status == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception.");
            }
            else if (status == HttpStatusCode.BadRequest)
            {
                // Deuda SP-4a: los 400 respondían sin dejar rastro en logs.
                _logger.LogWarning("Solicitud inválida en {Path}: {Message}", context.Request.Path, ex.Message);
            }
```

- [ ] **Step 4: Correr los tests para verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter ExceptionHandlingMiddlewareTests`
Expected: PASS (7 tests).

- [ ] **Step 5: Suite completa y commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: todos verdes.

```bash
git add services/puntuaciones
git commit -m "fix(puntuaciones): LogWarning con path en respuestas 400 del middleware (SP-4d, deuda SP-4a)"
```

---

### Task 11: Contratos, service-context y traceability (cierre de la serie SP-4)

**Files:**
- Modify: `contracts/http/puntuaciones-api.md`
- Modify: `contracts/events/operaciones-sesion-events.md`
- Modify: `services/puntuaciones/service-context.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: shapes implementados en Tasks 5–8 (verificarlos contra el código, no contra este plan).
- Produces: documentación canónica de SP-4d; serie SP-4 marcada completa.

- [ ] **Step 1: Actualizar `contracts/http/puntuaciones-api.md`**

Leer el archivo y, siguiendo su formato existente, agregar los dos endpoints:

- `GET /puntuaciones/partidas/{partidaId}/historial` — query params `limit` (default 100, máx. 500), `offset` (default 0), `tipo` (opcional, `TipoEvento` exacto); response `{ partidaId, total, entradas: [ { occurredAt, tipoEvento, juegoId|null, participanteId|null, equipoId|null, detalle: {} } ] }` en orden `occurredAt ASC`; códigos: 200, 400 (limit/offset inválidos), 401, **403 (rol `Participante`)**, 404 (partida no proyectada); autorización **solo `Operador`/`Administrador`**. Notas: `UbicacionActualizada` muestreada (máx. 1 por participante por minuto); las invitaciones de equipo no aparecen (Identity no publica al broker — limitación documentada).
- `GET /puntuaciones/participantes/{participanteId}/historial-partidas` — response `{ participanteId, partidas: [ { partidaId, modalidad, fechaFin, equipoId|null, puntosTotales, posicion, gano, juegos: [ { juegoId, orden, tipoJuego, puntos } ] } ] }` en orden `fechaFin DESC`; 200 con lista vacía para participante sin partidas o desconocido; 401 sin token; cualquier rol autenticado. Notas: participación = marcador propio (Individual) o membresía resuelta del historial con ≥1 marcador del equipo (excluye `ConvocatoriaCreada`); limitación: integrante sin acciones de juego autoradas no ve la partida; canceladas excluidas (RB-30).
- Actualizar la sección Status del documento: serie SP-4 completa (SP-4a/4b/4c/4d).

- [ ] **Step 2: Actualizar `contracts/events/operaciones-sesion-events.md`**

En la sección **Transport**, junto a la nota de la projection queue (SP-4a), agregar:

```markdown
- **Historial queue (SP-4d):** `puntuaciones.operaciones-sesion.historial`, durable, bound to `operaciones-sesion.#` (todos los eventos del registro). Consumed by the Puntuaciones history consumer (`eventos_historial`): dedup por índice único de `eventId` en la propia tabla, `UbicacionActualizada` muestreada al escribir (máx. 1 por participante por minuto), best-effort ack-siempre per ADR-0012. Payloads intactos.
```

y eliminar/ajustar la frase "Remaining events have no Puntuaciones consumer until SP-4b/4d" (ya tienen consumidor).

- [ ] **Step 3: Actualizar `services/puntuaciones/service-context.md`**

Leer el archivo y, siguiendo su formato: estado → SP-4d completo (historial/auditoría + HU-27 + purga + middleware); deudas **retiradas**: retención/índice de `eventos_procesados` y `ArgumentException`→400 sin log; deuda que **queda**: unit tests de las ramas warn+ack del worker de la era SP-4a; pending de la serie: vacío (SP-4 completa; cableado de clientes → SP-5). Documentar la segunda cola, la tabla `eventos_historial` y los dos endpoints nuevos.

- [ ] **Step 4: Actualizar `docs/04-sdd/traceability-matrix.md`**

Leer el archivo y agregar la fila SP-4d siguiendo el formato de las filas SP-4a/4b/4c: fuentes RF-12, RF-24, RF-35, RF-37, RB-15, RB-30, RB-31, HU-27, HU-43; artefactos: spec/design SP-4d, `eventos_historial`, `HistorialEventsConsumer`, los 2 endpoints, purga y middleware; limitaciones documentadas (invitaciones de equipo ausentes, miembro sin acciones no listado, muestreo de ubicaciones).

- [ ] **Step 5: Verificación final y commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: todos verdes. Verificar que cada shape documentado coincide con los DTOs del código (casing camelCase del JSON, nombres de campos).

```bash
git add contracts services/puntuaciones/service-context.md docs/04-sdd/traceability-matrix.md
git commit -m "docs(puntuaciones): contratos historial SP-4d, service-context y traceability (serie SP-4 completa)"
```

---

## Cierre del slice (post-plan)

- Review final whole-branch del rango SP-4d (patrón de los slices anteriores; ledger en `.superpowers/sdd/progress.md`).
- Recomendación heredada del review SP-4c: extraer `OnMessageReceived` de `Program.cs` — **no** entra en este plan (no hay tarea que toque ese bloque); queda anotada en el ledger para decidir en el review final.
- Post-slice: serie SP-4 completa → siguiente SP-5 (cableado de clientes web/móvil a HTTP + SignalR de Puntuaciones).
