---
name: UMBRAL
description: Consola operativa de Trivia y BDT — energía de juego, densidad de operador.
colors:
  bg: "#ffffff"
  surface: "#f7f4f7"
  surface-sunk: "#efebef"
  ink: "#1b131a"
  ink-soft: "#433942"
  muted: "#6e666d"
  line: "#e1dce0"
  line-strong: "#cec8ce"
  primary: "#b545ae"
  primary-fill: "#982f93"
  primary-strong: "#7d2278"
  primary-wash: "#fbe8f8"
  accent: "#3e5fad"
  accent-wash: "#e6efff"
  state-live: "#b545ae"
  state-lobby: "#3e5fad"
  state-done: "#756f74"
  success: "#1c8742"
  success-wash: "#e1f5e4"
  warning: "#f2af48"
  warning-wash: "#fbedd1"
  danger: "#cc272e"
  danger-wash: "#ffe7e4"
typography:
  display:
    fontFamily: "Space Grotesk, Segoe UI, system-ui, sans-serif"
    fontSize: "clamp(1.75rem, 1.2rem + 2vw, 2.5rem)"
    fontWeight: 600
    lineHeight: 1.1
    letterSpacing: "-0.02em"
  headline:
    fontFamily: "Space Grotesk, Segoe UI, system-ui, sans-serif"
    fontSize: "1.5rem"
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: "-0.015em"
  title:
    fontFamily: "Inter, Segoe UI, system-ui, sans-serif"
    fontSize: "1.125rem"
    fontWeight: 600
    lineHeight: 1.3
    letterSpacing: "-0.01em"
  body:
    fontFamily: "Inter, Segoe UI, system-ui, sans-serif"
    fontSize: "0.9375rem"
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: "normal"
  label:
    fontFamily: "Inter, Segoe UI, system-ui, sans-serif"
    fontSize: "0.8125rem"
    fontWeight: 600
    lineHeight: 1.4
    letterSpacing: "0.005em"
  mono:
    fontFamily: "JetBrains Mono, ui-monospace, SFMono-Regular, monospace"
    fontSize: "0.8125rem"
    fontWeight: 500
    lineHeight: 1.45
    letterSpacing: "normal"
rounded:
  sm: "6px"
  md: "8px"
  lg: "12px"
  xl: "16px"
  pill: "999px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "12px"
  lg: "16px"
  xl: "24px"
  "2xl": "32px"
  "3xl": "48px"
components:
  button-primary:
    backgroundColor: "{colors.primary-fill}"
    textColor: "{colors.bg}"
    typography: "{typography.label}"
    rounded: "{rounded.md}"
    padding: "10px 16px"
  button-primary-hover:
    backgroundColor: "{colors.primary-strong}"
    textColor: "{colors.bg}"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    typography: "{typography.label}"
    rounded: "{rounded.md}"
    padding: "10px 16px"
  input:
    backgroundColor: "{colors.bg}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    padding: "10px 12px"
  nav-item-active:
    backgroundColor: "{colors.primary-wash}"
    textColor: "{colors.primary-strong}"
    typography: "{typography.label}"
    rounded: "{rounded.md}"
    padding: "8px 12px"
  table-header:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.muted}"
    typography: "{typography.label}"
    padding: "10px 12px"
---

# Design System: UMBRAL

## 1. Overview

**Creative North Star: "El tablero que respira"**

UMBRAL es una consola operativa, no un dashboard de marketing ni un panel administrativo gris. Quien la usa cruza un umbral: pasa de preparar una experiencia a conducirla en vivo. El sistema visual existe para que ese momento se sienta bajo control y, a la vez, vivo. La superficie es blanca y nítida (cargar formularios largos sin fatiga, proyectarse legible en una sala), y el **magenta de marca es la señal de que algo está pasando ahora**: el lobby llenándose, la partida iniciada, el ranking moviéndose. El color no decora; señala estado.

La densidad es de grado operador. Los neutros hacen el trabajo pesado de formularios y tablas; el magenta aparece poco y por eso pesa. Cada estado (Lobby, En vivo, Cancelada, Terminada, QR legible/no legible) se comunica con **color más texto más forma**, nunca solo con color, porque la consola se opera en vivo, a veces se proyecta, y debe leerse con daltonismo o poca luz. La energía no viene de adornos sino de la verdad del juego mostrada con claridad y del movimiento reservado para cuando el estado cambia.

Rechaza explícitamente las cuatro anti-referencias de PRODUCT.md: el dashboard SaaS azul/teal con tarjetas iguales, gradientes suaves y glassmorphism; lo infantil o gamificado (confeti, mascotas, casino); el corporativo gris apagado sin vida; y el marketing llamativo de héroes y eslóganes. UMBRAL es marca propia: la UCAB es solo el contexto académico.

**Key Characteristics:**
- Superficie blanca pura; el magenta de marca carga la energía, no el fondo.
- El color codifica estado; los neutros cargan la densidad.
- Estado siempre como color + texto + forma (nunca solo color).
- Mismos tokens en web (CSS) y mobile (theme TS): una sola verdad, dos plataformas.
- WCAG 2.1 AA verificado en cada par de color.

## 2. Colors

Una superficie casi-neutra con un magenta inconfundible como única voz de marca, un indigo de apoyo y un set semántico mínimo para estados.

### Primary
- **Magenta Umbral** (`oklch(0.58 0.19 330)` / `#b545ae`): la marca y la señal de "en vivo". Reservado para acción primaria, foco, item de navegación activo y el estado de partida Iniciada.
- **Magenta Relleno** (`oklch(0.50 0.18 330)` / `#982f93`): fondo de botones primarios; texto blanco encima alcanza 6.6:1.
- **Magenta Profundo** (`oklch(0.43 0.16 330)` / `#7d2278`): hover/active de primarios y texto magenta sobre `primary-wash`.
- **Lavado Magenta** (`oklch(0.95 0.03 330)` / `#fbe8f8`): fondo del item de nav activo y de chips de estado "En vivo".

### Secondary
- **Indigo Señal** (`oklch(0.50 0.13 265)` / `#3e5fad`): segunda voz de marca, distinta en tono y luz. Enlaces, énfasis secundario y el estado calmo "Lobby/espera". Nunca compite como acción primaria; es deliberadamente más frío y sobrio que el magenta.
- **Lavado Indigo** (`oklch(0.95 0.025 265)` / `#e6efff`): fondo de chips "Lobby".

### Tertiary
- **Verde Validación** (`oklch(0.55 0.14 150)` / `#1c8742`): éxito, QR Legible.
- **Ámbar Atención** (`oklch(0.80 0.14 75)` / `#f2af48`): advertencia, QR No-legible; lleva texto oscuro (9.6:1), nunca texto blanco.
- **Rojo Corte** (`oklch(0.55 0.20 25)` / `#cc272e`): error, partida Cancelada.

### Neutral
- **Tinta** (`oklch(0.20 0.02 330)` / `#1b131a`): texto cuerpo, 18.2:1 sobre blanco. Un susurro de magenta en el tono evita el negro frío de oficina.
- **Tinta Suave** (`oklch(0.36 0.02 330)` / `#433942`): títulos secundarios, texto sobre superficies.
- **Apagado** (`oklch(0.52 0.015 330)` / `#6e666d`): texto secundario y metadatos, 5.5:1 sobre blanco.
- **Línea** (`oklch(0.90 0.008 330)` / `#e1dce0`): bordes y divisores por defecto.
- **Línea Fuerte** (`oklch(0.84 0.01 330)` / `#cec8ce`): bordes de input y separadores que deben verse.
- **Superficie** (`oklch(0.97 0.005 330)` / `#f7f4f7`) y **Superficie Hundida** (`oklch(0.945 0.006 330)` / `#efebef`): paneles, cabeceras de tabla, zonas agrupadas.

### Named Rules
**The One Live Voice Rule.** El magenta es la única marca y significa "vivo/ahora". Prohibido usarlo como relleno decorativo, como color de un estado que no sea actividad en curso, o repartido por más del ~10% de una pantalla. Su rareza es lo que lo hace señal.

**The State Is Never Color Alone Rule.** Todo estado se codifica con tres canales: color + etiqueta de texto + forma (punto, anillo, ícono). Quitar el color nunca debe destruir la información. Innegociable para AA, daltonismo y proyección.

## 3. Typography

**Display Font:** Space Grotesk (con Segoe UI / system-ui)
**Body Font:** Inter (con Segoe UI / system-ui)
**Label/Mono Font:** JetBrains Mono (con ui-monospace)

**Character:** Una grotesca de carácter (Space Grotesk) para títulos da personalidad y energía sin gritar; Inter humanista mantiene legible el cuerpo denso de formularios y tablas. Se emparejan en un eje de contraste real (grotesca geométrica vs. humanista neutra), no dos sans gemelas. El mono no es decorativo: porta identificadores, códigos de acceso y QR, donde el ancho fijo y la distinción 0/O, 1/l importan.

### Hierarchy
- **Display** (600, `clamp(1.75rem, 1.2rem + 2vw, 2.5rem)`, 1.1): título de página / cabecera de sección principal. Techo modesto: es una herramienta, no un héroe de landing.
- **Headline** (600, 1.5rem/24px, 1.2): títulos de bloques dentro de una pantalla.
- **Title** (600, 1.125rem/18px, 1.3): títulos de tarjeta, tabla o panel.
- **Body** (400, 0.9375rem/15px, 1.5): texto general; prosa acotada a 65–75ch.
- **Label** (600, 0.8125rem/13px, +0.005em): etiquetas de formulario, cabeceras de tabla, chips. **Sentence case**, no mayúsculas.
- **Mono** (500, 0.8125rem/13px): IDs, `codigoAcceso`, QR decodificado, timestamps técnicos.

### Named Rules
**The No-Eyebrow Rule.** Prohibido el kicker en mayúsculas con tracking ancho sobre cada sección (el "PANEL WEB" actual). La jerarquía se construye con escala y peso (ratio ≥1.25 entre pasos), no con mayúsculas. El único uso de mayúsculas permitido es algún chip de estado corto, y aun así con su forma e ícono.

**The Mono For Machine Strings Rule.** Todo identificador, código o QR se renderiza en mono; nunca un UUID en la tipografía de cuerpo. Si una celda muestra un UUID, va en mono, truncada, con acción de copiar.

## 4. Elevation

Plano por defecto. La profundidad se construye con capas tonales (bg → surface → surface-sunk) y bordes de 1px, no con sombras. Las sombras aparecen **solo como respuesta a estado**: elevación de overlays (modal, dropdown) y un leve realce en hover de elementos accionables. Prohibidas las sombras grandes y difusas decorativas del diseño anterior (`0 18px 50px`).

### Shadow Vocabulary
- **lift-hover** (`box-shadow: 0 1px 2px rgba(27,19,26,0.06), 0 2px 6px rgba(27,19,26,0.08)`): realce sutil en hover de filas/botones accionables.
- **overlay** (`box-shadow: 0 8px 24px rgba(27,19,26,0.16)`): modales y menús que flotan sobre la página.

### Named Rules
**The Flat-By-Default Rule.** Las superficies están planas en reposo. Una sombra es una afirmación de estado (esto flota, esto reacciona), nunca decoración ambiental. Si una tarjeta lleva sombra sin interacción ni elevación real, quítala.

## 5. Components

### Buttons
- **Shape:** esquinas contenidas (8px, `{rounded.md}`). Nada de píldoras 999px salvo chips de estado.
- **Primary:** fondo Magenta Relleno (`#982f93`), texto blanco, padding `10px 16px`, label 13px. Es la acción que cambia el estado del juego (Iniciar partida, Enviar pista, Crear formulario).
- **Hover / Focus:** hover a Magenta Profundo (`#7d2278`); foco con anillo `0 0 0 3px var(--primary-wash)` y borde magenta, siempre visible. Sin `translateY` exagerado: un realce de 1px como máximo.
- **Secondary:** fondo Superficie, texto Tinta, borde Línea Fuerte. Acciones de apoyo (Actualizar, Cancelar edición).
- **Ghost:** sin fondo ni borde, texto Indigo; acciones terciarias y enlaces de navegación inline.

### Chips (State Pills)
- **Style:** fondo del lavado correspondiente, texto del color de estado en su variante profunda, forma redondeada `{rounded.pill}`, con un punto/ícono de 8px a la izquierda y etiqueta de texto.
- **State:** `En vivo` (magenta, punto pulsante con reduce-motion → estático), `Lobby` (indigo, punto hueco), `Cancelada` (rojo, equis), `Terminada` (neutro Apagado, check tenue), `QR legible` (verde), `QR no legible` (ámbar). Nunca solo el punto: siempre acompaña la etiqueta.

### Cards / Containers
- **Corner Style:** 12px (`{rounded.lg}`).
- **Background:** Superficie sobre fondo blanco; agrupaciones internas en Superficie Hundida.
- **Shadow Strategy:** plano en reposo (ver Elevation). Sin glassmorphism, sin `backdrop-filter`.
- **Border:** 1px Línea.
- **Internal Padding:** `lg`–`xl` (16–24px).

### Inputs / Fields
- **Style:** fondo blanco, borde 1px Línea Fuerte, radio 8px, texto Tinta, label 13px en sentence case sobre el campo.
- **Focus:** borde Magenta + anillo `0 0 0 3px var(--primary-wash)`. Transición de borde/sombra 160ms ease-out.
- **Error:** borde Rojo Corte + mensaje en texto Rojo bajo el campo (texto, no solo borde). **Disabled:** fondo Superficie Hundida, texto Apagado.

### Navigation
- **Style:** nav rail persistente a la izquierda con áreas (Identidad / Trivia / BDT) agrupadas por rol; etiquetas en label 13px con ícono.
- **States:** default texto Tinta Suave; hover fondo Superficie; **active** fondo Lavado Magenta con texto Magenta Profundo y barra de 2px Magenta al borde interno. Móvil: el rail colapsa a navegación inferior/stack según plataforma (RN), conservando los mismos estados y tokens.

### Data Table (Signature Component)
La tabla de supervisión es el componente firma de la consola. Cabecera sticky en Superficie con label en sentence case; filas con hover `lift-hover`; zebra opcional en Superficie Hundida; **UUIDs y códigos en mono, truncados con acción copiar**; estados como State Pills; y un **empty state diseñado** (ícono + frase concreta + acción), nunca solo texto Apagado.

## 6. Do's and Don'ts

### Do:
- **Do** mantener el fondo en blanco puro (`#ffffff`) y dejar que el Magenta Umbral cargue la energía.
- **Do** reservar el magenta para acción primaria, foco, nav activa y el estado En vivo (la regla One Live Voice, ≤10% de la pantalla).
- **Do** codificar cada estado con color + texto + forma (la regla State Is Never Color Alone).
- **Do** renderizar todo UUID, código de acceso y QR en JetBrains Mono, truncado y con copiar.
- **Do** construir jerarquía con escala y peso (ratio ≥1.25); etiquetas en sentence case.
- **Do** mantener superficies planas en reposo; sombras solo en overlays y hover.
- **Do** verificar AA: cuerpo ≥4.5:1, texto grande ≥3:1, foco visible, `prefers-reduced-motion` con alternativa.

### Don't:
- **Don't** caer en el **dashboard SaaS azul/teal con tarjetas iguales, gradientes suaves y glassmorphism** (anti-referencia #1): nada de `backdrop-filter` decorativo ni el azul-académico como color primario.
- **Don't** volverlo **infantil o gamificado**: sin confeti, mascotas, estética de casino ni píldoras gigantes brillantes.
- **Don't** dejarlo caer en **corporativo gris apagado**: densidad no es lo mismo que sin vida; el magenta y el lenguaje de estados lo mantienen vivo.
- **Don't** vestirlo de **marketing llamativo**: nada de héroes enormes, eslóganes ni CTA gigantes; es una herramienta.
- **Don't** usar el kicker en mayúsculas con tracking sobre cada sección (la regla No-Eyebrow).
- **Don't** usar `border-left`/`border-right` de más de 1px como franja de color en tarjetas o alertas.
- **Don't** usar texto en gradiente (`background-clip: text`) ni sombras grandes difusas decorativas (`0 18px 50px`).
- **Don't** imprimir un UUID en la tipografía de cuerpo ni mostrar un estado solo por color.
