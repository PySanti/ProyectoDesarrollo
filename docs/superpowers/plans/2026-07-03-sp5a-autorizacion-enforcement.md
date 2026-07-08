# SP-5a — Autorización enforcement: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cerrar la deuda de autorización SP-3a §12: política coarse-role por prefijo en el gateway YARP + permisos funcionales (`GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas`) como realm roles técnicos composite de Keycloak, enforceados dentro de Identity, Partidas y Operaciones de Sesión; re-homing de rutas Identity a la convención de prefijo y update mecánico de paths en mobile + frontend.

**Architecture:** Los permisos viajan en `realm_access.roles` del token (Keycloak expande composites automáticamente; cero mappers custom). Cada servicio normaliza roles con su copia de `KeycloakRoleClaims` y enforcea con policies `RequireRole("<permiso>")` + `FallbackPolicy = RequireAuthenticatedUser` (fail-secure). El gateway suma la policy `OperadorOAdministrador` y sub-rutas `/identity/users` / `/identity/teams`. Spec: `docs/superpowers/specs/2026-07-03-sp5a-autorizacion-enforcement-design.md`.

**Tech Stack:** .NET 8, YARP (config-only), Microsoft.AspNetCore.Authentication.JwtBearer 8.0.7, xUnit + WebApplicationFactory + TestAuthHandler, Keycloak realm import JSON, React/Vitest (frontend), React Native/node --test (mobile).

## Global Constraints

- Commits terminan EXACTAMENTE con el trailer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` (nada después).
- `git add` SOLO de archivos exactos listados en la tarea. PROHIBIDO `git add -A`, `git add .`, `git add <directorio>`.
- PROHIBIDO `git checkout`/`git restore`/`git clean`/`git stash`/`git reset` de rango amplio. Ante un árbol inesperado: reportar, no limpiar.
- Cero `DateTime.Now`/`DateTime.UtcNow` en `src/` (doctrina TimeProvider; esta feature no necesita tiempo).
- Controllers sin lógica de negocio; solo dispatch/atributos.
- Suites por servicio deben quedar verdes al cierre de cada tarea (los conteos esperados se indican por tarea). Si un test pre-existente falla por la nueva auth, la corrección legítima es agregar identidad/rol de test — nunca debilitar el assert.
- Nombres EXACTOS de permisos: `GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas` (case-sensitive en policies y claims).
- Paths de test que cambien: reemplazo literal completo; un path olvidado = 404 ruidoso, correr la suite siempre.

---

### Task 1: Realm Keycloak — roles técnicos composite + verificación

**Files:**
- Modify: `infra/keycloak/import/umbral-realm.json`
- Create: `scripts/check-realm-composites.py`
- Modify: `infra/keycloak/README.md`

**Interfaces:**
- Produces: realm con 6 realm roles (3 base + 3 técnicos); `Operador` composite→`GestionarPartidas`; `Participante` composite→`GestionarEquipos`+`ParticiparEnPartidas`. Los tokens emitidos tras re-import llevan base+permisos en `realm_access.roles` (los servicios de Tasks 2-6 dependen de esto en producción; en tests se simula).

- [ ] **Step 1: Escribir el script de verificación (test que falla)**

Crear `scripts/check-realm-composites.py`:

```python
#!/usr/bin/env python3
"""Verifica que el realm UMBRAL-UCAB defina los permisos funcionales como
realm roles técnicos composite de los roles base (SP-5a, ADR-0013)."""
import json
import sys

REALM = "infra/keycloak/import/umbral-realm.json"
BASE = {"Administrador", "Operador", "Participante"}
TECNICOS = {"GestionarPartidas", "GestionarEquipos", "ParticiparEnPartidas"}
COMPOSITES = {
    "Operador": {"GestionarPartidas"},
    "Participante": {"GestionarEquipos", "ParticiparEnPartidas"},
}

def fail(msg):
    print(f"FAIL: {msg}")
    sys.exit(1)

with open(REALM) as f:
    realm = json.load(f)

roles = {r["name"]: r for r in realm.get("roles", {}).get("realm", [])}

if set(roles) != BASE | TECNICOS:
    fail(f"roles realm = {sorted(roles)}; esperado {sorted(BASE | TECNICOS)}")

for base_role, expected in COMPOSITES.items():
    r = roles[base_role]
    if not r.get("composite"):
        fail(f"{base_role} no es composite")
    got = set(r.get("composites", {}).get("realm", []))
    if got != expected:
        fail(f"{base_role} composites = {sorted(got)}; esperado {sorted(expected)}")

if roles["Administrador"].get("composite"):
    fail("Administrador no debe ser composite (sus privilegios = rol base)")

for t in TECNICOS:
    if roles[t].get("composite"):
        fail(f"{t} debe ser rol simple")

for user in realm.get("users", []):
    directos = TECNICOS & set(user.get("realmRoles", []))
    if directos:
        fail(f"usuario {user.get('username')} tiene roles técnicos directos: {sorted(directos)}")

print("OK: composites de permisos funcionales correctos")
```

- [ ] **Step 2: Correrlo y verificar que falla**

Run: `python3 scripts/check-realm-composites.py` (desde la raíz del repo)
Expected: `FAIL: roles realm = ['Administrador', 'Operador', 'Participante']; esperado [...]` y exit code 1.

- [ ] **Step 3: Editar el realm JSON**

En `infra/keycloak/import/umbral-realm.json`, reemplazar el array `roles.realm` completo por:

```json
"realm": [
  {
    "name": "Administrador",
    "description": "Administra usuarios y configuración de la plataforma."
  },
  {
    "name": "Operador",
    "description": "Crea y supervisa partidas de Trivia y BDT.",
    "composite": true,
    "composites": { "realm": ["GestionarPartidas"] }
  },
  {
    "name": "Participante",
    "description": "Juega Trivia y BDT desde la app móvil.",
    "composite": true,
    "composites": { "realm": ["GestionarEquipos", "ParticiparEnPartidas"] }
  },
  {
    "name": "GestionarPartidas",
    "description": "Permiso funcional (rol técnico, no asignable a usuarios): gestionar y operar partidas."
  },
  {
    "name": "GestionarEquipos",
    "description": "Permiso funcional (rol técnico, no asignable a usuarios): gestionar equipos."
  },
  {
    "name": "ParticiparEnPartidas",
    "description": "Permiso funcional (rol técnico, no asignable a usuarios): participar en partidas."
  }
]
```

No tocar `users[*].realmRoles` (los usuarios siguen solo con roles base; los composites se expanden solos en el token).

- [ ] **Step 4: Verificar que pasa + JSON válido**

Run: `python3 scripts/check-realm-composites.py && python3 -m json.tool infra/keycloak/import/umbral-realm.json > /dev/null && echo JSON-OK`
Expected: `OK: composites de permisos funcionales correctos` + `JSON-OK`

- [ ] **Step 5: Documentar re-seed en `infra/keycloak/README.md`**

Agregar al final de la sección de import/re-seed existente (ubicarla con grep `import`):

```markdown
## Permisos funcionales (SP-5a, ADR-0013)

El realm define 3 realm roles técnicos — `GestionarPartidas`, `GestionarEquipos`,
`ParticiparEnPartidas` — asignados como **composite** de los roles base
(Operador → GestionarPartidas; Participante → GestionarEquipos + ParticiparEnPartidas).
Keycloak los expande automáticamente en `realm_access.roles` del token. No se asignan
a usuarios directamente.

**Entornos con el realm ya importado:** re-importar el realm (o crear los 3 roles
técnicos y sus composites a mano en la consola admin). Los tokens emitidos antes del
re-seed no llevan los permisos → 403 hasta re-login/refresh.

Verificación: `python3 scripts/check-realm-composites.py`
```

- [ ] **Step 6: Commit**

```bash
git add infra/keycloak/import/umbral-realm.json scripts/check-realm-composites.py infra/keycloak/README.md
git commit -m "feat(sp5a): permisos funcionales como realm roles composite en Keycloak

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Gateway — policy OperadorOAdministrador + matriz de rutas + tests

**Files:**
- Modify: `gateway/src/Umbral.Gateway/Program.cs`
- Modify: `gateway/src/Umbral.Gateway/appsettings.json`
- Create: `gateway/tests/Umbral.Gateway.IntegrationTests/TestAuthHandler.cs`
- Modify: `gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs`

**Interfaces:**
- Consumes: policies `Administrador`/`Operador`/`Participante` ya definidas en `Program.cs` (Task-independiente).
- Produces: policy `OperadorOAdministrador`; rutas `identity-users` (Administrador), `identity-teams` (Participante), `identity` resto (Default), `partidas` (OperadorOAdministrador). Los prefijos `/identity/users` y `/identity/teams` que Task 6 materializa en el servicio.

- [ ] **Step 1: Escribir los tests que fallan**

Crear `gateway/tests/Umbral.Gateway.IntegrationTests/TestAuthHandler.cs`:

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbral.Gateway.IntegrationTests;

/// <summary>
/// Esquema de test: autentica si llega "X-Test-Roles" (roles separados por coma).
/// Permite probar la matriz ruta→política (403 rol equivocado / paso con rol correcto)
/// sin token Keycloak real. Con rol correcto el proxy intenta el destino (muerto) →
/// el status resultante NO es 401 ni 403: esa es la señal de "política superada".
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Roles", out var rolesValue))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Test-Roles header"));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        foreach (var role in rolesValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
```

Agregar a `GatewayEndpointsTests.cs` (mismo archivo, después de los tests existentes; agregar usings `Microsoft.AspNetCore.Authentication;` y `Microsoft.AspNetCore.TestHost;` si faltan):

```csharp
    private HttpClient CreateClientWithRoles(string roles)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { })));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Roles", roles);
        return client;
    }

    private static void AssertPolicyPassed(HttpResponseMessage response)
    {
        // Destino del cluster muerto: si la política pasó, YARP intenta proxyar → 502/504,
        // nunca 401/403.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityUsers_sin_token_es_401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/identity/users/anything");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IdentityUsers_con_Participante_es_403_pin_de_precedencia()
    {
        // Si la sub-ruta /identity/users NO ganara sobre /identity/{**} (Default),
        // un Participante autenticado pasaría; el 403 pinnea la precedencia.
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/identity/users/anything");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityUsers_con_Administrador_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Administrador");
        var response = await client.GetAsync("/identity/users/anything");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task IdentityTeams_con_Operador_es_403()
    {
        var client = CreateClientWithRoles("Operador");
        var response = await client.GetAsync("/identity/teams/mine");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityTeams_con_Participante_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/identity/teams/mine");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task IdentityResto_autenticado_cualquier_rol_pasa()
    {
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/identity/otra-cosa");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task Partidas_con_Participante_es_403()
    {
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/partidas/anything");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Partidas_con_Administrador_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Administrador");
        var response = await client.GetAsync("/partidas/anything");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task Partidas_con_Operador_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Operador");
        var response = await client.GetAsync("/partidas/anything");
        AssertPolicyPassed(response);
    }
```

- [ ] **Step 2: Correr y verificar que fallan**

Run: `dotnet test gateway/Umbral.Gateway.sln --filter "IdentityUsers|IdentityTeams|IdentityResto|Partidas_con"`
Expected: FAIL — al menos: `IdentityUsers_con_Participante_es_403_pin_de_precedencia`, `IdentityTeams_con_Operador_es_403`, `Partidas_con_Participante_es_403` (hoy `/identity/{**}` es Default) y `Partidas_con_Administrador_pasa_la_politica` (hoy `/partidas` es solo `Operador` → 403 al admin). Los tests de 401 sin token pueden pasar ya — OK.

- [ ] **Step 3: Implementar — policy + rutas**

En `gateway/src/Umbral.Gateway/Program.cs`, reemplazar:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Administrador", p => p.RequireRole("Administrador"))
    .AddPolicy("Operador", p => p.RequireRole("Operador"))
    .AddPolicy("Participante", p => p.RequireRole("Participante"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
```

por:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Administrador", p => p.RequireRole("Administrador"))
    .AddPolicy("Operador", p => p.RequireRole("Operador"))
    .AddPolicy("Participante", p => p.RequireRole("Participante"))
    .AddPolicy("OperadorOAdministrador", p => p.RequireRole("Operador", "Administrador"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
```

En `gateway/src/Umbral.Gateway/appsettings.json`, reemplazar el objeto `Routes` completo por:

```json
"Routes": {
  "identity-users": {
    "ClusterId": "identity",
    "Order": 1,
    "Match": { "Path": "/identity/users/{**catch-all}" },
    "AuthorizationPolicy": "Administrador"
  },
  "identity-teams": {
    "ClusterId": "identity",
    "Order": 1,
    "Match": { "Path": "/identity/teams/{**catch-all}" },
    "AuthorizationPolicy": "Participante"
  },
  "identity": {
    "ClusterId": "identity",
    "Order": 2,
    "Match": { "Path": "/identity/{**catch-all}" },
    "AuthorizationPolicy": "Default"
  },
  "partidas": {
    "ClusterId": "partidas",
    "Match": { "Path": "/partidas/{**catch-all}" },
    "AuthorizationPolicy": "OperadorOAdministrador"
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
}
```

(`Clusters` queda intacto. La ruta `puntuaciones` queda Default — política fina diferida post-SP-4 per spec §9.)

- [ ] **Step 4: Correr toda la suite del gateway**

Run: `dotnet test gateway/Umbral.Gateway.sln`
Expected: PASS — los 5 tests pre-existentes + los 9 nuevos.

- [ ] **Step 5: Commit**

```bash
git add gateway/src/Umbral.Gateway/Program.cs gateway/src/Umbral.Gateway/appsettings.json gateway/tests/Umbral.Gateway.IntegrationTests/TestAuthHandler.cs gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs
git commit -m "feat(sp5a): matriz coarse-role por prefijo en gateway + policy OperadorOAdministrador

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Operaciones — normalizador de roles + policies + fallback + TestAuth con roles (suite verde, sin atributos aún)

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Utils/KeycloakRoleClaims.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/HealthController.cs`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/TestAuthHandler.cs`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/OperacionesSesionWebFactory.cs`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/SesionEndpointsTests.cs` (o archivo de tests nuevo pequeño — ver Step 1)

**Interfaces:**
- Produces: policies `GestionarPartidas` y `ParticiparEnPartidas` registradas + fallback autenticado (Task 4 les pone atributos); `TestAuthHandler` acepta header opcional `X-Test-Roles` (default: ambos permisos) y `OperacionesSesionWebFactory.CreateClientAs(Guid, string roles)` overload (Task 4 lo usa para los 403).

- [ ] **Step 1: Tests que fallan — policies registradas + fallback fail-secure**

Agregar al final de `SesionEndpointsTests.cs` (usings nuevos: `Microsoft.AspNetCore.Authorization;`, `Microsoft.AspNetCore.Authorization.Infrastructure;`, `Microsoft.Extensions.DependencyInjection;`):

```csharp
    [Fact]
    public async Task Politicas_de_permisos_funcionales_estan_registradas()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        var gestionar = await provider.GetPolicyAsync("GestionarPartidas");
        var participar = await provider.GetPolicyAsync("ParticiparEnPartidas");

        Assert.NotNull(gestionar);
        Assert.Contains(gestionar!.Requirements, r =>
            r is RolesAuthorizationRequirement roles && roles.AllowedRoles.Contains("GestionarPartidas"));
        Assert.NotNull(participar);
        Assert.Contains(participar!.Requirements, r =>
            r is RolesAuthorizationRequirement roles && roles.AllowedRoles.Contains("ParticiparEnPartidas"));
    }

    [Fact]
    public async Task Fallback_policy_es_fail_secure()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        var fallback = await provider.GetFallbackPolicyAsync();

        Assert.NotNull(fallback);
        Assert.Contains(fallback!.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }
```

(Si `SesionEndpointsTests` no tiene `_factory` accesible con `.Services`, poner ambos tests en un archivo nuevo `AutorizacionInfraTests.cs` con `IClassFixture<OperacionesSesionWebFactory>`.)

- [ ] **Step 2: Verificar que fallan**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests --filter "Politicas_de_permisos|Fallback_policy"`
Expected: FAIL — `GetPolicyAsync` devuelve null y fallback es null.

- [ ] **Step 3: Crear el normalizador**

Crear `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Utils/KeycloakRoleClaims.cs` (copia del patrón Identity/gateway):

```csharp
using System.Security.Claims;
using System.Text.Json;

namespace Umbral.OperacionesSesion.Api.Utils;

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
    {
        return KnownRoles.TryGetValue(role.Trim(), out var normalized) ? normalized : role.Trim();
    }

    private static IEnumerable<string> ReadRealmRoles(ClaimsIdentity identity)
    {
        var realmAccessClaim = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccessClaim))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(realmAccessClaim);
            return document.RootElement.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array
                ? rolesElement.EnumerateArray().Select(role => role.GetString()).OfType<string>().ToArray()
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IEnumerable<string> ReadClientRoles(ClaimsIdentity identity)
    {
        var resourceAccessClaim = identity.FindFirst("resource_access")?.Value;
        if (string.IsNullOrWhiteSpace(resourceAccessClaim))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(resourceAccessClaim);
            return document.RootElement.EnumerateObject()
                .SelectMany(client => client.Value.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array
                    ? rolesElement.EnumerateArray().Select(role => role.GetString()).OfType<string>()
                    : [])
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
```

- [ ] **Step 4: Wirear Program.cs**

En `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs`:

(a) Agregar usings arriba (junto a los existentes):

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Umbral.OperacionesSesion.Api.Utils;
```

(b) En el objeto `JwtBearerEvents` existente (que hoy solo tiene `OnMessageReceived`), agregar `OnTokenValidated` — reemplazar:

```csharp
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
```

por:

```csharp
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                    {
                        KeycloakRoleClaims.AddRolesFromKeycloakClaims(identity);
                    }
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
```

(c) Reemplazar `builder.Services.AddAuthorization();` por:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("GestionarPartidas", p => p.RequireRole("GestionarPartidas"))
    .AddPolicy("ParticiparEnPartidas", p => p.RequireRole("ParticiparEnPartidas"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
```

(d) En `Controllers/HealthController.cs`: agregar `using Microsoft.AspNetCore.Authorization;` y el atributo `[AllowAnonymous]` sobre la clase (junto a `[Route("health")]`).

- [ ] **Step 5: TestAuthHandler con roles (default = ambos permisos)**

Reemplazar el cuerpo de `HandleAuthenticateAsync` en `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/TestAuthHandler.cs`:

```csharp
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Sub", out var subValue) ||
            string.IsNullOrWhiteSpace(subValue.ToString()))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Test-Sub header"));
        }

        var sub = subValue.ToString();

        // Roles simulados: por defecto ambos permisos funcionales (como un token que los
        // trae por composites); los tests de 403 mandan "X-Test-Roles" explícito y acotado.
        var roles = Request.Headers.TryGetValue("X-Test-Roles", out var rolesValue)
            ? rolesValue.ToString()
            : "GestionarPartidas,ParticiparEnPartidas";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sub),
            new("sub", sub)
        };
        foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
```

Y en `OperacionesSesionWebFactory.cs` agregar el overload debajo del `CreateClientAs` existente:

```csharp
    public HttpClient CreateClientAs(Guid participanteId, string roles)
    {
        var client = CreateClientAs(participanteId);
        client.DefaultRequestHeaders.Add("X-Test-Roles", roles);
        return client;
    }
```

- [ ] **Step 6: Suite completa verde**

Run: `dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln` (si el .sln tiene otro nombre, localizarlo con `ls services/operaciones-sesion/*.sln`)
Expected: PASS — Unit 357, Integration 29, Contract 48+2 nuevos = 50. Si algún contract test pre-existente golpea un endpoint SIN `X-Test-Sub` esperando algo distinto de 401: ese test dependía de acceso anónimo — agregarle el header de identidad de test (no debilitar la nueva auth). Único endpoint legítimamente anónimo: `/health`.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Utils/KeycloakRoleClaims.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/HealthController.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/TestAuthHandler.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/OperacionesSesionWebFactory.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/SesionEndpointsTests.cs
git commit -m "feat(sp5a): normalizador de roles Keycloak + policies de permisos en Operaciones

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

(Si Step 1 creó `AutorizacionInfraTests.cs`, incluirlo en el `git add` en lugar de `SesionEndpointsTests.cs`.)

---

### Task 4: Operaciones — matriz [Authorize] por endpoint + tests 403

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs`
- Create: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/AutorizacionContractTests.cs`

**Interfaces:**
- Consumes: policies `GestionarPartidas`/`ParticiparEnPartidas` (Task 3), `CreateClientAs(Guid, string roles)` (Task 3), `Rutas.Base` (existente).

- [ ] **Step 1: Tests 403 que fallan**

Crear `AutorizacionContractTests.cs`:

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Umbral.OperacionesSesion.ContractTests;

/// <summary>
/// Matriz de permisos funcionales (SP-5a): endpoints de operador exigen GestionarPartidas,
/// endpoints de participante exigen ParticiparEnPartidas, GETs compartidos aceptan cualquiera.
/// </summary>
public class AutorizacionContractTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;

    public AutorizacionContractTests(OperacionesSesionWebFactory factory) => _factory = factory;

    [Theory]
    [InlineData("POST", "/partidas/{id}/publicacion")]
    [InlineData("POST", "/partidas/{id}/inicio")]
    [InlineData("POST", "/partidas/{id}/inicio-automatico")]
    [InlineData("POST", "/partidas/{id}/juego-actual/finalizacion")]
    [InlineData("POST", "/partidas/{id}/pregunta-actual/avance")]
    [InlineData("POST", "/partidas/{id}/etapa-actual/avance")]
    [InlineData("POST", "/partidas/{id}/pistas")]
    public async Task Endpoint_de_operador_sin_GestionarPartidas_es_403(string method, string template)
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), "ParticiparEnPartidas");
        var url = Rutas.Base + template.Replace("{id}", Guid.NewGuid().ToString());

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = JsonContent.Create(new { })
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/partidas/{id}/inscripciones")]
    [InlineData("DELETE", "/partidas/{id}/inscripciones/mia")]
    [InlineData("POST", "/partidas/{id}/inscripciones-equipo")]
    [InlineData("DELETE", "/partidas/{id}/inscripciones-equipo/mia")]
    [InlineData("POST", "/convocatorias/{id}/aceptacion")]
    [InlineData("POST", "/convocatorias/{id}/rechazo")]
    [InlineData("POST", "/partidas/{id}/pregunta-actual/respuesta")]
    [InlineData("POST", "/partidas/{id}/etapa-actual/tesoro")]
    [InlineData("GET", "/mi-sesion")]
    [InlineData("GET", "/mis-convocatorias")]
    public async Task Endpoint_de_participante_sin_ParticiparEnPartidas_es_403(string method, string template)
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), "GestionarPartidas");
        var url = Rutas.Base + template.Replace("{id}", Guid.NewGuid().ToString());

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "GET" ? null : JsonContent.Create(new { })
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GestionarPartidas")]
    [InlineData("ParticiparEnPartidas")]
    public async Task GET_compartido_acepta_cualquier_permiso(string roles)
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), roles);

        var lobby = await client.GetAsync($"{Rutas.Base}/partidas/{Guid.NewGuid()}/lobby");
        var estado = await client.GetAsync($"{Rutas.Base}/partidas/{Guid.NewGuid()}/estado");

        // Autorizado (la partida no existe → 404 de dominio, jamás 403/401).
        Assert.NotEqual(HttpStatusCode.Forbidden, lobby.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, lobby.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, estado.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, estado.StatusCode);
    }

    [Fact]
    public async Task Sin_identidad_es_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{Rutas.Base}/mi-sesion");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 2: Verificar que fallan**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests --filter "AutorizacionContractTests"`
Expected: FAIL — los 403 esperados devuelven otra cosa (sin atributos, el fallback ya autentica y el handler procesa).

- [ ] **Step 3: Poner atributos en `SesionesController.cs`**

`using Microsoft.AspNetCore.Authorization;` arriba. Atributo por acción, ENCIMA de su `[HttpX]`:

`[Authorize(Policy = "GestionarPartidas")]` sobre las acciones de:
- `POST partidas/{partidaId:guid}/publicacion`
- `POST partidas/{partidaId:guid}/inicio`
- `POST partidas/{partidaId:guid}/inicio-automatico`
- `POST partidas/{partidaId:guid}/juego-actual/finalizacion`
- `POST partidas/{partidaId:guid}/pregunta-actual/avance`
- `POST partidas/{partidaId:guid}/etapa-actual/avance`
- `POST partidas/{partidaId:guid}/pistas`

`[Authorize(Policy = "ParticiparEnPartidas")]` sobre las acciones de:
- `POST partidas/{partidaId:guid}/inscripciones`
- `DELETE partidas/{partidaId:guid}/inscripciones/mia`
- `POST partidas/{partidaId:guid}/inscripciones-equipo`
- `DELETE partidas/{partidaId:guid}/inscripciones-equipo/mia`
- `POST convocatorias/{convocatoriaId:guid}/aceptacion`
- `POST convocatorias/{convocatoriaId:guid}/rechazo`
- `POST partidas/{partidaId:guid}/pregunta-actual/respuesta`
- `POST partidas/{partidaId:guid}/etapa-actual/tesoro`
- `GET mi-sesion`
- `GET mis-convocatorias`

SIN atributo (fallback autenticado — compartidos):
- `GET partidas/{partidaId:guid}/lobby`
- `GET partidas/{partidaId:guid}/estado`
- `GET partidas/{partidaId:guid}/pregunta-actual`
- `GET partidas/{partidaId:guid}/etapa-actual`

Verificación de completitud: 21 acciones en el controller → 7 + 10 con atributo, 4 sin. `grep -c "Authorize(Policy" SesionesController.cs` = 17.

- [ ] **Step 4: Suite completa verde**

Run: `dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln`
Expected: PASS — contract 50 + 20 nuevos (7+10+2+1) = 70; Unit 357; Integration 29. Los controller unit tests existentes (21/21) instancian el controller directo → los atributos no los afectan.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/AutorizacionContractTests.cs
git commit -m "feat(sp5a): matriz de permisos funcionales por endpoint en Operaciones

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Partidas — JWT completo + permisos + tests + GUIA

**Files:**
- Modify: `services/partidas/src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj`
- Modify: `services/partidas/src/Umbral.Partidas.Api/Program.cs`
- Create: `services/partidas/src/Umbral.Partidas.Api/Utils/KeycloakRoleClaims.cs`
- Modify: `services/partidas/src/Umbral.Partidas.Api/Controllers/PartidasController.cs`
- Modify: `services/partidas/src/Umbral.Partidas.Api/Controllers/HealthController.cs`
- Create: `services/partidas/tests/Umbral.Partidas.ContractTests/TestAuthHandler.cs`
- Create: `services/partidas/tests/Umbral.Partidas.ContractTests/PartidasWebFactory.cs`
- Create: `services/partidas/tests/Umbral.Partidas.ContractTests/AutorizacionContractTests.cs`
- Modify: `services/partidas/tests/Umbral.Partidas.ContractTests/PartidasConfigEndpointsTests.cs`
- Modify: `GUIA-LEVANTAMIENTO.md`

**Interfaces:**
- Consumes: nada de otras tareas (patrón espejo de Operaciones/Task 3).
- Produces: `GET /partidas/{id}` sigue accesible con token de participante (dependencia viva: `IConfiguracionPartidaClient` de Operaciones reenvía el bearer del caller — SP-3a §12).

- [ ] **Step 1: Paquete JwtBearer**

En `Umbral.Partidas.Api.csproj`, dentro del `<ItemGroup>` de PackageReference:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.7" />
```

- [ ] **Step 2: Crear `Utils/KeycloakRoleClaims.cs`**

Contenido IDÉNTICO al de Task 3 Step 3 salvo la línea de namespace:

```csharp
namespace Umbral.Partidas.Api.Utils;
```

(El resto del archivo — `KnownRoles`, `AddRolesFromKeycloakClaims`, `NormalizeRole`, `ReadRealmRoles`, `ReadClientRoles` — copiar textual del bloque de Task 3 Step 3. Copia por servicio es el patrón establecido: gateway e Identity ya duplican este archivo; los servicios no comparten proyectos.)

- [ ] **Step 3: Infra de test que falla**

Crear `services/partidas/tests/Umbral.Partidas.ContractTests/TestAuthHandler.cs`:

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbral.Partidas.ContractTests;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Sub", out var subValue) ||
            string.IsNullOrWhiteSpace(subValue.ToString()))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Test-Sub header"));
        }

        var sub = subValue.ToString();
        var roles = Request.Headers.TryGetValue("X-Test-Roles", out var rolesValue)
            ? rolesValue.ToString()
            : "GestionarPartidas";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sub),
            new("sub", sub)
        };
        foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
```

Crear `services/partidas/tests/Umbral.Partidas.ContractTests/PartidasWebFactory.cs`:

```csharp
using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Umbral.Partidas.ContractTests;

public sealed class PartidasWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateClientAs(Guid userId, string? roles = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", userId.ToString());
        if (roles is not null)
        {
            client.DefaultRequestHeaders.Add("X-Test-Roles", roles);
        }
        return client;
    }
}
```

Crear `services/partidas/tests/Umbral.Partidas.ContractTests/AutorizacionContractTests.cs`:

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Umbral.Partidas.ContractTests;

/// <summary>
/// SP-5a: mutaciones de configuración exigen GestionarPartidas; GET /partidas/{id}
/// solo exige autenticación (Operaciones reenvía el token del participante — SP-3a §12).
/// </summary>
public class AutorizacionContractTests : IClassFixture<PartidasWebFactory>
{
    private readonly PartidasWebFactory _factory;

    public AutorizacionContractTests(PartidasWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Sin_token_es_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/partidas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/partidas")]
    [InlineData("POST", "/partidas/{id}/juegos/trivia")]
    [InlineData("POST", "/partidas/{id}/juegos/bdt")]
    public async Task Mutacion_sin_GestionarPartidas_es_403(string method, string template)
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), "ParticiparEnPartidas");
        var url = template.Replace("{id}", Guid.NewGuid().ToString());

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = JsonContent.Create(new { })
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_partida_con_token_de_participante_pasa()
    {
        // Pin de la llamada interna Operaciones→Partidas con el bearer del participante.
        var client = _factory.CreateClientAs(Guid.NewGuid(), "ParticiparEnPartidas");

        var response = await client.GetAsync($"/partidas/{Guid.NewGuid()}");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Health_es_anonimo()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 4: Verificar que fallan**

Run: `dotnet test services/partidas/tests/Umbral.Partidas.ContractTests --filter "AutorizacionContractTests"`
Expected: FAIL — `Sin_token_es_401` y los 403 fallan (hoy no hay auth: todo 2xx/404).

- [ ] **Step 5: Reemplazar `Program.cs` completo**

`services/partidas/src/Umbral.Partidas.Api/Program.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Umbral.Partidas.Api.Middleware;
using Umbral.Partidas.Api.Utils;
using Umbral.Partidas.Application;
using Umbral.Partidas.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPartidasApplication();
builder.Services.AddPartidasInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

static string? ResolveSetting(IConfiguration configuration, string key, string environmentVariable)
{
    var configuredValue = configuration[key];
    if (!string.IsNullOrWhiteSpace(configuredValue))
    {
        return configuredValue;
    }

    return Environment.GetEnvironmentVariable(environmentVariable);
}

var keycloakBaseUrl = ResolveSetting(builder.Configuration, "Keycloak:BaseUrl", "KEYCLOAK_BASE_URL");
var keycloakRealm = ResolveSetting(builder.Configuration, "Keycloak:Realm", "KEYCLOAK_REALM");
var keycloakClientId = ResolveSetting(builder.Configuration, "Keycloak:ClientId", "KEYCLOAK_CLIENT_ID");
var keycloakValidAudiencesRaw = ResolveSetting(builder.Configuration, "Keycloak:ValidAudiences", "KEYCLOAK_VALID_AUDIENCES");
var keycloakValidIssuersRaw = ResolveSetting(builder.Configuration, "Keycloak:ValidIssuers", "KEYCLOAK_VALID_ISSUERS");

if (!string.IsNullOrWhiteSpace(keycloakBaseUrl) &&
    !string.IsNullOrWhiteSpace(keycloakRealm) &&
    (!string.IsNullOrWhiteSpace(keycloakClientId) || !string.IsNullOrWhiteSpace(keycloakValidAudiencesRaw)))
{
    var authority = $"{keycloakBaseUrl.TrimEnd('/')}/realms/{keycloakRealm}";

    var validIssuers = (keycloakValidIssuersRaw ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
    if (!validIssuers.Contains(authority))
    {
        validIssuers.Add(authority);
    }

    var validAudiences = (keycloakValidAudiencesRaw ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
    if (!string.IsNullOrWhiteSpace(keycloakClientId) && !validAudiences.Contains(keycloakClientId))
    {
        validAudiences.Add(keycloakClientId);
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = keycloakClientId;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                    {
                        KeycloakRoleClaims.AddRolesFromKeycloakClaims(identity);
                    }
                    return Task.CompletedTask;
                }
            };
        });
}
else
{
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("GestionarPartidas", p => p.RequireRole("GestionarPartidas"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
```

- [ ] **Step 6: Atributos en controllers**

`PartidasController.cs`: `using Microsoft.AspNetCore.Authorization;` + `[Authorize(Policy = "GestionarPartidas")]` sobre cada una de las TRES acciones POST (`CrearPartida`, `AgregarJuegoTrivia`, `AgregarJuegoBdt`). La acción `GetPartida` queda SIN atributo (fallback autenticado).
`HealthController.cs`: `using Microsoft.AspNetCore.Authorization;` + `[AllowAnonymous]` sobre la clase.

- [ ] **Step 7: Migrar tests de contrato existentes a la factory**

En `PartidasConfigEndpointsTests.cs`, reemplazar:

```csharp
public class PartidasConfigEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PartidasConfigEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
```

por:

```csharp
public class PartidasConfigEndpointsTests : IClassFixture<PartidasWebFactory>
{
    private readonly HttpClient _client;

    public PartidasConfigEndpointsTests(PartidasWebFactory factory)
    {
        _client = factory.CreateClientAs(Guid.NewGuid());
    }
```

(`CreateClientAs` sin roles → default `GestionarPartidas` → el flujo de configuración completo sigue pasando.) `HealthContractTests.cs`: si usa `WebApplicationFactory<Program>` anónima, déjala — `/health` es anónimo.

- [ ] **Step 8: Suite completa verde**

Run: `dotnet test services/partidas/Umbral.Partidas.sln` (localizar nombre exacto con `ls services/partidas/*.sln`)
Expected: PASS — Unit/Integration intactos (persistencia y controllers directos no tocan HTTP auth); Contract = existentes + 6 nuevos.

- [ ] **Step 9: Documentar env vars en `GUIA-LEVANTAMIENTO.md`**

En la sección de Partidas (localizar con grep `partidas`), agregar las variables (mismo bloque que Operaciones):

```
KEYCLOAK_BASE_URL=http://localhost:8080
KEYCLOAK_REALM=UMBRAL-UCAB
KEYCLOAK_VALID_AUDIENCES=umbral-web,umbral-mobile,account
KEYCLOAK_VALID_ISSUERS=http://localhost:8080/realms/UMBRAL-UCAB
```

con la nota: "Desde SP-5a, Partidas valida JWT y exige el permiso `GestionarPartidas` en mutaciones; sin estas vars el servicio arranca sin validación JWT real (solo apto para tests)."

- [ ] **Step 10: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj services/partidas/src/Umbral.Partidas.Api/Program.cs services/partidas/src/Umbral.Partidas.Api/Utils/KeycloakRoleClaims.cs services/partidas/src/Umbral.Partidas.Api/Controllers/PartidasController.cs services/partidas/src/Umbral.Partidas.Api/Controllers/HealthController.cs services/partidas/tests/Umbral.Partidas.ContractTests/TestAuthHandler.cs services/partidas/tests/Umbral.Partidas.ContractTests/PartidasWebFactory.cs services/partidas/tests/Umbral.Partidas.ContractTests/AutorizacionContractTests.cs services/partidas/tests/Umbral.Partidas.ContractTests/PartidasConfigEndpointsTests.cs GUIA-LEVANTAMIENTO.md
git commit -m "feat(sp5a): JWT Keycloak + permiso GestionarPartidas en Partidas

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Identity — re-homing de rutas + swap GestionarEquipos + composites en TestAuth

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/UsersController.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsController.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamInvitationsController.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs`
- Modify: `services/identity-service/tests/Umbral.IdentityService.ContractTests/TestAuthHandler.cs`
- Modify: `services/identity-service/tests/Umbral.IdentityService.IntegrationTests/TestAuthHandler.cs`
- Modify: ~10 archivos de test con 66 literales de path (lista exacta vía grep en Step 4)
- Create: test 403 en `services/identity-service/tests/Umbral.IdentityService.ContractTests/Teams/TeamsContractTests.cs` (agregar al archivo)

**Interfaces:**
- Consumes: prefijos `/identity/users` y `/identity/teams` definidos en el gateway (Task 2).
- Produces: rutas nuevas que Task 7 usa en los clientes (`identity/users`, `identity/teams`).

- [ ] **Step 1: TestAuthHandlers simulan expansión composite (primero, para que el swap no rompa)**

En AMBOS `TestAuthHandler.cs` (ContractTests e IntegrationTests), reemplazar la construcción de claims:

```csharp
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId),
            new Claim(ClaimTypes.Role, roleValue.ToString())
        };
```

por:

```csharp
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new(ClaimTypes.Role, roleValue.ToString())
        };
        // Simula la expansión composite de Keycloak (SP-5a): el token de un rol base
        // trae también sus permisos funcionales.
        if (ComposedPermissions.TryGetValue(roleValue.ToString(), out var permisos))
        {
            claims.AddRange(permisos.Select(p => new Claim(ClaimTypes.Role, p)));
        }
```

y agregar el campo dentro de la clase:

```csharp
    private static readonly Dictionary<string, string[]> ComposedPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Operador"] = ["GestionarPartidas"],
        ["Participante"] = ["GestionarEquipos", "ParticiparEnPartidas"]
    };
```

(using `System.Linq;` si falta.)

- [ ] **Step 2: Test 403 que falla**

Agregar a `Teams/TeamsContractTests.cs` (siguiendo el estilo del archivo para crear el client — usa el patrón X-Test-Role/X-Test-UserId existente):

```csharp
    [Fact]
    public async Task Teams_con_rol_sin_GestionarEquipos_es_403()
    {
        // Operador autenticado pero sin el permiso GestionarEquipos.
        var client = CreateClient(); // usar el helper del archivo; si recibe rol, pasar "Operador"
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Operador");

        var response = await client.GetAsync("identity/teams/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
```

(Adaptar la creación del client al helper real del archivo — leerlo primero; el assert y el path son lo normativo. Si el endpoint "mine" tiene otro nombre real, usar el primer GET del controller de Teams.)

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.ContractTests --filter "Teams_con_rol_sin_GestionarEquipos"`
Expected: FAIL — hoy devuelve 404 (ruta `identity/teams` no existe aún) o 403-por-ParticipantOnly con semántica vieja; registrar el estado.

- [ ] **Step 3: Re-homing + swap de política**

- `UsersController.cs`: `[Route("api/identity/users")]` → `[Route("identity/users")]`
- `TeamsController.cs`: `[Route("api/teams")]` → `[Route("identity/teams")]`
- `TeamInvitationsController.cs`: `[Route("api/teams")]` → `[Route("identity/teams")]`
- `TeamsController.cs` y `TeamInvitationsController.cs`: `[Authorize(Policy = "ParticipantOnly")]` → `[Authorize(Policy = "GestionarEquipos")]`
- `Program.cs`: reemplazar

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("ParticipantOnly", policy => policy.RequireRole("Participante"));
});
```

por:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("GestionarEquipos", policy => policy.RequireRole("GestionarEquipos"));
});
```

Luego `grep -rn "ParticipantOnly" services/identity-service/src services/identity-service/tests --include="*.cs" | grep -v bin | grep -v obj` — si queda alguna referencia (p.ej. en tests que asserten el nombre de policy), actualizarla a `GestionarEquipos`.

- [ ] **Step 4: Churn de paths en tests (66 literales)**

```bash
grep -rln 'api/identity/users\|api/teams' services/identity-service/tests --include="*.cs" | grep -v bin | grep -v obj | xargs sed -i 's|api/identity/users|identity/users|g; s|api/teams|identity/teams|g'
grep -rn 'api/identity/users\|api/teams' services/identity-service/tests --include="*.cs" | grep -v bin | grep -v obj
```

Expected: segunda línea sin output (0 literales viejos).

- [ ] **Step 5: Suite completa verde**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln` (localizar con `ls services/identity-service/*.sln`)
Expected: PASS completo (referencia SP-1R: 205 unit + 37 integration + 26 contract; registrar conteo real + el test nuevo). Un 404 en cualquier test = path olvidado; volver a Step 4.

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Api/Controllers/UsersController.cs services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsController.cs services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamInvitationsController.cs services/identity-service/src/Umbral.IdentityService.Api/Program.cs services/identity-service/tests/Umbral.IdentityService.ContractTests/TestAuthHandler.cs services/identity-service/tests/Umbral.IdentityService.IntegrationTests/TestAuthHandler.cs
git add <cada archivo de test tocado en Steps 2 y 4 — listar exactos desde git status, uno por uno>
git commit -m "feat(sp5a): re-homing de rutas Identity a prefijo + permiso GestionarEquipos

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: Clientes — paths mobile + frontend al nuevo prefijo

**Files:**
- Modify: `mobile/src/features/teams/createTeamApi.js` (línea 4: `/api/teams` → `/identity/teams`)
- Modify: `mobile/src/features/teams/transferLeadershipApi.js` (línea 4: `/api/teams/leadership` → `/identity/teams/leadership`)
- Modify: `mobile/src/features/teams/inviteMemberApi.js` (líneas 4 y 41)
- Modify: `mobile/src/features/teams/invitationsApi.js` (líneas 4, 37, 82)
- Modify: `mobile/src/features/teams/leaveTeamApi.js` (línea 4)
- Modify: `mobile/tests/transferLeadershipFlow.test.js`, `mobile/tests/leaveTeamFlow.test.js`, `mobile/tests/inviteMemberFlow.test.js`, `mobile/tests/invitationsFlow.test.js` (asserts de URL)
- Modify: `frontend/src/api/identityApi.ts` (5 literales `/api/identity/users` → `/identity/users`: líneas 62, 107, 122, 138, 155)
- Modify: `frontend/src/features/identity/CreateUserPage.test.tsx`, `frontend/src/features/identity/UserManagementPage.test.tsx`, `frontend/src/app/App.test.tsx` (si contienen paths — grep primero)

**Interfaces:**
- Consumes: rutas re-homed de Task 6 (`identity/users`, `identity/teams`). Las base URLs (`EXPO_PUBLIC_IDENTITY_API_BASE_URL`, `VITE_IDENTITY_API_BASE_URL`) NO cambian.

- [ ] **Step 1: Reemplazo mecánico**

```bash
grep -rln '/api/teams' mobile/src mobile/tests | xargs sed -i 's|/api/teams|/identity/teams|g'
grep -rln '/api/identity/users' frontend/src | xargs sed -i 's|/api/identity/users|/identity/users|g'
grep -rn '/api/teams\|/api/identity/users' mobile/src mobile/tests frontend/src
```

Expected: última línea sin output.

- [ ] **Step 2: Suites**

Run: `cd mobile && npm test && npm run typecheck`
Expected: PASS (94+ tests) + typecheck limpio.

Run: `cd frontend && npm test`
Expected: PASS (vitest run completo).

- [ ] **Step 3: Commit**

```bash
git add mobile/src/features/teams/createTeamApi.js mobile/src/features/teams/transferLeadershipApi.js mobile/src/features/teams/inviteMemberApi.js mobile/src/features/teams/invitationsApi.js mobile/src/features/teams/leaveTeamApi.js mobile/tests/transferLeadershipFlow.test.js mobile/tests/leaveTeamFlow.test.js mobile/tests/inviteMemberFlow.test.js mobile/tests/invitationsFlow.test.js frontend/src/api/identityApi.ts
git add <tests frontend tocados si el grep del Step 1 los modificó — exactos desde git status>
git commit -m "feat(sp5a): clientes mobile y web al prefijo identity re-homed

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 8: Documentación — ADR-0013 + contratos + gateway-context + traceability

**Files:**
- Create: `docs/05-decisions/ADR-0013-permisos-funcionales-composites-keycloak.md`
- Modify: `contracts/http/identity-api.md` (paths re-homed + auth por endpoint)
- Modify: `contracts/http/partidas-config.md` y/o `contracts/http/partidas-api.md` (auth por endpoint — el que registre los endpoints activos)
- Modify: `contracts/http/operaciones-sesion-api.md` (auth por endpoint)
- Modify: `contracts/http/gateway-api.md` (matriz de rutas/políticas)
- Modify: `gateway/gateway-context.md` (matriz)
- Modify: `docs/04-sdd/traceability-matrix.md` (fila SP-5a)

**Interfaces:**
- Consumes: todo lo materializado en Tasks 1-7 (hashes de commits desde `git log --oneline`).

- [ ] **Step 1: ADR-0013**

Crear `docs/05-decisions/ADR-0013-permisos-funcionales-composites-keycloak.md`:

```markdown
# ADR-0013 — Permisos funcionales como realm roles técnicos composite en Keycloak

- **Estado:** Aceptado (2026-07-03, SP-5a)
- **Contexto:** CLAUDE.md manda que el token lleve rol base y permisos funcionales
  (`GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas`), que el gateway autorice
  coarse por rol sin consultar Identity, y que la autorización fina por permiso viva en cada
  microservicio. BR-R02/R03 definen permisos gestionados POR ROL con defaults fijos. No existía
  representación de permisos en el token (cero mappers).
- **Decisión:** Cada permiso funcional es un **realm role técnico** de Keycloak, asignado como
  **composite** de los roles base según BR-R03 (Operador→GestionarPartidas;
  Participante→GestionarEquipos+ParticiparEnPartidas; Administrador→ninguno: sus privilegios de
  gobernanza son el rol base, protegidos). Keycloak expande composites en `realm_access.roles`
  automáticamente — sin mappers custom. Los servicios enforcean con policies
  `RequireRole("<permiso>")` tras normalizar claims (`KeycloakRoleClaims`).
- **Los roles técnicos NO son roles de usuario.** La regla del dominio "no se crean roles
  nuevos" refiere a roles base de negocio; los 3 roles base siguen siendo los únicos asignables
  a usuarios. `scripts/check-realm-composites.py` verifica que ningún usuario tenga roles
  técnicos directos.
- **Gobernanza (SP-5b):** Identity persistirá las asignaciones permiso↔rol en su DB (panel +
  auditoría) y propagará cambios a Keycloak vía Admin API (add/remove composite) — mismo patrón
  "propagado a Keycloak" que el cambio de rol. Cambios efectivos al siguiente refresh del token.
- **Alternativas descartadas:** (B) Identity DB + eventos RabbitMQ + cache por servicio — token
  sin permisos contradice la directiva; alto costo transversal. (C) consulta HTTP a Identity con
  cache TTL — acoplamiento runtime y latencia.
- **Consecuencias:** los tokens emitidos antes del re-seed del realm no llevan permisos (403
  hasta refresh); la revocación de un permiso a un rol tarda hasta el TTL del token; el realm
  import es fuente de defaults, la gobernanza dinámica llega en SP-5b.
```

- [ ] **Step 2: Contratos HTTP**

- `identity-api.md`: reemplazar TODOS los paths `api/identity/users` → `identity/users` y `api/teams` → `identity/teams`; agregar (o completar) por endpoint el requisito de auth: users → rol `Administrador`; teams/invitations → permiso `GestionarEquipos` (401 sin token, 403 sin permiso).
- `partidas-config.md` (y `partidas-api.md` si registra endpoints activos): mutaciones → permiso `GestionarPartidas`; `GET /partidas/{id}` → autenticado (cualquier rol; usado por Operaciones con el token del caller).
- `operaciones-sesion-api.md`: sección "Autorización (SP-5a)" con la matriz de la spec §5.2 (7 endpoints GestionarPartidas / 10 ParticiparEnPartidas / 4 GET autenticado / health anónimo), y por endpoint una línea de auth si el formato del archivo ya es por-endpoint.
- `gateway-api.md`: matriz de rutas de la spec §4 (6 rutas con Order y política; puntuaciones Default con nota "política fina diferida post-SP-4").

Regla: seguir el formato ya existente de cada archivo; no inventar payloads; pipes internos en tablas escapados `\|` si la celda contiene texto libre con pipe.

- [ ] **Step 3: `gateway/gateway-context.md`**

Actualizar la descripción de rutas con la misma matriz (mantener el estilo del archivo).

- [ ] **Step 4: Fila de traceability**

En `docs/04-sdd/traceability-matrix.md`, agregar la fila SP-5a siguiendo el formato de la fila SP-3i (misma cantidad de columnas; pipes internos escapados `\|`): spec `2026-07-03-sp5a-autorizacion-enforcement-design.md` (988a366 + enmienda a53d69e), plan `2026-07-03-sp5a-autorizacion-enforcement.md`, commits de Tasks 1-8 (hashes de `git log --oneline` del rango del slice), suites (gateway, Operaciones, Partidas, Identity, mobile, frontend) con conteos reales.

- [ ] **Step 5: Verificación**

```bash
grep -rn 'api/identity/users\|api/teams' contracts/http/*.md | grep -v _legacy
git diff --check
```

Expected: primera línea sin hits activos (negaciones/históricos OK si están enmarcados como legacy); segunda sin output.

- [ ] **Step 6: Commit**

```bash
git add docs/05-decisions/ADR-0013-permisos-funcionales-composites-keycloak.md contracts/http/identity-api.md contracts/http/partidas-config.md contracts/http/operaciones-sesion-api.md contracts/http/gateway-api.md gateway/gateway-context.md docs/04-sdd/traceability-matrix.md
git add contracts/http/partidas-api.md
git commit -m "docs(sp5a): ADR-0013 + contratos con autorización + matriz gateway + traceability

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

(Si `partidas-api.md` no necesitó cambios, omitir su `git add`.)

---

## Verificación final del slice (controller, antes del review whole-branch)

```bash
dotnet test gateway/Umbral.Gateway.sln
dotnet test services/operaciones-sesion/*.sln
dotnet test services/partidas/*.sln
dotnet test services/identity-service/*.sln
cd mobile && npm test && npm run typecheck && cd ..
cd frontend && npm test && cd ..
python3 scripts/check-realm-composites.py
```

Todo verde + review final whole-branch (opus) sobre el rango completo del slice.
