import { TextStyle, ViewStyle } from 'react-native';

/**
 * Theme de marca UMBRAL para mobile (Fase 2 del rediseño).
 *
 * Espejo en **hex** de los tokens de `DESIGN.md` (raíz), porque RN no entiende `oklch()`.
 * Una sola verdad de marca, dos plataformas: la web usa los mismos valores vía CSS/OKLCH.
 *
 * Regla de color: el **magenta** carga la energía y significa "vivo / ahora"; los neutros
 * cargan la densidad. Estado = color + texto + forma, nunca solo color.
 */
export const colors = {
  // —— Superficies / tinta (neutros) ——
  bg: '#ffffff', // fondo blanco puro
  surface: '#f7f4f7', // paneles, tarjetas, cabeceras de tabla
  surfaceSunk: '#efebef', // agrupaciones internas, zonas hundidas
  ink: '#1b131a', // texto cuerpo (18.2:1 sobre blanco)
  inkSoft: '#433942', // títulos secundarios, texto sobre superficies
  muted: '#6e666d', // texto secundario, metadatos (5.5:1)
  line: '#e1dce0', // bordes y divisores por defecto
  lineStrong: '#cec8ce', // bordes de input, separadores visibles

  // —— Marca: magenta (única voz "viva") ——
  primaryBright: '#b545ae', // magenta señal: foco, nav activa, estado "En vivo"
  primaryFill: '#982f93', // fondo de botón primario (texto blanco 6.6:1)
  primaryStrong: '#7d2278', // pressed/active, texto magenta sobre wash
  primaryWash: '#fbe8f8', // fondo de nav activa y chips "En vivo"
  primaryDisabled: '#d3b3d0', // magenta desaturado para primario deshabilitado

  // —— Acento: indigo (segunda voz, calma "Lobby") ——
  accent: '#3e5fad',
  accentWash: '#e6efff',

  // —— Estado de partida (siempre con texto + forma) ——
  stateLive: '#b545ae',
  stateLiveWash: '#fbe8f8',
  stateLobby: '#3e5fad',
  stateLobbyWash: '#e6efff',
  stateDone: '#756f74',
  stateDoneWash: '#efebef',

  // —— Semántico ——
  success: '#1c8742',
  successWash: '#e1f5e4',
  warning: '#f2af48',
  warningWash: '#fbedd1',
  warningInk: '#5a3d0a', // texto oscuro sobre ámbar (nunca blanco)
  danger: '#cc272e',
  dangerWash: '#ffe7e4',

  // —— Util ——
  white: '#ffffff',
} as const;

export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 24,
  xxxl: 32,
} as const;

/** Radios alineados a `DESIGN.md`: esquinas contenidas, nada de píldoras salvo chips de estado. */
export const radius = {
  sm: 6,
  md: 8, // botones, inputs
  lg: 12, // tarjetas, paneles
  xl: 16, // contenedores grandes
  pill: 999, // solo chips de estado
} as const;

/**
 * Familias de fuente. Los valores son las claves de export de `@expo-google-fonts/*`
 * que carga `useAppFonts()` (ver `shared/fonts.ts`). Si una fuente no está cargada,
 * RN cae a la del sistema sin romper.
 */
export const fonts = {
  display: 'SpaceGrotesk_600SemiBold', // títulos de página / sección (grotesca con carácter)
  displayStrong: 'SpaceGrotesk_700Bold', // énfasis (puntajes)
  body: 'Inter_400Regular', // cuerpo
  bodyMedium: 'Inter_500Medium',
  semibold: 'Inter_600SemiBold', // títulos de tarjeta, labels, énfasis de cuerpo
  bold: 'Inter_700Bold',
  mono: 'JetBrainsMono_500Medium', // IDs, códigos, QR
  monoStrong: 'JetBrainsMono_600SemiBold',
} as const;

/**
 * Presets tipográficos (jerarquía de `DESIGN.md`): escala y peso construyen la jerarquía,
 * no las mayúsculas (regla No-Eyebrow). Labels en sentence case.
 */
export const typography: Record<
  'mega' | 'hero' | 'display' | 'headline' | 'title' | 'body' | 'bodyStrong' | 'label' | 'mono',
  TextStyle
> = {
  // Números/timer protagonistas del registro de juego (Space Grotesk 700).
  mega: { fontFamily: fonts.displayStrong, fontSize: 64, lineHeight: 66, letterSpacing: -1.5, color: colors.ink },
  hero: { fontFamily: fonts.displayStrong, fontSize: 40, lineHeight: 44, letterSpacing: -1, color: colors.ink },
  display: { fontFamily: fonts.display, fontSize: 30, lineHeight: 34, letterSpacing: -0.6, color: colors.ink },
  headline: { fontFamily: fonts.display, fontSize: 22, lineHeight: 28, letterSpacing: -0.3, color: colors.ink },
  title: { fontFamily: fonts.semibold, fontSize: 18, lineHeight: 24, letterSpacing: -0.2, color: colors.ink },
  body: { fontFamily: fonts.body, fontSize: 15, lineHeight: 22, color: colors.inkSoft },
  bodyStrong: { fontFamily: fonts.semibold, fontSize: 15, lineHeight: 22, color: colors.ink },
  label: { fontFamily: fonts.semibold, fontSize: 13, lineHeight: 18, letterSpacing: 0.1, color: colors.muted },
  mono: { fontFamily: fonts.mono, fontSize: 13, lineHeight: 19, color: colors.inkSoft },
};

/**
 * **Registro de juego (v2, solo mobile).** Capa de uso inmersiva sobre los mismos tokens: superficies
 * a sangre completa, gradientes del **mismo hue** magenta/indigo, glow magenta y parámetros de motion.
 * No introduce colores nuevos. El texto sobre `stage` va en blanco/`onStageMuted` (AA: los fondos
 * elegidos son ≥ `primary-fill`/indigo, donde el blanco alcanza ≥4.5:1).
 */
export const game = {
  stage: {
    magenta: colors.primaryFill, // #982f93 (blanco encima: 6.6:1)
    magentaDeep: colors.primaryStrong, // #7d2278
    indigo: colors.accent, // #3e5fad
    ink: '#1a1119', // oscuro dramático con tinte magenta
  },
  onStage: '#ffffff',
  // 0.80 (no 0.74): garantiza AA del texto atenuado pequeño aun sobre la superficie de color más clara
  // permitida (indigo #3e5fad → ~4.6:1). Estado/jerarquía siguen dándose por color + forma + tamaño.
  onStageMuted: 'rgba(255,255,255,0.80)',
  onStageLine: 'rgba(255,255,255,0.18)',
  onStageSunk: 'rgba(255,255,255,0.10)',
  // Gradientes verticales fill → deep del **mismo hue**. El tope nunca es más claro que el color base
  // permitido (regla AA: blanco solo sobre `primary-fill`/`accent` o más oscuro), por eso el indigo
  // arranca en el accent #3e5fad y no en un tono más claro.
  gradient: {
    magenta: ['#982f93', '#7d2278'] as const,
    indigo: ['#3e5fad', '#2c4790'] as const,
    ink: ['#2a1f29', '#140d13'] as const,
  },
  // Sombra/halo magenta para elementos "vivos" sobre fondo claro (permitido en el registro de juego).
  glow: {
    shadowColor: colors.primaryFill,
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.35,
    shadowRadius: 16,
    elevation: 8,
  } as ViewStyle,
  // Parámetros para la API `Animated` nativa de RN (sin dependencias nativas extra).
  motion: {
    fast: 140,
    base: 220,
    slow: 360,
    pressScale: 0.96,
    spring: { friction: 7, tension: 180 }, // feel de press (Animated.spring)
  },
} as const;
