# Remediación de la auditoría de conformidad Bloques 2-3 (R1-R4) — Design

- **Fecha:** 2026-07-11
- **Origen:** informe `docs/04-sdd/auditorias/2026-07-11-informe-conformidad-bloques2-3.md` (a60d6fb) — CONFORME CON RESERVAS, 0C/1I/3m.
- **Rama:** `feature/bloque-2`. **Cliente:** web (R1), docs (R2, R3), scripts mobile (R4). Sin cambios de backend ni gateway: ya autorizan al Administrador (`/partidas/{**catch-all}` → `OperadorOAdministrador`).
- **Decisión de alcance (usuario):** R1 completo — lista + detalle + **consola en modo observador**.

## R1 — Administrador con vistas de operación en modo lectura (H1, Important, D2)

Doctrina: CLAUDE.md §Clientes — *"publishing, lobby, live operation, rankings, clue delivery, … history — all in read/operate mode; admin views operations read-only"*; SRS:356.

### Rutas (`frontend/src/app/App.tsx`)

| Ruta | Antes | Después |
|---|---|---|
| `partidas` | `need="Operador"` | `need={["Operador", "Administrador"]}` |
| `partidas/crear` | `need="Operador"` | sin cambio (crear = operar) |
| `partidas/:partidaId` | `need="Operador"` | `need={["Operador", "Administrador"]}` |
| `partidas/:partidaId/sesion` | `need="Operador"` | `need={["Operador", "Administrador"]}` |

`partidas/:partidaId/historial`, `puntuaciones/equipos`, `equipos` ya permiten ambos — sin cambio.

### Navegación (`frontend/src/shell/navConfig.tsx`)

- Área `partidas`: `role: "Operador"` → `role: ["Operador", "Administrador"]`.
- Los items ganan `roles?: string[]` **opcional** (sin `roles` = hereda visibilidad del área): "Nueva partida" lleva `roles: ["Operador"]`; "Partidas" queda sin restricción. El filtrado ocurre en `areasForRoles` (único punto de filtrado existente y ya testeado): además de filtrar áreas, devuelve cada área con sus items filtrados por rol.
- Cerrar el minor diferido de 3b: los 2 títulos de test desactualizados de `areasForRoles` en `navConfig.test.tsx` (registrado en ledger: "cerrar en próximo toque de navConfig.test.tsx").

### Gating de acciones — prop `puedeOperar: boolean`

`App.tsx` calcula `puedeOperar = roles.includes("Operador")` y la pasa como prop. Cadena:

- `PartidasListPage` — oculta botón "Nueva partida" si `!puedeOperar`.
- `PartidaDetailPage` — oculta botón "Publicar" si `!puedeOperar` (el resto de la página ya es lectura). El link a historial queda.
- `SesionOperadorPage` — oculta botón "Iniciar" (inicio manual) si `!puedeOperar`; propaga la prop a los paneles.
  - `TriviaRuntimePanel` — oculta "Avanzar pregunta" y "Finalizar juego".
  - `BdtRuntimePanel` — oculta "Avanzar etapa" y "Finalizar juego".
  - `PistasPanel` — se oculta el panel COMPLETO si `!puedeOperar` (enviar pistas es operación; doctrina asigna clue delivery al operador).
- Queda visible para el admin (modo observador): estado de sesión, lobby, pregunta/etapa actual, rankings vivos, consolidado, ubicaciones BDT, historial.

Sin lógica nueva de negocio: el gating es de UI por doctrina; el backend sigue siendo autoritativo (si una acción se escapara, gateway/servicio devuelven 403 igualmente — defensa en profundidad ya auditada).

Sin cambios de `label`/`id`/`data-testid`/ARIA existentes (regla del frontend redesign); solo se condiciona el render de acciones.

## R2 — Matriz de rutas del gateway (H2, Minor, D6)

`contracts/http/gateway-api.md`, tabla "Route matrix (SP-5a)": añadir la fila `identity-teams-listing` — Order 0, match exacto `GET /identity/teams`, policy `OperadorOAdministrador`, cluster identity — con nota de que intercepta antes del catch-all `identity-teams` (Order 1, Participante), que conserva `/mine` y POST. Fuente ya correcta: `identity-api.md` §"Teams listing for the web console" y `gateway/src/Umbral.Gateway/appsettings.json:24-29`.

## R3 — Índice de eventos de Puntuaciones (H3, Minor, D6)

`contracts/events/puntuaciones-events.md:15-18`: reemplazar los "Payload not registered / Defined by SDD" de `PuntajeTriviaIncrementado`, `RankingTriviaActualizado`, `RankingBDTActualizado`, `RankingConsolidadoCalculado` por referencia cruzada a `contracts/http/puntuaciones-api.md` §"SignalR — ranking en vivo (SP-4c)", que ya registra los payloads completos. No duplicar los payloads (una sola fuente).

## R4 — Scripts de arranque mobile (H4, Minor, D12)

Borrar las líneas que generan `EXPO_PUBLIC_TRIVIA_API_BASE_URL` y `EXPO_PUBLIC_BDT_API_BASE_URL` en `mobile/run-local.sh` (líneas 16-17) y el bloque equivalente en `mobile/run-local.ps1` — simetría con la limpieza de `.env.example` hecha en 92506ef. Ningún código las lee (auditado).

## Pruebas

- `navConfig.test.tsx`: admin ve área "partidas"; item "Nueva partida" ausente para admin, presente para operador; títulos de los 2 tests `areasForRoles` corregidos.
- Routing (patrón existente de tests de App/rutas): admin accede a `partidas`, `partidas/:id`, `partidas/:id/sesion`; `partidas/crear` lo rebota al landing.
- Por componente con `puedeOperar={false}` vs `{true}`: PartidasListPage (botón crear), PartidaDetailPage (botón publicar), SesionOperadorPage (botón iniciar + PistasPanel ausente), TriviaRuntimePanel y BdtRuntimePanel (avanzar/finalizar).

## Gates

- Web: `npm test` + `npx tsc -b` + `npm run build` (artefactos generados se borran, nunca se commitean).
- Mobile: `npm test` + `npm run typecheck` (smoke — R4 no toca `mobile/src`).
- Sin stack vivo: cambios 100% UI/docs/scripts; la autorización viva ya quedó evidenciada en la auditoría y en los E2E por slice.

## Cierre documental

- `docs/04-sdd/traceability-matrix.md`: fila de remediación (H1-H4 → commits).
- Informe de auditoría: anexo breve "Remediación aplicada" con mapping hallazgo → commit (el veredicto original no se reescribe).
- Ledger `.git/sdd/progress.md`: sección de la remediación con línea por tarea.
