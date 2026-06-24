# SP-1R — Identity Graded-Structure Conformance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the Identity service into full literal compliance with CLAUDE.md's graded structure rules (Controllers + `ControllerBase` + controller unit tests, strictly-flat `Application/` with `DTOs/`, centralized exception middleware, `Infrastructure/Services/`), then amend CLAUDE.md + the migration design so SP-2..SP-5 are built compliant — all without changing any HTTP contract, business rule, event, or observable behavior.

**Architecture:** Layer-by-layer refactor, each task an independently green commit. Pure moves/renames (Tasks 1-3, 5) are regression-gated by the existing 165-test suite, which must stay green **unmodified** except for namespace `using` updates. New artifacts (Task 4 middleware, Task 6 controllers) are TDD'd. Behavior is preserved exactly: routes, status codes, and response bodies are unchanged, so the existing Contract + Integration suites are the safety net.

**Tech Stack:** .NET 8 (8.0.407), Clean Architecture + CQRS via MediatR, FluentValidation, EF Core + Npgsql, xUnit (Unit/Integration/Contract), ASP.NET Core MVC controllers, Keycloak JWT.

## Global Constraints

- **No HTTP/event contract change, no business-rule change, no status-code change.** The exact endpoint routes, HTTP verbs, status codes, and `{ "message": "..." }` error bodies are preserved.
- **Solution:** `services/identity-service/Umbral.IdentityService.sln`. Build: `dotnet build services/identity-service/Umbral.IdentityService.sln`. Test: `dotnet test services/identity-service/Umbral.IdentityService.sln`. Baseline: **165 tests passing**.
- **SDK-style csproj globbing is in effect** (no explicit `<Compile>` items) — moving `.cs` files within a project needs no `.csproj` edit.
- **`Application/DependencyInjection.cs` uses MediatR + FluentValidation assembly scanning** (no per-type registration) — flattening folders does **not** require editing it. Handlers/validators auto-register regardless of folder.
- **Controllers inherit the native `Microsoft.AspNetCore.Mvc.ControllerBase`** (no custom base class). They inject `MediatR.ISender` via constructor, take `[FromServices] IValidator<T>` per action, resolve the actor id with `AuthenticatedUserClaims.TryGetUserId`, dispatch via `_sender.Send(...)`, and contain **no** business logic and **no** `try/catch`.
- **Target Application namespaces (flat):** `…Application.Commands`, `…Application.Queries`, `…Application.Interfaces`, `…Application.Validators`, `…Application.DTOs`, `…Application.Handlers.Commands`, `…Application.Handlers.Queries`, `…Application.Exceptions`. (`…` = `Umbral.IdentityService`.)
- **TDD for new code; frequent commits; full suite green before each commit.** Old doctrine names may not be reintroduced.

---

### Task 1: Move `IUsuarioRepository` to Domain

Repository interfaces belong in Domain (CLAUDE.md). `IEquipoRepository`/`IInvitacionEquipoRepository` already live in `Domain/Abstractions/Persistence/`; `IUsuarioRepository` is the lone outlier in Application.

**Files:**
- Move: `src/Umbral.IdentityService.Application/Abstractions/Persistence/IUsuarioRepository.cs` → `src/Umbral.IdentityService.Domain/Abstractions/Persistence/IUsuarioRepository.cs`
- Modify (8 src consumers — switch the `using`): `Infrastructure/DependencyInjection.cs`, `Infrastructure/Persistence/UsuarioRepository.cs`, `Application/Users/CreateUser/CreateUserWithInitialRoleCommandHandler.cs`, `Application/Users/UpdateUserGeneralData/UpdateUserGeneralDataCommandHandler.cs`, `Application/Users/DeactivateUser/DeactivateUserCommandHandler.cs`, `Application/Users/GetUsers/GetUsersQueryHandler.cs`, `Application/Users/GetUserById/GetUserByIdQueryHandler.cs`, `Application/Teams/Queries/GetParticipantesElegibles/GetParticipantesElegiblesQueryHandler.cs`
- Modify (3 test consumers): `tests/Umbral.IdentityService.UnitTests/CreateUserHandlerTests.cs`, `tests/Umbral.IdentityService.UnitTests/Hu02HandlersTests.cs`, `tests/Umbral.IdentityService.UnitTests/Teams/Invitations/GetParticipantesElegiblesHandlerTests.cs`

**Interfaces:**
- Produces: `IUsuarioRepository` now in namespace `Umbral.IdentityService.Domain.Abstractions.Persistence` (consumed by Tasks 2-3 and all later tasks).

- [ ] **Step 1: Move the file and change its namespace**

```bash
git mv services/identity-service/src/Umbral.IdentityService.Application/Abstractions/Persistence/IUsuarioRepository.cs \
       services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IUsuarioRepository.cs
```
In the moved file, change the declared namespace:
`namespace Umbral.IdentityService.Application.Abstractions.Persistence;` → `namespace Umbral.IdentityService.Domain.Abstractions.Persistence;`

- [ ] **Step 2: Update the 11 consumers' `using`**

In each of the 8 src files and 3 test files listed above, replace
`using Umbral.IdentityService.Application.Abstractions.Persistence;`
with
`using Umbral.IdentityService.Domain.Abstractions.Persistence;`
**Dedupe:** several files (`GetParticipantesElegiblesQueryHandler.cs`, `GetParticipantesElegiblesHandlerTests.cs`, `Infrastructure/DependencyInjection.cs`) already import `Umbral.IdentityService.Domain.Abstractions.Persistence` — in those, just delete the Application one (don't add a duplicate).

The `Application/Abstractions/Persistence/` folder is now empty; remove it:
```bash
rmdir services/identity-service/src/Umbral.IdentityService.Application/Abstractions/Persistence 2>/dev/null || true
```

- [ ] **Step 3: Build**

Run: `dotnet build services/identity-service/Umbral.IdentityService.sln`
Expected: Build succeeded, 0 errors. (If the compiler reports an unresolved `IUsuarioRepository`, that file still imports the old namespace — fix its `using`.)

- [ ] **Step 4: Run the full suite**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: **Passed! Failed: 0, Passed: 165.**

- [ ] **Step 5: Commit**

```bash
git add services/identity-service
git commit -m "Move IUsuarioRepository to Domain (repository interfaces belong to Domain)"
```

---

### Task 2: Flatten `Application/` to the graded folders (+ `DTOs/`, unify `Exceptions/`)

Replace the per-slice folders with the mandated flat CQRS folders. This is a mechanical move-and-rename of every Application file; the build + the existing suite are the verification. Because one old slice namespace (e.g. `…Teams.CreateTeam`) splits across four artifact namespaces, `using` fixes are **compiler-guided**: do the moves, set each file's namespace per the table, rebuild, and resolve each `CS0246`/`CS0234` by adding the correct artifact-type `using`.

**Destination table** (source folder under `Application/` → destination folder → namespace):

| Artifact | Source files | Dest folder | Namespace |
|---|---|---|---|
| Commands (9) | `Teams/CreateTeam/CrearEquipoCommand.cs`, `Teams/LeaveTeam/SalirDeEquipoCommand.cs`, `Teams/TransferLeadership/TransferirLiderazgoCommand.cs`, `Teams/Invitations/EnviarInvitacion/EnviarInvitacionEquipoCommand.cs`, `Teams/Invitations/AceptarInvitacion/AceptarInvitacionEquipoCommand.cs`, `Teams/Invitations/RechazarInvitacion/RechazarInvitacionEquipoCommand.cs`, `Users/CreateUser/CreateUserWithInitialRoleCommand.cs`, `Users/DeactivateUser/DeactivateUserCommand.cs`, `Users/UpdateUserGeneralData/UpdateUserGeneralDataCommand.cs` | `Commands/` | `…Application.Commands` |
| Queries (4) | `Teams/Queries/GetInvitacionesRecibidas/GetInvitacionesRecibidasQuery.cs`, `Teams/Queries/GetParticipantesElegibles/GetParticipantesElegiblesQuery.cs`, `Users/GetUserById/GetUserByIdQuery.cs`, `Users/GetUsers/GetUsersQuery.cs` | `Queries/` | `…Application.Queries` |
| Command handlers (9) | the 9 `*CommandHandler.cs` matching the commands above | `Handlers/Commands/` | `…Application.Handlers.Commands` |
| Query handlers (4) | `GetInvitacionesRecibidasQueryHandler.cs`, `GetParticipantesElegiblesQueryHandler.cs`, `GetUserByIdQueryHandler.cs`, `GetUsersQueryHandler.cs` | `Handlers/Queries/` | `…Application.Handlers.Queries` |
| Validators (9) | the 9 `*CommandValidator.cs` | `Validators/` | `…Application.Validators` |
| DTOs / responses (13) | `CrearEquipoResponse.cs`, `SalirDeEquipoResponse.cs`, `TransferirLiderazgoResponse.cs`, `EnviarInvitacionEquipoResponse.cs`, `AceptarInvitacionEquipoResponse.cs`, `RechazarInvitacionEquipoResponse.cs`, `Teams/Queries/GetInvitacionesRecibidas/InvitacionRecibidasItemResponse.cs`, `Teams/Queries/GetParticipantesElegibles/ParticipanteElegibleResponse.cs`, `Users/Common/UserDetailResponse.cs`, `Users/Common/UserSummaryResponse.cs`, `CreateUserWithInitialRoleResponse.cs`, `DeactivateUserResponse.cs`, `UpdateUserGeneralDataResponse.cs` | `DTOs/` | `…Application.DTOs` |
| Interfaces / ports (4) | `Abstractions/Events/IEquipoEventsPublisher.cs`, `Abstractions/Identity/IKeycloakIdentityPort.cs`, `Abstractions/Notifications/IUserWelcomeEmailSender.cs`, `Abstractions/Security/ITemporaryPasswordGenerator.cs` | `Interfaces/` | `…Application.Interfaces` |
| App exceptions (10 merge in) | all 10 files in `Teams/Exceptions/` | `Exceptions/` | `…Application.Exceptions` |

The 5 files already in `Application/Exceptions/` stay put (namespace already `…Application.Exceptions`).

**Files (consumers needing `using` updates):** `Api/Program.cs`; `Infrastructure/DependencyInjection.cs`; the 4 Infrastructure service impls (`Events/NoOpEquipoEventsPublisher.cs`, `Identity/KeycloakIdentityAdapter.cs`, `Notifications/SmtpUserWelcomeEmailSender.cs`, `Security/CryptoTemporaryPasswordGenerator.cs`); intra-Application references between handlers/validators and their commands/responses/interfaces/exceptions; and the test files that import the old slice namespaces (per the inventory: `ContractTests/IdentityApiFactory.cs`, `IntegrationTests/IdentityApiFactory.cs`, `IntegrationTests/CreateUserEndpointIntegrationTests.cs`, `IntegrationTests/Teams/EquipoPersistenceTests.cs`, `IntegrationTests/Teams/InvitacionEquipoPersistenceTests.cs`, `UnitTests/CreateUserHandlerTests.cs`, `UnitTests/CreateUserValidatorAndDomainTests.cs`, `UnitTests/Hu02HandlersTests.cs`, `UnitTests/Hu02ValidatorsTests.cs`, `UnitTests/KeycloakIdentityAdapterTests.cs`, `UnitTests/Teams/CrearEquipoHandlerTests.cs`, `UnitTests/Teams/CrearEquipoValidatorTests.cs`, `UnitTests/Teams/SalirDeEquipoHandlerTests.cs`, `UnitTests/Teams/TransferirLiderazgoHandlerTests.cs`, `UnitTests/Teams/Invitations/*HandlerTests.cs`).

**Interfaces:**
- Produces: all Application types relocated to the 8 flat namespaces above (consumed by every later task). No type names, signatures, or behavior change — only namespaces and folders.

- [ ] **Step 1: Create the destination folders and move files (`git mv`)**

```bash
cd services/identity-service/src/Umbral.IdentityService.Application
mkdir -p Commands Queries Validators DTOs Interfaces Handlers/Commands Handlers/Queries
# Example moves (repeat for every file in the table; keep file names identical):
git mv Teams/CreateTeam/CrearEquipoCommand.cs            Commands/
git mv Teams/CreateTeam/CrearEquipoCommandHandler.cs     Handlers/Commands/
git mv Teams/CreateTeam/CrearEquipoCommandValidator.cs   Validators/
git mv Teams/CreateTeam/CrearEquipoResponse.cs           DTOs/
# …continue for LeaveTeam, TransferLeadership, Invitations/*, Queries/*, Users/* per the table…
git mv Abstractions/Events/IEquipoEventsPublisher.cs     Interfaces/
git mv Abstractions/Identity/IKeycloakIdentityPort.cs    Interfaces/
git mv Abstractions/Notifications/IUserWelcomeEmailSender.cs Interfaces/
git mv Abstractions/Security/ITemporaryPasswordGenerator.cs  Interfaces/
git mv Teams/Exceptions/*.cs                              Exceptions/
```
After moving, remove the now-empty trees:
```bash
rm -rf Teams Users Abstractions
```

- [ ] **Step 2: Set each moved file's namespace per the table**

In every moved file, rewrite the `namespace …;` line to the destination namespace from the table (e.g. every file now in `Commands/` → `namespace Umbral.IdentityService.Application.Commands;`; every file in `Handlers/Commands/` → `…Application.Handlers.Commands;`; the 10 merged exception files → `…Application.Exceptions;`). Do **not** touch type names or bodies.

- [ ] **Step 3: Fix `Program.cs` usings**

In `Api/Program.cs`, delete the per-slice usings (the block importing `…Application.Users.*`, `…Application.Teams.*`) and replace with:
```csharp
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Exceptions;
```
(Responses are returned via `var`, so no `DTOs` using is needed in `Program.cs`.)

- [ ] **Step 4: Fix `Infrastructure/DependencyInjection.cs` usings**

Replace the four `using …Application.Abstractions.{Events,Identity,Notifications,Security};` lines with a single `using Umbral.IdentityService.Application.Interfaces;`. (The `Application.Abstractions.Persistence` using was already removed in Task 1.) Leave the `Infrastructure.*` usings as-is (Task 3 changes those).

- [ ] **Step 5: Build and fix remaining usings compiler-guided**

Run: `dotnet build services/identity-service/Umbral.IdentityService.sln`
For each `CS0246`/`CS0234` (type/namespace not found): add the correct artifact-type `using` to that file — `…Application.Commands` for a command, `…Application.DTOs` for a response, `…Application.Interfaces` for a port, `…Application.Handlers.Commands`/`.Queries` for a handler, `…Application.Validators` for a validator, `…Application.Exceptions` for an app exception. The 4 Infrastructure impls need `…Application.Interfaces`; the test files need the artifact-type usings matching the types they reference. Repeat build until 0 errors.

- [ ] **Step 6: Run the full suite**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: **Passed! Failed: 0, Passed: 165.** (Pure relocation — count and outcomes unchanged.)

- [ ] **Step 7: Verify the graded folder set, then commit**

Run: `ls services/identity-service/src/Umbral.IdentityService.Application`
Expected exactly: `Commands  DTOs  DependencyInjection.cs  Exceptions  Handlers  Interfaces  Queries  Validators` (and `Handlers/` contains `Commands` + `Queries`). No `Teams/`, `Users/`, or `Abstractions/`.
```bash
git add services/identity-service
git commit -m "Flatten Application into graded CQRS folders (Commands/Queries/Interfaces/Validators/DTOs/Handlers + unified Exceptions)"
```

---

### Task 3: Introduce `Infrastructure/Services/`

CLAUDE.md mandates `Infrastructure/` contain `Persistence/` **and** `Services/`. Move the four non-persistence implementation groups under a new `Services/` folder.

**Files:**
- Move: `Infrastructure/Events/NoOpEquipoEventsPublisher.cs` → `Infrastructure/Services/Events/NoOpEquipoEventsPublisher.cs`
- Move: `Infrastructure/Identity/{KeycloakIdentityAdapter,KeycloakOptions}.cs` → `Infrastructure/Services/Identity/`
- Move: `Infrastructure/Notifications/{SmtpUserWelcomeEmailSender,SmtpOptions,WelcomeEmailTemplate}.cs` → `Infrastructure/Services/Notifications/`
- Move: `Infrastructure/Security/CryptoTemporaryPasswordGenerator.cs` → `Infrastructure/Services/Security/`
- Modify: `Infrastructure/DependencyInjection.cs` (usings); any test importing these Infrastructure namespaces (`UnitTests/KeycloakIdentityAdapterTests.cs`, `UnitTests/TemporaryPasswordGeneratorTests.cs`, and the two `IdentityApiFactory.cs` test files if they import them).

**Interfaces:**
- Produces: namespaces `…Infrastructure.Services.{Events,Identity,Notifications,Security}` (consumed by DI + tests).

- [ ] **Step 1: Move the files**

```bash
cd services/identity-service/src/Umbral.IdentityService.Infrastructure
mkdir -p Services/Events Services/Identity Services/Notifications Services/Security
git mv Events/NoOpEquipoEventsPublisher.cs        Services/Events/
git mv Identity/KeycloakIdentityAdapter.cs        Services/Identity/
git mv Identity/KeycloakOptions.cs                Services/Identity/
git mv Notifications/SmtpUserWelcomeEmailSender.cs Services/Notifications/
git mv Notifications/SmtpOptions.cs               Services/Notifications/
git mv Notifications/WelcomeEmailTemplate.cs      Services/Notifications/
git mv Security/CryptoTemporaryPasswordGenerator.cs Services/Security/
rmdir Events Identity Notifications Security 2>/dev/null || true
```

- [ ] **Step 2: Rename the namespaces**

In the moved files, change `namespace Umbral.IdentityService.Infrastructure.Events;` → `…Infrastructure.Services.Events;` (and likewise `.Identity`, `.Notifications`, `.Security`).

- [ ] **Step 3: Fix `Infrastructure/DependencyInjection.cs` usings**

Replace `using …Infrastructure.Events;`, `using …Infrastructure.Identity;`, `using …Infrastructure.Notifications;`, `using …Infrastructure.Security;` with `using …Infrastructure.Services.Events;`, `…Services.Identity;`, `…Services.Notifications;`, `…Services.Security;`. Leave `using …Infrastructure.Persistence;` as-is.

- [ ] **Step 4: Build, fixing test usings compiler-guided**

Run: `dotnet build services/identity-service/Umbral.IdentityService.sln`
Fix any `CS0234` in test files by repointing the Infrastructure using to its `…Services.*` form. Repeat until 0 errors.

- [ ] **Step 5: Run the full suite**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: **Passed! Failed: 0, Passed: 165.**

- [ ] **Step 6: Commit**

```bash
git add services/identity-service
git commit -m "Group non-persistence Infrastructure impls under Infrastructure/Services/ (graded layout)"
```

---

### Task 4: Centralized `ExceptionHandlingMiddleware` (TDD, registered dormant)

Add the middleware that maps domain/application exceptions to status codes, preserving the **exact** mapping in `Program.cs`. Register it now; the inline `try/catch` still handles mapped exceptions, so behavior is unchanged until Task 6 removes the inline catches. The middleware is proven in isolation by its unit test.

**Files:**
- Create: `src/Umbral.IdentityService.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Create: `tests/Umbral.IdentityService.UnitTests/Api/ExceptionHandlingMiddlewareTests.cs`
- Modify: `src/Umbral.IdentityService.Api/Program.cs` (register middleware)

**Interfaces:**
- Produces: `ExceptionHandlingMiddleware` (used by Program.cs in Task 6 as the sole exception→status mapper). Body shape `{ "message": "<exception.Message>" }`.

- [ ] **Step 1: Write the failing middleware test**

Create `tests/Umbral.IdentityService.UnitTests/Api/ExceptionHandlingMiddlewareTests.cs`:
```csharp
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.IdentityService.Api.Middleware;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.UnitTests.Api;

public sealed class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int status, string body)> InvokeWith(Exception ex)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ => throw ex, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    [Theory]
    [InlineData(typeof(AlreadyBelongsToActiveTeamException), 409)]
    [InlineData(typeof(NoActiveTeamForParticipantException), 404)]
    [InlineData(typeof(NoEsLiderException), 403)]
    [InlineData(typeof(KeycloakIntegrationException), 502)]
    [InlineData(typeof(PersistenceException), 500)]
    public async Task Maps_Exception_To_Status(Type exceptionType, int expected)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType, "msg")!;
        var (status, _) = await InvokeWith(ex);
        Assert.Equal(expected, status);
    }

    [Fact]
    public async Task Unmapped_Exception_Is_500()
    {
        var (status, _) = await InvokeWith(new InvalidOperationException("boom"));
        Assert.Equal(500, status);
    }

    [Fact]
    public async Task Body_Has_Message_Field()
    {
        var (_, body) = await InvokeWith(new UserNotFoundException("no user"));
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("no user", doc.RootElement.GetProperty("message").GetString());
    }
}
```
> Note: this assumes each mapped exception has a public `(string message)` constructor. If one does not, the implementer adapts that `InlineData` row to construct it with its real constructor (do not change the production exception types).

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln --filter ExceptionHandlingMiddleware`
Expected: FAIL (type `ExceptionHandlingMiddleware` does not exist).

- [ ] **Step 3: Implement the middleware**

Create `src/Umbral.IdentityService.Api/Middleware/ExceptionHandlingMiddleware.cs`:
```csharp
using System.Net;
using System.Text.Json;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var status = exception switch
        {
            AlreadyBelongsToActiveTeamException => HttpStatusCode.Conflict,
            ConcurrentTeamCreationException => HttpStatusCode.Conflict,
            UsuarioYaEnEquipoException => HttpStatusCode.Conflict,
            LeaveTeamConflictException => HttpStatusCode.Conflict,
            TransferirLiderazgoConflictException => HttpStatusCode.Conflict,
            EquipoLlenoException => HttpStatusCode.Conflict,
            InvitacionPendienteYaExisteException => HttpStatusCode.Conflict,
            DuplicateEmailException => HttpStatusCode.Conflict,
            NoActiveTeamForParticipantException => HttpStatusCode.NotFound,
            InvitacionNoEncontradaException => HttpStatusCode.NotFound,
            UserNotFoundException => HttpStatusCode.NotFound,
            NoEsLiderException => HttpStatusCode.Forbidden,
            KeycloakIntegrationException => HttpStatusCode.BadGateway,
            EmailDeliveryException => HttpStatusCode.BadGateway,
            PersistenceException => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception.");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = exception.Message }));
    }
}
```

- [ ] **Step 4: Register the middleware in `Program.cs`**

In `Api/Program.cs`, immediately after `app.UseCors("FrontendDev");`, add:
```csharp
app.UseMiddleware<Umbral.IdentityService.Api.Middleware.ExceptionHandlingMiddleware>();
```
(Placed before `UseAuthentication`/`UseAuthorization` so it wraps the whole pipeline. Inline endpoint `try/catch` still runs first and stays until Task 6, so behavior is unchanged.)

- [ ] **Step 5: Run the middleware test, then the full suite**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln --filter ExceptionHandlingMiddleware`
Expected: PASS.
Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: **Passed! Failed: 0, Passed: 172** (165 + 7 new middleware cases).

- [ ] **Step 6: Commit**

```bash
git add services/identity-service
git commit -m "Add ExceptionHandlingMiddleware (exact status mapping) registered dormant alongside inline catches"
```

---

### Task 5: Api prep — rename `Authentication/` → `Utils/`, extract request records to `Contracts/`

Pure relocations that make Task 6's controller swap a clean diff.

**Files:**
- Move: `Api/Authentication/AuthenticatedUserClaims.cs` → `Api/Utils/AuthenticatedUserClaims.cs`
- Move: `Api/Authentication/KeycloakRoleClaims.cs` → `Api/Utils/KeycloakRoleClaims.cs`
- Create: `Api/Contracts/TeamRequests.cs`, `Api/Contracts/UserRequests.cs`
- Modify: `Api/Program.cs` (usings; remove the request records from the bottom)

**Interfaces:**
- Produces: namespace `…Api.Utils` (helpers) and `…Api.Contracts` (request records: `CrearEquipoRequest(string NombreEquipo)`, `TransferirLiderazgoRequest(Guid NuevoLiderUserId)`, `EnviarInvitacionRequest(Guid InvitadoUserId)`, `UpdateUserGeneralDataRequest(string Name, string Email)`). Consumed by the controllers in Task 6.

- [ ] **Step 1: Rename the helpers folder + namespace**

```bash
cd services/identity-service/src/Umbral.IdentityService.Api
git mv Authentication Utils
```
In both moved files, change `namespace Umbral.IdentityService.Api.Authentication;` → `namespace Umbral.IdentityService.Api.Utils;`.
In `Program.cs`, change `using Umbral.IdentityService.Api.Authentication;` → `using Umbral.IdentityService.Api.Utils;`.

- [ ] **Step 2: Extract the request records into `Contracts/`**

Create `Api/Contracts/TeamRequests.cs`:
```csharp
namespace Umbral.IdentityService.Api.Contracts;

public sealed record CrearEquipoRequest(string NombreEquipo);
public sealed record TransferirLiderazgoRequest(Guid NuevoLiderUserId);
public sealed record EnviarInvitacionRequest(Guid InvitadoUserId);
```
Create `Api/Contracts/UserRequests.cs`:
```csharp
namespace Umbral.IdentityService.Api.Contracts;

public sealed record UpdateUserGeneralDataRequest(string Name, string Email);
```
In `Program.cs`, delete the four `public sealed record …Request(...)` declarations near the bottom (the `UpdateUserGeneralDataRequest`, `CrearEquipoRequest`, `TransferirLiderazgoRequest`, `EnviarInvitacionRequest` lines), keep `public partial class Program {}`, and add `using Umbral.IdentityService.Api.Contracts;` to the top.

- [ ] **Step 3: Build and run the full suite**

Run: `dotnet build services/identity-service/Umbral.IdentityService.sln` → 0 errors.
Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: **Passed! Failed: 0, Passed: 172.**

- [ ] **Step 4: Commit**

```bash
git add services/identity-service
git commit -m "Api prep: rename Authentication->Utils (claim helpers, not auth); extract request records to Contracts/"
```

---

### Task 6: Convert the 13 endpoints to 3 controllers + controller unit tests; slim `Program.cs`

The atomic Api swap. Controllers replace the minimal endpoints (routes/verbs/status/bodies identical); the inline `try/catch` is removed so the Task-4 middleware becomes the exception mapper. Controllers can't coexist with the minimal endpoints (route conflict), so this is one step. TDD via controller unit tests written first.

**Files:**
- Create: `Api/Controllers/UsersController.cs`, `Api/Controllers/TeamsController.cs`, `Api/Controllers/TeamInvitationsController.cs`
- Create: `tests/Umbral.IdentityService.UnitTests/Api/FakeSender.cs`, `…/Api/UsersControllerTests.cs`, `…/Api/TeamsControllerTests.cs`, `…/Api/TeamInvitationsControllerTests.cs`
- Modify: `Api/Program.cs` (add `AddControllers()`/`MapControllers()`; delete all `app.Map*` endpoint blocks + inline try/catch)

**Interfaces:**
- Consumes: all Task 2 commands/queries (`…Application.Commands`/`.Queries`), responses (`…Application.DTOs`), `IValidator<T>`, `ISender`, `AuthenticatedUserClaims` (`…Api.Utils`), request records (`…Api.Contracts`).
- Produces: 3 controllers serving the exact existing routes; `FakeSender : ISender` for tests.

- [ ] **Step 1: Write the shared test `FakeSender`**

Create `tests/Umbral.IdentityService.UnitTests/Api/FakeSender.cs`:
```csharp
using MediatR;

namespace Umbral.IdentityService.UnitTests.Api;

internal sealed class FakeSender : ISender
{
    public object? LastRequest { get; private set; }
    public object? NextResponse { get; set; }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult((TResponse)NextResponse!);
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult(NextResponse);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
```

- [ ] **Step 2: Write the failing controller unit tests**

Create `tests/Umbral.IdentityService.UnitTests/Api/TeamsControllerTests.cs`:
```csharp
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Api.Controllers;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.UnitTests.Api;

public sealed class TeamsControllerTests
{
    private static TeamsController BuildController(FakeSender sender, Guid? sub)
    {
        var controller = new TeamsController(sender);
        var claims = sub is null ? new ClaimsIdentity() : new ClaimsIdentity(new[] { new Claim("sub", sub.Value.ToString()) });
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(claims) }
        };
        return controller;
    }

    [Fact]
    public async Task Crear_Dispatches_Command_And_Returns_Created()
    {
        var actor = Guid.NewGuid();
        var sender = new FakeSender { NextResponse = new CrearEquipoResponse(Guid.NewGuid(), "Equipo A", Array.Empty<Guid>()) };
        var controller = BuildController(sender, actor);

        var result = await controller.Crear(new CrearEquipoRequest("Equipo A"), new InlineValidator<CrearEquipoCommand>(), CancellationToken.None);

        Assert.IsType<CreatedResult>(result);
        var command = Assert.IsType<CrearEquipoCommand>(sender.LastRequest);
        Assert.Equal(actor, command.ActorUserId);
        Assert.Equal("Equipo A", command.NombreEquipo);
    }

    [Fact]
    public async Task Crear_Returns_Unauthorized_When_No_Sub()
    {
        var controller = BuildController(new FakeSender(), sub: null);
        var result = await controller.Crear(new CrearEquipoRequest("X"), new InlineValidator<CrearEquipoCommand>(), CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Crear_Returns_400_When_Validation_Fails()
    {
        var validator = new InlineValidator<CrearEquipoCommand>();
        validator.RuleFor(c => c.NombreEquipo).Must(_ => false).WithMessage("bad");
        var controller = BuildController(new FakeSender(), Guid.NewGuid());

        var result = await controller.Crear(new CrearEquipoRequest(""), validator, CancellationToken.None);

        var obj = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, obj.StatusCode);
    }
}
```
> The `CrearEquipoResponse(...)` constructor args above must match its real record definition; if its shape differs, adjust the `NextResponse` construction (read `Application/DTOs/CrearEquipoResponse.cs`). Write analogous test classes `UsersControllerTests` (happy-path dispatch → `CreatedResult`/`OkObjectResult`; one `GetById` → `OkObjectResult`) and `TeamInvitationsControllerTests` (send → `CreatedResult`; inbox → `OkObjectResult`; unauthorized path) covering at least the create/dispatch and unauthorized paths per controller.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln --filter ControllerTests`
Expected: FAIL (controllers do not exist).

- [ ] **Step 4: Implement `TeamsController`**

Create `Api/Controllers/TeamsController.cs`:
```csharp
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Api.Utils;
using Umbral.IdentityService.Application.Commands;

namespace Umbral.IdentityService.Api.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize(Policy = "ParticipantOnly")]
public sealed class TeamsController : ControllerBase
{
    private readonly ISender _sender;

    public TeamsController(ISender sender) => _sender = sender;

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearEquipoRequest request,
        [FromServices] IValidator<CrearEquipoCommand> validator,
        CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var command = new CrearEquipoCommand(actorUserId, request.NombreEquipo);
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var response = await _sender.Send(command, cancellationToken);
        return Created($"/api/teams/{response.EquipoId}", response);
    }

    [HttpDelete("membership")]
    public async Task<IActionResult> Salir(
        [FromServices] IValidator<SalirDeEquipoCommand> validator,
        CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var command = new SalirDeEquipoCommand(actorUserId);
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("leadership")]
    public async Task<IActionResult> TransferirLiderazgo(
        [FromBody] TransferirLiderazgoRequest request,
        [FromServices] IValidator<TransferirLiderazgoCommand> validator,
        CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var command = new TransferirLiderazgoCommand(actorUserId, request.NuevoLiderUserId);
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }
}
```

- [ ] **Step 5: Implement `TeamInvitationsController`**

Create `Api/Controllers/TeamInvitationsController.cs` with base route `api/teams`, `[Authorize(Policy = "ParticipantOnly")]`, `ISender` ctor, and actions mirroring the current endpoints exactly:
- `[HttpPost("invitations")] Enviar([FromBody] EnviarInvitacionRequest, [FromServices] IValidator<EnviarInvitacionEquipoCommand>)` → builds `new EnviarInvitacionEquipoCommand(actorUserId, request.InvitadoUserId)`, validates, `Created($"/api/teams/invitations/{response.InvitacionEquipoId}", response)`.
- `[HttpGet("invitations")] Recibidas()` → `Ok(await _sender.Send(new GetInvitacionesRecibidasQuery(actorUserId), ct))` (no validator).
- `[HttpPost("invitations/{invitacionId:guid}/acceptance")] Aceptar(Guid invitacionId, [FromServices] IValidator<AceptarInvitacionEquipoCommand>)` → `new AceptarInvitacionEquipoCommand(actorUserId, invitacionId)`, validate, `Ok(response)`.
- `[HttpPost("invitations/{invitacionId:guid}/rejection")] Rechazar(Guid invitacionId, [FromServices] IValidator<RechazarInvitacionEquipoCommand>)` → `new RechazarInvitacionEquipoCommand(actorUserId, invitacionId)`, validate, `Ok(response)`.
- `[HttpGet("eligible-participants")] Elegibles()` → `Ok(await _sender.Send(new GetParticipantesElegiblesQuery(actorUserId), ct))` (no validator).

All actions: `TryGetUserId(User, …)` → `Unauthorized()` on failure; validation failure → `ValidationProblem(ModelState)` (same pattern as `TeamsController`). `using` the `…Application.Commands` and `…Application.Queries` namespaces.

- [ ] **Step 6: Implement `UsersController`**

Create `Api/Controllers/UsersController.cs`, base route `api/identity/users`, `[Authorize(Policy = "AdminOnly")]`, `ISender` ctor. Actions reproduce the current user endpoints exactly (the create/update bodies bind directly; `GetUsers`/`GetById` take no body; user actions do **not** read the `sub` claim):
- `[HttpPost] Create([FromBody] CreateUserWithInitialRoleCommand command, [FromServices] IValidator<CreateUserWithInitialRoleCommand> validator)` → validate (→ `ValidationProblem`), `Created($"/api/identity/users/{response.UserId}", response)`.
- `[HttpGet] GetUsers()` → `Ok(await _sender.Send(new GetUsersQuery(), ct))`.
- `[HttpGet("{userId:guid}")] GetById(Guid userId)` → `Ok(await _sender.Send(new GetUserByIdQuery(userId), ct))`.
- `[HttpPatch("{userId:guid}")] Update(Guid userId, [FromBody] UpdateUserGeneralDataRequest request, [FromServices] IValidator<UpdateUserGeneralDataCommand> validator)` → `new UpdateUserGeneralDataCommand(userId, request.Name, request.Email)`, validate, `Ok(response)`.
- `[HttpPatch("{userId:guid}/deactivation")] Deactivate(Guid userId, [FromServices] IValidator<DeactivateUserCommand> validator)` → `new DeactivateUserCommand(userId)`, validate, `Ok(response)`.

No `try/catch` anywhere — domain/app exceptions propagate to the middleware (which maps `UserNotFoundException`→404, `DuplicateEmailException`→409, `KeycloakIntegrationException`/`EmailDeliveryException`→502, `PersistenceException`→500, exactly as before).

- [ ] **Step 7: Slim `Program.cs`**

In `Api/Program.cs`:
1. After `builder.Services.AddIdentityInfrastructure(...)` (or alongside the other `builder.Services` calls), add `builder.Services.AddControllers();`.
2. Delete **all** `app.Map…` endpoint blocks (the entire team, invitation, and user endpoint region) and their inline `try/catch`.
3. After `app.UseAuthorization();`, add `app.MapControllers();`.
4. Remove now-unused `using`s (`FluentValidation`, `FluentValidation.Results`, `System.Security.Claims`, `MediatR`, the `…Application.Commands/.Queries/.Exceptions`, `…Domain.Exceptions`, `…Api.Contracts` imports the endpoints needed) — keep only what the remaining builder/auth/DB-bootstrap code uses; the compiler's unused-using and unresolved-symbol errors will guide. Keep `public partial class Program {}`.

The retained `Program.cs` is: builder + Keycloak JWT config + `AddCors` + `AddAuthorization` (the `AdminOnly`/`ParticipantOnly` policies) + `AddControllers` + DB bootstrap scope + `UseCors` + `UseMiddleware<ExceptionHandlingMiddleware>` + `UseAuthentication`/`UseAuthorization` + `MapControllers` + `app.Run()`.

- [ ] **Step 8: Run controller tests, then the full suite**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln --filter ControllerTests`
Expected: PASS.
Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: **Failed: 0** with all existing Contract + Integration tests green (they exercise the new controllers + middleware end-to-end over the unchanged routes/status codes) plus the new controller tests. Total ≈ 172 + new controller cases.

- [ ] **Step 9: Verify graded Api layout, then commit**

Run: `grep -n "app.Map" services/identity-service/src/Umbral.IdentityService.Api/Program.cs`
Expected: only `app.MapControllers();` (no `app.MapGet/MapPost/MapDelete/MapMethods`).
```bash
git add services/identity-service
git commit -m "Convert Identity endpoints to controllers (ControllerBase) + controller unit tests; slim Program.cs; middleware now maps exceptions"
```

---

### Task 7: Deliverable B — amend CLAUDE.md + migration design + R1 checklist

Docs-only. Encodes the graded structure (with the `DTOs/` + `Exceptions/` additions) as a mandatory, forward-looking acceptance criterion so SP-2..SP-5 are built compliant.

**Files:**
- Modify: `CLAUDE.md` ("Structure & coding rules (graded)" → `Application/`)
- Modify: `docs/superpowers/specs/2026-06-22-code-structure-doctrine-migration-design.md` (Global Constraints + R-phase)

- [ ] **Step 1: Amend CLAUDE.md's `Application/` graded rule**

In `CLAUDE.md`, under `### `Application/``, replace the "Must contain strictly these folders" sentence with:
```markdown
Must contain exactly these top-level folders (no per-feature slice folders): `Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `DTOs/`, `Handlers/`, `Handlers/Commands/`, `Handlers/Queries/`, and `Exceptions/`.
- `Handlers/Commands/` holds the `XCommandHandler` classes; `Handlers/Queries/` holds the `XQueryHandler` classes.
- `DTOs/` holds request/response models; `Interfaces/` holds application-layer ports (repository interfaces stay in `Domain/`); `Exceptions/` holds application-layer exceptions.
```

- [ ] **Step 2: Amend the migration design's Global Constraints**

In `docs/superpowers/specs/2026-06-22-code-structure-doctrine-migration-design.md`, in "## Global Constraints / Conventions", update the `Application/` folder list to match Step 1 (add `DTOs/` and `Exceptions/`), and append a sentence: "The graded structure (Controllers + `ControllerBase` + controller unit tests, the flat `Application/` folder set, `Infrastructure/{persistence,services}/`, centralized exception middleware) is a **mandatory acceptance criterion for every SP**; the SP-1R-refactored `identity-service` is the canonical reference implementation."

- [ ] **Step 3: Add the R1 structural checklist**

In the same design's "### Phases" → R1 description, append: "**R1 structural checklist (per SP):** `Api/Controllers/` present and `Program.cs` contains no `app.Map{Get,Post,Put,Delete,Patch}` (only `MapControllers`/`MapHub`); `Application/` has exactly the mandated folder set; `Infrastructure/` has `Persistence/` + `Services/`; a centralized exception middleware is registered; every controller has a unit test."

- [ ] **Step 4: Verify and commit**

Run: `grep -n "DTOs/" CLAUDE.md docs/superpowers/specs/2026-06-22-code-structure-doctrine-migration-design.md`
Expected: both files reference `DTOs/`.
```bash
git add CLAUDE.md docs/superpowers/specs/2026-06-22-code-structure-doctrine-migration-design.md
git commit -m "Doctrine: graded structure (incl. DTOs/, Exceptions/) is mandatory for every SP; Identity is the reference"
```

---

## Self-Review Notes

**Spec coverage:** Domain move → Task 1; flat `Application/` + `DTOs/` + unified `Exceptions/` → Task 2; `Infrastructure/Services/` → Task 3; centralized exception middleware → Task 4; `Api/Utils` rename + `Contracts/` → Task 5; Controllers (`ControllerBase`) + controller unit tests + slim `Program.cs` → Task 6; CLAUDE.md + migration-design amendments + R1 checklist (Deliverable B) → Task 7. Exact exception→status table → Task 4 middleware. "Existing suites pass unmodified except namespace usings" → Tasks 1-3, 5.

**Placeholder scan:** no TBD/TODO; the two "match the real record/constructor" notes (Task 4 InlineData, Task 6 `CrearEquipoResponse`) are explicit verification instructions, not deferred work — the implementer reads the named file to confirm the signature.

**Type consistency:** flat namespaces (`…Application.Commands/.Queries/.Interfaces/.Validators/.DTOs/.Handlers.Commands/.Handlers.Queries/.Exceptions`) used identically across Tasks 2-6; `ISender`-based controllers + `FakeSender` consistent between Task 6 controllers and tests; request records (`…Api.Contracts`) produced in Task 5 and consumed in Task 6; `AuthenticatedUserClaims` (`…Api.Utils`) produced in Task 5 and consumed in Task 6; middleware exception list (Task 4) matches the routes' exceptions retired in Task 6.

**Behavior preservation:** routes, verbs, status codes, and `{message}` bodies are unchanged; the existing Contract + Integration suites are required to pass **unmodified** (beyond namespace usings), which is the regression guarantee.
