# Pendientes de web (frontend/) — dejados por Santiago para Mariangel

Cambios de backend/contrato que requieren un ajuste en `frontend/`. Santiago no toca `frontend/`
(frontera dura anti-conflictos); cada entrada dice qué cambió en el backend y qué falta en la web.

---

## 1. Inicio manual con mínimos no alcanzados: ahora es 409, ya no 200+Cancelada

**Fix backend:** fix 1 (partida por equipos se cancelaba al iniciar). Commit en `fixes-santiago-2`.

**Qué cambió en el contrato** (`contracts/http/operaciones-sesion-api.md`):
`POST /operaciones-sesion/partidas/{id}/inicio` con los mínimos sin cumplir **ya no cancela** la
partida. Antes devolvía `200 { estado: "Cancelada" }`; **ahora devuelve `409`** y la partida
**sigue en `Lobby`**, para que el operador acepte las solicitudes pendientes y reintente. La
cancelación automática por mínimos quedó **exclusiva del inicio por tiempo** (`/inicio-automatico`),
que sigue devolviendo `200 { estado: "Cancelada" }`.

El `LobbyDto` gana un campo nuevo: **`participacionesConfirmadas`** — el quórum que el inicio exige
de verdad. En Individual = `inscritosActivos`; en Equipo cuenta solo equipos activos con ≥1
convocatoria **aceptada**, así que puede ser `< inscritosActivos`.

**Qué falta en `frontend/`** (`src/features/partidas/SesionOperadorPage.tsx`, `operacionesApi.ts`):
- `onIniciar()` hoy trata `r.estado === "Cancelada"` como pantalla de cancelación. Con el fix, el
  inicio manual insuficiente ya no llega por ahí: llega como **error 409**. Manejar ese 409
  mostrando un aviso tipo *"Aún no se cumplen los mínimos de participación (N de M confirmadas).
  Acepta las solicitudes pendientes y vuelve a iniciar"* — **sin** mandar la partida a la pantalla
  "La partida fue cancelada" (ya no está cancelada, sigue en Lobby).
- Mostrar en el lobby el `participacionesConfirmadas` junto a `inscritosActivos` (p. ej.
  "Confirmadas: 1 / mínimo 2") para que el operador entienda por qué el inicio se rechaza — hoy solo
  ve `inscritosActivos`, que en Equipo no es el número que decide el arranque.
- Los tests `SesionOperadorPage.test.tsx` que hoy afirman "inicio manual que devuelve Cancelada
  muestra la pantalla de mínimos no alcanzados" (mock `iniciarPartida` → `{ estado: "Cancelada" }`)
  ya no reflejan el contrato del inicio **manual**; ese caso ahora es 409. El push del hub
  `onCancelada` con motivo `MinimosNoAlcanzados` **sigue existiendo** para el inicio automático, así
  que la pantalla de cancelación sigue siendo válida por esa vía — no la borres, solo separa los dos
  caminos (manual = 409 en Lobby; automático = Cancelada por push).
