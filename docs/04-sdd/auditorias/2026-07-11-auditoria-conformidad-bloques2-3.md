# Plan de Auditoría — Bloques 2 y 3 · Conformidad con doctrina y CLAUDE.md

> Estado: **plan** (metodología, sin hallazgos). Fecha: 2026-07-11. Rama: `feature/bloque-2`, rango `aacdcd8..59be6f9` (develop → HEAD, 125 commits, 247 archivos, +23k/−8k). Read-only: la auditoría no muta código ni lo commitea; solo este plan y su informe se commitean en `docs/04-sdd/auditorias/`.

## 1. Contexto y propósito

Desde `develop` (aacdcd8: SP-4 + SP-5 + Bloque 6 CI) se implementaron, vía subagent-driven-development con review final APPROVED cada uno:

- **Bloque 2 — re-cableado de clientes:** 2a fundación gateway (identity/equipos vía :5080, CORS al borde) · 2b web config partidas (wizard multi-juego) · 2c-1..4 consola operador (publicar/lobby/inicio, runtime Trivia, runtime BDT, consolidado + retiro legacy web) · 2d mobile panel/participación (+ retiro legacy mobile) · 2e-1/2 mobile gameplay Trivia y BDT · 2f UI Puntuaciones web.
- **Bloque 3:** 3a push de rankings a clientes (SP-4c) · 3b vista web de equipos (GET /identity/teams + gateway + EquiposPage + deep-link) · 3c RNF-24 (refresh 270s con control de inactividad y modal, web + mobile, hubs por getter).

Esta auditoría verifica —contra las fuentes de autoridad, no contra la memoria del implementador ni los reviews previos— que el resultado conforma con la doctrina y las normas de CLAUDE.md. Es la primera auditoría de conformidad que cubre superficie de clientes (web/mobile) además de gateway y backend tocado.

**Salida:** informe `2026-07-11-informe-conformidad-bloques2-3.md` con veredicto por dimensión + tabla de hallazgos (Critical/Important/Minor, evidencia `archivo:línea` y fuente violada) + veredicto global + lista de remediación propuesta. Solo informe; los fixes serían otro slice, decisión del responsable.

## 2. Alcance

**Dentro (todo lo tocado en el rango):**
- `frontend/src/**` (web completa: api/, auth/, app/, shell/, features/).
- `mobile/src/**` (auth PKCE, panel, gameplay, hubs) + `mobile/.env` referencial.
- `gateway/**` (rutas YARP nuevas, matriz de autorización por rol, WebSockets).
- Backend tocado por el rango: `services/identity-service` (teams listing, policy `OperadorOAdministrador`), `services/operaciones-sesion` (endpoint partidas-publicadas y demás toques), `services/puntuaciones` (normalización `realm_access.roles`, `KeycloakRoleClaims`).
- `contracts/http/*.md` y `contracts/events/*.md` en lo que los clientes consumen.
- Artefactos SDD del rango: specs/plans 2a..3c en `docs/superpowers/{specs,plans}/`, `docs/04-sdd/traceability-matrix.md`, ledger `.git/sdd/progress.md`.

**Fuera (diferimientos documentados — verificar que estén correctamente anotados, NO marcarlos como huecos):**
- Retiro físico de `services/trivia-game-service` y `services/bdt-game-service` + secciones legacy en `infra/docker-compose.yml` + var muerta `EXPO_PUBLIC_TEAM_API_BASE_URL` (Bloque 3 de la auditoría de cobertura 2026-07-06).
- RNF-23 / BR-R05 correo asíncrono vía RabbitMQ (SMTP directo hoy; diferido de SP-5b).
- Cobertura 48.1% vs meta RNF-09 ≥90% (instrumentada en Bloque 6).
- Minors diferidos por reviews finales, registrados en ledger por slice (2a..3c).
- Equipos-admin (Bloque 4 de cobertura): HU-06/09/19-aprobación/48, BR-E06/E10/E11.

## 3. Fuentes de autoridad (orden de precedencia)

| # | Fuente | Ruta | Qué gobierna |
|---|---|---|---|
| 1 | AGENTS.md | `/AGENTS.md` | Ruleset maestro |
| 2 | CLAUDE.md | `/CLAUDE.md` | Topología 4 servicios + gateway obligatorio, regla de enrutamiento de clientes, doctrina auth/refresh, backend autoritativo, ranking, no-services, estructura graduada |
| 3 | ADRs | `docs/05-decisions/` | Slugs/puertos/DB (0009), estado runtime (0010), remediación (0011), composites Keycloak + matriz gateway (0013) |
| 4 | Business rules / SRS | `docs/02-project-context/business-rules.md`, `srs-summary.md`, `bdt-ranking-clarification.md` | BR-*/RF-*/RNF-* (RNF-21 gateway, RNF-24 refresh) |
| 5 | Contratos | `contracts/http/*.md`, `contracts/events/*.md` | Forma canónica HTTP/eventos/realtime |
| 6 | SDD del rango | specs/plans `2026-07-0[7-9]*` y `2026-07-10*` + homónimos | Decisiones aprobadas por slice |
| 7 | Traceability + ledger | `docs/04-sdd/traceability-matrix.md`, `.git/sdd/progress.md` | Estado declarado y diferimientos |

Regla de adjudicación: hallazgo que contradice lo que un spec/plan aprobado exige explícitamente → se escala como decisión del responsable (hallazgo + texto del spec, cuál gobierna), nunca se descarta en silencio.

## 4. Dimensiones

- **D1 — Gateway único:** TODO tráfico cliente↔backend pasa por `:5080`, incluido SignalR (negotiate + WebSocket). Cero URLs directas a servicios en web/mobile: grep de puertos `:5000/:5010/:5020/:5030/:5099` y de vars `VITE_*`/`EXPO_PUBLIC_*`; la única base permitida es la del gateway (+ Keycloak `:8080`, que es directo por doctrina). CORS al borde, no en servicios.
- **D2 — Regla de enrutamiento de clientes:** historias admin/operador → web, participante → mobile. Sin gameplay de participante en web; sin pantallas admin/operador en mobile; vistas admin de operación read-only donde el SRS lo pide.
- **D3 — Doctrina auth:** clientes autentican directo con Keycloak (web `umbral-web`, mobile `umbral-mobile` PKCE S256); refresh SOLO cliente↔Keycloak (RNF-24: 270s + actividad + modal, sin gateway/backend en el ciclo); gateway valida JWT y autoriza por rol base a nivel de ruta sin consultar Identity; permisos funcionales dentro de los servicios; sin passwords almacenados.
- **D4 — Backend autoritativo:** clientes validan solo usabilidad. Ninguna regla de negocio en cliente: no cierre de preguntas/etapas, no validación de correctitud de respuestas/QR, no cálculo de puntos/ranking, no decisión de avance de juegos.
- **D5 — Ranking en clientes:** display fiel a doctrina — Trivia por puntos acumulados (desempate tiempo), BDT por puntos de etapas ganadas (NUNCA por conteo de etapas; conteo solo informativo), consolidado por (juegos ganados, puntos totales, menor tiempo). Sin re-cálculo ni re-ordenamiento propio en cliente que contradiga el payload del backend.
- **D6 — Código↔contratos:** cada endpoint y evento realtime consumido por web/mobile existe en `contracts/` con verbo/ruta/roles/códigos/payload consistentes; tipos TS ↔ DTOs C# campo a campo en lo consumido.
- **D7 — Backend tocado (estructura graduada + límites):** en los archivos del rango de identity-service, operaciones-sesion y puntuaciones: `Application/` con carpetas exactas, controllers heredan `ControllerBase`, despachan por MediatR, sin lógica de negocio, con unit tests; dominio sin dependencia de infraestructura; sin lectura/escritura de DB ajena; manejo centralizado de excepciones intacto.
- **D8 — Realtime:** hubs alcanzables solo vía gateway (prefijos correctos, sin `PathRemovePrefix` indebido); membresía de grupos decidida server-side desde JWT; anti-leak en payloads (sin texto de preguntas/opciones/QR esperado hacia participantes, ubicación solo a operador, pista solo al destino); patrón token-por-getter en los 5 callers (web useSesionHub/useRankingHub; mobile PartidaLiveScreen×2, PartidaLobbyScreen) sin churn de conexión en refresh.
- **D9 — No-services:** cero consumo vivo desde clientes/gateway hacia trivia-game-service, bdt-game-service o team-service (URLs, env vars activas, imports). Las carpetas en disco y secciones compose son diferimiento documentado, no hallazgo; SÍ es hallazgo cualquier referencia activa remanente.
- **D10 — Pruebas:** re-ejecutar en HEAD (no confiar en el ledger): frontend `npm test` + `npx tsc -b` + `npm run build`; mobile `npm test` + `npm run typecheck`; Identity sln; gateway sln. Baselines declarados: frontend 182/29 archivos, mobile 88, Identity 170/41/41, gateway 24. Controllers nuevos con tests.
- **D11 — Integridad SDD:** cada slice 2a..3c con spec + plan commiteados, fila en traceability-matrix y cierre en ledger; lo declarado coincide con el código en HEAD.
- **D12 — Diferimientos:** los del §2 correctamente anotados donde corresponde; nada a medio construir (puerto medio-cableado, pantalla huérfana, env var que apunta a servicio muerto usada en código activo).

## 5. Método — 5 clústeres subagent-driven (read-only, paralelos)

| Clúster | Dimensiones | Foco |
|---|---|---|
| **A1** Web | D1-D5 (lado web) | `frontend/src/**`: bases de URL y hubs vía gateway; sin vistas de participante; auth/refresh RNF-24 (keycloak.ts, useSessionRefresh, core, modal, router dispose); sin reglas de negocio; display de rankings fiel |
| **A2** Mobile | D1-D5 (lado mobile) | `mobile/src/**`: ídem lado participante; PKCE S256 + SecureStore; refresh flow/core/scheduler; geoloc solo emisión (~2s), QR solo upload (validación en backend); sin pantallas admin |
| **A3** Gateway + backend tocado | D3, D7 | Rutas YARP del rango (orden, paths exactos, methods, policies) vs matriz ADR-0013; JWT/roles al borde; estructura graduada y límites en archivos tocados de identity/operaciones-sesion/puntuaciones (incl. `KeycloakRoleClaims`) |
| **A4** Contratos + realtime | D6, D8 | Endpoints/eventos consumidos por ambos clientes ↔ `contracts/` línea a línea; payloads TS↔C#; grupos/anti-leak; hubs vía gateway; token-por-getter en 5/5 callers |
| **A5** No-services + suites + SDD | D9-D12 | Grep legacy en clientes/gateway; **re-ejecutar los 4 gates en HEAD**; specs/plans/traceability/ledger del rango; diferimientos del §2 bien anotados |

Reglas para cada auditor:
- **Read-only HARD:** prohibido editar, stagear, commitear, `git checkout/restore/clean/stash/reset`. Solo lectura, grep y (solo A5) ejecutar suites/typecheck/build.
- Todo hallazgo: severidad + evidencia `archivo:línea` + fuente de autoridad violada + dimensión. Sin evidencia → no es hallazgo.
- No confiar en specs/reviews previos como prueba de conformidad: son insumo, la prueba es el código en HEAD.
- Los diferimientos del §2 no son hallazgos; SÍ lo es un diferimiento a medio construir.

Orquestación: los 5 auditores corren en paralelo (sonnet, general-purpose). El controller adjudica (dedup, severidad final, escalamientos) y sintetiza el informe con: veredicto global, tabla por dimensión (D1-D12, C/I/m), hallazgos con evidencia, suites re-ejecutadas y lista de remediación propuesta.

## 6. Criterio de veredicto global

- **CONFORME:** 0 Critical y 0 Important (Minors se listan para remediación futura).
- **CONFORME CON RESERVAS:** 0 Critical, ≥1 Important con remediación acotada.
- **NO CONFORME:** ≥1 Critical (violación de límite duro — tráfico cliente fuera del gateway, regla de negocio en cliente, DB cruzada, gameplay participante en web / admin en mobile, drift de contrato que rompe clientes, o regla graduada rota).
