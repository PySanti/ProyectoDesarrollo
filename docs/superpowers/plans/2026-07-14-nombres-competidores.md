# Nombres de competidores en pantallas de operador y participante — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que operador y participante lean nombres reales de participantes y equipos donde hoy solo ven GUIDs.

**Architecture:** Un endpoint nuevo `POST /identity/directory/names` en Identity resuelve lotes de ids a nombres. Web y móvil lo consumen a través de un hook `useNombres` con caché incremental a nivel de módulo, que cae al GUID corto cuando la resolución falla. Ningún evento, contrato de Puntuaciones ni proyección se toca.

**Tech Stack:** .NET 8 (Clean Architecture + MediatR + FluentValidation + xUnit), React 18 + Vite + TypeScript + vitest, React Native + Expo + `node --test`.

**Spec:** `docs/superpowers/specs/2026-07-14-nombres-competidores-design.md`

## Global Constraints

- **Nombre real completo** (`Usuario.Nombre`). No introducir alias ni `NombreVisible`.
- **Nombre actual siempre.** No leer ni escribir historial de nombres de equipo (BR-E11) en este slice.
- **La resolución de nombres nunca rompe la operación.** Todo fallo de red o de Identity degrada a GUID corto, sin error visible ni pantalla bloqueada.
- **Tope de lote: 200 ids por request**, contando `participanteIds` + `equipoIds` **sumados**.
- **Fallback visual: `id.slice(0, 8)`**, encapsulado dentro del hook. Ninguna pantalla maneja el caso de fallo.
- **Un id que no resuelve se omite de la respuesta** (no se devuelve `""`).
- **No tocar `data-testid`, `label` ni roles ARIA** existentes (regla de rediseño, CLAUDE.md). Cambiar el texto pintado sí es esperado.
- **Espacio de ids:** los participantes se resuelven por **sub de Keycloak** contra `Usuario.KeycloakId` (string, parseado a `Guid`), no por `Usuario.UsuarioId`. Los equipos por `Equipo.EquipoId`.
- **Fuera de alcance:** nombres de partida y de juego (`RendimientoEquipoPage.tsx:118`, `ConvocatoriasScreen.tsx:78`, `HistorialPartidaPage.tsx:137`). Son del servicio Partidas.

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `services/identity-service/src/Umbral.IdentityService.Application/Queries/ResolverNombresQuery.cs` | la query |
| `.../Application/DTOs/NombresResponse.cs` | forma de la respuesta |
| `.../Application/Handlers/Queries/ResolverNombresQueryHandler.cs` | resolución contra repos |
| `.../Application/Validators/ResolverNombresQueryValidator.cs` | tope de 200 ids |
| `.../Api/Contracts/DirectoryRequests.cs` | request HTTP |
| `.../Api/Controllers/DirectoryController.cs` | endpoint |
| `frontend/src/api/directoryApi.ts` | solo el fetch |
| `frontend/src/features/shared/useNombres.ts` | caché incremental + fallback |
| `mobile/src/features/shared/directoryApi.js` | solo el fetch |
| `mobile/src/features/shared/useNombres.js` | caché incremental + fallback |

---

### Task 1: Query, DTOs y handler de resolución (Identity Application)

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Queries/ResolverNombresQuery.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/DTOs/NombresResponse.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/ResolverNombresQueryHandler.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Directory/ResolverNombresQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `IUsuarioRepository.GetAllAsync(CancellationToken)` y `IEquipoRepository.GetAllAsync(CancellationToken)` (ambos ya existen en `Umbral.IdentityService.Domain.Abstractions.Persistence`).
- Produces: `ResolverNombresQuery(IReadOnlyList<Guid> ParticipanteIds, IReadOnlyList<Guid> EquipoIds) : IRequest<NombresResponse>`; `NombresResponse(IReadOnlyList<NombreParticipanteResponse> Participantes, IReadOnlyList<NombreEquipoResponse> Equipos)`; `NombreParticipanteResponse(Guid ParticipanteId, string Nombre)`; `NombreEquipoResponse(Guid EquipoId, string NombreEquipo)`; `ResolverNombresQueryHandler(IUsuarioRepository, IEquipoRepository)`.

- [ ] **Step 1: Write the failing test**

Crear `services/identity-service/tests/Umbral.IdentityService.UnitTests/Directory/ResolverNombresQueryHandlerTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.IdentityService.Application.Handlers.Queries;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Directory;

public class ResolverNombresQueryHandlerTests
{
    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public List<Usuario> Usuarios = new();
        public int GetAllCalls;
        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken ct)
        {
            GetAllCalls++;
            return Task.FromResult<IReadOnlyList<Usuario>>(Usuarios);
        }
        public Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken ct) => Task.FromResult<Usuario?>(null);
        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken ct) =>
            Task.FromResult<Usuario?>(Usuarios.FirstOrDefault(u => u.KeycloakId == keycloakId.ToString()));
        public Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken ct) => Task.FromResult(false);
        public Task AddAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public List<Equipo> Equipos = new();
        public int GetAllCalls;
        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken ct)
        {
            GetAllCalls++;
            return Task.FromResult<IReadOnlyList<Equipo>>(Equipos);
        }
        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult<Equipo?>(null);
        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult(false);
        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken ct) => Task.FromResult<Equipo?>(null);
        public Task AddAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task Resuelve_participante_por_KeycloakId_y_equipo_por_EquipoId()
    {
        // El competidorId de una partida Individual viaja en el espacio del sub de
        // Keycloak, por eso el usuario se crea con KeycloakId = sub.ToString().
        var sub = Guid.NewGuid();
        var usuarios = new FakeUsuarioRepository();
        usuarios.Usuarios.Add(Usuario.Crear(sub.ToString(), "María González", "maria@umbral.test", RolUsuario.Participante));
        var equipo = Equipo.CrearPorParticipante("Los Cazadores", Guid.NewGuid());
        var equipos = new FakeEquipoRepository { Equipos = { equipo } };
        var handler = new ResolverNombresQueryHandler(usuarios, equipos);

        var result = await handler.Handle(
            new ResolverNombresQuery(new[] { sub }, new[] { equipo.EquipoId }), CancellationToken.None);

        var p = Assert.Single(result.Participantes);
        Assert.Equal(sub, p.ParticipanteId);
        Assert.Equal("María González", p.Nombre);
        var e = Assert.Single(result.Equipos);
        Assert.Equal(equipo.EquipoId, e.EquipoId);
        Assert.Equal("Los Cazadores", e.NombreEquipo);
    }

    [Fact]
    public async Task Omite_ids_desconocidos_en_vez_de_devolver_vacio()
    {
        var handler = new ResolverNombresQueryHandler(new FakeUsuarioRepository(), new FakeEquipoRepository());

        var result = await handler.Handle(
            new ResolverNombresQuery(new[] { Guid.NewGuid() }, new[] { Guid.NewGuid() }), CancellationToken.None);

        Assert.Empty(result.Participantes);
        Assert.Empty(result.Equipos);
    }

    [Fact]
    public async Task Tolera_KeycloakId_no_parseable_a_Guid()
    {
        var sub = Guid.NewGuid();
        var usuarios = new FakeUsuarioRepository();
        // Caso real: KeycloakId se persiste como string y no siempre tiene forma de Guid
        // (ver TestKeycloakIdentityPort, que devuelve Guid con formato "N").
        usuarios.Usuarios.Add(Usuario.Crear("no-es-un-guid", "Fantasma", "f@umbral.test", RolUsuario.Participante));
        usuarios.Usuarios.Add(Usuario.Crear(sub.ToString(), "Ana", "ana@umbral.test", RolUsuario.Participante));
        var handler = new ResolverNombresQueryHandler(usuarios, new FakeEquipoRepository());

        var result = await handler.Handle(
            new ResolverNombresQuery(new[] { sub }, Array.Empty<Guid>()), CancellationToken.None);

        var p = Assert.Single(result.Participantes);
        Assert.Equal("Ana", p.Nombre);
    }

    [Fact]
    public async Task Lista_vacia_no_consulta_el_repositorio()
    {
        var usuarios = new FakeUsuarioRepository();
        var equipos = new FakeEquipoRepository();
        var handler = new ResolverNombresQueryHandler(usuarios, equipos);

        var result = await handler.Handle(
            new ResolverNombresQuery(Array.Empty<Guid>(), Array.Empty<Guid>()), CancellationToken.None);

        Assert.Empty(result.Participantes);
        Assert.Empty(result.Equipos);
        Assert.Equal(0, usuarios.GetAllCalls);
        Assert.Equal(0, equipos.GetAllCalls);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj" --filter "FullyQualifiedName~ResolverNombresQueryHandlerTests"`

Expected: FAIL de compilación — `ResolverNombresQuery`, `NombresResponse` y `ResolverNombresQueryHandler` no existen.

- [ ] **Step 3: Write minimal implementation**

`Application/Queries/ResolverNombresQuery.cs`:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record ResolverNombresQuery(
    IReadOnlyList<Guid> ParticipanteIds,
    IReadOnlyList<Guid> EquipoIds) : IRequest<NombresResponse>;
```

`Application/DTOs/NombresResponse.cs`:

```csharp
namespace Umbral.IdentityService.Application.DTOs;

public sealed record NombresResponse(
    IReadOnlyList<NombreParticipanteResponse> Participantes,
    IReadOnlyList<NombreEquipoResponse> Equipos);

public sealed record NombreParticipanteResponse(Guid ParticipanteId, string Nombre);

public sealed record NombreEquipoResponse(Guid EquipoId, string NombreEquipo);
```

`Application/Handlers/Queries/ResolverNombresQueryHandler.cs`:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class ResolverNombresQueryHandler
    : IRequestHandler<ResolverNombresQuery, NombresResponse>
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IEquipoRepository _equipos;

    public ResolverNombresQueryHandler(IUsuarioRepository usuarios, IEquipoRepository equipos)
    {
        _usuarios = usuarios;
        _equipos = equipos;
    }

    public async Task<NombresResponse> Handle(
        ResolverNombresQuery request, CancellationToken cancellationToken)
    {
        var participantes = new List<NombreParticipanteResponse>();
        if (request.ParticipanteIds.Count > 0)
        {
            var pedidos = request.ParticipanteIds.ToHashSet();
            var usuarios = await _usuarios.GetAllAsync(cancellationToken);
            // Los competidores viajan en el espacio del sub de Keycloak, no del UsuarioId
            // local: el join va por KeycloakId parseado (mismo patrón que
            // ListarEquiposQueryHandler). Un KeycloakId no parseable se ignora.
            foreach (var u in usuarios)
            {
                if (Guid.TryParse(u.KeycloakId, out var sub) && pedidos.Contains(sub))
                {
                    participantes.Add(new NombreParticipanteResponse(sub, u.Nombre));
                }
            }
        }

        var equipos = new List<NombreEquipoResponse>();
        if (request.EquipoIds.Count > 0)
        {
            var pedidos = request.EquipoIds.ToHashSet();
            var todos = await _equipos.GetAllAsync(cancellationToken);
            foreach (var e in todos)
            {
                if (pedidos.Contains(e.EquipoId))
                {
                    equipos.Add(new NombreEquipoResponse(e.EquipoId, e.NombreEquipo));
                }
            }
        }

        return new NombresResponse(participantes, equipos);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj" --filter "FullyQualifiedName~ResolverNombresQueryHandlerTests"`

Expected: PASS — 4 tests.

- [ ] **Step 5: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/Queries/ResolverNombresQuery.cs services/identity-service/src/Umbral.IdentityService.Application/DTOs/NombresResponse.cs services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/ResolverNombresQueryHandler.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Directory/ResolverNombresQueryHandlerTests.cs
git commit -m "feat(identity): query y handler de resolucion de nombres de competidores"
```

---

### Task 2: Validador de tope de lote

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Validators/ResolverNombresQueryValidator.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Directory/ResolverNombresQueryValidatorTests.cs`

**Interfaces:**
- Consumes: `ResolverNombresQuery` (Task 1).
- Produces: `ResolverNombresQueryValidator : AbstractValidator<ResolverNombresQuery>` con la constante pública `ResolverNombresQueryValidator.MaxIds = 200`.

Se registra solo por `AddValidatorsFromAssembly` en `Application/DependencyInjection.cs` — **no hay que tocar ese archivo**.

- [ ] **Step 1: Write the failing test**

Crear `services/identity-service/tests/Umbral.IdentityService.UnitTests/Directory/ResolverNombresQueryValidatorTests.cs`:

```csharp
using System;
using System.Linq;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Application.Validators;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Directory;

public class ResolverNombresQueryValidatorTests
{
    private static Guid[] Ids(int n) => Enumerable.Range(0, n).Select(_ => Guid.NewGuid()).ToArray();

    [Fact]
    public void Lote_en_el_tope_exacto_es_valido()
    {
        var validator = new ResolverNombresQueryValidator();

        var result = validator.Validate(new ResolverNombresQuery(Ids(200), Array.Empty<Guid>()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void El_tope_cuenta_ambas_listas_sumadas()
    {
        var validator = new ResolverNombresQueryValidator();

        var result = validator.Validate(new ResolverNombresQuery(Ids(150), Ids(51)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Lote_vacio_es_valido()
    {
        var validator = new ResolverNombresQueryValidator();

        var result = validator.Validate(new ResolverNombresQuery(Array.Empty<Guid>(), Array.Empty<Guid>()));

        Assert.True(result.IsValid);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj" --filter "FullyQualifiedName~ResolverNombresQueryValidatorTests"`

Expected: FAIL de compilación — `ResolverNombresQueryValidator` no existe.

- [ ] **Step 3: Write minimal implementation**

`Application/Validators/ResolverNombresQueryValidator.cs`:

```csharp
using FluentValidation;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Application.Validators;

public sealed class ResolverNombresQueryValidator : AbstractValidator<ResolverNombresQuery>
{
    public const int MaxIds = 200;

    public ResolverNombresQueryValidator()
    {
        RuleFor(q => q)
            .Must(q => q.ParticipanteIds.Count + q.EquipoIds.Count <= MaxIds)
            .OverridePropertyName("ids")
            .WithMessage($"El lote no puede superar {MaxIds} ids entre participanteIds y equipoIds.");
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj" --filter "FullyQualifiedName~ResolverNombresQueryValidatorTests"`

Expected: PASS — 3 tests.

- [ ] **Step 5: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/Validators/ResolverNombresQueryValidator.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Directory/ResolverNombresQueryValidatorTests.cs
git commit -m "feat(identity): validador de tope de lote para resolucion de nombres"
```

---

### Task 3: DirectoryController

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Api/Contracts/DirectoryRequests.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/DirectoryController.cs`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/DirectoryControllerTests.cs`

**Interfaces:**
- Consumes: `ResolverNombresQuery` + `NombresResponse` (Task 1), `ResolverNombresQueryValidator` (Task 2), `FakeSender` (ya existe en `UnitTests/Api/FakeSender.cs`).
- Produces: `DirectoryController(ISender)` con `Task<IActionResult> ResolverNombres(ResolverNombresRequest, IValidator<ResolverNombresQuery>, CancellationToken)`; `ResolverNombresRequest(IReadOnlyList<Guid>? ParticipanteIds, IReadOnlyList<Guid>? EquipoIds)`.

El controlador valida en línea con `[FromServices] IValidator<T>`, igual que `AdminTeamsController`: este repo **no** tiene pipeline behavior de validación en MediatR.

- [ ] **Step 1: Write the failing test**

Crear `services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/DirectoryControllerTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Api.Controllers;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Application.Validators;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Api;

public sealed class DirectoryControllerTests
{
    private static DirectoryController NuevoController(FakeSender sender)
    {
        var controller = new DirectoryController(sender);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };
        return controller;
    }

    [Fact]
    public async Task Despacha_la_query_y_devuelve_200_con_el_payload()
    {
        var sub = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var payload = new NombresResponse(
            new List<NombreParticipanteResponse> { new(sub, "María González") },
            new List<NombreEquipoResponse> { new(equipoId, "Los Cazadores") });
        var sender = new FakeSender { NextResponse = payload };
        var controller = NuevoController(sender);

        var result = await controller.ResolverNombres(
            new ResolverNombresRequest(new[] { sub }, new[] { equipoId }),
            new ResolverNombresQueryValidator(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(payload, ok.Value);
        var query = Assert.IsType<ResolverNombresQuery>(sender.LastRequest);
        Assert.Equal(new[] { sub }, query.ParticipanteIds);
        Assert.Equal(new[] { equipoId }, query.EquipoIds);
    }

    [Fact]
    public async Task Listas_nulas_se_normalizan_a_vacias()
    {
        var sender = new FakeSender
        {
            NextResponse = new NombresResponse(
                Array.Empty<NombreParticipanteResponse>(), Array.Empty<NombreEquipoResponse>())
        };
        var controller = NuevoController(sender);

        var result = await controller.ResolverNombres(
            new ResolverNombresRequest(null, null),
            new ResolverNombresQueryValidator(),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var query = Assert.IsType<ResolverNombresQuery>(sender.LastRequest);
        Assert.Empty(query.ParticipanteIds);
        Assert.Empty(query.EquipoIds);
    }

    [Fact]
    public async Task Lote_sobre_el_tope_devuelve_400_sin_despachar()
    {
        var sender = new FakeSender();
        var controller = NuevoController(sender);
        var demasiados = new Guid[201];
        for (var i = 0; i < demasiados.Length; i++) demasiados[i] = Guid.NewGuid();

        var result = await controller.ResolverNombres(
            new ResolverNombresRequest(demasiados, null),
            new ResolverNombresQueryValidator(),
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.Null(sender.LastRequest);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj" --filter "FullyQualifiedName~DirectoryControllerTests"`

Expected: FAIL de compilación — `DirectoryController` y `ResolverNombresRequest` no existen.

- [ ] **Step 3: Write minimal implementation**

`Api/Contracts/DirectoryRequests.cs`:

```csharp
namespace Umbral.IdentityService.Api.Contracts;

public sealed record ResolverNombresRequest(
    IReadOnlyList<Guid>? ParticipanteIds,
    IReadOnlyList<Guid>? EquipoIds);
```

`Api/Controllers/DirectoryController.cs`:

```csharp
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Api.Controllers;

// Directorio de nombres para pintar competidores en las pantallas de operador y de
// participante. Vive fuera de UsersController porque ese está bajo AdminOnly y este
// endpoint debe ser alcanzable por cualquier usuario autenticado, incluido Participante
// (mismo razonamiento que TeamsAdminController con la policy aditiva GestionarEquipos).
[ApiController]
[Route("identity/directory")]
[Authorize]
public sealed class DirectoryController : ControllerBase
{
    private readonly ISender _sender;

    public DirectoryController(ISender sender) => _sender = sender;

    [HttpPost("names")]
    public async Task<IActionResult> ResolverNombres(
        [FromBody] ResolverNombresRequest request,
        [FromServices] IValidator<ResolverNombresQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new ResolverNombresQuery(
            request.ParticipanteIds ?? Array.Empty<Guid>(),
            request.EquipoIds ?? Array.Empty<Guid>());

        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

            return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
        }

        var response = await _sender.Send(query, cancellationToken);
        return Ok(response);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj" --filter "FullyQualifiedName~DirectoryControllerTests"`

Expected: PASS — 3 tests.

- [ ] **Step 5: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Api/Contracts/DirectoryRequests.cs services/identity-service/src/Umbral.IdentityService.Api/Controllers/DirectoryController.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/DirectoryControllerTests.cs
git commit -m "feat(identity): endpoint POST /identity/directory/names"
```

---

### Task 4: Contract tests — matriz de autorización y forma de la respuesta

**Files:**
- Create: `services/identity-service/tests/Umbral.IdentityService.ContractTests/DirectoryContractTests.cs`

**Interfaces:**
- Consumes: `IdentityApiFactory.CreateClientAs(string role, Guid userId)` (ya existe en el proyecto ContractTests; fija los headers `X-Test-Role` y `X-Test-UserId`). Un cliente sin `X-Test-Role` falla la autenticación → 401.

**El test que más importa aquí es que un `Participante` reciba 200.** Es contraintuitivo en este repo, donde todo `/identity/users/**` es `AdminOnly`, y sin este test alguien puede endurecer la policy "arreglando" y romper el móvil en silencio.

- [ ] **Step 1: Write the failing test**

Crear `services/identity-service/tests/Umbral.IdentityService.ContractTests/DirectoryContractTests.cs`:

```csharp
using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Umbral.IdentityService.ContractTests;

/// <summary>
/// Contrato de POST /identity/directory/names (ver contracts/http/identity-api.md).
/// El arnés usa EF InMemory, así que no hay usuarios ni equipos sembrados: estos tests
/// cubren autorización y forma de la respuesta. La resolución real contra datos
/// persistidos vive en IntegrationTests/DirectoryEndpointIntegrationTests.
/// </summary>
public sealed class DirectoryContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public DirectoryContractTests(IdentityApiFactory factory) => _factory = factory;

    private static object CuerpoVacio() => new { participanteIds = Array.Empty<Guid>(), equipoIds = Array.Empty<Guid>() };

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var anonimo = _factory.CreateClient();

        var response = await anonimo.PostAsJsonAsync("/identity/directory/names", CuerpoVacio());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Participante")]
    [InlineData("Operador")]
    [InlineData("Administrador")]
    public async Task Cualquier_rol_autenticado_puede_resolver_nombres(string rol)
    {
        // Participante es el caso crítico: el móvil pinta el ranking en vivo con este
        // endpoint. Si alguien lo endurece a AdminOnly, este test lo atrapa.
        var client = _factory.CreateClientAs(rol, Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/identity/directory/names", CuerpoVacio());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Respuesta_tiene_participantes_y_equipos_como_arrays()
    {
        var client = _factory.CreateClientAs("Operador", Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/identity/directory/names",
            new { participanteIds = new[] { Guid.NewGuid() }, equipoIds = new[] { Guid.NewGuid() } });
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Ids desconocidos se omiten: arrays presentes y vacíos, nunca null.
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("participantes").ValueKind);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("equipos").ValueKind);
        Assert.Equal(0, doc.RootElement.GetProperty("participantes").GetArrayLength());
        Assert.Equal(0, doc.RootElement.GetProperty("equipos").GetArrayLength());
    }

    [Fact]
    public async Task Lote_sobre_el_tope_devuelve_400()
    {
        var client = _factory.CreateClientAs("Operador", Guid.NewGuid());
        var demasiados = new Guid[201];
        for (var i = 0; i < demasiados.Length; i++) demasiados[i] = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/identity/directory/names",
            new { participanteIds = demasiados, equipoIds = Array.Empty<Guid>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/identity-service/tests/Umbral.IdentityService.ContractTests/Umbral.IdentityService.ContractTests.csproj" --filter "FullyQualifiedName~DirectoryContractTests"`

Expected: FAIL — 404 en las rutas si el controlador no está descubierto, o PASS directo si Tasks 1-3 ya están mergeadas. **Si pasa a la primera, verificar que el endpoint existe de verdad** con `--filter "Sin_token_devuelve_401"` y confirmar que devuelve 401 y no 404 (ambos harían pasar un test mal escrito; este está escrito para distinguirlos porque los demás casos exigen 200).

- [ ] **Step 3: No hay implementación nueva**

El endpoint ya existe tras Task 3. Si algún test falla, el defecto está en `DirectoryController` o en su registro — corregir ahí, no debilitar el test.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/identity-service/tests/Umbral.IdentityService.ContractTests/Umbral.IdentityService.ContractTests.csproj" --filter "FullyQualifiedName~DirectoryContractTests"`

Expected: PASS — 6 tests (3 del `[Theory]`).

- [ ] **Step 5: Commit**

```bash
git add services/identity-service/tests/Umbral.IdentityService.ContractTests/DirectoryContractTests.cs
git commit -m "test(identity): contrato de POST /identity/directory/names"
```

---

### Task 5: Integration test — resolución contra datos persistidos

**Files:**
- Create: `services/identity-service/tests/Umbral.IdentityService.IntegrationTests/DirectoryEndpointIntegrationTests.cs`

**Interfaces:**
- Consumes: `IdentityApiFactory` (proyecto IntegrationTests; **no** tiene `CreateClientAs` — hay que fijar el header `X-Test-Role` a mano), `IdentityDbContext` vía `factory.Services.CreateScope()`.

Cubre lo que el contract test no puede: que un usuario y un equipo realmente persistidos se resuelvan a sus nombres pasando por HTTP → controller → handler → EF.

- [ ] **Step 1: Write the failing test**

Crear `services/identity-service/tests/Umbral.IdentityService.IntegrationTests/DirectoryEndpointIntegrationTests.cs`:

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Infrastructure.Persistence;
using Xunit;

namespace Umbral.IdentityService.IntegrationTests;

public sealed class DirectoryEndpointIntegrationTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public DirectoryEndpointIntegrationTests(IdentityApiFactory factory) => _factory = factory;

    private HttpClient ClienteComo(string rol)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", rol);
        return client;
    }

    [Fact]
    public async Task Resuelve_nombres_de_usuario_y_equipo_realmente_persistidos()
    {
        var sub = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Los Cazadores", Guid.NewGuid());
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            // KeycloakId = sub: es el espacio de ids en el que viaja competidorId.
            db.Usuarios.Add(Usuario.Crear(sub.ToString(), "María González", $"{sub}@umbral.test", RolUsuario.Participante));
            db.Equipos.Add(equipo);
            await db.SaveChangesAsync();
        }

        var response = await ClienteComo("Participante").PostAsJsonAsync("/identity/directory/names",
            new { participanteIds = new[] { sub }, equipoIds = new[] { equipo.EquipoId } });
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var participantes = doc.RootElement.GetProperty("participantes");
        Assert.Equal(1, participantes.GetArrayLength());
        Assert.Equal(sub, participantes[0].GetProperty("participanteId").GetGuid());
        Assert.Equal("María González", participantes[0].GetProperty("nombre").GetString());
        var equipos = doc.RootElement.GetProperty("equipos");
        Assert.Equal(1, equipos.GetArrayLength());
        Assert.Equal(equipo.EquipoId, equipos[0].GetProperty("equipoId").GetGuid());
        Assert.Equal("Los Cazadores", equipos[0].GetProperty("nombreEquipo").GetString());
    }

    [Fact]
    public async Task Id_inexistente_se_omite_y_el_conocido_se_resuelve()
    {
        var sub = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            db.Usuarios.Add(Usuario.Crear(sub.ToString(), "Ana", $"{sub}@umbral.test", RolUsuario.Participante));
            await db.SaveChangesAsync();
        }
        var desconocido = Guid.NewGuid();

        var response = await ClienteComo("Operador").PostAsJsonAsync("/identity/directory/names",
            new { participanteIds = new[] { sub, desconocido }, equipoIds = Array.Empty<Guid>() });
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var participantes = doc.RootElement.GetProperty("participantes");
        Assert.Equal(1, participantes.GetArrayLength());
        Assert.Equal(sub, participantes[0].GetProperty("participanteId").GetGuid());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/identity-service/tests/Umbral.IdentityService.IntegrationTests/Umbral.IdentityService.IntegrationTests.csproj" --filter "FullyQualifiedName~DirectoryEndpointIntegrationTests"`

Expected: PASS si Tasks 1-3 están mergeadas. Si falla por `db.Equipos` inexistente, verificar el nombre real del `DbSet` en `IdentityDbContext` y ajustar el test (no la implementación).

- [ ] **Step 3: No hay implementación nueva**

Igual que Task 4: cualquier fallo es defecto de Tasks 1-3.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/identity-service/tests/Umbral.IdentityService.IntegrationTests/Umbral.IdentityService.IntegrationTests.csproj" --filter "FullyQualifiedName~DirectoryEndpointIntegrationTests"`

Expected: PASS — 2 tests.

- [ ] **Step 5: Run the full identity solution and commit**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln"`
Expected: PASS — sin regresiones.

```bash
git add services/identity-service/tests/Umbral.IdentityService.IntegrationTests/DirectoryEndpointIntegrationTests.cs
git commit -m "test(identity): integracion del directorio de nombres contra datos persistidos"
```

---

### Task 6: Cliente HTTP del directorio (web)

**Files:**
- Create: `frontend/src/api/directoryApi.ts`
- Test: `frontend/src/api/directoryApi.test.ts`

**Interfaces:**
- Consumes: `IdentityApiError` de `./identityApi` (reexportado, igual que hace `adminTeamsApi.ts`).
- Produces: `resolverNombres(payload: ResolverNombresPayload, accessToken: string, fetchImpl?: typeof fetch): Promise<NombresResponse>`; `interface ResolverNombresPayload { participanteIds: string[]; equipoIds: string[] }`; `interface NombresResponse { participantes: { participanteId: string; nombre: string }[]; equipos: { equipoId: string; nombreEquipo: string }[] }`.

- [ ] **Step 1: Write the failing test**

Crear `frontend/src/api/directoryApi.test.ts`:

```ts
import { describe, expect, it, vi } from "vitest";
import { IdentityApiError, resolverNombres } from "./directoryApi";

const SUB = "abcdef12-0000-0000-0000-000000000000";

function fakeFetch(status: number, body: unknown) {
  return vi.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body)
  } as unknown as Response);
}

describe("resolverNombres", () => {
  it("hace POST al gateway con el cuerpo y el token", async () => {
    const fetchImpl = fakeFetch(200, { participantes: [{ participanteId: SUB, nombre: "María González" }], equipos: [] });

    const result = await resolverNombres({ participanteIds: [SUB], equipoIds: [] }, "tok", fetchImpl);

    expect(result.participantes[0].nombre).toBe("María González");
    const [url, init] = fetchImpl.mock.calls[0];
    expect(url).toContain("/identity/directory/names");
    expect(init.method).toBe("POST");
    expect(JSON.parse(init.body)).toEqual({ participanteIds: [SUB], equipoIds: [] });
    expect(init.headers.Authorization).toBe("Bearer tok");
  });

  it("lanza IdentityApiError con el status cuando la respuesta no es ok", async () => {
    const fetchImpl = fakeFetch(400, { message: "lote demasiado grande" });

    await expect(resolverNombres({ participanteIds: [], equipoIds: [] }, "tok", fetchImpl))
      .rejects.toMatchObject({ statusCode: 400 });
  });

  it("expone IdentityApiError para que los consumidores lo distingan", () => {
    expect(new IdentityApiError("x", 500).statusCode).toBe(500);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/api/directoryApi.test.ts`
Expected: FAIL — `Cannot find module './directoryApi'`.

- [ ] **Step 3: Write minimal implementation**

Crear `frontend/src/api/directoryApi.ts`:

```ts
// Directorio de nombres (Identity). Solo el fetch: la caché y el fallback viven en
// features/shared/useNombres.ts.
import { IdentityApiError } from "./identityApi";

export { IdentityApiError };

export interface ResolverNombresPayload {
  participanteIds: string[];
  equipoIds: string[];
}

export interface NombresResponse {
  participantes: { participanteId: string; nombre: string }[];
  equipos: { equipoId: string; nombreEquipo: string }[];
}

const baseUrl = import.meta.env.VITE_GATEWAY_BASE_URL as string | undefined;

function resolveBaseUrl(): string {
  if (!baseUrl) {
    throw new Error("Missing VITE_GATEWAY_BASE_URL environment variable.");
  }

  return baseUrl.replace(/\/$/, "");
}

export async function resolverNombres(
  payload: ResolverNombresPayload,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<NombresResponse> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/directory/names`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`
    },
    body: JSON.stringify(payload)
  });

  const body = (await response.json().catch(() => ({}))) as NombresResponse & { message?: string };
  if (!response.ok) {
    throw new IdentityApiError(body.message ?? `Identity API error. Status=${response.status}`, response.status);
  }

  return { participantes: body.participantes ?? [], equipos: body.equipos ?? [] };
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd frontend && npx vitest run src/api/directoryApi.test.ts`
Expected: PASS — 3 tests.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/directoryApi.ts frontend/src/api/directoryApi.test.ts
git commit -m "feat(web): cliente HTTP del directorio de nombres"
```

---

### Task 7: Hook `useNombres` con caché incremental (web)

**Files:**
- Create: `frontend/src/features/shared/useNombres.ts`
- Test: `frontend/src/features/shared/useNombres.test.ts`

**Interfaces:**
- Consumes: `resolverNombres`, `ResolverNombresPayload`, `NombresResponse` (Task 6).
- Produces: `useNombres(ids: { participanteIds: string[]; equipoIds: string[] }, accessToken: string): (id: string) => string`; `resetNombresCache(): void` (solo para tests).

**Esta es la única lógica con riesgo real del lado cliente.** La caché es requisito funcional, no optimización: en la sesión en vivo llegan competidores nuevos por push de SignalR, y un hook que resolviera solo al montar los dejaría como GUID para siempre. Los ids que no resuelven se cachean como `null` para no repedirlos en bucle.

- [ ] **Step 1: Write the failing test**

Crear `frontend/src/features/shared/useNombres.test.ts`:

```ts
import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { resetNombresCache, useNombres } from "./useNombres";
import * as directoryApi from "../../api/directoryApi";

const A = "aaaaaaaa-0000-0000-0000-000000000000";
const B = "bbbbbbbb-0000-0000-0000-000000000000";
const EQ = "eeeeeeee-0000-0000-0000-000000000000";

beforeEach(() => {
  resetNombresCache();
  vi.restoreAllMocks();
});

describe("useNombres", () => {
  it("resuelve nombres de participantes y equipos", async () => {
    vi.spyOn(directoryApi, "resolverNombres").mockResolvedValue({
      participantes: [{ participanteId: A, nombre: "María González" }],
      equipos: [{ equipoId: EQ, nombreEquipo: "Los Cazadores" }]
    });

    const { result } = renderHook(() => useNombres({ participanteIds: [A], equipoIds: [EQ] }, "tok"));

    await waitFor(() => expect(result.current(A)).toBe("María González"));
    expect(result.current(EQ)).toBe("Los Cazadores");
  });

  it("cae al GUID corto cuando el directorio falla, sin lanzar", async () => {
    vi.spyOn(directoryApi, "resolverNombres").mockRejectedValue(new Error("red caída"));

    const { result } = renderHook(() => useNombres({ participanteIds: [A], equipoIds: [] }, "tok"));

    await waitFor(() => expect(directoryApi.resolverNombres).toHaveBeenCalled());
    expect(result.current(A)).toBe("aaaaaaaa");
  });

  it("cae al GUID corto para un id que el directorio omite", async () => {
    vi.spyOn(directoryApi, "resolverNombres").mockResolvedValue({ participantes: [], equipos: [] });

    const { result } = renderHook(() => useNombres({ participanteIds: [A], equipoIds: [] }, "tok"));

    await waitFor(() => expect(directoryApi.resolverNombres).toHaveBeenCalled());
    expect(result.current(A)).toBe("aaaaaaaa");
  });

  it("no repide ids ya cacheados y pide solo los faltantes cuando llega uno nuevo", async () => {
    const spy = vi.spyOn(directoryApi, "resolverNombres")
      .mockResolvedValueOnce({ participantes: [{ participanteId: A, nombre: "Ana" }], equipos: [] })
      .mockResolvedValueOnce({ participantes: [{ participanteId: B, nombre: "Bruno" }], equipos: [] });

    // Primer render: solo A. Simula el estado inicial del ranking.
    const { result, rerender } = renderHook(
      ({ ids }) => useNombres(ids, "tok"),
      { initialProps: { ids: { participanteIds: [A], equipoIds: [] } } }
    );
    await waitFor(() => expect(result.current(A)).toBe("Ana"));

    // Segundo render: llega B por push de SignalR. A ya está cacheado.
    rerender({ ids: { participanteIds: [A, B], equipoIds: [] } });
    await waitFor(() => expect(result.current(B)).toBe("Bruno"));

    expect(spy).toHaveBeenCalledTimes(2);
    expect(spy.mock.calls[1][0]).toEqual({ participanteIds: [B], equipoIds: [] });
    expect(result.current(A)).toBe("Ana");
  });

  it("no llama al directorio cuando no hay ids que resolver", async () => {
    const spy = vi.spyOn(directoryApi, "resolverNombres");

    renderHook(() => useNombres({ participanteIds: [], equipoIds: [] }, "tok"));

    await waitFor(() => expect(spy).not.toHaveBeenCalled());
  });

  it("trocea en lotes de 200 contando ambas listas sumadas", async () => {
    const muchos = Array.from({ length: 250 }, (_, i) => `${String(i).padStart(8, "0")}-0000-0000-0000-000000000000`);
    const spy = vi.spyOn(directoryApi, "resolverNombres").mockResolvedValue({ participantes: [], equipos: [] });

    renderHook(() => useNombres({ participanteIds: muchos, equipoIds: [EQ] }, "tok"));

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(2));
    expect(spy.mock.calls[0][0].participanteIds).toHaveLength(200);
    expect(spy.mock.calls[0][0].equipoIds).toHaveLength(0);
    expect(spy.mock.calls[1][0].participanteIds).toHaveLength(50);
    expect(spy.mock.calls[1][0].equipoIds).toEqual([EQ]);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/shared/useNombres.test.ts`
Expected: FAIL — `Cannot find module './useNombres'`.

- [ ] **Step 3: Write minimal implementation**

Crear `frontend/src/features/shared/useNombres.ts`:

```ts
// Resolución de nombres de competidores con caché incremental a nivel de módulo.
//
// La caché es requisito funcional, no optimización: en la sesión en vivo llegan
// competidores nuevos por push de SignalR, así que el hook debe pedir solo los ids
// que aún no conoce cada vez que la lista crece.
//
// Contrato con las pantallas: nombreDe(id) SIEMPRE devuelve algo pintable. Si el
// directorio falla o el id no existe, cae al GUID corto. Ninguna pantalla maneja
// el error, porque la resolución de nombres nunca puede romper la operación.
import { useEffect, useState } from "react";
import { resolverNombres, type ResolverNombresPayload } from "../../api/directoryApi";

const MAX_LOTE = 200;

// null = id ya pedido y no resuelto (usuario dado de baja, equipo eliminado).
// Se cachea para no repedirlo en bucle.
const cache = new Map<string, string | null>();

export function resetNombresCache(): void {
  cache.clear();
}

export function nombreCorto(id: string): string {
  return id.slice(0, 8);
}

function trocear(participanteIds: string[], equipoIds: string[]): ResolverNombresPayload[] {
  const lotes: ResolverNombresPayload[] = [];
  let p = 0;
  let e = 0;

  while (p < participanteIds.length || e < equipoIds.length) {
    const loteP = participanteIds.slice(p, p + MAX_LOTE);
    const loteE = equipoIds.slice(e, e + (MAX_LOTE - loteP.length));
    lotes.push({ participanteIds: loteP, equipoIds: loteE });
    p += loteP.length;
    e += loteE.length;
  }

  return lotes;
}

export function useNombres(
  ids: { participanteIds: string[]; equipoIds: string[] },
  accessToken: string
): (id: string) => string {
  const [, setVersion] = useState(0);
  const claveParticipantes = ids.participanteIds.join(",");
  const claveEquipos = ids.equipoIds.join(",");

  useEffect(() => {
    let activo = true;
    const faltanP = ids.participanteIds.filter((id) => !cache.has(id));
    const faltanE = ids.equipoIds.filter((id) => !cache.has(id));
    if (faltanP.length === 0 && faltanE.length === 0) return;

    void (async () => {
      for (const lote of trocear(faltanP, faltanE)) {
        try {
          const respuesta = await resolverNombres(lote, accessToken);
          for (const p of respuesta.participantes) cache.set(p.participanteId, p.nombre);
          for (const e of respuesta.equipos) cache.set(e.equipoId, e.nombreEquipo);
          // Lo pedido que no volvió no existe: se marca para no repedirlo.
          for (const id of [...lote.participanteIds, ...lote.equipoIds]) {
            if (!cache.has(id)) cache.set(id, null);
          }
        } catch {
          // Degradación deliberada: la pantalla se queda con GUIDs cortos y sigue operativa.
          return;
        }
      }
      if (activo) setVersion((v) => v + 1);
    })();

    return () => {
      activo = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [claveParticipantes, claveEquipos, accessToken]);

  return (id: string) => cache.get(id) ?? nombreCorto(id);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd frontend && npx vitest run src/features/shared/useNombres.test.ts`
Expected: PASS — 6 tests.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/shared/useNombres.ts frontend/src/features/shared/useNombres.test.ts
git commit -m "feat(web): hook useNombres con cache incremental y fallback a GUID corto"
```

---

### Task 8: Sustituir GUIDs en la sesión del operador (web)

**Files:**
- Modify: `frontend/src/features/partidas/SesionOperadorPage.tsx:460-525`
- Modify: `frontend/src/features/partidas/SesionOperadorPage.test.tsx`

**Interfaces:**
- Consumes: `useNombres` (Task 7).

Cuatro sitios: lobby individual (`:462`, hoy `{participanteId}` crudo sin recortar), lobby equipos (`:483`, `{equipo.equipoId}`), inscritos individuales (`:507`, `{s.participanteId}`) e inscritos por equipo (`:523`, `{s.equipoId} ({s.miembros} miembros)`).

- [ ] **Step 1: Write the failing test**

Añadir a `frontend/src/features/partidas/SesionOperadorPage.test.tsx` (respetar el mock de `useSesionHub` que ya existe al inicio del archivo; **no** cambiar `data-testid`, `label` ni roles ARIA):

```tsx
it("pinta el nombre del participante en el lobby individual, no el GUID", async () => {
  vi.spyOn(directoryApi, "resolverNombres").mockResolvedValue({
    participantes: [{ participanteId: PARTICIPANTE_GUID, nombre: "María González" }],
    equipos: []
  });

  renderSesion({ lobby: { participantes: [PARTICIPANTE_GUID], equipos: [] } });

  expect(await screen.findByText("María González")).toBeInTheDocument();
  expect(screen.queryByText(PARTICIPANTE_GUID)).not.toBeInTheDocument();
});

it("pinta el nombre del equipo en el lobby de equipos, no el GUID", async () => {
  vi.spyOn(directoryApi, "resolverNombres").mockResolvedValue({
    participantes: [],
    equipos: [{ equipoId: EQUIPO_GUID, nombreEquipo: "Los Cazadores" }]
  });

  renderSesion({ lobby: { participantes: [], equipos: [{ equipoId: EQUIPO_GUID }] } });

  expect(await screen.findByText("Los Cazadores")).toBeInTheDocument();
});

it("mantiene el GUID corto si el directorio falla", async () => {
  vi.spyOn(directoryApi, "resolverNombres").mockRejectedValue(new Error("caído"));

  renderSesion({ lobby: { participantes: [PARTICIPANTE_GUID], equipos: [] } });

  expect(await screen.findByText(PARTICIPANTE_GUID.slice(0, 8))).toBeInTheDocument();
});
```

Añadir al inicio del archivo: `import * as directoryApi from "../../api/directoryApi";`, las constantes `PARTICIPANTE_GUID`/`EQUIPO_GUID`, y `beforeEach(() => resetNombresCache())` importando `resetNombresCache` de `../shared/useNombres`. La caché es de módulo: **sin el reset, los tests se contaminan entre sí**. Adaptar `renderSesion` a como el archivo ya construya su estado de lobby.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/SesionOperadorPage.test.tsx`
Expected: FAIL — se pinta el GUID, no "María González".

- [ ] **Step 3: Write minimal implementation**

En `SesionOperadorPage.tsx`, importar el hook e invocarlo una vez con todos los ids que la página pinta:

```tsx
import { useNombres } from "../shared/useNombres";

// ...dentro del componente, tras cargar lobby e inscritos:
const nombreDe = useNombres(
  {
    participanteIds: [
      ...(lobby?.participantes ?? []),
      ...inscritos.filter((s) => s.participanteId).map((s) => s.participanteId as string)
    ],
    equipoIds: [
      ...(lobby?.equipos ?? []).map((e) => e.equipoId),
      ...inscritos.filter((s) => s.equipoId).map((s) => s.equipoId as string)
    ]
  },
  accessToken
);
```

Sustituir los cuatro sitios:

```tsx
// :462 lobby individual
<td>{nombreDe(participanteId)}</td>

// :483 lobby equipos
<td>{nombreDe(equipo.equipoId)}</td>

// :507 inscritos individuales
<td>{nombreDe(s.participanteId)}</td>

// :523 inscritos por equipo
<td>{nombreDe(s.equipoId)} ({s.miembros} miembros)</td>
```

Adaptar los nombres de variables (`lobby`, `inscritos`, `accessToken`) a los reales del archivo.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd frontend && npx vitest run src/features/partidas/SesionOperadorPage.test.tsx`
Expected: PASS — incluidos los tests que ya existían.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/SesionOperadorPage.tsx frontend/src/features/partidas/SesionOperadorPage.test.tsx
git commit -m "feat(web): nombres de competidores en lobby e inscritos del operador"
```

---

### Task 9: Sustituir GUIDs en rankings, mapa, pistas e historial (web)

**Files:**
- Modify: `frontend/src/features/partidas/runtimeShared.tsx:56-58`
- Modify: `frontend/src/features/partidas/ConsolidadoPanel.tsx:102-105`
- Modify: `frontend/src/features/partidas/GeoMapPanel.tsx:45`
- Modify: `frontend/src/features/partidas/PistasPanel.tsx:57`
- Modify: `frontend/src/features/partidas/HistorialPartidaPage.tsx:38,138,139`
- Modify: `frontend/src/features/partidas/ConsolidadoPanel.test.tsx`, `TriviaRuntimePanel.test.tsx`, `BdtRuntimePanel.test.tsx`, `GeoMapPanel.test.tsx`, `PistasPanel.test.tsx`, `HistorialPartidaPage.test.tsx`

**Interfaces:**
- Consumes: `useNombres` (Task 7).

Los DTOs de ranking traen `tipoCompetidor: "Participante" | "Equipo"` junto a `competidorId`: úsalo para decidir a qué lista va cada id, no adivines.

`HistorialPartidaPage.tsx:137` (`juegoId`) **no se toca** — es un nombre de juego, fuera de alcance. `guidCorto` sigue existiendo para esa columna.

- [ ] **Step 1: Write the failing test**

En `ConsolidadoPanel.test.tsx` (patrón a repetir en los demás archivos, con sus propios datos):

```tsx
import * as directoryApi from "../../api/directoryApi";
import { resetNombresCache } from "../shared/useNombres";

beforeEach(() => {
  resetNombresCache();
  vi.restoreAllMocks();
});

it("pinta el nombre del competidor en vez del GUID corto", async () => {
  vi.spyOn(directoryApi, "resolverNombres").mockResolvedValue({
    participantes: [{ participanteId: "abcdef12-0000-0000-0000-000000000000", nombre: "María González" }],
    equipos: []
  });

  renderConsolidado();

  expect(await screen.findByText("María González")).toBeInTheDocument();
});

it("mantiene el GUID corto si el directorio falla", async () => {
  vi.spyOn(directoryApi, "resolverNombres").mockRejectedValue(new Error("caído"));

  renderConsolidado();

  expect(await screen.findByText("abcdef12")).toBeInTheDocument();
});
```

Los tests existentes que asertan `"abcdef12"` como resultado normal deben pasar a asertar el nombre. Es cambio de comportamiento esperado.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npx vitest run src/features/partidas/`
Expected: FAIL en los archivos tocados — se pinta el GUID corto.

- [ ] **Step 3: Write minimal implementation**

`runtimeShared.tsx` — el componente exportado se llama **`RankingView`** (lo importan `TriviaRuntimePanel.tsx:12` y `BdtRuntimePanel.tsx:11`). Recibe `nombreDe` por prop desde el panel que lo usa, para no llamar al hook dos veces:

```tsx
// añadir nombreDe a la firma de RankingView
export function RankingView({ ranking, nombreDe }: { ranking: RankingJuegoDto; nombreDe: (id: string) => string }) {
  // ...
  <td>{nombreDe(entrada.competidorId)}</td>
}
```

No tocar `aria-label="Ranking del juego"` ni `data-testid="ranking-juego"`.

En `TriviaRuntimePanel.tsx` y `BdtRuntimePanel.tsx`, calcular e inyectar:

```tsx
import { useNombres } from "../shared/useNombres";

const nombreDe = useNombres(
  {
    participanteIds: (ranking?.entradas ?? []).filter((e) => e.tipoCompetidor === "Participante").map((e) => e.competidorId),
    equipoIds: (ranking?.entradas ?? []).filter((e) => e.tipoCompetidor === "Equipo").map((e) => e.competidorId)
  },
  accessToken
);
// ...
<RankingView ranking={ranking} nombreDe={nombreDe} />
```

`ConsolidadoPanel.tsx` — mismo cálculo sobre `ranking.entradas` dentro de `ConsolidadoPanel` (no de `ConsolidadoTabla`, que no tiene el token); pasar `nombreDe` a `ConsolidadoTabla` por prop y sustituir `:105`:

```tsx
<td>{nombreDe(entrada.competidorId)}</td>
```

`GeoMapPanel.tsx:45` — solo participantes (la geolocalización BDT es siempre por persona):

```tsx
const nombreDe = useNombres({ participanteIds: ubicaciones.map((u) => u.participanteId), equipoIds: [] }, accessToken);
// ...
{nombreDe(u.participanteId)} · visto hace {hace(u.timestampUtc)}
```

`PistasPanel.tsx:57` — el panel ya tiene `opciones` (los ids del selector) y el flag `esEquipo`, así que la partición es directa:

```tsx
const nombreDe = useNombres(
  esEquipo
    ? { participanteIds: [], equipoIds: opciones }
    : { participanteIds: opciones, equipoIds: [] },
  accessToken
);
// ...
<option key={id} value={id}>{nombreDe(id)}</option>
```

No tocar `data-testid="pista-destino"` ni el `<option value="">— elige {esEquipo ? "equipo" : "participante"} —</option>`.

`HistorialPartidaPage.tsx` — sustituir solo `:138` y `:139`, dejando `:137` con `guidCorto`:

```tsx
<td>{guidCorto(e.juegoId)}</td>
<td>{e.participanteId ? nombreDe(e.participanteId) : "—"}</td>
<td>{e.equipoId ? nombreDe(e.equipoId) : "—"}</td>
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd frontend && npm test`
Expected: PASS — toda la suite web, sin regresiones.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/
git commit -m "feat(web): nombres de competidores en rankings, mapa BDT, pistas e historial"
```

---

### Task 10: Directorio y hook en móvil

**Files:**
- Create: `mobile/src/features/shared/directoryApi.js`
- Create: `mobile/src/features/shared/useNombres.js`
- Test: `mobile/tests/useNombres.test.js`

**Interfaces:**
- Consumes: `mapCommonError`, `networkError` de `../partidas/partidasPublicadasApi.js` (el patrón `{ ok, data }` que usa todo el móvil).
- Produces: `resolverNombres(apiBaseUrl, token, payload, fetchImpl?) → { ok: true, data: NombresResponse } | { ok: false, ... }`; `useNombres({ participanteIds, equipoIds }, apiBaseUrl, token) → (id) => string`; `resetNombresCache()`.

El cliente móvil **no lanza**: devuelve `{ ok: false }`. El hook trata `ok: false` igual que un throw en web — se queda con el GUID corto.

- [ ] **Step 1: Write the failing test**

Crear `mobile/tests/useNombres.test.js`. **El harness de móvil es ESM** (`node --test tests/*.test.js` con `import`, ver `mobile/tests/DeleteTeamScreenController.test.js`) y solo puede importar archivos `.js` — nunca `.tsx`. Por eso `trocear` y `nombreCorto` se exportan del `.js`:

```js
import test from "node:test";
import assert from "node:assert/strict";
import { nombreCorto, trocear } from "../src/features/shared/useNombres.js";

const A = "aaaaaaaa-0000-0000-0000-000000000000";
const EQ = "eeeeeeee-0000-0000-0000-000000000000";

test("nombreCorto recorta el GUID a 8 caracteres", () => {
  assert.strictEqual(nombreCorto(A), "aaaaaaaa");
});

test("trocear reparte en lotes de 200 contando ambas listas sumadas", () => {
  const muchos = Array.from({ length: 250 }, (_, i) => `${String(i).padStart(8, "0")}-x`);
  const lotes = trocear(muchos, [EQ]);
  assert.strictEqual(lotes.length, 2);
  assert.strictEqual(lotes[0].participanteIds.length, 200);
  assert.strictEqual(lotes[0].equipoIds.length, 0);
  assert.strictEqual(lotes[1].participanteIds.length, 50);
  assert.deepStrictEqual(lotes[1].equipoIds, [EQ]);
});

test("trocear con listas vacías no produce lotes", () => {
  assert.deepStrictEqual(trocear([], []), []);
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile && npm test`
Expected: FAIL — `Cannot find module '../src/features/shared/useNombres.js'`.

- [ ] **Step 3: Write minimal implementation**

Crear `mobile/src/features/shared/directoryApi.js`:

```js
import { mapCommonError, networkError } from "../partidas/partidasPublicadasApi.js";

export async function resolverNombres(apiBaseUrl, token, payload, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/identity/directory/names`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
      body: JSON.stringify(payload),
    });
  } catch {
    return networkError();
  }
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    return mapCommonError(response.status, body);
  }
  return { ok: true, data: { participantes: body?.participantes ?? [], equipos: body?.equipos ?? [] } };
}
```

Crear `mobile/src/features/shared/useNombres.js` — misma semántica que la versión web (caché de módulo, `null` para id pedido y no resuelto, fallback a GUID corto, troceo en 200 sumando ambas listas). `trocear` y `nombreCorto` se exportan para poder testearlos sin renderizar:

```js
import { useEffect, useState } from "react";
import { resolverNombres } from "./directoryApi.js";

const MAX_LOTE = 200;
const cache = new Map();

export function resetNombresCache() {
  cache.clear();
}

export function nombreCorto(id) {
  return id.slice(0, 8);
}

export function trocear(participanteIds, equipoIds) {
  const lotes = [];
  let p = 0;
  let e = 0;
  while (p < participanteIds.length || e < equipoIds.length) {
    const loteP = participanteIds.slice(p, p + MAX_LOTE);
    const loteE = equipoIds.slice(e, e + (MAX_LOTE - loteP.length));
    lotes.push({ participanteIds: loteP, equipoIds: loteE });
    p += loteP.length;
    e += loteE.length;
  }
  return lotes;
}

export function useNombres(ids, apiBaseUrl, token) {
  const [, setVersion] = useState(0);
  const claveParticipantes = ids.participanteIds.join(",");
  const claveEquipos = ids.equipoIds.join(",");

  useEffect(() => {
    let activo = true;
    const faltanP = ids.participanteIds.filter((id) => !cache.has(id));
    const faltanE = ids.equipoIds.filter((id) => !cache.has(id));
    if (faltanP.length === 0 && faltanE.length === 0) return;

    (async () => {
      for (const lote of trocear(faltanP, faltanE)) {
        const r = await resolverNombres(apiBaseUrl, token, lote);
        // El cliente móvil no lanza: un { ok: false } se trata igual que un fallo de red.
        // La pantalla se queda con GUIDs cortos y sigue operativa.
        if (!r.ok) return;
        for (const p of r.data.participantes) cache.set(p.participanteId, p.nombre);
        for (const eq of r.data.equipos) cache.set(eq.equipoId, eq.nombreEquipo);
        for (const id of [...lote.participanteIds, ...lote.equipoIds]) {
          if (!cache.has(id)) cache.set(id, null);
        }
      }
      if (activo) setVersion((v) => v + 1);
    })();

    return () => {
      activo = false;
    };
  }, [claveParticipantes, claveEquipos, apiBaseUrl, token]);

  return (id) => cache.get(id) ?? nombreCorto(id);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile && npm test`
Expected: PASS — 3 tests nuevos, sin regresiones.

Run: `cd mobile && npm run typecheck`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/src/features/shared/directoryApi.js mobile/src/features/shared/useNombres.js mobile/tests/useNombres.test.js
git commit -m "feat(mobile): directorio de nombres y hook useNombres"
```

---

### Task 11: Sustituir GUIDs en ranking en vivo y convocatorias (móvil)

**Files:**
- Create: `mobile/src/features/partidas/liveLabels.js`
- Modify: `mobile/src/features/partidas/liveShared.tsx:21-35`
- Modify: `mobile/src/features/partidas/PartidaLiveScreen.tsx:245-255`
- Modify: `mobile/src/features/partidas/ConvocatoriasScreen.tsx:79`
- Test: `mobile/tests/liveLabels.test.js`

**Interfaces:**
- Consumes: `useNombres` (Task 10).
- Produces: `etiquetaCompetidor(competidorId, resaltarId, nombreDe) → string` en `liveLabels.js`.

`liveShared.tsx` ya resalta la fila propia contra `resaltarId`: esa fila muestra **"Tú"** y las demás el nombre real. `ConvocatoriasScreen.tsx:78` (`partidaId`) **no se toca** — nombre de partida, fuera de alcance.

**Por qué un archivo `.js` nuevo:** el harness `node --test` no puede importar `.tsx`, así que la decisión "Tú vs nombre" se extrae a `liveLabels.js` para testearla sin renderizar React Native. Es el patrón que el repo ya usa (`DeleteTeamScreenController.js` en `.js`, las pantallas en `.tsx`).

- [ ] **Step 1: Write the failing test**

Crear `mobile/tests/liveLabels.test.js`:

```js
import test from "node:test";
import assert from "node:assert/strict";
import { etiquetaCompetidor } from "../src/features/partidas/liveLabels.js";

const A = "aaaaaaaa-0000-0000-0000-000000000000";
const B = "bbbbbbbb-0000-0000-0000-000000000000";

test("la fila propia se rotula Tú", () => {
  const nombreDe = () => "María González";
  assert.strictEqual(etiquetaCompetidor(A, A, nombreDe), "Tú");
});

test("las demás filas usan el nombre resuelto", () => {
  const nombreDe = (id) => (id === B ? "Pedro Ramírez" : "?");
  assert.strictEqual(etiquetaCompetidor(B, A, nombreDe), "Pedro Ramírez");
});

test("sin resaltarId ninguna fila es Tú", () => {
  const nombreDe = () => "Ana";
  assert.strictEqual(etiquetaCompetidor(A, undefined, nombreDe), "Ana");
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile && npm test`
Expected: FAIL — `Cannot find module '../src/features/partidas/liveLabels.js'`.

- [ ] **Step 3: Write minimal implementation**

Crear `mobile/src/features/partidas/liveLabels.js`:

```js
// Vive en .js y no dentro de liveShared.tsx porque el harness `node --test` no puede
// importar .tsx. Mismo patrón que DeleteTeamScreenController.js.
export function etiquetaCompetidor(competidorId, resaltarId, nombreDe) {
  return competidorId === resaltarId ? "Tú" : nombreDe(competidorId);
}
```

En `liveShared.tsx`, importarla y usarla en la fila (`:35`), recibiendo `nombreDe` por prop:

```tsx
import { etiquetaCompetidor } from "./liveLabels.js";

<AppText>{etiquetaCompetidor(e.competidorId, resaltarId, nombreDe)}</AppText>
```

En `PartidaLiveScreen.tsx`, calcular `nombreDe` a partir de las entradas del ranking (particionando por `tipoCompetidor`, igual que en web) y pasarlo a la tabla.

En `ConvocatoriasScreen.tsx:79`:

```tsx
<AppText>Equipo {nombreDe(c.equipoId)}</AppText>
```

con `const nombreDe = useNombres({ participanteIds: [], equipoIds: convocatorias.map((c) => c.equipoId) }, apiBaseUrl, token);`

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile && npm test`
Expected: PASS.

Run: `cd mobile && npm run typecheck`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add mobile/src/features/partidas/
git commit -m "feat(mobile): nombres de competidores en ranking en vivo y convocatorias"
```

---

### Task 12: Contratos y trazabilidad

**Files:**
- Modify: `contracts/http/identity-api.md`
- Modify: `contracts/http/gateway-api.md`
- Modify: `docs/04-sdd/SPECS-LIST.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

Sin tests: son documentos. El criterio de aceptación es que el contrato describa exactamente lo que Tasks 1-11 implementaron.

- [ ] **Step 1: Añadir la sección a `contracts/http/identity-api.md`**

Después de la sección "Teams listing for the web console", añadir:

```markdown
### Directorio de nombres (policy `Default` — cualquier usuario autenticado)

Resuelve lotes de ids de competidor a nombres, para que las pantallas de operador y de
participante pinten nombres en vez de GUIDs. Es el **único** endpoint de Identity alcanzable por
`Participante` fuera de `/identity/teams/**`: el móvil lo necesita para el ranking en vivo. No
usa `AdminOnly` a propósito — ver el caveat de exposición en el spec
`docs/superpowers/specs/2026-07-14-nombres-competidores-design.md`.

| Capability | Method | Path | Status | Notes |
|---|---|---|---|---|
| Resolver nombres de competidores | POST | `/identity/directory/names` | Registered | 200; body `{ participanteIds: [guid], equipoIds: [guid] }` (ambas opcionales, default `[]`); 400 si `participanteIds.length + equipoIds.length > 200`; 401 sin token |

Respuesta:

```json
{
  "participantes": [{ "participanteId": "guid", "nombre": "string" }],
  "equipos": [{ "equipoId": "guid", "nombreEquipo": "string" }]
}
```

- `participanteId` es el **sub de Keycloak** (la identidad dual slice-E del `competidorId` en
  modalidad `Individual`), resuelto contra `Usuario.KeycloakId`. `equipoId` es `Equipo.EquipoId`.
- **Un id que no resuelve se omite de la respuesta** — no se devuelve `""`. Esto difiere de
  `GET /identity/teams`, que sí usa `""`: aquí la omisión deja que el cliente caiga al GUID corto.
- Los nombres son siempre los **actuales**; este endpoint no consulta el historial de nombres de
  equipo (BR-E11).
```

- [ ] **Step 2: Añadir la fila a la matriz de `contracts/http/gateway-api.md`**

En la tabla de rutas, y en las notas explicar por qué no hay ruta explícita:

```markdown
| `/identity/directory/{**catch-all}` | 2 (heredado del catch-all) | Default (autenticado) | Identity |
```

Nota a añadir bajo la tabla:

```markdown
- `/identity/directory/names` **no tiene ruta propia**: cae en `/identity/{**catch-all}` (Order 2),
  cuya política `Default (autenticado)` es exactamente la que necesita. Las rutas de
  `Administrador` y `Participante` son Order 1 y no lo interceptan. Se lista aquí por claridad de
  la matriz, no porque exista una entrada de configuración separada.
```

- [ ] **Step 3: Añadir la fila a `docs/04-sdd/SPECS-LIST.md`**

```markdown
| Nombres de competidores en pantallas de operador y participante (refinamiento transversal) | Identity | web + mobile | Operador / Participante | docs/superpowers/specs/2026-07-14-nombres-competidores-design.md | Implemented (12 tasks). Refinamiento transversal de usabilidad sobre HU ya implementadas de lobby, ranking en vivo, consolidado, pistas, geolocalización BDT e historial — no introduce HU nueva. |
```

- [ ] **Step 4: Actualizar `docs/04-sdd/traceability-matrix.md`**

La tabla tiene 7 columnas: `Feature | Requirement | Owning service | Supporting services | SDD folder | Contracts | Status`. Añadir al final:

```markdown
| Nombres de competidores en pantallas de operador y participante (refinamiento transversal) | Las pantallas de sesión, ranking, geolocalización, pistas e historial muestran el nombre real (`Usuario.Nombre` / `Equipo.NombreEquipo`) en vez del `competidorId` crudo. Endpoint nuevo `POST /identity/directory/names` (policy Default, alcanzable por `Participante`), resolución por sub de Keycloak contra `Usuario.KeycloakId` y por `Equipo.EquipoId`; nombre **actual**, sin consultar el historial BR-E11. Fallback a GUID corto: la resolución nunca rompe la operación | Identity | Web + Mobile (consumidores); Gateway (sin cambios — cae en `/identity/{**catch-all}`, Order 2, Default) | docs/superpowers/specs/2026-07-14-nombres-competidores-design.md · docs/superpowers/plans/2026-07-14-nombres-competidores.md | contracts/http/identity-api.md · contracts/http/gateway-api.md | Implemented — 12 tasks. **Fuente:** verificación de superficies de operador (ninguna mostraba nombres). Refinamiento transversal sobre HU ya implementadas de lobby, ranking en vivo, consolidado, pistas, geolocalización BDT e historial — **no introduce HU nueva**. **Caveat aceptado:** cualquier usuario autenticado puede resolver el nombre de un GUID que ya conozca (relajación frente a `/identity/users/**`, que sigue `AdminOnly`); justificado en el spec. **Diferido:** nombres de partida y de juego (servicio Partidas)→slice propio. |
```

Ajustar el `Status` si alguna task se dejó fuera. No inventar códigos de HU o RNF: este slice no tiene ninguno y eso es deliberado.

- [ ] **Step 5: Verificar la suite completa y commitear**

Run: `dotnet test "services/identity-service/Umbral.IdentityService.sln"`
Run: `cd frontend && npm test`
Run: `cd mobile && npm test && npm run typecheck`
Expected: PASS en las tres.

```bash
git add contracts/http/identity-api.md contracts/http/gateway-api.md docs/04-sdd/SPECS-LIST.md docs/04-sdd/traceability-matrix.md
git commit -m "docs: contrato del directorio de nombres y trazabilidad del slice"
```

---

## Notas para quien ejecute

- **La caché de `useNombres` es de módulo.** Todo test que renderice un componente que use el hook debe llamar `resetNombresCache()` en `beforeEach`, o se contamina con el test anterior.
- **No debilitar los tests de autorización de Task 4.** Que `Participante` reciba 200 es intencional y está justificado en el spec.
- **El handler carga todos los usuarios/equipos con `GetAllAsync`.** Es deliberado: replica `ListarEquiposQueryHandler` y evita añadir métodos de repositorio y migraciones EF. Con la caché del cliente, la frecuencia real de llamada es baja. Si en el futuro molesta, el cambio es local al handler.
