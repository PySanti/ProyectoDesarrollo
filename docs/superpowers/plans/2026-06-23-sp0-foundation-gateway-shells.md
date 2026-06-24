# SP-0 Foundation (Gateway + Service Shells + Target DBs) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the target four-service topology's infrastructure — a YARP gateway plus three graded, runnable, DbContext-wired service shells (`partidas`, `operaciones-sesion`, `puntuaciones`) and their databases — so SP-2 only has to fill Partidas.

**Architecture:** Three new .NET 8 service shells mirror the post-SP-1R `identity-service` graded structure (Domain → Application → Infrastructure → Api + tests), each exposing an anonymous `/health` controller and an EF Core `DbContext` (no entities yet, InMemory fallback when no connection string). A YARP gateway is the single entry point: routing/role-mapping live entirely in `appsettings`, with a minimal fail-secure code floor (JWT validation reusing Identity's proven Keycloak realm-role flattening, three fixed role policies, and a `RequireAuthenticatedUser` fallback). Docker-compose, run-local scripts, target DBs, an ADR, and a final R1 structural gate complete the slice.

**Tech Stack:** .NET 8, ASP.NET Core (`Microsoft.NET.Sdk.Web`), MediatR 12.2.0, FluentValidation 11.11.0, EF Core 8.0.7 + Npgsql 8.0.4 + EF InMemory 8.0.7, `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.7, YARP `Yarp.ReverseProxy` 2.1.0, xUnit + `Microsoft.AspNetCore.Mvc.Testing` 8.0.7, PostgreSQL, Docker Compose.

## Global Constraints

Copied verbatim from `docs/superpowers/specs/2026-06-23-sp0-foundation-gateway-shells-design.md`. Every task's requirements implicitly include this section.

- **Service identity (→ ADR-0009):** new shells use suffix-less doctrine slugs and `Umbral.<Svc>.*` namespaces. `identity-service`, `trivia-game-service`, `bdt-game-service` are **NOT touched**.
  - Partidas — folder `services/partidas`, namespace root `Umbral.Partidas`, local port **5010**, DB `umbral_partidas`, connection-string key `PartidasDatabase`.
  - Operaciones de Sesión — folder `services/operaciones-sesion`, namespace root `Umbral.OperacionesSesion`, local port **5020**, DB `umbral_operaciones_sesion`, connection-string key `OperacionesSesionDatabase`.
  - Puntuaciones — folder `services/puntuaciones`, namespace root `Umbral.Puntuaciones`, local port **5030**, DB `umbral_puntuaciones`, connection-string key `PuntuacionesDatabase`.
  - Gateway — folder `gateway`, namespace root `Umbral.Gateway`, local port **5080**.
- **Graded structure (mandatory acceptance criterion, R1 gate):** `Api/Controllers/` present; `Program.cs` contains **no** `app.Map{Get,Post,Put,Delete,Patch}` (only `MapControllers`) for the three services; `Application/` has exactly `Commands/ Queries/ Interfaces/ Validators/ DTOs/ Handlers/ Handlers/Commands/ Handlers/Queries/ Exceptions/`; `Infrastructure/` has `Persistence/` + `Services/` (PascalCase, matching the canonical `identity-service`); centralized exception middleware registered; every controller has a unit test. Controllers inherit the native `ControllerBase`. The gateway is a routing host, not a domain service: its `/health` may be a minimal-API endpoint.
- **Gateway:** doctrine-complete (JWT validation, route-level role authz, WebSocket passthrough, anonymous `/health`), implemented config-first — routes/clusters/role-mapping in `appsettings`; the security border (JWT scheme, 3 role policies, `SetFallbackPolicy(RequireAuthenticatedUser)`) in code.
- **Canonical reference:** `services/identity-service` (post SP-1R). Mirror its patterns; do not invent new ones.
- **Discipline:** TDD per task; frequent commits; the build and all tests stay green at the end of every task. The three shells' automated tests must run **without** a live Postgres (EF InMemory fallback); real DB connectivity is exercised manually via run-local and fully in SP-2.

---

### Task 1: ADR-0009 — service slugs, ports & gateway topology

**Files:**
- Create: `docs/05-decisions/ADR-0009-service-slugs-ports-gateway-topology.md`

**Interfaces:**
- Produces: the authoritative slug/namespace/port/DB table that every later task references (this is "the migration ADR" `CLAUDE.md` points at).

- [ ] **Step 1: Write the ADR**

Create `docs/05-decisions/ADR-0009-service-slugs-ports-gateway-topology.md`:

```markdown
# ADR-0009: Service Slugs, Namespaces, Ports & Gateway Topology

## Status
Accepted (2026-06-23) — supersedes the "slugs finalized in the migration ADR" placeholder in CLAUDE.md.

## Context
The migration target is four services behind a YARP gateway. On-disk folders use a `-service`
suffix (`identity-service`); CLAUDE.md's run-local/commands use suffix-less slugs. SP-0 creates
three new shells and the gateway and must fix the convention.

## Decision
New shells use suffix-less doctrine slugs and `Umbral.<Svc>.*` namespaces. The existing
`identity-service` and the legacy `trivia-game-service` / `bdt-game-service` are NOT renamed in
SP-0; their rename/replacement is deferred (Identity rename is cosmetic and out of scope; the game
services are dismantled in SP-3/SP-4). The temporary coexistence of `identity-service` and
`partidas` is accepted migration debt.

| Component | Folder | Root namespace | Local port | Compose host port | Database | Conn-string key |
|---|---|---|---|---|---|---|
| Gateway | `gateway/` | `Umbral.Gateway` | 5080 | 5080:8080 | — | — |
| Identity (exists) | `services/identity-service` | `Umbral.IdentityService` | 5000 | 5001:8080 | `umbral_identity` | `IdentityDatabase` |
| Partidas | `services/partidas` | `Umbral.Partidas` | 5010 | (internal) | `umbral_partidas` | `PartidasDatabase` |
| Operaciones de Sesión | `services/operaciones-sesion` | `Umbral.OperacionesSesion` | 5020 | (internal) | `umbral_operaciones_sesion` | `OperacionesSesionDatabase` |
| Puntuaciones | `services/puntuaciones` | `Umbral.Puntuaciones` | 5030 | (internal) | `umbral_puntuaciones` | `PuntuacionesDatabase` |

Gateway routes: `/identity/*`→identity, `/partidas/*`→partidas, `/operaciones-sesion/*`→operaciones-sesion,
`/puntuaciones/*`→puntuaciones. Legacy trivia/bdt are not routed through the gateway (clients hit them
directly until SP-5). Only the gateway publishes a host port; the four services stay on the internal network.

## Consequences
- CLAUDE.md's suffix-less run-local slugs are now authoritative for the three new services.
- Mixed naming (`identity-service` vs `partidas`) persists until a later cosmetic-rename slice.
```

- [ ] **Step 2: Commit**

```bash
git add docs/05-decisions/ADR-0009-service-slugs-ports-gateway-topology.md
git commit -m "ADR-0009: service slugs, namespaces, ports & gateway topology"
```

---

### Task 2: Partidas service shell (canonical template)

Builds the full graded, runnable, DbContext-wired shell. Tasks 3 and 4 mirror this one by copy-rename, so get it right here.

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Domain/Umbral.Partidas.Domain.csproj`
- Create: `services/partidas/src/Umbral.Partidas.Application/Umbral.Partidas.Application.csproj`
- Create: `services/partidas/src/Umbral.Partidas.Application/DependencyInjection.cs`
- Create (empty, `.gitkeep`): `Application/{Commands,Queries,Interfaces,Validators,DTOs,Handlers,Handlers/Commands,Handlers/Queries,Exceptions}/`
- Create: `services/partidas/src/Umbral.Partidas.Infrastructure/Umbral.Partidas.Infrastructure.csproj`
- Create: `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/PartidasDbContext.cs`
- Create: `services/partidas/src/Umbral.Partidas.Infrastructure/DependencyInjection.cs` (at Infrastructure root, matching `identity-service`)
- Create (empty, `.gitkeep`): `Infrastructure/Services/`
- Create: `services/partidas/src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj`
- Create: `services/partidas/src/Umbral.Partidas.Api/Program.cs`
- Create: `services/partidas/src/Umbral.Partidas.Api/Controllers/HealthController.cs`
- Create: `services/partidas/src/Umbral.Partidas.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Create: `services/partidas/src/Umbral.Partidas.Api/appsettings.json`
- Create: `services/partidas/src/Umbral.Partidas.Api/appsettings.Development.json`
- Create: `services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj`
- Create: `services/partidas/tests/Umbral.Partidas.UnitTests/HealthControllerTests.cs`
- Create: `services/partidas/tests/Umbral.Partidas.IntegrationTests/Umbral.Partidas.IntegrationTests.csproj`
- Create: `services/partidas/tests/Umbral.Partidas.IntegrationTests/HealthEndpointTests.cs`
- Create: `services/partidas/tests/Umbral.Partidas.ContractTests/Umbral.Partidas.ContractTests.csproj`
- Create: `services/partidas/tests/Umbral.Partidas.ContractTests/HealthContractTests.cs`
- Create: `services/partidas/Umbral.Partidas.sln`
- Create: `services/partidas/Dockerfile`
- Create: `services/partidas/run-local.sh`, `run-local.ps1`, `.env.example`
- Create: `services/partidas/service-context.md`

**Interfaces:**
- Produces (consumed by Tasks 3, 4 as the rename source, and Task 6 compose): `Umbral.Partidas.Application.DependencyInjection.AddPartidasApplication(IServiceCollection)`; `Umbral.Partidas.Infrastructure.DependencyInjection.AddPartidasInfrastructure(IServiceCollection, IConfiguration)` (reads connection string `PartidasDatabase`, InMemory fallback `"partidas-dev"`); `Umbral.Partidas.Infrastructure.Persistence.PartidasDbContext`; `Umbral.Partidas.Api.Controllers.HealthController.Get()` → `Ok(new { status, service })`; `public partial class Program {}` in the Api.

- [ ] **Step 1: Create the four src project files (3 class libs + 1 web)**

`src/Umbral.Partidas.Domain/Umbral.Partidas.Domain.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

`src/Umbral.Partidas.Application/Umbral.Partidas.Application.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Umbral.Partidas.Domain/Umbral.Partidas.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.2.0" />
    <PackageReference Include="FluentValidation" Version="11.11.0" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.1" />
  </ItemGroup>
</Project>
```

`src/Umbral.Partidas.Infrastructure/Umbral.Partidas.Infrastructure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Umbral.Partidas.Application/Umbral.Partidas.Application.csproj" />
    <ProjectReference Include="../Umbral.Partidas.Domain/Umbral.Partidas.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.7" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.7" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="8.0.7" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.1" />
  </ItemGroup>
</Project>
```

`src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Umbral.Partidas.Application/Umbral.Partidas.Application.csproj" />
    <ProjectReference Include="../Umbral.Partidas.Infrastructure/Umbral.Partidas.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create the graded empty folders**

```bash
cd services/partidas
for d in Commands Queries Interfaces Validators DTOs Handlers Handlers/Commands Handlers/Queries Exceptions; do
  mkdir -p "src/Umbral.Partidas.Application/$d"; touch "src/Umbral.Partidas.Application/$d/.gitkeep"
done
mkdir -p src/Umbral.Partidas.Infrastructure/Services; touch src/Umbral.Partidas.Infrastructure/Services/.gitkeep
cd ../..
```

- [ ] **Step 3: Application & Infrastructure DI + DbContext**

`src/Umbral.Partidas.Application/DependencyInjection.cs`:

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Umbral.Partidas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPartidasApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
```

`src/Umbral.Partidas.Infrastructure/Persistence/PartidasDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace Umbral.Partidas.Infrastructure.Persistence;

// No entities yet — the Partida/Juego model and its configuration arrive in SP-2.
public sealed class PartidasDbContext : DbContext
{
    public PartidasDbContext(DbContextOptions<PartidasDbContext> options) : base(options)
    {
    }
}
```

`src/Umbral.Partidas.Infrastructure/DependencyInjection.cs` (at Infrastructure root, matching `identity-service`):

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Partidas.Infrastructure.Persistence;

namespace Umbral.Partidas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPartidasInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PartidasDatabase");

        services.AddDbContext<PartidasDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("partidas-dev");
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        return services;
    }
}
```

- [ ] **Step 4: Api — HealthController, middleware, Program, appsettings**

`src/Umbral.Partidas.Api/Controllers/HealthController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Umbral.Partidas.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new HealthStatus("healthy", "partidas"));
}

public sealed record HealthStatus(string Status, string Service);
```

`src/Umbral.Partidas.Api/Middleware/ExceptionHandlingMiddleware.cs`:

```csharp
using System.Net;
using System.Text.Json;

namespace Umbral.Partidas.Api.Middleware;

// Centralized exception handling. SP-2 adds domain/application exception → status mappings.
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
            _logger.LogError(ex, "Unhandled exception.");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
        }
    }
}
```

`src/Umbral.Partidas.Api/Program.cs`:

```csharp
using Umbral.Partidas.Api.Middleware;
using Umbral.Partidas.Application;
using Umbral.Partidas.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPartidasApplication();
builder.Services.AddPartidasInfrastructure(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program
{
}
```

`src/Umbral.Partidas.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "PartidasDatabase": ""
  }
}
```

`src/Umbral.Partidas.Api/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

- [ ] **Step 5: Write the failing controller unit test**

`tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

`tests/Umbral.Partidas.UnitTests/HealthControllerTests.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Umbral.Partidas.Api.Controllers;

namespace Umbral.Partidas.UnitTests;

public class HealthControllerTests
{
    [Fact]
    public void Get_returns_ok_with_healthy_status_and_service()
    {
        var controller = new HealthController();

        var result = controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var payload = Assert.IsType<HealthStatus>(ok.Value);
        Assert.Equal("healthy", payload.Status);
        Assert.Equal("partidas", payload.Service);
    }
}
```

- [ ] **Step 6: Run the unit test**

Run: `dotnet test services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj`
Expected: PASS. SP-0 is a structural-scaffold slice: `HealthController` is trivial and has no meaningful pre-implementation RED state, so no genuine RED→GREEN cycle is claimed here. What matters is that the controller unit test exists, asserts the controller's real contract (status + service), and passes; the full-solution green is Step 9.

- [ ] **Step 7: Write the integration & contract tests**

`tests/Umbral.Partidas.IntegrationTests/Umbral.Partidas.IntegrationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.7" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj" />
    <ProjectReference Include="../../src/Umbral.Partidas.Infrastructure/Umbral.Partidas.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

`tests/Umbral.Partidas.IntegrationTests/HealthEndpointTests.cs`:

```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Partidas.Infrastructure.Persistence;

namespace Umbral.Partidas.IntegrationTests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void DbContext_is_registered()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetService<PartidasDbContext>();

        Assert.NotNull(db);
    }
}
```

`tests/Umbral.Partidas.ContractTests/Umbral.Partidas.ContractTests.csproj` (same as the IntegrationTests csproj but named `Umbral.Partidas.ContractTests`):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.7" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

`tests/Umbral.Partidas.ContractTests/HealthContractTests.cs` (asserts the documented `/health` contract shape — a real assertion, not a placeholder):

```csharp
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Umbral.Partidas.ContractTests;

public class HealthContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthContractTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_body_matches_contract()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("healthy", json.RootElement.GetProperty("status").GetString());
        Assert.Equal("partidas", json.RootElement.GetProperty("service").GetString());
    }
}
```

- [ ] **Step 8: Create the solution and wire all six projects**

```bash
cd services/partidas
dotnet new sln -n Umbral.Partidas
dotnet sln add src/Umbral.Partidas.Domain/Umbral.Partidas.Domain.csproj \
               src/Umbral.Partidas.Application/Umbral.Partidas.Application.csproj \
               src/Umbral.Partidas.Infrastructure/Umbral.Partidas.Infrastructure.csproj \
               src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj \
               tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj \
               tests/Umbral.Partidas.IntegrationTests/Umbral.Partidas.IntegrationTests.csproj \
               tests/Umbral.Partidas.ContractTests/Umbral.Partidas.ContractTests.csproj
cd ../..
```

- [ ] **Step 9: Build and run the whole solution's tests (GREEN)**

Run: `dotnet test services/partidas/Umbral.Partidas.sln`
Expected: PASS — 4 tests (1 unit, 2 integration, 1 contract), build clean, output pristine (no warnings). The integration/contract tests run with the EF InMemory fallback (no Postgres needed).

- [ ] **Step 10: Dockerfile, run-local scripts, .env.example, service-context**

`services/partidas/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj"
RUN dotnet publish "src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Umbral.Partidas.Api.dll"]
```

`services/partidas/run-local.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

if [[ -f ../../.env ]]; then
  set -a
  # shellcheck disable=SC1091
  source ../../.env
  set +a
fi

if [[ -f .env ]]; then
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
fi

dotnet run --project "src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj"
```

`services/partidas/run-local.ps1`:

```powershell
Set-Location -Path $PSScriptRoot

function Import-DotEnv($path) {
  if (Test-Path $path) {
    Get-Content $path | Where-Object { $_ -match '^\s*[^#].*=' } | ForEach-Object {
      $name, $value = $_ -split '=', 2
      [Environment]::SetEnvironmentVariable($name.Trim(), $value.Trim().Trim("'`""))
    }
  }
}

Import-DotEnv (Join-Path $PSScriptRoot "../../.env")
Import-DotEnv (Join-Path $PSScriptRoot ".env")

dotnet run --project "src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj"
```

`services/partidas/.env.example`:

```bash
# Valores propios de este servicio (los globales vienen del .env raiz del repo).
ASPNETCORE_ENVIRONMENT='Development'
ASPNETCORE_URLS='http://0.0.0.0:5010'
ConnectionStrings__PartidasDatabase='Host=localhost;Port=55432;Database=umbral_partidas;Username=umbral;Password=16102005'
```

`services/partidas/service-context.md`:

```markdown
# Partidas — service context

Owns creation and configuration of a `Partida` and its `Juego`s (Trivia question config and
BDT stage config, per-stage `Puntaje`). Does NOT run the live session or compute scores/ranking.

Status: SP-0 shell — graded structure + `/health` + empty `PartidasDbContext` (→ `umbral_partidas`).
The Partida/Juego domain model and configuration endpoints arrive in SP-2.
```

```bash
chmod +x services/partidas/run-local.sh
```

- [ ] **Step 11: Commit**

```bash
git add services/partidas
git commit -m "SP-0: Partidas service shell (graded + runnable + DbContext + /health)"
```

---

### Task 3: Operaciones de Sesión shell (mirror of Partidas)

Created by copying the Partidas shell and applying a precise, ordered rename. `dotnet build` + tests are the safety net for any missed substitution.

**Files:**
- Create: the entire `services/operaciones-sesion/` tree (mirror of `services/partidas/` with `Umbral.OperacionesSesion` / `umbral_operaciones_sesion` / port 5020 / `OperacionesSesionDatabase` / `operaciones-sesion`).

**Interfaces:**
- Consumes: the `services/partidas` tree from Task 2 as the rename source.
- Produces: `AddOperacionesSesionApplication`, `AddOperacionesSesionInfrastructure` (conn-string key `OperacionesSesionDatabase`, InMemory fallback `"operaciones-sesion-dev"`), `OperacionesSesionDbContext`, Api `Program` partial, `HealthController.Get()` → `Ok(new { status="healthy", service="operaciones-sesion" })`.

- [ ] **Step 1: Copy the Partidas shell (without build artifacts) and rename files/dirs**

```bash
cd services
rsync -a --exclude bin --exclude obj --exclude .env partidas/ operaciones-sesion/
cd operaciones-sesion

# Rename project dirs/files (Umbral.Partidas* -> Umbral.OperacionesSesion*). Loop until
# stable and rename only the basename, so a parent-dir rename can't invalidate a queued
# child path (find lists all paths before any mv runs).
while [ -n "$(find . -depth -name '*Umbral.Partidas*' -print -quit)" ]; do
  find . -depth -name '*Umbral.Partidas*' -print0 | while IFS= read -r -d '' p; do
    mv "$p" "$(dirname "$p")/$(basename "$p" | sed 's/Umbral\.Partidas/Umbral.OperacionesSesion/')"
  done
done
# The DbContext FILE name contains "Partidas" but NOT "Umbral.Partidas", so the find above
# does not catch it — rename it explicitly (the content sed handles the class name).
mv src/Umbral.OperacionesSesion.Infrastructure/Persistence/PartidasDbContext.cs \
   src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs
cd ../..
```

- [ ] **Step 2: Apply ordered content substitutions**

```bash
cd services/operaciones-sesion
# Order matters: the DB-name and slug substitutions are specific; do them before the PascalCase token.
grep -rIl --exclude-dir=bin --exclude-dir=obj 'umbral_partidas' . | xargs -r sed -i 's/umbral_partidas/umbral_operaciones_sesion/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj '"partidas-dev"' . | xargs -r sed -i 's/"partidas-dev"/"operaciones-sesion-dev"/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj 'service = "partidas"' . | xargs -r sed -i 's/service = "partidas"/service = "operaciones-sesion"/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj '"partidas"' . | xargs -r sed -i 's/"partidas"/"operaciones-sesion"/g'   # ContractTests assertion
grep -rIl --exclude-dir=bin --exclude-dir=obj '5010' . | xargs -r sed -i 's/5010/5020/g'                               # .env.example port
grep -rIl --exclude-dir=bin --exclude-dir=obj 'Umbral\.Partidas' . | xargs -r sed -i 's/Umbral\.Partidas/Umbral.OperacionesSesion/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj 'PartidasDbContext' . | xargs -r sed -i 's/PartidasDbContext/OperacionesSesionDbContext/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj 'AddPartidas' . | xargs -r sed -i 's/AddPartidas/AddOperacionesSesion/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj 'PartidasDatabase' . | xargs -r sed -i 's/PartidasDatabase/OperacionesSesionDatabase/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj 'partidas-dev' . | xargs -r sed -i 's/partidas-dev/operaciones-sesion-dev/g'  # safety re-pass
# Fix the DbContext placeholder comment (domain prose the token seds don't catch).
sed -i 's|// No entities yet.*|// No entities yet — live-session (runtime) state arrives in SP-3.|' \
  src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs
cd ../..
```

Then update `services/operaciones-sesion/service-context.md` body to describe Operaciones de Sesión:

```markdown
# Operaciones de Sesión — service context

Owns the live experience: publishing a partida (→ Lobby), start, question/stage synchronization,
answer/QR validation, sequential advance, clues, geolocation, reconnection, inscriptions & team
convocatorias; stores only transient session state and emits domain events via RabbitMQ.

Status: SP-0 shell — graded structure + `/health` + empty `OperacionesSesionDbContext`
(→ `umbral_operaciones_sesion`). Runtime extracted from Trivia/BDT in SP-3.
```

- [ ] **Step 3: Verify no stray `Partidas`/`partidas` references remain**

Run: `grep -rIn --exclude-dir=bin --exclude-dir=obj -i 'partidas' services/operaciones-sesion ; echo "exit=$?"`
Expected: no output (exit=1 from grep). If any line prints, fix that file by hand (it is a missed substitution).

- [ ] **Step 4: Build & test the renamed solution (GREEN)**

Run: `dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln`
Expected: PASS — 4 tests, build clean, output pristine. A rename miss surfaces here as a build error; fix and re-run.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion
git commit -m "SP-0: Operaciones de Sesion service shell (graded + runnable + DbContext + /health)"
```

---

### Task 4: Puntuaciones shell (mirror of Partidas)

Identical procedure to Task 3, targeting Puntuaciones.

**Files:**
- Create: the entire `services/puntuaciones/` tree (mirror of `services/partidas/` with `Umbral.Puntuaciones` / `umbral_puntuaciones` / port 5030 / `PuntuacionesDatabase` / `puntuaciones`).

**Interfaces:**
- Consumes: the `services/partidas` tree from Task 2 as the rename source.
- Produces: `AddPuntuacionesApplication`, `AddPuntuacionesInfrastructure` (conn-string key `PuntuacionesDatabase`, InMemory fallback `"puntuaciones-dev"`), `PuntuacionesDbContext`, Api `Program` partial, `HealthController.Get()` → `Ok(new { status="healthy", service="puntuaciones" })`.

- [ ] **Step 1: Copy the Partidas shell and rename files/dirs**

```bash
cd services
rsync -a --exclude bin --exclude obj --exclude .env partidas/ puntuaciones/
cd puntuaciones
# Rename project dirs/files; loop until stable, rename only the basename (see Task 3 note).
while [ -n "$(find . -depth -name '*Umbral.Partidas*' -print -quit)" ]; do
  find . -depth -name '*Umbral.Partidas*' -print0 | while IFS= read -r -d '' p; do
    mv "$p" "$(dirname "$p")/$(basename "$p" | sed 's/Umbral\.Partidas/Umbral.Puntuaciones/')"
  done
done
# DbContext FILE name contains "Partidas" but not "Umbral.Partidas" — rename explicitly.
mv src/Umbral.Puntuaciones.Infrastructure/Persistence/PartidasDbContext.cs \
   src/Umbral.Puntuaciones.Infrastructure/Persistence/PuntuacionesDbContext.cs
cd ../..
```

- [ ] **Step 2: Apply ordered content substitutions**

```bash
cd services/puntuaciones
grep -rIl --exclude-dir=bin --exclude-dir=obj 'umbral_partidas' . | xargs -r sed -i 's/umbral_partidas/umbral_puntuaciones/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj '"partidas-dev"' . | xargs -r sed -i 's/"partidas-dev"/"puntuaciones-dev"/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj 'service = "partidas"' . | xargs -r sed -i 's/service = "partidas"/service = "puntuaciones"/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj '"partidas"' . | xargs -r sed -i 's/"partidas"/"puntuaciones"/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj '5010' . | xargs -r sed -i 's/5010/5030/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj 'Umbral\.Partidas' . | xargs -r sed -i 's/Umbral\.Partidas/Umbral.Puntuaciones/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj 'PartidasDbContext' . | xargs -r sed -i 's/PartidasDbContext/PuntuacionesDbContext/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj 'AddPartidas' . | xargs -r sed -i 's/AddPartidas/AddPuntuaciones/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj 'PartidasDatabase' . | xargs -r sed -i 's/PartidasDatabase/PuntuacionesDatabase/g'
grep -rIl --exclude-dir=bin --exclude-dir=obj 'partidas-dev' . | xargs -r sed -i 's/partidas-dev/puntuaciones-dev/g'
# Fix the DbContext placeholder comment (domain prose the token seds don't catch).
sed -i 's|// No entities yet.*|// No entities yet — scoring/ranking projections arrive in SP-4.|' \
  src/Umbral.Puntuaciones.Infrastructure/Persistence/PuntuacionesDbContext.cs
cd ../..
```

Then update `services/puntuaciones/service-context.md` body:

```markdown
# Puntuaciones — service context

Tracks scores and won stages, computes each game's native ranking and the consolidated partida
ranking, team-performance queries, and materializes audit/history. A read/projection model fed by
RabbitMQ domain events, broadcasting via SignalR. Owns neither configuration nor runtime.

Status: SP-0 shell — graded structure + `/health` + empty `PuntuacionesDbContext`
(→ `umbral_puntuaciones`). Scoring/ranking projections arrive in SP-4.
```

- [ ] **Step 3: Verify no stray references remain**

Run: `grep -rIn --exclude-dir=bin --exclude-dir=obj -i 'partidas' services/puntuaciones ; echo "exit=$?"`
Expected: no output (exit=1).

- [ ] **Step 4: Build & test (GREEN)**

Run: `dotnet test services/puntuaciones/Umbral.Puntuaciones.sln`
Expected: PASS — 4 tests, build clean, output pristine.

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones
git commit -m "SP-0: Puntuaciones service shell (graded + runnable + DbContext + /health)"
```

---

### Task 5: YARP gateway (config-first, fail-secure)

**Files:**
- Create: `gateway/src/Umbral.Gateway/Umbral.Gateway.csproj`
- Create: `gateway/src/Umbral.Gateway/Program.cs`
- Create: `gateway/src/Umbral.Gateway/Security/KeycloakJwtExtensions.cs`
- Create: `gateway/src/Umbral.Gateway/Security/KeycloakRoleClaims.cs`
- Create: `gateway/src/Umbral.Gateway/appsettings.json`
- Create: `gateway/src/Umbral.Gateway/appsettings.Development.json`
- Create: `gateway/tests/Umbral.Gateway.IntegrationTests/Umbral.Gateway.IntegrationTests.csproj`
- Create: `gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs`
- Create: `gateway/Umbral.Gateway.sln`
- Create: `gateway/Dockerfile`, `gateway/run-local.sh`, `gateway/run-local.ps1`, `gateway/.env.example`
- Modify: `gateway/gateway-context.md`

**Interfaces:**
- Consumes: token claim shape and realm-role flattening from Identity's `KeycloakRoleClaims` (copied verbatim into `Umbral.Gateway.Security`); service routes target the cluster addresses defined in `appsettings`.
- Produces: a running reverse proxy with anonymous `/health`, JWT auth, three role policies (`Administrador`/`Operador`/`Participante`), and a `RequireAuthenticatedUser` fallback; `public partial class Program {}` for tests.

- [ ] **Step 1: Project file**

`gateway/src/Umbral.Gateway/Umbral.Gateway.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Yarp.ReverseProxy" Version="2.1.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.7" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Security helpers (JWT wiring + realm-role flattening)**

`gateway/src/Umbral.Gateway/Security/KeycloakRoleClaims.cs` (copied verbatim from `services/identity-service/src/Umbral.IdentityService.Api/Utils/KeycloakRoleClaims.cs`, only the namespace changed):

```csharp
using System.Security.Claims;
using System.Text.Json;

namespace Umbral.Gateway.Security;

internal static class KeycloakRoleClaims
{
    private static readonly Dictionary<string, string> KnownRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["administrador"] = "Administrador",
        ["operador"] = "Operador",
        ["participante"] = "Participante"
    };

    public static void AddRolesFromKeycloakClaims(ClaimsIdentity identity)
    {
        foreach (var role in ReadRealmRoles(identity).Concat(ReadClientRoles(identity)).Select(NormalizeRole).Distinct(StringComparer.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(role) && !identity.HasClaim(identity.RoleClaimType, role))
            {
                identity.AddClaim(new Claim(identity.RoleClaimType, role));
            }
        }
    }

    private static string NormalizeRole(string role)
        => KnownRoles.TryGetValue(role.Trim(), out var normalized) ? normalized : role.Trim();

    private static IEnumerable<string> ReadRealmRoles(ClaimsIdentity identity)
    {
        var realmAccessClaim = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccessClaim)) return [];
        try
        {
            using var document = JsonDocument.Parse(realmAccessClaim);
            return document.RootElement.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array
                ? rolesElement.EnumerateArray().Select(role => role.GetString()).OfType<string>().ToArray()
                : [];
        }
        catch (JsonException) { return []; }
    }

    private static IEnumerable<string> ReadClientRoles(ClaimsIdentity identity)
    {
        var resourceAccessClaim = identity.FindFirst("resource_access")?.Value;
        if (string.IsNullOrWhiteSpace(resourceAccessClaim)) return [];
        try
        {
            using var document = JsonDocument.Parse(resourceAccessClaim);
            return document.RootElement.EnumerateObject()
                .SelectMany(client => client.Value.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array
                    ? rolesElement.EnumerateArray().Select(role => role.GetString()).OfType<string>()
                    : [])
                .ToArray();
        }
        catch (JsonException) { return []; }
    }
}
```

`gateway/src/Umbral.Gateway/Security/KeycloakJwtExtensions.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Umbral.Gateway.Security;

public static class KeycloakJwtExtensions
{
    public static IServiceCollection AddKeycloakJwtAuth(
        this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var baseUrl = Resolve(configuration, "Keycloak:BaseUrl", "KEYCLOAK_BASE_URL");
        var realm = Resolve(configuration, "Keycloak:Realm", "KEYCLOAK_REALM");
        var clientId = Resolve(configuration, "Keycloak:ClientId", "KEYCLOAK_CLIENT_ID");
        var audiencesRaw = Resolve(configuration, "Keycloak:ValidAudiences", "KEYCLOAK_VALID_AUDIENCES");
        var issuersRaw = Resolve(configuration, "Keycloak:ValidIssuers", "KEYCLOAK_VALID_ISSUERS");

        // No realm configured (e.g. offline tests): still register the scheme so the fallback
        // policy 401s unauthenticated requests. No metadata fetch happens without a token to validate.
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(realm))
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            return services;
        }

        var authority = $"{baseUrl.TrimEnd('/')}/realms/{realm}";
        var validIssuers = Split(issuersRaw);
        if (!validIssuers.Contains(authority)) validIssuers.Add(authority);
        var validAudiences = Split(audiencesRaw);
        if (!string.IsNullOrWhiteSpace(clientId) && !validAudiences.Contains(clientId)) validAudiences.Add(clientId);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = clientId;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = validIssuers,
                    ValidateAudience = true,
                    ValidAudiences = validAudiences,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RoleClaimType = "roles"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        if (ctx.Principal?.Identity is ClaimsIdentity identity)
                        {
                            KeycloakRoleClaims.AddRolesFromKeycloakClaims(identity);
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    private static string? Resolve(IConfiguration c, string key, string env)
        => !string.IsNullOrWhiteSpace(c[key]) ? c[key] : Environment.GetEnvironmentVariable(env);

    private static List<string> Split(string? raw)
        => (raw ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}
```

- [ ] **Step 3: Program.cs (the minimal floor) + appsettings (all routing in config)**

`gateway/src/Umbral.Gateway/Program.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Umbral.Gateway.Security;

var builder = WebApplication.CreateBuilder(args);

// Routing: entirely from appsettings (ReverseProxy section).
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// AuthN: Keycloak JWT validation (values from config/env).
builder.Services.AddKeycloakJwtAuth(builder.Configuration, builder.Environment);

// AuthZ: the three fixed base roles + secure-by-default fallback.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Administrador", p => p.RequireRole("Administrador"))
    .AddPolicy("Operador", p => p.RequireRole("Operador"))
    .AddPolicy("Participante", p => p.RequireRole("Participante"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Gateway's own liveness check — anonymous, minimal-API (the proxy host owns no domain logic).
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }))
   .AllowAnonymous();

// Reverse proxy (WebSocket passthrough is automatic). Routes without an explicit
// AuthorizationPolicy inherit the fallback policy → fail-secure.
app.MapReverseProxy();

app.Run();

public partial class Program
{
}
```

`gateway/src/Umbral.Gateway/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Yarp": "Information"
    }
  },
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      "identity": {
        "ClusterId": "identity",
        "Match": { "Path": "/identity/{**catch-all}" },
        "AuthorizationPolicy": "Default"
      },
      "partidas": {
        "ClusterId": "partidas",
        "Match": { "Path": "/partidas/{**catch-all}" },
        "AuthorizationPolicy": "Operador"
      },
      "operaciones-sesion": {
        "ClusterId": "operaciones-sesion",
        "Match": { "Path": "/operaciones-sesion/{**catch-all}" },
        "AuthorizationPolicy": "Default"
      },
      "puntuaciones": {
        "ClusterId": "puntuaciones",
        "Match": { "Path": "/puntuaciones/{**catch-all}" },
        "AuthorizationPolicy": "Default"
      }
    },
    "Clusters": {
      "identity": { "Destinations": { "d1": { "Address": "http://localhost:5000/" } } },
      "partidas": { "Destinations": { "d1": { "Address": "http://localhost:5010/" } } },
      "operaciones-sesion": { "Destinations": { "d1": { "Address": "http://localhost:5020/" } } },
      "puntuaciones": { "Destinations": { "d1": { "Address": "http://localhost:5030/" } } }
    }
  }
}
```

`gateway/src/Umbral.Gateway/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Yarp": "Debug"
    }
  }
}
```

- [ ] **Step 4: Write the failing integration tests (RED)**

`gateway/tests/Umbral.Gateway.IntegrationTests/Umbral.Gateway.IntegrationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.7" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/Umbral.Gateway/Umbral.Gateway.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

`gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs`:

```csharp
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Umbral.Gateway.IntegrationTests;

public class GatewayEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GatewayEndpointsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_is_anonymous_and_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Explicit_policy_route_without_token_is_401()
    {
        // /partidas carries an explicit AuthorizationPolicy ("Operador"): no token → 401,
        // enforced before the proxy ever contacts the (unreachable) destination.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/partidas/anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Fallback_policy_is_fail_secure()
    {
        // Directly proves the fail-secure FLOOR: any route LACKING an explicit AuthorizationPolicy
        // inherits this fallback, which denies anonymous access. This assertion fails iff
        // SetFallbackPolicy(RequireAuthenticatedUser) is removed — independent of any route policy.
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        var fallback = await provider.GetFallbackPolicyAsync();

        Assert.NotNull(fallback);
        Assert.Contains(fallback!.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task Default_policy_route_without_token_is_401()
    {
        // /puntuaciones uses AuthorizationPolicy "Default" (YARP's reserved word → the application
        // default policy = RequireAuthenticatedUser). No token → 401. Pins the reserved-word contract
        // the three "Default" routes depend on; a config typo (unknown policy name) would not 401.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/puntuaciones/anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 5: Create the solution**

```bash
cd gateway
dotnet new sln -n Umbral.Gateway
dotnet sln add src/Umbral.Gateway/Umbral.Gateway.csproj \
               tests/Umbral.Gateway.IntegrationTests/Umbral.Gateway.IntegrationTests.csproj
cd ..
```

- [ ] **Step 6: Build & test (GREEN)**

Run: `dotnet test gateway/Umbral.Gateway.sln`
Expected: PASS — 4 tests. `/health` → 200; `/partidas/anything` (explicit `Operador` policy) without a token → 401 (JWT pipeline); `/puntuaciones/anything` (a `"Default"`-policy route) without a token → 401 (pins YARP's reserved-word → `RequireAuthenticatedUser`); and the fallback policy is asserted to deny anonymous access — the fail-secure floor isolated from any route policy. Runs offline. Output pristine.

- [ ] **Step 7: Dockerfile, run-local, .env.example, gateway-context**

`gateway/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Umbral.Gateway/Umbral.Gateway.csproj"
RUN dotnet publish "src/Umbral.Gateway/Umbral.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Umbral.Gateway.dll"]
```

`gateway/run-local.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

if [[ -f ../.env ]]; then
  set -a
  # shellcheck disable=SC1091
  source ../.env
  set +a
fi

if [[ -f .env ]]; then
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
fi

dotnet run --project "src/Umbral.Gateway/Umbral.Gateway.csproj"
```

`gateway/run-local.ps1`:

```powershell
Set-Location -Path $PSScriptRoot

function Import-DotEnv($path) {
  if (Test-Path $path) {
    Get-Content $path | Where-Object { $_ -match '^\s*[^#].*=' } | ForEach-Object {
      $name, $value = $_ -split '=', 2
      [Environment]::SetEnvironmentVariable($name.Trim(), $value.Trim().Trim("'`""))
    }
  }
}

Import-DotEnv (Join-Path $PSScriptRoot "../.env")
Import-DotEnv (Join-Path $PSScriptRoot ".env")

dotnet run --project "src/Umbral.Gateway/Umbral.Gateway.csproj"
```

`gateway/.env.example`:

```bash
# Gateway (entry point). Routing lives in appsettings; only Keycloak values come from env.
ASPNETCORE_ENVIRONMENT='Development'
ASPNETCORE_URLS='http://0.0.0.0:5080'
KEYCLOAK_BASE_URL="http://localhost:8080"
KEYCLOAK_REALM='UMBRAL-UCAB'
KEYCLOAK_VALID_AUDIENCES='umbral-web,umbral-mobile,account'
```

Replace `gateway/gateway-context.md` body with:

```markdown
# Gateway — context

The mandatory YARP entry point. Validates the Keycloak JWT and applies coarse, route-level
authorization by base role; routes to the four services; passes WebSockets through for SignalR.
Owns no domain logic, scores, rankings, or DB access.

Config-first: routes/clusters/role-mapping live in `appsettings.json` (`ReverseProxy`); the
security border (JWT scheme, 3 role policies, `RequireAuthenticatedUser` fallback) is the only code.
Status: SP-0 — routes to identity (5000) + the three shells (5010/5020/5030); legacy trivia/bdt
are not routed (clients hit them directly until SP-5).
```

```bash
chmod +x gateway/run-local.sh
```

- [ ] **Step 8: Commit**

```bash
git add gateway
git commit -m "SP-0: YARP gateway (config-first routing, fail-secure JWT + role authz, /health)"
```

---

### Task 6: Compose topology, target databases & health contract

**Files:**
- Modify: `infra/docker-compose.yml`
- Create: `contracts/http/health.md`

**Interfaces:**
- Consumes: the four Dockerfiles (gateway + 3 shells) from Tasks 2–5.
- Produces: a `docker compose config`-valid topology; the three target databases; the common `/health` HTTP contract.

- [ ] **Step 1: Add the gateway + three service entries to docker-compose**

In `infra/docker-compose.yml`, fix the header comment block to read:

```yaml
# Docker Compose for the four-service UMBRAL topology, behind the YARP gateway.
# Target services: gateway, identity-service, partidas, operaciones-sesion, puntuaciones.
# Legacy (in transit, dismantled in SP-3/SP-4): trivia-game-service, bdt-game-service.
# Do not add audit-service, scoring-service, or treasure-hunt-service.
```

Then add these services (after the `identity-service` block, before `trivia-game-service`):

```yaml
  gateway:
    build:
      context: ../gateway
    ports:
      - "5080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      KEYCLOAK_BASE_URL: http://keycloak:8080
      KEYCLOAK_REALM: UMBRAL-UCAB
      KEYCLOAK_VALID_AUDIENCES: umbral-web,umbral-mobile,account
      ReverseProxy__Clusters__identity__Destinations__d1__Address: http://identity-service:8080/
      ReverseProxy__Clusters__partidas__Destinations__d1__Address: http://partidas:8080/
      ReverseProxy__Clusters__operaciones-sesion__Destinations__d1__Address: http://operaciones-sesion:8080/
      ReverseProxy__Clusters__puntuaciones__Destinations__d1__Address: http://puntuaciones:8080/
    depends_on:
      - identity-service
      - partidas
      - operaciones-sesion
      - puntuaciones
      - keycloak

  partidas:
    build:
      context: ../services/partidas
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__PartidasDatabase: Host=postgres;Port=5432;Database=umbral_partidas;Username=umbral;Password=16102005
    depends_on:
      - postgres

  operaciones-sesion:
    build:
      context: ../services/operaciones-sesion
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__OperacionesSesionDatabase: Host=postgres;Port=5432;Database=umbral_operaciones_sesion;Username=umbral;Password=16102005
    depends_on:
      - postgres

  puntuaciones:
    build:
      context: ../services/puntuaciones
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__PuntuacionesDatabase: Host=postgres;Port=5432;Database=umbral_puntuaciones;Username=umbral;Password=16102005
    depends_on:
      - postgres
```

- [ ] **Step 2: Validate the compose file**

Run: `docker compose -f infra/docker-compose.yml config >/dev/null && echo OK`
Expected: `OK` (the YAML parses and references resolve). If Docker is unavailable in this environment, instead run `python3 -c "import yaml,sys; yaml.safe_load(open('infra/docker-compose.yml')); print('OK')"` to confirm valid YAML, and note in the report that `docker compose config` must be run where Docker is available.

- [ ] **Step 3: Create the three target databases**

Requires the infra Postgres running (`docker compose -f infra/docker-compose.yml up -d postgres`). Run:

```bash
for db in umbral_partidas umbral_operaciones_sesion umbral_puntuaciones; do
  docker exec -i umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE $db;" || true
done
docker exec -i umbral-postgres psql -U umbral -d umbral -c "\l" | grep umbral_
```

Expected: the three databases appear in the list (`|| true` tolerates "already exists" on re-run). If Postgres/Docker is unavailable here, record the exact commands in the report for the operator to run.

- [ ] **Step 4: Write the common /health contract**

`contracts/http/health.md`:

```markdown
# Contract: GET /health (all backend services + gateway)

**Auth:** none (anonymous).
**Request:** `GET /health`
**Response:** `200 OK`, `Content-Type: application/json`

```json
{ "status": "healthy", "service": "<service-slug>" }
```

`service` is the service slug (`gateway`, `partidas`, `operaciones-sesion`, `puntuaciones`,
…). Used by compose/orchestration liveness checks. Business contracts for each service arrive
in their own slice.
```

- [ ] **Step 5: Commit**

```bash
git add infra/docker-compose.yml contracts/http/health.md
git commit -m "SP-0: compose entries (gateway + 3 services), target DBs, /health contract"
```

---

### Task 7: R1 structural gate

**Files:**
- Create: `.git/sdd/sp0-r1-gate.md` (review record; `.git/` is gitignored — this is the durable ledger artifact, not a committed file)

**Interfaces:**
- Consumes: the three shells (Tasks 2–4) and the gateway (Task 5).
- Produces: a pass/fail R1 verdict; any defects route to a fix wave (amend the relevant task's commit) before SP-0 closes.

- [ ] **Step 1: Run the R1 structural checklist against each shell + gateway**

For each of `services/partidas`, `services/operaciones-sesion`, `services/puntuaciones` confirm:

```bash
for svc in partidas operaciones-sesion puntuaciones; do
  echo "== $svc =="
  ls services/$svc/src/*.Api/Controllers/ 2>/dev/null && echo "Controllers/ OK" || echo "Controllers/ MISSING"
  grep -qE 'app\.Map(Get|Post|Put|Delete|Patch)\(' services/$svc/src/*.Api/Program.cs && echo "MINIMAL-API FOUND (defect)" || echo "no minimal-API routes OK"
  for d in Commands Queries Interfaces Validators DTOs Handlers Handlers/Commands Handlers/Queries Exceptions; do
    test -d services/$svc/src/*.Application/$d || echo "Application/$d MISSING"
  done
  test -d services/$svc/src/*.Infrastructure/Persistence && echo "Persistence/ OK" || echo "Persistence/ MISSING"
  test -d services/$svc/src/*.Infrastructure/Services && echo "Services/ OK" || echo "Services/ MISSING"
  grep -q 'UseMiddleware<ExceptionHandlingMiddleware>' services/$svc/src/*.Api/Program.cs && echo "exc middleware OK" || echo "exc middleware MISSING"
  ls services/$svc/tests/*.UnitTests/HealthControllerTests.cs >/dev/null 2>&1 && echo "controller unit test OK" || echo "controller unit test MISSING"
done
```

Expected: every line ends `OK`, with `no minimal-API routes OK` for all three. For the gateway confirm: `/health` is `AllowAnonymous`; the fallback policy is present; the 401 test passes.

- [ ] **Step 2: Run the full SP-0 test sweep**

Run:
```bash
dotnet test services/partidas/Umbral.Partidas.sln
dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln
dotnet test services/puntuaciones/Umbral.Puntuaciones.sln
dotnet test gateway/Umbral.Gateway.sln
```
Expected: all green (4+4+4+4 = 16 tests), output pristine.

- [ ] **Step 3: Record the R1 verdict**

Write `.git/sdd/sp0-r1-gate.md` with the checklist results, the test totals, and a PASS/FAIL verdict. If any defect was found, fix it in the owning task's files, re-run that solution's tests, amend the commit, and re-run this gate. Under ultracode, this gate may instead be executed as a multi-agent Workflow (finders per surface → adversarial verification → synthesis) — see the Execution Handoff note.

- [ ] **Step 4: Final commit (docs/ledger only if anything tracked changed)**

```bash
git add -A
git commit -m "SP-0: R1 structural gate passed (3 shells + gateway, 16 tests green)" || echo "nothing to commit"
```

---

## Self-Review

**Spec coverage** (against `2026-06-23-sp0-foundation-gateway-shells-design.md`):
- §2 naming/ports/DBs → Task 1 (ADR) + per-task names. ✓
- §3 shell anatomy (graded + DbContext + tests) → Tasks 2–4. ✓
- §4 gateway config-first + fail-secure floor → Task 5. ✓
- §5 infra/compose/DBs/run-local → run-local/.env per shell+gateway (Tasks 2,3,4,5) + compose+DBs (Task 6). ✓
- §6 contracts/docs/ADR → ADR (Task 1), health.md + service-context/gateway-context (Tasks 2–6). ✓
- §7 testing + R1 gate → per-task tests + Task 7. ✓

**Placeholder scan:** No "TBD/TODO". The graded `Application/` folders are intentionally empty (`.gitkeep`) — that is the SP-0 deliverable, not a placeholder. ContractTests assert a real `/health` shape (not a no-op).

**Type consistency:** `AddPartidasApplication`/`AddPartidasInfrastructure`/`PartidasDbContext`/`PartidasDatabase` consistent across Task 2 and the rename maps in Tasks 3–4 (`OperacionesSesion*`/`OperacionesSesionDatabase`, `Puntuaciones*`/`PuntuacionesDatabase`). Gateway code registers three named policies (`Administrador`/`Operador`/`Participante`); `appsettings` routes reference `Operador` (a named policy) and `Default` — note `Default` is YARP's reserved keyword that resolves to the framework default policy (`RequireAuthenticatedUser`), not a custom named policy. `public partial class Program {}` present in every Api + the gateway for `WebApplicationFactory<Program>`.

**Known intentional deviations from the design's schematic:** (1) the design's `Program.cs` floor used `.Bind(o)` for JWT; the plan uses Identity's proven explicit `TokenValidationParameters` + `OnTokenValidated` realm-role flattening (correctness over the schematic). (2) Per-service defense-in-depth JWT validation (CLAUDE.md line 73) is deferred to SP-2: the shells expose **only** an anonymous `/health` and MUST NOT ship any authenticated (non-`/health`) route until per-service JWT validation (mirroring `identity-service`'s `Program.cs`) is added; the gateway authenticates at the edge now. (3) Shell automated tests use the EF InMemory fallback so they need no live Postgres — this **mirrors how the canonical `identity-service` tests run**. The spec §3/§9 wording (startup connectivity validation; real/ephemeral-Postgres integration test) was **reconciled** to this reality (spec updated 2026-06-23); real DB connectivity is exercised via run-local in SP-0 and fully covered in SP-2. (4) `identity-service` has no Dockerfile (pre-existing); the new shells/gateway include one — Identity's is out of SP-0 scope.
