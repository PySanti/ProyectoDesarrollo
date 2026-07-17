# Participación sin puntuar — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el historial y los dos rankings incluyan a todo el que participó, con 0 puntos si no anotó, en vez de solo a quien puntuó.

**Architecture:** Hoy los marcadores son el **único universo** de competidores de Puntuaciones, y un marcador solo nace al acreditar puntos. Se añaden dos proyecciones (participación y convocatorias) alimentadas por eventos que **ya se publican al broker** desde el Bloque 4B pero que la cola de proyección no escucha. Los dos calculadores pasan a operar sobre `participaciones ∪ marcadores`.

**Tech Stack:** .NET 8, EF Core + Npgsql (migraciones), MediatR, RabbitMQ, xUnit.

**Spec:** `docs/superpowers/specs/2026-07-15-participacion-sin-puntuar-design.md`

## Global Constraints

- **Solo se toca Puntuaciones.** Ni Operaciones de Sesión, ni Identity, ni el gateway, ni web, ni móvil.
- **Sin endpoints nuevos y sin cambiar la forma de ningún DTO.** Cambia **quién** sale en las listas, no **cómo**. Los contract tests existentes deben seguir verdes **sin tocarlos**: si hace falta modificarlos, algo se rompió.
- **Participar = inscripción aceptada.** No se exige "haber actuado". En Equipo, para el miembro, equivale a convocatoria aceptada.
- **Sin backfill.** Las partidas anteriores al cambio conservan el comportamiento actual (sin filas de participación). No escribir código de migración de datos.
- **Todo degrada al comportamiento de hoy, nada rompe** (best-effort, ADR-0012): evento perdido → el competidor no aparece, como hoy.
- **Las canceladas siguen excluidas** del historial (RB-30). No tocar ese filtro.
- **Idempotencia obligatoria** en los tres proyectores: patrón `EventoYaProcesadoAsync` + `RegistrarEventoProcesado`, como los proyectores existentes.

---

### Task 1: Entidades, persistencia y migración

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Entities/ParticipacionProyectada.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Entities/ConvocatoriaProyectada.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Domain/Abstractions/Persistence/IProyeccionesRepository.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/PuntuacionesDbContext.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure/Persistence/ProyeccionesRepository.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/Fakes/FakeProyeccionesRepository.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/ProyeccionesRepositoryTests.cs`

**Interfaces:**
- Produces (lo consumen las Tasks 2-6):
  ```csharp
  ParticipacionProyectada.Nueva(Guid partidaId, Guid competidorId, TipoCompetidor tipo)
  // props: PartidaId, CompetidorId, TipoCompetidor

  ConvocatoriaProyectada.Nueva(Guid convocatoriaId, Guid partidaId, Guid equipoId, Guid usuarioId)
  // props: ConvocatoriaId, PartidaId, EquipoId, UsuarioId, Aceptada (bool, nace false)
  ConvocatoriaProyectada.Responder(bool aceptada)

  // en IProyeccionesRepository:
  Task<ParticipacionProyectada?> GetParticipacionAsync(Guid partidaId, Guid competidorId, CancellationToken ct);
  void AddParticipacion(ParticipacionProyectada participacion);
  Task<IReadOnlyList<ParticipacionProyectada>> GetParticipacionesDePartidaAsync(Guid partidaId, CancellationToken ct);
  Task<ConvocatoriaProyectada?> GetConvocatoriaAsync(Guid convocatoriaId, CancellationToken ct);
  void AddConvocatoria(ConvocatoriaProyectada convocatoria);
  Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConParticipacionDeParticipanteAsync(Guid participanteId, CancellationToken ct);
  Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConParticipacionDeEquipoAsync(Guid equipoId, CancellationToken ct);
  Task<IReadOnlyList<ParticipacionEquipoHistorial>> GetEquiposConConvocatoriaAceptadaAsync(Guid usuarioId, CancellationToken ct);
  ```

`ParticipacionEquipoHistorial(Guid PartidaId, Guid EquipoId)` **ya existe** en `Domain/Abstractions/Persistence/ParticipacionEquipoHistorial.cs` — se reutiliza, no se crea.

Convención de la casa a respetar: tablas y columnas en minúscula sin separadores (`entity.ToTable("marcadores")`, `.HasColumnName("competidorid")`); ver `PuntuacionesDbContext.OnModelCreating`.

- [ ] **Step 1: Write the failing test**

Añadir a `ProyeccionesRepositoryTests.cs` (leer primero el arnés: hay un helper que crea el contexto con InMemory y un `NewCtx(...)`; **adaptar los nombres reales**):

```csharp
    [Fact]
    public async Task Participacion_se_guarda_y_se_lee_por_partida()
    {
        await using var ctx = NewCtx("part-" + Guid.NewGuid());
        var repo = new ProyeccionesRepository(ctx);
        var partidaId = Guid.NewGuid();
        var competidorId = Guid.NewGuid();
        repo.AddParticipacion(ParticipacionProyectada.Nueva(partidaId, competidorId, TipoCompetidor.Participante));
        await ctx.SaveChangesAsync();

        var r = await repo.GetParticipacionesDePartidaAsync(partidaId, CancellationToken.None);

        var p = Assert.Single(r);
        Assert.Equal(competidorId, p.CompetidorId);
        Assert.Equal(TipoCompetidor.Participante, p.TipoCompetidor);
    }

    [Fact]
    public async Task Convocatoria_se_responde_y_se_lee_por_usuario()
    {
        await using var ctx = NewCtx("conv-" + Guid.NewGuid());
        var repo = new ProyeccionesRepository(ctx);
        var partidaId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var c = ConvocatoriaProyectada.Nueva(convocatoriaId, partidaId, equipoId, usuarioId);
        repo.AddConvocatoria(c);
        await ctx.SaveChangesAsync();
        Assert.False(c.Aceptada); // nace pendiente

        c.Responder(true);
        await ctx.SaveChangesAsync();

        var r = await repo.GetEquiposConConvocatoriaAceptadaAsync(usuarioId, CancellationToken.None);
        var fila = Assert.Single(r);
        Assert.Equal(partidaId, fila.PartidaId);
        Assert.Equal(equipoId, fila.EquipoId);
    }

    [Fact]
    public async Task Convocatoria_rechazada_no_aparece_como_participacion()
    {
        await using var ctx = NewCtx("conv-rech-" + Guid.NewGuid());
        var repo = new ProyeccionesRepository(ctx);
        var usuarioId = Guid.NewGuid();
        var c = ConvocatoriaProyectada.Nueva(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), usuarioId);
        repo.AddConvocatoria(c);
        c.Responder(false);
        await ctx.SaveChangesAsync();

        // Rechazar es no participar: el criterio del spec es convocatoria ACEPTADA.
        Assert.Empty(await repo.GetEquiposConConvocatoriaAceptadaAsync(usuarioId, CancellationToken.None));
    }

    [Fact]
    public async Task Partidas_terminadas_con_participacion_de_participante_ignora_las_no_terminadas()
    {
        await using var ctx = NewCtx("part-term-" + Guid.NewGuid());
        var repo = new ProyeccionesRepository(ctx);
        var participanteId = Guid.NewGuid();
        var terminada = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Individual);
        terminada.Terminar(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        var enCurso = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Individual);
        repo.AddPartida(terminada);
        repo.AddPartida(enCurso);
        repo.AddParticipacion(ParticipacionProyectada.Nueva(terminada.PartidaId, participanteId, TipoCompetidor.Participante));
        repo.AddParticipacion(ParticipacionProyectada.Nueva(enCurso.PartidaId, participanteId, TipoCompetidor.Participante));
        await ctx.SaveChangesAsync();

        var r = await repo.GetPartidasTerminadasConParticipacionDeParticipanteAsync(participanteId, CancellationToken.None);

        var p = Assert.Single(r);
        Assert.Equal(terminada.PartidaId, p.PartidaId);
    }
```

**Verificar las firmas reales antes de escribir:** `PartidaProyectada.DesdePublicacion` y el método que la marca terminada (¿`Terminar(fecha)`?) se usan en los tests existentes de ese archivo — copiar de ahí, no inventar.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj" --filter "FullyQualifiedName~ProyeccionesRepositoryTests"`
Expected: FAIL de compilación — no existen `ParticipacionProyectada` ni `ConvocatoriaProyectada`.

- [ ] **Step 3: Crear las entidades**

`ParticipacionProyectada.cs`:

```csharp
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Domain.Entities;

// Quién compite en una partida, con independencia de si anotó. Antes de esta proyección el único
// universo de competidores era el de marcadores, y un marcador solo nace al acreditar puntos: quien
// no puntuaba no existía. La alimenta InscripcionAceptada.
public sealed class ParticipacionProyectada
{
    private ParticipacionProyectada(Guid partidaId, Guid competidorId, TipoCompetidor tipoCompetidor)
    {
        PartidaId = partidaId;
        CompetidorId = competidorId;
        TipoCompetidor = tipoCompetidor;
    }

    private ParticipacionProyectada() { } // EF

    public Guid PartidaId { get; private set; }
    public Guid CompetidorId { get; private set; }
    public TipoCompetidor TipoCompetidor { get; private set; }

    public static ParticipacionProyectada Nueva(Guid partidaId, Guid competidorId, TipoCompetidor tipoCompetidor)
        => new(partidaId, competidorId, tipoCompetidor);
}
```

`ConvocatoriaProyectada.cs`:

```csharp
namespace Umbral.Puntuaciones.Domain.Entities;

// Vínculo miembro↔equipo↔partida. Hacen falta los dos eventos: ConvocatoriaRespondida sabe quién
// aceptó pero NO de qué equipo, y ConvocatoriaCreada sabe el equipo pero no si aceptó. Se unen por
// ConvocatoriaId.
public sealed class ConvocatoriaProyectada
{
    private ConvocatoriaProyectada(Guid convocatoriaId, Guid partidaId, Guid equipoId, Guid usuarioId)
    {
        ConvocatoriaId = convocatoriaId;
        PartidaId = partidaId;
        EquipoId = equipoId;
        UsuarioId = usuarioId;
        Aceptada = false;
    }

    private ConvocatoriaProyectada() { } // EF

    public Guid ConvocatoriaId { get; private set; }
    public Guid PartidaId { get; private set; }
    public Guid EquipoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public bool Aceptada { get; private set; }

    public static ConvocatoriaProyectada Nueva(Guid convocatoriaId, Guid partidaId, Guid equipoId, Guid usuarioId)
        => new(convocatoriaId, partidaId, equipoId, usuarioId);

    public void Responder(bool aceptada) => Aceptada = aceptada;
}
```

- [ ] **Step 4: Configurar el DbContext**

En `PuntuacionesDbContext.cs`, añadir los DbSet junto a los existentes:

```csharp
    public DbSet<ParticipacionProyectada> Participaciones => Set<ParticipacionProyectada>();
    public DbSet<ConvocatoriaProyectada> Convocatorias => Set<ConvocatoriaProyectada>();
```

Y en `OnModelCreating`, tras el bloque de `Marcador`:

```csharp
        modelBuilder.Entity<ParticipacionProyectada>(entity =>
        {
            entity.ToTable("participaciones_proyectadas");
            entity.HasKey(x => new { x.PartidaId, x.CompetidorId });
            entity.Property(x => x.PartidaId).HasColumnName("partidaid");
            entity.Property(x => x.CompetidorId).HasColumnName("competidorid");
            entity.Property(x => x.TipoCompetidor).HasColumnName("tipocompetidor").IsRequired();
            entity.HasIndex(x => x.CompetidorId).HasDatabaseName("ix_participaciones_proyectadas_competidorid");
        });

        modelBuilder.Entity<ConvocatoriaProyectada>(entity =>
        {
            entity.ToTable("convocatorias_proyectadas");
            entity.HasKey(x => x.ConvocatoriaId);
            entity.Property(x => x.ConvocatoriaId).HasColumnName("convocatoriaid").ValueGeneratedNever();
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.EquipoId).HasColumnName("equipoid").IsRequired();
            entity.Property(x => x.UsuarioId).HasColumnName("usuarioid").IsRequired();
            entity.Property(x => x.Aceptada).HasColumnName("aceptada").IsRequired();
            entity.HasIndex(x => x.UsuarioId).HasDatabaseName("ix_convocatorias_proyectadas_usuarioid");
        });
```

- [ ] **Step 5: Implementar el repositorio**

En `IProyeccionesRepository.cs`, añadir los siete métodos del bloque **Interfaces** de arriba.

En `ProyeccionesRepository.cs`, implementarlos siguiendo el estilo del archivo (expression-bodied, `AsNoTracking()` en las lecturas que no se mutan):

```csharp
    public Task<ParticipacionProyectada?> GetParticipacionAsync(Guid partidaId, Guid competidorId, CancellationToken cancellationToken)
        => _db.Participaciones.FirstOrDefaultAsync(p => p.PartidaId == partidaId && p.CompetidorId == competidorId, cancellationToken);

    public void AddParticipacion(ParticipacionProyectada participacion) => _db.Participaciones.Add(participacion);

    public async Task<IReadOnlyList<ParticipacionProyectada>> GetParticipacionesDePartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => await _db.Participaciones.AsNoTracking().Where(p => p.PartidaId == partidaId).ToListAsync(cancellationToken);

    public Task<ConvocatoriaProyectada?> GetConvocatoriaAsync(Guid convocatoriaId, CancellationToken cancellationToken)
        => _db.Convocatorias.FirstOrDefaultAsync(c => c.ConvocatoriaId == convocatoriaId, cancellationToken);

    public void AddConvocatoria(ConvocatoriaProyectada convocatoria) => _db.Convocatorias.Add(convocatoria);

    // Participación (individual) = inscripción aceptada, no tener marcador.
    public async Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConParticipacionDeParticipanteAsync(Guid participanteId, CancellationToken cancellationToken)
        => await _db.Partidas.AsNoTracking()
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada
                && p.Modalidad == Modalidad.Individual
                && _db.Participaciones.Any(x => x.PartidaId == p.PartidaId
                    && x.CompetidorId == participanteId
                    && x.TipoCompetidor == TipoCompetidor.Participante))
            .OrderByDescending(p => p.FechaFin)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConParticipacionDeEquipoAsync(Guid equipoId, CancellationToken cancellationToken)
        => await _db.Partidas.AsNoTracking()
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada
                && p.Modalidad == Modalidad.Equipo
                && _db.Participaciones.Any(x => x.PartidaId == p.PartidaId
                    && x.CompetidorId == equipoId
                    && x.TipoCompetidor == TipoCompetidor.Equipo))
            .OrderByDescending(p => p.FechaFin)
            .ToListAsync(cancellationToken);

    // Solo Aceptada: que te convoquen no es jugar (puedes rechazar).
    public async Task<IReadOnlyList<ParticipacionEquipoHistorial>> GetEquiposConConvocatoriaAceptadaAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var filas = await _db.Convocatorias.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.Aceptada)
            .Select(c => new { c.PartidaId, c.EquipoId })
            .Distinct()
            .ToListAsync(cancellationToken);
        return filas.Select(f => new ParticipacionEquipoHistorial(f.PartidaId, f.EquipoId)).ToList();
    }
```

Comprobar los `using` del archivo: si faltan `Umbral.Puntuaciones.Domain.Enums` o `Umbral.Puntuaciones.Domain.Abstractions.Persistence`, añadirlos.

- [ ] **Step 6: Actualizar el fake de tests**

`FakeProyeccionesRepository.cs` implementa `IProyeccionesRepository`, así que **no compilará** hasta implementar los siete métodos nuevos. Seguir el estilo del fake (listas en memoria):

```csharp
    public List<ParticipacionProyectada> Participaciones { get; } = new();
    public List<ConvocatoriaProyectada> Convocatorias { get; } = new();

    public Task<ParticipacionProyectada?> GetParticipacionAsync(Guid partidaId, Guid competidorId, CancellationToken cancellationToken)
        => Task.FromResult(Participaciones.FirstOrDefault(p => p.PartidaId == partidaId && p.CompetidorId == competidorId));

    public void AddParticipacion(ParticipacionProyectada participacion) => Participaciones.Add(participacion);

    public Task<IReadOnlyList<ParticipacionProyectada>> GetParticipacionesDePartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ParticipacionProyectada>>(Participaciones.Where(p => p.PartidaId == partidaId).ToList());

    public Task<ConvocatoriaProyectada?> GetConvocatoriaAsync(Guid convocatoriaId, CancellationToken cancellationToken)
        => Task.FromResult(Convocatorias.FirstOrDefault(c => c.ConvocatoriaId == convocatoriaId));

    public void AddConvocatoria(ConvocatoriaProyectada convocatoria) => Convocatorias.Add(convocatoria);

    public Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConParticipacionDeParticipanteAsync(Guid participanteId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PartidaProyectada>>(Partidas
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada && p.Modalidad == Modalidad.Individual
                && Participaciones.Any(x => x.PartidaId == p.PartidaId && x.CompetidorId == participanteId
                    && x.TipoCompetidor == TipoCompetidor.Participante))
            .ToList());

    public Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConParticipacionDeEquipoAsync(Guid equipoId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PartidaProyectada>>(Partidas
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada && p.Modalidad == Modalidad.Equipo
                && Participaciones.Any(x => x.PartidaId == p.PartidaId && x.CompetidorId == equipoId
                    && x.TipoCompetidor == TipoCompetidor.Equipo))
            .ToList());

    public Task<IReadOnlyList<ParticipacionEquipoHistorial>> GetEquiposConConvocatoriaAceptadaAsync(Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ParticipacionEquipoHistorial>>(Convocatorias
            .Where(c => c.UsuarioId == usuarioId && c.Aceptada)
            .Select(c => new ParticipacionEquipoHistorial(c.PartidaId, c.EquipoId))
            .Distinct()
            .ToList());
```

El nombre real de la lista de partidas del fake (arriba se asume `Partidas`) hay que **verificarlo** leyendo el archivo.

- [ ] **Step 7: Generar la migración**

Run desde la raíz del repo:

```bash
dotnet ef migrations add SP4eParticipacionYConvocatorias \
  --project services/puntuaciones/src/Umbral.Puntuaciones.Infrastructure \
  --startup-project services/puntuaciones/src/Umbral.Puntuaciones.Api \
  --output-dir Persistence/Migrations
```

Expected: crea `Persistence/Migrations/<timestamp>_SP4eParticipacionYConvocatorias.cs` con `CreateTable` para las dos tablas. Las migraciones existentes viven ahí (`SP4aProyecciones`, `SP4bXminMarcadores`, `SP4dHistorial`) — el `--output-dir` es obligatorio o EF las pone en `Migrations/` a secas.

Abrir el archivo generado y comprobar que crea **solo** las dos tablas nuevas. Si toca alguna existente, el snapshot estaba desactualizado: parar y avisar.

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS — los 4 nuevos y todos los que ya existían.

- [ ] **Step 9: Commit**

```bash
git add services/puntuaciones/
git commit -m "feat(puntuaciones): proyecciones de participacion y convocatorias"
```

---

### Task 2: Proyectores y cableado del broker

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarInscripcionAceptadaCommand.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarConvocatoriaCreadaCommand.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Commands/ProyectarConvocatoriaRespondidaCommand.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarInscripcionAceptadaCommandHandler.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarConvocatoriaCreadaCommandHandler.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Commands/ProyectarConvocatoriaRespondidaCommandHandler.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/ProyeccionEventMapper.cs:22-32`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/RabbitMqConsumerOptions.cs:17-26`
- Create: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ProyectarParticipacionHandlersTests.cs`

**Interfaces:**
- Consumes: las entidades y los métodos de repositorio de la Task 1.
- Produces (lo consumen las Tasks 3-6): filas en `participaciones_proyectadas` y `convocatorias_proyectadas`.

**Forma real de los eventos** (`services/operaciones-sesion/.../Interfaces/ParticipacionEvents.cs`, verificada):

```
InscripcionAceptada  { partidaId, sesionPartidaId, inscripcionId, modalidad, participanteId?, equipoId?, instante }
ConvocatoriaCreada   { partidaId, sesionPartidaId, convocatoriaId, equipoId, usuarioId }
ConvocatoriaRespondida { partidaId, sesionPartidaId, convocatoriaId, usuarioId, estadoConvocatoria }
```

`modalidad` es `"Individual" | "Equipo"`; `estadoConvocatoria` es el `ToString()` de `{Pendiente, Aceptada, Rechazada}`. En `InscripcionAceptada` viaja `participanteId` **xor** `equipoId` según modalidad.

- [ ] **Step 1: Write the failing tests**

Crear `ProyectarParticipacionHandlersTests.cs`. Leer primero `ProyectarCicloDeVidaHandlersTests.cs` para copiar el arnés real (cómo construye `FakeProyeccionesRepository` y el fake de unit of work):

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.Handlers.Commands;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ProyectarParticipacionHandlersTests
{
    private static readonly DateTime T0 = new(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task InscripcionAceptada_individual_proyecta_al_participante()
    {
        var repo = new FakeProyeccionesRepository();
        var handler = new ProyectarInscripcionAceptadaCommandHandler(repo, new FakePuntuacionesUnitOfWork());
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();

        await handler.Handle(new ProyectarInscripcionAceptadaCommand(
            Guid.NewGuid(), T0, partidaId, "Individual", participanteId, null), CancellationToken.None);

        var p = Assert.Single(repo.Participaciones);
        Assert.Equal(participanteId, p.CompetidorId);
        Assert.Equal(TipoCompetidor.Participante, p.TipoCompetidor);
    }

    [Fact]
    public async Task InscripcionAceptada_equipo_proyecta_al_equipo()
    {
        var repo = new FakeProyeccionesRepository();
        var handler = new ProyectarInscripcionAceptadaCommandHandler(repo, new FakePuntuacionesUnitOfWork());
        var equipoId = Guid.NewGuid();

        await handler.Handle(new ProyectarInscripcionAceptadaCommand(
            Guid.NewGuid(), T0, Guid.NewGuid(), "Equipo", null, equipoId), CancellationToken.None);

        var p = Assert.Single(repo.Participaciones);
        // El competidor en Equipo es el equipo, no sus miembros.
        Assert.Equal(equipoId, p.CompetidorId);
        Assert.Equal(TipoCompetidor.Equipo, p.TipoCompetidor);
    }

    [Fact]
    public async Task InscripcionAceptada_repetida_no_duplica()
    {
        var repo = new FakeProyeccionesRepository();
        var uow = new FakePuntuacionesUnitOfWork();
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var cmd = new ProyectarInscripcionAceptadaCommand(eventId, T0, partidaId, "Individual", participanteId, null);

        await new ProyectarInscripcionAceptadaCommandHandler(repo, uow).Handle(cmd, CancellationToken.None);
        await new ProyectarInscripcionAceptadaCommandHandler(repo, uow).Handle(cmd, CancellationToken.None);

        Assert.Single(repo.Participaciones);
    }

    [Fact]
    public async Task ConvocatoriaCreada_nace_pendiente_y_ConvocatoriaRespondida_la_acepta()
    {
        var repo = new FakeProyeccionesRepository();
        var uow = new FakePuntuacionesUnitOfWork();
        var convocatoriaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        await new ProyectarConvocatoriaCreadaCommandHandler(repo, uow).Handle(
            new ProyectarConvocatoriaCreadaCommand(Guid.NewGuid(), T0, Guid.NewGuid(), convocatoriaId, Guid.NewGuid(), usuarioId),
            CancellationToken.None);
        Assert.False(Assert.Single(repo.Convocatorias).Aceptada);

        await new ProyectarConvocatoriaRespondidaCommandHandler(repo, uow).Handle(
            new ProyectarConvocatoriaRespondidaCommand(Guid.NewGuid(), T0, convocatoriaId, usuarioId, "Aceptada"),
            CancellationToken.None);

        Assert.True(Assert.Single(repo.Convocatorias).Aceptada);
    }

    [Fact]
    public async Task ConvocatoriaRespondida_rechazada_deja_aceptada_en_false()
    {
        var repo = new FakeProyeccionesRepository();
        var uow = new FakePuntuacionesUnitOfWork();
        var convocatoriaId = Guid.NewGuid();

        await new ProyectarConvocatoriaCreadaCommandHandler(repo, uow).Handle(
            new ProyectarConvocatoriaCreadaCommand(Guid.NewGuid(), T0, Guid.NewGuid(), convocatoriaId, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await new ProyectarConvocatoriaRespondidaCommandHandler(repo, uow).Handle(
            new ProyectarConvocatoriaRespondidaCommand(Guid.NewGuid(), T0, convocatoriaId, Guid.NewGuid(), "Rechazada"),
            CancellationToken.None);

        Assert.False(Assert.Single(repo.Convocatorias).Aceptada);
    }

    [Fact]
    public async Task ConvocatoriaRespondida_sin_creada_no_lanza()
    {
        var repo = new FakeProyeccionesRepository();

        // Best-effort (ADR-0012): si se pierde ConvocatoriaCreada no hay fila que actualizar
        // (falta el EquipoId para crearla). Se ackea y el miembro cae al comportamiento de hoy.
        await new ProyectarConvocatoriaRespondidaCommandHandler(repo, new FakePuntuacionesUnitOfWork()).Handle(
            new ProyectarConvocatoriaRespondidaCommand(Guid.NewGuid(), T0, Guid.NewGuid(), Guid.NewGuid(), "Aceptada"),
            CancellationToken.None);

        Assert.Empty(repo.Convocatorias);
    }
}
```

El nombre real del fake de unit of work (arriba `FakePuntuacionesUnitOfWork`) hay que **verificarlo** en `Fakes/`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~ProyectarParticipacionHandlersTests"`
Expected: FAIL de compilación — no existen los commands ni los handlers.

- [ ] **Step 3: Crear los commands**

`ProyectarInscripcionAceptadaCommand.cs`:

```csharp
using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarInscripcionAceptadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, string Modalidad,
    Guid? ParticipanteId, Guid? EquipoId) : IRequest;
```

`ProyectarConvocatoriaCreadaCommand.cs`:

```csharp
using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarConvocatoriaCreadaCommand(
    Guid EventId, DateTime OccurredAt, Guid PartidaId, Guid ConvocatoriaId,
    Guid EquipoId, Guid UsuarioId) : IRequest;
```

`ProyectarConvocatoriaRespondidaCommand.cs`:

```csharp
using MediatR;

namespace Umbral.Puntuaciones.Application.Commands;

public sealed record ProyectarConvocatoriaRespondidaCommand(
    Guid EventId, DateTime OccurredAt, Guid ConvocatoriaId, Guid UsuarioId,
    string EstadoConvocatoria) : IRequest;
```

- [ ] **Step 4: Crear los handlers**

`ProyectarInscripcionAceptadaCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarInscripcionAceptadaCommandHandler : IRequestHandler<ProyectarInscripcionAceptadaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarInscripcionAceptadaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarInscripcionAceptadaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        // Identidad dual: en Equipo el competidor es el equipo; en Individual, el participante.
        // El evento trae participanteId xor equipoId segun modalidad.
        var esEquipo = request.Modalidad == "Equipo";
        var competidorId = esEquipo ? request.EquipoId : request.ParticipanteId;
        if (competidorId is null)
        {
            // Payload incoherente con su modalidad: no hay competidor que proyectar.
            return;
        }

        var tipo = esEquipo ? TipoCompetidor.Equipo : TipoCompetidor.Participante;
        if (await _repo.GetParticipacionAsync(request.PartidaId, competidorId.Value, cancellationToken) is null)
        {
            _repo.AddParticipacion(ParticipacionProyectada.Nueva(request.PartidaId, competidorId.Value, tipo));
        }

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "InscripcionAceptada", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
```

`ProyectarConvocatoriaCreadaCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarConvocatoriaCreadaCommandHandler : IRequestHandler<ProyectarConvocatoriaCreadaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarConvocatoriaCreadaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarConvocatoriaCreadaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        if (await _repo.GetConvocatoriaAsync(request.ConvocatoriaId, cancellationToken) is null)
        {
            _repo.AddConvocatoria(ConvocatoriaProyectada.Nueva(
                request.ConvocatoriaId, request.PartidaId, request.EquipoId, request.UsuarioId));
        }

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "ConvocatoriaCreada", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
```

`ProyectarConvocatoriaRespondidaCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarConvocatoriaRespondidaCommandHandler : IRequestHandler<ProyectarConvocatoriaRespondidaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarConvocatoriaRespondidaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarConvocatoriaRespondidaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        // Si se perdio ConvocatoriaCreada no hay fila que actualizar y no se puede crear: este
        // evento no trae EquipoId. Se ackea (best-effort ADR-0012) y el miembro cae al
        // comportamiento previo — solo ve la partida si actuo.
        var convocatoria = await _repo.GetConvocatoriaAsync(request.ConvocatoriaId, cancellationToken);
        convocatoria?.Responder(request.EstadoConvocatoria == "Aceptada");

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "ConvocatoriaRespondida", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
```

Verificar el nombre real de la interfaz de unit of work (`IPuntuacionesUnitOfWork`) y la firma de `EventoProcesado.Registrar` leyendo `ProyectarPuntajeTriviaCommandHandler.cs:39-41`.

- [ ] **Step 5: Cablear el mapper**

En `ProyeccionEventMapper.cs`, añadir los tres casos al `switch` (líneas 22-32):

```csharp
                "InscripcionAceptada" => MapInscripcionAceptada(envelope),
                "ConvocatoriaCreada" => MapConvocatoriaCreada(envelope),
                "ConvocatoriaRespondida" => MapConvocatoriaRespondida(envelope),
```

Y, junto al resto de payloads y mappers privados:

```csharp
    private sealed record InscripcionAceptadaPayload(
        Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, string Modalidad,
        Guid? ParticipanteId, Guid? EquipoId, DateTime Instante);
    private sealed record ConvocatoriaCreadaPayload(
        Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid EquipoId, Guid UsuarioId);
    private sealed record ConvocatoriaRespondidaPayload(
        Guid PartidaId, Guid SesionPartidaId, Guid ConvocatoriaId, Guid UsuarioId, string EstadoConvocatoria);

    private static IBaseRequest? MapInscripcionAceptada(EnvelopeResumen e)
        => Deserializar<InscripcionAceptadaPayload>(e) is { } p
            ? new ProyectarInscripcionAceptadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.Modalidad, p.ParticipanteId, p.EquipoId)
            : null;

    private static IBaseRequest? MapConvocatoriaCreada(EnvelopeResumen e)
        => Deserializar<ConvocatoriaCreadaPayload>(e) is { } p
            ? new ProyectarConvocatoriaCreadaCommand(e.EventId, e.OccurredAt, p.PartidaId, p.ConvocatoriaId, p.EquipoId, p.UsuarioId)
            : null;

    private static IBaseRequest? MapConvocatoriaRespondida(EnvelopeResumen e)
        => Deserializar<ConvocatoriaRespondidaPayload>(e) is { } p
            ? new ProyectarConvocatoriaRespondidaCommand(e.EventId, e.OccurredAt, p.ConvocatoriaId, p.UsuarioId, p.EstadoConvocatoria)
            : null;
```

`Modalidad` se deserializa como `string`, **no** como el enum `Modalidad` del dominio: el handler compara con `"Equipo"` y así un valor inesperado no revienta la deserialización del envelope.

- [ ] **Step 6: Ligar las tres rutas**

En `RabbitMqConsumerOptions.cs`, añadir al array `Bindings` (líneas 17-26):

```csharp
        "operaciones-sesion.inscripcion-aceptada.v1",
        "operaciones-sesion.convocatoria-creada.v1",
        "operaciones-sesion.convocatoria-respondida.v1",
```

Sin esto los eventos **no llegan a la cola** y las proyecciones quedan vacías: la cola liga rutas explícitas, no `#`. Las tres claves están verificadas contra `SesionEventRouting.cs:22,23,28`.

> **Nota de despliegue (no es código):** la cola `puntuaciones.operaciones-sesion.proyecciones` ya existe en RabbitMQ con sus bindings actuales. `QueueBind` es idempotente y añade los nuevos al arrancar, así que no hace falta borrar la cola. Los eventos publicados **antes** de este arranque no se recuperan — coherente con "sin backfill".

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS — los 6 nuevos y todos los existentes.

- [ ] **Step 8: Commit**

```bash
git add services/puntuaciones/
git commit -m "feat(puntuaciones): proyectores de inscripcion aceptada y convocatorias"
```

---

### Task 3: Ranking por juego incluye a quien no anotó

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/RankingCalculator.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerRankingJuegoQueryHandler.cs:23-24`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerRankingJuegoQueryHandlerTests.cs`
- Test: crear `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/RankingCalculatorTests.cs` **solo si no existe** (comprobar antes con `ls`)

**Interfaces:**
- Consumes: `GetParticipacionesDePartidaAsync` (Task 1).
- Produces (lo consume nadie más; la Task 4 hace lo análogo en el otro calculador):
  ```csharp
  RankingCalculator.Calcular(
      IEnumerable<Marcador> marcadores, IEnumerable<ParticipacionProyectada> participaciones)
      → IReadOnlyList<EntradaRankingDto>
  ```

**El calculador es estático y toma colecciones:** sus tests no necesitan repositorio, se le pasan listas.

`RankingCalculator` es **por juego** y las participaciones son **por partida**: quien participa en la partida participa en todos sus juegos, así que el universo del juego es `participaciones(partida) ∪ marcadores(juego)`.

Efecto visible: hoy, al arrancar un juego, nadie ha puntuado y el operador ve una tabla **vacía**.

- [ ] **Step 1: Write the failing test**

Crear (o añadir a) `RankingCalculatorTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Xunit;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class RankingCalculatorTests
{
    [Fact]
    public void Participante_sin_marcador_sale_ultimo_con_cero()
    {
        var juegoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var anotador = Guid.NewGuid();
        var mudo = Guid.NewGuid();
        var m = Marcador.Nuevo(juegoId, anotador, partidaId, TipoCompetidor.Participante);
        m.Acreditar(10, 500);

        var r = RankingCalculator.Calcular(
            new[] { m },
            new[]
            {
                ParticipacionProyectada.Nueva(partidaId, anotador, TipoCompetidor.Participante),
                ParticipacionProyectada.Nueva(partidaId, mudo, TipoCompetidor.Participante)
            });

        Assert.Equal(2, r.Count);
        Assert.Equal(anotador, r[0].CompetidorId);
        Assert.Equal(mudo, r[1].CompetidorId);
        Assert.Equal(0, r[1].Puntos);
        Assert.Equal(2, r[1].Posicion);
    }

    [Fact]
    public void Al_arrancar_el_juego_todos_salen_a_cero_en_vez_de_lista_vacia()
    {
        var partidaId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var r = RankingCalculator.Calcular(
            Array.Empty<Marcador>(),
            new[]
            {
                ParticipacionProyectada.Nueva(partidaId, a, TipoCompetidor.Participante),
                ParticipacionProyectada.Nueva(partidaId, b, TipoCompetidor.Participante)
            });

        // Antes: entradas vacías hasta el primer acierto; el operador no veía a nadie.
        Assert.Equal(2, r.Count);
        Assert.All(r, e => Assert.Equal(0, e.Puntos));
        Assert.All(r, e => Assert.Equal(1, e.Posicion)); // 0/0 empatan exacto
    }

    [Fact]
    public void Sin_participaciones_ni_marcadores_devuelve_vacio()
    {
        var r = RankingCalculator.Calcular(Array.Empty<Marcador>(), Array.Empty<ParticipacionProyectada>());

        Assert.Empty(r);
    }

    [Fact]
    public void Competidor_con_marcador_pero_sin_participacion_sigue_saliendo()
    {
        // Si se perdió InscripcionAceptada (best-effort ADR-0012), el marcador prueba que jugó:
        // el universo es la UNIÓN, no solo las participaciones.
        var juegoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var m = Marcador.Nuevo(juegoId, Guid.NewGuid(), partidaId, TipoCompetidor.Participante);
        m.Acreditar(5, 100);

        var r = RankingCalculator.Calcular(new[] { m }, Array.Empty<ParticipacionProyectada>());

        Assert.Single(r);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~RankingCalculatorTests"`
Expected: FAIL de compilación — `Calcular` toma un solo argumento.

- [ ] **Step 3: Write the implementation**

Reescribir `RankingCalculator.cs` entero:

```csharp
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

public static class RankingCalculator
{
    // Universo = participaciones ∪ marcadores. Antes eran solo los marcadores, y un marcador solo
    // nace al acreditar puntos: quien no anotaba no aparecía, y al arrancar el juego la tabla salía
    // vacía. Se mantiene la unión (no solo participaciones) porque si se pierde InscripcionAceptada
    // el marcador prueba que jugó.
    private sealed record Fila(Guid CompetidorId, TipoCompetidor Tipo, int Puntos, long TiempoMs, int Unidades);

    public static IReadOnlyList<EntradaRankingDto> Calcular(
        IEnumerable<Marcador> marcadores, IEnumerable<ParticipacionProyectada> participaciones)
    {
        var filas = marcadores
            .Select(m => new Fila(m.CompetidorId, m.TipoCompetidor, m.PuntosAcumulados, m.TiempoAcumuladoMs, m.UnidadesGanadas))
            .ToList();

        // Materializar antes de concatenar: filas se lee dentro del Where.
        var sinMarcador = participaciones
            .Where(p => filas.All(f => f.CompetidorId != p.CompetidorId))
            .Select(p => new Fila(p.CompetidorId, p.TipoCompetidor, 0, 0L, 0))
            .ToList();

        var ordenados = filas
            .Concat(sinMarcador)
            .OrderByDescending(f => f.Puntos)
            .ThenBy(f => f.TiempoMs)
            .ToList();

        var entradas = new List<EntradaRankingDto>(ordenados.Count);
        for (var i = 0; i < ordenados.Count; i++)
        {
            var actual = ordenados[i];
            var posicion = i + 1;
            if (i > 0)
            {
                var previo = ordenados[i - 1];
                var empateExacto = previo.Puntos == actual.Puntos && previo.TiempoMs == actual.TiempoMs;
                if (empateExacto)
                {
                    posicion = entradas[i - 1].Posicion;
                }
            }

            entradas.Add(new EntradaRankingDto(
                posicion, actual.CompetidorId, actual.Tipo, actual.Puntos, actual.TiempoMs, actual.Unidades));
        }

        return entradas;
    }
}
```

**El `.ToList()` de `sinMarcador` no es decorativo:** sin él, `Where` es perezoso y se evalúa durante el `Concat`, leyendo `filas` mientras se recorre. Materializar deja el orden de evaluación explícito.

Actualizar `ObtenerRankingJuegoQueryHandler.cs` (líneas 23-24):

```csharp
        var marcadores = await _repo.GetMarcadoresDeJuegoAsync(request.JuegoId, cancellationToken);
        var participaciones = await _repo.GetParticipacionesDePartidaAsync(request.PartidaId, cancellationToken);
        return new RankingJuegoResponse(
            juego.JuegoId, juego.TipoJuego, DateTime.UtcNow,
            RankingCalculator.Calcular(marcadores, participaciones));
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS. Si algún test de `ObtenerRankingJuegoQueryHandlerTests` falla porque ahora espera entradas que antes no existían, **leerlo antes de tocarlo**: si afirmaba "sin marcadores → vacío", codifica el comportamiento que este slice deroga y se sustituye por la versión nueva; anotarlo en el mensaje de commit.

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones/
git commit -m "feat(puntuaciones): el ranking por juego incluye a quien no anoto"
```

---

### Task 4: Ranking consolidado incluye a quien no anotó

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/CalculadorRankingConsolidado.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerRankingConsolidadoQueryHandler.cs:29-31`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/CalculadorRankingConsolidadoTests.cs`

**Interfaces:**
- Consumes: `GetParticipacionesDePartidaAsync` (Task 1).
- Produces (lo consumen las Tasks 5 y 6):
  ```csharp
  CalculadorRankingConsolidado.Calcular(
      IEnumerable<Marcador> marcadoresDePartida, IEnumerable<ParticipacionProyectada> participaciones)
      → IReadOnlyList<EntradaRankingConsolidadoDto>
  ```

**Ojo con la guarda de salida temprana.** Hoy es `if (marcadores.Count == 0) return vacío` (línea 15). Debe pasar a exigir que **ambas** colecciones estén vacías: si hay participaciones y ningún marcador, la respuesta correcta es todos a 0, no una lista vacía.

`ganadoresPorJuego` **no cambia**: se agrupa sobre marcadores y quien no anotó no gana ningún juego.

- [ ] **Step 1: Write the failing test**

Añadir a `CalculadorRankingConsolidadoTests.cs` (leer el arnés existente y reusar sus helpers si los hay):

```csharp
    [Fact]
    public void Competidor_con_participacion_y_sin_marcador_sale_ultimo_con_cero()
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var anotador = Guid.NewGuid();
        var mudo = Guid.NewGuid();
        var m = Marcador.Nuevo(juegoId, anotador, partidaId, TipoCompetidor.Participante);
        m.Acreditar(30, 900);

        var r = CalculadorRankingConsolidado.Calcular(
            new[] { m },
            new[]
            {
                ParticipacionProyectada.Nueva(partidaId, anotador, TipoCompetidor.Participante),
                ParticipacionProyectada.Nueva(partidaId, mudo, TipoCompetidor.Participante)
            });

        Assert.Equal(2, r.Count);
        Assert.Equal(anotador, r[0].CompetidorId);
        Assert.Equal(1, r[0].JuegosGanados);
        Assert.Equal(mudo, r[1].CompetidorId);
        Assert.Equal(0, r[1].PuntosTotales);
        Assert.Equal(0, r[1].JuegosGanados);
        Assert.Equal(2, r[1].Posicion);
    }

    [Fact]
    public void Dos_sin_anotar_comparten_posicion()
    {
        var partidaId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var r = CalculadorRankingConsolidado.Calcular(
            Array.Empty<Marcador>(),
            new[]
            {
                ParticipacionProyectada.Nueva(partidaId, a, TipoCompetidor.Participante),
                ParticipacionProyectada.Nueva(partidaId, b, TipoCompetidor.Participante)
            });

        // 0 juegos ganados, 0 puntos, 0 ms: empate exacto → misma posición.
        Assert.Equal(2, r.Count);
        Assert.All(r, e => Assert.Equal(1, e.Posicion));
    }

    [Fact]
    public void Sin_participaciones_ni_marcadores_devuelve_vacio()
    {
        var r = CalculadorRankingConsolidado.Calcular(
            Array.Empty<Marcador>(), Array.Empty<ParticipacionProyectada>());

        Assert.Empty(r);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~CalculadorRankingConsolidadoTests"`
Expected: FAIL de compilación — `Calcular` toma un solo argumento.

- [ ] **Step 3: Write the implementation**

En `CalculadorRankingConsolidado.cs`, cambiar la firma, la guarda y añadir los ceros. El bloque de `ganadoresPorJuego` (líneas 20-34) se deja **igual**:

```csharp
    private sealed record Agregado(
        Guid CompetidorId, TipoCompetidor TipoCompetidor, int JuegosGanados, int PuntosTotales, long TiempoTotalMs);

    public static IReadOnlyList<EntradaRankingConsolidadoDto> Calcular(
        IEnumerable<Marcador> marcadoresDePartida, IEnumerable<ParticipacionProyectada> participaciones)
    {
        var marcadores = marcadoresDePartida.ToList();
        var participes = participaciones.ToList();
        // Con participaciones y sin marcadores la respuesta correcta es todos a 0, no lista vacía.
        if (marcadores.Count == 0 && participes.Count == 0)
        {
            return Array.Empty<EntradaRankingConsolidadoDto>();
        }

        // ... ganadoresPorJuego se queda tal cual ...

        var agregados = marcadores
            .GroupBy(m => m.CompetidorId)
            .Select(g => new Agregado(
                g.Key,
                g.First().TipoCompetidor,
                ganadoresPorJuego.Count(id => id == g.Key),
                g.Sum(m => m.PuntosAcumulados),
                g.Sum(m => m.TiempoAcumuladoMs)))
            .ToList();

        // Participó y no anotó: entra con ceros. Materializar antes del AddRange — el Where lee
        // `agregados`, y añadirle elementos mientras se enumera lo rompería.
        var sinMarcador = participes
            .Where(p => agregados.All(a => a.CompetidorId != p.CompetidorId))
            .Select(p => new Agregado(p.CompetidorId, p.TipoCompetidor, 0, 0, 0L))
            .ToList();
        agregados.AddRange(sinMarcador);

        var ordenados = agregados
            .OrderByDescending(a => a.JuegosGanados)
            .ThenByDescending(a => a.PuntosTotales)
            .ThenBy(a => a.TiempoTotalMs)
            .ToList();

        var entradas = new List<EntradaRankingConsolidadoDto>(ordenados.Count);
        for (var i = 0; i < ordenados.Count; i++)
        {
            var actual = ordenados[i];
            var posicion = i + 1;
            if (i > 0)
            {
                var previo = ordenados[i - 1];
                var empateExacto = previo.JuegosGanados == actual.JuegosGanados
                    && previo.PuntosTotales == actual.PuntosTotales
                    && previo.TiempoTotalMs == actual.TiempoTotalMs;
                if (empateExacto)
                {
                    posicion = entradas[i - 1].Posicion;
                }
            }

            entradas.Add(new EntradaRankingConsolidadoDto(
                posicion, actual.CompetidorId, actual.TipoCompetidor,
                actual.JuegosGanados, actual.PuntosTotales, actual.TiempoTotalMs));
        }

        return entradas;
    }
```

El `agregados` anónimo de hoy pasa a ser el record `Agregado` para poder mezclarlo con los ceros: dos tipos anónimos distintos no son compatibles en un `AddRange`.

Requiere `using Umbral.Puntuaciones.Domain.Enums;` si no está.

Actualizar `ObtenerRankingConsolidadoQueryHandler.cs` (líneas 29-31):

```csharp
        var marcadores = await _repo.GetMarcadoresDePartidaAsync(request.PartidaId, cancellationToken);
        var participaciones = await _repo.GetParticipacionesDePartidaAsync(request.PartidaId, cancellationToken);
        return new RankingConsolidadoResponse(
            request.PartidaId, DateTime.UtcNow,
            CalculadorRankingConsolidado.Calcular(marcadores, participaciones));
```

- [ ] **Step 4: Arreglar los otros dos llamantes del calculador**

`CalculadorRankingConsolidado.Calcular` lo llaman otros dos handlers, que ahora **no compilan**. Aquí
solo se les pasa el argumento nuevo; **el filtro de participación de cada uno es cosa de las Tasks 5
y 6** — no adelantarlo.

En `ObtenerHistorialPartidasQueryHandler.ConstruirPartidaJugadaAsync`:

```csharp
        var marcadores = await _proyecciones.GetMarcadoresDePartidaAsync(partida.PartidaId, cancellationToken);
        var participaciones = await _proyecciones.GetParticipacionesDePartidaAsync(partida.PartidaId, cancellationToken);
        var entradas = CalculadorRankingConsolidado.Calcular(marcadores, participaciones);
```

En `ObtenerRendimientoEquipoQueryHandler.Handle`, dentro del `foreach`:

```csharp
            var marcadores = await _repo.GetMarcadoresDePartidaAsync(partida.PartidaId, cancellationToken);
            var participaciones = await _repo.GetParticipacionesDePartidaAsync(partida.PartidaId, cancellationToken);
            var entradas = CalculadorRankingConsolidado.Calcular(marcadores, participaciones);
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/puntuaciones/
git commit -m "feat(puntuaciones): el consolidado incluye a quien no anoto"
```

---

### Task 5: Historial del participante por participación

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerHistorialPartidasQueryHandler.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerHistorialPartidasQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `GetPartidasTerminadasConParticipacionDeParticipanteAsync`, `GetEquiposConConvocatoriaAceptadaAsync`, `GetParticipacionesDePartidaAsync` (Task 1); `CalculadorRankingConsolidado.Calcular(marcadores, participaciones)` (Task 4).

**Las dos condiciones del caso Equipo son necesarias; no simplificar a una.** En teoría la segunda sobra —las convocatorias solo se crean al aceptar la preinscripción, así que un miembro convocado implica un equipo aceptado—, pero la entrega es best-effort: si se perdiera `InscripcionAceptada` y sí llegara `ConvocatoriaCreada`, el equipo no estaría ni en participaciones ni en marcadores y el `entradas.First(...)` **lanzaría**.

El handler deja de usar `IHistorialRepository`: si tras el cambio `_historial` queda sin usos, quitar el campo y el parámetro del constructor, y actualizar a quien lo construya.

- [ ] **Step 1: Write the failing test**

Añadir a `ObtenerHistorialPartidasQueryHandlerTests.cs` (leer el arnés real y reusar sus helpers):

```csharp
    [Fact]
    public async Task Incluye_partida_individual_terminada_donde_no_puntuo()
    {
        var repo = new FakeProyeccionesRepository();
        var participanteId = Guid.NewGuid();
        var partida = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Individual);
        partida.Terminar(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        repo.Partidas.Add(partida);
        repo.Participaciones.Add(ParticipacionProyectada.Nueva(partida.PartidaId, participanteId, TipoCompetidor.Participante));

        var r = await new ObtenerHistorialPartidasQueryHandler(repo).Handle(
            new ObtenerHistorialPartidasQuery(participanteId), CancellationToken.None);

        // Antes: sin marcador la partida no aparecía; jugar y no acertar era no haber jugado.
        var jugada = Assert.Single(r.Partidas);
        Assert.Equal(partida.PartidaId, jugada.PartidaId);
        Assert.Equal(0, jugada.PuntosTotales);
        Assert.False(jugada.Gano);
    }

    [Fact]
    public async Task Miembro_que_acepto_convocatoria_y_no_actuo_ve_la_partida()
    {
        var repo = new FakeProyeccionesRepository();
        var usuarioId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var partida = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Equipo);
        partida.Terminar(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        repo.Partidas.Add(partida);
        repo.Participaciones.Add(ParticipacionProyectada.Nueva(partida.PartidaId, equipoId, TipoCompetidor.Equipo));
        var convocatoria = ConvocatoriaProyectada.Nueva(Guid.NewGuid(), partida.PartidaId, equipoId, usuarioId);
        convocatoria.Responder(true);
        repo.Convocatorias.Add(convocatoria);

        var r = await new ObtenerHistorialPartidasQueryHandler(repo).Handle(
            new ObtenerHistorialPartidasQuery(usuarioId), CancellationToken.None);

        // Retira la limitación: antes solo veía la partida si había autorado una acción de juego.
        var jugada = Assert.Single(r.Partidas);
        Assert.Equal(equipoId, jugada.EquipoId);
    }

    [Fact]
    public async Task Miembro_con_convocatoria_rechazada_no_ve_la_partida()
    {
        var repo = new FakeProyeccionesRepository();
        var usuarioId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var partida = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Equipo);
        partida.Terminar(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        repo.Partidas.Add(partida);
        repo.Participaciones.Add(ParticipacionProyectada.Nueva(partida.PartidaId, equipoId, TipoCompetidor.Equipo));
        var convocatoria = ConvocatoriaProyectada.Nueva(Guid.NewGuid(), partida.PartidaId, equipoId, usuarioId);
        convocatoria.Responder(false);
        repo.Convocatorias.Add(convocatoria);

        var r = await new ObtenerHistorialPartidasQueryHandler(repo).Handle(
            new ObtenerHistorialPartidasQuery(usuarioId), CancellationToken.None);

        Assert.Empty(r.Partidas);
    }

    [Fact]
    public async Task Equipo_sin_participacion_proyectada_no_rompe_al_miembro()
    {
        var repo = new FakeProyeccionesRepository();
        var usuarioId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var partida = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Equipo);
        partida.Terminar(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        repo.Partidas.Add(partida);
        // Sin participación del equipo (InscripcionAceptada perdida, best-effort ADR-0012).
        var convocatoria = ConvocatoriaProyectada.Nueva(Guid.NewGuid(), partida.PartidaId, equipoId, usuarioId);
        convocatoria.Responder(true);
        repo.Convocatorias.Add(convocatoria);

        var r = await new ObtenerHistorialPartidasQueryHandler(repo).Handle(
            new ObtenerHistorialPartidasQuery(usuarioId), CancellationToken.None);

        // Debe omitir la partida, no lanzar: sin el guard, entradas.First(...) reventaría.
        Assert.Empty(r.Partidas);
    }
```

El handler se construye **solo con `IProyeccionesRepository`**: el Step 3 le retira `IHistorialRepository`, porque la pertenencia a equipo pasa a resolverse por convocatoria aceptada y ya no se consulta el relato de auditoría. Si el arnés del archivo tiene un helper `Construir(...)` con dos dependencias, ajustarlo a una.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~ObtenerHistorialPartidasQueryHandlerTests"`
Expected: FAIL — la partida sin marcador no aparece.

- [ ] **Step 3: Write the implementation**

Reescribir el cuerpo de `ObtenerHistorialPartidasQueryHandler`. El comentario de cabecera (líneas 10-14) debe reescribirse: ya no hay limitación del integrante pasivo.

```csharp
// HU-27 (RF-24): historial único de partidas jugadas con puntuación y posición. Participación =
// inscripción aceptada (Individual) o convocatoria aceptada al equipo (Equipo) — no exige haber
// anotado. Posición/gano del mismo CalculadorRankingConsolidado de SP-4b (RF-44: sin duplicar el
// cálculo). Canceladas excluidas (RB-30).
public sealed class ObtenerHistorialPartidasQueryHandler
    : IRequestHandler<ObtenerHistorialPartidasQuery, HistorialPartidasResponse>
{
    private readonly IProyeccionesRepository _proyecciones;

    public ObtenerHistorialPartidasQueryHandler(IProyeccionesRepository proyecciones)
        => _proyecciones = proyecciones;

    public async Task<HistorialPartidasResponse> Handle(
        ObtenerHistorialPartidasQuery request, CancellationToken cancellationToken)
    {
        var partidas = new List<PartidaJugadaDto>();

        var individuales = await _proyecciones.GetPartidasTerminadasConParticipacionDeParticipanteAsync(
            request.ParticipanteId, cancellationToken);
        foreach (var partida in individuales)
        {
            partidas.Add(await ConstruirPartidaJugadaAsync(
                partida, competidorId: request.ParticipanteId, equipoId: null, cancellationToken));
        }

        foreach (var participacion in await _proyecciones.GetEquiposConConvocatoriaAceptadaAsync(
            request.ParticipanteId, cancellationToken))
        {
            var partida = await _proyecciones.GetPartidaAsync(participacion.PartidaId, cancellationToken);
            if (partida is null || partida.Estado != EstadoPartidaProyectada.Terminada)
            {
                continue;
            }
            // Segundo guard imprescindible: si se perdió InscripcionAceptada del equipo, éste no
            // está en el universo del calculador y el First() de abajo lanzaría.
            var participes = await _proyecciones.GetParticipacionesDePartidaAsync(partida.PartidaId, cancellationToken);
            var marcadores = await _proyecciones.GetMarcadoresDePartidaAsync(partida.PartidaId, cancellationToken);
            var equipoEnUniverso = participes.Any(p => p.CompetidorId == participacion.EquipoId)
                || marcadores.Any(m => m.CompetidorId == participacion.EquipoId);
            if (!equipoEnUniverso)
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
        var participaciones = await _proyecciones.GetParticipacionesDePartidaAsync(partida.PartidaId, cancellationToken);
        var entradas = CalculadorRankingConsolidado.Calcular(marcadores, participaciones);
        // Los filtros de Handle garantizan que el competidor está en el universo del calculador.
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

Quitar el `using` de `IHistorialRepository` si queda sin usar. Si algo construye este handler con dos dependencias (DI lo resuelve solo; los tests no), ajustarlo.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS. Si un test viejo afirmaba que el integrante sin acciones **no** ve la partida, codifica la limitación que este slice retira: sustituirlo y anotarlo en el commit.

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones/
git commit -m "feat(puntuaciones): el historial del participante va por participacion, no por marcador"
```

---

### Task 6: Rendimiento de equipo por participación

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Handlers/Queries/ObtenerRendimientoEquipoQueryHandler.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Application/ObtenerRendimientoEquipoQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `GetPartidasTerminadasConParticipacionDeEquipoAsync`, `GetParticipacionesDePartidaAsync` (Task 1); `CalculadorRankingConsolidado.Calcular(marcadores, participaciones)` (Task 4).

- [ ] **Step 1: Write the failing test**

Añadir a `ObtenerRendimientoEquipoQueryHandlerTests.cs`:

```csharp
    [Fact]
    public async Task Incluye_partida_donde_el_equipo_no_anoto()
    {
        var repo = new FakeProyeccionesRepository();
        var equipoId = Guid.NewGuid();
        var partida = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Equipo);
        partida.Terminar(new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        repo.Partidas.Add(partida);
        repo.Participaciones.Add(ParticipacionProyectada.Nueva(partida.PartidaId, equipoId, TipoCompetidor.Equipo));

        var r = await new ObtenerRendimientoEquipoQueryHandler(repo).Handle(
            new ObtenerRendimientoEquipoQuery(equipoId), CancellationToken.None);

        // Antes: solo partidas "donde el equipo anotó".
        var fila = Assert.Single(r.Partidas);
        Assert.Equal(partida.PartidaId, fila.PartidaId);
        Assert.False(fila.Gano);
    }
```

Adaptar al arnés real del archivo (nombre del helper de construcción y de la lista de partidas del fake).

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~ObtenerRendimientoEquipoQueryHandlerTests"`
Expected: FAIL — la partida sin marcador del equipo no aparece.

- [ ] **Step 3: Write the implementation**

En `ObtenerRendimientoEquipoQueryHandler.cs`, reescribir el comentario de cabecera y el cuerpo:

```csharp
// RF-44: posición en el consolidado y si la ganó, por partida por equipos terminada donde el equipo
// participó (inscripción aceptada), anotara o no. "Sin duplicar el cálculo de puntajes": reusa
// CalculadorRankingConsolidado.
public sealed class ObtenerRendimientoEquipoQueryHandler
    : IRequestHandler<ObtenerRendimientoEquipoQuery, RendimientoEquipoResponse>
{
    private readonly IProyeccionesRepository _repo;

    public ObtenerRendimientoEquipoQueryHandler(IProyeccionesRepository repo) => _repo = repo;

    public async Task<RendimientoEquipoResponse> Handle(ObtenerRendimientoEquipoQuery request, CancellationToken cancellationToken)
    {
        var partidas = await _repo.GetPartidasTerminadasConParticipacionDeEquipoAsync(request.EquipoId, cancellationToken);
        var rendimiento = new List<RendimientoPartidaDto>(partidas.Count);
        foreach (var partida in partidas)
        {
            var marcadores = await _repo.GetMarcadoresDePartidaAsync(partida.PartidaId, cancellationToken);
            var participaciones = await _repo.GetParticipacionesDePartidaAsync(partida.PartidaId, cancellationToken);
            var entradas = CalculadorRankingConsolidado.Calcular(marcadores, participaciones);
            // El repo garantiza participación del equipo en cada partida devuelta, y la
            // participación está en el universo del calculador: la entrada existe.
            var propia = entradas.First(e => e.CompetidorId == request.EquipoId);
            rendimiento.Add(new RendimientoPartidaDto(partida.PartidaId, partida.FechaFin, propia.Posicion, propia.Posicion == 1));
        }

        return new RendimientoEquipoResponse(request.EquipoId, rendimiento);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones/
git commit -m "feat(puntuaciones): el rendimiento de equipo va por participacion, no por marcador"
```

---

### Task 7: Contratos y trazabilidad

**Files:**
- Modify: `contracts/http/puntuaciones-api.md` (líneas 53, 104, 109, 201-203)
- Modify: `docs/04-sdd/SPECS-LIST.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

**Esto no es papeleo:** la línea 104 justifica la limitación con *"no hay evento de inscripción en el broker"*, premisa falsa desde el Bloque 4B. Si no se corrige, el contrato sigue explicando un comportamiento que ya no existe con una razón que nunca fue revisada.

- [ ] **Step 1: Actualizar `contracts/http/puntuaciones-api.md`**

Localizar cada línea **por su contenido** (los números se desplazan) y sustituir:

La de `:53` ("Juego conocido sin marcadores → `200` con `entradas: []`"):

```markdown
- Juego de una partida con competidores → `200` con todos ellos, a `0` mientras nadie haya anotado
  (empatados en posición 1). `entradas: []` solo si la partida no tiene participaciones proyectadas
  ni marcadores.
```

La de `:104` ("**Participación = tener ≥1 marcador**…"):

```markdown
- **Participación = inscripción aceptada**, no haber anotado: los competidores que nunca puntuaron
  aparecen con `0` y en la última posición. Se proyecta desde `InscripcionAceptada`
  (`participaciones_proyectadas`). El universo es `participaciones ∪ marcadores`: si se perdiera
  `InscripcionAceptada` (best-effort ADR-0012), el marcador prueba que el competidor jugó.
  Las partidas anteriores al slice de 2026-07-15 no tienen participaciones proyectadas (sin
  backfill) y conservan el comportamiento previo.
```

La de `:109` (rendimiento "donde el equipo tiene ≥1 marcador"): sustituir esa condición por **"donde el equipo tiene participación proyectada (inscripción aceptada), anotara o no"**.

Las de `:201-203` (participación del historial): sustituir por:

```markdown
- **Participación:** inscripción aceptada propia (`Individual`) o **convocatoria aceptada** a un
  equipo con participación en la partida (`Equipo`), proyectadas desde `InscripcionAceptada` y
  `ConvocatoriaCreada`/`ConvocatoriaRespondida`. No se exige haber anotado ni haber autorado ninguna
  acción de juego: la limitación previa del integrante pasivo queda retirada. Canceladas excluidas
  (RB-30).
```

- [ ] **Step 2: `docs/04-sdd/SPECS-LIST.md`**

Añadir al final de la tabla:

```markdown
| Participación sin puntuar en historial y rankings (corrección) | Puntuaciones | backend | Participante / Operador | docs/superpowers/specs/2026-07-15-participacion-sin-puntuar-design.md | Implemented (7 tasks). Quien participó aparece con 0 aunque no anote, en los dos rankings y en el historial. Dos proyecciones nuevas desde eventos que ya se publicaban. Sin endpoints nuevos ni cambios de DTO. |
```

- [ ] **Step 3: `docs/04-sdd/traceability-matrix.md`**

Tabla de 7 columnas (`Feature | Requirement | Owning service | Supporting services | SDD folder | Contracts | Status`). Añadir al final, sustituyendo `NNN` por el total real del Step 4:

```markdown
| Participación sin puntuar en historial y rankings (corrección) | Quien participó aparece en el historial y en los dos rankings aunque no anotara, con 0 puntos y última posición. **Causa raíz:** el marcador solo nace al acreditar puntos (`ProyectarPuntajeTriviaCommandHandler`) y era el único universo de competidores; en Trivia la pregunta cierra al primer acierto, así que puntúa un solo competidor por pregunta y el resto quedaba invisible. **Arreglo:** proyecciones `participaciones_proyectadas` (desde `InscripcionAceptada`) y `convocatorias_proyectadas` (desde `ConvocatoriaCreada` + `ConvocatoriaRespondida`, unidas por `convocatoriaId`); `RankingCalculator` y `CalculadorRankingConsolidado` operan sobre `participaciones ∪ marcadores`; historial y rendimiento filtran por participación | Puntuaciones | Operaciones de Sesión (emisor de los eventos, **sin cambios**); Gateway, Identity, web y móvil **sin cambios** | docs/superpowers/specs/2026-07-15-participacion-sin-puntuar-design.md · docs/superpowers/plans/2026-07-15-participacion-sin-puntuar.md | contracts/http/puntuaciones-api.md | Implemented — 7 tasks, commit por task. Suite Puntuaciones verde en HEAD: **NNN** (línea base antes del slice: 201 = 151 unit + 30 integration + 20 contract). **Fuente:** pregunta del usuario, verificada en código (13 hechos con archivo y línea en el spec §2). **Hallazgo clave:** el contrato justificaba la limitación con *"no hay evento de inscripción en el broker"* — falso desde el Bloque 4B, que añadió `InscripcionAceptada`. La premisa desapareció y nadie revisó la consecuencia; este slice cobra trabajo ya hecho. **Decisión:** participar = inscripción aceptada (no "haber actuado"); para el miembro, convocatoria aceptada. **Decisión:** proyecciones propias en vez de consultar `EventosHistorial` — hay precedente (`HistorialRepository.cs:52-58`), pero el caso del miembro exigiría un join sobre JSON (`convocatoriaId` vive en `DetalleJson`) y el historial está para narrar, no para rankear. **Decisión:** sin backfill — las partidas anteriores conservan el comportamiento previo. **Retira:** la limitación documentada *"el integrante que jamás autoró una acción de juego no ve la partida"*. **Verificación de no-regresión:** los contract tests siguen verdes **sin modificar** — cambia quién sale en las listas, no la forma de los DTOs. |
```

- [ ] **Step 4: Verificar la suite y anotar el conteo**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS. Anotar el total. **Comprobar además que no se tocó ningún archivo de `ContractTests`**:

```bash
git diff --name-only HEAD~6 -- services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/
```

Expected: **salida vacía**. Si aparece algún archivo, la forma de un DTO cambió y eso contradice el spec: parar y avisar.

Sustituir el `NNN` del Step 3 por el total real.

- [ ] **Step 5: Commit**

```bash
git add contracts/http/puntuaciones-api.md docs/04-sdd/SPECS-LIST.md docs/04-sdd/traceability-matrix.md
git commit -m "docs: contrato de participacion sin puntuar + trazabilidad"
```

---

## Notas para quien ejecute

- **`dotnet test` sobre el `.sln` de Puntuaciones cubre los tres proyectos** (unit, integration, contract). La línea base antes de este slice es **201** (151 + 30 + 20).
- **No tocar los contract tests.** Son la prueba de que la forma de los DTOs no cambió. Si uno falla, el fallo es real.
- **Los dos `.ToList()` antes de `Concat`/`AddRange`** (Tasks 3 y 4) no son estilo: sin ellos el `Where` perezoso lee la lista que se está modificando.
- **La guarda de salida temprana del consolidado** debe exigir que **ambas** colecciones estén vacías. Dejarla como está haría que una partida con participaciones y sin marcadores siguiera devolviendo lista vacía — el bug que este slice arregla.
- **Verificar las firmas reales antes de escribir**, aunque este plan las cite: los números de línea se desplazan con cada task, y los nombres de los fakes (`FakeProyeccionesRepository.Partidas`, `FakePuntuacionesUnitOfWork`) hay que confirmarlos leyendo `tests/.../Application/Fakes/`.
- **Tests que codifican el comportamiento viejo:** los hay casi seguro (p. ej. "sin marcadores → vacío", o el integrante pasivo que no ve la partida). No borrarlos a ciegas: leerlos, comprobar que afirman justo lo que el spec deroga, sustituirlos y anotarlo en el commit.
