# Plan de reconstrucción de frontend — Mobile (Fase 2)

Registro **persistente** del rediseño visual de la app móvil de UMBRAL (React Native + Expo).
Es la fuente de verdad del avance de la **Fase 2** del rediseño global; el roadmap macro vive en
`frontend-redesign-plan.md` (mismo directorio), que referencia este archivo.

> **Naturaleza del trabajo:** reconstrucción **visual + arquitectura de información (IA)**, *no*
> funcional. No cambia contratos (`contracts/`), reglas de negocio ni HUs; respeta el routing
> mobile (solo `Participante` / `Líder de equipo`) y la autoridad del backend (ver `AGENTS.md`).
> No se tocan `testID`, textos ni comportamiento que verifican los tests (`tests/*.test.js`).

Artefactos de marca (fuente de verdad visual): `PRODUCT.md`, `DESIGN.md` (raíz; trae **hex** porque
RN no entiende `oklch()`). `impeccable` **no corre en RN**: se reusan los tokens vía un **theme TS**
y la verificación visual es **manual con Expo**.

## Decisiones de arranque (2026-06-14)

| Decisión | Elección | Implicación |
|---|---|---|
| Tipografía | **Fuentes custom** (Space Grotesk / Inter / JetBrains Mono) | `@expo-google-fonts/*` + `expo-font`; gate de carga de fuentes en el arranque. Máxima fidelidad con la web. |
| Navegación | **Mantener stack + Home hub** | Re-skin de la `Home` como hub; sin bottom-tabs. Menor cambio de IA, menos riesgo en tests. |
| Estructura de fases | **Por flujo vertical** | Tras la fundación, cada fase re-skinnea un flujo completo y deja algo demostrable en el teléfono. |

## Estado actual (punto de partida)

- **Patrón:** cada pantalla es `Container` (lógica) + `Screen` (presentación). Tests de lógica/flujo
  en `tests/*.test.js` (`node --test`), no visuales. Algunos verifican contenido (p. ej.
  `leaveTeamScreenContent.test.js`) → preservar textos.
- **Theme actual = azul/teal** (`#174a7c` primario, `#1ba6a6` acento) — la **anti-referencia #1**.
  Centralizado en `shared/theme.ts` (colores/espaciado/radios) + `shared/styles.ts` (`screenStyles`).
- **Pantallas:** `Splash`, `Login`, `Home` (hub) + Teams (4: Create/Join/Transfer/Leave) +
  Trivia (5: GamesList/Lobby/Answer/Result/Score) + BDT (3: PublishedGames/ActiveStage/TreasureUpload).

## Verificación (regla por fase)

Toda fase se cierra con **las tres**:
1. `npx tsc --noEmit` (mobile/) — limpio.
2. `npm test` (`node --test tests/*.test.js`) — verde (sin tocar comportamiento ni testIDs).
3. **Pase manual en Expo** — el chequeo descrito en cada fase, en dispositivo/emulador:
   toques ≥44px, contraste legible a pleno sol, `prefers-reduced-motion` con alternativa, safe areas.

---

## Fases

### Fase 0 — Fundación compartida (theme + fuentes + primitivos) · ✅ código (pase Expo pendiente)
Sin esto, ninguna pantalla puede re-skinnearse coherentemente.

**Hecho (2026-06-14):**
- `shared/theme.ts` reescrito: paleta magenta (hex de `DESIGN.md`), claves legacy conservadas como
  **alias** (las pantallas actuales ya adoptan la paleta sin romperse), radios alineados a `DESIGN.md`,
  + objetos `fonts` y `typography`.
- Fuentes custom instaladas (`expo-font` + `@expo-google-fonts/{space-grotesk,inter,jetbrains-mono}`);
  hook `shared/fonts.ts > useAppFonts()` y **gate en `App.tsx`** (Splash mientras cargan).
- Primitivos en `shared/ui/` (+ barrel `index.ts`): `AppText`, `Button` (primary/secondary/ghost/danger),
  `Card`, `StatePill` (live/lobby/done/ok/warn/cancel), `Field`, `Notice`, `EmptyPanel`, `Mono`, `ScreenHeader`.
- **Kitchen sink** `screens/DesignPreviewScreen.tsx` (ruta `DesignPreview`, solo `__DEV__`, entrada en Home)
  para el pase visual: muestra tipografía, botones, chips, formulario, avisos, mono y empty state.
- Verificado: `tsc --noEmit` exit 0 · `npm test` 83/83 verdes.
- **Pendiente (usuario):** pase manual en Expo — abrir app → "Design preview (dev)" desde Home; confirmar
  que cargan las fuentes (Space Grotesk/Inter/JetBrains Mono), la paleta es magenta y los toques ≥44px.

Detalle original de la fase:
- **Theme:** reescribir `shared/theme.ts` mapeando los **hex de `DESIGN.md`** (blue→magenta).
  Tokens semánticos: superficies (`bg`/`surface`/`surface-sunk`), tinta (`ink`/`ink-soft`/`muted`),
  líneas, marca (`primary`/`primary-fill`/`primary-strong`/`primary-wash`), acento indigo,
  estado (`state-live`/`state-lobby`/`state-done`, `success`/`warning`/`danger` + washes).
- **Tipografía:** objeto `typography` con la escala (display/headline/title/body/label/mono:
  familia + tamaño + peso + lineHeight + letterSpacing). Cargar fuentes con `expo-font` +
  `@expo-google-fonts/{space-grotesk,inter,jetbrains-mono}`; gate en el arranque (Splash mientras carga).
- **Primitivos** (espejo de `components.css` web) en `shared/components/ui/`: `Button`
  (primary/secondary/ghost/danger), `Card`, `StatePill` (punto + etiqueta; variantes
  live/lobby/done/ok/warn/cancel — **estado nunca solo color**), `Field`/`Input`, `Notice`
  (error/success/info), `EmptyPanel` (icono + qué es + a dónde ir), `Mono` (IDs/códigos), `ScreenHeader`.
- **Comprobable manualmente:** la app arranca, las fuentes cargan, la `Home` ya se ve con la
  paleta magenta (aunque sin re-skin fino). Pantalla "kitchen sink" temporal opcional para ver
  todos los primitivos juntos.

### Fase 1 — Arranque + Hub (Splash · Login · Home) · ✅ código (pase Expo pendiente)
Primera impresión de marca.

**Hecho (2026-06-14):**
- `SplashScreen`: wordmark UMBRAL + spinner magenta; legible sin las fuentes de marca (corre durante
  el gate de fuentes y la restauración de sesión).
- `LoginScreen`: identidad UMBRAL (wordmark display, **sin kicker en mayúsculas**), card de bienvenida,
  `Button` primario con estado `loading`, error como `Notice`. Comportamiento de `login()` intacto.
- `HomeScreen`: **fuera el heroCard azul y el kicker**; hub con `ScreenHeader` + cards Equipo/Partidas
  usando `Button`/`Card`. **One Live Voice**: un primario por sección (Crear / Buscar / Jugar),
  apoyo en `secondary`, Salir en `danger`, sesión/preview en `ghost`. Rol en chip **neutro** (no magenta:
  es metadato, no estado "vivo"). Navegación y `logout` intactos; preview dev sigue gated por `__DEV__`.
- Shell: header de `native-stack` pasado a **blanco** (tinta ink, chevron magenta, título Space Grotesk,
  plano sin sombra) — coherente con "superficie blanca que respira".
- Verificado: `tsc --noEmit` exit 0 · `npm test` 83/83 verdes.
- **Pendiente (usuario):** pase manual en Expo — login → hub; confirmar wordmark con Space Grotesk,
  jerarquía sin eyebrow, header blanco y navegación a cada sección.

### Fase 2 — Equipos (Create · Join · Transfer · Leave) · ✅ código (pase Expo pendiente)

**Hecho (2026-06-14):**
- `CreateTeamScreen` / `JoinTeamScreen`: **reescritas** con primitivos (`ScreenHeader` + `Card` + `Field`
  + `Button` + `Notice`). La pantalla `JoinTeamScreen` conservaba copy legacy de unión por código durante
  el re-skin visual; eso es deuda de migración de código/copy, no doctrina activa. La doctrina actual exige
  unión por `InvitacionEquipo` en Identity.
- `LeaveTeamScreen`: delega el render a un **Controller `.js` testeado** vía `components` + `styles`.
  Re-skin **solo de los valores del objeto `styles`** (mismas claves, mismo controller, mismos
  componentes inyectados) → cero riesgo para los tests. Avisos como bloque error/éxito on-brand.
- `TransferLeadershipScreen`: **migrada a TSX + primitivos** (`ScreenHeader` + `Card` + `Notice` +
  `Button` + `Pressable`/`Modal`) el 2026-07-16, reemplazando el input de userId en mono por una lista
  de miembros elegibles y un modal de confirmación. Decisión registrada en
  `docs/superpowers/specs/2026-07-16-transferir-liderazgo-lista-mobile-design.md` (D3); su lógica vive
  en `transferLeadershipScreenModel.js`/`transferLeadershipFlow.js`, cubiertos por
  `transferLeadershipScreenModel.test.js`/`transferLeadershipFlow.test.js`.
- Verificado: `tsc --noEmit` exit 0 · `npm test` 83/83 verdes (incluye `LeaveTeamScreenController`,
  `leaveTeamScreenContent`, `joinTeamScreenModel`).
- **Pendiente (usuario):** pase manual en Expo — recorrer crear → invitaciones/unión legacy pendiente de
  migrar, transferir → salir; confirmar campos/botones de marca y los estados sin-equipo / error / éxito.

**Nota de patrón:** Leave/Delete/TeamHistory mantienen la arquitectura Controller-inyectado por
**testabilidad**; no se migran a los primitivos (API distinta). Se re-skinean por el objeto `styles`.
`TransferLeadershipScreen` es la excepción aprobada (ver D3 arriba): migró a primitivos directos. Las
pantallas sin controller (Create/Join y las de Trivia/BDT que lo permitan) también usan los primitivos
directamente.

### Fase 3 — Trivia (GamesList · Lobby · Answer · Result · Score) · ✅ código (pase Expo pendiente)

**Hecho (2026-06-14):**
- Las 5 pantallas (presentacionales con lógica inline; los tests de Trivia son de flujo, no las
  renderizan) **reescritas** con primitivos. Lógica de API/estado y copy intactos.
- `GamesList`: `FlatList` con cards de marca + **`StatePill`** por `estado` (helper compartido
  `shared/statusPill.ts`), filtros como chips de marca, empty state con `EmptyPanel`, error con `Notice`.
- `Lobby`: cabecera display, `Card` con `StatePill` + `DetailRow`, botones primario/secundario/ghost,
  mensajes con `Notice`.
- `Answer`: `Field` para ID de pregunta (**mono**) y opción; resultado con `StatePill` (ok/registrada).
- `Result`: `StatePill` correcta/incorrecta + `DetailRow`. `Score`: número grande en **display** + `DetailRow`.
- **Punto pulsante "En vivo":** implementado en `StatePill` (halo `Animated`) para el estado `live`,
  con **alternativa estática** vía `AccessibilityInfo.isReduceMotionEnabled` (regla reduce-motion).
- Nuevos primitivos extraídos: `DetailRow` (label→valor) y helper `gameStatePill` (reutilizables en BDT).
- Verificado: `tsc --noEmit` exit 0 · `npm test` 83/83 verdes.
- **Pendiente (usuario):** pase manual en Expo — lista (chips/estados/pull-to-refresh) → lobby (punto
  que late) → responder → resultado → puntaje; confirmar mono en UUIDs y toques ≥44px.

### Fase 4 — BDT (PublishedGames · ActiveStage · TreasureUpload) · ✅ código (pase Expo pendiente)

**Hecho (2026-06-14):**
- Las 3 pantallas son **controller-driven** (como equipos Leave/Transfer): inyectan `components` +
  `styles` al controller `.js` testeado. Re-skin **solo de valores de `styles`** (claves, controller y
  componentes inyectados intactos) → cero riesgo para los tests de controllers/flows/permisos.
- Nuevo módulo `shared/controllerStyles.ts` (`cs.*`): **fragmentos de marca** (title/card/error/success/
  filters/primaryButton/secondaryButton…) como fuente única para mapear las claves legacy a tokens.
  Reusado por las 3 pantallas BDT.
- **QR en mobile:** el decode es **autoritativo en backend**; en el teléfono el participante solo sube
  la foto. Los estados "legible/no-legible" se reflejan como **error/éxito** de la subida (bloque
  `cs.error`/`cs.success` on-brand). El botón del image picker pasa a `secondaryButton` de marca.
- `ActiveStage`: cuenta regresiva, hint de geolocalización obligatoria y estado de etapa con estilos de
  marca. El ranking BDT vive en backend/Puntuaciones y se ordena por puntos acumulados de etapas ganadas,
  con desempate por menor tiempo de esas etapas; aquí solo se muestran datos de etapa.
- Verificado: `tsc --noEmit` exit 0 · `npm test` 83/83 verdes (incluye los 3 controllers BDT, flows,
  `bdtGeolocationPermission`, `bdtTreasureImagePicker`).
- **Pendiente (usuario):** pase manual en Expo — lista BDT → etapa activa (permiso de ubicación,
  cuenta regresiva) → subir tesoro (seleccionar foto, error/éxito de subida).

**Estado tras Fase 4:** las **11 pantallas** están re-skinneadas. `shared/styles.ts` (`screenStyles`) y
`shared/components/ScreenWrapper.tsx` quedaron **sin uso** (solo se referencian entre sí) → se eliminan
en la Fase 5 junto con los alias de color legacy que ya no consume nadie.

### Fase 5 — Pase final, pulido y documentación · ✅ código (pase Expo pendiente)

**Hecho (2026-06-14):**
- **Limpieza de legacy:** eliminados `shared/styles.ts` (`screenStyles`) y `shared/components/ScreenWrapper.tsx`
  (quedaron sin uso tras migrar las 11 pantallas). `theme.ts` purgado de **alias de color legacy**
  (solo quedan tokens canónicos + `white`/`primaryDisabled`, ambos en uso). `tsc` confirma cero referencias rotas.
- **Pulido táctil:** chips de filtro (Trivia y BDT) a **≥44px** de alto. Botones/inputs ya ≥48px.
  Reduce-motion cubierto por `StatePill`; safe areas vía `SafeAreaView`; header plano en blanco.
- **Documentación:** sección **mobile** añadida a `design-system.md` (mapa de archivos + API de primitivos
  + convenciones). `frontend-redesign-plan.md` marca **Fase 2 = código completo**.
- **Dev tool:** `DesignPreviewScreen` y su ruta **eliminados** (botón en Home + ruta + tipo + archivo)
  tras feedback del primer pase Expo (se veía "sin terminar").
- Verificado: `tsc --noEmit` exit 0 · `npm test` 83/83 verdes.

**Ajustes post-pase Expo (2026-06-14):**
- **Bundle no actualizaba en el teléfono:** el script `npm start` hardcodeaba una IP de packager obsoleta
  (`192.168.125.236`) que pisaba la de `.env` (`192.168.1.115` = IP real) → el teléfono no alcanzaba Metro
  y Expo Go servía el bundle viejo. Fix: quitada la IP del script (`expo start --clear --host lan` deja
  autodetectar / respeta `.env`). El bundle se validó con `expo export` (OK).
- **Roles técnicos en el chip de Home** (`default-roles-umbral-ucab`…): `auth/tokenClaims.js` ahora filtra
  a solo roles de app. Esto además colapsa el header (el texto largo de roles inflaba el espacio bajo el título).
- Verificado tras ajustes: `tsc --noEmit` exit 0 · `npm test` 83/83 verdes.

**Pendiente (usuario):** continuar el barrido manual final de las 11 pantallas en Expo (táctil, contraste a
pleno sol, reduce-motion, safe areas), análogo al pase pendiente de la web.

---

## Para un agente nuevo: cómo continuar
Lee, en orden: este archivo → `frontend-redesign-plan.md` + `design-system.md` (mismo dir) →
`DESIGN.md` + `PRODUCT.md` (raíz, marca y tokens **hex**) → `CLAUDE.md`/`AGENTS.md` (mobile = solo
`Participante`/`Líder`, autoridad del backend) → `GUIA-LEVANTAMIENTO.md` (levantar Expo + servicios).

**Regla de oro:** rediseño **visual + IA, no funcional**. No tocar contratos, reglas, `testID`, textos
ni comportamiento que usan los tests. Cerrar cada fase con `tsc --noEmit` + `npm test` + pase manual Expo.
