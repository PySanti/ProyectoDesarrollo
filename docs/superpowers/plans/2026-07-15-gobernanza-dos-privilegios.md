# Gobernanza: modelo de dos privilegios — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** El panel de gobernanza gobierna exactamente dos privilegios (`GestionarPartidas`, `GestionarEquipos`) y lo que asigna **sobrevive a un reinicio de Docker**.

**Architecture:** El realm de Keycloak declara sólo lo **fijo** (`Participante → ParticiparEnPartidas`); la tabla `permisos_rol` gobierna lo **variable** (los dos privilegios). No se solapan, así que `keycloak-config` y el panel dejan de pelear. Un reconciliador empuja la DB a Keycloak al arrancar.

**Tech Stack:** .NET 8 (Identity Service, Clean Architecture + MediatR + FluentValidation), PostgreSQL 16, Keycloak 25 + keycloak-config-cli 6.5.1, React 18 + Vite + Vitest.

**Spec:** `docs/superpowers/specs/2026-07-15-gobernanza-dos-privilegios-design.md`

## Global Constraints

- **Todas las respuestas y comentarios de código en español.** Los identificadores del dominio ya están en español (`PermisoFuncional`, `RolUsuario`, `permisos_rol`).
- **Nunca hacer push ni merge a otras ramas sin permiso explícito.** Commits locales en `feature/fixes-santiago`.
- **Todo commit termina con:** `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
- **Ledger:** tras cada tarea, añadir una línea a `.git/sdd/progress.md` con formato `YYYY-MM-DD | HU-04 | task N | <qué se hizo>`.
- **Levantar Docker siempre con `--env-file .env`:** `docker compose -f infra/docker-compose.yml --env-file .env up -d`. Omitirlo deja a Identity sin `KEYCLOAK_CLIENT_SECRET` y el reconciliador falla con 401.
- **El orden de las tareas es de seguridad, no de conveniencia.** Cada tarea deja el sistema funcionando. La 2 amplía el acceso *antes* de que la 5 lo quite; invertirlas rompe los equipos en el móvil (riesgo R1 del spec).
- **Defaults objetivo:** Administrador → `GestionarEquipos`; Operador → `GestionarPartidas`; Participante → ninguno.
- **Enums (no cambian):** `PermisoFuncional`: GestionarPartidas=1, GestionarEquipos=2, ParticiparEnPartidas=3. `RolUsuario`: Administrador=1, Operador=2, Participante=3.

## File Structure

| Archivo | Responsabilidad | Tarea |
|---|---|---|
| `Domain/Enums/PermisosGobernables.cs` | **Nuevo.** Única fuente de qué privilegios gobierna el panel. | 1 |
| `Application/Validators/ActualizarPermisosRolCommandValidator.cs` | Rechaza permisos no gobernables. | 1 |
| `Api/Controllers/TeamsController.cs` | Flujo propio del participante → rol. | 2 |
| `Api/Controllers/TeamInvitationsController.cs` | Ídem. | 2 |
| `Api/Program.cs` | Policy `Participante`; llamada al reconciliador; migración. | 2, 3, 5 |
| `Infrastructure/Services/Identity/PermisosRolKeycloakReconciler.cs` | **Recuperado del respaldo.** DB → Keycloak al arrancar. | 3 |
| `Infrastructure/DependencyInjection.cs` | Registro del reconciliador. | 3 |
| `infra/docker-compose.yml` | `depends_on: keycloak-config`. | 3 |
| `infra/keycloak/import/umbral-realm.json` | Declara sólo el composite fijo. | 4 |
| `frontend/src/features/identity/GovernancePage.tsx` | Panel a 2 privilegios. | 6 |
| `frontend/src/api/identityApi.ts` | Tipo de lo gobernable. | 6 |

---

### Task 1: `PermisosGobernables` y el validador que lo aplica

Hoy el validador acepta los tres permisos. Si nadie lo impide, un `PUT` a mano puede mover `ParticiparEnPartidas` a otro rol y descuadrar el modelo (riesgo R4). Esta tarea crea la **única fuente de verdad** de qué es gobernable, que la tarea 3 también consumirá.

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Enums/PermisosGobernables.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Application/Validators/ActualizarPermisosRolCommandValidator.cs:17-19`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/ActualizarPermisosRolCommandValidatorTests.cs` (crear si no existe)

**Interfaces:**
- Produces: `Umbral.IdentityService.Domain.Enums.PermisosGobernables.Todos` de tipo `IReadOnlySet<PermisoFuncional>`, con `{ GestionarPartidas, GestionarEquipos }`. La tarea 3 lo consume.

- [ ] **Step 1: Escribir el test que falla**

Crear `services/identity-service/tests/Umbral.IdentityService.UnitTests/ActualizarPermisosRolCommandValidatorTests.cs`:

```csharp
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Validators;

namespace Umbral.IdentityService.UnitTests;

/// <summary>
/// El panel gobierna dos privilegios. ParticiparEnPartidas existe en el dominio pero esta fijo al
/// rol Participante (composite declarado en el realm): moverlo por API descuadraria el modelo y
/// podria tumbar el gameplay del movil.
/// </summary>
public class ActualizarPermisosRolCommandValidatorTests
{
    private readonly ActualizarPermisosRolCommandValidator _validator = new();

    [Theory]
    [InlineData("GestionarPartidas")]
    [InlineData("GestionarEquipos")]
    public async Task Acepta_los_permisos_gobernables(string permiso)
    {
        var command = new ActualizarPermisosRolCommand("Administrador", new List<string> { permiso });

        var resultado = await _validator.ValidateAsync(command);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public async Task Rechaza_ParticiparEnPartidas_por_no_ser_gobernable()
    {
        var command = new ActualizarPermisosRolCommand("Administrador", new List<string> { "ParticiparEnPartidas" });

        var resultado = await _validator.ValidateAsync(command);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public async Task Rechaza_un_permiso_inexistente()
    {
        var command = new ActualizarPermisosRolCommand("Administrador", new List<string> { "NoExiste" });

        var resultado = await _validator.ValidateAsync(command);

        Assert.False(resultado.IsValid);
    }
}
```

- [ ] **Step 2: Ejecutar el test y verificar que falla**

Run:
```bash
dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj --filter "FullyQualifiedName~ActualizarPermisosRolCommandValidatorTests"
```
Expected: FAIL. `Rechaza_ParticiparEnPartidas_por_no_ser_gobernable` falla porque el validador actual acepta los tres valores del enum.

- [ ] **Step 3: Crear `PermisosGobernables`**

Crear `services/identity-service/src/Umbral.IdentityService.Domain/Enums/PermisosGobernables.cs`:

```csharp
namespace Umbral.IdentityService.Domain.Enums;

/// <summary>
/// Los privilegios que el panel de gobernanza (HU-04) puede mover entre roles.
/// <para>
/// <see cref="PermisoFuncional.ParticiparEnPartidas"/> queda fuera a proposito: existe en el dominio,
/// pero esta fijo al rol Participante como composite declarado en umbral-realm.json. Solo el rol
/// Participante tiene cliente donde jugar, asi que moverlo no habilitaria nada y quitarlo tumbaria
/// el gameplay del movil.
/// </para>
/// </summary>
public static class PermisosGobernables
{
    public static readonly IReadOnlySet<PermisoFuncional> Todos = new HashSet<PermisoFuncional>
    {
        PermisoFuncional.GestionarPartidas,
        PermisoFuncional.GestionarEquipos
    };
}
```

- [ ] **Step 4: Aplicarlo en el validador**

En `ActualizarPermisosRolCommandValidator.cs`, sustituir la regla de las líneas 17-19:

```csharp
        RuleForEach(c => c.Permisos)
            .Must(p => Enum.TryParse<PermisoFuncional>(p, ignoreCase: false, out var permiso)
                       && PermisosGobernables.Todos.Contains(permiso))
            .WithMessage("Permiso inválido: el panel solo gobierna GestionarPartidas y GestionarEquipos.");
```

- [ ] **Step 5: Ejecutar los tests y verificar que pasan**

Run:
```bash
dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj
```
Expected: PASS, incluidos los 3 nuevos.

⚠️ Si falla algún test **preexistente** de `ActualizarPermisosRolHandlerTests` o `GovernanceControllerTests` que use `ParticiparEnPartidas` como dato de prueba, **cambiar ese dato a `GestionarEquipos`** — no relajar el validador. El caso de uso desapareció a propósito.

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Domain/Enums/PermisosGobernables.cs \
        services/identity-service/src/Umbral.IdentityService.Application/Validators/ActualizarPermisosRolCommandValidator.cs \
        services/identity-service/tests/Umbral.IdentityService.UnitTests/ActualizarPermisosRolCommandValidatorTests.cs
git commit -m "feat(identity): el panel de gobernanza solo gobierna dos privilegios

ParticiparEnPartidas deja de ser asignable por API. Sigue existiendo en el
dominio, fijo al rol Participante via composite del realm: solo ese rol tiene
cliente donde jugar, asi que moverlo no habilita nada y quitarlo tumbaria el
gameplay del movil.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: El flujo de equipos del participante pasa a depender del rol

El nuevo default deja al Participante **sin** `GestionarEquipos` (tarea 5). Pero hoy `TeamsController` exige ese permiso para crear su equipo, invitar y aceptar. **Esta tarea va antes que la 5 a propósito:** amplía el acceso primero, para que quitar el permiso después no rompa nada. En este punto el Participante cumple ambas condiciones (tiene el rol *y* el permiso), así que el sistema sigue funcionando.

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs:117-122`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsController.cs:14`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamInvitationsController.cs:14`
- Test: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/TeamsControllerTests.cs` (ajustar si asevera la policy)

**Interfaces:**
- Produces: policy `"Participante"` registrada en el contenedor de Identity, exigiendo el rol `Participante`.

- [ ] **Step 1: Escribir el test que falla**

Añadir a `services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/TeamsControllerTests.cs` (crear el archivo con este contenido si no existe):

```csharp
using Microsoft.AspNetCore.Authorization;
using Umbral.IdentityService.Api.Controllers;

namespace Umbral.IdentityService.UnitTests.Api;

public class TeamsControllerPolicyTests
{
    /// <summary>
    /// El flujo propio del participante (su equipo) lo concede el rol, no un privilegio: el panel de
    /// gobernanza deja al Participante sin GestionarEquipos por defecto.
    /// </summary>
    [Theory]
    [InlineData(typeof(TeamsController))]
    [InlineData(typeof(TeamInvitationsController))]
    public void El_flujo_de_equipos_del_participante_exige_el_rol_no_el_privilegio(Type controlador)
    {
        var authorize = controlador
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal("Participante", authorize.Policy);
    }
}
```

- [ ] **Step 2: Ejecutar el test y verificar que falla**

Run:
```bash
dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj --filter "FullyQualifiedName~TeamsControllerPolicyTests"
```
Expected: FAIL con `Assert.Equal() Failure: Expected: "Participante", Actual: "GestionarEquipos"`.

- [ ] **Step 3: Registrar la policy**

En `Program.cs`, dentro de `builder.Services.AddAuthorization(options => { ... })` (línea ~117), añadir:

```csharp
    // El flujo propio del participante (su equipo, invitaciones) lo concede el rol: el panel de
    // gobernanza deja al Participante sin GestionarEquipos, que ahora solo abre los paneles de
    // administrar equipos ajenos.
    options.AddPolicy("Participante", policy => policy.RequireRole("Participante"));
```

- [ ] **Step 4: Cambiar las dos policies de clase**

En `TeamsController.cs:14` y `TeamInvitationsController.cs:14`, sustituir:

```csharp
[Authorize(Policy = "GestionarEquipos")]
```

por:

```csharp
[Authorize(Policy = "Participante")]
```

- [ ] **Step 5: Ejecutar los tests y verificar que pasan**

Run:
```bash
dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj
dotnet test services/identity-service/tests/Umbral.IdentityService.ContractTests/Umbral.IdentityService.ContractTests.csproj
```
Expected: PASS. Si un contract test montaba un usuario con `GestionarEquipos` pero sin el rol `Participante` para llegar a estos endpoints, **añadirle el rol** en su `TestAuthHandler`.

- [ ] **Step 6: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Api/Program.cs \
        services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsController.cs \
        services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamInvitationsController.cs \
        services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/TeamsControllerTests.cs
git commit -m "feat(identity): el equipo propio del participante depende del rol, no del privilegio

GestionarEquipos pasa a gobernar solo los paneles de administrar equipos
ajenos. Crear el equipo propio, invitar y aceptar son la funcion principal del
participante segun el SRS y no deben requerir un privilegio que el panel puede
retirarle.

Amplia el acceso antes de que el reset de defaults le quite GestionarEquipos,
para que no exista una ventana en la que el movil pierda los equipos.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Recuperar el reconciliador, limitado a lo gobernable

Sin esto, nada de lo que asigne el panel sobrevive a un `docker compose up`. El código está verificado en vivo en la rama `backup/gobernanza-santiago` (`aa8085b`); el único cambio es **iterar `PermisosGobernables.Todos` en vez del enum completo**. Si iterase el enum, vería que la DB no declara `ParticiparEnPartidas`, lo borraría de Keycloak y **tumbaría el gameplay entero** (riesgo R2).

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Identity/PermisosRolKeycloakReconciler.cs`
- Create: `services/identity-service/tests/Umbral.IdentityService.UnitTests/PermisosRolKeycloakReconcilerTests.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/DependencyInjection.cs:60`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs` (final del scope de arranque, tras `HistorialBackfill`)
- Modify: `infra/docker-compose.yml` (`depends_on` de `identity-service`)

**Interfaces:**
- Consumes: `PermisosGobernables.Todos` (tarea 1).
- Produces: `PermisosRolKeycloakReconciler.ReconcileAsync(CancellationToken)`, registrado como `Scoped`.

- [ ] **Step 1: Recuperar los dos archivos del respaldo**

```bash
git checkout backup/gobernanza-santiago -- \
  services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Identity/PermisosRolKeycloakReconciler.cs \
  services/identity-service/tests/Umbral.IdentityService.UnitTests/PermisosRolKeycloakReconcilerTests.cs
```

- [ ] **Step 2: Escribir el test que falla (el que protege el gameplay)**

Añadir a `PermisosRolKeycloakReconcilerTests.cs`, dentro de la clase:

```csharp
    [Fact]
    public async Task Nunca_toca_el_composite_fijo_de_ParticiparEnPartidas()
    {
        var (reconciler, repo, kc) = Crear();
        // La DB solo gobierna los dos privilegios; ParticiparEnPartidas vive fijo en el realm.
        repo.Datos[RolUsuario.Participante] = new List<PermisoFuncional>();

        await reconciler.ReconcileAsync(CancellationToken.None);

        // Si lo quitara, el rol Participante perderia el permiso y el gameplay del movil caeria entero.
        Assert.DoesNotContain(
            kc.CompositesQuitados,
            par => par.Permiso == nameof(PermisoFuncional.ParticiparEnPartidas));
        Assert.DoesNotContain(
            kc.CompositesAgregados,
            par => par.Permiso == nameof(PermisoFuncional.ParticiparEnPartidas));
    }
```

Y actualizar el test existente `Cubre_los_tres_roles_y_los_tres_permisos`, que ahora es falso (son dos permisos):

```csharp
    [Fact]
    public async Task Cubre_los_tres_roles_y_los_dos_permisos_gobernables()
    {
        var (reconciler, _, kc) = Crear();

        await reconciler.ReconcileAsync(CancellationToken.None);

        // Matriz vacia => los 6 pares gobernables se afirman como ausentes. ParticiparEnPartidas no cuenta.
        Assert.Equal(6, kc.CompositesQuitados.Count);
        Assert.Empty(kc.CompositesAgregados);
    }
```

- [ ] **Step 3: Ejecutar y verificar que falla**

Run:
```bash
dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj --filter "FullyQualifiedName~PermisosRolKeycloakReconcilerTests"
```
Expected: FAIL. `Nunca_toca_el_composite_fijo` falla (el reconciliador recuperado itera el enum entero y lo quita) y `Cubre_los_tres_roles_y_los_dos_permisos_gobernables` falla con `Expected: 6, Actual: 9`.

- [ ] **Step 4: Limitar el reconciliador a lo gobernable**

En `PermisosRolKeycloakReconciler.cs`, dentro de `ReconcileAsync`, sustituir el bucle interno:

```csharp
                // Add/Remove de Keycloak son idempotentes (y Remove tolera el 404), así que se
                // afirma el estado completo sin leer antes: 3 roles x 2 permisos = 6 llamadas.
                // Solo lo gobernable: ParticiparEnPartidas es un composite fijo del realm y borrarlo
                // dejaria al Participante sin poder jugar.
                foreach (var permiso in PermisosGobernables.Todos)
                {
                    if (deseados.Contains(permiso))
                        await _keycloak.AddCompositeToRoleAsync(rol.ToString(), permiso.ToString(), cancellationToken);
                    else
                        await _keycloak.RemoveCompositeFromRoleAsync(rol.ToString(), permiso.ToString(), cancellationToken);
                }
```

Y en el XML doc de la clase, sustituir el párrafo que habla de `keycloak-config` (ya no aplica: el realm dejará de declarar los gobernables en la tarea 4) por:

```csharp
/// <para>
/// El realm declara lo fijo (Participante -> ParticiparEnPartidas) y la DB gobierna lo variable (los
/// dos privilegios del panel). Como no se solapan, keycloak-config puede reaplicar el realm sin pisar
/// la gobernanza. Este reconciliador es quien lleva la matriz de permisos_rol a Keycloak al arrancar.
/// </para>
```

- [ ] **Step 5: Ejecutar y verificar que pasan**

Run:
```bash
dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj --filter "FullyQualifiedName~PermisosRolKeycloakReconcilerTests"
```
Expected: PASS (5 tests).

- [ ] **Step 6: Registrar en DI**

En `DependencyInjection.cs`, después de la línea 60 (`services.AddScoped<IPermisosRolRepository, PermisosRolRepository>();`):

```csharp
        services.AddScoped<PermisosRolKeycloakReconciler>();
```

- [ ] **Step 7: Llamarlo al arrancar**

En `Program.cs`, añadir el `using` si falta:

```csharp
using Umbral.IdentityService.Infrastructure.Services.Identity;
```

Y al final del `using (var scope = app.Services.CreateScope())`, tras la llamada a `HistorialBackfill.EjecutarAsync(...)`:

```csharp
    // La DB manda sobre la gobernanza; Keycloak es su espejo. Corre despues del seed de permisos_rol.
    await scope.ServiceProvider
        .GetRequiredService<PermisosRolKeycloakReconciler>()
        .ReconcileAsync(CancellationToken.None);
```

- [ ] **Step 8: Ordenar el arranque en Docker**

En `infra/docker-compose.yml`, dentro de `identity-service.depends_on`, añadir:

```yaml
      # El reconciliador escribe composites sobre roles que keycloak-config debe haber creado antes.
      keycloak-config:
        condition: service_completed_successfully
```

- [ ] **Step 9: Ejecutar toda la suite**

Run:
```bash
dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj
```
Expected: PASS.

- [ ] **Step 10: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Identity/PermisosRolKeycloakReconciler.cs \
        services/identity-service/src/Umbral.IdentityService.Infrastructure/DependencyInjection.cs \
        services/identity-service/src/Umbral.IdentityService.Api/Program.cs \
        services/identity-service/tests/Umbral.IdentityService.UnitTests/PermisosRolKeycloakReconcilerTests.cs \
        infra/docker-compose.yml
git commit -m "feat(identity): reconcilia los privilegios gobernables contra Keycloak al arrancar

La DB es la fuente de la gobernanza y Keycloak su espejo. Sin esto, lo que el
panel asigna no sobrevive a un docker compose up.

Solo itera los privilegios gobernables: ParticiparEnPartidas es un composite
fijo del realm y borrarlo dejaria al Participante sin poder jugar.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: El realm declara sólo lo fijo

Aquí desaparece la causa raíz: `keycloak-config` reaplica `umbral-realm.json` en cada `up` (con `IMPORT_CACHE_ENABLED: "false"`, deliberado) y borra lo que el panel asignó. Si el realm deja de declarar los privilegios gobernables, no hay nada que borrar.

**Files:**
- Modify: `infra/keycloak/import/umbral-realm.json` (bloque `roles.realm`)
- Modify: `scripts/check-realm-composites.py:10-13,35-36`
- Modify: `infra/docker-compose.yml` (comentario de `keycloak-config`)

- [ ] **Step 1: Quitar los composites gobernables**

En el bloque `roles.realm`, dejar los tres roles base así:

```json
      {
        "name": "Administrador",
        "description": "Administra usuarios y configuración de la plataforma."
      },
      {
        "name": "Operador",
        "description": "Crea y supervisa partidas de Trivia y BDT."
      },
      {
        "name": "Participante",
        "description": "Juega Trivia y BDT desde la app móvil.",
        "composite": true,
        "composites": { "realm": ["ParticiparEnPartidas"] }
      },
```

Los tres roles técnicos (`GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas`) se quedan **tal cual**: siguen existiendo como realm roles.

Cambios exactos: `Operador` pierde `composite` y `composites`; `Participante` conserva sólo `ParticiparEnPartidas`; `Administrador` no se toca.

- [ ] **Step 2: Actualizar el comentario del compose**

En `infra/docker-compose.yml`, el bloque de comentario sobre `keycloak-config` (líneas ~62-68) afirma que converge el realm entero. Añadir al final de ese comentario:

```yaml
  # Los composites de los privilegios gobernables (GestionarPartidas, GestionarEquipos) NO se
  # declaran aquí: los gobierna permisos_rol y los aplica el reconciliador de Identity al arrancar.
  # El realm solo declara el composite fijo Participante -> ParticiparEnPartidas.
```

- [ ] **Step 3: Actualizar el verificador del realm**

`scripts/check-realm-composites.py` codifica las expectativas viejas y **fallaría** con el realm nuevo
(`Operador` ya no es composite). Sustituir las líneas 10-13:

```python
# El realm solo declara el composite fijo. Los privilegios gobernables
# (GestionarPartidas, GestionarEquipos) los pone el reconciliador de Identity
# desde permisos_rol, asi que no deben aparecer declarados aqui.
COMPOSITES = {
    "Participante": {"ParticiparEnPartidas"},
}
NO_COMPOSITE = {"Administrador", "Operador"}
```

Y sustituir las líneas 35-36:

```python
for base_role in NO_COMPOSITE:
    if roles[base_role].get("composite"):
        fail(f"{base_role} no debe declarar composites: sus privilegios los gobierna permisos_rol")
```

- [ ] **Step 4: Verificar el JSON y el script**

Run:
```bash
python -c "import json;json.load(open('infra/keycloak/import/umbral-realm.json',encoding='utf-8'));print('JSON valido')"
python scripts/check-realm-composites.py
```
Expected: `JSON valido` y `OK: composites de permisos funcionales correctos`.

> **Ojo con lo que este script prueba.** Sólo valida el **JSON en disco**, no el Keycloak en ejecución.
> Que pase no significa que el realm cargado esté bien: eso lo verifica la tarea 7.

- [ ] **Step 5: Commit**

```bash
git add infra/keycloak/import/umbral-realm.json infra/docker-compose.yml scripts/check-realm-composites.py
git commit -m "fix(infra): el realm declara solo el composite fijo, no los gobernables

Causa raiz del bug: keycloak-config reaplica umbral-realm.json en cada up
(IMPORT_CACHE_ENABLED=false, deliberado) y borraba los composites que el panel
habia asignado. Al no declararlos, no hay nada que borrar.

El realm declara lo fijo (Participante -> ParticiparEnPartidas); permisos_rol
gobierna lo variable. No se solapan.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Migración con guardia de una sola vez

Aplica los defaults nuevos. **El guardia no es opcional:** un `DELETE FROM permisos_rol` sin él correría en cada arranque y borraría todo lo que asignes por el panel — un bug peor que el original (riesgo R3).

El seed actual (`Program.cs:149-160`) usa `WHERE NOT EXISTS (SELECT 1 FROM permisos_rol)`, que aquí no sirve: la tabla ya tiene datos.

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs:149-160`

- [ ] **Step 1: Añadir la tabla de migraciones y el reset guardado**

En `Program.cs`, **sustituir** el bloque `ExecuteSqlRawAsync` que crea `permisos_rol` (líneas 149-160) por:

```csharp
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS permisos_rol (
                rol integer NOT NULL,
                permiso integer NOT NULL,
                PRIMARY KEY (rol, permiso)
            );

            CREATE TABLE IF NOT EXISTS migraciones_aplicadas (
                nombre varchar(200) PRIMARY KEY,
                fechaaplicacionutc timestamp with time zone NOT NULL
            );

            -- Reset a los defaults del modelo de dos privilegios: Administrador->GestionarEquipos,
            -- Operador->GestionarPartidas, Participante->ninguno. El bloque es atomico y corre UNA
            -- sola vez: sin el guardia, cada arranque borraria lo asignado desde el panel.
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM migraciones_aplicadas WHERE nombre = '2026-07-15-dos-privilegios') THEN
                    DELETE FROM permisos_rol;
                    INSERT INTO permisos_rol (rol, permiso) VALUES (1, 2), (2, 1);
                    INSERT INTO migraciones_aplicadas (nombre, fechaaplicacionutc)
                    VALUES ('2026-07-15-dos-privilegios', now());
                END IF;
            END $$;
            """);
```

Nota: este bloque vive dentro del `if (dbContext.Database.IsRelational())` que ya existe en la línea 134, así que los tests con base InMemory no lo ejecutan.

- [ ] **Step 2: Compilar**

Run:
```bash
dotnet build services/identity-service/src/Umbral.IdentityService.Api/Umbral.IdentityService.Api.csproj
```
Expected: `Build succeeded`.

- [ ] **Step 3: Ejecutar la suite completa de Identity**

Run:
```bash
dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj
dotnet test services/identity-service/tests/Umbral.IdentityService.IntegrationTests/Umbral.IdentityService.IntegrationTests.csproj
dotnet test services/identity-service/tests/Umbral.IdentityService.ContractTests/Umbral.IdentityService.ContractTests.csproj
```
Expected: PASS.

> **Por qué no hay test automatizado del guardia:** los IntegrationTests usan `UseInMemoryDatabase`, que no ejecuta SQL crudo ni bloques `DO $$`. Un test ahí no probaría nada. El guardia se verifica en vivo en la tarea 7, paso 3, que es la única prueba que de verdad lo ejercita.

- [ ] **Step 4: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Api/Program.cs
git commit -m "feat(identity): resetea permisos_rol a los defaults de dos privilegios

Administrador->GestionarEquipos, Operador->GestionarPartidas, Participante->
ninguno. permisos_rol pasa a contener solo lo gobernable.

El reset corre una sola vez, guardado por la tabla migraciones_aplicadas: sin
el guardia, cada arranque borraria lo asignado desde el panel.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: El panel web muestra dos privilegios

**Files:**
- Modify: `frontend/src/api/identityApi.ts:168`
- Modify: `frontend/src/features/identity/GovernancePage.tsx:15-19`
- Test: `frontend/src/features/identity/GovernancePage.test.tsx`, `frontend/src/api/identityApi.test.ts`

**Interfaces:**
- Produces: tipo `PermisoGobernable = "GestionarPartidas" | "GestionarEquipos"` exportado desde `identityApi.ts`, consumido por `GovernancePage.tsx`.

- [ ] **Step 1: Escribir el test que falla**

Añadir a `frontend/src/features/identity/GovernancePage.test.tsx`:

```tsx
  /* El panel gobierna dos privilegios. ParticiparEnPartidas esta fijo al rol Participante
     (composite del realm) y no es asignable: mostrarlo prometeria algo que el backend rechaza. */
  it("no ofrece ParticiparEnPartidas como privilegio asignable", async () => {
    render(<GovernancePage accessToken="token" />);

    await screen.findByText("Gestionar partidas");
    expect(screen.getByText("Gestionar equipos")).toBeInTheDocument();
    expect(screen.queryByText("Participar en partidas")).not.toBeInTheDocument();
  });
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run:
```bash
cd frontend && npx vitest run src/features/identity/GovernancePage.test.tsx
```
Expected: FAIL — "Participar en partidas" sigue en el panel.

- [ ] **Step 3: Renombrar el tipo a lo que de verdad modela**

En `frontend/src/api/identityApi.ts:168`, sustituir:

```ts
export type PermisoFuncional = "GestionarPartidas" | "GestionarEquipos" | "ParticiparEnPartidas";
```

por:

```ts
/* Lo que el panel de gobernanza puede mover entre roles. El dominio tiene un tercer permiso
   funcional (ParticiparEnPartidas), pero esta fijo al rol Participante y no es asignable: el
   backend lo rechaza. El nombre dice "gobernable" para que el tipo no mienta. */
export type PermisoGobernable = "GestionarPartidas" | "GestionarEquipos";
```

Y en la interfaz `RolePermissions` (línea ~172), cambiar el tipo del campo:

```ts
  permisos: PermisoGobernable[];
```

- [ ] **Step 4: Actualizar el panel**

En `GovernancePage.tsx`, cambiar el import de la línea 5 (`PermisoFuncional` → `PermisoGobernable`) y la lista de las líneas 15-19:

```tsx
const PERMISOS: { key: PermisoGobernable; label: string }[] = [
  { key: "GestionarPartidas", label: "Gestionar partidas" },
  { key: "GestionarEquipos", label: "Gestionar equipos" }
];
```

Sustituir las demás apariciones de `PermisoFuncional` en el archivo (`CardState.confirmed`, `CardState.marked`, la firma de `sameSet`) por `PermisoGobernable`.

- [ ] **Step 5: Ejecutar los tests y el typecheck**

Run:
```bash
cd frontend && npm test && npx tsc --noEmit
```
Expected: PASS y typecheck limpio. Si `identityApi.test.ts` monta respuestas con `"ParticiparEnPartidas"` (líneas ~18-21), cambiarlas a `"GestionarEquipos"`: ese valor ya no forma parte del contrato del panel.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/api/identityApi.ts \
        frontend/src/features/identity/GovernancePage.tsx \
        frontend/src/features/identity/GovernancePage.test.tsx \
        frontend/src/api/identityApi.test.ts
git commit -m "feat(web): el panel de gobernanza ofrece solo los dos privilegios gobernables

ParticiparEnPartidas esta fijo al rol Participante y el backend rechaza
asignarlo: ofrecerlo prometia algo que no podia cumplirse.

El tipo pasa a llamarse PermisoGobernable para que no mienta: modela lo que el
panel puede mover, no los permisos funcionales del dominio.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: Verificación en vivo

Los tests no cubren lo que de verdad falla: el guardia de la migración (InMemory no ejecuta SQL) y la supervivencia a un reinicio. **Esta tarea es obligatoria** — es la única que ejercita el bug original.

**Files:** ninguno (verificación).

- [ ] **Step 1: Levantar de cero**

```bash
docker compose -f infra/docker-compose.yml --env-file .env up -d --build
```

Verificar que Identity arrancó y reconcilió:

```bash
docker logs umbral-identity-service 2>&1 | grep -i "reconciliad"
```
Expected: tres líneas, una por rol. `Permisos de Administrador reconciliados en Keycloak: [GestionarEquipos]`, `Operador: [GestionarPartidas]`, `Participante: []`.

⚠️ Si aparece `Failed to get Keycloak token. StatusCode=401`, el contenedor no tiene `KEYCLOAK_CLIENT_SECRET`: se levantó sin `--env-file .env`.

- [ ] **Step 2: Confirmar los defaults en la base de datos**

```bash
docker exec umbral-postgres psql -U umbral -d umbral_identity -c "SELECT rol, permiso FROM permisos_rol ORDER BY rol, permiso;"
```
Expected: exactamente dos filas — `1 | 2` y `2 | 1`.

- [ ] **Step 3: Probar el guardia de la migración (R3)**

Asignar algo por el panel (o simularlo) y reiniciar:

```bash
docker exec umbral-postgres psql -U umbral -d umbral_identity -c "INSERT INTO permisos_rol (rol, permiso) VALUES (1, 1) ON CONFLICT DO NOTHING;"
docker compose -f infra/docker-compose.yml --env-file .env restart identity-service
sleep 15
docker exec umbral-postgres psql -U umbral -d umbral_identity -c "SELECT rol, permiso FROM permisos_rol ORDER BY rol, permiso;"
```
Expected: **tres filas** — `1 | 1`, `1 | 2`, `2 | 1`. La fila `1 | 1` **sobrevive**.

⚠️ Si sólo quedan dos filas, el guardia no funciona y la migración está borrando la gobernanza en cada arranque. **Parar y arreglar la tarea 5 antes de seguir.**

- [ ] **Step 4: Probar el bug original de punta a punta**

1. Entrar a la web como administrador y asignar «Gestionar partidas» al rol Administrador.
2. Reiniciar el stack completo:
   ```bash
   docker compose -f infra/docker-compose.yml --env-file .env up -d
   ```
3. Comprobar que el composite **sobrevive** en Keycloak:
   ```bash
   docker exec umbral-keycloak /opt/keycloak/bin/kcadm.sh config credentials \
     --server http://localhost:8080 --realm master --user admin --password admin
   docker exec umbral-keycloak /opt/keycloak/bin/kcadm.sh get-roles \
     -r UMBRAL-UCAB --rname Administrador --effective --fields name
   ```
   Expected: la lista incluye `GestionarPartidas` **y** `GestionarEquipos`.

Esto es exactamente lo que hoy falla. Si sobrevive, la causa raíz está resuelta.

- [ ] **Step 5: Probar que el móvil no se rompió (R1 y R2)**

Con un usuario `Participante` en la app móvil:
1. Crear un equipo → debe funcionar (tarea 2: ahora va por rol).
2. Comprobar que el token del participante sigue llevando `ParticiparEnPartidas`:
   ```bash
   docker exec umbral-keycloak /opt/keycloak/bin/kcadm.sh get-roles \
     -r UMBRAL-UCAB --rname Participante --effective --fields name
   ```
   Expected: incluye `ParticiparEnPartidas`. **No** debe incluir `GestionarEquipos`.
3. Entrar a una partida y jugar → debe funcionar.

⚠️ Si falla cualquiera de los tres, es R1 o R2. **Parar y reportar** antes de dar la tarea por buena.

- [ ] **Step 6: Ledger**

```bash
echo "2026-07-15 | HU-04 | task 7 | verificacion en vivo: guardia de migracion OK, privilegio sobrevive al reinicio, movil intacto" >> .git/sdd/progress.md
```

---

## Notas para quien implemente

- **El respaldo es tu amigo.** `backup/gobernanza-santiago` (`aa8085b`) tiene el reconciliador verificado en vivo. La tarea 3 lo recupera con `git checkout <rama> -- <ruta>`.
- **No inviertas el orden de las tareas 2 y 5.** La 2 amplía el acceso, la 5 lo quita. Al revés hay una ventana en la que el móvil pierde los equipos.
- **Si un test preexistente usa `ParticiparEnPartidas` como dato de prueba**, cámbialo a `GestionarEquipos`. No relajes el validador para que pase: el caso de uso desapareció a propósito.
- **Este plan no toca** el enum `PermisoFuncional`, `SesionesController`, el SRS, el modelo de dominio, el diagrama de clases, el ADR-0013 ni los contratos. Es deliberado: eliminar el permiso del dominio tocaba 37 archivos para un resultado indistinguible desde el panel.
- **Lo que este plan NO arregla:** el síntoma original que dio origen a todo (asignar «Gestionar partidas» al admin y que aparezca el panel de creación) necesita el **sub-proyecto 2**, que es el que hace que la web lea permisos del token y gatee las áreas por privilegio. Este plan construye los cimientos: que la gobernanza sea coherente y sobreviva.
