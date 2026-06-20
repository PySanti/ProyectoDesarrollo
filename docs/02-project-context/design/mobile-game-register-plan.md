# Plan — Registro de juego para Mobile (v2 inmersivo)

Evolución del mobile **sobre el v1 ya enviado** (ver `mobile-redesign-plan.md`): mantiene **la misma
paleta y fuentes** de la web, pero cambia el **registro** de "administrativo/ejecutivo" a
**competitivo/inmersivo de juego**. No es un rediseño de marca; es una **capa de uso** (cómo se aplican
los mismos tokens) más intensa para el participante.

Decisiones del usuario (2026-06-14): **Intensidad = Inmersivo total** · **Palancas = las cuatro**
(motion, tipografía protagonista, color inmersivo, iconografía) · **Momentos estelares = los cuatro**
(podio, cuenta regresiva dramática, lobby llenándose, puntaje con reacción).

> **Naturaleza del trabajo:** sigue siendo **visual + IA, no funcional**. No cambia contratos
> (`contracts/`), reglas de negocio, HUs, ni `testID`/`accessibilityLabel`/textos que verifican los
> tests. Mantiene autoridad del backend y routing mobile (solo Participante/Líder).

## Guardrail innegociable (qué NO es)

`PRODUCT.md`/`DESIGN.md` **rechazan lo infantil/gamificado**: prohibido confeti, mascotas, estética de
casino, emojis decorativos, brillos neón o **colores nuevos**. "Inmersivo" = energía por **color de marca
+ tipografía grande + movimiento con propósito**, no por adornos. Reglas que se mantienen:
- **Misma paleta y fuentes.** Cero hues nuevos. Magenta = "vivo/ahora"; indigo = calma/espera.
- **Estado = color + texto + forma** (nunca solo color), también sobre fondos de color.
- **WCAG AA** en cada par: texto blanco solo sobre `primary-fill #982f93`/indigo o más oscuros (no sobre
  `primaryBright`). Verificar contraste en superficies de color.
- **`prefers-reduced-motion`** con alternativa estática para **toda** animación nueva.
- Táctil ≥44–48px. Safe areas en pantallas a sangre completa.

## Lo que ya existe (se reutiliza, no se tira)

Theme TS + primitivos `shared/ui/` (Button, Card, StatePill con pulso, Field, Notice, EmptyPanel, Mono,
ScreenHeader, DetailRow), helpers `statusPill`/`controllerStyles`, gate de fuentes. El registro de juego
**extiende** esto con tokens y primitivos nuevos; las pantallas migran de "blanco restrained" a
"stage inmersivo" reutilizando la lógica intacta.

## Stack propuesto (se confirma/verifica en G0)

| Necesidad | Propuesta | Nota |
|---|---|---|
| Motion (press, transiciones, count-up, fades) | **`Animated` nativo de RN** (sin dep) | Reanimated 4 se descartó: su `react-native-worklets` crashea en el Expo Go del usuario (`installTurboModule` ABI mismatch). `Animated` no tiene módulo nativo extra y ya estaba probado (pulso v1). |
| Heros/degradados magenta→magenta | **`expo-linear-gradient`** (`expo install`) | Degradado del mismo hue; no introduce color nuevo. |
| Iconografía de línea | **`@expo/vector-icons`** (Feather) | Ya viene con Expo (sin dep extra); stroke de línea coherente con los SVG de la web. Alternativa: set propio en `react-native-svg`. |

**Riesgo a despejar en G0:** que Expo (Go/dev build) arranque con Reanimated + babel; se verifica antes de seguir.

## Arquitectura: la traba de las pantallas controller-driven

Las 3 de **BDT** y las de equipos **Leave/Transfer** renderizan su JSX en un `Controller.js` **testeado**
(recibe `components`+`styles`). El re-skin v1 se hizo por valores de `styles`, pero un **layout inmersivo
a sangre completa no cabe** en esa estructura sin tocar el controller. Decisión del plan (G4):
- La **lógica** vive en módulos `*Model.js`/`*Flow.js` **ya testeados aparte**. Se migran esas pantallas a
  **componentes presentacionales** (como las de Trivia) que consumen esos módulos, y se retira el JSX del
  controller. Los tests de comportamiento (model/flow/permittos) se conservan; los tests que verifican la
  **estructura** del controller se reescriben/retiran (cobertura de comportamiento se mantiene a nivel model).
- Equipos Leave/Transfer: registro menos "arcade" (no son partida); se les da energía moderada sin migrar,
  salvo que se decida lo contrario.

---

## Fases (cada una testeable: `tsc` + `npm test` + pase manual Expo)

### Fase G0 — Fundación del registro de juego · ✅ código (pase Expo pendiente)

**Hecho (2026-06-14):**
- **Tokens de juego** en `theme.ts > game` (sin colores nuevos): superficies *stage* (magenta/indigo/ink),
  texto `onStage*` (blanco/translúcidos), `gradient` (magenta→magenta y demás del mismo hue), `glow` (sombra
  magenta), `motion` (duraciones + springs). Tipografía **XL**: `hero` (40) y `mega` (64) en Space Grotesk 700.
- **Deps:** `expo-linear-gradient@15` y `@expo/vector-icons@15` (Feather). **Reanimated probado y
  descartado** (ver abajo): motion con `Animated` nativo.
- **Primitivos base** (`shared/ui/`): `Stage` (fondo color/gradiente a sangre + safe area), `Icon` (Feather),
  `PressableScale` (press con spring `Animated`), `BigNumber` (número gigante), `Hero` (cabecera dramática), y
  **upgrade de `Button`** (press-scale + `icon` + variante `onStage`), API anterior intacta. Hook compartido
  `shared/useReducedMotion.ts` (AccessibilityInfo) para la alternativa estática.
- **Entrega comprobable:** `SplashScreen` reescrito como `Stage` magenta con **gradiente** y **entrada animada**
  del wordmark (`Animated`, reduce-motion aware) → al abrir la app se valida la cadena nueva.
- **Incidencia y giro de stack (importante):** primero se intentó **Reanimated 4**; el bundle compilaba pero
  en el **Expo Go del usuario crasheaba** (`Exception in HostFunction: TurboModule "installTurboModule"` en
  `NativeWorklets` = mismatch ABI entre `react-native-worklets` JS y el nativo del Expo Go). Se **quitaron
  reanimated + worklets**, se revirtió el plugin de babel y el motion se reimplementó con `Animated` nativo
  (sin módulo nativo extra; ya probado en el pulso v1). Lección: en Expo Go, preferir `Animated` salvo dev build propio.
  - (Aparte se resolvió que `babel.config.js` no sirve aquí por `"type":"module"`; el config real es `babel.config.cjs`.)
- Verificado: `tsc --noEmit` exit 0 · `npm test` 83/83 · **`expo export` (bundle Metro real) OK** sin reanimated (2.74 MB).
- **Pendiente (usuario):** reiniciar Metro **con caché limpia** (el caché de babel tenía el plugin de worklets)
  y confirmar el Splash magenta animado (y estático en reduce-motion).

### Fase G1 — Home + navegación (energía de entrada) · ✅ código (pase Expo pendiente)

**Hecho (2026-06-14):**
- `HomeScreen` reescrito como **hub inmersivo**: `Stage` magenta con **gradiente** (continuo con el Splash),
  `Hero` (título grande + rol en chip translúcido), y **cards de navegación con ícono** (`NavCard` local) sobre
  paneles translúcidos (`onStageSunk`/`onStageLine`), con `PressableScale` (press con spring, reduce-motion aware).
  Dos cards *feature* (Jugar Trivia / Buscar tesoro) + acciones de equipo + "Cerrar sesión" ghost. Navegación
  y `logout` intactos. **Roles ya filtrados** (chip limpio).
- **Navegación:** header nativo **oculto en Home** (full-bleed inmersivo); transición de stack
  `slide_from_right`. `Stage` ahora fija la **status bar** clara en variantes de color (oscura en `plain`).
- Verificado: `tsc --noEmit` exit 0 · `npm test` 83/83 · `expo export` (bundle Metro) OK.
- **Pendiente (usuario):** abrir la app → ver el hub magenta encendido, cards con ícono que "hunden" al tocar,
  y la transición al navegar a Trivia/BDT/Equipo.

### Fase G2 — Trivia inmersivo + momentos estelares · ✅ código (pase Expo pendiente)

**Hecho (2026-06-14):**
- **Primitivos nuevos** (`shared/ui/`): `Countdown` (timer gigante presentacional — recibe los segundos,
  no posee reloj; normal→ámbar→rojo + pulso de urgencia; reduce-motion estático con color que igual
  comunica), `Reaction` (veredicto correcto/incorrecto: disco con check/✕ verde/rojo + pop sobrio, sin
  confeti), `Panel` (tarjeta translúcida "glass" para contenido sobre `Stage` de color). Hook
  `shared/useCountUp.ts` (count-up de enteros, reduce-motion salta al valor). `DetailRow` ahora acepta
  `onStage` (texto claro sobre color).
- **Pantallas reskineadas** (lógica/handlers/contratos intactos; sólo JSX/estilos):
  - `Score`: stage **magenta** + **count-up** del puntaje (número 88px) + detalles en `Panel`.
  - `Result`: stage **ink** dramático + **`Reaction`** correcto/incorrecto + detalles en `Panel`.
  - `Lobby`: stage **indigo** (calma/espera) + **count-up de participantes** ("llenándose") + pill de
    estado (late "En vivo") en el `Hero` + botones `onStage`.
  - `Answer`: stage **magenta** inmersivo + `Hero` "Responde"; al enviar, **`Reaction`** del resultado
    (acierto/registro). Form en `Card` claro (inputs legibles). **Countdown NO cableado aquí** (decisión
    del usuario: no inventar un timer falso en pantalla placeholder; se demuestra de verdad en G4).
  - `GamesList`: stage **ink** (catálogo oscuro), cards translúcidas con **ícono por modalidad**
    (user/users) que **se hunden** al tocar (`PressableScale`), **pills vivas** y chips de filtro
    translúcidos. Estados loading/error/empty también sobre stage.
- `Stage` ahora fija `keyboardShouldPersistTaps="handled"` (forms) y acepta `contentStyle` para alojar
  un `FlatList` sin padding doble.
- Verificado: `tsc --noEmit` exit 0 · `npm test` 83/83 · `expo export` (bundle Metro) OK (2.75 MB).
- **Maqueta de partida en vivo (giro pedido por el usuario):** como hoy **no se puede jugar Trivia**
  (no existe la ejecución sincronizada en backend), G2 incluye una **maqueta navegable con datos mock**
  que además es **plantilla de integración**. Estructura (`src/features/trivia/live/`):
  - `liveTriviaTypes.ts` — interfaz **`LiveTriviaSource`** + formas de datos = **el contrato** que el
    backend deberá cumplir. Documenta pieza por pieza qué ya existe (`answerTriviaQuestion`/
    `getTriviaQuestionResult`/`getTriviaScore`) y qué **falta** (push SignalR de la pregunta activa).
  - `mockLiveTriviaSource.ts` — implementación mock (guion de 3 preguntas) que imita las formas reales.
  - `TriviaLivePlayScreen` (+container) — pantalla inmersiva que depende **solo** de `LiveTriviaSource`;
    recorre pregunta → **Countdown** (timer local de la maqueta) → tap → **Reaction** + reveal de
    opciones (verde/rojo) → siguiente → **Score count-up** + ranking teaser. Banner "Maqueta · datos de
    ejemplo" visible.
  - `README.md` — pasos exactos para cablear al backend (crear `BackendLiveTriviaSource`, cambiar **una
    línea** del container, quitar accesos demo). La pantalla **no cambia** al integrar.
  - **Accesos demo (a quitar al integrar):** botón "Jugar partida en vivo (demo)" en el Lobby y "Probar
    partida en vivo (demo)" en los estados vacío/error de la lista — para que sea probable **sin backend**.
- **El Countdown se estrena aquí** (en la maqueta en vivo), no en Answer. Answer sigue siendo el form
  real (HU-26) re-skineado; su reacción usa el resultado real al enviar.
- **Pendiente (usuario):** abrir Trivia → en la lista vacía tocar "Probar partida en vivo (demo)" y
  recorrer pregunta con **cuenta regresiva**, **reacción** y **puntaje con count-up**.

### Fase G3 — Ranking podio (clímax competitivo) · ✅ código (pase Expo pendiente)

**Hecho (2026-06-14):**
- **`Podium`** (`shared/ui/Podium.tsx`): top-3 en pilares de **distinta altura** (2.º · 1.º · 3.º) + el
  resto como lista; resalta al propio participante (**"Tú"** con contorno claro + chip). Soporta `delta`
  de posición opcional (chevron ▲/▼). **Sin colores nuevos**: el rango lo dan altura + número, no
  oro/plata/bronce.
- **Agnóstico del criterio (clave para G4/BDT):** recibe `PodiumEntry.valor` **ya formateado** por quien
  llama (p. ej. `"300 pts"` en Trivia; mañana `"3 etapas · 4:12"` en BDT) — **no asume puntaje**. Así el
  mismo componente sirve para el ranking de BDT (ordenado por `EtapasGanadas` + `TiempoAcumulado`), que
  **no** es por puntaje.
- **Aplicado a Trivia:** el `FinalBlock` de la maqueta en vivo reemplaza el ranking teaser por `<Podium>`
  (mapeando el ranking mock → `PodiumEntry`). El mock ajusta los puntajes rivales (250/150/50) para que
  el puesto refleje tu desempeño (3/3 = 1.º).
- Verificado: `tsc --noEmit` exit 0 · `npm test` 83/83 · `expo export` (bundle Metro) OK.
- **Pendiente (usuario):** terminar la partida demo y ver el **podio** con "Tú" resaltado al puesto que
  corresponda según cuántas acertaste.

### Fase G4 — BDT inmersivo · ✅ código (pase Expo pendiente)

**Hecho (2026-06-14):**
- **Migración controller→presentacional preservando cobertura.** Los 3 controllers BDT tenían tests de
  **comportamiento** (no solo estructura): countdown, gate de permiso, suscripción realtime filtrada,
  mapeo 409, validación de imagen, inscripción en vuelo. En vez de retirarlos, se **extrajo la
  orquestación a hooks `.js` testeables** y se reescribieron los tests como **tests de hook** (mismas
  aserciones, sobre el valor de retorno en vez del texto renderizado):
  - `useBdtActiveStage.js` (+ `useBdtActiveStage.test.js`) ← reemplaza `BdtActiveStageScreenController`.
  - `useBdtTreasureUpload.js` (+ test) ← reemplaza `BdtTreasureUploadScreenController`.
  - `useBdtPublishedGames.js` (+ test) ← reemplaza `BdtPublishedGamesScreenController`.
  - Controllers viejos y sus tests **eliminados**. Lógica de red/validación intacta en `*Model.js`/`*Flow.js`.
- **Pantallas inmersivas** (presentacionales, consumen los hooks):
  - `ActiveStage`: stage **ink** + **`Countdown` real** (¡por fin con datos vivos! deriva de `cierraEnUtc`,
    tictac de 1s, ámbar→rojo), etapa en `Panel`, "Subir tesoro" gateado por permiso+tiempo.
  - `TreasureUpload`: stage **magenta**, foto del QR + **`Reaction`** del resultado (decodificado vs no
    legible; recordando que la validación es **autoritativa del backend**).
  - `PublishedGames`: stage **ink** (catálogo), cards translúcidas con **ícono** (map-pin) y pills, filtros
    translúcidos; estado de **espera** en stage **indigo** con la posición en lobby protagonista.
- Tests: 83 → **81** (consolidación: aserciones de render-only pasaron a aserciones de comportamiento de
  hook; la cobertura de comportamiento se mantiene). `tsc` exit 0 · `expo export` OK.
- **Maqueta de ranking BDT (decisión del usuario: construirla).** Mismo patrón que Trivia en vivo, en
  `src/features/bdt/ranking/`:
  - `bdtRankingTypes.ts` — interfaz **`BdtRankingSource`** + formas + helpers de formato = **plantilla**.
    Documenta la **regla BDT**: orden por `EtapasGanadas` y desempate por `TiempoAcumuladoEtapasGanadas`
    (**no** puntaje); mapeo al evento/endpoint real `RankingBDTActualizado`.
  - `mockBdtRankingSource.ts` — guion que ilustra el **desempate** (Ana y Tú con 3 etapas; Ana gana por
    menos tiempo → 1.ª; Tú 2.º).
  - `BdtRankingScreen` (+container) — aplica el **`Podium`** con `valor = "N etapas · m:ss"`. Banner
    "Maqueta · datos de ejemplo". Acceso: botón **"Ver ranking BDT (demo)"** en `PublishedGames`.
- **Pendiente (usuario):** recorrer BDT → lista oscura, etapa activa con **cuenta regresiva** corriendo,
  subir una foto para ver la **reacción** del QR, y abrir **"Ver ranking BDT (demo)"** para ver el podio
  ordenado por etapas/tiempo (no puntaje).

### Fase G5 — Pulido, accesibilidad y documentación · ✅ código (pase Expo pendiente)

**Hecho (2026-06-14):**
- **Auditoría AA de superficies de color** (cálculo de contraste): el gradiente indigo arrancaba en
  `#4f70bd` (más claro que el accent) → con `onStageMuted` al 74% el texto pequeño daba ~3.5:1 (falla
  AA normal). Correcciones de token en `theme.ts`: gradiente indigo arranca en el **accent `#3e5fad`**
  (nunca más claro que la base permitida) y `onStageMuted` sube a **0.80** (texto atenuado pequeño ≥4.5:1
  aun sobre la superficie de color más clara). Magenta/ink ya cumplían holgado.
- **Reduce-motion: cobertura total.** Auditados los 8 módulos con `Animated` (Splash, Button,
  PressableScale, StatePill, Countdown, Reaction, useCountUp): **todos** con alternativa estática vía
  `useReducedMotion`; el color/forma comunican sin movimiento.
- **Táctil:** botones/chips/cards ≥44–48px (Button 48, chips 44, cards 56). Safe areas a sangre vía
  `react-native-safe-area-context` (`Stage`).
- **Perf:** sin Reanimated (Expo Go); único re-render por segundo es el `Countdown` (ActiveStage/Trivia
  en vivo), aceptable. Listas: Trivia con `FlatList`; BDT cortas con `ScrollView`.
- **Documentación:** `design-system.md` (sección mobile) actualizada con los primitivos del registro de
  juego, las maquetas, la nota AA y el nuevo patrón de hooks BDT; conteo de tests 83 → 81. Este plan
  marcado completo.
- Verificado: `tsc --noEmit` exit 0 · `npm test` 81/81 · `expo export` (bundle Metro) OK.
- **Pendiente (usuario):** barrido visual final de las pantallas en Expo (inmersión, motion + reduce-motion,
  contraste a pleno sol, toques ≥44px).

---

## Estado final

Registro de juego mobile **completo (G0–G5)**: foundation, Home inmersivo, Trivia inmersivo + maqueta en
vivo, Podio, BDT inmersivo + maqueta de ranking, y pulido/AA/docs. Verificado con `tsc` + `npm test`
(81) + `expo export` en cada fase. **Falta solo el pase visual manual en Expo** y, a futuro, cablear las
maquetas (`LiveTriviaSource`, `BdtRankingSource`) al backend cuando exista la ejecución sincronizada.

---

## Verificación (regla por fase)
1. `npx tsc --noEmit` limpio. 2. `npm test` verde (sin tocar comportamiento/testIDs). 3. **Pase manual Expo**
en dispositivo: inmersión, motion + reduce-motion, contraste a pleno sol, toques ≥44px.

## Para un agente nuevo
Leer: este plan → `mobile-redesign-plan.md` (v1) + `design-system.md` → `DESIGN.md`/`PRODUCT.md` (paleta/fuentes
intactas) → `bdt-ranking-clarification.md` (regla de ranking BDT) → `AGENTS.md`/`CLAUDE.md`. **Regla de oro:**
inmersivo **sí**, kitsch **no**; misma paleta/fuentes; AA + reduce-motion; sin tocar contratos/testIDs/comportamiento.
