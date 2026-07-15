# Web gateada por privilegio — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que asignar «Gestionar partidas» a un rol **haga aparecer el área Partidas** en su panel — el síntoma que originó todo el trabajo.

**Architecture:** La web pasa a leer los privilegios del token (viajan en `realm_access.roles` como realm roles composite, ADR-0013) y a abrir **áreas enteras** por privilegio en vez de por rol base. El backend cierra las policies que faltan con **rol AND privilegio**.

**Tech Stack:** React 18 + Vite + TypeScript + Vitest + keycloak-js; .NET 8 (Identity, Partidas, Puntuaciones).

**Spec:** `docs/superpowers/specs/2026-07-15-web-gateada-por-privilegio-design.md`

## Global Constraints

- **Comentarios y documentación en español, con acentos correctos** ("está", "propósito", "así", "prometería"). Esto ya falló dos veces en el sub-proyecto 1.
- **Nunca hacer push ni merge.** Commits locales en `feature/fixes-santiago`.
- **Prohibido `git stash`, `git reset`, `git restore`, `git clean`.** Sólo `git add <rutas exactas>` y `git commit`. Excepción: los `git show <sha>:<ruta>` para leer código revertido.
- **Todo commit termina con:** `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
- **Ledger:** tras cada tarea, una línea en `.git/sdd/progress.md`.
- **Regla del frontend del proyecto:** no cambiar `label`, `id`, `data-testid` ni roles ARIA de los que dependen los tests, salvo que la tarea lo pida.
- **Los privilegios gobernables son exactamente dos:** `GestionarPartidas` y `GestionarEquipos`. `ParticiparEnPartidas` existe en el dominio pero está fijo al rol `Participante` y **la web no lo usa para nada**.
- **Defaults vigentes** (ya aplicados en el entorno): Administrador → `GestionarEquipos`; Operador → `GestionarPartidas`; Participante → ninguno.
- **`roles` y `permisos` van en campos separados, nunca mezclados.** `AppShell.tsx:44` hace `roles.join(" · ")` y lo muestra en la barra superior: mezclarlos enseñaría «Administrador · GestionarEquipos» al usuario.
- **NO arrancar Docker.** La verificación en vivo es la tarea 6 y requiere autorización del usuario.

## Código recuperable

El intento anterior se revirtió en `60ce104`. Para leer el original: `git show 2fabefd:<ruta>`.

| Archivo | Estado |
|---|---|
| `frontend/src/auth/keycloak.ts` | Recuperable casi tal cual (Task 1 lo ajusta) |
| `frontend/src/shell/AppShell.tsx`, `NavRail.tsx` | Recuperables tal cual (Task 4) |
| `frontend/src/app/App.tsx`, `shell/navConfig.tsx` | **Rehacer**: aquel intento sólo gateaba «Nueva partida»; el modelo nuevo gatea el área entera |

## File Structure

| Archivo | Responsabilidad | Tarea |
|---|---|---|
| `frontend/src/auth/keycloak.ts` | Extraer privilegios del token; `refresh()` devuelve el `AuthUser` completo | 1 |
| `frontend/src/auth/useSessionRefresh.ts` | Propagar el `AuthUser` re-parseado, no sólo el string | 2 |
| `frontend/src/app/App.tsx` | Estado de sesión (T2); guardias de ruta y pantalla sin accesos (T4) | 2, 4 |
| `frontend/src/shell/navConfig.tsx` | Qué áreas ve cada quien; landing | 3 |
| `frontend/src/shell/AppShell.tsx`, `NavRail.tsx` | Pasar `permisos` al nav | 4 |
| Controllers de Identity, Partidas, Puntuaciones | Policies rol AND privilegio | 5 |

---

### Task 1: La web lee los privilegios del token

Hoy `extractRoles` filtra por `knownRoles` y **tira `GestionarPartidas` a la basura**. Ésta es la raíz del síntoma: el token trae el privilegio y la web lo descarta al parsear.

Además, `refresh()` devuelve `string`, así que al refrescar (cada 270s) los privilegios quedan congelados los del login. Esta tarea cambia el contrato para que devuelva el `AuthUser` re-parseado; la Task 2 lo consume.

**Files:**
- Modify: `frontend/src/auth/keycloak.ts`
- Test: `frontend/src/auth/keycloak.test.ts` (crear)

**Interfaces:**
- Produces:
  - `AuthUser` gana `permisos: string[]`.
  - `extractPermisos(parsed): string[]` y `extractRoles(parsed): string[]`, **exportadas** (funciones puras, testeables).
  - `AuthProvider.refresh(): Promise<AuthUser>` (era `Promise<string>`). **Task 2 depende de esta firma.**

- [ ] **Step 1: Escribir el test que falla**

Crear `frontend/src/auth/keycloak.test.ts`:

```ts
import { describe, expect, it } from "vitest";
import { extractPermisos, extractRoles } from "./keycloak";

/* Los privilegios viajan en el mismo realm_access.roles que los roles base: ADR-0013 los modela
   como realm roles composite y Keycloak los expande solo. Se extraen aparte para no mezclarlos:
   el shell muestra `roles` al usuario. */
describe("extracción de credenciales del token", () => {
  const tokenCon = (roles: string[]) => ({ realm_access: { roles } }) as never;

  it("extrae los privilegios gobernables, no sólo los roles base", () => {
    const parsed = tokenCon(["Administrador", "GestionarPartidas", "GestionarEquipos"]);

    expect(extractPermisos(parsed)).toEqual(["GestionarPartidas", "GestionarEquipos"]);
  });

  it("no mezcla los privilegios con los roles base", () => {
    const parsed = tokenCon(["Administrador", "GestionarPartidas"]);

    expect(extractRoles(parsed)).toEqual(["Administrador"]);
  });

  it("descarta los roles técnicos de Keycloak", () => {
    const parsed = tokenCon(["Operador", "offline_access", "uma_authorization", "default-roles-umbral"]);

    expect(extractRoles(parsed)).toEqual(["Operador"]);
    expect(extractPermisos(parsed)).toEqual([]);
  });

  it("devuelve listas vacías si el token no trae realm_access", () => {
    expect(extractRoles(undefined)).toEqual([]);
    expect(extractPermisos(undefined)).toEqual([]);
  });
});
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `cd frontend && npx vitest run src/auth/keycloak.test.ts`
Expected: FAIL — `extractPermisos` no existe y `extractRoles` no está exportada.

- [ ] **Step 3: Añadir el mapa de privilegios**

En `keycloak.ts`, después del mapa `knownRoles` (línea ~26):

```ts
// Los privilegios funcionales viajan en el mismo `realm_access.roles` que los roles base: ADR-0013
// los modela como realm roles composite de Keycloak. Se extraen aparte para no mezclarlos con el rol
// base (el shell muestra `roles` al usuario).
// Sólo los dos gobernables: ParticiparEnPartidas está fijo al rol Participante y la web no lo usa.
const knownPermisos = new Map<string, string>([
  ["gestionarpartidas", "GestionarPartidas"],
  ["gestionarequipos", "GestionarEquipos"]
]);
```

- [ ] **Step 4: Extraer el filtro común y exportar los dos extractores**

Sustituir la función `extractRoles` (líneas ~115-131) por:

```ts
function extractKnown(
  parsed: Keycloak.KeycloakTokenParsed | undefined,
  known: Map<string, string>
): string[] {
  const realmRoles = Array.isArray(parsed?.realm_access?.roles) ? parsed.realm_access.roles : [];

  const resourceAccess = parsed?.resource_access ?? {};
  const clientRoles = Object.values(resourceAccess).flatMap((client) =>
    Array.isArray(client?.roles) ? client.roles : []
  );

  // Keep only recognized UMBRAL app roles; drop Keycloak technical roles.
  return Array.from(
    new Set(
      [...realmRoles, ...clientRoles]
        .map((role) => known.get(role.trim().toLowerCase()))
        .filter((role): role is string => Boolean(role))
    )
  );
}

export function extractRoles(parsed: Keycloak.KeycloakTokenParsed | undefined): string[] {
  return extractKnown(parsed, knownRoles);
}

export function extractPermisos(parsed: Keycloak.KeycloakTokenParsed | undefined): string[] {
  return extractKnown(parsed, knownPermisos);
}
```

- [ ] **Step 5: Añadir `permisos` a `AuthUser` y cambiar el contrato de `refresh`**

En la interfaz `AuthUser` (línea ~3):

```ts
export interface AuthUser {
  username: string;
  /** Roles base del usuario: Administrador / Operador / Participante. */
  roles: string[];
  /** Privilegios funcionales del rol: el privilegio autoriza, no el rol. */
  permisos: string[];
  token: string;
}
```

En la interfaz `AuthProvider` (línea ~16), cambiar la firma y su comentario:

```ts
  /**
   * Fuerza el refresh del token contra Keycloak (RNF-24). Resuelve al usuario re-parseado del token
   * nuevo, no sólo al string: si el administrador cambió los privilegios del rol, el token nuevo los
   * trae y la sesión debe reflejarlos sin obligar a cerrar sesión.
   */
  refresh(): Promise<AuthUser>;
```

- [ ] **Step 6: Construir el `AuthUser` en un solo sitio**

En la clase `KeycloakAuthProvider`, añadir un método privado y usarlo desde `init` y `refresh`:

```ts
  private usuarioDelToken(): AuthUser {
    const parsed = this.keycloak.tokenParsed;
    const username =
      (parsed?.preferred_username as string | undefined) ??
      (parsed?.name as string | undefined) ??
      "unknown";

    return {
      username,
      roles: extractRoles(parsed),
      permisos: extractPermisos(parsed),
      token: this.keycloak.token as string
    };
  }
```

En `init` (líneas ~64-82), sustituir el cuerpo del `.then(...)` por:

```ts
      .then((authenticated) => {
        if (!authenticated || !this.keycloak.token) {
          return null;
        }

        return this.usuarioDelToken();
      })
```

En `refresh` (líneas ~103-112):

```ts
  async refresh(): Promise<AuthUser> {
    // -1 fuerza el refresh aunque el token siga válido: RNF-24 pide refresh
    // incondicional en cada tick de 270s. keycloak-js usa el refresh token
    // internamente, directo contra Keycloak (sin gateway/backend).
    await this.keycloak.updateToken(-1);
    if (!this.keycloak.token) {
      throw new Error("Keycloak no devolvió token tras el refresh.");
    }
    return this.usuarioDelToken();
  }
```

- [ ] **Step 7: Ejecutar el test enfocado**

Run: `cd frontend && npx vitest run src/auth/keycloak.test.ts`
Expected: PASS (4 tests).

- [ ] **Step 8: Comprobar el alcance de la rotura**

Run: `cd frontend && npx tsc --noEmit`
Expected: **errores esperados** en `useSessionRefresh.ts` y `App.tsx` — consumen `refresh()` como `string` y no pasan `permisos`. **Los arregla la Task 2. No los toques aquí.** Anota en tu reporte qué archivos salen.

- [ ] **Step 9: Commit**

```bash
git add frontend/src/auth/keycloak.ts frontend/src/auth/keycloak.test.ts
git commit -m "feat(web): lee los privilegios funcionales del token

La raiz del sintoma: knownRoles solo reconocia los tres roles base, asi que
GestionarPartidas llegaba en realm_access.roles y se descartaba al parsear. El
panel escribia privilegios que el cliente tiraba a la basura.

refresh() pasa a devolver el AuthUser re-parseado en vez del string del token:
si el admin cambia los privilegios de un rol, el token nuevo los trae y la
sesion debe reflejarlos sin obligar a cerrar sesion.

Deja tsc en rojo a proposito: los consumidores del contrato viejo los actualiza
la tarea siguiente.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: El refresh deja de congelar los privilegios

Cierra la rotura que dejó la Task 1. Hoy `App.tsx:60` hace `{ ...prev.user, token }`: cambia el string y **conserva los roles y privilegios del login**. Con los privilegios moviéndose desde el panel, eso significa que un cambio de gobernanza no surte efecto hasta cerrar sesión.

**Files:**
- Modify: `frontend/src/auth/useSessionRefresh.ts:10` (firma de `onToken`)
- Modify: `frontend/src/app/App.tsx:59-70`
- Test: `frontend/src/auth/useSessionRefresh.test.ts`

**Interfaces:**
- Consumes: `AuthProvider.refresh(): Promise<AuthUser>` y `AuthUser` (Task 1).
- Produces: `useSessionRefresh`'s `onToken: (user: AuthUser) => void`.

- [ ] **Step 1: Escribir el test que falla**

Añadir a `frontend/src/auth/useSessionRefresh.test.ts`. **Lee el archivo primero** y sigue su patrón de mock de `authProvider` — el mock existente devuelve un string desde `refresh()` y hay que actualizarlo a un `AuthUser`.

```ts
  /* El admin puede cambiar los privilegios de un rol en cualquier momento. Si el refresh sólo
     renovara el string del token, la sesión seguiría con los privilegios del login y el cambio no
     surtiría efecto hasta cerrar sesión. */
  it("propaga los privilegios nuevos del token refrescado", async () => {
    const usuarioRefrescado = {
      username: "admin",
      roles: ["Administrador"],
      permisos: ["GestionarPartidas"],
      token: "token-nuevo"
    };
    vi.spyOn(authProvider, "refresh").mockResolvedValue(usuarioRefrescado);
    const onToken = vi.fn();

    renderHook(() => useSessionRefresh({ enabled: true, onToken, onExpired: vi.fn() }));
    await act(async () => {
      vi.advanceTimersByTime(REFRESH_INTERVAL_MS);
    });

    expect(onToken).toHaveBeenCalledWith(usuarioRefrescado);
  });
```

> Si el archivo usa otro estilo (por ejemplo dispara el tick de otra forma), **adapta el test a ese
> estilo** en vez de introducir uno nuevo. Lo que no es negociable es lo que asevera: que `onToken`
> recibe el usuario completo con los privilegios nuevos.

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `cd frontend && npx vitest run src/auth/useSessionRefresh.test.ts`
Expected: FAIL — `onToken` recibe un string, no el usuario.

- [ ] **Step 3: Cambiar la firma de `onToken`**

En `useSessionRefresh.ts`, importar el tipo y cambiar la firma (líneas 3-11):

```ts
import { authProvider, type AuthUser } from "./keycloak";
```

```ts
export function useSessionRefresh(opts: {
  enabled: boolean;
  onToken: (user: AuthUser) => void;
  onExpired: () => void;
}): { modalVisible: boolean; continuar: () => void } {
```

Y en el `crearSessionRefreshCore` (líneas 23-31), renombrar la variable para que diga lo que es:

```ts
      refrescar: () =>
        authProvider.refresh().then(
          (user) => {
            onTokenRef.current(user);
            return true;
          },
          () => false
        ),
```

`sessionRefreshCore` no se toca: sólo consume el booleano.

- [ ] **Step 4: Consumirlo en `App.tsx`**

En `App.tsx`, sustituir el `onToken` (líneas ~61-64):

```ts
    onToken: (user) => setAuthState({ status: "ready", user }),
```

Sustituye el objeto entero en vez de fusionar sobre el previo: el token nuevo trae roles y
privilegios frescos, y conservar los viejos es justo el bug.

- [ ] **Step 5: Ejecutar los tests y el typecheck**

Run: `cd frontend && npm test && npx tsc --noEmit`
Expected: PASS y typecheck limpio.

Si `App.test.tsx` falla porque sus mocks de `authProvider.init` no devuelven `permisos`, **añade
`permisos: []`** a esos mocks (o el valor que corresponda al rol que simulan: `["GestionarPartidas"]`
para un operador). El contrato real siempre lo trae, así que el mock debe traerlo — **no añadas un
`?? []` defensivo en el código de producción para tapar un mock incompleto.**

- [ ] **Step 6: Commit**

```bash
git add frontend/src/auth/useSessionRefresh.ts frontend/src/auth/useSessionRefresh.test.ts frontend/src/app/App.tsx frontend/src/app/App.test.tsx
git commit -m "fix(web): el refresh deja de congelar los privilegios del login

App.tsx conservaba roles y privilegios del estado previo y solo renovaba el
string del token, asi que un cambio de gobernanza no surtia efecto hasta cerrar
sesion. Con los privilegios moviendose desde el panel, eso se nota: los roles
casi nunca cambian, los privilegios si.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: El nav abre las áreas por privilegio

**Files:**
- Modify: `frontend/src/shell/navConfig.tsx`
- Test: `frontend/src/shell/navConfig.test.tsx`

**Interfaces:**
- Produces:
  - `NavAreaDef` gana `permisos?: readonly string[]`.
  - `areasForRoles(roles: string[], permisos?: string[]): NavAreaDef[]`.
  - `landingPath(roles: string[], permisos?: string[]): string | null` — **`null` significa «ninguna área»**. Task 4 depende de esto.

- [ ] **Step 1: Escribir los tests que fallan**

Añadir a `frontend/src/shell/navConfig.test.tsx` (lee el archivo primero y sigue su estilo):

```tsx
  /* El privilegio abre el área entera, consulta incluida: sin él no aparece nada de esa área. */
  it("oculta el área Partidas a quien no tiene GestionarPartidas, aunque sea Operador", () => {
    const areas = areasForRoles(["Operador"], []);

    expect(areas.map((area) => area.id)).not.toContain("partidas");
  });

  it("muestra el área Partidas a un Administrador con GestionarPartidas", () => {
    const areas = areasForRoles(["Administrador"], ["GestionarPartidas"]);

    expect(areas.map((area) => area.id)).toContain("partidas");
  });

  it("oculta el área Equipos a quien no tiene GestionarEquipos", () => {
    const areas = areasForRoles(["Administrador"], ["GestionarPartidas"]);

    expect(areas.map((area) => area.id)).not.toContain("equipos");
  });

  /* Identidad no es un privilegio: viene con el rol y está protegida, o un admin podría
     quitarse a sí mismo el acceso a la gobernanza y dejar el sistema cerrado sin llave. */
  it("muestra Identidad a un Administrador sin ningún privilegio", () => {
    const areas = areasForRoles(["Administrador"], []);

    expect(areas.map((area) => area.id)).toEqual(["identidad"]);
  });

  it("no da landing a un Operador sin privilegios: no tiene ninguna área", () => {
    expect(landingPath(["Operador"], [])).toBeNull();
  });

  it("lleva al Administrador a Identidad, que siempre tiene", () => {
    expect(landingPath(["Administrador"], [])).toBe("/identidad/usuarios");
  });

  it("lleva a Partidas a quien puede gestionarlas", () => {
    expect(landingPath(["Operador"], ["GestionarPartidas"])).toBe("/partidas");
  });
```

Importa `landingPath` si el archivo aún no lo hace.

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `cd frontend && npx vitest run src/shell/navConfig.test.tsx`
Expected: FAIL — las áreas se abren por rol y `landingPath` no acepta privilegios ni devuelve `null`.

- [ ] **Step 3: Atar las áreas a los privilegios**

En `navConfig.tsx`, añadir el campo a `NavAreaDef`:

```tsx
export interface NavAreaDef {
  id: string;
  label: string;
  role: Role | readonly Role[];
  icon: IconComponent;
  /** Privilegio que gobierna el área entera. Sin él, el área no existe para el usuario:
      ni en el menú ni por URL directa (ver `Require` en App.tsx). */
  permisos?: readonly string[];
  items: NavItemDef[];
}
```

En `NAV_AREAS`: el área `partidas` gana `permisos: ["GestionarPartidas"]`, el área `equipos` gana
`permisos: ["GestionarEquipos"]`, e `identidad` **no gana nada**. El item «Nueva partida» **pierde**
su `permisos: ["GestionarPartidas"]`: es redundante cuando el área ya lo exige.

Añade sobre el área `partidas` el comentario: `// GestionarPartidas gobierna el CRUD de partidas completo, consulta incluida.`

- [ ] **Step 4: Filtrar por rol Y privilegio**

Sustituir `areasForRoles`:

```tsx
/* El rol base delimita el ámbito del área y el privilegio funcional la habilita: sin el privilegio
   de gestión, nada de esa área aparece. Dentro del área, un item puede además exigir su propio rol. */
export function areasForRoles(roles: string[], permisos: string[] = []): NavAreaDef[] {
  return NAV_AREAS.filter((area) => {
    const allowedRoles = typeof area.role === "string" ? [area.role] : area.role;
    return (
      allowedRoles.some((role) => roles.includes(role)) &&
      (!area.permisos || area.permisos.some((permiso) => permisos.includes(permiso)))
    );
  }).map((area) => ({
    ...area,
    items: area.items.filter(
      (item) =>
        (!item.roles || item.roles.some((role) => roles.includes(role))) &&
        (!item.permisos || item.permisos.some((permiso) => permisos.includes(permiso)))
    )
  }));
}
```

- [ ] **Step 5: `landingPath` que no entra en bucle**

Sustituir `landingPath`:

```tsx
/* Primera área disponible, en orden de prioridad. Depende de los privilegios porque un Operador sin
   GestionarPartidas ya no tiene /partidas: aterrizar ahí lo rebotaría contra su propio landing en
   bucle. `null` = ninguna área; App.tsx muestra la pantalla de sin accesos. */
export function landingPath(roles: string[], permisos: string[] = []): string | null {
  const areas = areasForRoles(roles, permisos);
  if (areas.length === 0) {
    return null;
  }

  const primerItem = areas.flatMap((area) => area.items)[0];
  return primerItem?.path ?? null;
}
```

> **Por qué derivarlo de `areasForRoles` y no repetir la lista de condiciones:** el landing y el nav
> no pueden discrepar. Si el landing apuntara a un área que el nav oculta, la ruta rebotaría al
> landing y volvería a rebotar: bucle infinito. Derivarlo hace que esa clase de bug sea imposible.
>
> Efecto sobre el orden: el orden de `NAV_AREAS` (identidad → partidas → equipos) pasa a decidir el
> landing. Un Administrador con `GestionarPartidas` aterriza en `/identidad/usuarios/nuevo`, no en
> `/partidas`. Coherente: Identidad es su área propia.

- [ ] **Step 6: Ejecutar los tests**

Run: `cd frontend && npx vitest run src/shell/navConfig.test.tsx`
Expected: PASS.

⚠️ El test preexistente que afirme que un Operador ve el área Partidas **sin** privilegios ahora es
falso: el modelo cambió. Actualízalo pasándole `["GestionarPartidas"]`. Si un test preexistente falla
por otra razón, **para y repórtalo**.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/shell/navConfig.tsx frontend/src/shell/navConfig.test.tsx
git commit -m "feat(web): las areas del nav se abren por privilegio, no por rol base

El privilegio abre el area entera, consulta incluida: sin el no aparece nada de
esa area. Identidad queda fuera: viene con el rol Administrador y esta
protegida, o un admin podria quitarse el acceso a la gobernanza y cerrar el
sistema sin llave.

landingPath se deriva de areasForRoles y puede devolver null. Antes un Operador
sin GestionarPartidas aterrizaria en /partidas, la ruta lo rebotaria a su
landing, y su landing seria /partidas: bucle infinito.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Las rutas y la pantalla sin accesos

**Files:**
- Modify: `frontend/src/app/App.tsx`
- Modify: `frontend/src/shell/AppShell.tsx` (recuperable: `git show 2fabefd:frontend/src/shell/AppShell.tsx`)
- Modify: `frontend/src/shell/NavRail.tsx` (recuperable: `git show 2fabefd:frontend/src/shell/NavRail.tsx`)
- Test: `frontend/src/app/App.test.tsx`

**Interfaces:**
- Consumes: `AuthUser.permisos` (T1); `areasForRoles`, `landingPath(): string | null` (T3).

- [ ] **Step 1: Escribir los tests que fallan**

Añadir a `frontend/src/app/App.test.tsx` (lee el archivo y sigue su patrón de mock de `authProvider`):

```tsx
  /* El síntoma que originó todo: el privilegio autoriza, no el rol base. El backend ya lo aplica;
     la web debe coincidir. */
  it("deja entrar a la creación de partidas a un admin con GestionarPartidas", async () => {
    // mock: roles ["Administrador"], permisos ["GestionarPartidas"]
    // navegar a /partidas/crear → debe renderizar el panel, no redirigir
  });

  it("muestra 'Nueva partida' en el nav a un admin con GestionarPartidas", async () => {
    // mock: roles ["Administrador"], permisos ["GestionarPartidas"]
  });

  it("oculta el área Partidas a un operador sin GestionarPartidas", async () => {
    // mock: roles ["Operador"], permisos []
  });

  it("muestra la pantalla de sin accesos a un operador sin ningún privilegio", async () => {
    // mock: roles ["Operador"], permisos [] → UnauthorizedScreen
  });
```

> Los comentarios describen **qué** aserta cada test; escríbelos siguiendo el estilo real del
> archivo (cómo monta el router, cómo mockea `authProvider.init`, cómo consulta el DOM). **No
> inventes un estilo nuevo.** Los cuatro casos son obligatorios.

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `cd frontend && npx vitest run src/app/App.test.tsx`
Expected: FAIL.

- [ ] **Step 3: Generalizar el guardia de ruta**

En `App.tsx`, sustituir `RequireRole` por:

```tsx
/* Guardia de ruta: `have` son las credenciales del usuario (roles base o privilegios funcionales) y
   `need` las que la ruta exige. Basta una coincidencia. */
function Require({
  have,
  need,
  landing,
  children
}: {
  have: string[];
  need: string | readonly string[];
  landing: string;
  children: JSX.Element;
}) {
  const allowed = typeof need === "string" ? [need] : need;
  return have.some((credencial) => allowed.includes(credencial)) ? (
    children
  ) : (
    <Navigate to={landing} replace />
  );
}
```

- [ ] **Step 4: Cortar antes de montar el router**

En `App.tsx`, sustituir el chequeo de rol (líneas ~258-261):

```tsx
  const { roles, permisos } = authState.user;
  /* Sin ningún área no hay dónde aterrizar: cubre al participante que entra a la web (ningún área
     es suya) y al operador al que le retiraron los privilegios. Sin esto, el landing sería null y
     el index redirigiría a la nada. */
  if (areasForRoles(roles, permisos).length === 0) {
    return <UnauthorizedScreen username={authState.user.username} onLogout={onLogout} />;
  }
```

Importa `areasForRoles` desde `../shell/navConfig`.

- [ ] **Step 5: Gatear las rutas por privilegio**

En el `useMemo` del router:

```tsx
    const { roles, permisos, token, username } = user;
    const landing = landingPath(roles, permisos) ?? "/";
    /* El área Partidas exige el privilegio, así que dentro de ella siempre se puede operar. */
    const puedeOperar = permisos.includes("GestionarPartidas");
```

Rutas que cambian a `have={permisos} need="GestionarPartidas"`: `partidas`, `partidas/crear`,
`partidas/:partidaId`, `partidas/:partidaId/sesion`, `partidas/:partidaId/historial`.

Rutas que cambian a `have={permisos} need="GestionarEquipos"`: `equipos`, `puntuaciones/equipos`.

La ruta `identidad/equipos` mantiene su guardia de rol `Administrador` **y** añade el privilegio —
anídala:

```tsx
          {
            path: "identidad/equipos",
            element: (
              <Require have={roles} need="Administrador" landing={landing}>
                <Require have={permisos} need="GestionarEquipos" landing={landing}>
                  <TeamsAdminPage accessToken={token} />
                </Require>
              </Require>
            )
          },
```

Las rutas de `identidad/usuarios`, `identidad/usuarios/nuevo` e `identidad/gobernanza` **no cambian**:
siguen por rol `Administrador`.

`AppShell` recibe `permisos={permisos}`.

- [ ] **Step 6: Pasar `permisos` al nav**

Recupera los dos archivos del intento revertido, que ya lo hacían:

```bash
git show 2fabefd:frontend/src/shell/AppShell.tsx > frontend/src/shell/AppShell.tsx
git show 2fabefd:frontend/src/shell/NavRail.tsx > frontend/src/shell/NavRail.tsx
```

Verifica que `AppShell` sigue haciendo `roles.join(" · ")` para `roleLabel` (sólo `roles`, **nunca**
`permisos`: es lo que se le muestra al usuario en la barra superior).

- [ ] **Step 7: Ejecutar todo**

Run: `cd frontend && npm test && npx tsc --noEmit`
Expected: PASS y typecheck limpio.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/app/App.tsx frontend/src/app/App.test.tsx frontend/src/shell/AppShell.tsx frontend/src/shell/NavRail.tsx
git commit -m "feat(web): las rutas se autorizan por privilegio y sin areas no se entra

Cierra el sintoma original: un admin con GestionarPartidas ya ve y entra al
panel de creacion de partidas.

Quien no tiene ninguna area ve la pantalla de sin accesos. Una sola condicion
cubre los dos casos: el participante que entra a la web y el operador al que le
retiraron los privilegios.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Las policies que faltan en el backend

**Los privilegios son aditivos, no sustitutivos.** El panel puede darle `GestionarEquipos` a cualquier rol. Si `AdminTeamsController` exigiera **sólo** el privilegio, un Participante con él podría **borrar equipos ajenos** llamando al puerto 5001 directamente, saltándose el filtro por rol del gateway — y esos puertos están expuestos en el compose. Por eso: **rol AND privilegio**.

**Files:**
- Modify: `services/partidas/src/Umbral.Partidas.Api/Controllers/PartidasController.cs:62,69`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs` (policies compuestas)
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/AdminTeamsController.cs:13`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/TeamsAdminController.cs:8-13`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs` (policies compuestas)
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/EquiposController.cs:10`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/HistorialController.cs:12`
- Test: los tests de controller y de contrato de los tres servicios

- [ ] **Step 1: Escribir los tests que fallan**

Para **cada** policy compuesta hacen falta **tres** casos, o el AND no está probado. Añade a los
contract tests de cada servicio (sigue el estilo de cada archivo):

```
- rol correcto SIN el privilegio  → 403
- privilegio SIN el rol correcto  → 403      <-- éste es el que prueba el AND
- rol correcto CON el privilegio  → 200
```

El caso del medio es el que detecta la escalada. Un test que sólo cubra los otros dos pasaría igual
con una policy de sólo-privilegio.

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run:
```bash
dotnet test services/partidas/tests/Umbral.Partidas.ContractTests/Umbral.Partidas.ContractTests.csproj
dotnet test services/identity-service/tests/Umbral.IdentityService.ContractTests/Umbral.IdentityService.ContractTests.csproj
dotnet test services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/Umbral.Puntuaciones.ContractTests.csproj
```
Expected: FAIL en los casos nuevos.

- [ ] **Step 3: Partidas — los GET**

En `PartidasController.cs`, añadir a **ambos** GET (líneas ~62 y ~69) el atributo que los POST ya
tienen:

```csharp
    [Authorize(Policy = "GestionarPartidas")]
```

> **No hace falta AND aquí:** el gateway ya restringe `/partidas/{**catch-all}` a
> `OperadorOAdministrador`, y el servicio no expone otra vía. El AND emerge de la composición.
> Documenta esa dependencia en un comentario sobre el primer GET.

- [ ] **Step 4: Identity — policies compuestas**

En `Program.cs` de Identity, dentro de `AddAuthorization`:

```csharp
    // Los privilegios son aditivos, no sustitutivos: el panel puede dar GestionarEquipos a cualquier
    // rol. El rol delimita el ámbito y el privilegio habilita el CRUD, y se exigen los dos porque los
    // puertos de servicio están expuestos y una policy de sólo-privilegio se saltaría el filtro por
    // rol del gateway.
    options.AddPolicy("AdminGestionarEquipos", policy =>
        policy.RequireRole("Administrador").RequireRole("GestionarEquipos"));
    options.AddPolicy("OperadorOAdminGestionarEquipos", policy =>
        policy.RequireRole("Operador", "Administrador").RequireRole("GestionarEquipos"));
```

> **Sobre `RequireRole` encadenado.** Cada llamada añade un `RolesAuthorizationRequirement` a la
> policy, y una policy exige **todos** sus requirements: por tanto encadenar es **AND**, y los
> argumentos dentro de una misma llamada son **OR**. Así,
> `RequireRole("Operador", "Administrador").RequireRole("GestionarEquipos")` significa
> «(Operador o Administrador) **y** GestionarEquipos».
>
> ⚠️ **Esto es conocimiento del framework, no está verificado en este codebase** — no hay ningún
> `RequireRole` encadenado hoy. **No lo des por hecho:** el test del Step 1 «privilegio SIN el rol
> → 403» es exactamente lo que lo comprueba. Si tras implementar ese test **no** pasa (devuelve 200),
> el encadenado no se comporta como AND aquí: **para y repórtalo** en vez de improvisar. La salida
> sería una policy con `RequireAssertion` evaluando ambas condiciones a mano.

`AdminTeamsController.cs:13` → `[Authorize(Policy = "AdminGestionarEquipos")]`
`TeamsAdminController.cs:13` → `[Authorize(Policy = "OperadorOAdminGestionarEquipos")]`

Y actualiza el comentario de cabecera de `TeamsAdminController` (líneas 8-10), que dice «la policy de
clase GestionarEquipos es aditiva y esos roles no tienen ese permiso funcional»: con los defaults
nuevos el Administrador **sí** tiene `GestionarEquipos`, así que ya no es cierto.

- [ ] **Step 5: Puntuaciones — policies compuestas**

En `Program.cs` de Puntuaciones, con el mismo comentario del Step 4:

```csharp
    .AddPolicy("OperadorOAdminGestionarEquipos", p =>
        p.RequireRole("Operador", "Administrador").RequireRole("GestionarEquipos"))
    .AddPolicy("OperadorOAdminGestionarPartidas", p =>
        p.RequireRole("Operador", "Administrador").RequireRole("GestionarPartidas"))
```

`EquiposController.cs:10` → `[Authorize(Policy = "OperadorOAdminGestionarEquipos")]`
`HistorialController.cs:12` → `[Authorize(Policy = "OperadorOAdminGestionarPartidas")]`

**No toques** `RankingsController` ni `ParticipantesController` (`[Authorize]` a secas): los consume
el móvil y no son áreas del panel web.

- [ ] **Step 6: Ejecutar las tres suites**

Run:
```bash
dotnet test services/partidas/Umbral.Partidas.sln
dotnet test services/identity-service/Umbral.IdentityService.sln
dotnet test services/puntuaciones/Umbral.Puntuaciones.sln
```
Expected: PASS.

⚠️ Los `TestAuthHandler` de estos servicios simulan la expansión composite de Keycloak. Si un test
falla porque su principal no tiene el privilegio que ahora se exige, **revisa si el fixture refleja
los defaults reales** (Administrador → `GestionarEquipos`; Operador → `GestionarPartidas`). Ajusta el
**fixture** si está desactualizado; **no relajes la policy**. Si dudas, para y repórtalo.

- [ ] **Step 7: Commit**

```bash
git add services/partidas/src services/partidas/tests services/identity-service/src services/identity-service/tests services/puntuaciones/src services/puntuaciones/tests
git commit -m "feat(backend): cierra las policies del panel web con rol AND privilegio

Los GET de Partidas no tenian policy: la consulta quedaba abierta a cualquier
Operador/Administrador aunque el panel le hubiera retirado GestionarPartidas.

Los paneles web de equipos pasan a exigir rol AND privilegio. Solo el privilegio
abriria escalada: el panel puede dar GestionarEquipos a cualquier rol, y los
puertos de servicio estan expuestos, asi que un Participante con ese privilegio
podria borrar equipos ajenos llamando al 5001 y saltandose el gateway.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Verificación en vivo — el síntoma original

**Files:** ninguno (verificación).

⚠️ **Requiere autorización del usuario antes de ejecutarse.** Arranca Docker y usa su Keycloak real.

- [ ] **Step 1: Preparar el escenario**

La DB del usuario tiene hoy `(1,1)` — `Administrador → GestionarPartidas` — insertada a mano al
verificar el guardia de la migración del sub-proyecto 1. **Hay que quitarla**, o el área Partidas ya
le aparecería al admin y la prueba no probaría nada:

```bash
docker exec umbral-postgres psql -U umbral -d umbral_identity -c "DELETE FROM permisos_rol WHERE rol = 1 AND permiso = 1;"
docker compose -f infra/docker-compose.yml --env-file .env restart identity-service
```

- [ ] **Step 2: Reconstruir y levantar**

```bash
docker compose -f infra/docker-compose.yml --env-file .env up -d --build
```

- [ ] **Step 3: Estado de partida — el admin NO ve Partidas**

Entrar a la web como administrador. **Esperado:** ve **Identidad** y **Equipos** (su default es
`GestionarEquipos`), y **no** ve el área Partidas.

- [ ] **Step 4: El síntoma original**

1. En el panel de gobernanza, marcar «Gestionar partidas» para el rol Administrador y guardar.
2. Cerrar sesión y volver a entrar.
3. **Esperado: aparece el área Partidas, con «Partidas» y «Nueva partida».** Entrar en «Nueva
   partida» y comprobar que el panel de creación carga.

**Esto es lo que originó todo el trabajo.** Si no aparece, para y reporta.

- [ ] **Step 5: La simétrica**

1. Desmarcar «Gestionar equipos» para el rol Administrador y guardar.
2. Cerrar sesión y volver a entrar.
3. **Esperado: el área Equipos desaparece.**

- [ ] **Step 6: Sin recargar — el fix del refresh (D4)**

Con la sesión abierta, cambiar los privilegios del rol desde otro navegador (o por API) y esperar al
siguiente tick de refresh (~270s). **Esperado:** el nav se recalcula solo, sin cerrar sesión.

> Si esperar 270s no es práctico, basta con dejar constancia de que el test automatizado de la
> Task 2 cubre la propagación. Anótalo en el reporte.

- [ ] **Step 7: El móvil sigue vivo**

Un participante entra al móvil, crea un equipo y juega. Descarta que las policies de la Task 5 hayan
roto algo del lado del participante.

- [ ] **Step 8: Ledger**

```bash
echo "2026-07-15 | HU-04 | sub-2 task 6 | verificacion en vivo: el sintoma original resuelto" >> .git/sdd/progress.md
```

---

## Notas para quien implemente

- **La Task 1 deja `tsc` en rojo a propósito.** Cambia el contrato de `refresh()`; la Task 2 actualiza a los consumidores. No arregles `App.tsx` en la Task 1.
- **Si un mock de test no trae `permisos`**, añádeselo. No metas un `?? []` defensivo en producción: el contrato real siempre lo trae, y ese `??` escondería un bug de verdad.
- **`RequireRole` encadenado es AND; los argumentos dentro de una llamada son OR.** De ahí sale «(Operador o Administrador) y GestionarEquipos».
- **No toques el gateway.** `/partidas` sigue en `OperadorOAdministrador`: el participante no tiene UI de gestión hasta el sub-proyecto 3.
- **El prop `puedeOperar`** queda siempre `true` dentro del área Partidas. Es deuda conocida y deliberada: eliminarlo arrastra tres páginas y sus tests a un cambio que no lo necesita.
