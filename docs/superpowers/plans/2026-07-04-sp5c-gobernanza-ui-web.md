# SP-5c — UI web de gobernanza — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** UI web (solo Administrador) para el panel de permisos por rol y el cambio de rol de usuarios, consumiendo los endpoints de gobernanza de SP-5b vía gateway.

**Architecture:** Tres funciones nuevas en el API client existente (`identityApi.ts`, patrón fetchImpl inyectable); página nueva `GovernancePage` (cards por rol, Guardar por card); modal inline en `UserManagementPage` (patrón del modal de `PublishedBdtGamesPage`). Cero backend, cero CSS nuevo (clases del design system), cero cambios a labels/ids/data-testid existentes.

**Tech Stack:** React 18 + Vite + TypeScript, react-router-dom, vitest + @testing-library/react.

**Spec:** `docs/superpowers/specs/2026-07-04-sp5c-gobernanza-ui-web-design.md` (commit 1be7dec).

## Global Constraints

- Commits terminan SOLO con: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
- `git add` archivo por archivo (nunca `-A`, `.` ni directorios).
- No modificar ningún `label`/`id`/`data-testid`/rol ARIA existente; solo agregar nuevos.
- Reutilizar clases CSS existentes (`.card`, `.stack`, `.notice`, `.modal-*`, `.pill`, `.secondary-button`, `.badge`, `.row`, `.muted`); no escribir CSS nuevo.
- Tests: correr desde `frontend/` con `npx vitest run <archivo>` (suite completa: `npm test`). Baseline actual: 53 tests / 9 archivos, todo verde.
- Shapes del backend son camelCase y se usan tal cual (sin capa de mapeo).

---

### Task 1 (H1): API client — getGovernanceRoles, updateRolePermissions, changeUserRole

**Files:**
- Modify: `frontend/src/api/identityApi.ts` (agregar al final; no tocar lo existente)
- Create: `frontend/src/api/identityApi.test.ts`

**Interfaces:**
- Consumes: helpers privados existentes del archivo (`resolveBaseUrl`, `buildAuthHeaders`, `parseJsonBody`, `throwIfNotOk`, `IdentityApiError`).
- Produces (Tasks 2 y 3 dependen de estas firmas exactas):
  - `type PermisoFuncional = "GestionarPartidas" | "GestionarEquipos" | "ParticiparEnPartidas"`
  - `interface RolePermissions { rol: "Administrador" | "Operador" | "Participante"; permisos: PermisoFuncional[]; privilegiosGobernanza: boolean }`
  - `interface GovernanceRolesResponse { roles: RolePermissions[] }`
  - `interface ChangeUserRoleResponse { usuarioId: string; rol: "Administrador" | "Operador" | "Participante" }`
  - `getGovernanceRoles(accessToken, fetchImpl?) : Promise<GovernanceRolesResponse>`
  - `updateRolePermissions(rol, permisos, accessToken, fetchImpl?) : Promise<RolePermissions>`
  - `changeUserRole(userId, rol, accessToken, fetchImpl?) : Promise<ChangeUserRoleResponse>`

- [ ] **Step 1: Tests (RED)**

Crear `frontend/src/api/identityApi.test.ts` (estilo de `bdtApi.test.ts`: `vi.stubEnv` + import dinámico + fetchMock):

```typescript
import { beforeEach, describe, expect, it, vi } from "vitest";

describe("identityApi governance", () => {
  beforeEach(() => {
    vi.resetModules();
    vi.unstubAllEnvs();
  });

  it("GET governance roles con bearer token", async () => {
    vi.stubEnv("VITE_IDENTITY_API_BASE_URL", "https://gw.example.test/");
    const { getGovernanceRoles } = await import("./identityApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        roles: [
          { rol: "Administrador", permisos: [], privilegiosGobernanza: true },
          { rol: "Operador", permisos: ["GestionarPartidas"], privilegiosGobernanza: false },
          {
            rol: "Participante",
            permisos: ["GestionarEquipos", "ParticiparEnPartidas"],
            privilegiosGobernanza: false
          }
        ]
      })
    });

    const result = await getGovernanceRoles("admin-token", fetchMock as unknown as typeof fetch);

    expect(fetchMock).toHaveBeenCalledWith("https://gw.example.test/identity/governance/roles", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer admin-token"
      }
    });
    expect(result.roles).toHaveLength(3);
    expect(result.roles[0].privilegiosGobernanza).toBe(true);
  });

  it("PUT permisos de un rol con set completo en el body", async () => {
    vi.stubEnv("VITE_IDENTITY_API_BASE_URL", "https://gw.example.test");
    const { updateRolePermissions } = await import("./identityApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ rol: "Operador", permisos: ["GestionarEquipos"], privilegiosGobernanza: false })
    });

    const result = await updateRolePermissions(
      "Operador",
      ["GestionarEquipos"],
      "admin-token",
      fetchMock as unknown as typeof fetch
    );

    expect(fetchMock).toHaveBeenCalledWith(
      "https://gw.example.test/identity/governance/roles/Operador/permisos",
      {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Authorization: "Bearer admin-token"
        },
        body: JSON.stringify({ permisos: ["GestionarEquipos"] })
      }
    );
    expect(result.permisos).toEqual(["GestionarEquipos"]);
  });

  it("PUT no-ok mapea a IdentityApiError con statusCode", async () => {
    vi.stubEnv("VITE_IDENTITY_API_BASE_URL", "https://gw.example.test");
    const { IdentityApiError, updateRolePermissions } = await import("./identityApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 502,
      json: async () => ({ message: "keycloak caido" })
    });

    await expect(
      updateRolePermissions("Operador", [], "admin-token", fetchMock as unknown as typeof fetch)
    ).rejects.toMatchObject({ name: "IdentityApiError", message: "keycloak caido", statusCode: 502 });

    await expect(
      updateRolePermissions("Operador", [], "admin-token", fetchMock as unknown as typeof fetch)
    ).rejects.toBeInstanceOf(IdentityApiError);
  });

  it("PATCH cambio de rol con body { rol }", async () => {
    vi.stubEnv("VITE_IDENTITY_API_BASE_URL", "https://gw.example.test");
    const { changeUserRole } = await import("./identityApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ usuarioId: "u1", rol: "Operador" })
    });

    const result = await changeUserRole("u1", "Operador", "admin-token", fetchMock as unknown as typeof fetch);

    expect(fetchMock).toHaveBeenCalledWith("https://gw.example.test/identity/users/u1/role", {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
        Authorization: "Bearer admin-token"
      },
      body: JSON.stringify({ rol: "Operador" })
    });
    expect(result.rol).toBe("Operador");
  });

  it("PATCH 409 conserva el mensaje del backend", async () => {
    vi.stubEnv("VITE_IDENTITY_API_BASE_URL", "https://gw.example.test");
    const { changeUserRole } = await import("./identityApi");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 409,
      json: async () => ({ message: "El usuario tiene un equipo activo" })
    });

    await expect(
      changeUserRole("u1", "Operador", "admin-token", fetchMock as unknown as typeof fetch)
    ).rejects.toMatchObject({ statusCode: 409, message: "El usuario tiene un equipo activo" });
  });
});
```

- [ ] **Step 2: Run RED**

Run: `cd frontend && npx vitest run src/api/identityApi.test.ts`
Expected: FAIL — `getGovernanceRoles` no exportada.

- [ ] **Step 3: Implementación**

Agregar al FINAL de `frontend/src/api/identityApi.ts`:

```typescript
export type PermisoFuncional = "GestionarPartidas" | "GestionarEquipos" | "ParticiparEnPartidas";

export interface RolePermissions {
  rol: "Administrador" | "Operador" | "Participante";
  permisos: PermisoFuncional[];
  privilegiosGobernanza: boolean;
}

export interface GovernanceRolesResponse {
  roles: RolePermissions[];
}

export interface ChangeUserRoleResponse {
  usuarioId: string;
  rol: "Administrador" | "Operador" | "Participante";
}

export async function getGovernanceRoles(
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<GovernanceRolesResponse> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/governance/roles`, {
    method: "GET",
    headers: buildAuthHeaders(accessToken)
  });

  const body = await parseJsonBody<GovernanceRolesResponse>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as GovernanceRolesResponse;
}

export async function updateRolePermissions(
  rol: string,
  permisos: PermisoFuncional[],
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<RolePermissions> {
  const response = await fetchImpl(
    `${resolveBaseUrl()}/identity/governance/roles/${rol}/permisos`,
    {
      method: "PUT",
      headers: buildAuthHeaders(accessToken),
      body: JSON.stringify({ permisos })
    }
  );

  const body = await parseJsonBody<RolePermissions>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as RolePermissions;
}

export async function changeUserRole(
  userId: string,
  rol: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<ChangeUserRoleResponse> {
  const response = await fetchImpl(`${resolveBaseUrl()}/identity/users/${userId}/role`, {
    method: "PATCH",
    headers: buildAuthHeaders(accessToken),
    body: JSON.stringify({ rol })
  });

  const body = await parseJsonBody<ChangeUserRoleResponse>(response);
  throwIfNotOk(response, body as { message?: string });
  return body as ChangeUserRoleResponse;
}
```

- [ ] **Step 4: GREEN + suite completa**

Run: `cd frontend && npx vitest run src/api/identityApi.test.ts` → 5/5 PASS.
Run: `cd frontend && npm test` → 58 tests / 10 archivos, verde (53 + 5).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/identityApi.ts frontend/src/api/identityApi.test.ts
git commit -m "feat(sp5c): API client de gobernanza — roles, permisos por rol y cambio de rol

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2 (H2): GovernancePage — panel de permisos + ruta + nav

**Files:**
- Create: `frontend/src/features/identity/GovernancePage.tsx`
- Create: `frontend/src/features/identity/GovernancePage.test.tsx`
- Modify: `frontend/src/app/App.tsx` (import + ruta tras `identidad/usuarios/nuevo`)
- Modify: `frontend/src/shell/navConfig.tsx` (import `Lock` + item en área `identidad`)

**Interfaces:**
- Consumes (Task 1): `getGovernanceRoles`, `updateRolePermissions`, `IdentityApiError`, `PermisoFuncional`, `RolePermissions`.
- Produces: `export function GovernancePage({ accessToken }: { accessToken: string })`; ruta `/identidad/gobernanza`.

- [ ] **Step 1: Tests (RED)**

Crear `frontend/src/features/identity/GovernancePage.test.tsx` (estilo `UserManagementPage.test.tsx`: `vi.spyOn` sobre el módulo API):

```tsx
import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { GovernancePage } from "./GovernancePage";
import * as identityApi from "../../api/identityApi";

const MATRIZ: identityApi.GovernanceRolesResponse = {
  roles: [
    { rol: "Administrador", permisos: [], privilegiosGobernanza: true },
    { rol: "Operador", permisos: ["GestionarPartidas"], privilegiosGobernanza: false },
    {
      rol: "Participante",
      permisos: ["GestionarEquipos", "ParticiparEnPartidas"],
      privilegiosGobernanza: false
    }
  ]
};

describe("GovernancePage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renderiza la matriz: 3 cards, badge de gobernanza solo en Administrador", async () => {
    vi.spyOn(identityApi, "getGovernanceRoles").mockResolvedValue(MATRIZ);

    render(<GovernancePage accessToken="token" />);

    expect(await screen.findByTestId("gov-card-Administrador")).toBeInTheDocument();
    expect(screen.getByTestId("gov-card-Operador")).toBeInTheDocument();
    expect(screen.getByTestId("gov-card-Participante")).toBeInTheDocument();
    expect(screen.getAllByTestId("gov-badge-admin")).toHaveLength(1);
    expect(screen.getByTestId("gov-check-Operador-GestionarPartidas")).toBeChecked();
    expect(screen.getByTestId("gov-check-Operador-GestionarEquipos")).not.toBeChecked();
  });

  it("guardar deshabilitado sin cambios; toggle habilita y el PUT manda el set completo", async () => {
    vi.spyOn(identityApi, "getGovernanceRoles").mockResolvedValue(MATRIZ);
    const putSpy = vi.spyOn(identityApi, "updateRolePermissions").mockResolvedValue({
      rol: "Operador",
      permisos: ["GestionarPartidas", "GestionarEquipos"],
      privilegiosGobernanza: false
    });

    render(<GovernancePage accessToken="token" />);

    const save = await screen.findByTestId("gov-save-Operador");
    expect(save).toBeDisabled();

    await userEvent.click(screen.getByTestId("gov-check-Operador-GestionarEquipos"));
    expect(save).toBeEnabled();

    await userEvent.click(save);

    expect(putSpy).toHaveBeenCalledWith(
      "Operador",
      ["GestionarPartidas", "GestionarEquipos"],
      "token"
    );
    // Tras el éxito el estado confirmado se actualiza: Guardar vuelve a deshabilitarse.
    expect(await screen.findByTestId("gov-save-Operador")).toBeDisabled();
  });

  it("502 al guardar muestra el mensaje de Keycloak en la card correcta y conserva lo marcado", async () => {
    vi.spyOn(identityApi, "getGovernanceRoles").mockResolvedValue(MATRIZ);
    vi.spyOn(identityApi, "updateRolePermissions").mockRejectedValue(
      new identityApi.IdentityApiError("bad gateway", 502)
    );

    render(<GovernancePage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("gov-check-Participante-GestionarEquipos"));
    await userEvent.click(screen.getByTestId("gov-save-Participante"));

    expect(await screen.findByTestId("gov-error-Participante")).toHaveTextContent(/keycloak no disponible/i);
    expect(screen.getByTestId("gov-check-Participante-GestionarEquipos")).not.toBeChecked();
    expect(screen.getByTestId("gov-save-Participante")).toBeEnabled();
  });

  it("error de carga muestra gov-load-error con reintento", async () => {
    const getSpy = vi
      .spyOn(identityApi, "getGovernanceRoles")
      .mockRejectedValueOnce(new identityApi.IdentityApiError("boom", 500))
      .mockResolvedValueOnce(MATRIZ);

    render(<GovernancePage accessToken="token" />);

    expect(await screen.findByTestId("gov-load-error")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /reintentar/i }));

    expect(await screen.findByTestId("gov-card-Operador")).toBeInTheDocument();
    expect(getSpy).toHaveBeenCalledTimes(2);
  });
});
```

- [ ] **Step 2: Run RED**

Run: `cd frontend && npx vitest run src/features/identity/GovernancePage.test.tsx`
Expected: FAIL — módulo `./GovernancePage` no existe.

- [ ] **Step 3: Página**

Crear `frontend/src/features/identity/GovernancePage.tsx`:

```tsx
import { useEffect, useState } from "react";
import {
  getGovernanceRoles,
  IdentityApiError,
  PermisoFuncional,
  RolePermissions,
  updateRolePermissions
} from "../../api/identityApi";
import { Lock } from "../../shell/icons";

interface GovernancePageProps {
  accessToken: string;
}

const PERMISOS: { key: PermisoFuncional; label: string }[] = [
  { key: "GestionarPartidas", label: "Gestionar partidas" },
  { key: "GestionarEquipos", label: "Gestionar equipos" },
  { key: "ParticiparEnPartidas", label: "Participar en partidas" }
];

interface CardState {
  info: RolePermissions;
  /* Último set confirmado por el servidor; Guardar solo se habilita si marked difiere. */
  confirmed: PermisoFuncional[];
  marked: PermisoFuncional[];
  saving: boolean;
  error: string | null;
  saved: boolean;
}

function sameSet(a: PermisoFuncional[], b: PermisoFuncional[]): boolean {
  return a.length === b.length && a.every((permiso) => b.includes(permiso));
}

function mapGovernanceError(caught: unknown): string {
  if (caught instanceof IdentityApiError) {
    if (caught.statusCode === 502) {
      return "Keycloak no disponible. Reintenta: volver a guardar repara el estado.";
    }
    return caught.message || "Error inesperado en Identity Service.";
  }
  return "Error inesperado al guardar permisos.";
}

export function GovernancePage({ accessToken }: GovernancePageProps) {
  const [cards, setCards] = useState<CardState[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    void loadMatriz();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadMatriz() {
    setIsLoading(true);
    setLoadError(null);
    try {
      const response = await getGovernanceRoles(accessToken);
      setCards(
        response.roles.map((info) => ({
          info,
          confirmed: info.permisos,
          marked: info.permisos,
          saving: false,
          error: null,
          saved: false
        }))
      );
    } catch (caught) {
      setLoadError(
        caught instanceof IdentityApiError
          ? caught.message || "No fue posible cargar la matriz de permisos."
          : "No fue posible cargar la matriz de permisos."
      );
    } finally {
      setIsLoading(false);
    }
  }

  function toggle(rol: string, permiso: PermisoFuncional) {
    setCards((current) =>
      current.map((card) => {
        if (card.info.rol !== rol) {
          return card;
        }
        const marked = card.marked.includes(permiso)
          ? card.marked.filter((p) => p !== permiso)
          : [...card.marked, permiso];
        return { ...card, marked, error: null, saved: false };
      })
    );
  }

  async function save(rol: string) {
    const card = cards.find((c) => c.info.rol === rol);
    if (!card || card.saving) {
      return;
    }

    setCards((current) =>
      current.map((c) => (c.info.rol === rol ? { ...c, saving: true, error: null, saved: false } : c))
    );

    try {
      /* PUT set completo (E5): el backend hace el diff server-side. */
      const permisosOrdenados = PERMISOS.map((p) => p.key).filter((p) => card.marked.includes(p));
      const updated = await updateRolePermissions(rol, permisosOrdenados, accessToken);
      setCards((current) =>
        current.map((c) =>
          c.info.rol === rol
            ? {
                ...c,
                confirmed: updated.permisos,
                marked: updated.permisos,
                saving: false,
                saved: true
              }
            : c
        )
      );
    } catch (caught) {
      setCards((current) =>
        current.map((c) =>
          c.info.rol === rol ? { ...c, saving: false, error: mapGovernanceError(caught) } : c
        )
      );
    }
  }

  return (
    <div className="page">
      <div className="stack">
        <div>
          <h1>Gobernanza</h1>
          <p className="muted">
            Permisos funcionales por rol. Los cambios se aplican primero en Keycloak y luego se
            registran en UMBRAL; los usuarios los reciben en su próximo token.
          </p>
        </div>

        {loadError ? (
          <div className="notice error" role="alert" data-testid="gov-load-error">
            {loadError}{" "}
            <button type="button" className="secondary-button" onClick={loadMatriz}>
              Reintentar
            </button>
          </div>
        ) : null}

        {isLoading ? <p className="muted">Cargando matriz de permisos…</p> : null}

        {cards.map((card) => (
          <div className="card stack" key={card.info.rol} data-testid={`gov-card-${card.info.rol}`}>
            <div className="card-head">
              <h2 className="q-title">{card.info.rol}</h2>
              {card.info.privilegiosGobernanza ? (
                <span className="badge" data-testid="gov-badge-admin">
                  <Lock /> Privilegios de gobernanza — protegidos
                </span>
              ) : null}
            </div>

            {PERMISOS.map((permiso) => (
              <label key={permiso.key} className="row">
                <input
                  type="checkbox"
                  data-testid={`gov-check-${card.info.rol}-${permiso.key}`}
                  checked={card.marked.includes(permiso.key)}
                  disabled={card.saving}
                  onChange={() => toggle(card.info.rol, permiso.key)}
                />
                {permiso.label}
              </label>
            ))}

            {card.error ? (
              <div className="notice error" role="alert" data-testid={`gov-error-${card.info.rol}`}>
                {card.error}
              </div>
            ) : null}

            {card.saved ? (
              <p className="muted" role="status">
                Permisos de {card.info.rol} guardados.
              </p>
            ) : null}

            <div className="row">
              <button
                type="button"
                data-testid={`gov-save-${card.info.rol}`}
                disabled={card.saving || sameSet(card.marked, card.confirmed)}
                onClick={() => save(card.info.rol)}
              >
                {card.saving ? "Guardando…" : "Guardar"}
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Ruta + nav**

`frontend/src/app/App.tsx` — agregar import junto a los de identity:

```tsx
import { GovernancePage } from "../features/identity/GovernancePage";
```

y la ruta inmediatamente DESPUÉS del bloque de `identidad/usuarios/nuevo`:

```tsx
          {
            path: "identidad/gobernanza",
            element: (
              <RequireRole roles={roles} need="Administrador" landing={landing}>
                <GovernancePage accessToken={token} />
              </RequireRole>
            )
          },
```

`frontend/src/shell/navConfig.tsx` — agregar `Lock` al import de `./icons` y el item al final de `items` del área `identidad`:

```tsx
      { label: "Gobernanza", path: "/identidad/gobernanza", icon: Lock }
```

(`titleForPath` lo resuelve solo — recorre `NAV_AREAS`.)

- [ ] **Step 5: GREEN + suite completa**

Run: `cd frontend && npx vitest run src/features/identity/GovernancePage.test.tsx` → 4/4 PASS.
Run: `cd frontend && npm test` → 62 tests / 11 archivos, verde (58 + 4; `App.test.tsx` no asserta el número de items del nav — si algo falla ahí, leer el test y ajustar SOLO si el fallo es por el item nuevo, documentándolo en el reporte).
Run: `cd frontend && npm run build` → sin errores de tsc.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/identity/GovernancePage.tsx frontend/src/features/identity/GovernancePage.test.tsx frontend/src/app/App.tsx frontend/src/shell/navConfig.tsx
git commit -m "feat(sp5c): página Gobernanza — matriz de permisos por rol con guardado por card

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3 (H3): Cambio de rol desde Gestión de usuarios

**Files:**
- Modify: `frontend/src/features/identity/UserManagementPage.tsx`
- Modify: `frontend/src/features/identity/UserManagementPage.test.tsx` (solo AGREGAR tests)

**Interfaces:**
- Consumes (Task 1): `changeUserRole`, `IdentityApiError`; tipos existentes `IdentityUserSummary`.
- Produces: modal de cambio de rol con testids `role-change-*` (sección 4 del spec).

- [ ] **Step 1: Tests (RED)**

AGREGAR al final del `describe` de `frontend/src/features/identity/UserManagementPage.test.tsx` (helper local para no repetir mocks):

```tsx
  function mockListWith(users: identityApi.IdentityUserSummary[]) {
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue(users);
  }

  const ANA: identityApi.IdentityUserSummary = {
    userId: "u1",
    keycloakId: "k1",
    name: "Ana",
    email: "ana@demo.com",
    role: "Participante",
    status: "Activo"
  };

  const ROOT: identityApi.IdentityUserSummary = {
    userId: "u2",
    keycloakId: "k2",
    name: "Root",
    email: "root@demo.com",
    role: "Administrador",
    status: "Activo"
  };

  it("deshabilita Cambiar rol para un Administrador con title explicativo", async () => {
    mockListWith([ROOT]);

    render(<UserManagementPage accessToken="token" />);

    const button = await screen.findByTestId("role-change-open-u2");
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute("title", "El rol de un Administrador es inmutable.");
  });

  it("cambio a rol no-admin en un click: llama API, actualiza fila y cierra modal", async () => {
    mockListWith([ANA]);
    const changeSpy = vi.spyOn(identityApi, "changeUserRole").mockResolvedValue({
      usuarioId: "u1",
      rol: "Operador"
    });

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("role-change-open-u1"));
    const select = screen.getByTestId("role-change-select");
    // El rol actual no es opción.
    expect(within(select).queryByRole("option", { name: "Participante" })).toBeNull();
    await userEvent.selectOptions(select, "Operador");
    await userEvent.click(screen.getByTestId("role-change-confirm"));

    expect(changeSpy).toHaveBeenCalledWith("u1", "Operador", "token");
    await waitFor(() => expect(screen.queryByTestId("role-change-modal")).toBeNull());
    expect(screen.getByRole("cell", { name: "Operador" })).toBeInTheDocument();
  });

  it("promover a Administrador exige segundo click y no llama a la API en el primero", async () => {
    mockListWith([ANA]);
    const changeSpy = vi.spyOn(identityApi, "changeUserRole").mockResolvedValue({
      usuarioId: "u1",
      rol: "Administrador"
    });

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("role-change-open-u1"));
    await userEvent.selectOptions(screen.getByTestId("role-change-select"), "Administrador");

    expect(screen.getByTestId("role-change-warning")).toHaveTextContent(/irreversible/i);

    await userEvent.click(screen.getByTestId("role-change-confirm"));
    expect(changeSpy).not.toHaveBeenCalled();
    expect(screen.getByTestId("role-change-confirm")).toHaveTextContent("Entiendo, promover");

    await userEvent.click(screen.getByTestId("role-change-confirm"));
    expect(changeSpy).toHaveBeenCalledWith("u1", "Administrador", "token");
  });

  it("409 del backend queda inline en el modal sin cerrarlo", async () => {
    mockListWith([ANA]);
    vi.spyOn(identityApi, "changeUserRole").mockRejectedValue(
      new identityApi.IdentityApiError("El usuario u1 tiene un equipo activo", 409)
    );

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("role-change-open-u1"));
    await userEvent.selectOptions(screen.getByTestId("role-change-select"), "Operador");
    await userEvent.click(screen.getByTestId("role-change-confirm"));

    expect(await screen.findByTestId("role-change-error")).toHaveTextContent(/equipo activo/i);
    expect(screen.getByTestId("role-change-modal")).toBeInTheDocument();
  });

  it("502 muestra mensaje de Keycloak inline", async () => {
    mockListWith([ANA]);
    vi.spyOn(identityApi, "changeUserRole").mockRejectedValue(
      new identityApi.IdentityApiError("bad gateway", 502)
    );

    render(<UserManagementPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("role-change-open-u1"));
    await userEvent.selectOptions(screen.getByTestId("role-change-select"), "Operador");
    await userEvent.click(screen.getByTestId("role-change-confirm"));

    expect(await screen.findByTestId("role-change-error")).toHaveTextContent(/keycloak no disponible/i);
  });
```

Ajustar el import de testing-library del archivo a:

```tsx
import { render, screen, waitFor, within } from "@testing-library/react";
```

- [ ] **Step 2: Run RED**

Run: `cd frontend && npx vitest run src/features/identity/UserManagementPage.test.tsx`
Expected: FAIL — testid `role-change-open-u2` no existe (los 6 tests existentes siguen PASS).

- [ ] **Step 3: Implementación**

En `frontend/src/features/identity/UserManagementPage.tsx`:

1. Ampliar el import del API:

```tsx
import {
  changeUserRole,
  deactivateIdentityUser,
  getIdentityUserById,
  getIdentityUsers,
  IdentityApiError,
  IdentityUserDetail,
  IdentityUserSummary,
  updateIdentityUserGeneralData
} from "../../api/identityApi";
```

2. Estado nuevo (junto a los useState existentes):

```tsx
  const [roleModalUser, setRoleModalUser] = useState<IdentityUserSummary | null>(null);
  const [roleTarget, setRoleTarget] = useState("");
  const [roleArmed, setRoleArmed] = useState(false);
  const [roleError, setRoleError] = useState<string | null>(null);
  const [roleSaving, setRoleSaving] = useState(false);
  const [roleSuccess, setRoleSuccess] = useState<string | null>(null);
```

3. Handlers (tras `onDeactivateUser`):

```tsx
  function openRoleModal(user: IdentityUserSummary) {
    setRoleModalUser(user);
    setRoleTarget("");
    setRoleArmed(false);
    setRoleError(null);
    setRoleSuccess(null);
  }

  function closeRoleModal() {
    if (roleSaving) {
      return;
    }
    setRoleModalUser(null);
  }

  async function onChangeRole() {
    if (!roleModalUser || !roleTarget) {
      return;
    }

    // Promoción a admin es irreversible: primer click arma, segundo ejecuta.
    if (roleTarget === "Administrador" && !roleArmed) {
      setRoleArmed(true);
      return;
    }

    setRoleSaving(true);
    setRoleError(null);
    try {
      const response = await changeUserRole(roleModalUser.userId, roleTarget, accessToken);
      setUsers((current) =>
        current.map((user) =>
          user.userId === response.usuarioId ? { ...user, role: response.rol } : user
        )
      );
      setRoleSuccess(`Rol de ${roleModalUser.name} actualizado a ${response.rol}.`);
      setRoleModalUser(null);
    } catch (caught) {
      setRoleError(mapRoleChangeError(caught));
    } finally {
      setRoleSaving(false);
    }
  }
```

4. Mapper (junto a `mapHu02ErrorMessage`, al final del archivo):

```tsx
function mapRoleChangeError(caught: unknown): string {
  if (!(caught instanceof IdentityApiError)) {
    return "Error inesperado al cambiar el rol.";
  }
  switch (caught.statusCode) {
    case 502:
      return "Keycloak no disponible. Inténtalo de nuevo.";
    case 409:
    case 400:
    case 404:
      return caught.message || "No fue posible cambiar el rol.";
    default:
      return caught.message || "Error inesperado en Identity Service.";
  }
}
```

5. Tabla: agregar columnas Rol y Acciones. En `<thead>`:

```tsx
                      <th scope="col">Estado</th>
                      <th scope="col">Rol</th>
                      <th scope="col">Acciones</th>
```

y en cada `<tr>` del body, tras la celda de `StatusPill`:

```tsx
                        <td>{user.role}</td>
                        <td>
                          <button
                            type="button"
                            className="secondary-button"
                            data-testid={`role-change-open-${user.userId}`}
                            disabled={user.role === "Administrador"}
                            title={
                              user.role === "Administrador"
                                ? "El rol de un Administrador es inmutable."
                                : undefined
                            }
                            onClick={() => openRoleModal(user)}
                          >
                            Cambiar rol
                          </button>
                        </td>
```

6. Notice de éxito de la página (inmediatamente después del bloque de `listError`):

```tsx
        {roleSuccess ? (
          <div className="notice success" role="status" data-testid="role-change-success">
            {roleSuccess}
          </div>
        ) : null}
```

7. Modal (al final del JSX, justo antes del cierre de `.page`, patrón del modal de `PublishedBdtGamesPage`):

```tsx
        {roleModalUser ? (
          <div className="modal-backdrop" role="presentation">
            <section
              className="modal-card"
              role="dialog"
              aria-modal="true"
              aria-labelledby="role-change-title"
              data-testid="role-change-modal"
            >
              <div className="modal-header">
                <div>
                  <span className="badge">Cambiar rol</span>
                  <h2 id="role-change-title">{roleModalUser.name}</h2>
                </div>
                <button type="button" className="secondary-button" onClick={closeRoleModal}>
                  Cerrar
                </button>
              </div>

              <p className="muted">
                {roleModalUser.email} · Rol actual: <strong>{roleModalUser.role}</strong>
              </p>

              <label htmlFor="role-change-select">
                Nuevo rol
                <select
                  id="role-change-select"
                  data-testid="role-change-select"
                  value={roleTarget}
                  disabled={roleSaving}
                  onChange={(event) => {
                    setRoleTarget(event.target.value);
                    setRoleArmed(false);
                    setRoleError(null);
                  }}
                >
                  <option value="">Selecciona un rol…</option>
                  {(["Administrador", "Operador", "Participante"] as const)
                    .filter((rol) => rol !== roleModalUser.role)
                    .map((rol) => (
                      <option key={rol} value={rol}>
                        {rol}
                      </option>
                    ))}
                </select>
              </label>

              {roleTarget === "Administrador" ? (
                <div className="notice" role="alert" data-testid="role-change-warning">
                  Promover a Administrador es irreversible: el rol de un administrador no puede
                  volver a cambiarse.
                </div>
              ) : null}

              {roleError ? (
                <div className="notice error" role="alert" data-testid="role-change-error">
                  {roleError}
                </div>
              ) : null}

              <div className="row">
                <button
                  type="button"
                  data-testid="role-change-confirm"
                  disabled={roleSaving || !roleTarget}
                  onClick={onChangeRole}
                >
                  {roleSaving
                    ? "Cambiando…"
                    : roleTarget === "Administrador" && roleArmed
                      ? "Entiendo, promover"
                      : "Cambiar rol"}
                </button>
                <button
                  type="button"
                  className="secondary-button"
                  disabled={roleSaving}
                  onClick={closeRoleModal}
                >
                  Cancelar
                </button>
              </div>
            </section>
          </div>
        ) : null}
```

8. Actualizar la prosa de cabecera de la página (dejó de ser cierta):

```tsx
          <p className="muted">
            Consulta, actualiza datos generales, desactiva usuarios y cambia el rol de operadores y
            participantes. El rol de un Administrador es inmutable.
          </p>
```

(Ningún test existente asserta ese texto — verificado. No tocar ningún otro label/id/testid.)

- [ ] **Step 4: GREEN + suite completa**

Run: `cd frontend && npx vitest run src/features/identity/UserManagementPage.test.tsx` → 11/11 PASS (6 existentes + 5 nuevos).
Run: `cd frontend && npm test` → 67 tests / 11 archivos, verde.
Run: `cd frontend && npm run build` → sin errores de tsc.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/identity/UserManagementPage.tsx frontend/src/features/identity/UserManagementPage.test.tsx
git commit -m "feat(sp5c): cambio de rol desde gestión de usuarios — modal con confirmación reforzada para promover a admin

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4 (H4): Docs — traceability + estado del redesign

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md`
- Modify: `docs/02-project-context/design/frontend-redesign-plan.md` (una línea de estado si el doc lista superficies web; si no tiene sección aplicable, omitirlo y documentar por qué)

**Interfaces:**
- Consumes: hashes reales de H1-H3 (`git log --oneline -4`), fila SP-5b existente como plantilla.

- [ ] **Step 1: Traceability**

Agregar fila SP-5c debajo de la fila SP-5b, con la MISMA cantidad de columnas (verificar con `awk -F'|' '{print NF}'` que header, SP-5b y SP-5c coinciden). Contenido: spec `2026-07-04-sp5c-gobernanza-ui-web-design.md` (1be7dec), plan (hash real del commit del plan), commits H1-H3 reales, suite frontend final real (67 tests / 11 archivos). Sin pipes internos en celdas (usar `/` o `·`).

- [ ] **Step 2: Redesign plan**

Si `frontend-redesign-plan.md` tiene tabla/lista de superficies por página: agregar una línea para "Gobernanza (`/identidad/gobernanza`)" con estado "construida sobre design system (SP-5c)". Si el doc no tiene sección aplicable, no tocarlo.

- [ ] **Step 3: Verificación + commit**

Run: `git diff --check` → limpio.

```bash
git add docs/04-sdd/traceability-matrix.md
# + docs/02-project-context/design/frontend-redesign-plan.md solo si cambió
git commit -m "docs(sp5c): fila de traceability + estado de la superficie Gobernanza

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Verificación final del slice (controller, antes del review whole-branch)

```bash
cd frontend && npm test        # 67 / 11 verde
cd frontend && npm run build   # tsc + bundle sin errores
```

Todo verde + review final whole-branch (opus) sobre el rango del slice (spec 1be7dec → HEAD).
