# Bloque 7b — UI de aprobación de inscripciones (HU-19) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** El operador ve y resuelve (acepta/rechaza) las solicitudes de inscripción pendientes desde la consola web, y el participante ve en mobile si su inscripción está `Pendiente` o `Activa`. Cierra HU-19 (paquete R1 del informe de completitud; catch-up de UI diferido por el spec 4B).

**Architecture:** Solo clientes — el backend de HU-19 ya existe y está verde (`POST /operaciones-sesion/partidas/{partidaId}/inscripciones/{inscripcionId}/aceptacion|rechazo`, ambos devuelven **200 + LobbyDto** actualizado; `LobbyDto` trae `solicitudesPendientesIndividual[]` y `solicitudesPendientesEquipo[]`). Web: extender tipos + 2 funciones API + panel de solicitudes en `LobbyView` gateado `puedeOperar`. Mobile: `partidaLobbyFlow` expone `estadoInscripcion` (hoy lo colapsa en booleano) y la pantalla muestra el estado real.

**Tech Stack:** React/Vite/TypeScript + vitest (web) · React Native/Expo + `node --test` (mobile).

## Global Constraints

- Rama de trabajo: `feature/bloque-7` (ya activa). NO crear ramas nuevas.
- Commits terminan con: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`
- PROHIBIDO a implementadores: `git stash/reset/checkout/restore/clean`. Solo `git add <paths exactos>` + `git commit`.
- Gates frontend: `cd frontend && npx vitest run && npx tsc -b && npm run build`. `tsc -b`/build generan artefactos (`tsconfig*.tsbuildinfo`, `vite.config.js/.d.ts`, `vitest.config.js/.d.ts`) — **borrarlos, nunca commitearlos**.
- Gates mobile: `cd mobile && npm test && npm run typecheck` (Node ≥ 20.19.4).
- No añadir dependencias. Textos de UI en español. NO cambiar `data-testid`/labels existentes; los nuevos siguen la convención kebab del archivo.
- El admin observador NO opera: toda acción mutante nueva en web va gateada con `ctx.puedeOperar` (patrón existente de `btn-iniciar`).

---

### Task 1: Web — tipos y funciones API de aprobación

**Files:**
- Modify: `frontend/src/api/operacionesApi.ts` (interface `LobbyDto` ~línea 11-21; funciones nuevas junto a `getLobby` ~línea 93)
- Modify: `frontend/src/api/operacionesApi.test.ts` (tests nuevos siguiendo el patrón del archivo)

**Interfaces:**
- Consumes: helper interno existente `request<T>(path, init, fetchImpl)` y `buildAuthHeaders(accessToken)` (ya en el archivo).
- Produces (Task 2 los usa): tipos `SolicitudIndividual { inscripcionId: string; participanteId: string; fechaInscripcion: string }`, `SolicitudEquipo { inscripcionId: string; equipoId: string; miembros: number; fechaInscripcion: string }`; campos nuevos en `LobbyDto`; funciones `aceptarInscripcion(partidaId, inscripcionId, accessToken, fetchImpl?)` y `rechazarInscripcion(partidaId, inscripcionId, accessToken, fetchImpl?)`, ambas `Promise<LobbyDto>`.

- [ ] **Step 1: Tests que fallan**

En `operacionesApi.test.ts`, siguiendo el patrón de los tests existentes de `getLobby` (mock de `fetch`, stub de `VITE_GATEWAY_BASE_URL`), añadir:

```typescript
it("aceptarInscripcion hace POST a la ruta de aceptación y devuelve el LobbyDto", async () => {
  const lobby = {
    partidaId: "p1",
    sesionPartidaId: "s1",
    estado: "Lobby",
    modalidad: "Individual",
    minimosParticipacion: 1,
    maximosParticipacion: 10,
    inscritosActivos: 1,
    participantes: ["u1"],
    equipos: [],
    solicitudesPendientesIndividual: [],
    solicitudesPendientesEquipo: []
  };
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(JSON.stringify(lobby), { status: 200 })
  );

  const result = await aceptarInscripcion("p1", "i1", "token-abc", fetchMock);

  expect(fetchMock).toHaveBeenCalledWith(
    "https://gw.example.test/operaciones-sesion/partidas/p1/inscripciones/i1/aceptacion",
    expect.objectContaining({ method: "POST" })
  );
  expect(result.inscritosActivos).toBe(1);
});

it("rechazarInscripcion hace POST a la ruta de rechazo", async () => {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(JSON.stringify({ solicitudesPendientesIndividual: [], solicitudesPendientesEquipo: [] }), { status: 200 })
  );

  await rechazarInscripcion("p1", "i1", "token-abc", fetchMock);

  expect(fetchMock).toHaveBeenCalledWith(
    "https://gw.example.test/operaciones-sesion/partidas/p1/inscripciones/i1/rechazo",
    expect.objectContaining({ method: "POST" })
  );
});

it("aceptarInscripcion propaga el error del backend (409 solicitud no pendiente)", async () => {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(JSON.stringify({ message: "La inscripción no está pendiente." }), { status: 409 })
  );

  await expect(aceptarInscripcion("p1", "i1", "t", fetchMock)).rejects.toMatchObject({
    name: "OperacionesApiError",
    statusCode: 409
  });
});
```

Ajustar la URL base esperada al valor que stubbea el archivo (leer el `beforeEach` real; si stubbea otro dominio, usar ese). Importar `aceptarInscripcion`/`rechazarInscripcion` en el import existente del módulo.

- [ ] **Step 2: Verificar RED**

Run: `cd frontend && npx vitest run src/api/operacionesApi.test.ts`
Expected: FAIL — `aceptarInscripcion`/`rechazarInscripcion` no existen (error de import/compilación).

- [ ] **Step 3: Implementación**

En `operacionesApi.ts`:

(a) Tipos nuevos junto a `LobbyEquipo` y campos en `LobbyDto`:

```typescript
export interface SolicitudIndividual {
  inscripcionId: string;
  participanteId: string;
  fechaInscripcion: string;
}

export interface SolicitudEquipo {
  inscripcionId: string;
  equipoId: string;
  miembros: number;
  fechaInscripcion: string;
}
```

y dentro de `LobbyDto`, tras `equipos: LobbyEquipo[];`:

```typescript
  solicitudesPendientesIndividual: SolicitudIndividual[];
  solicitudesPendientesEquipo: SolicitudEquipo[];
```

(b) Funciones nuevas tras `getLobby`:

```typescript
export async function aceptarInscripcion(
  partidaId: string,
  inscripcionId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<LobbyDto> {
  return request<LobbyDto>(
    `/operaciones-sesion/partidas/${partidaId}/inscripciones/${inscripcionId}/aceptacion`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}

export async function rechazarInscripcion(
  partidaId: string,
  inscripcionId: string,
  accessToken: string,
  fetchImpl: typeof fetch = fetch
): Promise<LobbyDto> {
  return request<LobbyDto>(
    `/operaciones-sesion/partidas/${partidaId}/inscripciones/${inscripcionId}/rechazo`,
    { method: "POST", headers: buildAuthHeaders(accessToken) },
    fetchImpl
  );
}
```

(c) Revisar fixtures existentes del test file y de otros tests que construyan `LobbyDto` literal (grep `solicitudesPendientes` y `inscritosActivos` en `frontend/src/`): al ser campos requeridos nuevos, los literales tipados existentes fallarán en `tsc` — añadirles `solicitudesPendientesIndividual: [], solicitudesPendientesEquipo: []`.

- [ ] **Step 4: Verificar GREEN**

Run: `cd frontend && npx vitest run src/api/operacionesApi.test.ts`
Expected: PASS completo.

- [ ] **Step 5: Typecheck (atrapa fixtures desactualizados)**

Run: `cd frontend && npx tsc -b`
Expected: exit 0. Si falla por literales `LobbyDto` sin los campos nuevos, completarlos (Step 3c) y repetir. Borrar artefactos generados.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/api/operacionesApi.ts frontend/src/api/operacionesApi.test.ts
git commit -m "feat(web): tipos y API de aprobación de inscripciones HU-19 (7b)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

(Si el Step 5 tocó otros archivos de test, añadirlos al add con path exacto.)

---

### Task 2: Web — panel de solicitudes pendientes en la consola del operador

**Files:**
- Modify: `frontend/src/features/partidas/SesionOperadorPage.tsx` (componente `LobbyView` ~línea 270-344; `VistaCtx` y su construcción; handlers junto a los existentes tipo `onIniciar`)
- Modify: el archivo de tests existente de la página (`frontend/src/features/partidas/SesionOperadorPage.test.tsx` o donde el repo teste `LobbyView` — localizar con `grep -rln "lobby-panel" frontend/src`)

**Interfaces:**
- Consumes: `aceptarInscripcion`/`rechazarInscripcion` de Task 1; `ctx.puedeOperar`, `ctx.onActualizar` existentes; `lobby.solicitudesPendientesIndividual/Equipo`.
- Produces: en `VistaCtx`: `onAceptarSolicitud(inscripcionId: string): void` y `onRechazarSolicitud(inscripcionId: string): void`; testids nuevos `solicitudes-panel`, `btn-aceptar-solicitud`, `btn-rechazar-solicitud`.

- [ ] **Step 1: Tests que fallan**

En el archivo de tests de la página, siguiendo su patrón de render (mocks de hubs/API que ya use), añadir 3 tests:

```tsx
it("muestra las solicitudes pendientes con botones para el operador", () => {
  // render de LobbyView (o la página) con un lobby que tenga
  // solicitudesPendientesIndividual: [{ inscripcionId: "i1", participanteId: "u1", fechaInscripcion: "2026-07-12T10:00:00Z" }]
  // y puedeOperar: true
  expect(screen.getByTestId("solicitudes-panel")).toBeInTheDocument();
  expect(screen.getAllByTestId("btn-aceptar-solicitud")).toHaveLength(1);
  expect(screen.getAllByTestId("btn-rechazar-solicitud")).toHaveLength(1);
});

it("oculta los botones de aprobar/rechazar al admin observador (puedeOperar=false) pero muestra la lista", () => {
  // mismo lobby, puedeOperar: false
  expect(screen.getByTestId("solicitudes-panel")).toBeInTheDocument();
  expect(screen.queryByTestId("btn-aceptar-solicitud")).toBeNull();
  expect(screen.queryByTestId("btn-rechazar-solicitud")).toBeNull();
});

it("sin solicitudes pendientes no renderiza el panel", () => {
  // lobby con ambas listas vacías
  expect(screen.queryByTestId("solicitudes-panel")).toBeNull();
});
```

Adaptar el arnés al patrón real del archivo (leerlo primero; reusar sus builders/mocks). Los fixtures de lobby del arnés deben incluir los campos nuevos.

- [ ] **Step 2: Verificar RED**

Run: `cd frontend && npx vitest run src/features/partidas`
Expected: los 3 tests nuevos FALLAN (`solicitudes-panel` no existe).

- [ ] **Step 3: Implementación**

(a) En `SesionOperadorPage` (componente página), junto a los handlers existentes (patrón de `onIniciar`/`onActualizar`), añadir:

```tsx
const onAceptarSolicitud = async (inscripcionId: string) => {
  try {
    const lobbyActualizado = await aceptarInscripcion(partidaId, inscripcionId, accessToken);
    aplicarLobby(lobbyActualizado); // usar el setter/estado que ya alimenta la vista lobby;
                                    // si no existe un setter directo, llamar al mismo refetch que usa onActualizar
  } catch (error) {
    mostrarError(error); // canal de error existente de la página (leer cómo muestran errores onIniciar/enviarPista)
  }
};

const onRechazarSolicitud = async (inscripcionId: string) => {
  try {
    const lobbyActualizado = await rechazarInscripcion(partidaId, inscripcionId, accessToken);
    aplicarLobby(lobbyActualizado);
  } catch (error) {
    mostrarError(error);
  }
};
```

Los nombres `aplicarLobby`/`mostrarError` son descriptivos: usar los mecanismos REALES del archivo (estado de vista lobby y canal de error ya existentes — leer el archivo antes; si la página refresca lobby solo vía `onActualizar`, es válido resolver ambos handlers como `await aceptarInscripcion(...); ctx.onActualizar();`). Añadir ambos a `VistaCtx` (tipo + objeto construido).

(b) En `LobbyView`, tras el bloque de la tabla de equipos y antes de `compact-actions`, insertar:

```tsx
      {lobby.solicitudesPendientesIndividual.length > 0 || lobby.solicitudesPendientesEquipo.length > 0 ? (
        <div className="table-wrap" data-testid="solicitudes-panel">
          <h2>Solicitudes pendientes</h2>
          <table aria-label="Solicitudes de inscripción pendientes">
            <thead>
              <tr>
                <th scope="col">{lobby.modalidad === "Equipo" ? "Equipo" : "Participante"}</th>
                <th scope="col">Fecha</th>
                {ctx.puedeOperar ? <th scope="col">Acciones</th> : null}
              </tr>
            </thead>
            <tbody>
              {lobby.solicitudesPendientesIndividual.map((s) => (
                <tr key={s.inscripcionId}>
                  <td>{s.participanteId}</td>
                  <td>{new Date(s.fechaInscripcion).toLocaleString()}</td>
                  {ctx.puedeOperar ? (
                    <td className="compact-actions">
                      <button type="button" data-testid="btn-aceptar-solicitud" onClick={() => ctx.onAceptarSolicitud(s.inscripcionId)}>
                        Aceptar
                      </button>
                      <button type="button" className="secondary-button" data-testid="btn-rechazar-solicitud" onClick={() => ctx.onRechazarSolicitud(s.inscripcionId)}>
                        Rechazar
                      </button>
                    </td>
                  ) : null}
                </tr>
              ))}
              {lobby.solicitudesPendientesEquipo.map((s) => (
                <tr key={s.inscripcionId}>
                  <td>{s.equipoId} ({s.miembros} miembros)</td>
                  <td>{new Date(s.fechaInscripcion).toLocaleString()}</td>
                  {ctx.puedeOperar ? (
                    <td className="compact-actions">
                      <button type="button" data-testid="btn-aceptar-solicitud" onClick={() => ctx.onAceptarSolicitud(s.inscripcionId)}>
                        Aceptar
                      </button>
                      <button type="button" className="secondary-button" data-testid="btn-rechazar-solicitud" onClick={() => ctx.onRechazarSolicitud(s.inscripcionId)}>
                        Rechazar
                      </button>
                    </td>
                  ) : null}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
```

- [ ] **Step 4: Verificar GREEN + suite del feature**

Run: `cd frontend && npx vitest run src/features/partidas`
Expected: PASS completo (nuevos + existentes).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/SesionOperadorPage.tsx <archivo-de-tests-tocado>
git commit -m "feat(web): panel de solicitudes pendientes con aceptar/rechazar en lobby operador (7b)

HU-19: lista solicitudes Individual/Equipo del LobbyDto; acciones gateadas
con puedeOperar (admin observador solo ve la lista).

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Mobile — estado real de la inscripción (Pendiente vs Activa)

**Files:**
- Modify: `mobile/src/features/partidas/partidaLobbyFlow.js:26-46` (dejar de colapsar el estado)
- Modify: `mobile/src/features/partidas/PartidaLobbyScreen.tsx` (banner de estado + texto de éxito al inscribirse)
- Modify: test existente del flow (localizar con `grep -rln "partidaLobbyFlow" mobile/tests/`) y de la screen si existe.

**Interfaces:**
- Consumes: `getMiSesion` ya devuelve `sesion.inscripcion.estado` (`"Pendiente" | "Activa"`, `MiSesionDto`).
- Produces: `cargarLobby` devuelve además `estadoInscripcion: "Pendiente" | "Activa" | null` (null = sin participación en esta partida). `inscrito` se conserva sin cambio de semántica (los callers existentes no se tocan).

- [ ] **Step 1: Test del flow que falla**

En el test del flow (patrón `node --test` del archivo real), añadir caso: `getMiSesion` mockeado devolviendo `{ ok: true, sesion: { partidaId: "p1", inscripcion: { inscripcionId: "i1", estado: "Pendiente" } } }` → `cargarLobby` retorna `estadoInscripcion === "Pendiente"` e `inscrito === true`; y caso sin sesión → `estadoInscripcion === null`.

- [ ] **Step 2: Verificar RED**

Run: `cd mobile && npm test`
Expected: los casos nuevos FALLAN (`estadoInscripcion` undefined).

- [ ] **Step 3: Implementar en el flow**

En `partidaLobbyFlow.js`, reemplazar las líneas que calculan `inscrito` (26-27) y el return (46):

```js
  const sesionActual =
    mia.ok && mia.sesion != null && mia.sesion.partidaId === partidaId ? mia.sesion : null;
  const inscrito = sesionActual != null;
  const estadoInscripcion = sesionActual?.inscripcion?.estado ?? null;
```

y en el return: `return { ok: true, lobby: body, inscrito, estadoInscripcion, esLider };`

- [ ] **Step 4: Verificar GREEN del flow**

Run: `cd mobile && npm test`
Expected: PASS.

- [ ] **Step 5: Banner en la screen**

En `PartidaLobbyScreen.tsx`: (a) guardar `estadoInscripcion` en estado junto a `inscrito` (misma carga, línea ~63); (b) tipo del resultado (línea ~31) gana `estadoInscripcion: "Pendiente" | "Activa" | null`; (c) render, junto al bloque de inscritos (~línea 123), añadir:

```tsx
{estadoInscripcion === "Pendiente" ? (
  <Text style={styles.avisoPendiente}>
    Tu solicitud está pendiente de aprobación del operador.
  </Text>
) : null}
{estadoInscripcion === "Activa" ? (
  <Text style={styles.avisoActiva}>Inscripción confirmada. Estás dentro.</Text>
) : null}
```

con estilos nuevos en el `StyleSheet` del archivo siguiendo su paleta (`shared/theme`): `avisoPendiente` (color de advertencia del theme) y `avisoActiva` (color de éxito). (d) El mensaje de éxito al inscribirse (línea ~106) cambia de `"¡Listo! Estás dentro."` a `"Solicitud enviada. Pendiente de aprobación del operador."` (el de cancelar queda igual) y tras la acción se re-carga el lobby (si ya lo hace, no tocar).

- [ ] **Step 6: Typecheck + suite mobile**

Run: `cd mobile && npm run typecheck && npm test`
Expected: exit 0 + PASS. Si algún test de la screen asertaba el texto viejo `"¡Listo! Estás dentro."`, actualizarlo al nuevo.

- [ ] **Step 7: Commit**

```bash
git add mobile/src/features/partidas/partidaLobbyFlow.js mobile/src/features/partidas/PartidaLobbyScreen.tsx <tests-tocados>
git commit -m "feat(mobile): estado Pendiente/Activa de la inscripción en el lobby (7b)

HU-19: el participante ve si su solicitud espera aprobación del operador;
el flow expone estadoInscripcion en lugar de colapsarlo en un booleano.

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Gates completos del slice + ledger

**Files:** ninguno nuevo (verificación; fixes commiteados si un gate falla).

**Interfaces:**
- Consumes: Tasks 1-3 completas.
- Produces: evidencia de cierre 7b.

- [ ] **Step 1:** `cd frontend && npx vitest run` → todo verde (215+). Luego `npx tsc -b && npm run build` → exit 0; borrar artefactos generados (`git status` limpio).
- [ ] **Step 2:** `cd mobile && npm test && npm run typecheck` → todo verde.
- [ ] **Step 3:** Verificación de criterio: `grep -n "solicitudesPendientes" frontend/src/api/operacionesApi.ts frontend/src/features/partidas/SesionOperadorPage.tsx mobile/src/features/partidas/partidaLobbyFlow.js` — las dos primeras con hits; `grep -n "estadoInscripcion" mobile/src/features/partidas/PartidaLobbyScreen.tsx` con hits.
- [ ] **Step 4:** Append al ledger `.git/sdd/progress.md`: "7b DONE" + hashes de los 3 commits + números exactos de gates.

---

## Addendum 7b-bis (aprobado 2026-07-12): estado de inscripción de Equipo en mi-sesión + follow-ups de test

Origen: review final del slice (opus). Gap: en modalidad Equipo, `ObtenerMiSesionQueryHandler` cae al
fallback `estado = "Equipo"` (las inscripciones de equipo tienen `ParticipanteId == Guid.Empty`), por lo que
el banner mobile de Task 3 nunca aparece para equipos. Fix backend mínimo + 2 follow-ups de test del triage.

### Task 5: Backend — mi-sesión expone el estado real de la inscripción de equipo

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs:21-32`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerMiSesionQueryHandlerPendienteTests.cs` (o el archivo hermano `ObtenerMiSesionQueryHandlerTests.cs` si encaja mejor — leer ambos primero y seguir su arnés)
- Modify: `contracts/http/operaciones-sesion-api.md` (nota de `MiSesionDto.inscripcion`)

**Interfaces:**
- Consumes: `MiSesionDto`/`InscripcionResumenDto` existentes (shape SIN cambio); `InscripcionPartida.OcupaParticipacion`, `.Convocatorias`, `.Estado`.
- Produces: para un caller en modalidad Equipo con convocatoria en una inscripción de equipo, `inscripcion.estado` = `"Pendiente"` o `"Activa"` (el estado real de la inscripción del equipo) e `inscripcion.inscripcionId` = el id de esa inscripción. El fallback `"Equipo"` queda solo para el caso sin inscripción resoluble.

- [ ] **Step 1: Tests que fallan.** En el archivo de tests elegido, siguiendo su arnés existente (builder de sesión Equipo con preinscripción + convocatoria): (a) caller con convocatoria en inscripción de equipo **Pendiente** → `miSesion.Inscripcion.Estado == "Pendiente"` y `InscripcionId` = id de esa inscripción; (b) tras `AceptarInscripcion` (operador) → `Estado == "Activa"`. Correr `dotnet test` con filtro al archivo → RED (hoy devuelve "Equipo").
- [ ] **Step 2: Implementación.** En el handler, tras el `FirstOrDefault` existente de `inscripcion` (línea ~21-22), añadir:

```csharp
        // 7b-bis: en Equipo la inscripción del caller no tiene su ParticipanteId (es del equipo);
        // se resuelve por su convocatoria para exponer el estado real (Pendiente/Activa) — HU-19.
        if (inscripcion is null && sesion.Modalidad == Modalidad.Equipo)
        {
            inscripcion = sesion.Inscripciones.FirstOrDefault(
                i => i.OcupaParticipacion && i.Convocatorias.Any(c => c.UsuarioId == request.ParticipanteId));
        }
```

  (El resto del método no cambia: `inscEstado` ya usa `inscripcion?.Estado.ToString() ?? "Equipo"`.)
- [ ] **Step 3: GREEN + regresión.** `dotnet test` del proyecto UnitTests completo (verificar que los tests existentes de mi-sesión Individual y el fallback "Equipo" para no-inscritos siguen verdes; si algún test existente asertaba `"Equipo"` para un caller CON convocatoria, actualizarlo — ese era exactamente el comportamiento roto). Después suite completa del servicio: `export DOTNET_ROOT=/snap/dotnet-sdk/current; dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln` → verde.
- [ ] **Step 4: Contrato.** En `contracts/http/operaciones-sesion-api.md`, actualizar la nota de `MiSesionDto` (línea ~73): `inscripcion.estado` ∈ `Pendiente|Activa` también en modalidad Equipo (resuelto por la convocatoria del caller); `"Equipo"` solo como fallback sin inscripción resoluble.
- [ ] **Step 5: Commit** con mensaje `fix(operaciones): mi-sesion expone estado real de inscripcion de equipo (7b-bis)` + cuerpo breve (gap del review 7b) + trailer `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.

### Task 6: Web — tests de clic aceptar/rechazar + fixture Equipo del panel

**Files:**
- Modify: `frontend/src/features/partidas/SesionOperadorPage.test.tsx`

**Interfaces:**
- Consumes: panel de Task 2 (testids `btn-aceptar-solicitud`/`btn-rechazar-solicitud`), mocks del arnés existente (mismo patrón que el click-test de `btn-iniciar`), `aceptarInscripcion`/`rechazarInscripcion` mockeables vía el mock del módulo `operacionesApi` que el arnés ya use.
- Produces: 3 tests nuevos; cero cambios de producción.

- [ ] **Step 1:** Leer el arnés del archivo (cómo mockea `operacionesApi` y cómo hace click en `btn-iniciar`). Añadir: (a) click en `btn-aceptar-solicitud` → `aceptarInscripcion` llamada con `(partidaId, "i1", token)` y la vista muestra el lobby devuelto (p.ej. la solicitud desaparece de la tabla); (b) click en `btn-rechazar-solicitud` → ídem con `rechazarInscripcion`; (c) fixture con `solicitudesPendientesEquipo: [{ inscripcionId: "ie1", equipoId: "e1", miembros: 3, fechaInscripcion: "2026-07-12T10:00:00Z" }]` → la fila Equipo renderiza "e1" y "3" y sus botones aparecen con `puedeOperar`.
- [ ] **Step 2:** RED si algún wiring está mal; GREEN esperado directo (el código ya existe — estos tests PINNEAN comportamiento). `npx vitest run src/features/partidas` verde completo.
- [ ] **Step 3: Commit** `test(web): clic aceptar/rechazar + fixture Equipo del panel de solicitudes (7b-bis)` + trailer.
