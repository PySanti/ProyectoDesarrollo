# Plan de reconstrucción de frontend (web + mobile)

Registro **persistente** del rediseño visual de UMBRAL con el skill `impeccable`. Es la fuente
de verdad del avance; se actualiza a medida que avanzan las fases y se atienden observaciones.

> **Naturaleza del trabajo:** reconstrucción **visual + arquitectura de información (IA)**, *no*
> funcional. No cambia contratos (`contracts/`), reglas de negocio ni HUs; respeta el routing
> web/mobile y la autoridad del backend (ver `AGENTS.md`). Donde la IA cambia navegación, se
> mantiene dentro de flujos ya documentados.

Artefactos de marca (fuente de verdad visual): `PRODUCT.md`, `DESIGN.md`, `.impeccable/design.json` (raíz).

## Dirección de marca (resumen)

- Registro **product**. North Star: "El tablero que respira". Tema **claro** (blanco puro).
- Primario **magenta** `oklch(0.58 0.19 330)` = señal de "lo que está vivo"; neutros cargan densidad.
  Acento indigo. WCAG **AA** verificado. Tipografía: Space Grotesk + Inter + JetBrains Mono.
- Web (Administrador/Operador) = "en control". Mobile (Participante/Líder) = "competitivo/urgencia".
- Anti-referencias: SaaS azul genérico, infantil/gamificado, corporativo gris, marketing llamativo.

## Fases

### Fase 0 — Fundación compartida · ✅ COMPLETA
- `PRODUCT.md` (cubre las dos superficies), `DESIGN.md` (tokens hex + OKLCH, formato Stitch),
  `.impeccable/design.json` (sidecar: rampas, sombras, motion, componentes, narrativa).

### Fase 1 — Web · ✅ COMPLETA
- Todas las superficies de Administrador/Operador crafteadas con el sistema de diseño. Pendiente
  transversal: pase visual en navegador con datos reales (gates de auth). Verificado por build + 52 tests.
- **App-shell · ✅ construido.** react-router v6, nav rail por área (Identidad/Trivia/BDT) filtrado
  por rol, topbar (identidad + pill de rol + cerrar sesión), estados (carga/no-autorizado/404),
  responsive (rail → iconos → drawer). Tokens CSS en `frontend/src/styles/{tokens,base,components,shell}.css`.
  Pendiente: pase visual en navegador (bloqueado por gate Keycloak; verificado por build + tests).
- **Superficies a craftear (orden sugerido):**
  1. **Operación Trivia · ✅ crafteada.** `/trivia/operar` (`TriviaOperationsPage`) reescrita como
     **supervisión pura** en layout master-detail: lista de partidas (pill de estado vivo) +
     panel de detalle (cabecera con estado + acciones Iniciar/Actualizar, stat strip, y paneles
     Participantes/Equipos/Ranking con conteos, empty states que enseñan y skeleton de carga).
     Estilos en `frontend/src/styles/trivia-ops.css`. **Cambio de IA:** el "Crear formulario"
     que vivía embebido en Operar se movió a su **propio tab "Crear formulario"**
     (`/trivia/formularios/nuevo`, `CreateTriviaFormPage`), reflejando el flujo Formulario →
     Partida → Supervisión. Verificado: tsc + vite build + 52 tests (tests divididos por página).
     Pendiente: pase visual en navegador (gate Operador + trivia-service con datos).
  2. **Crear Trivia / formularios · ✅ crafteada.** `CreateTriviaFormPage` (editor de preguntas con
     badge numerado, contador en vivo, opciones en grid, meta strip puntaje/tiempo/correcta, barra
     de acciones add/submit) y `CreateTriviaGamePage` (formulario agrupado en secciones "Datos" +
     "Configuración", con aviso si no hay formularios completos). Estilos en
     `frontend/src/styles/create-forms.css` (compartido) + helper `.btn-icon` en components.css.
     Controles y labels intactos (sin romper contratos ni tests). Verificado: tsc + build + 52 tests.
  3. **Crear BDT · ✅ crafteada.** `CreateBdtGamePage` reagrupada en secciones (Datos / Configuración /
     Etapas con contador). Cada **etapa** es ahora una card hermana de las preguntas de Trivia
     (badge numerado, eliminar con icono). El **decode de QR** se muestra como estado on-brand
     (`.notice success/error/info`; hint muted en idle) y el `input[type=file]` lleva botón de marca
     (`::file-selector-button`). Reusa `create-forms.css`. Labels/ids/testids/textos de estado
     intactos. Verificado: tsc + build + 52 tests.
  4. **Partidas BDT publicadas · ✅ crafteada.** `PublishedBdtGamesPage`: cabecera con contador,
     **state pills** (pill--lobby/live/done) en vez de badges planos —coherente con Operación Trivia—,
     acciones con icono (Iniciar BDT con Play), aviso de inicio con fecha formateada, **empty state
     que enseña** (`.empty-panel` compartido, reemplaza la nota plana) y modal de resumen con
     `.badge` + id en `.mono`. Se eliminó el `.eyebrow` sin estilo. Extraje `.empty-panel` y `.mono`
     a components.css (Operar Trivia ahora también usa `.empty-panel`). Estado/textos/testids/roles
     intactos. Verificado: tsc + build + 52 tests.
  5. **Identidad · ✅ crafteada.** `CreateUserPage` con cabecera `create-head` + nota de que el rol
     inicial es permanente. `UserManagementPage` (ya tenía tabla paginada + StatusPill por OBS-01/02):
     pasada fina — contador de usuarios en la cabecera, botón Recargar con icono que gira al cargar,
     **empty state que enseña** (`.empty-panel` + icono Users), ID en `.mono`, y cabeceras de card
     generalizadas a `.card-head` (antes reusaban `.question-card-header`). Labels/testids intactos.
     Verificado: tsc + build + 52 tests.
  6. **Gobernanza (`/identidad/gobernanza`) · ✅ construida sobre design system (SP-5c).** `GovernancePage`
     (matriz de permisos por rol, guardado por card) + modal de cambio de rol en `UserManagementPage`.
     Solo `Administrador`. Verificado: tsc + build + 67 tests (11 archivos).

### Fase 2 — Mobile (React Native) · ✅ código COMPLETO (pase Expo pendiente)
- `impeccable` es web-only (su tooling no corre en RN). Se reusan los tokens de `DESIGN.md` vía un
  **theme TS** (RN no entiende `oklch()`; por eso `DESIGN.md` lleva hex). Verificación manual con Expo.
- **Plan detallado y bitácora por fases:** `mobile-redesign-plan.md` (mismo directorio). Decisiones
  (2026-06-14): fuentes custom, mantener stack + Home hub, fases por flujo vertical.
- **Hecho (2026-06-14):** las **11 pantallas** re-skinneadas con el theme + primitivos RN
  (`shared/ui/`), fuentes de marca cargadas (Space Grotesk/Inter/JetBrains Mono), `StatePill` con punto
  "En vivo" pulsante (reduce-motion), código/UUIDs en mono, y limpieza del CSS-in-JS legacy
  (`shared/styles.ts` + `ScreenWrapper` eliminados). Sistema mobile documentado en `design-system.md`.
  Verificado por fase: `tsc --noEmit` + `npm test` (83 tests).
- **Pendiente transversal:** pase visual manual en Expo con datos reales (dispositivo/LAN), análogo al
  pendiente de Fase 1 web. Único bloqueo: requiere entorno levantado (Keycloak + servicios + Expo).
- **v2 — Registro de juego (inmersivo):** sobre el v1, una capa más **competitiva/inmersiva** para el
  participante manteniendo **misma paleta y fuentes**. Plan: `mobile-game-register-plan.md`. Decidido
  (2026-06-14): inmersivo total, las 4 palancas (motion/tipografía/color/iconos) y los 4 momentos estelares
  (podio/cuenta regresiva/lobby vivo/puntaje con reacción). Guardrail: inmersivo sí, kitsch no.

## Observaciones del usuario (2026-06-13) y su resolución

| ID | Observación | Estado | Detalle |
|----|-------------|--------|---------|
| OBS-01 | No mostrar el rol/permisos del usuario en el panel | ✅ Hecho | `UserManagementPage`: se quitaron los displays de rol (línea "Rol" e input solo-lectura). El rol se sigue asignando al **crear** (HU-01 lo exige por contrato); solo se oculta en gestión. UI-only, sin cambio de contrato. |
| OBS-02 | Lista de usuarios como tabla con paginación | ✅ Hecho | `UserManagementPage`: la lista pasó de `<ul>` a tabla (`Nombre`/`Correo`/`Estado` con state pill), paginación cliente (8/pág, Anterior/Siguiente, "Página X de Y · N usuarios") y empty state. |
| OBS-03 | Al entrar a :5173 no mostrar el login genérico de Keycloak; un login acorde a UMBRAL | ✅ Hecho | **Frontend:** el flujo de auth ya no redirige directo (`auth/keycloak.ts` dejó `onLoad: login-required`); al entrar se muestra una **pantalla de login UMBRAL** (`shell/states.tsx` `LoginScreen`) con botón "Iniciar sesión" que recién ahí va a Keycloak. **Página de credenciales:** tema `umbral` (`infra/keycloak/themes/umbral/login/`, overlay CSS) **aplicado automáticamente** vía `loginTheme=umbral` en el realm import. |
| OBS-04 | El pill de rol del usuario logueado mostraba todos los roles técnicos de Keycloak | ✅ Hecho | `auth/keycloak.ts` `extractRoles` ahora filtra a **solo roles de aplicación** (Administrador/Operador/Participante) y descarta los técnicos (`default-roles-*`, `offline_access`, `uma_authorization`, `manage-account`…). El pill de la topbar queda limpio en todos lados. |
| OBS-05 | Tras "Iniciar sesión" el redirect daba **"page not found"** | ✅ Hecho | Dos causas: (a) `login()` enviaba `redirect_uri` sin barra final → no casaba con el patrón registrado `http://localhost:5173/*`; corregido a `origin + "/"`. (b) Causa raíz: el realm `UMBRAL-UCAB` **se había perdido** porque el contenedor de Keycloak no persistía `/opt/keycloak/data` y se recreó. Solución durable: realm import (`infra/keycloak/import/umbral-realm.json`) + `start-dev --import-realm` + volumen `umbral-keycloak-data` en `docker-compose.yml`. Verificado: realm 200, auth endpoint 302, import OK en logs. |
| OBS-06 | Cargar la lista de usuarios → UI "Usuario no autenticado" / backend "Failed to validate the token" | ✅ Hecho | El realm import asignaba a cada usuario solo su rol de app (`["Administrador"]`), lo que **reemplaza** `default-roles-umbral-ucab`. Sin esos roles por defecto el usuario no tiene acceso al cliente `account`, así el token salía **sin claim `aud`** y los servicios (`ValidateAudience=true`, audiencias válidas incluyen `account`) lo rechazaban. Fix: `realmRoles: ["default-roles-umbral-ucab", "<rol>"]`. Re-sembrado y verificado: token con `aud:"account"`. Nota: re-sembrar rota las claves de firma del realm → reiniciar los backend en `dotnet run` y re-loguear en la web. |
| OBS-07 | Crear usuario → "Error de integración con Keycloak" | ✅ Hecho | `identity-service` usa `client_credentials` (cliente confidencial `identity-service` + secret) contra la Admin API. Dos desajustes del realm re-importado: (a) el **secret** del cliente no coincidía con `services/identity-service/.env`; (b) el **service account** necesitaba `realm-admin` (el adaptador hace `GET /admin/realms/.../roles/{name}`, que requiere `view-realm`, además de `manage-users`). Fix: SA → `realm-admin` en el import; secret alineado a `umbral-identity-secret` (import + `.env` + realm corriendo). Aplicado **en caliente vía Admin API** (sin re-sembrar, sin rotar claves). Verificado: el SA obtiene token y lee un rol de realm. Requiere reiniciar `identity-service` para tomar el `.env`. |
| OBS-08 | Un `Participante` autenticado podía entrar a la web, y un `Administrador`/`Operador` a la app móvil (cada cliente solo debe atender a su rol) | ✅ Hecho (2026-06-15) | **Gating de cliente por rol** en cada front. **Web** (`frontend/`): `shell/states.tsx` `UnauthorizedScreen` muestra en rojo (`role="alert"`, color `var(--danger)`) el mensaje exacto **"El panel web es exclusivo para administradores y operadores"** + botón Cerrar sesión; `app/App.tsx` lo activa cuando la cuenta no tiene `Administrador` ni `Operador` (el guard ya existía) y le pasa `onLogout`. **Mobile** (`mobile/`): nueva `screens/RoleRestrictedScreen.tsx` con `Notice variant="error"` y el mensaje exacto **"El panel móvil es exclusivo para participantes"** + botón Cerrar sesión; `navigation/RootNavigator.tsx` la renderiza cuando `session.user.roles` **no** incluye `Participante`, en vez del `AppStack`. **Nota de alcance:** es la única excepción al "rediseño solo visual" — implementa la regla de ruteo por actor (web = Administrador/Operador, móvil = Participante) ya documentada en `CLAUDE.md`/`AGENTS.md`; no cambia contratos, reglas de negocio ni HU. El backend sigue siendo la autoridad de permisos por endpoint; esto es gating de UX. Verificado: `tsc --noEmit` limpio en ambos, 52 vitest (web) + 81 node:test (mobile). |
| OBS-09 | Al crear/editar usuarios, el admin no tenía feedback sobre el envío del correo de credenciales (la feature backend de correo es de HU-01/HU-02) | ✅ Hecho (2026-06-15) | **Feedback de correo en web** (cambio de copy/estado, no de contrato). **Crear** (`features/identity/CreateUserPage.tsx`): texto anticipatorio ("se enviará un correo con la contraseña temporal"), botón "Creando usuario y enviando correo…" + aviso `role="status"` durante el envío, y `mapErrorMessage` distingue el `502` por correo del de Keycloak avisando que **el usuario no fue creado**. **Editar** (`features/identity/UserManagementPage.tsx`): nota fija (`data-testid="hu02-email-hint"`) avisando que cambiar el correo de un usuario sin primer login reenvía credenciales; si el correo cambió, botón "Guardando y enviando correo…" + aviso `role="status"`; `mapHu02ErrorMessage` añade el `502` (correo vs Keycloak) avisando que **el cambio se revirtió**. El backend (HU-01/HU-02) es la autoridad: el front solo refleja estado. Verificado: `tsc --noEmit` limpio · 53 vitest (web). |

## Verificación (Fase 1 web, al 2026-06-14)
- `tsc --noEmit`: limpio · `vite build`: OK · `vitest`: 52 tests pasando.
- Tests del shell cubren render + routing + filtrado por rol. Tests de HU-02 actualizados a la tabla/sin-rol.
- Pase visual en navegador con datos reales: ✅ realizado (2026-06-14) — 5 superficies verificadas con Keycloak + servicios levantados.
- Commit del rediseño completo + infra Keycloak en `feature/design-improve`: ✅ realizado (commit `6132c5e`).

## Impacto en SDD
- OBS-01/02 tocan la **UI de HU-02** (Identity). Es cambio de presentación, no de contrato ni reglas.
  Nota añadida en `docs/04-sdd/specs/HU-02-.../acceptance.md`.

## Para un agente nuevo: cómo continuar

Lee, en este orden:
1. Este archivo (estado y roadmap) + **`design-system.md`** (mismo directorio): la API de clases CSS
   implementada y las convenciones. Reutilizar primitivos, no reinventar.
2. `AGENTS.md` (raíz, reglas canónicas) y `CLAUDE.md` (resumen operativo): separación web/mobile,
   web = solo Administrador/Operador, autoridad del backend.
3. `PRODUCT.md` + `DESIGN.md` (raíz): marca y tokens.
4. Para levantar el entorno: `GUIA-LEVANTAMIENTO.md` + **`infra/keycloak/README.md`** (el realm
   `UMBRAL-UCAB` se siembra solo vía import; credenciales de prueba `admin/operador/participante`).

**Regla de oro:** rediseño **visual + IA, no funcional**. No tocar contratos, reglas, ni
`label`/`id`/`data-testid`/roles que usan los tests. Verificar cada cambio con
`tsc --noEmit` + `vite build` + `vitest run` (52 tests) desde `frontend/`.
Única excepción registrada: **OBS-08** (gating de cliente por rol) — implementa la regla de
ruteo por actor ya documentada en `CLAUDE.md`/`AGENTS.md`, sin tocar contratos ni HU.

## Siguientes pasos
1. ~~**Pase visual en navegador** de las 5 superficies con datos reales.~~ ✅ Realizado (2026-06-14).
2. ~~**Commit** de todo el rediseño + infra Keycloak en `feature/design-improve`.~~ ✅ Realizado (commit `6132c5e`).
3. **Fase 2 — Mobile (React Native):** `impeccable` NO corre en RN. Reusar los tokens de `DESIGN.md`
   creando un **theme TS** en `mobile/` (RN no entiende `oklch()`; por eso `DESIGN.md` lleva hex),
   luego re-skin de pantallas existentes preservando comportamiento. Verificación manual con Expo
   (`mobile/`): toques ≥44px, contraste a pleno sol.
