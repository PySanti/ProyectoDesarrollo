# Panel de Gestión de Equipo (mobile) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the flat 8-button "Tu equipo" section on mobile's `HomeScreen` with a single
`TeamPanel` screen that shows team status (belongs/not, name, leader/member, member list) and
exposes only the actions valid for that state.

**Architecture:** One small backend extension (Identity: `GET /identity/teams/mine` gains a
`nombre` field per participant) + one new mobile feature slice (`teamPanelApi.js` /
`teamPanelFlow.js` / `TeamPanelScreen.tsx` / `TeamPanelScreenContainer.tsx`) following the
existing `mobile/src/features/teams/` pattern. No existing screen, endpoint shape (besides the
one field addition), or business rule changes.

**Tech Stack:** .NET 8 / MediatR / xUnit (Identity backend), React Native + TypeScript + Expo,
`node --test` (mobile).

## Global Constraints

- Design doc: `docs/superpowers/specs/2026-07-16-panel-gestion-equipo-mobile-design.md` — read
  before implementing if anything below is ambiguous.
- Mobile feature files follow the Container/Screen/Flow/Api split already used in
  `mobile/src/features/teams/` (see `InvitationsScreenContainer.tsx` /
  `InvitationsScreen.tsx` / `invitationsFlow.js` / `invitationsApi.js` as the reference).
- No new screens besides `TeamPanelScreen` — every button navigates to a screen that already
  exists (`CreateTeam`, `Invitations`, `InviteMember`, `TransferLeadership`, `LeaveTeam`,
  `DeleteTeam`, `TeamHistory`, `RendimientoEquipo`).
- `contracts/http/identity-api.md` is the source of truth for the HTTP shape — update it in the
  same task as the backend change.
- Backend: `ImplicitUsings` is enabled in `Umbral.IdentityService.Application` — no explicit
  `using System.Linq;` needed.
- Mobile tests: `node --test tests/*.test.js` (run from `mobile/`). Backend tests:
  `dotnet test services/identity-service/Umbral.IdentityService.sln`.
- Commits end with `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

### Task 1: Backend — `GET /identity/teams/mine` returns member names

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Application/DTOs/EquipoMineResponse.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/ObtenerMiEquipoQueryHandler.cs`
- Modify: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/ObtenerMiEquipoQueryHandlerTests.cs`
- Modify: `services/identity-service/tests/Umbral.IdentityService.ContractTests/Teams/MiEquipoContractTests.cs`
- Modify: `contracts/http/identity-api.md` (line 103)

**Interfaces:**
- Consumes: `IUsuarioRepository.GetAllAsync(CancellationToken)` → `Task<IReadOnlyList<Usuario>>`
  (already used identically in `ListarEquiposQueryHandler.cs`); `Usuario.KeycloakId` (string),
  `Usuario.Nombre` (string).
- Produces: `MiembroEquipoResponse(Guid UsuarioId, string Nombre, bool EsLider)` — Task 2/3
  (mobile) consume this shape's JSON serialization (`usuarioId`, `nombre`, `esLider`).

- [ ] **Step 1: Write the failing unit test**

Replace the full contents of
`services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/ObtenerMiEquipoQueryHandlerTests.cs`
with:

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

namespace Umbral.IdentityService.UnitTests.Teams;

public class ObtenerMiEquipoQueryHandlerTests
{
    // Fake a mano de IEquipoRepository: solo GetActiveByMemberUserIdAsync se usa aquí.
    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public Equipo? Activo;
        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult(Activo);
        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult(Activo is not null);
        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken ct) => Task.FromResult(Activo);
        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Equipo>>(Activo is null ? Array.Empty<Equipo>() : new[] { Activo });
        public Task AddAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Equipo equipo, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public List<Usuario> Usuarios = new();
        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Usuario>>(Usuarios);
        public Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<Usuario?>(null);
        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken ct) =>
            Task.FromResult<Usuario?>(Usuarios.FirstOrDefault(u => u.KeycloakId == keycloakId.ToString()));
        public Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task AddAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task Sin_equipo_activo_devuelve_null()
    {
        var handler = new ObtenerMiEquipoQueryHandler(new FakeEquipoRepository { Activo = null }, new FakeUsuarioRepository());

        var result = await handler.Handle(new ObtenerMiEquipoQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Con_equipo_activo_mapea_miembros_lider_y_nombre()
    {
        // ParticipanteEquipo.UsuarioId guarda el sub de Keycloak, no el UsuarioId local:
        // el nombre se resuelve por KeycloakId (igual que ListarEquiposQueryHandler).
        var lider = Guid.NewGuid();
        var liderUsuario = Usuario.Crear(lider.ToString(), "Ana", "ana@umbral.test", RolUsuario.Participante);
        var miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Los Halcones", lider);
        equipo.AgregarParticipante(miembro);
        var usuarios = new FakeUsuarioRepository();
        usuarios.Usuarios.Add(liderUsuario);
        var handler = new ObtenerMiEquipoQueryHandler(new FakeEquipoRepository { Activo = equipo }, usuarios);

        var result = await handler.Handle(new ObtenerMiEquipoQuery(lider), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(equipo.EquipoId, result!.EquipoId);
        Assert.Equal("Los Halcones", result.NombreEquipo);
        Assert.Equal("Activo", result.Estado);
        Assert.Equal(2, result.Participantes.Count);
        var pLider = result.Participantes.Single(p => p.UsuarioId == lider);
        Assert.Equal("Ana", pLider.Nombre);
        Assert.True(pLider.EsLider);
        // Usuario no registrado en la tabla local → nombre vacío, no explota.
        var pMiembro = result.Participantes.Single(p => p.UsuarioId == miembro);
        Assert.Equal("", pMiembro.Nombre);
        Assert.False(pMiembro.EsLider);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj --filter "FullyQualifiedName~ObtenerMiEquipoQueryHandlerTests"`
Expected: FAIL — compile error (`MiembroEquipoResponse` has no `Nombre` member yet; handler
constructor doesn't accept `IUsuarioRepository`).

- [ ] **Step 3: Update the DTO**

Replace the full contents of
`services/identity-service/src/Umbral.IdentityService.Application/DTOs/EquipoMineResponse.cs`
with:

```csharp
namespace Umbral.IdentityService.Application.DTOs;

public sealed record EquipoMineResponse(
    Guid EquipoId,
    string NombreEquipo,
    string Estado,
    IReadOnlyList<MiembroEquipoResponse> Participantes);

public sealed record MiembroEquipoResponse(Guid UsuarioId, string Nombre, bool EsLider);
```

- [ ] **Step 4: Update the handler**

Replace the full contents of
`services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/ObtenerMiEquipoQueryHandler.cs`
with:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class ObtenerMiEquipoQueryHandler : IRequestHandler<ObtenerMiEquipoQuery, EquipoMineResponse?>
{
    private readonly IEquipoRepository _equipos;
    private readonly IUsuarioRepository _usuarios;

    public ObtenerMiEquipoQueryHandler(IEquipoRepository equipos, IUsuarioRepository usuarios)
    {
        _equipos = equipos;
        _usuarios = usuarios;
    }

    public async Task<EquipoMineResponse?> Handle(ObtenerMiEquipoQuery request, CancellationToken cancellationToken)
    {
        var equipo = await _equipos.GetActiveByMemberUserIdAsync(request.ActorUserId, cancellationToken);
        if (equipo is null) return null;

        var usuarios = await _usuarios.GetAllAsync(cancellationToken);
        // Los miembros de equipo (ParticipanteEquipo.UsuarioId) guardan el sub de Keycloak,
        // no el UsuarioId local: hay que resolver el nombre por KeycloakId parseado (igual
        // que ListarEquiposQueryHandler).
        var nombres = new Dictionary<Guid, string>();
        foreach (var u in usuarios)
        {
            if (Guid.TryParse(u.KeycloakId, out var keycloakId))
                nombres[keycloakId] = u.Nombre;
        }

        return new EquipoMineResponse(
            equipo.EquipoId,
            equipo.NombreEquipo,
            equipo.Estado.ToString(),
            equipo.Participantes
                .Select(p => new MiembroEquipoResponse(
                    p.UsuarioId,
                    nombres.TryGetValue(p.UsuarioId, out var nombre) ? nombre : "",
                    p.EsLider))
                .ToList());
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests/Umbral.IdentityService.UnitTests.csproj --filter "FullyQualifiedName~ObtenerMiEquipoQueryHandlerTests"`
Expected: PASS (2/2)

- [ ] **Step 6: Update the contract test to assert the new field's presence**

In `services/identity-service/tests/Umbral.IdentityService.ContractTests/Teams/MiEquipoContractTests.cs`,
inside `MiEquipo_Returns200_WithShape_ForLeader`, change:

```csharp
        var first = participantes[0];
        Assert.True(first.TryGetProperty("usuarioId", out _));
        Assert.True(first.TryGetProperty("esLider", out _));
```

to:

```csharp
        var first = participantes[0];
        Assert.True(first.TryGetProperty("usuarioId", out _));
        Assert.True(first.TryGetProperty("nombre", out _));
        Assert.True(first.TryGetProperty("esLider", out _));
```

- [ ] **Step 7: Run the contract test to verify it passes**

Run: `dotnet test services/identity-service/tests/Umbral.IdentityService.ContractTests/Umbral.IdentityService.ContractTests.csproj --filter "FullyQualifiedName~MiEquipoContractTests"`
Expected: PASS (3/3)

- [ ] **Step 8: Update the contract doc**

In `contracts/http/identity-api.md`, line 103, change:

```
| Get my active team | GET | `/identity/teams/mine` | Registered | 200 `{ equipoId, nombreEquipo, estado, participantes:[{ usuarioId, esLider }] }`; 404 if caller has no active team; 401/403 per above |
```

to:

```
| Get my active team | GET | `/identity/teams/mine` | Registered | 200 `{ equipoId, nombreEquipo, estado, participantes:[{ usuarioId, nombre, esLider }] }`; `nombre` resolved via the local user reference (`Usuario.KeycloakId`), `""` when no local row exists (same resolution as the admin listing below); 404 if caller has no active team; 401/403 per above |
```

- [ ] **Step 9: Run the full Identity test suite to confirm no regressions**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: PASS, same total count as before Task 1 plus the 0 net-new tests (2 existing tests
extended, no new test methods added).

- [ ] **Step 10: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/DTOs/EquipoMineResponse.cs services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/ObtenerMiEquipoQueryHandler.cs services/identity-service/tests/Umbral.IdentityService.UnitTests/Teams/ObtenerMiEquipoQueryHandlerTests.cs services/identity-service/tests/Umbral.IdentityService.ContractTests/Teams/MiEquipoContractTests.cs contracts/http/identity-api.md
git commit -m "$(cat <<'EOF'
feat(identity): GET /identity/teams/mine devuelve nombre por participante

El panel de gestión de equipo de mobile necesita mostrar una lista de
integrantes legible, no solo IDs de Keycloak. Resuelve el nombre igual
que ya hace el listado admin (Usuario.KeycloakId, "" sin referencia local).

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Mobile — data layer (`teamPanelApi.js` / `teamPanelFlow.js`)

**Files:**
- Create: `mobile/src/features/teams/teamPanelApi.js`
- Create: `mobile/src/features/teams/teamPanelFlow.js`
- Create: `mobile/tests/teamPanelFlow.test.js`

**Interfaces:**
- Consumes: `GET /identity/teams/mine` (Task 1's shape: `{ equipoId, nombreEquipo, estado,
  participantes: [{ usuarioId, nombre, esLider }] }`, 404 when no active team).
- Produces: `fetchMyTeamStatus({ apiBaseUrl, token, currentUserId, fetchImpl })` →
  `Promise<{ ok: true, status: "sinEquipo" } | { ok: true, status: "lider" | "miembro",
  equipoId: string, nombreEquipo: string, participantes: Array<{ usuarioId: string, nombre:
  string, esLider: boolean }> } | { ok: false, type: string, message: string }>`. Task 3
  (`TeamPanelScreen.tsx`) consumes this exact shape.

- [ ] **Step 1: Write the failing test**

Create `mobile/tests/teamPanelFlow.test.js`:

```js
import test from "node:test";
import assert from "node:assert/strict";
import { fetchMyTeamStatus } from "../src/features/teams/teamPanelFlow.js";

const API_BASE = "https://api.test";
const TOKEN = "test-token";
const USER_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const OTHER_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

test("fetchMyTeamStatus calls GET /identity/teams/mine with Bearer token", async () => {
  let requestedUrl;
  let requestedHeaders;

  await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async (url, options) => {
      requestedUrl = url;
      requestedHeaders = options.headers;
      return { ok: false, status: 404 };
    },
  });

  assert.equal(requestedUrl, `${API_BASE}/identity/teams/mine`);
  assert.equal(requestedHeaders["Authorization"], `Bearer ${TOKEN}`);
});

test("fetchMyTeamStatus returns sinEquipo on 404", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async () => ({ ok: false, status: 404 }),
  });

  assert.equal(result.ok, true);
  assert.equal(result.status, "sinEquipo");
});

test("fetchMyTeamStatus returns lider when current user is esLider true", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async () => ({
      ok: true,
      status: 200,
      json: async () => ({
        equipoId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        nombreEquipo: "Los Halcones",
        estado: "Activo",
        participantes: [
          { usuarioId: USER_ID, nombre: "Ana", esLider: true },
          { usuarioId: OTHER_ID, nombre: "Beto", esLider: false },
        ],
      }),
    }),
  });

  assert.equal(result.ok, true);
  assert.equal(result.status, "lider");
  assert.equal(result.nombreEquipo, "Los Halcones");
  assert.equal(result.participantes.length, 2);
});

test("fetchMyTeamStatus returns miembro when current user is esLider false", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: OTHER_ID,
    fetchImpl: async () => ({
      ok: true,
      status: 200,
      json: async () => ({
        equipoId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        nombreEquipo: "Los Halcones",
        estado: "Activo",
        participantes: [
          { usuarioId: USER_ID, nombre: "Ana", esLider: true },
          { usuarioId: OTHER_ID, nombre: "Beto", esLider: false },
        ],
      }),
    }),
  });

  assert.equal(result.ok, true);
  assert.equal(result.status, "miembro");
});

test("fetchMyTeamStatus returns network error when fetch throws", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async () => {
      throw new Error("network down");
    },
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "network");
});

test("fetchMyTeamStatus returns unauthorized on 401", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async () => ({ ok: false, status: 401 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "unauthorized");
});

test("fetchMyTeamStatus returns error on unexpected status", async () => {
  const result = await fetchMyTeamStatus({
    apiBaseUrl: API_BASE,
    token: TOKEN,
    currentUserId: USER_ID,
    fetchImpl: async () => ({ ok: false, status: 500 }),
  });

  assert.equal(result.ok, false);
  assert.equal(result.type, "error");
});
```

- [ ] **Step 2: Run test to verify it fails**

Run (from `mobile/`): `node --test tests/teamPanelFlow.test.js`
Expected: FAIL — `Cannot find module '../src/features/teams/teamPanelFlow.js'`

- [ ] **Step 3: Write `teamPanelApi.js`**

Create `mobile/src/features/teams/teamPanelApi.js`:

```js
export async function loadMyTeam(apiBaseUrl, token, fetchImpl = fetch) {
  let response;
  try {
    response = await fetchImpl(`${apiBaseUrl}/identity/teams/mine`, {
      method: "GET",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
  } catch {
    return {
      ok: false,
      type: "network",
      message: "No se pudo conectar con el servidor. Verifica tu conexion e intenta de nuevo.",
    };
  }

  if (response.status === 404) {
    return { ok: true, data: null };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "Debes tener rol Participante para ver tu equipo." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo cargar la informacion de tu equipo." };
  }

  const data = await response.json();
  return { ok: true, data };
}
```

- [ ] **Step 4: Write `teamPanelFlow.js`**

Create `mobile/src/features/teams/teamPanelFlow.js`:

```js
import { loadMyTeam } from "./teamPanelApi.js";

export async function fetchMyTeamStatus({ apiBaseUrl, token, currentUserId, fetchImpl }) {
  let result;
  try {
    result = await loadMyTeam(apiBaseUrl, token, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al cargar tu equipo. Intenta nuevamente.",
    };
  }

  if (!result.ok) {
    return result;
  }

  if (result.data === null) {
    return { ok: true, status: "sinEquipo" };
  }

  const equipo = result.data;
  const yo = equipo.participantes.find((p) => p.usuarioId === currentUserId);
  const soyLider = yo ? yo.esLider : false;

  return {
    ok: true,
    status: soyLider ? "lider" : "miembro",
    equipoId: equipo.equipoId,
    nombreEquipo: equipo.nombreEquipo,
    participantes: equipo.participantes,
  };
}
```

- [ ] **Step 5: Run test to verify it passes**

Run (from `mobile/`): `node --test tests/teamPanelFlow.test.js`
Expected: PASS (7/7)

- [ ] **Step 6: Run the full mobile test suite to confirm no regressions**

Run (from `mobile/`): `npm test`
Expected: PASS, same count as before plus 7 new tests.

- [ ] **Step 7: Commit**

```bash
git add mobile/src/features/teams/teamPanelApi.js mobile/src/features/teams/teamPanelFlow.js mobile/tests/teamPanelFlow.test.js
git commit -m "$(cat <<'EOF'
feat(mobile): data layer del panel de gestion de equipo

fetchMyTeamStatus resuelve los 3 estados (sin equipo / lider / miembro)
a partir de GET /identity/teams/mine, comparando el usuarioId propio
contra la lista de participantes.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Mobile — `TeamPanelScreen` + navigation wiring + `HomeScreen` simplification

**Files:**
- Create: `mobile/src/features/teams/TeamPanelScreen.tsx`
- Create: `mobile/src/features/teams/TeamPanelScreenContainer.tsx`
- Modify: `mobile/src/navigation/types.ts`
- Modify: `mobile/src/navigation/RootNavigator.tsx`
- Modify: `mobile/src/screens/HomeScreen.tsx`

**Interfaces:**
- Consumes: `fetchMyTeamStatus` from Task 2 (exact shape above); `useAuth()` →
  `session.user.sub` (string, Keycloak subject — already the same id key server-side resolves
  `usuarioId` to, per `authTypes.ts`); `mobileEnv.gatewayApiBaseUrl` (existing config, same one
  `InvitationsScreenContainer.tsx` uses).
- Produces: `TeamPanelScreenContainer` registered under route name `"TeamPanel"` in
  `AppStackParamList` — nothing else consumes it (leaf screen).

- [ ] **Step 1: Add the route to `AppStackParamList`**

In `mobile/src/navigation/types.ts`, add `TeamPanel: undefined;` to `AppStackParamList`:

```ts
export type AppStackParamList = {
  Home: undefined;
  TeamPanel: undefined;
  CreateTeam: undefined;
  Invitations: undefined;
  InviteMember: undefined;
  TransferLeadership: undefined;
  LeaveTeam: undefined;
  DeleteTeam: undefined;
  TeamHistory: undefined;
  PartidasPanel: undefined;
  PartidaLobby: { partidaId: string; nombre: string };
  PartidaLive: { partidaId: string; nombre: string };
  Convocatorias: undefined;
  HistorialPartidas: undefined;
  RendimientoEquipo: undefined;
};
```

- [ ] **Step 2: Write `TeamPanelScreen.tsx`**

Create `mobile/src/features/teams/TeamPanelScreen.tsx`:

```tsx
import React, { useCallback, useState } from "react";
import { ActivityIndicator, SafeAreaView, ScrollView, StyleSheet, View } from "react-native";
import { useFocusEffect } from "@react-navigation/native";
import { NativeStackNavigationProp } from "@react-navigation/native-stack";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, radius, spacing } from "../../shared/theme";
import { AppStackParamList } from "../../navigation/types";
import { fetchMyTeamStatus } from "./teamPanelFlow.js";

type Participante = { usuarioId: string; nombre: string; esLider: boolean };

type TeamStatus =
  | { status: "sinEquipo" }
  | { status: "lider" | "miembro"; nombreEquipo: string; participantes: Participante[] };

type TeamPanelNavigation = NativeStackNavigationProp<AppStackParamList, "TeamPanel">;

type TeamPanelScreenProps = {
  apiBaseUrl: string;
  token: string;
  currentUserId: string;
  navigation: TeamPanelNavigation;
};

export function TeamPanelScreen({ apiBaseUrl, token, currentUserId, navigation }: TeamPanelScreenProps) {
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [team, setTeam] = useState<TeamStatus | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setErrorMessage(null);
    const result = await fetchMyTeamStatus({ apiBaseUrl, token, currentUserId, fetchImpl: undefined });
    setLoading(false);
    if (!result.ok) {
      setErrorMessage(result.message ?? "No se pudo cargar tu equipo.");
      return;
    }
    if (result.status === "sinEquipo") {
      setTeam({ status: "sinEquipo" });
      return;
    }
    setTeam({
      status: result.status,
      nombreEquipo: result.nombreEquipo as string,
      participantes: result.participantes as Participante[],
    });
  }, [apiBaseUrl, token, currentUserId]);

  useFocusEffect(
    useCallback(() => {
      load();
    }, [load])
  );

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <ScreenHeader title="Gestión de equipo" subtitle="Tu equipo, tus compañeros y tus acciones." />
        {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
        {loading ? (
          <ActivityIndicator color={colors.primaryBright} size="large" />
        ) : team?.status === "sinEquipo" ? (
          <SinEquipoView navigation={navigation} />
        ) : team ? (
          <ConEquipoView team={team} navigation={navigation} />
        ) : null}
      </ScrollView>
    </SafeAreaView>
  );
}

function SinEquipoView({ navigation }: { navigation: TeamPanelNavigation }) {
  return (
    <View style={styles.group}>
      <Card>
        <AppText variant="body" color={colors.muted}>
          No pertenecés a ningún equipo activo.
        </AppText>
      </Card>
      <Button label="Crear equipo" onPress={() => navigation.navigate("CreateTeam")} />
      <Button label="Invitaciones" variant="secondary" onPress={() => navigation.navigate("Invitations")} />
      <Button label="Historial de equipos" variant="secondary" onPress={() => navigation.navigate("TeamHistory")} />
    </View>
  );
}

function ConEquipoView({
  team,
  navigation,
}: {
  team: Extract<TeamStatus, { status: "lider" | "miembro" }>;
  navigation: TeamPanelNavigation;
}) {
  const esLider = team.status === "lider";
  return (
    <View style={styles.group}>
      <Card>
        <View style={styles.teamHeader}>
          <AppText variant="title">{team.nombreEquipo}</AppText>
          <RoleBadge label={esLider ? "Líder" : "Miembro"} />
        </View>
        <View style={styles.membersList}>
          {team.participantes.map((p) => (
            <View key={p.usuarioId} style={styles.memberRow}>
              <AppText variant="body">{p.nombre || "Sin nombre"}</AppText>
              {p.esLider ? <RoleBadge label="Líder" /> : null}
            </View>
          ))}
        </View>
      </Card>

      <Button label="Invitaciones" onPress={() => navigation.navigate("Invitations")} />
      {esLider ? <Button label="Invitar miembro" onPress={() => navigation.navigate("InviteMember")} /> : null}
      {esLider ? (
        <Button
          label="Transferir liderazgo"
          variant="secondary"
          onPress={() => navigation.navigate("TransferLeadership")}
        />
      ) : null}
      <Button label="Salir del equipo" variant="secondary" onPress={() => navigation.navigate("LeaveTeam")} />
      {esLider ? (
        <Button label="Eliminar equipo" variant="danger" onPress={() => navigation.navigate("DeleteTeam")} />
      ) : null}
      <Button label="Historial de equipos" variant="secondary" onPress={() => navigation.navigate("TeamHistory")} />
      <Button
        label="Rendimiento de equipo"
        variant="secondary"
        onPress={() => navigation.navigate("RendimientoEquipo")}
      />
    </View>
  );
}

function RoleBadge({ label }: { label: string }) {
  return (
    <View style={styles.badge}>
      <AppText variant="label" color={colors.primaryStrong}>
        {label}
      </AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, gap: spacing.lg },
  group: { gap: spacing.sm },
  teamHeader: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  membersList: { gap: spacing.xs, marginTop: spacing.sm },
  memberRow: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  badge: {
    backgroundColor: colors.primaryWash,
    borderRadius: radius.pill,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
  },
});
```

- [ ] **Step 3: Write `TeamPanelScreenContainer.tsx`**

Create `mobile/src/features/teams/TeamPanelScreenContainer.tsx`:

```tsx
import React from "react";
import { StyleSheet, Text } from "react-native";
import { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useAuth } from "../../auth/AuthProvider";
import { mobileEnv } from "../../config/env";
import { AppStackParamList } from "../../navigation/types";
import { TeamPanelScreen } from "./TeamPanelScreen";

type Props = NativeStackScreenProps<AppStackParamList, "TeamPanel">;

export function TeamPanelScreenContainer({ navigation }: Props) {
  const { session } = useAuth();

  if (!session) {
    return <Text style={styles.message}>Sesion no disponible.</Text>;
  }

  return (
    <TeamPanelScreen
      apiBaseUrl={mobileEnv.gatewayApiBaseUrl}
      token={session.token}
      currentUserId={session.user.sub}
      navigation={navigation}
    />
  );
}

const styles = StyleSheet.create({
  message: {
    margin: 20,
    color: "#b91c1c",
  },
});
```

- [ ] **Step 4: Register the screen in `RootNavigator.tsx`**

In `mobile/src/navigation/RootNavigator.tsx`, add the import (after the `CreateTeamScreenContainer` import):

```tsx
import { TeamPanelScreenContainer } from "../features/teams/TeamPanelScreenContainer";
```

And add the screen registration (right after the `Home` screen entry):

```tsx
      <AppStack.Screen
        name="TeamPanel"
        component={TeamPanelScreenContainer}
        options={{ title: "Gestión de equipo" }}
      />
```

- [ ] **Step 5: Simplify `HomeScreen.tsx`**

In `mobile/src/screens/HomeScreen.tsx`, replace the entire "Tu equipo" section:

```tsx
      <AppText variant="label" color={game.onStageMuted} style={styles.sectionLabel}>
        Tu equipo
      </AppText>
      <View style={styles.group}>
        <NavCard icon="plus-circle" label="Crear equipo" onPress={() => navigation.navigate("CreateTeam")} />
        <NavCard icon="mail" label="Invitaciones" onPress={() => navigation.navigate("Invitations")} />
        <NavCard icon="user-plus" label="Invitar miembro" onPress={() => navigation.navigate("InviteMember")} />
        <NavCard icon="repeat" label="Transferir liderazgo" onPress={() => navigation.navigate("TransferLeadership")} />
        <NavCard icon="log-out" label="Salir del equipo" onPress={() => navigation.navigate("LeaveTeam")} />
        <NavCard icon="trash-2" label="Eliminar equipo" onPress={() => navigation.navigate("DeleteTeam")} />
        <NavCard icon="clock" label="Historial de equipos" onPress={() => navigation.navigate("TeamHistory")} />
        <NavCard icon="award" label="Rendimiento de mi equipo" onPress={() => navigation.navigate("RendimientoEquipo")} />
      </View>
```

with:

```tsx
      <AppText variant="label" color={game.onStageMuted} style={styles.sectionLabel}>
        Tu equipo
      </AppText>
      <View style={styles.group}>
        <NavCard
          icon="users"
          label="Gestión de equipo"
          sublabel="Tu equipo, integrantes y acciones"
          feature
          onPress={() => navigation.navigate("TeamPanel")}
        />
      </View>
```

- [ ] **Step 6: Typecheck**

Run (from `mobile/`): `npm run typecheck`
Expected: no errors.

- [ ] **Step 7: Run the full mobile test suite**

Run (from `mobile/`): `npm test`
Expected: PASS, same count as after Task 2 (Task 3 adds no new `.test.js` files — UI screens
in this codebase aren't unit-tested with React Native Testing Library, only their
`Flow`/`Api`/`Controller` logic modules are, matching the existing `teams/` pattern).

- [ ] **Step 8: Commit**

```bash
git add mobile/src/features/teams/TeamPanelScreen.tsx mobile/src/features/teams/TeamPanelScreenContainer.tsx mobile/src/navigation/types.ts mobile/src/navigation/RootNavigator.tsx mobile/src/screens/HomeScreen.tsx
git commit -m "$(cat <<'EOF'
feat(mobile): panel de gestion de equipo reemplaza las tarjetas sueltas

TeamPanelScreen muestra estado de membresia (sin equipo/lider/miembro),
nombre del equipo y lista de integrantes, con el set de botones que
corresponde a cada caso. HomeScreen pasa de 8 tarjetas fijas a 1 sola
tarjeta "Gestion de equipo" que navega al panel, mismo patron que
"Partidas" -> PartidasPanel.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Self-Review Notes

- **Spec coverage:** D1 (contrato extendido) → Task 1. D2 (panel único + tarjeta en Home) →
  Task 3. D3 (refetch a foco) → Task 3 Step 2 (`useFocusEffect`). D4 (patrón
  Container/Screen/Flow/Api) → Tasks 2+3. Los 3 estados y sus botones (tabla del spec) → Task 3
  Step 2 (`SinEquipoView` / `ConEquipoView`). Manejo de errores (404 = estado válido, no error)
  → Task 2 Step 3 (`loadMyTeam` devuelve `ok:true, data:null` en 404).
- **Placeholder scan:** none found — every step has complete, runnable code.
- **Type consistency:** `MiembroEquipoResponse(UsuarioId, Nombre, EsLider)` (Task 1, C#) →
  JSON `{ usuarioId, nombre, esLider }` → `Participante` type (Task 3, TS) — consistent field
  names/casing throughout. `fetchMyTeamStatus` return shape (Task 2) matches exactly what
  `TeamPanelScreen.tsx` destructures (Task 3): `status`, `nombreEquipo`, `participantes`.
