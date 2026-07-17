# Bloque 4A — Slice equipos-admin / ciclo de vida — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cerrar end-to-end HU-06, HU-09, HU-48 y BR-E06/E10/E11: eliminación de equipo por líder y por admin, CRUD administrativo de equipos, historial de nombres por participante, guard de borrado contra partidas activas, con eventos + correo de notificación y UI en web (admin) y mobile (líder/participante).

**Architecture:** Todo el ciclo de vida vive en **Identity** (dominio `Equipo` + tablas nuevas `historial_nombre_equipo` y `participaciones_activas_equipo`). El guard BR-E10 se resuelve por **proyección local alimentada por eventos RabbitMQ** que **Operaciones de Sesión** empieza a emitir (`InscripcionEquipoCreada`/`Cancelada`); Identity estrena un **consumidor** que mantiene la proyección. Notificar = evento de dominio + correo SMTP best-effort. Clientes cablean directo a Identity (el gateway sigue siendo Bloque 2).

**Tech Stack:** .NET 8, Clean Architecture + MediatR/CQRS, EF Core + PostgreSQL, RabbitMQ (RabbitMQ.Client), xUnit; React 18 + Vite + TypeScript + vitest (web); React Native + Expo, `node --test` (mobile).

## Global Constraints

- **Identity NO usa migraciones EF**: el esquema se crea con `EnsureCreatedAsync()` (`services/identity-service/src/Umbral.IdentityService.Api/Program.cs:116`). Tablas nuevas → nuevos `DbSet` + config en `IdentityDbContext.OnModelCreating`; **NO** crear carpeta `Migrations`. El backfill de historial es un paso de arranque idempotente, no una migración.
- **Estructura estándar por servicio** (CLAUDE.md): `Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `DTOs/`, `Handlers/Commands/`, `Handlers/Queries/`, `Exceptions/`. Interfaces de repositorio en `Domain/Abstractions/Persistence/`. Controllers heredan de `ControllerBase`, despachan por `_mediator.Send`/`ISender`, sin lógica de negocio, **con unit tests obligatorios**.
- **Eventos best-effort (ADR-0012)**: un fallo de broker/SMTP se loguea y NUNCA revierte la operación de dominio ni llega al caller.
- **Eventos versión 1**, envelope camelCase `{ eventId, eventType, version, occurredAt, payload }`; routing key `identity.<kebab>.v1` / `operaciones-sesion.<kebab>.v1`. Consumidores deduplican por `eventId` / son idempotentes por clave.
- **Frontend redesign**: reconstrucción visual sin cambiar `label`/`id`/`data-testid`/roles ARIA que usen tests. Reusar el design system implementado (`docs/02-project-context/design/design-system.md`).
- **El admin NO compone membresía** (BR-E05 intacta): crear = nombre + líder válido (único integrante inicial); editar = renombrar + reasignar liderazgo entre integrantes existentes.
- **Soft delete**: eliminar equipo = `Estado = Eliminado`; las queries de membresía ya filtran `Estado == Activo`, liberando integrantes. Nunca se borran filas de `historial_nombre_equipo`.
- Solución Identity: `services/identity-service/Umbral.IdentityService.sln`. Solución Operaciones: `services/operaciones-sesion/Umbral.OperacionesSesion.sln`.
- Ejecutar tests backend: `dotnet test "services/identity-service/Umbral.IdentityService.sln"`. Web: `cd frontend && npm test`. Mobile: `cd mobile && npm test`.

---

## Fase A — Dominio Equipo: ciclo de vida (HU-06/HU-09 núcleo, BR-E06)

### Task A1: Excepciones de dominio para el ciclo de vida

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Exceptions/EquipoEliminadoInmutableException.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Exceptions/NuevoLiderNoPerteneceAlEquipoException.cs` (ya existe — reusar, no recrear)
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/EquipoCicloVidaDomainTests.cs`

**Interfaces:**
- Produces: `EquipoEliminadoInmutableException(Guid equipoId)` — lanzada cuando se intenta desactivar/reactivar/renombrar/reasignar/eliminar un equipo ya `Eliminado`.

- [ ] **Step 1: Escribir la excepción**

```csharp
namespace Umbral.IdentityService.Domain.Exceptions;

public sealed class EquipoEliminadoInmutableException : Exception
{
    public Guid EquipoId { get; }

    public EquipoEliminadoInmutableException(Guid equipoId)
        : base($"El equipo {equipoId} está eliminado y no admite cambios.")
        => EquipoId = equipoId;
}
```

- [ ] **Step 2: Compilar el proyecto Domain**

Run: `dotnet build "services/identity-service/src/Umbral.IdentityService.Domain/Umbral.IdentityService.Domain.csproj"`
Expected: BUILD SUCCEEDED.

- [ ] **Step 3: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Domain/Exceptions/EquipoEliminadoInmutableException.cs
git commit -m "feat(identity): excepción EquipoEliminadoInmutable para ciclo de vida de equipo"
```

### Task A2: Métodos de ciclo de vida en `Equipo` (líder/admin, estado, renombrar, reasignar, crear)

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Domain/Entities/Equipo.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/EquipoCicloVidaDomainTests.cs`

**Interfaces:**
- Consumes: `EstadoEquipo { Activo=1, Desactivado=2, Eliminado=3 }`, `ParticipanteEquipo.CrearCreador/CrearIntegrante/MarcarComoLider/QuitarLiderazgo`, excepciones existentes (`EquipoNoActivoException`, `NuevoLiderNoPerteneceAlEquipoException`, `NuevoLiderDebeSerDiferenteException`, `ActorNoEsLiderEquipoException`, `ParticipanteNoPerteneceAlEquipoException`) y `EquipoEliminadoInmutableException` (Task A1).
- Produces (métodos públicos nuevos en `Equipo`):
  - `static Equipo CrearPorAdmin(string nombreEquipo, Guid liderUserId)` → equipo Activo con el líder como único integrante.
  - `IReadOnlyList<Guid> EliminarPorLider(Guid actorUserId)` → valida que el actor sea el líder; NO exige equipo vacío; `Estado = Eliminado`; devuelve los `UsuarioId` de los integrantes que había (para notificar).
  - `IReadOnlyList<Guid> EliminarPorAdmin()` → `Estado = Eliminado` sin validar actor; devuelve integrantes que había.
  - `void Desactivar()` / `void Reactivar()` → Activo↔Desactivado; rechazan si `Eliminado`.
  - `void Renombrar(string nuevoNombre)` → mismas reglas de nombre que la creación; rechaza si `Eliminado`.
  - `(Guid LiderAnteriorUserId, Guid NuevoLiderUserId) ReasignarLiderazgoPorAdmin(Guid nuevoLiderUserId)` → sin exigir que el actor sea líder; el nuevo líder debe ser integrante y distinto del actual.

- [ ] **Step 1: Escribir los tests que fallan**

```csharp
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class EquipoCicloVidaDomainTests
{
    private static Equipo EquipoConLiderYMiembro(out Guid lider, out Guid miembro)
    {
        lider = Guid.NewGuid();
        miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Los Andes", lider);
        equipo.AgregarParticipante(miembro);
        return equipo;
    }

    [Fact]
    public void CrearPorAdmin_asigna_lider_como_unico_integrante()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorAdmin("Equipo Admin", lider);

        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
        Assert.Single(equipo.Participantes);
        Assert.True(equipo.Participantes[0].EsLider);
        Assert.Equal(lider, equipo.Participantes[0].UsuarioId);
    }

    [Fact]
    public void EliminarPorLider_con_integrantes_elimina_y_devuelve_todos_los_miembros()
    {
        var equipo = EquipoConLiderYMiembro(out var lider, out var miembro);

        var afectados = equipo.EliminarPorLider(lider);

        Assert.Equal(EstadoEquipo.Eliminado, equipo.Estado);
        Assert.Contains(lider, afectados);
        Assert.Contains(miembro, afectados);
        Assert.Equal(2, afectados.Count);
    }

    [Fact]
    public void EliminarPorLider_cuando_actor_no_es_lider_lanza()
    {
        var equipo = EquipoConLiderYMiembro(out _, out var miembro);
        Assert.Throws<ActorNoEsLiderEquipoException>(() => equipo.EliminarPorLider(miembro));
    }

    [Fact]
    public void EliminarPorAdmin_elimina_sin_validar_actor_y_devuelve_miembros()
    {
        var equipo = EquipoConLiderYMiembro(out var lider, out var miembro);

        var afectados = equipo.EliminarPorAdmin();

        Assert.Equal(EstadoEquipo.Eliminado, equipo.Estado);
        Assert.Equal(new[] { lider, miembro }.OrderBy(x => x), afectados.OrderBy(x => x));
    }

    [Fact]
    public void Desactivar_y_Reactivar_alternan_estado()
    {
        var equipo = EquipoConLiderYMiembro(out _, out _);
        equipo.Desactivar();
        Assert.Equal(EstadoEquipo.Desactivado, equipo.Estado);
        equipo.Reactivar();
        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
    }

    [Fact]
    public void Operaciones_sobre_equipo_eliminado_lanzan()
    {
        var equipo = EquipoConLiderYMiembro(out var lider, out var miembro);
        equipo.EliminarPorAdmin();

        Assert.Throws<EquipoEliminadoInmutableException>(() => equipo.Desactivar());
        Assert.Throws<EquipoEliminadoInmutableException>(() => equipo.Renombrar("X"));
        Assert.Throws<EquipoEliminadoInmutableException>(() => equipo.ReasignarLiderazgoPorAdmin(miembro));
    }

    [Fact]
    public void Renombrar_cambia_el_nombre()
    {
        var equipo = EquipoConLiderYMiembro(out _, out _);
        equipo.Renombrar("  Nuevo Nombre  ");
        Assert.Equal("Nuevo Nombre", equipo.NombreEquipo);
    }

    [Fact]
    public void ReasignarLiderazgoPorAdmin_mueve_el_liderazgo_a_un_integrante()
    {
        var equipo = EquipoConLiderYMiembro(out var lider, out var miembro);

        var (anterior, nuevo) = equipo.ReasignarLiderazgoPorAdmin(miembro);

        Assert.Equal(lider, anterior);
        Assert.Equal(miembro, nuevo);
        Assert.True(equipo.Participantes.Single(p => p.UsuarioId == miembro).EsLider);
        Assert.False(equipo.Participantes.Single(p => p.UsuarioId == lider).EsLider);
    }

    [Fact]
    public void ReasignarLiderazgoPorAdmin_a_no_integrante_lanza()
    {
        var equipo = EquipoConLiderYMiembro(out _, out _);
        Assert.Throws<NuevoLiderNoPerteneceAlEquipoException>(
            () => equipo.ReasignarLiderazgoPorAdmin(Guid.NewGuid()));
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que fallan (no compila / métodos inexistentes)**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~EquipoCicloVidaDomainTests`
Expected: FAIL de compilación por métodos inexistentes.

- [ ] **Step 3: Implementar los métodos en `Equipo`**

Añadir dentro de la clase `Equipo` (mantener `EnsureCardinalityInvariant`, `MaximoIntegrantes`, y helpers existentes):

```csharp
    public static Equipo CrearPorAdmin(string nombreEquipo, Guid liderUserId)
    {
        return new Equipo(nombreEquipo, liderUserId);
    }

    public IReadOnlyList<Guid> EliminarPorLider(Guid actorUserId)
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);

        var lider = Participantes.SingleOrDefault(p => p.EsLider);
        if (lider is null || lider.UsuarioId != actorUserId)
            throw new ActorNoEsLiderEquipoException(actorUserId);

        var afectados = Participantes.Select(p => p.UsuarioId).ToList();
        Estado = EstadoEquipo.Eliminado;
        return afectados;
    }

    public IReadOnlyList<Guid> EliminarPorAdmin()
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);

        var afectados = Participantes.Select(p => p.UsuarioId).ToList();
        Estado = EstadoEquipo.Eliminado;
        return afectados;
    }

    public void Desactivar()
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);
        Estado = EstadoEquipo.Desactivado;
    }

    public void Reactivar()
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);
        Estado = EstadoEquipo.Activo;
    }

    public void Renombrar(string nuevoNombre)
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);
        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new ArgumentException("NombreEquipo requerido", nameof(nuevoNombre));
        NombreEquipo = nuevoNombre.Trim();
    }

    public (Guid LiderAnteriorUserId, Guid NuevoLiderUserId) ReasignarLiderazgoPorAdmin(Guid nuevoLiderUserId)
    {
        if (Estado == EstadoEquipo.Eliminado)
            throw new EquipoEliminadoInmutableException(EquipoId);
        if (nuevoLiderUserId == Guid.Empty)
            throw new ArgumentException("NuevoLiderUserId requerido", nameof(nuevoLiderUserId));

        var liderActual = Participantes.SingleOrDefault(p => p.EsLider)
            ?? throw new InvalidOperationException("El equipo debe tener exactamente un lider.");

        if (liderActual.UsuarioId == nuevoLiderUserId)
            throw new NuevoLiderDebeSerDiferenteException(nuevoLiderUserId);

        var nuevoLider = Participantes.SingleOrDefault(p => p.UsuarioId == nuevoLiderUserId)
            ?? throw new NuevoLiderNoPerteneceAlEquipoException(nuevoLiderUserId);

        liderActual.QuitarLiderazgo();
        nuevoLider.MarcarComoLider();
        EnsureCardinalityInvariant();

        return (liderActual.UsuarioId, nuevoLiderUserId);
    }
```

Añadir el `using` si falta: `using Umbral.IdentityService.Domain.Exceptions;` ya está en el archivo.

- [ ] **Step 4: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~EquipoCicloVidaDomainTests`
Expected: PASS (10 tests).

- [ ] **Step 5: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Domain/Entities/Equipo.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/EquipoCicloVidaDomainTests.cs
git commit -m "feat(identity): métodos de ciclo de vida de Equipo (eliminar líder/admin, estado, renombrar, reasignar)"
```

---

## Fase B — Historial de nombres de equipo (HU-48, BR-E11)

### Task B1: Entidad `HistorialNombreEquipo` + interfaz de repositorio

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Entities/HistorialNombreEquipo.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IHistorialNombreEquipoRepository.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/HistorialNombreEquipoDomainTests.cs`

**Interfaces:**
- Produces:
  - `HistorialNombreEquipo` con props `{ Guid Id, Guid UsuarioId, Guid EquipoId, string NombreEquipo, DateTime FechaRegistroUtc }` y factoría `static HistorialNombreEquipo Registrar(Guid usuarioId, Guid equipoId, string nombreEquipo, DateTime fechaUtc)`.
  - `IHistorialNombreEquipoRepository`:
    - `Task AddRangeAsync(IEnumerable<HistorialNombreEquipo> registros, CancellationToken ct)`
    - `Task<IReadOnlyList<HistorialNombreEquipo>> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct)` (orden ascendente por `FechaRegistroUtc`)
    - `Task<bool> AnyAsync(CancellationToken ct)` (para el backfill idempotente)

- [ ] **Step 1: Escribir el test que falla**

```csharp
using Umbral.IdentityService.Domain.Entities;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class HistorialNombreEquipoDomainTests
{
    [Fact]
    public void Registrar_crea_fila_con_datos_y_fecha()
    {
        var usuario = Guid.NewGuid();
        var equipo = Guid.NewGuid();
        var fecha = new DateTime(2026, 7, 8, 12, 0, 0, DateTimeKind.Utc);

        var h = HistorialNombreEquipo.Registrar(usuario, equipo, "  Titanes  ", fecha);

        Assert.NotEqual(Guid.Empty, h.Id);
        Assert.Equal(usuario, h.UsuarioId);
        Assert.Equal(equipo, h.EquipoId);
        Assert.Equal("Titanes", h.NombreEquipo);
        Assert.Equal(fecha, h.FechaRegistroUtc);
    }

    [Fact]
    public void Registrar_con_nombre_vacio_lanza()
    {
        Assert.Throws<ArgumentException>(
            () => HistorialNombreEquipo.Registrar(Guid.NewGuid(), Guid.NewGuid(), "  ", DateTime.UtcNow));
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~HistorialNombreEquipoDomainTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar entidad e interfaz**

`HistorialNombreEquipo.cs`:

```csharp
namespace Umbral.IdentityService.Domain.Entities;

public sealed class HistorialNombreEquipo
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid EquipoId { get; private set; }
    public string NombreEquipo { get; private set; }
    public DateTime FechaRegistroUtc { get; private set; }

    private HistorialNombreEquipo()
    {
        NombreEquipo = string.Empty;
    }

    private HistorialNombreEquipo(Guid usuarioId, Guid equipoId, string nombreEquipo, DateTime fechaUtc)
    {
        if (usuarioId == Guid.Empty) throw new ArgumentException("UsuarioId requerido", nameof(usuarioId));
        if (equipoId == Guid.Empty) throw new ArgumentException("EquipoId requerido", nameof(equipoId));
        if (string.IsNullOrWhiteSpace(nombreEquipo)) throw new ArgumentException("NombreEquipo requerido", nameof(nombreEquipo));

        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        EquipoId = equipoId;
        NombreEquipo = nombreEquipo.Trim();
        FechaRegistroUtc = fechaUtc;
    }

    public static HistorialNombreEquipo Registrar(Guid usuarioId, Guid equipoId, string nombreEquipo, DateTime fechaUtc)
        => new(usuarioId, equipoId, nombreEquipo, fechaUtc);
}
```

`IHistorialNombreEquipoRepository.cs`:

```csharp
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Domain.Abstractions.Persistence;

public interface IHistorialNombreEquipoRepository
{
    Task AddRangeAsync(IEnumerable<HistorialNombreEquipo> registros, CancellationToken cancellationToken);
    Task<IReadOnlyList<HistorialNombreEquipo>> GetByUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<bool> AnyAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~HistorialNombreEquipoDomainTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Domain/Entities/HistorialNombreEquipo.cs services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IHistorialNombreEquipoRepository.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/HistorialNombreEquipoDomainTests.cs
git commit -m "feat(identity): entidad HistorialNombreEquipo + repositorio (HU-48/BR-E11)"
```

### Task B2: Persistencia del historial (DbContext, repo, mapeo) + registro DI

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/IdentityDbContext.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/HistorialNombreEquipoRepository.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/DependencyInjection.cs:57` (junto a los otros `AddScoped` de repos)
- Test: `services/identity-service/tests/Umbral.IdentityService.IntegrationTests/Teams/HistorialNombreEquipoPersistenceTests.cs`

**Interfaces:**
- Consumes: `HistorialNombreEquipo`, `IHistorialNombreEquipoRepository` (Task B1), `IdentityDbContext`.
- Produces: `HistorialNombreEquipoRepository : IHistorialNombreEquipoRepository`; tabla `historial_nombre_equipo`.

- [ ] **Step 1: Escribir el test de persistencia que falla**

Usar el patrón de `EquipoPersistenceTests` (misma carpeta) con `IdentityDbContext` sobre InMemory o Npgsql de test; si el proyecto de integración ya tiene un `IdentityApiFactory`/contexto compartido, reusarlo. Test mínimo:

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Infrastructure.Persistence;
using Xunit;

namespace Umbral.IdentityService.IntegrationTests.Teams;

public sealed class HistorialNombreEquipoPersistenceTests
{
    private static IdentityDbContext NewContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"hist-{Guid.NewGuid()}").Options);

    [Fact]
    public async Task AddRange_y_GetByUsuario_devuelve_orden_ascendente_por_fecha()
    {
        var usuario = Guid.NewGuid();
        var equipo = Guid.NewGuid();
        await using var ctx = NewContext();
        var repo = new HistorialNombreEquipoRepository(ctx);

        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await repo.AddRangeAsync(new[]
        {
            HistorialNombreEquipo.Registrar(usuario, equipo, "Segundo", t0.AddDays(1)),
            HistorialNombreEquipo.Registrar(usuario, equipo, "Primero", t0),
        }, CancellationToken.None);

        var result = await repo.GetByUsuarioAsync(usuario, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Primero", result[0].NombreEquipo);
        Assert.Equal("Segundo", result[1].NombreEquipo);
    }

    [Fact]
    public async Task GetByUsuario_sin_registros_devuelve_lista_vacia()
    {
        await using var ctx = NewContext();
        var repo = new HistorialNombreEquipoRepository(ctx);
        var result = await repo.GetByUsuarioAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Empty(result);
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~HistorialNombreEquipoPersistenceTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Añadir el DbSet + mapeo en `IdentityDbContext`**

En `IdentityDbContext.cs`, añadir el `DbSet` tras `PermisosRol`:

```csharp
    public DbSet<HistorialNombreEquipo> HistorialNombresEquipo => Set<HistorialNombreEquipo>();
```

y dentro de `OnModelCreating`, un bloque nuevo:

```csharp
        modelBuilder.Entity<HistorialNombreEquipo>(entity =>
        {
            entity.ToTable("historial_nombre_equipo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UsuarioId).HasColumnName("usuarioid").IsRequired();
            entity.Property(x => x.EquipoId).HasColumnName("equipoid").IsRequired();
            entity.Property(x => x.NombreEquipo).HasColumnName("nombreequipo").HasMaxLength(120).IsRequired();
            entity.Property(x => x.FechaRegistroUtc).HasColumnName("fecharegistroutc").IsRequired();
            entity.HasIndex(x => x.UsuarioId).HasDatabaseName("ix_historial_nombre_equipo_usuarioid");
        });
```

- [ ] **Step 4: Implementar el repositorio**

`HistorialNombreEquipoRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class HistorialNombreEquipoRepository : IHistorialNombreEquipoRepository
{
    private readonly IdentityDbContext _db;

    public HistorialNombreEquipoRepository(IdentityDbContext db) => _db = db;

    public async Task AddRangeAsync(IEnumerable<HistorialNombreEquipo> registros, CancellationToken cancellationToken)
    {
        await _db.HistorialNombresEquipo.AddRangeAsync(registros, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistorialNombreEquipo>> GetByUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
        => await _db.HistorialNombresEquipo
            .Where(x => x.UsuarioId == usuarioId)
            .OrderBy(x => x.FechaRegistroUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken)
        => _db.HistorialNombresEquipo.AnyAsync(cancellationToken);
}
```

- [ ] **Step 5: Registrar en DI**

En `DependencyInjection.cs`, junto a los `AddScoped` de repos (tras la línea `services.AddScoped<IInvitacionEquipoRepository, InvitacionEquipoRepository>();`):

```csharp
        services.AddScoped<IHistorialNombreEquipoRepository, HistorialNombreEquipoRepository>();
```

- [ ] **Step 6: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~HistorialNombreEquipoPersistenceTests`
Expected: PASS (2 tests).

- [ ] **Step 7: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/IdentityDbContext.cs services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/HistorialNombreEquipoRepository.cs services/identity-service/src/Umbral.IdentityService.Infrastructure/DependencyInjection.cs services/identity-service/tests/Umbral.IdentityService.IntegrationTests/Teams/HistorialNombreEquipoPersistenceTests.cs
git commit -m "feat(identity): persistencia de historial_nombre_equipo + repo + DI"
```

### Task B3: Escribir historial en altas de membresía (creación, invitación aceptada)

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Commands/CrearEquipoCommandHandler.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Commands/AceptarInvitacionEquipoCommandHandler.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/HistorialNombreEnAltasTests.cs`

**Interfaces:**
- Consumes: `IHistorialNombreEquipoRepository.AddRangeAsync`, `HistorialNombreEquipo.Registrar`, `TimeProvider` (ya registrado como singleton).
- Produces: al crear equipo o aceptar invitación, se inserta 1 fila de historial para el nuevo integrante con el nombre actual del equipo.

- [ ] **Step 1: Escribir tests que fallan (handlers con repos falsos)**

Usar el estilo de `CrearEquipoHandlerTests`/`AceptarInvitacionEquipoHandlerTests` existentes (fakes in-memory). Test nuevo:

```csharp
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Handlers.Commands;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class HistorialNombreEnAltasTests
{
    // Fake mínimo del repo de historial que captura lo insertado.
    private sealed class FakeHistorialRepo : IHistorialNombreEquipoRepository
    {
        public List<HistorialNombreEquipo> Registros { get; } = new();
        public Task AddRangeAsync(IEnumerable<HistorialNombreEquipo> r, CancellationToken ct)
        { Registros.AddRange(r); return Task.CompletedTask; }
        public Task<IReadOnlyList<HistorialNombreEquipo>> GetByUsuarioAsync(Guid u, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<HistorialNombreEquipo>>(Registros.Where(x => x.UsuarioId == u).ToList());
        public Task<bool> AnyAsync(CancellationToken ct) => Task.FromResult(Registros.Count > 0);
    }

    [Fact]
    public async Task CrearEquipo_registra_historial_del_lider()
    {
        // Arrange: reutilizar los fakes de CrearEquipoHandlerTests para IEquipoRepository e IIdentityEventsPublisher,
        // inyectar FakeHistorialRepo y TimeProvider.System, ejecutar CrearEquipoCommand.
        // Assert: hist.Registros contiene 1 fila con UsuarioId = actor y NombreEquipo = "Equipo A".
        Assert.True(true); // placeholder de estructura — ver nota de implementación abajo
    }
}
```

> Nota de implementación: copiar los fakes de `IEquipoRepository` e `IIdentityEventsPublisher` de `CrearEquipoHandlerTests.cs` (misma carpeta) para no reinventarlos; el assert real verifica `hist.Registros.Single().NombreEquipo == "Equipo A"` y `UsuarioId == actor`. Escribir el equivalente para `AceptarInvitacionEquipo` (1 fila para el invitado con el nombre del equipo).

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~HistorialNombreEnAltasTests`
Expected: FAIL (constructor de handler no acepta el repo de historial).

- [ ] **Step 3: Inyectar el repo y escribir la fila en `CrearEquipoCommandHandler`**

Añadir `IHistorialNombreEquipoRepository _historial` y `TimeProvider _time` al constructor; tras `AddAsync(equipo,...)` y antes de publicar el evento:

```csharp
        await _historial.AddRangeAsync(new[]
        {
            HistorialNombreEquipo.Registrar(
                request.ActorUserId, equipo.EquipoId, equipo.NombreEquipo, _time.GetUtcNow().UtcDateTime)
        }, cancellationToken);
```

- [ ] **Step 4: Ídem en `AceptarInvitacionEquipoCommandHandler`**

Tras `AgregarParticipante` + `UpdateAsync`, insertar 1 fila para `invitacion.InvitadoUserId` con `equipo.NombreEquipo`.

- [ ] **Step 5: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~HistorialNombreEnAltasTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/Handlers/Commands/CrearEquipoCommandHandler.cs services/identity-service/src/Umbral.IdentityService.Application/Handlers/Commands/AceptarInvitacionEquipoCommandHandler.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/HistorialNombreEnAltasTests.cs
git commit -m "feat(identity): registrar historial de nombre al crear equipo y aceptar invitación"
```

### Task B4: Query + endpoint `GET /identity/teams/mine/history` (HU-48)

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Queries/GetHistorialNombresEquipoQuery.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/DTOs/HistorialNombreEquipoResponse.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/GetHistorialNombresEquipoQueryHandler.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsController.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/TeamsControllerTests.cs` (añadir casos)

**Interfaces:**
- Consumes: `IHistorialNombreEquipoRepository.GetByUsuarioAsync`, `AuthenticatedUserClaims.TryGetUserId`.
- Produces:
  - `GetHistorialNombresEquipoQuery(Guid ActorUserId) : IRequest<HistorialNombresEquipoResponse>`
  - `HistorialNombresEquipoResponse(IReadOnlyList<HistorialNombreEquipoItem> Historial)`, `HistorialNombreEquipoItem(string NombreEquipo, Guid EquipoId, DateTime FechaRegistro)`
  - endpoint `GET /identity/teams/mine/history` → 200 (lista, posiblemente vacía).

- [ ] **Step 1: Escribir el test de controller que falla**

En `TeamsControllerTests.cs`, añadir:

```csharp
    [Fact]
    public async Task Historial_Dispatches_Query_And_Returns_Ok()
    {
        var actor = Guid.NewGuid();
        var response = new HistorialNombresEquipoResponse(
            new[] { new HistorialNombreEquipoItem("Titanes", Guid.NewGuid(), DateTime.UtcNow) });
        var sender = new FakeSender { NextResponse = response };
        var controller = BuildController(sender, actor);

        var result = await controller.MiHistorial(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        var query = Assert.IsType<GetHistorialNombresEquipoQuery>(sender.LastRequest);
        Assert.Equal(actor, query.ActorUserId);
    }

    [Fact]
    public async Task Historial_Returns_Unauthorized_When_No_Sub()
    {
        var controller = BuildController(new FakeSender(), sub: null);
        var result = await controller.MiHistorial(CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }
```

Añadir el `using Umbral.IdentityService.Application.Queries;` al archivo de test si falta.

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~TeamsControllerTests.Historial`
Expected: FAIL de compilación.

- [ ] **Step 3: Crear query, DTO y handler**

Query:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record GetHistorialNombresEquipoQuery(Guid ActorUserId)
    : IRequest<HistorialNombresEquipoResponse>;
```

DTO:

```csharp
namespace Umbral.IdentityService.Application.DTOs;

public sealed record HistorialNombresEquipoResponse(
    IReadOnlyList<HistorialNombreEquipoItem> Historial);

public sealed record HistorialNombreEquipoItem(
    string NombreEquipo, Guid EquipoId, DateTime FechaRegistro);
```

Handler:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class GetHistorialNombresEquipoQueryHandler
    : IRequestHandler<GetHistorialNombresEquipoQuery, HistorialNombresEquipoResponse>
{
    private readonly IHistorialNombreEquipoRepository _historial;

    public GetHistorialNombresEquipoQueryHandler(IHistorialNombreEquipoRepository historial)
        => _historial = historial;

    public async Task<HistorialNombresEquipoResponse> Handle(
        GetHistorialNombresEquipoQuery request, CancellationToken cancellationToken)
    {
        var registros = await _historial.GetByUsuarioAsync(request.ActorUserId, cancellationToken);
        var items = registros
            .Select(r => new HistorialNombreEquipoItem(r.NombreEquipo, r.EquipoId, r.FechaRegistroUtc))
            .ToList();
        return new HistorialNombresEquipoResponse(items);
    }
}
```

- [ ] **Step 4: Añadir la acción al controller**

En `TeamsController.cs`, tras `MiEquipo`:

```csharp
    [HttpGet("mine/history")]
    public async Task<IActionResult> MiHistorial(CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var response = await _sender.Send(new GetHistorialNombresEquipoQuery(actorUserId), cancellationToken);
        return Ok(response);
    }
```

- [ ] **Step 5: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~TeamsControllerTests`
Expected: PASS (incluye los nuevos).

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/Queries/GetHistorialNombresEquipoQuery.cs services/identity-service/src/Umbral.IdentityService.Application/DTOs/HistorialNombreEquipoResponse.cs services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/GetHistorialNombresEquipoQueryHandler.cs services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsController.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/TeamsControllerTests.cs
git commit -m "feat(identity): endpoint GET /identity/teams/mine/history (HU-48)"
```

### Task B5: Backfill idempotente de historial al arranque

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/HistorialBackfill.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs` (junto a `EnsureCreatedAsync`, ~línea 116)
- Test: `services/identity-service/tests/Umbral.IdentityService.IntegrationTests/Teams/HistorialBackfillTests.cs`

**Interfaces:**
- Consumes: `IdentityDbContext.Equipos` (con `Participantes`), `IdentityDbContext.HistorialNombresEquipo`, `TimeProvider`.
- Produces: `static Task HistorialBackfill.EjecutarAsync(IdentityDbContext db, TimeProvider time, CancellationToken ct)` — si la tabla de historial está vacía, inserta una fila por (integrante de equipo `Activo`) con el nombre actual y `FechaRegistroUtc = time.GetUtcNow()`. No hace nada si ya hay filas (idempotente).

- [ ] **Step 1: Escribir el test que falla**

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Infrastructure.Persistence;
using Xunit;

namespace Umbral.IdentityService.IntegrationTests.Teams;

public sealed class HistorialBackfillTests
{
    private static IdentityDbContext NewContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"backfill-{Guid.NewGuid()}").Options);

    [Fact]
    public async Task Backfill_inserta_una_fila_por_integrante_de_equipo_activo()
    {
        await using var ctx = NewContext();
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Cascada", lider);
        equipo.AgregarParticipante(Guid.NewGuid());
        ctx.Equipos.Add(equipo);
        await ctx.SaveChangesAsync();

        await HistorialBackfill.EjecutarAsync(ctx, TimeProvider.System, CancellationToken.None);

        Assert.Equal(2, await ctx.HistorialNombresEquipo.CountAsync());
    }

    [Fact]
    public async Task Backfill_es_idempotente()
    {
        await using var ctx = NewContext();
        var equipo = Equipo.CrearPorParticipante("Cascada", Guid.NewGuid());
        ctx.Equipos.Add(equipo);
        await ctx.SaveChangesAsync();

        await HistorialBackfill.EjecutarAsync(ctx, TimeProvider.System, CancellationToken.None);
        await HistorialBackfill.EjecutarAsync(ctx, TimeProvider.System, CancellationToken.None);

        Assert.Equal(1, await ctx.HistorialNombresEquipo.CountAsync());
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~HistorialBackfillTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar el backfill**

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public static class HistorialBackfill
{
    public static async Task EjecutarAsync(IdentityDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        if (await db.HistorialNombresEquipo.AnyAsync(cancellationToken))
            return;

        var equipos = await db.Equipos
            .Include(e => e.Participantes)
            .Where(e => e.Estado == EstadoEquipo.Activo)
            .ToListAsync(cancellationToken);

        var ahora = time.GetUtcNow().UtcDateTime;
        var filas = equipos.SelectMany(e => e.Participantes
            .Select(p => HistorialNombreEquipo.Registrar(p.UsuarioId, e.EquipoId, e.NombreEquipo, ahora)));

        db.HistorialNombresEquipo.AddRange(filas);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Invocar en `Program.cs`**

Junto al bloque de `EnsureCreatedAsync` (~línea 116), tras crear el esquema:

```csharp
    await HistorialBackfill.EjecutarAsync(dbContext, app.Services.GetRequiredService<TimeProvider>(), CancellationToken.None);
```

Añadir `using Umbral.IdentityService.Infrastructure.Persistence;` si falta.

- [ ] **Step 5: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~HistorialBackfillTests`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/HistorialBackfill.cs services/identity-service/src/Umbral.IdentityService.Api/Program.cs services/identity-service/tests/Umbral.IdentityService.IntegrationTests/Teams/HistorialBackfillTests.cs
git commit -m "feat(identity): backfill idempotente de historial de nombres al arranque"
```

---

## Fase C — Eventos de Identity: eliminación y liderazgo

### Task C1: Definir eventos de integración + routing

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Application/Interfaces/IIdentityEventsPublisher.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Events/RabbitMqIdentityEventsPublisher.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Events/CompositeIdentityEventsPublisher.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Events/NoOpIdentityEventsPublisher.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Messaging/IdentityEventRouting.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Infrastructure/Messaging/IdentityEventRoutingTests.cs` (crear si no existe; si existe uno de routing, extenderlo)

**Interfaces:**
- Produces (nuevos records + métodos en `IIdentityEventsPublisher`):
  - `EquipoEliminadoIntegrationEvent(Guid EquipoId, string NombreEquipo, string Origen, IReadOnlyList<Guid> Miembros, DateTime OccurredOnUtc)` — `Origen` ∈ `"Lider"|"Admin"`.
  - `LiderazgoEquipoModificadoIntegrationEvent(Guid EquipoId, Guid LiderAnteriorUserId, Guid NuevoLiderUserId, string Origen, DateTime OccurredOnUtc)`.
  - `EquipoDesactivadoIntegrationEvent(Guid EquipoId, DateTime OccurredOnUtc)`, `EquipoReactivadoIntegrationEvent(Guid EquipoId, DateTime OccurredOnUtc)`.
  - Métodos `PublishEquipoEliminadoAsync`, `PublishLiderazgoEquipoModificadoAsync`, `PublishEquipoDesactivadoAsync`, `PublishEquipoReactivadoAsync`.
  - routing keys: `identity.equipo-eliminado.v1`, `identity.liderazgo-equipo-modificado.v1`, `identity.equipo-desactivado.v1`, `identity.equipo-reactivado.v1`.

- [ ] **Step 1: Escribir el test de routing que falla**

```csharp
using Umbral.IdentityService.Infrastructure.Services.Messaging;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Infrastructure.Messaging;

public sealed class IdentityEventRoutingTests
{
    [Theory]
    [InlineData("EquipoEliminado", "identity.equipo-eliminado.v1")]
    [InlineData("LiderazgoEquipoModificado", "identity.liderazgo-equipo-modificado.v1")]
    [InlineData("EquipoDesactivado", "identity.equipo-desactivado.v1")]
    [InlineData("EquipoReactivado", "identity.equipo-reactivado.v1")]
    public void RoutingKeyFor_mapea_los_eventos_de_ciclo_de_vida(string eventType, string expected)
        => Assert.Equal(expected, IdentityEventRouting.RoutingKeyFor(eventType));
}
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~IdentityEventRoutingTests`
Expected: FAIL (`KeyNotFoundException`).

- [ ] **Step 3: Añadir records + métodos a la interfaz**

En `IIdentityEventsPublisher.cs`, añadir a la interfaz las 4 firmas y al final los 4 records (siguiendo el estilo de los existentes).

- [ ] **Step 4: Implementar en los 3 publicadores**

- `RabbitMqIdentityEventsPublisher`: 4 métodos `=> Publicar("EquipoEliminado", e)`, etc.
- `CompositeIdentityEventsPublisher`: 4 métodos `=> FanOut(p => p.PublishEquipoEliminadoAsync(e, ct))`, etc.
- `NoOpIdentityEventsPublisher`: 4 métodos que devuelven `Task.CompletedTask` (seguir el patrón del archivo).

- [ ] **Step 5: Añadir las 4 keys al mapa en `IdentityEventRouting`**

```csharp
        ["EquipoEliminado"] = "identity.equipo-eliminado.v1",
        ["LiderazgoEquipoModificado"] = "identity.liderazgo-equipo-modificado.v1",
        ["EquipoDesactivado"] = "identity.equipo-desactivado.v1",
        ["EquipoReactivado"] = "identity.equipo-reactivado.v1",
```

- [ ] **Step 6: Compilar Infrastructure + ejecutar test**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~IdentityEventRoutingTests`
Expected: PASS (4 casos). Verificar que la solución completa compila: `dotnet build "services/identity-service/Umbral.IdentityService.sln"`.

- [ ] **Step 7: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/Interfaces/IIdentityEventsPublisher.cs services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Events/ services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Messaging/IdentityEventRouting.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Infrastructure/Messaging/IdentityEventRoutingTests.cs
git commit -m "feat(identity): eventos de ciclo de vida de equipo (eliminado, liderazgo, desactivado, reactivado)"
```

### Task C2: Correo de notificación de ciclo de vida (puerto + SMTP)

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Interfaces/ITeamLifecycleNotifier.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Notifications/SmtpTeamLifecycleNotifier.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Notifications/TeamLifecycleEmailTemplate.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/DependencyInjection.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Infrastructure/Notifications/TeamLifecycleEmailTemplateTests.cs`

**Interfaces:**
- Consumes: `IUsuarioRepository.GetByIdAsync` (para resolver correo/nombre por `UsuarioId`), `SmtpOptions`, patrón de `SmtpUserWelcomeEmailSender`/`WelcomeEmailTemplate`.
- Produces:
  - `ITeamLifecycleNotifier` con:
    - `Task NotificarEquipoEliminadoAsync(string nombreEquipo, IReadOnlyList<Guid> miembros, CancellationToken ct)`
    - `Task NotificarLiderazgoModificadoAsync(Guid liderAnteriorUserId, Guid nuevoLiderUserId, CancellationToken ct)`
  - Implementación SMTP **best-effort**: resuelve destinatarios y envía; cualquier fallo se traga y loguea (NO lanza — a diferencia del welcome sender que sí compensa).
  - `TeamLifecycleEmailTemplate` con `BuildEquipoEliminado(nombreEquipo)` y `BuildLiderazgo(esNuevoLider)` → `(Subject, PlainText)` puros y testeables.

- [ ] **Step 1: Escribir el test de plantilla que falla**

```csharp
using Umbral.IdentityService.Infrastructure.Services.Notifications;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Infrastructure.Notifications;

public sealed class TeamLifecycleEmailTemplateTests
{
    [Fact]
    public void Eliminado_menciona_el_nombre_del_equipo()
    {
        var (subject, body) = TeamLifecycleEmailTemplate.BuildEquipoEliminado("Titanes");
        Assert.Contains("Titanes", body);
        Assert.False(string.IsNullOrWhiteSpace(subject));
    }

    [Fact]
    public void Liderazgo_distingue_nuevo_lider_de_anterior()
    {
        var (_, nuevo) = TeamLifecycleEmailTemplate.BuildLiderazgo(esNuevoLider: true);
        var (_, anterior) = TeamLifecycleEmailTemplate.BuildLiderazgo(esNuevoLider: false);
        Assert.NotEqual(nuevo, anterior);
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~TeamLifecycleEmailTemplateTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar plantilla, puerto e implementación SMTP**

`TeamLifecycleEmailTemplate.cs` (métodos `static (string Subject, string PlainText) BuildEquipoEliminado(string nombreEquipo)` y `BuildLiderazgo(bool esNuevoLider)` con textos en español). `ITeamLifecycleNotifier.cs` con las 2 firmas. `SmtpTeamLifecycleNotifier.cs`: inyecta `IUsuarioRepository`, `IOptions<SmtpOptions>`, `ILogger`; resuelve destinatarios por `GetByIdAsync`, envía con `SmtpClient` (patrón del welcome sender), y **envuelve todo en try/catch que loguea y no relanza**.

- [ ] **Step 4: Registrar en DI**

En `DependencyInjection.cs`, tras `AddScoped<IUserWelcomeEmailSender, ...>()`:

```csharp
        services.AddScoped<ITeamLifecycleNotifier, SmtpTeamLifecycleNotifier>();
```

- [ ] **Step 5: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~TeamLifecycleEmailTemplateTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/Interfaces/ITeamLifecycleNotifier.cs services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Notifications/SmtpTeamLifecycleNotifier.cs services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Notifications/TeamLifecycleEmailTemplate.cs services/identity-service/src/Umbral.IdentityService.Infrastructure/DependencyInjection.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Infrastructure/Notifications/TeamLifecycleEmailTemplateTests.cs
git commit -m "feat(identity): notificador SMTP best-effort de ciclo de vida de equipo"
```

---

## Fase D — Guard BR-E10: proyección local por eventos RabbitMQ

### Task D1: Operaciones emite `InscripcionEquipoCreada`/`Cancelada`

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ParticipacionEvents.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/SesionEventRouting.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/RabbitMqSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs` (no-op para estos 2, no tienen payload realtime)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PreinscribirEquipoCommandHandler.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/CancelarInscripcionEquipoCommandHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/` (routing + handler publica)

**Interfaces:**
- Produces (records nuevos en `ParticipacionEvents.cs`):
  - `InscripcionEquipoCreadaEvent(Guid PartidaId, Guid SesionPartidaId, Guid InscripcionId, Guid EquipoId, DateTime Instante)`
  - `InscripcionEquipoCanceladaEvent(Guid PartidaId, Guid InscripcionId, Guid EquipoId, DateTime Instante)`
  - métodos en `ISesionEventsPublisher`: `PublicarInscripcionEquipoCreadaAsync`, `PublicarInscripcionEquipoCanceladaAsync`.
  - routing keys `operaciones-sesion.inscripcion-equipo-creada.v1`, `operaciones-sesion.inscripcion-equipo-cancelada.v1`.
- Consumes en el handler de cancelación: hoy `SesionPartida.CancelarInscripcionEquipo(Guid equipoId, bool callerEsLider)` retorna `void` (`services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs:132`). **Cambiar su firma a `Guid CancelarInscripcionEquipo(...)`** devolviendo `inscripcion.Id.Valor` de la inscripción cancelada, para poder portarlo en el evento. El handler pasa ese Guid al `InscripcionEquipoCanceladaEvent`.
- Proyecto de tests de Operaciones: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/`.

- [ ] **Step 1: Escribir el test de routing que falla**

En el proyecto de unit tests de Operaciones, añadir (o crear) `SesionEventRoutingTests` con:

```csharp
[Theory]
[InlineData("InscripcionEquipoCreada", "operaciones-sesion.inscripcion-equipo-creada.v1")]
[InlineData("InscripcionEquipoCancelada", "operaciones-sesion.inscripcion-equipo-cancelada.v1")]
public void RoutingKeyFor_mapea_inscripcion_equipo(string eventType, string expected)
    => Assert.Equal(expected, SesionEventRouting.RoutingKeyFor(eventType));
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter FullyQualifiedName~SesionEventRoutingTests`
Expected: FAIL.

- [ ] **Step 3: Añadir records, firmas, routing y las implementaciones de publicador**

Records en `ParticipacionEvents.cs`; firmas en `ISesionEventsPublisher`; 2 keys en `SesionEventRouting`; `RabbitMqSesionEventsPublisher` publica con `Publicar("InscripcionEquipoCreada", evento)`; `Composite` hace `FanOut`; `NoOp` y `SignalR` devuelven `Task.CompletedTask` (sin payload realtime documentado).

- [ ] **Step 4: Publicar en los handlers**

En `PreinscribirEquipoCommandHandler`, tras `SaveChangesAsync` (junto a la emisión de convocatorias):

```csharp
        await _events.PublicarInscripcionEquipoCreadaAsync(
            new InscripcionEquipoCreadaEvent(
                sesion.PartidaId, sesion.Id.Valor, inscripcion.Id.Valor, equipo.EquipoId,
                _timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);
```

En `CancelarInscripcionEquipoCommandHandler`: capturar el `Guid` que ahora devuelve `sesion.CancelarInscripcionEquipo(...)` (ver el cambio de firma en Interfaces) y tras `SaveChangesAsync` emitir `InscripcionEquipoCanceladaEvent(request.PartidaId, inscripcionId, equipo.EquipoId, now)`. Inyectar `ISesionEventsPublisher` y `TimeProvider` en el handler de cancelación (hoy no los tiene).

- [ ] **Step 5: Escribir/ejecutar un test de handler que verifique la publicación**

Añadir un test con un `ISesionEventsPublisher` falso que capture las llamadas y afirme que `PreinscribirEquipo` publica `InscripcionEquipoCreadaEvent` con el `EquipoId` correcto. Ejecutar:

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter FullyQualifiedName~PreinscribirEquipo`
Expected: PASS.

- [ ] **Step 6: Ejecutar toda la suite de Operaciones**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"`
Expected: PASS (sin regresiones).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/
git commit -m "feat(operaciones): emitir InscripcionEquipoCreada/Cancelada para el guard de equipos (BR-E10)"
```

### Task D2: Proyección `participaciones_activas_equipo` (entidad, repo, persistencia)

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Entities/ParticipacionActivaEquipo.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IParticipacionActivaEquipoRepository.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/ParticipacionActivaEquipoRepository.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/IdentityDbContext.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/DependencyInjection.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.IntegrationTests/Teams/ParticipacionActivaEquipoPersistenceTests.cs`

**Interfaces:**
- Produces:
  - `ParticipacionActivaEquipo { Guid EquipoId, Guid PartidaId, DateTime FechaRegistroUtc }` con factoría `Registrar(equipoId, partidaId, fechaUtc)`.
  - `IParticipacionActivaEquipoRepository`:
    - `Task UpsertAsync(Guid equipoId, Guid partidaId, DateTime fechaUtc, CancellationToken ct)` (idempotente por PK compuesta)
    - `Task RemoveByPartidaAsync(Guid partidaId, CancellationToken ct)` (borra todas las filas de esa partida — sirve para cancelación por inscripción y para fin/cancelación de partida)
    - `Task RemoveAsync(Guid equipoId, Guid partidaId, CancellationToken ct)`
    - `Task<bool> ExistsByEquipoAsync(Guid equipoId, CancellationToken ct)`
  - tabla `participaciones_activas_equipo`, PK compuesta `(equipoid, partidaid)`.

- [ ] **Step 1: Escribir el test de persistencia que falla**

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Infrastructure.Persistence;
using Xunit;

namespace Umbral.IdentityService.IntegrationTests.Teams;

public sealed class ParticipacionActivaEquipoPersistenceTests
{
    private static IdentityDbContext NewContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"pae-{Guid.NewGuid()}").Options);

    [Fact]
    public async Task Upsert_es_idempotente_y_Exists_lo_detecta()
    {
        await using var ctx = NewContext();
        var repo = new ParticipacionActivaEquipoRepository(ctx);
        var equipo = Guid.NewGuid();
        var partida = Guid.NewGuid();

        await repo.UpsertAsync(equipo, partida, DateTime.UtcNow, CancellationToken.None);
        await repo.UpsertAsync(equipo, partida, DateTime.UtcNow, CancellationToken.None);

        Assert.True(await repo.ExistsByEquipoAsync(equipo, CancellationToken.None));
        Assert.Equal(1, await ctx.ParticipacionesActivasEquipo.CountAsync());
    }

    [Fact]
    public async Task RemoveByPartida_borra_y_Exists_devuelve_false()
    {
        await using var ctx = NewContext();
        var repo = new ParticipacionActivaEquipoRepository(ctx);
        var equipo = Guid.NewGuid();
        var partida = Guid.NewGuid();
        await repo.UpsertAsync(equipo, partida, DateTime.UtcNow, CancellationToken.None);

        await repo.RemoveByPartidaAsync(partida, CancellationToken.None);

        Assert.False(await repo.ExistsByEquipoAsync(equipo, CancellationToken.None));
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~ParticipacionActivaEquipoPersistenceTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Entidad, interfaz, repo**

`ParticipacionActivaEquipo.cs` (entidad simple con factoría). `IParticipacionActivaEquipoRepository.cs` (firmas arriba). `ParticipacionActivaEquipoRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class ParticipacionActivaEquipoRepository : IParticipacionActivaEquipoRepository
{
    private readonly IdentityDbContext _db;

    public ParticipacionActivaEquipoRepository(IdentityDbContext db) => _db = db;

    public async Task UpsertAsync(Guid equipoId, Guid partidaId, DateTime fechaUtc, CancellationToken cancellationToken)
    {
        var existe = await _db.ParticipacionesActivasEquipo
            .AnyAsync(x => x.EquipoId == equipoId && x.PartidaId == partidaId, cancellationToken);
        if (existe) return;

        _db.ParticipacionesActivasEquipo.Add(ParticipacionActivaEquipo.Registrar(equipoId, partidaId, fechaUtc));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveByPartidaAsync(Guid partidaId, CancellationToken cancellationToken)
    {
        var filas = await _db.ParticipacionesActivasEquipo
            .Where(x => x.PartidaId == partidaId).ToListAsync(cancellationToken);
        if (filas.Count == 0) return;
        _db.ParticipacionesActivasEquipo.RemoveRange(filas);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid equipoId, Guid partidaId, CancellationToken cancellationToken)
    {
        var fila = await _db.ParticipacionesActivasEquipo
            .FirstOrDefaultAsync(x => x.EquipoId == equipoId && x.PartidaId == partidaId, cancellationToken);
        if (fila is null) return;
        _db.ParticipacionesActivasEquipo.Remove(fila);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsByEquipoAsync(Guid equipoId, CancellationToken cancellationToken)
        => _db.ParticipacionesActivasEquipo.AnyAsync(x => x.EquipoId == equipoId, cancellationToken);
}
```

- [ ] **Step 4: DbSet + mapeo (PK compuesta) + DI**

En `IdentityDbContext`: `public DbSet<ParticipacionActivaEquipo> ParticipacionesActivasEquipo => Set<ParticipacionActivaEquipo>();` y bloque:

```csharp
        modelBuilder.Entity<ParticipacionActivaEquipo>(entity =>
        {
            entity.ToTable("participaciones_activas_equipo");
            entity.HasKey(x => new { x.EquipoId, x.PartidaId });
            entity.Property(x => x.EquipoId).HasColumnName("equipoid");
            entity.Property(x => x.PartidaId).HasColumnName("partidaid");
            entity.Property(x => x.FechaRegistroUtc).HasColumnName("fecharegistroutc").IsRequired();
        });
```

En `DependencyInjection.cs`: `services.AddScoped<IParticipacionActivaEquipoRepository, ParticipacionActivaEquipoRepository>();`

- [ ] **Step 5: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~ParticipacionActivaEquipoPersistenceTests`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Domain/Entities/ParticipacionActivaEquipo.cs services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IParticipacionActivaEquipoRepository.cs services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/ services/identity-service/src/Umbral.IdentityService.Infrastructure/DependencyInjection.cs services/identity-service/tests/Umbral.IdentityService.IntegrationTests/Teams/ParticipacionActivaEquipoPersistenceTests.cs
git commit -m "feat(identity): proyección participaciones_activas_equipo (guard BR-E10)"
```

### Task D3: Consumidor RabbitMQ en Identity que alimenta la proyección

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Messaging/OperacionesEnvelopeReader.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Interfaces/IParticipacionProjectionUpdater.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Services/ParticipacionProjectionUpdater.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Api/Workers/OperacionesInscripcionesConsumer.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Messaging/RabbitMqConsumerOptions.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs` (registrar el worker + options)
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Infrastructure/Messaging/ParticipacionProjectionUpdaterTests.cs`

**Interfaces:**
- Consumes: `IParticipacionActivaEquipoRepository` (Task D2), el envelope camelCase `{ eventId, eventType, occurredAt, payload }`, exchange `umbral.operaciones-sesion` (topic, durable), patrón del consumidor de Puntuaciones (`services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/OperacionesSesionEventsConsumer.cs`).
- Produces:
  - `IParticipacionProjectionUpdater` con `Task AplicarAsync(string eventType, JsonElement payload, CancellationToken ct)` — mapea:
    - `InscripcionEquipoCreada` → `Upsert(equipoId, partidaId, occurredAt)`
    - `InscripcionEquipoCancelada` → `Remove(equipoId, partidaId)`
    - `PartidaFinalizada` / `PartidaCancelada` → `RemoveByPartida(partidaId)`
    - cualquier otro → no-op.
  - `OperacionesInscripcionesConsumer : BackgroundService` con cola `identity.operaciones-sesion.participaciones` (durable), bound a las 4 routing keys: `operaciones-sesion.inscripcion-equipo-creada.v1`, `operaciones-sesion.inscripcion-equipo-cancelada.v1`, `operaciones-sesion.partida-finalizada.v1`, `operaciones-sesion.partida-cancelada.v1`. Best-effort ack-siempre, scope por mensaje (`IServiceScopeFactory`), idempotente.
  - `RabbitMqConsumerOptions` `{ bool Enabled, string? Host, int Port=5672, string User, string Password, string Exchange="umbral.operaciones-sesion", string Queue="identity.operaciones-sesion.participaciones", string[] Bindings }`.

- [ ] **Step 1: Escribir el test del updater que falla (idempotencia + mapeo)**

```csharp
using System.Text.Json;
using Umbral.IdentityService.Application.Services;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Infrastructure.Messaging;

public sealed class ParticipacionProjectionUpdaterTests
{
    private sealed class FakeRepo : IParticipacionActivaEquipoRepository
    {
        public HashSet<(Guid, Guid)> Filas { get; } = new();
        public Task UpsertAsync(Guid e, Guid p, DateTime f, CancellationToken ct) { Filas.Add((e, p)); return Task.CompletedTask; }
        public Task RemoveByPartidaAsync(Guid p, CancellationToken ct) { Filas.RemoveWhere(x => x.Item2 == p); return Task.CompletedTask; }
        public Task RemoveAsync(Guid e, Guid p, CancellationToken ct) { Filas.Remove((e, p)); return Task.CompletedTask; }
        public Task<bool> ExistsByEquipoAsync(Guid e, CancellationToken ct) => Task.FromResult(Filas.Any(x => x.Item1 == e));
    }

    private static JsonElement Payload(object o) =>
        JsonSerializer.SerializeToElement(o, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public async Task Creada_luego_Cancelada_deja_la_proyeccion_vacia()
    {
        var repo = new FakeRepo();
        var updater = new ParticipacionProjectionUpdater(repo);
        var equipo = Guid.NewGuid(); var partida = Guid.NewGuid();

        await updater.AplicarAsync("InscripcionEquipoCreada",
            Payload(new { equipoId = equipo, partidaId = partida, instante = DateTime.UtcNow }), CancellationToken.None);
        Assert.True(await repo.ExistsByEquipoAsync(equipo, CancellationToken.None));

        await updater.AplicarAsync("InscripcionEquipoCancelada",
            Payload(new { equipoId = equipo, partidaId = partida }), CancellationToken.None);
        Assert.False(await repo.ExistsByEquipoAsync(equipo, CancellationToken.None));
    }

    [Fact]
    public async Task PartidaFinalizada_limpia_por_partida()
    {
        var repo = new FakeRepo();
        var updater = new ParticipacionProjectionUpdater(repo);
        var partida = Guid.NewGuid();
        await updater.AplicarAsync("InscripcionEquipoCreada",
            Payload(new { equipoId = Guid.NewGuid(), partidaId = partida, instante = DateTime.UtcNow }), CancellationToken.None);

        await updater.AplicarAsync("PartidaFinalizada",
            Payload(new { partidaId = partida, sesionPartidaId = Guid.NewGuid(), fechaFin = DateTime.UtcNow }), CancellationToken.None);

        Assert.Empty(repo.Filas);
    }

    [Fact]
    public async Task Evento_desconocido_es_noop()
    {
        var repo = new FakeRepo();
        var updater = new ParticipacionProjectionUpdater(repo);
        await updater.AplicarAsync("JuegoActivado", Payload(new { partidaId = Guid.NewGuid() }), CancellationToken.None);
        Assert.Empty(repo.Filas);
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~ParticipacionProjectionUpdaterTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar el updater (Application)**

`IParticipacionProjectionUpdater.cs` (firma arriba). `ParticipacionProjectionUpdater.cs`:

```csharp
using System.Text.Json;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Services;

public sealed class ParticipacionProjectionUpdater : IParticipacionProjectionUpdater
{
    private readonly IParticipacionActivaEquipoRepository _repo;

    public ParticipacionProjectionUpdater(IParticipacionActivaEquipoRepository repo) => _repo = repo;

    public async Task AplicarAsync(string eventType, JsonElement payload, CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case "InscripcionEquipoCreada":
                await _repo.UpsertAsync(Guid(payload, "equipoId"), Guid(payload, "partidaId"), DateTime.UtcNow, cancellationToken);
                break;
            case "InscripcionEquipoCancelada":
                await _repo.RemoveAsync(Guid(payload, "equipoId"), Guid(payload, "partidaId"), cancellationToken);
                break;
            case "PartidaFinalizada":
            case "PartidaCancelada":
                await _repo.RemoveByPartidaAsync(Guid(payload, "partidaId"), cancellationToken);
                break;
        }
    }

    private static Guid Guid(JsonElement payload, string prop) =>
        payload.TryGetProperty(prop, out var v) ? v.GetGuid() : System.Guid.Empty;
}
```

Registrar en DI (`DependencyInjection.cs`): `services.AddScoped<IParticipacionProjectionUpdater, ParticipacionProjectionUpdater>();`

- [ ] **Step 4: Ejecutar y verificar que pasan los tests del updater**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~ParticipacionProjectionUpdaterTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Implementar el `BackgroundService` + options + envelope reader**

`RabbitMqConsumerOptions.cs` (props arriba, con `Bindings` por defecto = las 4 keys). `OperacionesEnvelopeReader.cs`: `static bool TryRead(ReadOnlySpan<byte> body, out string eventType, out JsonElement payload)` (parsea el envelope, extrae `eventType` y `payload`). `OperacionesInscripcionesConsumer.cs`: copiar la estructura de `OperacionesSesionEventsConsumer` de Puntuaciones (conexión con reintento a 30 s, `ExchangeDeclare` topic durable, `QueueDeclare` durable, `QueueBind` por cada binding, `AsyncEventingBasicConsumer`, `autoAck:false` + `BasicAck` en `finally`, scope por mensaje vía `IServiceScopeFactory` para resolver `IParticipacionProjectionUpdater`). No arrancar si `!Enabled || Host vacío`.

- [ ] **Step 6: Registrar el worker en `Program.cs`**

Enlazar `RabbitMqConsumerOptions` desde configuración (sección `RabbitMqConsumer` o equivalente; documentarlo en GUIA-LEVANTAMIENTO en Fase H) y `builder.Services.AddHostedService<OperacionesInscripcionesConsumer>();` solo si está habilitado, siguiendo cómo Puntuaciones registra el suyo.

- [ ] **Step 7: Compilar solución + suite Identity**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln"`
Expected: PASS (sin regresiones).

- [ ] **Step 8: Commit**

```bash
git add services/identity-service/
git commit -m "feat(identity): consumidor RabbitMQ que proyecta participaciones activas de equipo (BR-E10)"
```

---

## Fase E — Eliminación por líder con guard + notificación (HU-06, BR-E06/E10)

### Task E1: Excepción de aplicación + command/handler de eliminación por líder

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Exceptions/EquipoConParticipacionActivaException.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Commands/EliminarMiEquipoCommand.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/DTOs/EliminarEquipoResponse.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Commands/EliminarMiEquipoCommandHandler.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/EliminarMiEquipoHandlerTests.cs`

**Interfaces:**
- Consumes: `IEquipoRepository.GetActiveByMemberUserIdAsync/UpdateAsync`, `IInvitacionEquipoRepository.DeletePendientesByEquipoAsync`, `IParticipacionActivaEquipoRepository.ExistsByEquipoAsync`, `IIdentityEventsPublisher.PublishEquipoEliminadoAsync`, `ITeamLifecycleNotifier.NotificarEquipoEliminadoAsync`, `Equipo.EliminarPorLider`.
- Produces:
  - `EquipoConParticipacionActivaException(Guid equipoId)` → mapear a **409 Conflict**.
  - `EliminarMiEquipoCommand(Guid ActorUserId) : IRequest<EliminarEquipoResponse>`
  - `EliminarEquipoResponse(Guid EquipoId, string Estado)`
  - Handler que: carga equipo activo del actor (404 vía `NoActiveTeamForParticipantException` si no hay), valida guard (`ExistsByEquipoAsync` → 409), llama `EliminarPorLider(actor)` (403 si no es líder), persiste, borra invitaciones pendientes, publica evento y notifica (best-effort).

- [ ] **Step 1: Escribir tests del handler que fallan**

```csharp
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Handlers.Commands;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class EliminarMiEquipoHandlerTests
{
    [Fact]
    public async Task Elimina_borra_invitaciones_publica_y_notifica()
    {
        // Arrange: fakes de IEquipoRepository (devuelve equipo activo con líder=actor y 1 miembro),
        // IInvitacionEquipoRepository (captura DeletePendientes), IParticipacionActivaEquipoRepository
        // (ExistsByEquipo=false), IIdentityEventsPublisher y ITeamLifecycleNotifier (capturan llamadas).
        // Act: Handle(new EliminarMiEquipoCommand(actor)).
        // Assert: equipo.Estado == Eliminado; invitaciones borradas; evento EquipoEliminado con Origen="Lider"
        // y los 2 miembros; notifier llamado con los 2 miembros.
        Assert.True(true); // estructura — completar con los fakes reales
    }

    [Fact]
    public async Task Con_participacion_activa_lanza_EquipoConParticipacionActiva()
    {
        // ExistsByEquipoAsync => true ⇒ Assert.ThrowsAsync<EquipoConParticipacionActivaException>.
        Assert.True(true);
    }
}
```

> Nota: reusar los fakes de `SalirDeEquipo`/`CrearEquipo` handler tests (misma carpeta `Teams/`) para `IEquipoRepository` e `IInvitacionEquipoRepository`; crear fakes triviales para los repos/servicios nuevos.

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~EliminarMiEquipoHandlerTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar excepción, command, DTO, handler**

Excepción:

```csharp
namespace Umbral.IdentityService.Application.Exceptions;

public sealed class EquipoConParticipacionActivaException : Exception
{
    public Guid EquipoId { get; }
    public EquipoConParticipacionActivaException(Guid equipoId)
        : base($"El equipo {equipoId} participa en una partida en Lobby/Iniciada y no puede eliminarse.")
        => EquipoId = equipoId;
}
```

Handler (núcleo):

```csharp
public async Task<EliminarEquipoResponse> Handle(EliminarMiEquipoCommand request, CancellationToken cancellationToken)
{
    var equipo = await _equipos.GetActiveByMemberUserIdAsync(request.ActorUserId, cancellationToken)
        ?? throw new NoActiveTeamForParticipantException(request.ActorUserId);

    if (await _participaciones.ExistsByEquipoAsync(equipo.EquipoId, cancellationToken))
        throw new EquipoConParticipacionActivaException(equipo.EquipoId);

    var nombre = equipo.NombreEquipo;
    IReadOnlyList<Guid> miembros;
    try { miembros = equipo.EliminarPorLider(request.ActorUserId); }
    catch (ActorNoEsLiderEquipoException) { throw new NoEsLiderException(request.ActorUserId); }

    await _equipos.UpdateAsync(equipo, cancellationToken);
    await _invitaciones.DeletePendientesByEquipoAsync(equipo.EquipoId, cancellationToken);

    await _events.PublishEquipoEliminadoAsync(
        new EquipoEliminadoIntegrationEvent(equipo.EquipoId, nombre, "Lider", miembros, DateTime.UtcNow),
        cancellationToken);
    await _notifier.NotificarEquipoEliminadoAsync(nombre, miembros, cancellationToken);

    return new EliminarEquipoResponse(equipo.EquipoId, equipo.Estado.ToString());
}
```

(Verificar que `NoEsLiderException` acepta un `Guid`; si no, usar el constructor real.)

- [ ] **Step 4: Mapear la excepción a 409 en el middleware**

En `ExceptionHandlingMiddleware.cs`, añadir a la expresión `switch`:

```csharp
            EquipoConParticipacionActivaException => HttpStatusCode.Conflict,
```

- [ ] **Step 5: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~EliminarMiEquipoHandlerTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/Exceptions/EquipoConParticipacionActivaException.cs services/identity-service/src/Umbral.IdentityService.Application/Commands/EliminarMiEquipoCommand.cs services/identity-service/src/Umbral.IdentityService.Application/DTOs/EliminarEquipoResponse.cs services/identity-service/src/Umbral.IdentityService.Application/Handlers/Commands/EliminarMiEquipoCommandHandler.cs services/identity-service/src/Umbral.IdentityService.Api/Middleware/ExceptionHandlingMiddleware.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/EliminarMiEquipoHandlerTests.cs
git commit -m "feat(identity): eliminar mi equipo (líder) con guard BR-E10, limpieza de invitaciones y notificación (HU-06)"
```

### Task E2: Endpoint `DELETE /identity/teams/mine`

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsController.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/TeamsControllerTests.cs`

**Interfaces:**
- Consumes: `EliminarMiEquipoCommand` (Task E1), `AuthenticatedUserClaims.TryGetUserId`.
- Produces: `DELETE /identity/teams/mine` → 204 NoContent (401 sin sub).

- [ ] **Step 1: Escribir el test de controller que falla**

```csharp
    [Fact]
    public async Task EliminarMiEquipo_Dispatches_And_Returns_NoContent()
    {
        var actor = Guid.NewGuid();
        var sender = new FakeSender { NextResponse = new EliminarEquipoResponse(Guid.NewGuid(), "Eliminado") };
        var controller = BuildController(sender, actor);

        var result = await controller.EliminarMiEquipo(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var command = Assert.IsType<EliminarMiEquipoCommand>(sender.LastRequest);
        Assert.Equal(actor, command.ActorUserId);
    }

    [Fact]
    public async Task EliminarMiEquipo_Returns_Unauthorized_When_No_Sub()
    {
        var controller = BuildController(new FakeSender(), sub: null);
        Assert.IsType<UnauthorizedResult>(await controller.EliminarMiEquipo(CancellationToken.None));
    }
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~TeamsControllerTests.EliminarMiEquipo`
Expected: FAIL de compilación.

- [ ] **Step 3: Añadir la acción**

En `TeamsController.cs`:

```csharp
    [HttpDelete("mine")]
    public async Task<IActionResult> EliminarMiEquipo(CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        await _sender.Send(new EliminarMiEquipoCommand(actorUserId), cancellationToken);
        return NoContent();
    }
```

- [ ] **Step 4: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~TeamsControllerTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsController.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/TeamsControllerTests.cs
git commit -m "feat(identity): DELETE /identity/teams/mine (HU-06)"
```

---

## Fase F — CRUD administrativo de equipos (HU-09)

### Task F1: Query admin (listar/detalle) + DTOs

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Queries/GetEquiposAdminQuery.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Queries/GetEquipoAdminByIdQuery.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/DTOs/EquipoAdminResponse.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/GetEquiposAdminQueryHandler.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/GetEquipoAdminByIdQueryHandler.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IEquipoRepository.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/EquipoRepository.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/GetEquiposAdminQueryHandlerTests.cs`

**Interfaces:**
- Produces:
  - En `IEquipoRepository`: `Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken ct)` (todos los estados, con `Participantes`).
  - `EquipoAdminResponse(Guid EquipoId, string NombreEquipo, string Estado, Guid? LiderUserId, IReadOnlyList<EquipoAdminIntegrante> Integrantes)`, `EquipoAdminIntegrante(Guid UsuarioId, bool EsLider)`.
  - `GetEquiposAdminQuery() : IRequest<IReadOnlyList<EquipoAdminResponse>>`, `GetEquipoAdminByIdQuery(Guid EquipoId) : IRequest<EquipoAdminResponse?>`.

- [ ] **Step 1: Escribir el test del handler de listado que falla**

Fake de `IEquipoRepository.GetAllAsync` que devuelve 2 equipos (uno Activo, uno Eliminado); assert que el handler mapea estado, líder e integrantes correctamente y **incluye ambos** (el admin ve todos los estados).

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~GetEquiposAdminQueryHandlerTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Añadir `GetAllAsync` al repo (interfaz + impl)**

En `EquipoRepository.cs`:

```csharp
    public async Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken cancellationToken)
        => await _db.Equipos.Include(e => e.Participantes).ToListAsync(cancellationToken);
```

(Ajustar el nombre del campo `DbContext` según el archivo — revisar el existente.)

- [ ] **Step 4: Crear queries, DTOs y handlers**

Mapear `Equipo` → `EquipoAdminResponse` (líder = `Participantes.FirstOrDefault(p => p.EsLider)?.UsuarioId`). `GetEquipoAdminByIdQueryHandler` usa `GetByIdAsync` y devuelve `null` si no existe.

- [ ] **Step 5: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~GetEquiposAdminQueryHandlerTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/Queries/GetEquiposAdminQuery.cs services/identity-service/src/Umbral.IdentityService.Application/Queries/GetEquipoAdminByIdQuery.cs services/identity-service/src/Umbral.IdentityService.Application/DTOs/EquipoAdminResponse.cs services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/GetEquiposAdminQueryHandler.cs services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/GetEquipoAdminByIdQueryHandler.cs services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IEquipoRepository.cs services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/EquipoRepository.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/GetEquiposAdminQueryHandlerTests.cs
git commit -m "feat(identity): queries admin de equipos (listar/detalle) (HU-09)"
```

### Task F2: Commands admin (crear, renombrar, reasignar líder, cambiar estado, eliminar) + handlers

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Commands/CrearEquipoAdminCommand.cs`
- Create: `.../Commands/RenombrarEquipoAdminCommand.cs`
- Create: `.../Commands/ReasignarLiderazgoAdminCommand.cs`
- Create: `.../Commands/CambiarEstadoEquipoAdminCommand.cs`
- Create: `.../Commands/EliminarEquipoAdminCommand.cs`
- Create: los 5 handlers en `.../Handlers/Commands/`
- Create: validadores en `.../Validators/` para crear/renombrar/reasignar/cambiar-estado
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/EquipoAdminHandlersTests.cs`

**Interfaces:**
- Consumes: `IEquipoRepository`, `IUsuarioRepository.GetByIdAsync` (validar líder existe), `IEquipoRepository.ExistsActiveTeamByUserIdAsync` (líder sin equipo activo al crear), `IHistorialNombreEquipoRepository.AddRangeAsync` (alta al crear + renombrar), `IParticipacionActivaEquipoRepository.ExistsByEquipoAsync` (guard al eliminar), `IIdentityEventsPublisher`, `ITeamLifecycleNotifier`, `Equipo.CrearPorAdmin/Renombrar/ReasignarLiderazgoPorAdmin/Desactivar/Reactivar/EliminarPorAdmin`, `TimeProvider`.
- Produces (todos `IRequest<EquipoAdminResponse>` salvo eliminar que es `IRequest`):
  - `CrearEquipoAdminCommand(string NombreEquipo, Guid LiderUserId)` → crea, registra historial del líder, publica `EquipoCreado`.
  - `RenombrarEquipoAdminCommand(Guid EquipoId, string NombreEquipo)` → renombra, registra historial (1 fila por integrante actual con el nombre nuevo).
  - `ReasignarLiderazgoAdminCommand(Guid EquipoId, Guid NuevoLiderUserId)` → reasigna, publica `LiderazgoEquipoModificado` (Origen="Admin"), notifica a anterior y nuevo.
  - `CambiarEstadoEquipoAdminCommand(Guid EquipoId, string Estado)` → `"Desactivado"|"Activo"`, publica `EquipoDesactivado`/`EquipoReactivado`.
  - `EliminarEquipoAdminCommand(Guid EquipoId)` → guard BR-E10 (`ExistsByEquipoAsync` → `EquipoConParticipacionActivaException`), `EliminarPorAdmin`, borra invitaciones pendientes, publica `EquipoEliminado` (Origen="Admin"), notifica.
  - Todos lanzan `UserNotFoundException`/`404` cuando el equipo no existe (reusar excepción existente o `NoActiveTeamForParticipantException` no aplica — usar una excepción "equipo no encontrado"; si no existe, crear `EquipoNoEncontradoException` → 404 en el middleware).

- [ ] **Step 1: Añadir `EquipoNoEncontradoException` + mapeo 404**

Crear `services/identity-service/src/Umbral.IdentityService.Application/Exceptions/EquipoNoEncontradoException.cs` y añadir al middleware `EquipoNoEncontradoException => HttpStatusCode.NotFound,`.

- [ ] **Step 2: Escribir tests de handlers que fallan (uno por command)**

En `EquipoAdminHandlersTests.cs`, con fakes de todos los repos/servicios:
- Crear: líder inexistente → `UserNotFoundException`; líder con equipo activo → `AlreadyBelongsToActiveTeamException`; happy path → equipo Activo, 1 fila de historial, evento `EquipoCreado`.
- Renombrar: equipo inexistente → `EquipoNoEncontradoException`; happy → nombre cambiado + N filas de historial (N = integrantes).
- Reasignar: nuevo líder no integrante → `NuevoLiderNoPerteneceAlEquipoException` (mapea a 409 vía `TransferirLiderazgoConflictException` — envolver como en `SalirDeEquipo`); happy → evento `LiderazgoEquipoModificado` + notifier a 2 destinatarios.
- Cambiar estado: `"Desactivado"` → estado Desactivado + evento `EquipoDesactivado`; `"Activo"` → Reactivado.
- Eliminar: con participación activa → `EquipoConParticipacionActivaException`; happy → Estado Eliminado, invitaciones borradas, evento `EquipoEliminado` Origen="Admin".

- [ ] **Step 3: Ejecutar y verificar que fallan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~EquipoAdminHandlersTests`
Expected: FAIL de compilación.

- [ ] **Step 4: Implementar commands, validadores y handlers**

Seguir los patrones de los handlers existentes. Para renombrar-historial: `equipo.Participantes.Select(p => HistorialNombreEquipo.Registrar(p.UsuarioId, equipo.EquipoId, nuevoNombre, ahora))`. Envolver excepciones de dominio de reasignación en `TransferirLiderazgoConflictException` como hace el flujo participante. Todos los handlers que mutan devuelven el `EquipoAdminResponse` recargado.

- [ ] **Step 5: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~EquipoAdminHandlersTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/ services/identity-service/src/Umbral.IdentityService.Api/Middleware/ExceptionHandlingMiddleware.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/EquipoAdminHandlersTests.cs
git commit -m "feat(identity): commands admin de equipos (crear/renombrar/reasignar/estado/eliminar) (HU-09)"
```

### Task F3: `AdminTeamsController` + unit tests

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/AdminTeamsController.cs`
- Create: request contracts en `services/identity-service/src/Umbral.IdentityService.Api/Contracts/` (`CrearEquipoAdminRequest`, `RenombrarEquipoRequest`, `ReasignarLiderazgoAdminRequest`, `CambiarEstadoEquipoRequest`)
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/AdminTeamsControllerTests.cs`

**Interfaces:**
- Consumes: los commands/queries de F1/F2, `ISender`, patrón de `TeamsController`/`GovernanceController`.
- Produces: controller `[ApiController] [Route("identity/admin/teams")] [Authorize(Policy = "AdminOnly")]` con:
  - `GET /` → 200 lista
  - `GET /{id:guid}` → 200 / 404
  - `POST /` → 201 (Location `/identity/admin/teams/{id}`)
  - `PATCH /{id:guid}/name` → 200
  - `PATCH /{id:guid}/leadership` → 200
  - `PATCH /{id:guid}/estado` → 200
  - `DELETE /{id:guid}` → 204

- [ ] **Step 1: Escribir los unit tests del controller que fallan**

Cubrir despacho correcto de cada acción (verificar `sender.LastRequest` es el command/query esperado con los ids/valores del path/body) y los códigos (201 con Location en crear, 204 en eliminar, 404 cuando el detalle devuelve null). Usar `FakeSender` como en `TeamsControllerTests`.

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~AdminTeamsControllerTests`
Expected: FAIL de compilación.

- [ ] **Step 3: Implementar contracts + controller**

Controller que despacha por `_sender.Send`, sin lógica de negocio; `GET /{id}` devuelve `NotFound()` si el query da `null`.

- [ ] **Step 4: Ejecutar y verificar que pasan**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --filter FullyQualifiedName~AdminTeamsControllerTests`
Expected: PASS.

- [ ] **Step 5: Suite completa Identity**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Api/Controllers/AdminTeamsController.cs services/identity-service/src/Umbral.IdentityService.Api/Contracts/ services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/AdminTeamsControllerTests.cs
git commit -m "feat(identity): AdminTeamsController (CRUD admin de equipos) (HU-09)"
```

---

## Fase G — Cliente web (admin, HU-09)

### Task G1: Cliente API web `adminTeamsApi`

**Files:**
- Create: `frontend/src/api/adminTeamsApi.ts`
- Test: `frontend/src/api/adminTeamsApi.test.ts`

**Interfaces:**
- Consumes: `VITE_IDENTITY_API_BASE_URL` (patrón de `identityApi.ts`), `IdentityApiError`.
- Produces (todas `(..., accessToken, fetchImpl=fetch)`):
  - `listAdminTeams()` → `AdminTeam[]`
  - `getAdminTeam(id)` → `AdminTeam`
  - `createAdminTeam({ nombreEquipo, liderUserId })` → `AdminTeam`
  - `renameAdminTeam(id, { nombreEquipo })` → `AdminTeam`
  - `reassignAdminTeamLeader(id, { nuevoLiderUserId })` → `AdminTeam`
  - `setAdminTeamEstado(id, { estado })` → `AdminTeam`
  - `deleteAdminTeam(id)` → `void` (lanza `IdentityApiError` con statusCode 409 en participación activa)
  - tipo `AdminTeam { equipoId; nombreEquipo; estado; liderUserId?; integrantes: { usuarioId; esLider }[] }`

- [ ] **Step 1: Escribir tests con `fetch` mockeado que fallan**

Cubrir: `listAdminTeams` hace GET a `/identity/admin/teams` con `Authorization`; `deleteAdminTeam` propaga 409 como `IdentityApiError` con `statusCode===409`; `createAdminTeam` hace POST con el body correcto. Seguir el estilo de `identityApi.test.ts`.

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `cd frontend && npm test -- adminTeamsApi`
Expected: FAIL (módulo inexistente).

- [ ] **Step 3: Implementar `adminTeamsApi.ts`**

Reusar `resolveBaseUrl`/`IdentityApiError` (exportarlos de `identityApi.ts` o replicar el patrón local).

- [ ] **Step 4: Ejecutar y verificar que pasan**

Run: `cd frontend && npm test -- adminTeamsApi`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/adminTeamsApi.ts frontend/src/api/adminTeamsApi.test.ts
git commit -m "feat(web): cliente API adminTeamsApi (HU-09)"
```

### Task G2: Página admin de equipos + ruta + navegación

**Files:**
- Create: `frontend/src/features/identity/TeamsAdminPage.tsx`
- Create: `frontend/src/features/identity/TeamsAdminPage.test.tsx`
- Modify: `frontend/src/app/App.tsx` (añadir ruta `identidad/equipos`)
- Modify: el componente de navegación lateral (localizar el que lista `identidad/usuarios`, `identidad/gobernanza`) para añadir el enlace "Equipos".

**Interfaces:**
- Consumes: `adminTeamsApi` (Task G1), token del contexto de auth (patrón de `UserManagementPage.tsx`/`GovernancePage.tsx`), design system implementado.
- Produces: página que lista equipos (tabla nombre/estado/integrantes/líder) y ofrece crear (nombre + selección de líder), renombrar, reasignar líder (entre integrantes), desactivar/reactivar, eliminar (con confirmación destructiva y mensaje claro ante 409). Reusar `data-testid`/roles del patrón existente; para la lista de usuarios elegibles como líder, reusar la query de participantes elegibles si aplica o el listado de usuarios de `identityApi`.

- [ ] **Step 1: Escribir el test de la página que falla**

Con `adminTeamsApi` mockeado: render → muestra la fila de un equipo; click "Eliminar" + confirmar → llama `deleteAdminTeam`; si `deleteAdminTeam` rechaza con `IdentityApiError(409)` → muestra mensaje "participa en una partida activa". Seguir el estilo de `UserManagementPage.test.tsx`.

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `cd frontend && npm test -- TeamsAdminPage`
Expected: FAIL.

- [ ] **Step 3: Implementar la página, la ruta y el enlace de navegación**

Añadir en `App.tsx` un objeto de ruta `{ path: "identidad/equipos", element: <TeamsAdminPage /> }` junto a las otras `identidad/*`. Añadir el enlace en la navegación.

- [ ] **Step 4: Ejecutar tests + build**

Run: `cd frontend && npm test -- TeamsAdminPage && npm run build`
Expected: PASS + build OK.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/identity/TeamsAdminPage.tsx frontend/src/features/identity/TeamsAdminPage.test.tsx frontend/src/app/App.tsx
git add -A frontend/src   # el componente de navegación modificado
git commit -m "feat(web): página admin de equipos + ruta identidad/equipos (HU-09)"
```

---

## Fase H — Cliente mobile (líder/participante, HU-06 + HU-48)

### Task H1: API + flow de eliminar equipo (líder)

**Files:**
- Create: `mobile/src/features/teams/deleteTeamApi.js`
- Create: `mobile/src/features/teams/deleteTeamFlow.js`
- Test: `mobile/tests/deleteTeamFlow.test.js`

**Interfaces:**
- Consumes: patrón de `leaveTeamApi.js`/`leaveTeamFlow.js`, endpoint `DELETE /identity/teams/mine`.
- Produces:
  - `deleteMyTeam(apiBaseUrl, token, fetchImpl=fetch)` → `{ ok:true }` en 204; `{ ok:false, type, message }` para 404 (`notFound`), 409 (`activeParticipation`, mensaje "Tu equipo participa en una partida activa; no puede eliminarse."), 401 (`unauthorized`), 403 (`forbidden`, "Solo el líder puede eliminar el equipo."), red (`network`).
  - `submitDeleteTeam({ apiBaseUrl, token, fetchImpl })` → envuelve como `submitLeaveTeam`.

- [ ] **Step 1: Escribir el test que falla**

```js
const { test } = require("node:test");
const assert = require("node:assert/strict");
const { submitDeleteTeam } = require("../src/features/teams/deleteTeamFlow.js");

test("204 => ok", async () => {
  const fetchImpl = async () => ({ status: 204, ok: true });
  const r = await submitDeleteTeam({ apiBaseUrl: "http://x", token: "t", fetchImpl });
  assert.equal(r.ok, true);
});

test("409 => activeParticipation con mensaje claro", async () => {
  const fetchImpl = async () => ({ status: 409, ok: false });
  const r = await submitDeleteTeam({ apiBaseUrl: "http://x", token: "t", fetchImpl });
  assert.equal(r.ok, false);
  assert.equal(r.type, "activeParticipation");
  assert.match(r.message, /partida activa/i);
});
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `cd mobile && node --test tests/deleteTeamFlow.test.js`
Expected: FAIL (módulo inexistente).

- [ ] **Step 3: Implementar api + flow**

Copiar la forma de `leaveTeamApi.js`/`leaveTeamFlow.js`, cambiando la ruta a `/identity/teams/mine`, tratando 204 como éxito (sin body) y 409 como `activeParticipation`.

- [ ] **Step 4: Ejecutar y verificar que pasa**

Run: `cd mobile && node --test tests/deleteTeamFlow.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/src/features/teams/deleteTeamApi.js mobile/src/features/teams/deleteTeamFlow.js mobile/tests/deleteTeamFlow.test.js
git commit -m "feat(mobile): flujo de eliminar equipo (líder) (HU-06)"
```

### Task H2: Pantalla "Eliminar equipo" + entrada de navegación

**Files:**
- Create: `mobile/src/features/teams/DeleteTeamScreen.tsx`
- Create: `mobile/src/features/teams/DeleteTeamScreenContainer.tsx`
- Modify: `mobile/src/navigation/RootNavigator.tsx`
- Modify: `mobile/src/navigation/types.ts` (añadir `DeleteTeam` a `AppStackParamList`)
- Modify: la pantalla de equipo/Home desde donde el líder navega (añadir botón "Eliminar equipo")

**Interfaces:**
- Consumes: `submitDeleteTeam` (Task H1), patrón de `LeaveTeamScreenContainer`.
- Produces: pantalla con confirmación destructiva; en éxito navega a Home (equipo eliminado); muestra el mensaje del flow ante 409/otros.

- [ ] **Step 1: Registrar la ruta y el tipo**

Añadir `DeleteTeam: undefined;` a `AppStackParamList` en `types.ts` y `<AppStack.Screen name="DeleteTeam" component={DeleteTeamScreenContainer} options={{ title: "Eliminar equipo" }} />` en `RootNavigator.tsx`.

- [ ] **Step 2: Implementar pantalla + container**

Seguir `LeaveTeamScreen*`. Botón primario destructivo "Eliminar equipo" que llama `submitDeleteTeam`; en `ok` navega a `Home`; si `!ok` muestra `message`.

- [ ] **Step 3: Añadir el punto de entrada de navegación**

En la pantalla de equipo del líder (la misma que enlaza a `LeaveTeam`/`TransferLeadership`), añadir el botón que navega a `DeleteTeam`.

- [ ] **Step 4: Typecheck + tests**

Run: `cd mobile && npm run typecheck && npm test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/src/features/teams/DeleteTeamScreen.tsx mobile/src/features/teams/DeleteTeamScreenContainer.tsx mobile/src/navigation/RootNavigator.tsx mobile/src/navigation/types.ts
git add -A mobile/src   # pantalla de equipo modificada
git commit -m "feat(mobile): pantalla eliminar equipo + navegación (HU-06)"
```

### Task H3: Historial de nombres de equipo (API + flow + pantalla)

**Files:**
- Create: `mobile/src/features/teams/teamHistoryApi.js`
- Create: `mobile/src/features/teams/teamHistoryFlow.js`
- Create: `mobile/src/features/teams/TeamHistoryScreen.tsx`
- Create: `mobile/src/features/teams/TeamHistoryScreenContainer.tsx`
- Modify: `mobile/src/navigation/RootNavigator.tsx`, `mobile/src/navigation/types.ts`
- Modify: la pantalla de equipo/Home (enlace "Historial de equipos")
- Test: `mobile/tests/teamHistoryFlow.test.js`

**Interfaces:**
- Consumes: `GET /identity/teams/mine/history`.
- Produces:
  - `fetchTeamHistory(apiBaseUrl, token, fetchImpl=fetch)` → `{ ok:true, data: { historial: [{ nombreEquipo, equipoId, fechaRegistro }] } }` en 200; `{ ok:false, type, message }` para 401/red.
  - `loadTeamHistory({ apiBaseUrl, token, fetchImpl })` → envoltura con manejo de error; **lista vacía es un éxito** (no error).
  - pantalla que renderiza la lista (o estado vacío "Aún no perteneces a ningún equipo").

- [ ] **Step 1: Escribir el test del flow que falla**

```js
const { test } = require("node:test");
const assert = require("node:assert/strict");
const { loadTeamHistory } = require("../src/features/teams/teamHistoryFlow.js");

test("200 con lista => ok con items", async () => {
  const fetchImpl = async () => ({ status: 200, ok: true, json: async () => ({ historial: [{ nombreEquipo: "Titanes", equipoId: "e", fechaRegistro: "2026-07-08T00:00:00Z" }] }) });
  const r = await loadTeamHistory({ apiBaseUrl: "http://x", token: "t", fetchImpl });
  assert.equal(r.ok, true);
  assert.equal(r.data.historial.length, 1);
});

test("200 con lista vacía => ok", async () => {
  const fetchImpl = async () => ({ status: 200, ok: true, json: async () => ({ historial: [] }) });
  const r = await loadTeamHistory({ apiBaseUrl: "http://x", token: "t", fetchImpl });
  assert.equal(r.ok, true);
  assert.equal(r.data.historial.length, 0);
});
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `cd mobile && node --test tests/teamHistoryFlow.test.js`
Expected: FAIL.

- [ ] **Step 3: Implementar api + flow**

- [ ] **Step 4: Implementar pantalla + container + navegación + enlace**

Añadir `TeamHistory: undefined;` a `AppStackParamList`, la `AppStack.Screen`, y el enlace desde la pantalla de equipo.

- [ ] **Step 5: Typecheck + tests**

Run: `cd mobile && npm run typecheck && npm test`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add mobile/src/features/teams/teamHistoryApi.js mobile/src/features/teams/teamHistoryFlow.js mobile/src/features/teams/TeamHistoryScreen.tsx mobile/src/features/teams/TeamHistoryScreenContainer.tsx mobile/src/navigation/RootNavigator.tsx mobile/src/navigation/types.ts mobile/tests/teamHistoryFlow.test.js
git add -A mobile/src   # pantalla de equipo modificada
git commit -m "feat(mobile): historial de nombres de equipo (HU-48)"
```

---

## Fase I — Contratos, trazabilidad y aceptación

### Task I1: Actualizar contratos HTTP y de eventos

**Files:**
- Modify: `contracts/http/identity-api.md` (endpoints `DELETE /identity/teams/mine`, `GET /identity/teams/mine/history`, y toda la sección `identity/admin/teams`)
- Modify: `contracts/events/identity-events.md` (registrar `EquipoEliminado`, `LiderazgoEquipoModificado`, `EquipoDesactivado`, `EquipoReactivado` con routing keys y payloads; documentar el consumidor `identity.operaciones-sesion.participaciones`)
- Modify: `contracts/events/operaciones-sesion-events.md` (registrar `InscripcionEquipoCreada`/`InscripcionEquipoCancelada` en registry, routing y payloads)

**Interfaces:**
- Consumes: las firmas reales implementadas en Fases C/D/E/F. Copiar shapes exactos.

- [ ] **Step 1: Documentar los endpoints de Identity** (verbo, ruta, auth, respuestas 2xx/4xx incl. 409 de guard).
- [ ] **Step 2: Documentar los 4 eventos de Identity + el consumidor** (tabla registry, tabla routing, bloques de payload JSON).
- [ ] **Step 3: Documentar los 2 eventos de Operaciones** (registry, routing, payloads).
- [ ] **Step 4: Commit**

```bash
git add contracts/http/identity-api.md contracts/events/identity-events.md contracts/events/operaciones-sesion-events.md
git commit -m "docs(contracts): equipos-admin — endpoints admin/mine y eventos de ciclo de vida + inscripción equipo"
```

### Task I2: SDD acceptance + matriz de trazabilidad + GUIA-LEVANTAMIENTO

**Files:**
- Create: `docs/04-sdd/specs/<slug-equipos-admin>/acceptance.md` (o el layout que use el repo; verificar `docs/04-sdd/SPECS-LIST.md` y añadir la entrada)
- Modify: `docs/04-sdd/traceability-matrix.md` (marcar HU-06, HU-09, HU-48, BR-E06/E10/E11 con el slice y su estado)
- Modify: `GUIA-LEVANTAMIENTO.md` (vars de entorno del consumidor RabbitMQ de Identity: `RabbitMqConsumer__Enabled`, `__Host`, etc.)
- Modify: `docs/04-sdd/auditorias/2026-07-06-auditoria-cobertura-requisitos.md` (nota de que Bloque 4A queda cubierto; HU-19 queda como 4B pendiente)

**Interfaces:**
- Consumes: resultados de las fases anteriores.

- [ ] **Step 1: Escribir `acceptance.md`** con criterios verificables por HU (06/09/48) y BR (E06/E10/E11), incluyendo el caveat de consistencia eventual del guard.
- [ ] **Step 2: Actualizar la matriz de trazabilidad** con las filas correspondientes.
- [ ] **Step 3: Documentar las env vars del consumidor** en GUIA-LEVANTAMIENTO.
- [ ] **Step 4: Actualizar la auditoría** (Bloque 4A hecho; 4B = HU-19 pendiente).
- [ ] **Step 5: Commit**

```bash
git add docs/
git commit -m "docs(sdd): acceptance + trazabilidad + guía para el slice equipos-admin (Bloque 4A)"
```

### Task I3: Verificación final end-to-end

**Files:** ninguno (verificación).

- [ ] **Step 1: Suite backend Identity**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln"`
Expected: PASS.

- [ ] **Step 2: Suite backend Operaciones**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"`
Expected: PASS.

- [ ] **Step 3: Web**

Run: `cd frontend && npm test && npm run build`
Expected: PASS + build OK.

- [ ] **Step 4: Mobile**

Run: `cd mobile && npm run typecheck && npm test`
Expected: PASS.

- [ ] **Step 5: Verificación manual del guard (dev)** — con infra levantada, inscribir un equipo en una partida en Lobby, intentar `DELETE /identity/teams/mine` y confirmar 409; cancelar la inscripción, reintentar y confirmar 204. Documentar el resultado en el PR/nota de cierre.

---

## Self-Review (registro)

- **Cobertura del spec:** HU-06 → E1/E2/H1/H2; HU-09 → F1/F2/F3/G1/G2; HU-48 → B1–B5/B4/H3; BR-E06 → E1 (borra invitaciones) + Fase C evento; BR-E10 → D1/D2/D3/E1/F2 (guard) + nota de la primera mitad ya cubierta (documentar en I1); BR-E11 → B1–B5 (historial sobrevive a soft delete). Notificación → C2 + E1/F2. Eventos → C1/D1.
- **Placeholders:** los Steps con `Assert.True(true)` en A?/E1/F2 son andamiaje explícito con nota de qué asertar y qué fakes reusar (los fakes reales viven en tests existentes de la misma carpeta); completarlos es parte del Step. No hay TODO/TBD sin instrucción.
- **Consistencia de tipos:** `EquipoConParticipacionActivaException` (409), `EquipoEliminadoInmutableException`, `EquipoNoEncontradoException` (404) usadas consistentemente; `AdminTeam`/`EquipoAdminResponse` alineados web↔backend; nombres de métodos de dominio (`EliminarPorLider/EliminarPorAdmin/ReasignarLiderazgoPorAdmin/Desactivar/Reactivar/Renombrar/CrearPorAdmin`) idénticos en dominio, handlers y plan.
- **Verificado en código:** proyecto de tests de Operaciones = `Umbral.OperacionesSesion.UnitTests`; `SesionPartida.CancelarInscripcionEquipo` hoy retorna `void` (`SesionPartida.cs:132`) → la Task D1 cambia su firma a `Guid`. Identity usa `EnsureCreatedAsync` (no migraciones) → tablas nuevas por `DbSet` + backfill de arranque (Task B5). Identity hoy solo publica eventos → la Task D3 estrena el primer consumidor, modelado sobre `OperacionesSesionEventsConsumer` de Puntuaciones.
