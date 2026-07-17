# Bloque 3c — RNF-24 refresh de sesión Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refresh del token de Keycloak cada 270s con control de inactividad y modal de continuación, en web y mobile, cliente↔Keycloak directo (RNF-24).

**Architecture:** Un núcleo puro por cliente (máquina de decisión sin timers ni red: `tick()`/`marcarActividad()`/`continuar()`) manejado por un interval de 270s; el refresh va directo a Keycloak (web: `keycloak-js updateToken(-1)`; mobile: `grant_type=refresh_token` al token endpoint). El token nuevo se propaga por el state/context existente; los hubs SignalR pasan a recibir el token por getter/ref para no reconectar en cada refresh.

**Tech Stack:** keycloak-js (web), expo-auth-session/SecureStore (mobile), React 18 + vitest (web), RN + node:test (mobile).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-10-bloque3c-rnf24-refresh-sesion-design.md`.
- Intervalo de refresh: **270_000 ms** exactos (constante `REFRESH_INTERVAL_MS = 270_000`). Refresh SIEMPRE directo a Keycloak — nunca vía gateway/backend.
- Semántica del ciclo (idéntica web/mobile): actividad desde el tick anterior → refresh silencioso; sin actividad → modal "¿Sigues ahí?" / "Tu sesión está por expirar." con botones "Continuar sesión" y "Salir"; modal abierto → ticks ignorados; "Continuar" → refresh; cualquier refresh fallido → logout con aviso "Tu sesión expiró. Inicia sesión de nuevo."; sin countdown.
- Hubs: las conexiones vivas NO se reconectan al refrescar — token por getter `() => string` leído de un ref; `accessToken`/`token` sale de las deps de los efectos de hub.
- Sin cambios de realm, backend, gateway ni contratos.
- Web: no cambiar `label`/`id`/`data-testid`/ARIA existentes; reutilizar clases del design system (`modal-backdrop`, `modal-card`, `stack`, `compact-actions`, `notice`, `muted`).
- Mobile: `keycloakMobileAuth.ts` importa módulos expo → NO testeable bajo `node --test`; la lógica testeable vive en módulos puros con deps inyectadas (patrón flows del repo).
- Commits terminan con `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a subagentes: `git stash/reset/checkout/restore/clean`. Solo `git add <rutas exactas>` + `git commit`.
- Gates: web `npm test` + `npx tsc -b` + `npm run build` (borrar artefactos de tsc, no commitearlos); mobile `npm test` + `npm run typecheck`.

---

### Task 1: Web — núcleo puro `sessionRefreshCore`

**Files:**
- Create: `frontend/src/auth/sessionRefreshCore.ts`
- Test: `frontend/src/auth/sessionRefreshCore.test.ts`

**Interfaces:**
- Produces: `crearSessionRefreshCore(cb: SessionRefreshCallbacks): SessionRefreshCore` con `SessionRefreshCallbacks = { refrescar: () => Promise<boolean>; onModal: (visible: boolean) => void; onExpirada: () => void }` y `SessionRefreshCore = { marcarActividad(): void; tick(): Promise<void>; continuar(): Promise<void> }`. Task 2 lo consume. Sin timers dentro: el caller posee el interval.

- [ ] **Step 1: Test (falla)**

Crear `frontend/src/auth/sessionRefreshCore.test.ts`:

```ts
import { describe, expect, it, vi } from "vitest";
import { crearSessionRefreshCore } from "./sessionRefreshCore";

function armar(refrescarOk = true) {
  const refrescar = vi.fn().mockResolvedValue(refrescarOk);
  const onModal = vi.fn();
  const onExpirada = vi.fn();
  const core = crearSessionRefreshCore({ refrescar, onModal, onExpirada });
  return { core, refrescar, onModal, onExpirada };
}

describe("sessionRefreshCore", () => {
  it("tick con actividad refresca en silencio y consume la actividad", async () => {
    const { core, refrescar, onModal } = armar();
    core.marcarActividad();
    await core.tick();
    expect(refrescar).toHaveBeenCalledTimes(1);
    expect(onModal).not.toHaveBeenCalled();
    // La actividad se consumió: el siguiente tick sin actividad nueva abre el modal.
    await core.tick();
    expect(onModal).toHaveBeenCalledWith(true);
    expect(refrescar).toHaveBeenCalledTimes(1);
  });

  it("tick sin actividad abre el modal y NO refresca", async () => {
    const { core, refrescar, onModal } = armar();
    await core.tick();
    expect(onModal).toHaveBeenCalledWith(true);
    expect(refrescar).not.toHaveBeenCalled();
  });

  it("con modal abierto los ticks se ignoran", async () => {
    const { core, refrescar, onModal } = armar();
    await core.tick(); // abre modal
    onModal.mockClear();
    core.marcarActividad(); // actividad posterior no cierra el modal sola
    await core.tick();
    await core.tick();
    expect(refrescar).not.toHaveBeenCalled();
    expect(onModal).not.toHaveBeenCalled();
  });

  it("continuar() refresca y cierra el modal si el refresh funciona", async () => {
    const { core, refrescar, onModal } = armar();
    await core.tick(); // abre modal
    await core.continuar();
    expect(refrescar).toHaveBeenCalledTimes(1);
    expect(onModal).toHaveBeenLastCalledWith(false);
  });

  it("continuar() sin modal abierto es no-op", async () => {
    const { core, refrescar } = armar();
    await core.continuar();
    expect(refrescar).not.toHaveBeenCalled();
  });

  it("refresh fallido en tick silencioso dispara onExpirada", async () => {
    const { core, refrescar, onExpirada } = armar(false);
    core.marcarActividad();
    await core.tick();
    expect(refrescar).toHaveBeenCalledTimes(1);
    expect(onExpirada).toHaveBeenCalledTimes(1);
  });

  it("refresh fallido desde continuar() dispara onExpirada y el modal queda abierto", async () => {
    const { core, onModal, onExpirada } = armar(false);
    await core.tick();
    onModal.mockClear();
    await core.continuar();
    expect(onExpirada).toHaveBeenCalledTimes(1);
    expect(onModal).not.toHaveBeenCalledWith(false);
  });
});
```

- [ ] **Step 2: Correr — falla**

Run: `cd frontend && npx vitest run src/auth/sessionRefreshCore.test.ts`
Expected: FAIL (módulo no existe).

- [ ] **Step 3: Implementación**

Crear `frontend/src/auth/sessionRefreshCore.ts`:

```ts
// Núcleo puro del ciclo RNF-24: decide refresh silencioso vs modal de continuación.
// Sin timers ni red: el caller posee el interval (270s) y llama tick(); el refresh
// real se inyecta como callback que resuelve true (token renovado) o false (fallo).
export interface SessionRefreshCallbacks {
  refrescar: () => Promise<boolean>;
  onModal: (visible: boolean) => void;
  onExpirada: () => void;
}

export interface SessionRefreshCore {
  marcarActividad(): void;
  tick(): Promise<void>;
  continuar(): Promise<void>;
}

export function crearSessionRefreshCore(cb: SessionRefreshCallbacks): SessionRefreshCore {
  let activo = false;
  let modalPendiente = false;
  let refrescando = false;

  async function ejecutarRefresh(): Promise<void> {
    if (refrescando) return;
    refrescando = true;
    try {
      const ok = await cb.refrescar();
      if (ok) {
        if (modalPendiente) {
          modalPendiente = false;
          cb.onModal(false);
        }
      } else {
        cb.onExpirada();
      }
    } finally {
      refrescando = false;
    }
  }

  return {
    marcarActividad() {
      activo = true;
    },
    async tick() {
      if (modalPendiente || refrescando) return;
      if (activo) {
        // Se consume ANTES del await: actividad ocurrida durante el refresh cuenta
        // para la ventana siguiente en vez de perderse.
        activo = false;
        await ejecutarRefresh();
      } else {
        modalPendiente = true;
        cb.onModal(true);
      }
    },
    async continuar() {
      if (!modalPendiente) return;
      await ejecutarRefresh();
    },
  };
}
```

- [ ] **Step 4: Correr — PASS**

Run: `npx vitest run src/auth/sessionRefreshCore.test.ts` → 7 PASS.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/auth/sessionRefreshCore.ts frontend/src/auth/sessionRefreshCore.test.ts
git commit -m "feat(web): nucleo puro del ciclo de refresh de sesion RNF-24

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Web — `refresh()` en authProvider + hook + modal + wiring en App

**Files:**
- Modify: `frontend/src/auth/keycloak.ts`
- Create: `frontend/src/auth/useSessionRefresh.ts`
- Create: `frontend/src/auth/SessionExpiryModal.tsx`
- Test: `frontend/src/auth/SessionExpiryModal.test.tsx` (create)
- Modify: `frontend/src/shell/states.tsx` (LoginScreen gana prop opcional `notice`)
- Modify: `frontend/src/app/App.tsx`
- Modify: `frontend/src/app/App.test.tsx` (solo si el mock de `../auth/keycloak` necesita el método nuevo)

**Interfaces:**
- Consumes: `crearSessionRefreshCore` (Task 1, firma exacta de su bloque Produces).
- Produces: `authProvider.refresh(): Promise<string>` (token nuevo o rechaza); `useSessionRefresh({ enabled, onToken, onExpired }) => { modalVisible, continuar }`; `SessionExpiryModal({ visible, onContinuar, onSalir })`; `LoginScreen({ onLogin, notice? })`. Constante exportada `REFRESH_INTERVAL_MS = 270_000` en `useSessionRefresh.ts`.

- [ ] **Step 1: `refresh()` en `keycloak.ts`**

En la interfaz `AuthProvider` añadir:

```ts
  /** Fuerza el refresh del token contra Keycloak (RNF-24). Resuelve al token nuevo. */
  refresh(): Promise<string>;
```

En `KeycloakAuthProvider` añadir:

```ts
  async refresh(): Promise<string> {
    // -1 fuerza el refresh aunque el token siga válido: RNF-24 pide refresh
    // incondicional en cada tick de 270s. keycloak-js usa el refresh token
    // internamente, directo contra Keycloak (sin gateway/backend).
    await this.keycloak.updateToken(-1);
    if (!this.keycloak.token) {
      throw new Error("Keycloak no devolvió token tras el refresh.");
    }
    return this.keycloak.token;
  }
```

Si `frontend/src/app/App.test.tsx` (u otro test) mockea el módulo `../auth/keycloak` con un objeto `authProvider`, añadir `refresh: vi.fn().mockResolvedValue("tok")` a ese mock para que el tipo cierre.

- [ ] **Step 2: Test del modal (falla)**

Crear `frontend/src/auth/SessionExpiryModal.test.tsx`:

```tsx
import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SessionExpiryModal } from "./SessionExpiryModal";

describe("SessionExpiryModal", () => {
  it("no renderiza nada cuando visible=false", () => {
    render(<SessionExpiryModal visible={false} onContinuar={() => {}} onSalir={() => {}} />);
    expect(screen.queryByTestId("session-expiry-modal")).not.toBeInTheDocument();
  });

  it("visible: muestra textos y los botones llaman sus callbacks", async () => {
    const onContinuar = vi.fn();
    const onSalir = vi.fn();
    render(<SessionExpiryModal visible onContinuar={onContinuar} onSalir={onSalir} />);
    expect(screen.getByText("¿Sigues ahí?")).toBeInTheDocument();
    expect(screen.getByText("Tu sesión está por expirar.")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Continuar sesión" }));
    expect(onContinuar).toHaveBeenCalledTimes(1);
    await userEvent.click(screen.getByRole("button", { name: "Salir" }));
    expect(onSalir).toHaveBeenCalledTimes(1);
  });
});
```

Run: `npx vitest run src/auth/SessionExpiryModal.test.tsx` → FAIL (módulo no existe).

- [ ] **Step 3: Modal**

Crear `frontend/src/auth/SessionExpiryModal.tsx`. ANTES de escribir el markup, mirar el modal existente de `frontend/src/features/identity/UserManagementPage.tsx` (usa `modal-backdrop`/`modal-card`) y copiar su estructura/clases de botones exactas:

```tsx
// Modal de continuación de sesión (RNF-24): aparece cuando el tick de 270s
// encuentra al usuario inactivo. Sin countdown: Keycloak decide la expiración real.
export function SessionExpiryModal({
  visible,
  onContinuar,
  onSalir
}: {
  visible: boolean;
  onContinuar: () => void;
  onSalir: () => void;
}) {
  if (!visible) {
    return null;
  }
  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true" aria-label="Sesión por expirar">
      <div className="modal-card stack" data-testid="session-expiry-modal">
        <h2>¿Sigues ahí?</h2>
        <p className="muted">Tu sesión está por expirar.</p>
        <div className="compact-actions">
          <button type="button" onClick={onContinuar}>
            Continuar sesión
          </button>
          <button type="button" className="ghost" onClick={onSalir}>
            Salir
          </button>
        </div>
      </div>
    </div>
  );
}
```

(La clase del botón secundario — `ghost`/`secondary`/lo que use el design system — se toma del patrón real de UserManagementPage; el test no la asserta.)

Run: `npx vitest run src/auth/SessionExpiryModal.test.tsx` → 2 PASS.

- [ ] **Step 4: Hook `useSessionRefresh`**

Crear `frontend/src/auth/useSessionRefresh.ts`:

```ts
// Ciclo RNF-24 en la web: interval de 270s + listeners de actividad + núcleo puro.
import { useEffect, useRef, useState } from "react";
import { authProvider } from "./keycloak";
import { crearSessionRefreshCore, type SessionRefreshCore } from "./sessionRefreshCore";

export const REFRESH_INTERVAL_MS = 270_000;

export function useSessionRefresh(opts: {
  enabled: boolean;
  onToken: (token: string) => void;
  onExpired: () => void;
}): { modalVisible: boolean; continuar: () => void } {
  const [modalVisible, setModalVisible] = useState(false);
  const onTokenRef = useRef(opts.onToken);
  onTokenRef.current = opts.onToken;
  const onExpiredRef = useRef(opts.onExpired);
  onExpiredRef.current = opts.onExpired;
  const coreRef = useRef<SessionRefreshCore | null>(null);

  useEffect(() => {
    if (!opts.enabled) return;

    const core = crearSessionRefreshCore({
      refrescar: () =>
        authProvider.refresh().then(
          (token) => {
            onTokenRef.current(token);
            return true;
          },
          () => false
        ),
      onModal: setModalVisible,
      onExpirada: () => onExpiredRef.current()
    });
    coreRef.current = core;

    const marcar = () => core.marcarActividad();
    // RNF-24: clicks/toques, teclado, scroll y navegación cuentan como actividad.
    window.addEventListener("pointerdown", marcar, { capture: true, passive: true });
    window.addEventListener("keydown", marcar, { capture: true });
    window.addEventListener("scroll", marcar, { capture: true, passive: true });
    window.addEventListener("popstate", marcar);
    const interval = window.setInterval(() => void core.tick(), REFRESH_INTERVAL_MS);

    return () => {
      window.removeEventListener("pointerdown", marcar, { capture: true });
      window.removeEventListener("keydown", marcar, { capture: true });
      window.removeEventListener("scroll", marcar, { capture: true });
      window.removeEventListener("popstate", marcar);
      window.clearInterval(interval);
      coreRef.current = null;
      setModalVisible(false);
    };
  }, [opts.enabled]);

  return { modalVisible, continuar: () => void coreRef.current?.continuar() };
}
```

No lleva test propio: el núcleo (decisiones) ya está testeado en Task 1 y el hook es cableado fino de listeners/interval; el gate de suite completa + tsc cubre la integración de tipos.

- [ ] **Step 5: `LoginScreen` con aviso opcional**

En `frontend/src/shell/states.tsx`, cambiar la firma de `LoginScreen` a:

```tsx
export function LoginScreen({ onLogin, notice }: { onLogin: () => void; notice?: string | null }) {
```

y dentro de la card, inmediatamente antes del `<button ...>Iniciar sesión</button>`, añadir:

```tsx
        {notice ? (
          <div className="notice" role="status">
            {notice}
          </div>
        ) : null}
```

No tocar nada más del componente (prop opcional → los usos existentes compilan igual).

- [ ] **Step 6: Wiring en `App.tsx`**

En `frontend/src/app/App.tsx`:

1. Imports nuevos: `import { useSessionRefresh } from "../auth/useSessionRefresh";` y `import { SessionExpiryModal } from "../auth/SessionExpiryModal";`.
2. Dentro de `App()`, después de los `useState` existentes:

```tsx
  const sesionExpiradaKey = "umbral.sesion.expirada";
  const { modalVisible, continuar } = useSessionRefresh({
    enabled: authState.status === "ready",
    onToken: (token) =>
      setAuthState((prev) =>
        prev.status === "ready" ? { status: "ready", user: { ...prev.user, token } } : prev
      ),
    onExpired: () => {
      // El logout de Keycloak recarga la página: el aviso sobrevive en sessionStorage.
      window.sessionStorage.setItem(sesionExpiradaKey, "1");
      void authProvider.logout();
    }
  });
```

3. En la rama `unauthenticated` (hoy `return <LoginScreen onLogin={...} />;`), leer y limpiar el flag:

```tsx
  if (authState.status === "unauthenticated") {
    const expirada = window.sessionStorage.getItem(sesionExpiradaKey) === "1";
    if (expirada) {
      window.sessionStorage.removeItem(sesionExpiradaKey);
    }
    return (
      <LoginScreen
        onLogin={() => void authProvider.login()}
        notice={expirada ? "Tu sesión expiró. Inicia sesión de nuevo." : null}
      />
    );
  }
```

(Respetar la forma real de la rama existente; solo se añade la prop `notice` y el flag.)

4. En el `return` autenticado (donde se renderiza el `RouterProvider`), envolver o acompañar con el modal:

```tsx
  return (
    <>
      <RouterProvider router={router} />
      <SessionExpiryModal
        visible={modalVisible}
        onContinuar={continuar}
        onSalir={() => void onLogout()}
      />
    </>
  );
```

(Usar el nombre real de la variable del router y del handler de logout que ya existen en el archivo.)

- [ ] **Step 7: Gates**

Run (en `frontend/`): `npm test` && `npx tsc -b` && `npm run build`
Expected: suite completa verde (los tests de App existentes no deben romperse — el hook con `enabled` corre pero el interval de 270s jamás dispara dentro de un test), tsc limpio, build OK. Borrar artefactos de tsc si aparecen.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/auth/keycloak.ts frontend/src/auth/useSessionRefresh.ts frontend/src/auth/SessionExpiryModal.tsx frontend/src/auth/SessionExpiryModal.test.tsx frontend/src/shell/states.tsx frontend/src/app/App.tsx
git commit -m "feat(web): refresh de sesion 270s con inactividad y modal (RNF-24)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

(Añadir `frontend/src/app/App.test.tsx` al add solo si se tocó su mock.)

---

### Task 3: Web — hubs con token por getter (sin reconexión en refresh)

**Files:**
- Modify: `frontend/src/api/sesionHub.ts`
- Modify: `frontend/src/api/rankingHub.ts`
- Modify: `frontend/src/features/partidas/useSesionHub.ts`
- Modify: `frontend/src/features/partidas/useRankingHub.ts`
- Test: `frontend/src/api/sesionHub.test.ts` (ajustar si construye la conexión con string)

**Interfaces:**
- Produces: `crearSesionHub(getToken: () => string)` y `crearRankingHub(getToken: () => string)` — MISMOS nombres, el parámetro pasa de `accessToken: string` a getter. Las firmas públicas de `useSesionHub(partidaId, accessToken, handlers)` y `useRankingHub(partidaId, accessToken, handlers)` NO cambian (los callers quedan intactos).

- [ ] **Step 1: Factories con getter**

En `frontend/src/api/sesionHub.ts` reemplazar:

```ts
export function crearSesionHub(accessToken: string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(sesionHubUrl(), { accessTokenFactory: () => accessToken })
```

por:

```ts
// getToken en vez de string: el token se lee en cada handshake, así un refresh
// (RNF-24) no obliga a reconectar la conexión viva.
export function crearSesionHub(getToken: () => string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(sesionHubUrl(), { accessTokenFactory: getToken })
```

Mismo cambio exacto en `frontend/src/api/rankingHub.ts` para `crearRankingHub`.

- [ ] **Step 2: Hooks con tokenRef**

En `frontend/src/features/partidas/useSesionHub.ts`, dentro de `useSesionHub`, después del `handlersRef` existente añadir:

```ts
  // El token va por ref: un refresh de sesión no debe derribar la conexión viva
  // (solo se usa en el handshake de conexión/reconexión).
  const tokenRef = useRef(accessToken);
  tokenRef.current = accessToken;
```

Cambiar `const connection = crearSesionHub(accessToken);` por `const connection = crearSesionHub(() => tokenRef.current);` y las deps del efecto de `[partidaId, accessToken]` a `[partidaId]`.

Mismo patrón exacto en `useRankingHub.ts` (mismo ref, `crearRankingHub(() => tokenRef.current)`, deps `[partidaId]`).

- [ ] **Step 3: Ajustar tests que construyen con string**

Buscar usos: `grep -rn "crearSesionHub\|crearRankingHub" frontend/src --include="*.test.*"`. Cualquier test que llame `crearSesionHub("tok")` pasa a `crearSesionHub(() => "tok")`. Los tests de los hooks (`useRankingHub.test.ts`) no cambian su API pública — solo ajustar si mockean la factory con aserciones sobre el argumento (asertar que recibe una función, no un string).

- [ ] **Step 4: Gates**

Run (en `frontend/`): `npm test` && `npx tsc -b` && `npm run build` → verde/limpio/OK.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/sesionHub.ts frontend/src/api/rankingHub.ts frontend/src/features/partidas/useSesionHub.ts frontend/src/features/partidas/useRankingHub.ts
git commit -m "refactor(web): hubs SignalR leen el token por getter para no reconectar en refresh

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

(Sumar al add los archivos de test tocados en Step 3.)

---

### Task 4: Mobile — flow de refresh + núcleo puro

**Files:**
- Create: `mobile/src/auth/sessionRefreshFlow.js`
- Create: `mobile/src/auth/sessionRefreshCore.js`
- Modify: `mobile/src/auth/keycloakMobileAuth.ts`
- Test: `mobile/tests/sessionRefreshFlow.test.js` (create)
- Test: `mobile/tests/sessionRefreshCore.test.js` (create)

**Interfaces:**
- Consumes: `buildAuthUser(accessToken)` de `mobile/src/auth/tokenClaims.js` (ya existe; lanza si el JWT es inválido); constantes `storageKey`/`refreshKey` y `discovery` internos de `keycloakMobileAuth.ts`.
- Produces:
  - `refrescarTokenFlow({ tokenEndpoint, clientId, refreshToken, fetchImpl, buildUser })` → `Promise<{ ok: true; token: string; user: unknown; refreshToken: string | null } | { ok: false }>` (módulo puro, testeable).
  - `crearSessionRefreshCore({ refrescar, onModal, onExpirada })` → `{ marcarActividad, tick, continuar }` — semántica IDÉNTICA al core web de Task 1.
  - `refreshSessionAsync(): Promise<AuthSessionState | null>` en `keycloakMobileAuth.ts` (persiste en SecureStore; `null` en cualquier fallo, no lanza). Task 5 la consume.

- [ ] **Step 1: Test del flow (falla)**

Crear `mobile/tests/sessionRefreshFlow.test.js`:

```js
import test from "node:test";
import assert from "node:assert/strict";
import { refrescarTokenFlow } from "../src/auth/sessionRefreshFlow.js";

const base = {
  tokenEndpoint: "http://kc:8080/realms/UMBRAL-UCAB/protocol/openid-connect/token",
  clientId: "umbral-mobile",
  refreshToken: "R1",
  buildUser: (token) => ({ sub: "u1", desde: token }),
};

test("refresh exitoso devuelve token nuevo, user y refresh rotado", async () => {
  let captured;
  const fetchImpl = async (url, init) => {
    captured = { url, init };
    return {
      ok: true,
      json: async () => ({ access_token: "A2", refresh_token: "R2" }),
    };
  };
  const r = await refrescarTokenFlow({ ...base, fetchImpl });
  assert.deepEqual(r, { ok: true, token: "A2", user: { sub: "u1", desde: "A2" }, refreshToken: "R2" });
  assert.equal(captured.url, base.tokenEndpoint);
  assert.equal(captured.init.method, "POST");
  assert.match(captured.init.body, /grant_type=refresh_token/);
  assert.match(captured.init.body, /client_id=umbral-mobile/);
  assert.match(captured.init.body, /refresh_token=R1/);
});

test("sin refresh token devuelve ok:false sin llamar la red", async () => {
  const fetchImpl = async () => {
    throw new Error("no debe llamarse");
  };
  const r = await refrescarTokenFlow({ ...base, refreshToken: null, fetchImpl });
  assert.deepEqual(r, { ok: false });
});

test("HTTP no-ok devuelve ok:false", async () => {
  const fetchImpl = async () => ({ ok: false, json: async () => ({}) });
  const r = await refrescarTokenFlow({ ...base, fetchImpl });
  assert.deepEqual(r, { ok: false });
});

test("respuesta sin access_token devuelve ok:false", async () => {
  const fetchImpl = async () => ({ ok: true, json: async () => ({}) });
  const r = await refrescarTokenFlow({ ...base, fetchImpl });
  assert.deepEqual(r, { ok: false });
});

test("buildUser que lanza (JWT invalido) devuelve ok:false", async () => {
  const fetchImpl = async () => ({ ok: true, json: async () => ({ access_token: "basura" }) });
  const buildUser = () => {
    throw new Error("jwt invalido");
  };
  const r = await refrescarTokenFlow({ ...base, fetchImpl, buildUser });
  assert.deepEqual(r, { ok: false });
});

test("respuesta sin refresh_token nuevo conserva refreshToken null", async () => {
  const fetchImpl = async () => ({ ok: true, json: async () => ({ access_token: "A2" }) });
  const r = await refrescarTokenFlow({ ...base, fetchImpl });
  assert.deepEqual(r, { ok: true, token: "A2", user: { sub: "u1", desde: "A2" }, refreshToken: null });
});
```

Run: `cd mobile && npm test` → FAIL (módulo no existe).

- [ ] **Step 2: Flow**

Crear `mobile/src/auth/sessionRefreshFlow.js`:

```js
// Refresh RNF-24 contra el token endpoint de Keycloak (cliente↔Keycloak directo,
// sin gateway/backend). Módulo puro con deps inyectadas: testeable bajo node:test.
export async function refrescarTokenFlow({ tokenEndpoint, clientId, refreshToken, fetchImpl, buildUser }) {
  if (!refreshToken) {
    return { ok: false };
  }
  try {
    const body =
      `grant_type=refresh_token` +
      `&client_id=${encodeURIComponent(clientId)}` +
      `&refresh_token=${encodeURIComponent(refreshToken)}`;
    const response = await fetchImpl(tokenEndpoint, {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body,
    });
    if (!response.ok) {
      return { ok: false };
    }
    const data = await response.json();
    if (!data?.access_token) {
      return { ok: false };
    }
    const user = buildUser(data.access_token);
    return { ok: true, token: data.access_token, user, refreshToken: data.refresh_token ?? null };
  } catch {
    return { ok: false };
  }
}
```

Run: `npm test` → tests del flow PASS.

- [ ] **Step 3: Test del core mobile (falla)**

Crear `mobile/tests/sessionRefreshCore.test.js` — mismos 7 casos que el core web, adaptados a node:test:

```js
import test from "node:test";
import assert from "node:assert/strict";
import { crearSessionRefreshCore } from "../src/auth/sessionRefreshCore.js";

function armar(refrescarOk = true) {
  const llamadas = { refrescar: 0, modal: [], expirada: 0 };
  const core = crearSessionRefreshCore({
    refrescar: async () => {
      llamadas.refrescar += 1;
      return refrescarOk;
    },
    onModal: (v) => llamadas.modal.push(v),
    onExpirada: () => {
      llamadas.expirada += 1;
    },
  });
  return { core, llamadas };
}

test("tick con actividad refresca en silencio y consume la actividad", async () => {
  const { core, llamadas } = armar();
  core.marcarActividad();
  await core.tick();
  assert.equal(llamadas.refrescar, 1);
  assert.deepEqual(llamadas.modal, []);
  await core.tick();
  assert.deepEqual(llamadas.modal, [true]);
  assert.equal(llamadas.refrescar, 1);
});

test("tick sin actividad abre modal sin refrescar", async () => {
  const { core, llamadas } = armar();
  await core.tick();
  assert.deepEqual(llamadas.modal, [true]);
  assert.equal(llamadas.refrescar, 0);
});

test("con modal abierto los ticks se ignoran", async () => {
  const { core, llamadas } = armar();
  await core.tick();
  core.marcarActividad();
  await core.tick();
  await core.tick();
  assert.equal(llamadas.refrescar, 0);
  assert.deepEqual(llamadas.modal, [true]);
});

test("continuar refresca y cierra el modal en exito", async () => {
  const { core, llamadas } = armar();
  await core.tick();
  await core.continuar();
  assert.equal(llamadas.refrescar, 1);
  assert.deepEqual(llamadas.modal, [true, false]);
});

test("continuar sin modal es no-op", async () => {
  const { core, llamadas } = armar();
  await core.continuar();
  assert.equal(llamadas.refrescar, 0);
});

test("refresh fallido en tick dispara onExpirada", async () => {
  const { core, llamadas } = armar(false);
  core.marcarActividad();
  await core.tick();
  assert.equal(llamadas.expirada, 1);
});

test("refresh fallido desde continuar deja el modal abierto y expira", async () => {
  const { core, llamadas } = armar(false);
  await core.tick();
  await core.continuar();
  assert.equal(llamadas.expirada, 1);
  assert.deepEqual(llamadas.modal, [true]);
});
```

Run: `npm test` → FAIL.

- [ ] **Step 4: Core mobile**

Crear `mobile/src/auth/sessionRefreshCore.js` — traducción literal del core web (Task 1 Step 3) a JS sin tipos:

```js
// Núcleo puro del ciclo RNF-24 (espejo del core web): decide refresh silencioso
// vs modal. Sin timers ni red; el caller posee el interval de 270s.
export function crearSessionRefreshCore({ refrescar, onModal, onExpirada }) {
  let activo = false;
  let modalPendiente = false;
  let refrescando = false;

  async function ejecutarRefresh() {
    if (refrescando) return;
    refrescando = true;
    try {
      const ok = await refrescar();
      if (ok) {
        if (modalPendiente) {
          modalPendiente = false;
          onModal(false);
        }
      } else {
        onExpirada();
      }
    } finally {
      refrescando = false;
    }
  }

  return {
    marcarActividad() {
      activo = true;
    },
    async tick() {
      if (modalPendiente || refrescando) return;
      if (activo) {
        activo = false;
        await ejecutarRefresh();
      } else {
        modalPendiente = true;
        onModal(true);
      }
    },
    async continuar() {
      if (!modalPendiente) return;
      await ejecutarRefresh();
    },
  };
}
```

Run: `npm test` → PASS.

- [ ] **Step 5: `refreshSessionAsync` en `keycloakMobileAuth.ts`**

Añadir import: `import { refrescarTokenFlow } from "./sessionRefreshFlow.js";` y la función:

```ts
/**
 * Refresca la sesión con el refresh token guardado (RNF-24), directo contra
 * Keycloak. Devuelve la sesión nueva persistida, o null en cualquier fallo
 * (sin refresh token, HTTP != 200, red, token inválido) — el caller decide logout.
 */
export async function refreshSessionAsync(): Promise<AuthSessionState | null> {
  try {
    const refreshToken = await SecureStore.getItemAsync(refreshKey);
    const r = (await refrescarTokenFlow({
      tokenEndpoint: discovery.tokenEndpoint,
      clientId: mobileEnv.keycloakClientId,
      refreshToken,
      fetchImpl: fetch,
      buildUser: buildAuthUser,
    })) as
      | { ok: true; token: string; user: AuthSessionState["user"]; refreshToken: string | null }
      | { ok: false };
    if (!r.ok) {
      return null;
    }
    const sessionState: AuthSessionState = { token: r.token, user: r.user };
    await SecureStore.setItemAsync(storageKey, JSON.stringify(sessionState));
    if (r.refreshToken) {
      await SecureStore.setItemAsync(refreshKey, r.refreshToken);
    }
    return sessionState;
  } catch {
    return null;
  }
}
```

- [ ] **Step 6: Gates**

Run (en `mobile/`): `npm test` && `npm run typecheck` → verde/limpio.

- [ ] **Step 7: Commit**

```bash
git add mobile/src/auth/sessionRefreshFlow.js mobile/src/auth/sessionRefreshCore.js mobile/src/auth/keycloakMobileAuth.ts mobile/tests/sessionRefreshFlow.test.js mobile/tests/sessionRefreshCore.test.js
git commit -m "feat(mobile): flow de refresh de token y nucleo del ciclo RNF-24

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: Mobile — scheduler + actividad + modal en `AuthProvider`

**Files:**
- Modify: `mobile/src/auth/AuthProvider.tsx`
- Create: `mobile/src/auth/SessionExpiryModal.tsx`

**Interfaces:**
- Consumes: `crearSessionRefreshCore` y `refreshSessionAsync` (Task 4, firmas exactas de sus bloques Produces); componentes `AppText`/`Button` de `mobile/src/shared/ui` y tokens de `mobile/src/shared/theme` (mirar sus exports reales antes de usar).
- Produces: comportamiento RNF-24 completo en mobile; sin cambios de API pública (`useAuth()` mantiene `{ loading, session, login, logout }`).

- [ ] **Step 1: Modal RN**

Crear `mobile/src/auth/SessionExpiryModal.tsx` (mirar `mobile/src/shared/ui` para los componentes reales — `Button` acepta `label`/`onPress`/`variant` según su uso en PartidaLiveScreen):

```tsx
// Modal de continuación de sesión (RNF-24), espejo del modal web.
import React from "react";
import { Modal, StyleSheet, View } from "react-native";
import { AppText, Button } from "../shared/ui";
import { colors, spacing } from "../shared/theme";

export function SessionExpiryModal({
  visible,
  onContinuar,
  onSalir,
}: {
  visible: boolean;
  onContinuar: () => void;
  onSalir: () => void;
}) {
  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onSalir}>
      <View style={styles.backdrop}>
        <View style={styles.card}>
          <AppText variant="bodyStrong">¿Sigues ahí?</AppText>
          <AppText>Tu sesión está por expirar.</AppText>
          <Button label="Continuar sesión" onPress={onContinuar} />
          <Button label="Salir" variant="secondary" onPress={onSalir} />
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    justifyContent: "center",
    padding: spacing.xl,
    backgroundColor: "rgba(0,0,0,0.55)",
  },
  card: {
    gap: spacing.md,
    borderRadius: 12,
    padding: spacing.xl,
    backgroundColor: colors.bg,
  },
});
```

(Ajustar props/variants a los reales de `shared/ui` — no inventar; si `Card` existe úsese en vez del View card.)

- [ ] **Step 2: Scheduler + actividad en `AuthProvider.tsx`**

Modificar `mobile/src/auth/AuthProvider.tsx`:

1. Imports nuevos: `useRef`, `Alert`, `View` de react-native, `crearSessionRefreshCore` (de `./sessionRefreshCore.js`), `refreshSessionAsync` (de `./keycloakMobileAuth`), `SessionExpiryModal` (de `./SessionExpiryModal`).
2. Constante módulo: `const REFRESH_INTERVAL_MS = 270_000;`
3. Dentro del componente, tras los `useState` existentes:

```tsx
  const [modalVisible, setModalVisible] = useState(false);
  const coreRef = useRef<ReturnType<typeof crearSessionRefreshCore> | null>(null);
  // logout se recrea en cada render: el core lo alcanza por ref.
  const logoutRef = useRef<() => Promise<void>>(async () => {});
```

4. Después de definir `logout()`: `logoutRef.current = logout;`
5. Efecto del ciclo — keyed en EXISTENCIA de sesión (booleano), no en el objeto, para que cada refresh no reinicie el interval:

```tsx
  const haySesion = session != null;
  useEffect(() => {
    if (!haySesion) return;

    const core = crearSessionRefreshCore({
      refrescar: async () => {
        const nueva = await refreshSessionAsync();
        if (nueva) {
          setSession(nueva);
          return true;
        }
        return false;
      },
      onModal: setModalVisible,
      onExpirada: () => {
        Alert.alert("Sesión expirada", "Tu sesión expiró. Inicia sesión de nuevo.");
        void logoutRef.current();
      },
    });
    coreRef.current = core;
    const interval = setInterval(() => void core.tick(), REFRESH_INTERVAL_MS);

    return () => {
      clearInterval(interval);
      coreRef.current = null;
      setModalVisible(false);
    };
  }, [haySesion]);
```

6. Reemplazar el `return` final por wrapper de actividad + modal (RNF-24: todo toque cuenta; el capture devuelve `false` para no robar gestos):

```tsx
  return (
    <AuthContext.Provider value={value}>
      <View
        style={{ flex: 1 }}
        onStartShouldSetResponderCapture={() => {
          coreRef.current?.marcarActividad();
          return false;
        }}
      >
        {children}
        <SessionExpiryModal
          visible={modalVisible}
          onContinuar={() => void coreRef.current?.continuar()}
          onSalir={() => void logout()}
        />
      </View>
    </AuthContext.Provider>
  );
```

Nota: no se añade listener de navegación — navegar en mobile implica tocar, y el toque ya marca actividad (decisión del spec).

- [ ] **Step 3: Gates**

Run (en `mobile/`): `npm test` && `npm run typecheck` → verde/limpio. (No hay infra de tests de componentes RN en el repo; la lógica del ciclo quedó testeada en Task 4 — el provider es cableado.)

- [ ] **Step 4: Commit**

```bash
git add mobile/src/auth/AuthProvider.tsx mobile/src/auth/SessionExpiryModal.tsx
git commit -m "feat(mobile): scheduler 270s con actividad y modal de continuacion (RNF-24)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Mobile — hubs con token por getter

**Files:**
- Modify: `mobile/src/features/partidas/sesionHub.js`
- Modify: `mobile/src/features/partidas/rankingHub.js`
- Modify: `mobile/src/features/partidas/PartidaLiveScreen.tsx`
- Test: `mobile/tests/sesionHub.test.js` / `mobile/tests/rankingHub.test.js` (solo si asertan la factory con string)

**Interfaces:**
- Produces: `crearSesionHub(gatewayBaseUrl, getToken)` y `crearRankingHub(gatewayBaseUrl, getToken)` — segundo parámetro pasa de string a `() => string`. Callers conocidos: `PartidaLiveScreen.tsx` (ambos), `PartidaLobbyScreen.tsx` y cualquier otro que salga en grep.

- [ ] **Step 1: Factories**

En `mobile/src/features/partidas/sesionHub.js`:

```js
// getToken en vez de string: el token se lee en cada handshake, así un refresh
// (RNF-24) no obliga a reconectar la conexión viva.
export function crearSesionHub(gatewayBaseUrl, getToken) {
  return new HubConnectionBuilder()
    .withUrl(sesionHubUrl(gatewayBaseUrl), { accessTokenFactory: getToken })
    .withAutomaticReconnect()
    .build();
}
```

Mismo cambio en `rankingHub.js` para `crearRankingHub`.

- [ ] **Step 2: Callers**

`grep -rn "crearSesionHub(\|crearRankingHub(" mobile/src --include="*.tsx" --include="*.js"` y actualizar TODOS los callers. En `PartidaLiveScreen.tsx`:

1. Añadir tras los refs existentes:

```tsx
  const tokenRef = useRef(token);
  tokenRef.current = token;
```

2. Efecto del hub de sesión: `crearSesionHub(apiBaseUrl, () => tokenRef.current)` y deps de `[apiBaseUrl, token, partidaId]` a `[apiBaseUrl, partidaId]`.
3. Efecto del hub de rankings: `crearRankingHub(apiBaseUrl, () => tokenRef.current)` y deps igual a `[apiBaseUrl, partidaId]`.

En los demás callers (p. ej. `PartidaLobbyScreen.tsx`): si el componente no sufre refresh largo o se remonta con frecuencia, basta pasar `() => token` (closure del render actual) — el handshake inicial es lo único que usa el token ahí; aplicar `tokenRef` solo si el efecto ya tenía `token` en deps y conviene quitarlo (mismo patrón de arriba). Decisión por caller, documentar en el reporte.

- [ ] **Step 3: Tests**

Los tests actuales de hubs solo asertan `sesionHubUrl`/`rankingHubUrl` (sin tocar). Si algún test construye la conexión con string, cambiar a `() => "tok"`.

- [ ] **Step 4: Gates**

Run (en `mobile/`): `npm test` && `npm run typecheck` → verde/limpio.

- [ ] **Step 5: Commit**

```bash
git add mobile/src/features/partidas/sesionHub.js mobile/src/features/partidas/rankingHub.js mobile/src/features/partidas/PartidaLiveScreen.tsx
git commit -m "refactor(mobile): hubs SignalR leen el token por getter para no reconectar en refresh

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

(Sumar al add cualquier otro caller/test tocado en Steps 2-3.)

---

### Task 7: Gate E2E contra Keycloak real (la corre el controlador de la sesión)

**Files:**
- Ninguno en el repo (script en el scratchpad). Cierra con commit de docs (traceability).

Pasos:

- [ ] **Step 1:** Variante del script PKCE del scratchpad que imprima el JSON completo del token endpoint (access + refresh) en vez de solo el access token.
- [ ] **Step 2:** Login participante → guardar `A1` (access) y `R1` (refresh). Decodificar `A1`: `exp - iat == 300` (lifetime del realm = premisa RNF-24).
- [ ] **Step 3:** `POST http://localhost:8080/realms/UMBRAL-UCAB/protocol/openid-connect/token` con `grant_type=refresh_token&client_id=umbral-web&refresh_token=R1` (directo a Keycloak, sin gateway) → 200 con `access_token` `A2` ≠ `A1` y `refresh_token` `R2` (flujo cliente↔Keycloak verificado).
- [ ] **Step 4:** `A2` funciona contra el gateway: `GET http://localhost:5080/identity/teams/mine` con `A2` → 200/404 (no 401).
- [ ] **Step 5:** Reuso del refresh viejo `R1` → comportamiento de rotación de Keycloak (documentar lo observado; con rotación estricta sería 400).
- [ ] **Step 6:** Gates finales de ambos clientes en HEAD (web: `npm test` + `npx tsc -b` + `npm run build`; mobile: `npm test` + `npm run typecheck`).
- [ ] **Step 7:** Traceability: fila 3c en `docs/04-sdd/traceability-matrix.md` + evidencia en el ledger `.git/sdd/progress.md`. Commit docs con trailer.

---

## Self-review (hecho)

- Cobertura del spec: núcleo/semántica → T1/T4; web refresh+hook+modal+App+aviso → T2; hubs web → T3; mobile flow+refreshSessionAsync → T4; scheduler+actividad+modal mobile → T5; hubs mobile → T6; E2E + realm premisa → T7. Sin cambios realm/backend/gateway (ninguna task los toca).
- Sin placeholders; código completo en cada step de código.
- Consistencia: `crearSessionRefreshCore({refrescar,onModal,onExpirada})` idéntico T1 (TS) y T4 (JS); `REFRESH_INTERVAL_MS = 270_000` en T2 y T5; `refreshSessionAsync(): AuthSessionState | null` producido en T4 y consumido en T5; getters `() => string` en T3 y T6; textos del modal idénticos web/mobile.
