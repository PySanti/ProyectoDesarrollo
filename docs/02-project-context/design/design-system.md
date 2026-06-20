# Sistema de diseño implementado (web + mobile)

Referencia del **código real** del frontend de UMBRAL: qué archivos lo componen, qué clases/primitivos
existen y cómo extenderlo. La **primera mitad** documenta la **web** (CSS); la **segunda** (al final),
el **mobile** (React Native + theme TS). Complementa a `DESIGN.md` (raíz), que define el *lenguaje* de
marca (tokens, intención, do's/don'ts); este documento describe la *materialización* en código.

> **Regla de oro (la siguió todo el rediseño):** la reconstrucción es **visual + IA, no funcional**.
> No se tocan contratos (`contracts/`), reglas de negocio, ni se cambian `label`/`id`/`data-testid`/
> roles ARIA que usan los tests. Cada superficie se verifica con `tsc --noEmit` + `vite build` +
> `vitest run` (52 tests verdes) antes de darse por hecha.

## Mapa de archivos CSS

`frontend/src/styles.css` es solo un agregador de `@import`:

| Archivo | Contenido |
|---|---|
| `styles/tokens.css` | Tokens en **OKLCH** (espejo de `DESIGN.md`): colores, tipografía, radios, espaciado (escala 4pt), sombras, motion, z-index, métricas del shell. `@import` de Google Fonts. |
| `styles/base.css` | Reset, body blanco, tipografía base, `:focus-visible` (anillo magenta), `prefers-reduced-motion`, `.skip-link`. |
| `styles/components.css` | **Primitivos compartidos** (la API de clases que se reutiliza en todas las superficies). Ver abajo. |
| `styles/shell.css` | App-shell: `.sh-shell`, `.sh-rail`, `.sh-nav-item`, `.sh-topbar`, `.sh-role-pill`, `.sh-state` (login/carga/no-autorizado/404), responsive (rail → iconos → drawer). |
| `styles/trivia-ops.css` | Superficie **Operación Trivia** (master-detail supervisión): `.ops-grid`, `.ops-master`, `.ops-row`, `.ops-detail__*`, `.ops-stats`, `.ops-panel`, `.ops-skel`, `.ops-spin`, `.ops-icon-btn`. |
| `styles/create-forms.css` | Superficies **de creación** (Crear formulario / Crear Trivia / Crear BDT): `.create-head`, `.form-section`, `.form-section__title/__hint`, `.q-title`, `.q-badge`, `.q-meta`, `.stage-status`, `.create-actions`. |

**Para una superficie nueva:** crea `styles/<superficie>.css`, añádelo con un `@import` en `styles.css`,
y **reutiliza primero los primitivos** de `components.css`; solo añade clases propias para layout
específico de esa pantalla.

## Tokens (resumen; fuente: `tokens.css` / `DESIGN.md`)

- Superficies/tinta: `--bg` (blanco puro), `--surface`, `--surface-sunk`, `--ink`, `--ink-soft`, `--muted`, `--line`, `--line-strong`.
- Marca: `--primary` magenta (`oklch(0.58 0.19 330)`) + `--primary-fill/-strong/-wash`. Acento indigo: `--accent*`.
- Estado: `--success*`, `--warning*`, `--danger*` (cada uno con `-strong` y `-wash`).
- Tipografía: `--font-display` (Space Grotesk), `--font-body` (Inter), `--font-mono` (JetBrains Mono). Tamaños `--fs-*` (escala fija, no fluida).
- Espaciado `--sp-xs…-3xl`, radios `--r-*`, sombras `--shadow-hover/-overlay`, motion `--ease`/`--dur*`, z-index semántico `--z-*`.

**Regla de color:** magenta solo en lo **activo / primario / "vivo"**; los neutros cargan la densidad.

## Primitivos compartidos (`components.css`)

**Layout:** `.page` (y `.page.wide`), `.stack` (grid + gap), `.row` (2 col), `.actions` / `.compact-actions`, `.card`, `.card-head` (cabecera flex título+acción), `.muted`, `.mono`, `.empty-panel` (empty state que enseña: icono + copy en panel punteado).

**Botones:** `button` (primario magenta), `.secondary-button`, `.ghost-button`, `.row-link` (botón-texto inline, p. ej. nombre en una tabla), `.btn-icon` (icono + texto alineados).

**Formularios:** `label` + `input/select/textarea` (foco magenta), `input[type=file]` con `::file-selector-button` de marca, `fieldset`/`legend`. Agrupación: `.form-section` (+ `.form-section__title/__hint`), `.create-head` (cabecera de página con chip a la derecha), `.create-actions` (barra add/submit). Editores numerados (pregunta de Trivia / etapa de BDT): `.question-card` + `.question-card-header`, `.q-title` + `.q-badge` (chip numerado), `.q-meta` (strip de 3 campos).

**Feedback:** `.notice` (+ `.error` / `.success` / `.info`), `.badge` (chip neutro), `.pill` (state pill: punto + texto) con variantes `--live` (magenta que late), `--lobby` (indigo), `--done` (gris), `--ok`, `--warn`, `--cancel`. `.stage-status` (hint del decode de QR en BDT).

**Tablas:** `.table-wrap` (scroll + borde) + `table`/`th`/`td` (header sticky, hover de fila).

**Modal / detalle:** `.modal-backdrop`, `.modal-card`, `.modal-header`, `.detail-grid` (dt/dd en rejilla, `.detail-wide` ocupa toda la fila).

## Convenciones aprendidas

- **State pills:** mapear `estado` → `{cls,label}` con un helper local (`pill--live` para "Iniciada", `pill--lobby` para "Lobby", `pill--done` para el resto). El `label` se mantiene = texto de estado para no romper aserciones de tests.
- **Iconos:** SVG inline en `frontend/src/shell/icons.tsx` (stroke 2px, `currentColor`, `aria-hidden`). Reusar ese set; no introducir otra librería.
- **Empty states que enseñan:** `.empty-panel` con icono + qué es + a dónde ir (no solo "no hay nada").
- **Carga:** skeleton (`.ops-skel` / shimmer) o icono que gira (`.ops-spin`), no spinners sueltos.
- **Bans respetados:** sin side-stripes decorativos, sin gradient-text, sin glassmorphism por defecto, sin eyebrows tracked en cada sección (se eliminó un `.eyebrow` suelto).
- **Reduced motion:** toda animación tiene alternativa en `@media (prefers-reduced-motion: reduce)`.

## Verificación

```powershell
cd frontend
npx tsc --noEmit      # tipos
npx vitest run        # 52 tests (componentes + routing + auth)
npx vite build        # bundle de producción
```

Pendiente transversal de Fase 1: **pase visual en navegador con datos reales** (los gates de auth
impiden screenshot automático; requiere Keycloak + servicios `dotnet run` con datos).

---

# Sistema de diseño implementado (mobile · React Native)

Espejo en RN del sistema web. `impeccable` **no corre en RN**: la marca se materializa con un
**theme TS** (hex de `DESIGN.md`, porque RN no entiende `oklch()`) y un set de **primitivos**.
La verificación visual es **manual con Expo**. Roadmap y avance: `mobile-redesign-plan.md`.

> **Regla de oro (igual que en web):** reconstrucción **visual + IA, no funcional**. No se tocan
> contratos, reglas, `testID`/`accessibilityLabel` ni textos/comportamiento que verifican los tests
> (`mobile/tests/*.test.js`, `node --test`). Cada fase cierra con `tsc --noEmit` + `npm test` + pase Expo.

## Mapa de archivos (`mobile/src/shared/`)

| Archivo | Contenido |
|---|---|
| `theme.ts` | **Tokens** en hex (espejo de `DESIGN.md`): `colors` (superficies/tinta/marca/acento/estado/semántico), `spacing`, `radius`, `fonts` (familias `@expo-google-fonts`) y `typography` (presets display/headline/title/body/bodyStrong/label/mono). |
| `fonts.ts` | `useAppFonts()` — carga Space Grotesk + Inter + JetBrains Mono (`expo-font`). El gate vive en `App.tsx` (Splash mientras cargan). |
| `ui/` | **Primitivos** + barrel `index.ts`. La API de componentes que reutilizan todas las pantallas. |
| `statusPill.ts` | `gameStatePill(estado)` — mapea el `estado` del backend a `{state,label}` de `StatePill` (Trivia y BDT). |
| `controllerStyles.ts` | `cs.*` — fragmentos de estilo de marca para pantallas **controller-driven** (BDT, equipos Leave/Transfer): re-skin por **valores** del objeto `styles` inyectado, sin tocar el controller testeado. |

## Primitivos (`ui/`)

- **`AppText`** (`variant`: display/headline/title/body/bodyStrong/label/mono + `color`): único punto para usar las fuentes correctas.
- **`Button`** (`variant`: primary/secondary/ghost/danger, `loading`, `disabled`): altura táctil ≥48px, pressed oscurece, `accessibilityRole="button"`.
- **`Card`** (`sunk?`): superficie + borde 1px, **plano por reposo** (sin sombras decorativas).
- **`StatePill`** (`state`: live/lobby/done/ok/warn/cancel): punto + etiqueta (**estado nunca solo color**). `live` **late** con halo `Animated`; alternativa estática vía `AccessibilityInfo.isReduceMotionEnabled` (reduce-motion).
- **`Field`** (`label`, `error`, `hint`): input con foco magenta, error en texto bajo el campo, ≥48px.
- **`Notice`** (`variant`: error/success/info): bloque con lavado + borde + texto.
- **`EmptyPanel`** (`title`, `message`, `action?`, `icon?`): empty state que **enseña** (panel punteado).
- **`Mono`** (`chip?`): identificadores/códigos/QR en JetBrains Mono (regla Mono For Machine Strings).
- **`ScreenHeader`** (`title`, `subtitle?`, `right?`): cabecera display, **sin eyebrow** (No-Eyebrow).
- **`DetailRow`** (`label`, `value`, `onStage?`): fila etiqueta→valor para paneles de detalle (con
  `onStage` usa texto claro sobre superficies de color).

## Primitivos del **registro de juego** (v2 inmersivo, `ui/` + `shared/`)

Capa de uso más intensa para el participante (misma paleta/fuentes). Tokens en `theme.ts > game`
(superficies *stage*, `onStage*`, `gradient` mismo-hue, `glow`, `motion`). **Toda animación tiene
alternativa estática** (`shared/useReducedMotion.ts`); color/forma siguen comunicando sin movimiento.

- **`Stage`** (`variant`: magenta/indigo/ink/plain, `gradient?`, `scroll?`): lienzo inmersivo a sangre
  completa con safe area (`react-native-safe-area-context`) y status bar adaptada.
- **`Hero`** / **`BigNumber`**: cabecera dramática (Space Grotesk 700) / número protagonista (XL).
- **`Panel`**: tarjeta translúcida "glass" para contenido sobre un `Stage` de color (equivalente a `Card`).
- **`Icon`** (Feather, `@expo/vector-icons`): iconografía de línea coherente con la web.
- **`PressableScale`**: envoltorio táctil con press-scale (spring `Animated`), reduce-motion aware.
- **`Countdown`** (`seconds`, `warnAt`/`dangerAt`): timer gigante **presentacional** (recibe los
  segundos; no posee reloj) normal→ámbar→rojo + pulso de urgencia.
- **`Reaction`** (`correct`): veredicto ✓/✕ verde/rojo con pop sobrio (sin confeti).
- **`Podium`** (`entries: PodiumEntry[]`): top-3 por **altura** + "Tú" resaltado + deltas. **Agnóstico
  del criterio**: muestra `valor` ya formateado (Trivia: "300 pts"; BDT: "3 etapas · 4:12") — **no asume
  puntaje**, por eso sirve también al ranking BDT (etapas/tiempo).
- **`shared/useCountUp.ts`**: anima un entero 0→target (count-up), reduce-motion salta al valor.

**AA en superficies de color:** texto blanco/`onStage` solo sobre `primary-fill #982f93` / `accent
#3e5fad` o más oscuro; los gradientes nunca arrancan más claros que esa base. `onStageMuted` es blanco
al **80%** (no 74%) para que el texto atenuado pequeño pase AA aun sobre la superficie más clara permitida.

**Maquetas (datos mock + plantilla de integración)**, para ver/probar el juego sin backend en vivo:
`features/trivia/live/` (interfaz `LiveTriviaSource` + mock + `TriviaLivePlayScreen`: pregunta→Countdown→
Reaction→Score count-up→Podium) y `features/bdt/ranking/` (`BdtRankingSource` + mock + Podium por
etapas/tiempo). Cada carpeta documenta cómo cablear el backend (cambiar **una** fuente; la pantalla no
cambia). Accesos "(demo)" auto-removibles en estados vacíos/lobby.

## Convenciones aprendidas (mobile)

- **Patrones de pantalla:** **presentacionales** con lógica inline o vía **hook** (Login, Home,
  Create/Join, las 5 de Trivia, las 3 de BDT) → usan los **primitivos** directamente. La lógica de las
  pantallas BDT vive en **hooks `.js` testeables** (`useBdtActiveStage`/`useBdtTreasureUpload`/
  `useBdtPublishedGames`) que reemplazaron a los antiguos `Controller.js`: la orquestación se testea por
  el **valor de retorno del hook**, no por el texto renderizado. Equipos **Leave/Transfer** siguen
  controller-driven (re-skin por valores de `styles` vía `cs.*`, sin tocar el controller testeado).
- **Estado:** `gameStatePill` conserva el texto del backend como etiqueta para no romper aserciones.
- **One Live Voice:** magenta solo en acción primaria / foco / estado "En vivo"; rol y metadatos en chip neutro.
- **QR en mobile:** el decode es **autoritativo en backend**; el teléfono solo sube la foto → estados como error/éxito.
- **Táctil:** botones y chips ≥44–48px. **Header** de `native-stack` en blanco, plano (sin sombra).
- **Roles:** `auth/tokenClaims.js > buildAuthUser` filtra `realm_access.roles` a **solo roles de app**
  (Administrador/Operador/Participante), descartando los técnicos de Keycloak (espejo de OBS-04 web).
  El chip de rol del Home queda limpio (antes mostraba `default-roles-umbral-ucab`, etc.).

## Verificación

```bash
cd mobile
npx tsc --noEmit              # tipos
node --test tests/*.test.js   # 81 tests (lógica/flujos/hooks)
npm start                     # pase visual manual en Expo
```

