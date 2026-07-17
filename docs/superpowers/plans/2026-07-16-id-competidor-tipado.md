# Tipado del id local y renombrado del subject — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que filtrar el `UsuarioId` local al mundo de equipos no compile, y que el estado persistido de ese mundo se llame `SubjectId`, que es lo que de verdad guarda.

**Architecture:** Dos cambios independientes en Identity. Primero un **renombrado** de estado persistido (`UsuarioId`/`InvitadoUserId`/`InvitadoPorUserId` → `SubjectId`/`InvitadoSubjectId`/`InvitadoPorSubjectId`), que sigue siendo `Guid` y arrastra columnas e índices. Después un **tipo** (`UsuarioLocalId`, `readonly record struct`) para `Usuario.UsuarioId`, con `ValueConverter` de EF — copiando el patrón que Partidas ya usa con `PartidaId`. El tipo vive solo dentro del dominio: los DTOs desenvuelven a `Guid` en el borde y ningún contrato HTTP cambia.

**Tech Stack:** .NET 8, EF Core + Npgsql, MediatR, xUnit.

**Spec:** `docs/superpowers/specs/2026-07-16-id-competidor-tipado-design.md`

## Global Constraints

- **Cero cambios en contratos HTTP.** Los DTOs siguen exponiendo `Guid` crudo (`UserDetailResponse(Guid UserId, string KeycloakId, …)`, `ParticipanteElegibleResponse(Guid UserId, …)`). Web y móvil no se enteran.
- **Cero cambios en contratos de eventos.** Los integration events siguen llevando `Guid`.
- **Cero cambios** en Operaciones de Sesión, Puntuaciones, Partidas, gateway, `frontend/` y `mobile/`.
- **El límite del renombrado:** se renombra **estado persistido** — propiedades de entidad de dominio y los parámetros de sus factories/constructores, que son la API de la entidad. **No** se renombran parámetros de handlers, propiedades de comandos (`request.ActorUserId`) ni variables locales: son ~400 sitios de churn mecánico, son locales y no engañan a nadie que lea el modelo.
- **El rojo de los ciclos TDD de Task 1 y Task 2 es un error de compilación**, no una aserción fallida. C# no permite otra cosa al cambiar el nombre o el tipo de un miembro público. Está previsto; el compilador es la lista de trabajo.
- **Convención de columnas de este servicio:** minúsculas sin separadores (`usuarioid`, `invitadouserid`) → `subjectid`, `invitadosubjectid`, `invitadoporsubjectid`.
- La suite de Identity está en **345 verdes** (249 unit + 49 integration + 47 contract). Debe acabar en 345 o más.

---

## File Structure

| Archivo | Responsabilidad | Task |
|---|---|---|
| `src/…Domain/Entities/ParticipanteEquipo.cs` | **Modificar** — `UsuarioId` → `SubjectId` | 1 |
| `src/…Domain/Entities/InvitacionEquipo.cs` | **Modificar** — `InvitadoUserId`/`InvitadoPorUserId` → `InvitadoSubjectId`/`InvitadoPorSubjectId` | 1 |
| `src/…Domain/Entities/HistorialNombreEquipo.cs` | **Modificar** — `UsuarioId` → `SubjectId` | 1 |
| `src/…Domain/Entities/Equipo.cs` | **Modificar** — usos de `p.UsuarioId` y params de sus métodos | 1 |
| `src/…Infrastructure/Persistence/IdentityDbContext.cs` | **Modificar** — `HasColumnName` + `HasDatabaseName` de los 3 índices (Task 1); `ValueConverter` de `UsuarioLocalId` (Task 2) | 1, 2 |
| `src/…Infrastructure/Persistence/Migrations/*_RenombrarSubjectIdEnEquipos.cs` | **Crear** (generada) — `RenameColumn` × 4 + `RenameIndex` × 3 | 1 |
| `contracts/http/identity-api.md:76` | **Modificar** — la nota cita el nombre viejo | 1 |
| `src/…Domain/ValueObjects/UsuarioLocalId.cs` | **Crear** — carpeta nueva en este servicio | 2 |
| `src/…Domain/Entities/Usuario.cs` | **Modificar** — `UsuarioId`: `Guid` → `UsuarioLocalId` | 2 |
| `src/…Domain/Abstractions/Persistence/IUsuarioRepository.cs` | **Modificar** — firmas + el doc comment que explica los dos espacios | 2 |
| `src/…Infrastructure/Persistence/UsuarioRepository.cs` | **Modificar** — 2 sitios | 2 |
| `docs/04-sdd/SPECS-LIST.md`, `docs/04-sdd/traceability-matrix.md` | **Modificar** | 3 |

### Aviso de churn (Task 1)

El renombrado toca, entre `src/` y `tests/`:

| Símbolo | Sitios |
|---|---|
| `InvitadoUserId` | 37 |
| `InvitadoPorUserId` | 12 |
| `.UsuarioId` sobre `ParticipanteEquipo` (`p.`, `x.`, `lider.`, `nuevoLider.`, `miembro.`, `participante.`) | 64 |

Es mecánico pero **no es un find-and-replace ciego**: `UsuarioId` también es el nombre del id local en `Usuario`, que en Task 1 **no se toca**. El discriminante es el tipo del receptor, no el texto.

**Cinco comentarios citan el nombre viejo** y quedarían mintiendo:

- `src/…/Handlers/Queries/GetParticipantesElegiblesQueryHandler.cs:43`
- `src/…/Handlers/Queries/ListarEquiposQueryHandler.cs:25`
- `tests/…/Teams/Invitations/GetParticipantesElegiblesHandlerTests.cs:59`
- `tests/…/Teams/ListarEquiposQueryHandlerTests.cs:61`
- `contracts/http/identity-api.md:76`

---

### Task 1: Renombrar el estado persistido a `SubjectId`

Va primero porque es independiente del tipo y deja el código legible antes de tipar: con `SubjectId` a la vista, los sitios que de verdad usan el id local (Task 2) saltan solos.

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Domain/Entities/ParticipanteEquipo.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Domain/Entities/InvitacionEquipo.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Domain/Entities/HistorialNombreEquipo.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Domain/Entities/Equipo.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/IdentityDbContext.cs:53,56-58,67-68,71-72,92,96`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/Migrations/<timestamp>_RenombrarSubjectIdEnEquipos.cs` (generada)
- Modify: `contracts/http/identity-api.md:76`
- Test: todos los de `tests/…/Teams/` que nombren las propiedades (el compilador los enumera)

**Interfaces:**
- Produces: `ParticipanteEquipo.SubjectId` (`Guid`), `InvitacionEquipo.InvitadoSubjectId` / `InvitadoPorSubjectId` (`Guid`), `HistorialNombreEquipo.SubjectId` (`Guid`).
- Produces: `InvitacionEquipo.Crear(Guid equipoId, Guid invitadoSubjectId, Guid invitadoPorSubjectId)`, `HistorialNombreEquipo.Registrar(Guid subjectId, Guid equipoId, string nombreEquipo, DateTime fechaUtc)`, `ParticipanteEquipo.CrearCreador(Guid subjectId)` / `CrearIntegrante(Guid subjectId)`.
- Consumes: nada.

- [ ] **Step 1: Renombrar en `ParticipanteEquipo.cs`**

Archivo completo tras el cambio:

```csharp
namespace Umbral.IdentityService.Domain.Entities;

public sealed class ParticipanteEquipo
{
    public Guid ParticipanteEquipoId { get; private set; }

    // El sub de OIDC (hoy Keycloak), no el UsuarioId local de la tabla usuarios: son dos Guid
    // sin relacion. Con este id llega el actor en el token.
    public Guid SubjectId { get; private set; }
    public DateTime FechaUnionUtc { get; private set; }
    public bool EsLider { get; private set; }

    private ParticipanteEquipo()
    {
    }

    private ParticipanteEquipo(Guid subjectId, bool esLider)
    {
        if (subjectId == Guid.Empty)
        {
            throw new ArgumentException("SubjectId requerido", nameof(subjectId));
        }

        ParticipanteEquipoId = Guid.NewGuid();
        SubjectId = subjectId;
        FechaUnionUtc = DateTime.UtcNow;
        EsLider = esLider;
    }

    public static ParticipanteEquipo CrearCreador(Guid subjectId)
    {
        return new ParticipanteEquipo(subjectId, true);
    }

    public static ParticipanteEquipo CrearIntegrante(Guid subjectId)
    {
        return new ParticipanteEquipo(subjectId, false);
    }

    public void MarcarComoLider()
    {
        EsLider = true;
    }

    public void QuitarLiderazgo()
    {
        EsLider = false;
    }
}
```

- [ ] **Step 2: Renombrar en `InvitacionEquipo.cs`**

Solo cambian las dos propiedades, la factory y sus guardas. El resto del archivo (`Aceptar`, `Rechazar`) no se toca:

```csharp
    public Guid InvitacionEquipoId { get; private set; }
    public Guid EquipoId { get; private set; }

    // Subs de OIDC, no UsuarioId local. Ver ParticipanteEquipo.SubjectId.
    public Guid InvitadoSubjectId { get; private set; }
    public Guid InvitadoPorSubjectId { get; private set; }
    public EstadoInvitacion Estado { get; private set; }
    public DateTime FechaCreacionUtc { get; private set; }

    private InvitacionEquipo() { }

    public static InvitacionEquipo Crear(Guid equipoId, Guid invitadoSubjectId, Guid invitadoPorSubjectId)
    {
        if (equipoId == Guid.Empty) throw new ArgumentException("EquipoId requerido", nameof(equipoId));
        if (invitadoSubjectId == Guid.Empty) throw new ArgumentException("InvitadoSubjectId requerido", nameof(invitadoSubjectId));
        if (invitadoPorSubjectId == Guid.Empty) throw new ArgumentException("InvitadoPorSubjectId requerido", nameof(invitadoPorSubjectId));

        return new InvitacionEquipo
        {
            InvitacionEquipoId = Guid.NewGuid(),
            EquipoId = equipoId,
            InvitadoSubjectId = invitadoSubjectId,
            InvitadoPorSubjectId = invitadoPorSubjectId,
            Estado = EstadoInvitacion.Pendiente,
            FechaCreacionUtc = DateTime.UtcNow,
        };
    }
```

- [ ] **Step 3: Renombrar en `HistorialNombreEquipo.cs`**

```csharp
namespace Umbral.IdentityService.Domain.Entities;

public sealed class HistorialNombreEquipo
{
    public Guid Id { get; private set; }

    // Sub de OIDC, no UsuarioId local. Ver ParticipanteEquipo.SubjectId.
    public Guid SubjectId { get; private set; }
    public Guid EquipoId { get; private set; }
    public string NombreEquipo { get; private set; }
    public DateTime FechaRegistroUtc { get; private set; }

    private HistorialNombreEquipo()
    {
        NombreEquipo = string.Empty;
    }

    private HistorialNombreEquipo(Guid subjectId, Guid equipoId, string nombreEquipo, DateTime fechaUtc)
    {
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId requerido", nameof(subjectId));
        if (equipoId == Guid.Empty) throw new ArgumentException("EquipoId requerido", nameof(equipoId));
        if (string.IsNullOrWhiteSpace(nombreEquipo)) throw new ArgumentException("NombreEquipo requerido", nameof(nombreEquipo));

        Id = Guid.NewGuid();
        SubjectId = subjectId;
        EquipoId = equipoId;
        NombreEquipo = nombreEquipo.Trim();
        FechaRegistroUtc = fechaUtc;
    }

    public static HistorialNombreEquipo Registrar(Guid subjectId, Guid equipoId, string nombreEquipo, DateTime fechaUtc)
        => new(subjectId, equipoId, nombreEquipo, fechaUtc);
}
```

- [ ] **Step 4: Compilar y dejar que el compilador liste el trabajo**

Run: `dotnet build "services/identity-service/Umbral.IdentityService.sln" --nologo`

Expected: FAIL con decenas de `error CS1061: 'ParticipanteEquipo' does not contain a definition for 'UsuarioId'` y equivalentes. **Ese es el rojo esperado**, no una señal de que algo va mal.

Anotar la lista de archivos del output: es la lista de trabajo del Step 5.

- [ ] **Step 5: Arreglar los llamadores hasta que compile**

Regla de sustitución, aplicada **por tipo del receptor, no por texto**:

| Sobre un… | Hoy | Queda |
|---|---|---|
| `ParticipanteEquipo` | `.UsuarioId` | `.SubjectId` |
| `InvitacionEquipo` | `.InvitadoUserId` | `.InvitadoSubjectId` |
| `InvitacionEquipo` | `.InvitadoPorUserId` | `.InvitadoPorSubjectId` |
| `HistorialNombreEquipo` | `.UsuarioId` | `.SubjectId` |
| `Usuario` | `.UsuarioId` | **no se toca** (es el id local; Task 2) |

**No** renombrar `request.ActorUserId`, `liderUserId`, `nuevoLiderUserId` ni variables locales de handlers: son parámetros, no estado, y quedan fuera por decisión del spec.

**Tampoco** los nombres de métodos de repositorio que reciben el sub — p. ej.
`IHistorialNombreEquipoRepository.GetByUsuarioAsync(Guid usuario, …)`. Engañan igual que las
propiedades, pero no son estado y **siguen compilando** tras el renombrado (toman `Guid`), así que
el compilador no los fuerza. Quedan fuera por el mismo criterio del spec; anotarlos como deuda en la
fila de trazabilidad de la Task 3.

`Equipo.cs` concentra la mayor parte (64 sitios de `.UsuarioId` sobre participantes). Sus métodos públicos que reciben el sub renombran también el parámetro — p. ej. `AgregarParticipante(Guid subjectId)`, `CrearPorParticipante(string nombreEquipo, Guid creadorSubjectId)` — porque son la API de la entidad.

Repetir hasta que `dotnet build` pase.

- [ ] **Step 6: Actualizar el mapeo EF**

En `IdentityDbContext.cs`, tres bloques. `ParticipanteEquipo` (líneas 48-59):

```csharp
            entity.Property(x => x.SubjectId).HasColumnName("subjectid").IsRequired();
            entity.Property(x => x.FechaUnionUtc).HasColumnName("fechaunionutc").IsRequired();
            entity.Property(x => x.EsLider).HasColumnName("eslider").IsRequired();
            entity.HasIndex(x => x.SubjectId)
                .HasDatabaseName("ux_equipos_participantes_subjectid")
                .IsUnique();
```

`InvitacionEquipo` (líneas 61-77):

```csharp
            entity.Property(x => x.InvitadoSubjectId).HasColumnName("invitadosubjectid").IsRequired();
            entity.Property(x => x.InvitadoPorSubjectId).HasColumnName("invitadoporsubjectid").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.FechaCreacionUtc).HasColumnName("fechacreacionutc").IsRequired();
            entity.HasIndex(x => x.InvitadoSubjectId)
                .HasDatabaseName("ix_invitaciones_equipo_invitadosubjectid");
```

`HistorialNombreEquipo` (líneas 87-97):

```csharp
            entity.Property(x => x.SubjectId).HasColumnName("subjectid").IsRequired();
```
```csharp
            entity.HasIndex(x => x.SubjectId).HasDatabaseName("ix_historial_nombre_equipo_subjectid");
```

Los índices se renombran junto a la columna: un índice llamado `ux_equipos_participantes_usuarioid` sobre una columna `subjectid` es la misma mentira, mudada de sitio.

- [ ] **Step 7: Generar la migración**

Run:

```bash
dotnet ef migrations add RenombrarSubjectIdEnEquipos \
  --project services/identity-service/src/Umbral.IdentityService.Infrastructure \
  --startup-project services/identity-service/src/Umbral.IdentityService.Api
```

Verificar que la migración generada usa `RenameColumn`/`RenameIndex` y **no** `DropColumn`+`AddColumn`. Si EF generó drop+add, los datos se perderían: corregir a mano a esto:

```csharp
            migrationBuilder.RenameColumn(name: "usuarioid", table: "equipos_participantes", newName: "subjectid");
            migrationBuilder.RenameIndex(name: "ux_equipos_participantes_usuarioid", table: "equipos_participantes", newName: "ux_equipos_participantes_subjectid");
            migrationBuilder.RenameColumn(name: "usuarioid", table: "historial_nombre_equipo", newName: "subjectid");
            migrationBuilder.RenameIndex(name: "ix_historial_nombre_equipo_usuarioid", table: "historial_nombre_equipo", newName: "ix_historial_nombre_equipo_subjectid");
            migrationBuilder.RenameColumn(name: "invitadouserid", table: "invitaciones_equipo", newName: "invitadosubjectid");
            migrationBuilder.RenameColumn(name: "invitadoporuserid", table: "invitaciones_equipo", newName: "invitadoporsubjectid");
            migrationBuilder.RenameIndex(name: "ix_invitaciones_equipo_invitadouserid", table: "invitaciones_equipo", newName: "ix_invitaciones_equipo_invitadosubjectid");
```

`RenameColumn` preserva los datos y no hace falta backfill.

- [ ] **Step 8: Arreglar los cinco comentarios que citan el nombre viejo**

En `src/…/Handlers/Queries/GetParticipantesElegiblesQueryHandler.cs:43`, sustituir
`// ParticipanteEquipo.UsuarioId guarda el sub, y con el sub llega el actor en el token.` por:

```csharp
        // ParticipanteEquipo.SubjectId guarda el sub, y con el sub llega el actor en el token.
```

En `src/…/Handlers/Queries/ListarEquiposQueryHandler.cs:25`, sustituir
`// Los miembros de equipo (ParticipanteEquipo.UsuarioId) guardan el sub de Keycloak,` por:

```csharp
        // Los miembros de equipo (ParticipanteEquipo.SubjectId) guardan el sub de Keycloak,
```

Lo mismo en `tests/…/Teams/Invitations/GetParticipantesElegiblesHandlerTests.cs:59` y
`tests/…/Teams/ListarEquiposQueryHandlerTests.cs:61`: cambiar `ParticipanteEquipo.UsuarioId` por
`ParticipanteEquipo.SubjectId` en el texto del comentario.

En `contracts/http/identity-api.md:76`, sustituir
`` `ParticipanteEquipo.UsuarioId` guarda el sub pese a su nombre, `` por:

```markdown
> `ParticipanteEquipo.SubjectId` lo dice en su nombre desde el slice del 2026-07-16,
```

- [ ] **Step 9: Correr la suite y verificar verde**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --nologo`

Expected: PASS, 0 fallos, **345 tests**. Un renombrado no cambia comportamiento: si algún test cambia de resultado, el renombrado tocó algo que no debía — parar e investigar.

En particular, los tres tests de regresión del bug de invitaciones (`GetElegibles_Devuelve_El_Sub_De_Keycloak_Y_No_El_UsuarioId_Local`, `GetElegibles_Excluye_Al_Propio_Lider`, `GetElegibles_Excluye_A_Quien_Ya_Tiene_Equipo_Por_Su_Sub`, de `dfc56c7`) deben seguir verdes **sin tocar sus aserciones**. Solo cambia el comentario del Step 8.

- [ ] **Step 10: Verificar la migración contra la base real (manual)**

**Ningún test ejercita las migraciones** — este paso es su única verificación.

```bash
docker compose -f "infra/docker-compose.yml" up -d postgres
dotnet ef database update \
  --project services/identity-service/src/Umbral.IdentityService.Infrastructure \
  --startup-project services/identity-service/src/Umbral.IdentityService.Api
docker exec -it umbral-postgres psql -U umbral -d umbral_identity -c "\d equipos_participantes"
docker exec -it umbral-postgres psql -U umbral -d umbral_identity -c "\d invitaciones_equipo"
docker exec -it umbral-postgres psql -U umbral -d umbral_identity -c "\d historial_nombre_equipo"
```

Expected: las columnas se llaman `subjectid`, `invitadosubjectid`, `invitadoporsubjectid`; los índices `ux_equipos_participantes_subjectid`, `ix_invitaciones_equipo_invitadosubjectid`, `ix_historial_nombre_equipo_subjectid`. Y **los datos siguen ahí**: `SELECT count(*) FROM equipos_participantes;` debe dar 3.

- [ ] **Step 11: Commit**

```bash
git add services/identity-service/src services/identity-service/tests contracts/http/identity-api.md
git commit -m "refactor(identity): el estado del mundo de equipos se llama SubjectId

Las columnas y propiedades se llamaban UsuarioId pero guardan el sub de OIDC,
no el UsuarioId local: dos Guid sin relacion con el mismo nombre. De ahi salio
el bug de las invitaciones invisibles (dfc56c7).

SubjectId y no SubKeycloak: sub es el termino del estandar OIDC, asi que el
nombre sigue siendo cierto si algun dia se cambia de proveedor.

Los indices se renombran con sus columnas: uno llamado _usuarioid sobre una
columna subjectid seria la misma mentira mudada de sitio.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: `UsuarioLocalId` — que la fuga no compile

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/ValueObjects/UsuarioLocalId.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Domain/Entities/Usuario.cs:8,15,31`
- Modify: `services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IUsuarioRepository.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/IdentityDbContext.cs:22-33`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/UsuarioRepository.cs:27,49`
- Modify: los 11 sitios que consumen `Usuario.UsuarioId` (el compilador los enumera)
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Domain/UsuarioLocalIdTests.cs` (crear)

**Interfaces:**
- Consumes: nada de Task 1 (son independientes).
- Produces: `UsuarioLocalId` (`readonly record struct`) con `UsuarioLocalId.New()`, `UsuarioLocalId.From(Guid)`, y la propiedad `Valor` (`Guid`).
- Produces: `Usuario.UsuarioId` de tipo `UsuarioLocalId`.
- Produces: `IUsuarioRepository.GetByIdAsync(UsuarioLocalId userId, CancellationToken)` y `ExistsByEmailAsync(string email, UsuarioLocalId? excludingUserId, CancellationToken)`.

- [ ] **Step 1: Escribir el test del value object**

Crear `services/identity-service/tests/Umbral.IdentityService.UnitTests/Domain/UsuarioLocalIdTests.cs`:

```csharp
using Umbral.IdentityService.Domain.ValueObjects;

namespace Umbral.IdentityService.UnitTests.Domain;

public sealed class UsuarioLocalIdTests
{
    [Fact]
    public void From_conserva_el_valor()
    {
        var guid = Guid.NewGuid();

        var id = UsuarioLocalId.From(guid);

        Assert.Equal(guid, id.Valor);
    }

    [Fact]
    public void New_genera_valores_distintos()
    {
        Assert.NotEqual(UsuarioLocalId.New(), UsuarioLocalId.New());
    }

    [Fact]
    public void Dos_ids_con_el_mismo_valor_son_iguales()
    {
        var guid = Guid.NewGuid();

        // record struct: igualdad por valor. Sin esto, un id no serviria como clave ni se
        // podria comparar, y los repositorios lo necesitan.
        Assert.Equal(UsuarioLocalId.From(guid), UsuarioLocalId.From(guid));
    }
}
```

La garantía de verdad de este task —que `UsuarioLocalId` no se pueda pasar donde se espera un `Guid`— **no se puede escribir como test xUnit**: es una propiedad del compilador, y se ejerce en cada build. Estos tests solo fijan el comportamiento del tipo.

- [ ] **Step 2: Correr el test y verificar que falla**

Run: `dotnet build "services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj" --nologo`

Expected: FAIL con `error CS0246: The type or namespace name 'UsuarioLocalId' could not be found`.

- [ ] **Step 3: Crear el value object**

Crear `services/identity-service/src/Umbral.IdentityService.Domain/ValueObjects/UsuarioLocalId.cs`. La carpeta `ValueObjects/` no existe todavía en este servicio; el patrón es el de `services/partidas/src/Umbral.Partidas.Domain/ValueObjects/PartidaId.cs`:

```csharp
namespace Umbral.IdentityService.Domain.ValueObjects;

/// <summary>
/// El id local del usuario, generado por UMBRAL. No confundir con el sub de OIDC
/// (<c>Usuario.KeycloakId</c>), que es el id con el que el actor llega en el token y con el que se
/// indexa el mundo de equipos (<c>ParticipanteEquipo.SubjectId</c>). Son dos Guid sin relacion
/// entre si: este tipo existe para que mezclarlos no compile.
/// </summary>
public readonly record struct UsuarioLocalId(Guid Valor)
{
    public static UsuarioLocalId New() => new(Guid.NewGuid());
    public static UsuarioLocalId From(Guid valor) => new(valor);
}
```

- [ ] **Step 4: Correr el test y verificar que pasa**

Run: `dotnet test "services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj" --nologo --filter "FullyQualifiedName~UsuarioLocalIdTests"`

Expected: PASS, 3 tests.

- [ ] **Step 5: Tipar `Usuario.UsuarioId`**

En `Usuario.cs`, añadir `using Umbral.IdentityService.Domain.ValueObjects;` y cambiar tres sitios. La propiedad (línea 8):

```csharp
    public UsuarioLocalId UsuarioId { get; private set; }
```

El ctor privado (línea 15):

```csharp
    private Usuario(UsuarioLocalId usuarioId, string keycloakId, string nombre, string correo, RolUsuario rol)
```

La factory (línea 31):

```csharp
        return new Usuario(UsuarioLocalId.New(), keycloakId.Trim(), nombre.Trim(), correo.Trim().ToLowerInvariant(), rol);
```

- [ ] **Step 6: Compilar y dejar que el compilador liste el trabajo**

Run: `dotnet build "services/identity-service/Umbral.IdentityService.sln" --nologo`

Expected: FAIL. Los 11 sitios de producción que consumen `Usuario.UsuarioId` dejan de compilar, más sus tests. **Ese es el rojo esperado.**

Los 11: `CambiarRolUsuarioCommandHandler` (4), `UsuarioRepository` (2), y uno en cada uno de `CreateUserWithInitialRoleCommandHandler`, `DeactivateUserCommandHandler`, `UpdateUserGeneralDataCommandHandler`, `GetUserByIdQueryHandler`, `GetUsersQueryHandler`. Todos son flujos de administración de usuarios, donde el id local es el **correcto**.

- [ ] **Step 7: Desenvolver en el borde**

Regla: los DTOs y los integration events siguen llevando `Guid` (constraint global), así que cada sitio desenvuelve con `.Valor`. Ejemplos exactos:

`CambiarRolUsuarioCommandHandler.cs:43`:
```csharp
            return new CambiarRolUsuarioResponse(usuario.UsuarioId.Valor, rolAnterior.ToString());
```

`CambiarRolUsuarioCommandHandler.cs:57`:
```csharp
            throw new UsuarioConEquipoActivoException(usuario.UsuarioId.Valor);
```

`CambiarRolUsuarioCommandHandler.cs:66`:
```csharp
            new RolUsuarioModificadoIntegrationEvent(usuario.UsuarioId.Valor, rolAnterior.ToString(), rolNuevo.ToString(),
```

`CambiarRolUsuarioCommandHandler.cs:70`:
```csharp
        return new CambiarRolUsuarioResponse(usuario.UsuarioId.Valor, rolNuevo.ToString());
```

`DeactivateUserCommandHandler.cs:29`:
```csharp
        return new DeactivateUserResponse(user.UsuarioId.Valor, user.Estado.ToString());
```

En `CreateUserWithInitialRoleCommandHandler.cs:78`, `UpdateUserGeneralDataCommandHandler.cs:84`,
`GetUserByIdQueryHandler.cs:26` y `GetUsersQueryHandler.cs:22`, el `usuario.UsuarioId` que se pasa
al constructor del DTO pasa a `usuario.UsuarioId.Valor` (en `GetUsersQueryHandler` el receptor se
llama `user`).

- [ ] **Step 8: Tipar el repositorio**

En `IUsuarioRepository.cs`, añadir `using Umbral.IdentityService.Domain.ValueObjects;` y cambiar dos firmas:

```csharp
    Task<Usuario?> GetByIdAsync(UsuarioLocalId userId, CancellationToken cancellationToken);
```
```csharp
    Task<bool> ExistsByEmailAsync(string email, UsuarioLocalId? excludingUserId, CancellationToken cancellationToken);
```

El doc comment de `GetByKeycloakIdAsync` explica hoy los dos espacios y cita el nombre viejo. Sustituirlo por:

```csharp
    /// <summary>
    /// Busca un usuario por su sub de OIDC (el `sub` del JWT). A diferencia de
    /// <see cref="GetByIdAsync"/> (que busca por el <c>UsuarioLocalId</c>), este método debe
    /// usarse siempre que el id disponible provenga del token o del mundo de equipos, que se
    /// indexa por sub (<c>ParticipanteEquipo.SubjectId</c>). Los dos espacios de id no se mezclan:
    /// por eso el local es un tipo propio y este toma un Guid crudo.
    /// </summary>
    Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken);
```

En `UsuarioRepository.cs`, las dos consultas comparan value objects, que EF traduce vía el converter del Step 9. Línea 27:

```csharp
        return _dbContext.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == userId, cancellationToken);
```
(no cambia el texto: cambia el tipo de `userId`, y ahora sí es type-safe)

Línea 49:
```csharp
            query = query.Where(u => u.UsuarioId != excludingUserId.Value);
```
(idem)

Los llamadores de `GetByIdAsync` reciben un `Guid` de la ruta HTTP: envuelven con `UsuarioLocalId.From(...)` en el handler, que es el borde. El compilador los señala.

- [ ] **Step 9: Registrar el `ValueConverter` en EF**

En `IdentityDbContext.cs`, añadir los `using`:

```csharp
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Umbral.IdentityService.Domain.ValueObjects;
```

Declarar el converter dentro de la clase, antes de `OnModelCreating` (patrón de `PartidasDbContext.cs:19-20`):

```csharp
    private static readonly ValueConverter<UsuarioLocalId, Guid> UsuarioLocalIdConverter =
        new(v => v.Valor, v => UsuarioLocalId.From(v));
```

Y aplicarlo en el mapeo de `Usuario` (línea 26):

```csharp
            entity.Property(x => x.UsuarioId).HasColumnName("usuarioid").HasConversion(UsuarioLocalIdConverter);
```

`HasKey(x => x.UsuarioId)` (línea 25) no cambia: EF soporta converters en claves primarias.

**No hace falta migración**: la columna sigue siendo `uuid` y el valor sigue siendo el mismo Guid. Si `dotnet ef migrations add` generara algo, es señal de que el mapeo quedó mal — parar y revisar.

- [ ] **Step 10: Añadir el test del mapeo**

No existe un archivo de persistencia de `Usuario`: crear
`services/identity-service/tests/Umbral.IdentityService.IntegrationTests/UsuarioPersistenceTests.cs`.

El test **asevera el modelo de EF, no un round-trip de datos**. Motivo: todo el suite de persistencia
de este servicio usa `UseInMemoryDatabase`, y el proveedor InMemory no garantiza ejercer los
`ValueConverter` — guarda el objeto tal cual. Un round-trip pasaría **igual sin el converter
registrado**, y no probaría nada. Aseverar el modelo sí muerde: quitar el `HasConversion` del Step 9
lo pone en rojo. Es el mismo patrón que `Teams/InvitacionEquipoPersistenceTests.cs`, que inspecciona
`dbContext.Model` en vez de escribir filas.

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.ValueObjects;
using Umbral.IdentityService.Infrastructure.Persistence;
using Xunit;

namespace Umbral.IdentityService.IntegrationTests;

public sealed class UsuarioPersistenceTests
{
    private static IdentityDbContext NewContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"usuario-{Guid.NewGuid()}").Options);

    [Fact]
    public void UsuarioId_se_mapea_con_el_ValueConverter_a_Guid()
    {
        using var ctx = NewContext();

        var propiedad = ctx.Model.FindEntityType(typeof(Usuario))!.FindProperty(nameof(Usuario.UsuarioId));

        Assert.NotNull(propiedad);
        var converter = propiedad!.GetValueConverter();
        // Sin converter, Npgsql no sabe guardar un UsuarioLocalId y el servicio revienta al
        // primer SaveChanges contra Postgres — que ningun test toca (todos son InMemory).
        Assert.NotNull(converter);
        Assert.Equal(typeof(UsuarioLocalId), converter!.ModelClrType);
        Assert.Equal(typeof(Guid), converter.ProviderClrType);
    }

    [Fact]
    public void UsuarioId_sigue_mapeado_a_la_columna_usuarioid()
    {
        using var ctx = NewContext();

        var propiedad = ctx.Model.FindEntityType(typeof(Usuario))!.FindProperty(nameof(Usuario.UsuarioId));

        // El tipado no cambia la columna: sigue siendo uuid y por eso este task no lleva migracion.
        Assert.Equal("usuarioid", propiedad!.GetColumnName());
    }
}
```

- [ ] **Step 11: Correr la suite y verificar verde**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln" --nologo`

Expected: PASS, 0 fallos. **349 tests** (345 + 3 de `UsuarioLocalIdTests` + 1 de round-trip).

- [ ] **Step 12: Verificar que el tipo muerde**

Añadir temporalmente a cualquier handler del mundo de equipos una línea que filtre el id local, p. ej. en `GetParticipantesElegiblesQueryHandler`, dentro del bucle:

```csharp
            var fuga = usuario.UsuarioId; // deberia ser Guid para el mundo de equipos
            if (miembrosActuales.Contains(fuga)) continue;
```

Run: `dotnet build "services/identity-service/Umbral.IdentityService.sln" --nologo`

Expected: FAIL con `error CS1503` / `CS1929` — `UsuarioLocalId` no se convierte a `Guid`. **Ese error es el entregable de este task**: es exactamente el bug de `dfc56c7`, y ahora no compila.

Quitar la línea después.

- [ ] **Step 13: Commit**

```bash
git add services/identity-service/src services/identity-service/tests
git commit -m "refactor(identity): el id local del usuario es un tipo propio

UsuarioLocalId, readonly record struct con ValueConverter de EF (patron de
PartidaId en Partidas). Filtrarlo al mundo de equipos deja de compilar: es el
bug de dfc56c7, que hoy pasaba porque ambos ids eran Guid y el compilador no
podia distinguirlos.

El tipo vive solo dentro del dominio: los DTOs y los eventos siguen llevando
Guid, asi que ningun contrato cambia.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Cierre documental

**Files:**
- Modify: `docs/04-sdd/SPECS-LIST.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: todo lo anterior.
- Produces: nada.

- [ ] **Step 1: Verificar el alcance real del diff**

Run: `git diff --stat <sha-del-spec>^..HEAD`

Expected: **solo** `services/identity-service`, `contracts/http/identity-api.md` y `docs/`. Si aparece Operaciones, Puntuaciones, Partidas, gateway, `frontend/` o `mobile/`, se violó un constraint global — parar y entender por qué.

- [ ] **Step 2: Añadir la fila a `SPECS-LIST.md`**

Al final de la tabla:

```markdown
| Tipado del id local y renombrado del subject (refactor preventivo) | Identity | backend | — | docs/superpowers/specs/2026-07-16-id-competidor-tipado-design.md | Implemented (3 tasks). No introduce HU ni cambia reglas. Secuela estructural del bug de invitaciones invisibles (`dfc56c7`): dos Guid sin relación —`Usuario.UsuarioId` local y el sub de OIDC— se llamaban igual, y el compilador no podía distinguirlos. Tipa el local (`UsuarioLocalId`, patrón `PartidaId`) para que filtrarlo al mundo de equipos no compile, y renombra el estado persistido de ese mundo a `SubjectId` (`SubjectId`/`InvitadoSubjectId`/`InvitadoPorSubjectId`), columnas e índices incluidos. **Cero cambios de contrato**: el tipo vive solo en el dominio y los DTOs desenvuelven en el borde. **Fuera de alcance:** parámetros locales de handlers (`request.ActorUserId`) — ~400 sitios de churn que no engañan al lector; y el lock-in (el sub sigue regado en tres bases), que necesita ADR propio. |
```

- [ ] **Step 3: Añadir la fila a `traceability-matrix.md`**

Al final de la tabla, con los conteos reales de la suite (línea base 345 = 249 unit + 49 integration + 47 contract; final esperado 349), la verificación del Step 12 de Task 2 (la fuga no compila), y las limitaciones declaradas del spec: el tipado **no es una garantía sino fricción** (nadie impide un `.Valor` distraído), y **ningún test cubre las migraciones** (el renombrado de columnas se verificó a mano contra Postgres en Task 1 Step 10).

- [ ] **Step 4: Commit**

```bash
git add docs/04-sdd/SPECS-LIST.md docs/04-sdd/traceability-matrix.md
git commit -m "docs: trazabilidad del tipado del id local

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

> **Aviso para quien commitee la Task 3:** `SPECS-LIST.md` y `traceability-matrix.md` pueden tener filas sin commitear de otro slice (el de nombres de partida). Si `git status` las muestra modificadas antes de empezar, stagear **solo** las filas de este slice — `git add -p` no basta si las filas son contiguas; usar `git add -e` o construir el índice con plumbing.

---

## Verificación final

- `dotnet test "services/identity-service/Umbral.IdentityService.sln" --nologo` verde, 349 tests.
- La migración de renombrado aplicada contra Postgres y las 3 tablas inspeccionadas con `\d`, **con los datos intactos** (Task 1 Step 10) — ningún test cubre esto.
- La fuga del id local no compila (Task 2 Step 12).
- `git diff --stat` solo toca `services/identity-service`, `contracts/http/identity-api.md` y `docs/`.
- Prueba manual de humo: crear un equipo e invitar a un participante sin equipo desde el móvil, y confirmar que la invitación le aparece — el flujo que arregló `dfc56c7` debe seguir funcionando.
