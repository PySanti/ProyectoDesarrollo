# Informe de Auditoría — Bloques 2 y 3 · Conformidad con doctrina y CLAUDE.md

> Estado: **informe final**. Fecha: 2026-07-11. Plan: `2026-07-11-auditoria-conformidad-bloques2-3.md` (5406c6b). Rama: `feature/bloque-2`, rango `aacdcd8..59be6f9` (125 commits, 247 archivos). Método: 5 clústeres subagent read-only en paralelo (A1 web · A2 mobile · A3 gateway+backend tocado · A4 contratos+realtime · A5 no-services+suites+SDD); adjudicación y síntesis por el controller.

## 1. Veredicto global

**CONFORME CON RESERVAS** — 0 Critical · **1 Important** · 3 Minor.

La doctrina dura se cumple en todo el rango: todo el tráfico de ambos clientes pasa por el gateway (incluido SignalR), no hay reglas de negocio en cliente, no hay gameplay de participante en web ni pantallas de operación en mobile, el refresh RNF-24 es estrictamente cliente↔Keycloak, los rankings se muestran fieles al payload del backend, el backend tocado respeta la estructura graduada y los límites de servicio, no queda consumo vivo de los servicios retirados, y las 4 suites en HEAD igualan los baselines exactos. La reserva única es funcional-doctrinal: el Administrador quedó sin las vistas de operación en modo lectura que CLAUDE.md y el SRS le asignan.

## 2. Veredicto por dimensión

| Dim | Ámbito | Veredicto | C/I/m |
|---|---|---|---|
| D1 | Gateway único (web+mobile) | CONFORME | 0/0/0 |
| D2 | Enrutamiento de clientes | **HALLAZGO** | 0/1/0 |
| D3 | Doctrina auth (Keycloak directo, RNF-24, borde) | CONFORME | 0/0/0 |
| D4 | Backend autoritativo | CONFORME | 0/0/0 |
| D5 | Ranking en clientes | CONFORME | 0/0/0 |
| D6 | Código↔contratos | HALLAZGOS | 0/0/2 |
| D7 | Backend tocado (estructura graduada + límites) | CONFORME | 0/0/0 |
| D8 | Realtime (hubs vía gateway, grupos, anti-leak, getter) | CONFORME | 0/0/0 |
| D9 | No-services | CONFORME | 0/0/0 |
| D10 | Pruebas (4 gates re-ejecutados en HEAD) | CONFORME | 0/0/0 |
| D11 | Integridad SDD (13 slices × 4 artefactos) | CONFORME | 0/0/0 |
| D12 | Diferimientos | HALLAZGO | 0/0/1 |

## 3. Hallazgos

### H1 — [Important · D2] Administrador sin vistas de operación en modo lectura (web)

- **Evidencia:** `frontend/src/shell/navConfig.tsx:34-42` (área "partidas" con `role: "Operador"`) y `frontend/src/app/App.tsx:141-164` (rutas `partidas`, `partidas/crear`, `partidas/:partidaId` con `need="Operador"`). El Administrador solo accede a `partidas/:partidaId/historial`, `puntuaciones/equipos` y `equipos`.
- **Fuente violada:** CLAUDE.md §Clientes: *"…publishing, lobby, live operation, rankings… — all in read/operate mode; admin views operations read-only"*; SRS (`docs/01-project-source/srs.md:356`): el admin *"puede consultar partidas, rankings, historial y detalles operativos en modo lectura"*.
- **Agravante de consistencia:** el gateway SÍ autoriza al admin (`/partidas/{**catch-all}` → policy `OperadorOAdministrador`, `contracts/http/gateway-api.md:30`), y `PartidaDetailPage` es de solo lectura por diseño — el bloqueo es exclusivamente de UI (nav + guard de ruta).
- **Adjudicación:** no existe diferimiento documentado (buscado en `docs/04-sdd/auditorias/`, specs y plans del rango — nada). Los specs de 2b/2c dimensionaron la consola para el Operador sin pronunciarse sobre el acceso lectura del admin, así que no hay conflicto spec↔doctrina que escalar: es un hueco. Severidad Important: gap funcional real contra doctrina, con remediación acotada.
- **Remediación propuesta (decisión de alcance del responsable):** mínimo — permitir Administrador en lista y detalle de partidas (páginas ya read-only). Punto a decidir: si el admin también ve la consola de sesión (`SesionOperadorPage`) con las acciones de operación ocultas/deshabilitadas (la doctrina dice "views operations read-only", lo que sugiere que sí, en modo observador), o solo lista+detalle en esta pasada.

### H2 — [Minor · D6] Matriz de rutas del gateway sin la fila `identity-teams-listing`

- **Evidencia:** `contracts/http/gateway-api.md:24-33` (tabla "Route matrix (SP-5a)") no lista la ruta `identity-teams-listing` (Order 0, `GET /identity/teams` exacto, policy `OperadorOAdministrador`) que existe en `gateway/src/Umbral.Gateway/appsettings.json:24-29` y consume `frontend/src/api/identityApi.ts:249-261`.
- **Fuente violada:** CLAUDE.md — `contracts/` es fuente de verdad de los contratos HTTP.
- **Adjudicación (dedup A3-Minor vs A4-Important → Minor):** drift solo documental. El comportamiento es correcto y está pinneado por 4 tests del gateway; la ruta está completamente documentada en `contracts/http/identity-api.md:84-96`; ningún cliente puede romperse. El costo real es que un lector de la matriz no ve que la regla específica intercepta `GET /identity/teams` antes del catch-all Participante. Fix = una fila de tabla.

### H3 — [Minor · D6] Índice de eventos de Puntuaciones desactualizado

- **Evidencia:** `contracts/events/puntuaciones-events.md:15-18` lista `PuntajeTriviaIncrementado`, `RankingTriviaActualizado`, `RankingBDTActualizado`, `RankingConsolidadoCalculado` como "Payload not registered / Defined by SDD", mientras `contracts/http/puntuaciones-api.md:202-225` (§SignalR SP-4c) los registra completos y ambos clientes los consumen con el shape correcto (`frontend/src/features/partidas/useRankingHub.ts:33-35`, `mobile/src/features/partidas/PartidaLiveScreen.tsx:144-151`).
- **Fuente violada:** consistencia interna de `contracts/` (events vs http).
- **Adjudicación:** Minor — el código coincide con el contrato HTTP; solo el índice hermano quedó stale. Fix = registrar payloads o referencia cruzada al §SP-4c.

### H4 — [Minor · D12, borde D1] Scripts de arranque mobile aún generan vars legacy

- **Evidencia:** `mobile/run-local.sh:16-17` y el bloque equivalente de `mobile/run-local.ps1` siguen escribiendo `EXPO_PUBLIC_TRIVIA_API_BASE_URL=http://IP:5015` y `EXPO_PUBLIC_BDT_API_BASE_URL=http://IP:5016` en el `.env` generado. El commit `92506ef` (bloque 2d) limpió `mobile/.env.example` pero no los scripts.
- **Fuente violada:** CLAUDE.md gateway obligatorio (única base además de Keycloak) + plan §2 (el diferimiento documentado solo cubre `EXPO_PUBLIC_TEAM_API_BASE_URL`, no estas dos).
- **Adjudicación:** Minor — ningún código activo lee esas vars (`mobile/src/config/env.ts` no las consume; grep 0 en `mobile/src`), el `.env` generado está gitignored. Es un diferimiento ejecutado a medias (asimetría plantilla↔scripts), no una fuga de tráfico. Fix = borrar 2 líneas en cada script.

**Nota informativa (no hallazgo):** el evento `ConvocatoriaCreada` está documentado y emitido pero ningún cliente lo escucha aún (inbox de convocatorias funciona por GET) — coherente con los minors diferidos del ledger.

## 4. Suites re-ejecutadas en HEAD (59be6f9)

| Gate | Resultado | Baseline | Match |
|---|---|---|---|
| frontend `npm test` | 182/182 (29 archivos) | 182/29 | ✓ |
| frontend `npx tsc -b` + `npm run build` | exit 0 ambos | — | ✓ |
| mobile `npm test` + `npm run typecheck` | 88/0 + exit 0 | 88 | ✓ |
| Identity sln | 170 Unit + 41 Integration + 41 Contract, 0 fallos | 170/41/41 | ✓ |
| Gateway sln | 24, 0 fallos | 24 | ✓ |

Artefactos generados por tsc/build borrados tras el gate; working tree limpio al cierre (`git status` vacío).

## 5. Cobertura de la auditoría

- **A1 web:** 65 archivos `frontend/src`; greps de puertos/vars/gameplay/sorts/almacenamiento de credenciales; lectura completa de api (6 módulos + 2 factories), auth (ciclo RNF-24 end-to-end incl. `router.dispose`), paneles runtime, consolidado y rendimiento.
- **A2 mobile:** 89 archivos `mobile/src`; 15 endpoints únicos todos sobre el gateway; PKCE S256 confirmado en la fuente de expo-auth-session; sin librería QR ni texto esperado en cliente (grep 0); geolocalización emisión pura cada ~2 s; `RootNavigator` bloquea roles no-Participante.
- **A3 gateway+backend:** 34 archivos del rango; JWT/policies/fallback fail-secure; CORS movido al borde (Identity retiró el propio); sin fuga de precedencia en rutas (tests lo pinnean); controllers `ControllerBase`+MediatR con tests dedicados; carpetas `Application/` exactas; interfaces en Domain / implementaciones en Infrastructure; grep cross-DB limpio; cero rastro de refresh en gateway.
- **A4 contratos+realtime:** 47 endpoints únicos cruzados campo a campo contra 6 contratos y los DTOs C# (28 web + 24 mobile, 5 compartidos) — sin drift en lo consumido; 15/16 eventos realtime cruzados; 2 hubs de servidor leídos (grupos server-side desde JWT, `HubException` si no inscrito); anti-leak verificado en records de payloads; sin `PathRemovePrefix`; patrón token-por-getter en 5/5 callers.
- **A5:** greps no-services en las 3 superficies (0 activos); 4 gates re-ejecutados; 13 slices × 4 artefactos SDD verificados; 5 diferimientos con anotación durable localizada.

## 6. Remediación propuesta (slice aparte, decisión del responsable)

| # | Hallazgo | Esfuerzo | Acción |
|---|---|---|---|
| R1 | H1 admin read-only partidas | Acotado (UI + tests de nav/rutas) | Habilitar Administrador en lista+detalle; decidir alcance sobre consola de sesión en modo observador |
| R2 | H2 matriz gateway-api.md | Trivial | Añadir fila `identity-teams-listing` |
| R3 | H3 puntuaciones-events.md | Trivial | Registrar payloads o referencia cruzada a puntuaciones-api.md §SP-4c |
| R4 | H4 run-local mobile | Trivial | Borrar las 2 líneas legacy en `.sh` y `.ps1` |

R2-R4 son cambios de documentación/scripts sin riesgo; R1 es el único con código de producto y decisión de alcance.

## 7. Remediación aplicada (2026-07-11, posterior al veredicto)

Slice de remediación ejecutado sobre la misma rama (spec dadc5ad, plan cc0cb76). El veredicto original de §1 no se reescribe; con estos cierres el estado efectivo queda sin hallazgos abiertos:

| Hallazgo | Commit | Cierre |
|---|---|---|
| H1 (Important, D2) | 19dfce0 · 7129e35 · 566cd50 | Admin con lista, detalle y consola de sesión en modo observador: rutas/nav abiertas a ambos roles y prop `puedeOperar` que oculta todas las acciones mutantes (crear, publicar, iniciar, avanzar/finalizar, panel de pistas); vistas de solo lectura (lobby, rankings, geolocalización, estado) intactas. |
| H2 (Minor, D6) | d965dde | Fila `identity-teams-listing` añadida a la matriz de `gateway-api.md`, espejo del `appsettings.json` real, con nota de precedencia. |
| H3 (Minor, D6) | d965dde | Los 4 eventos de ranking en `puntuaciones-events.md` referencian los payloads registrados en `puntuaciones-api.md` §SP-4c; las reglas de broker permanecen SDD-gated (hoy solo existe la pata SignalR). |
| H4 (Minor, D12) | d965dde | `run-local.sh`/`.ps1` de mobile ya no generan las vars legacy (y se retiraron sus variables de puerto sin uso). |

Gates post-remediación: frontend 194/194 (29 archivos) + `tsc -b` + build; mobile 88/88 + typecheck. Reviews por tarea APPROVED (0 Critical / 0 Important).
