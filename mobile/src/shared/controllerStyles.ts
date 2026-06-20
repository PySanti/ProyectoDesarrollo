import { TextStyle, ViewStyle } from 'react-native';
import { colors, fonts, radius, spacing, typography } from './theme';

/**
 * Fragmentos de estilo de marca para las pantallas **controller-driven** (BDT, y equipos
 * Leave/Transfer): esas pantallas inyectan un objeto `styles` por claves al controller testeado,
 * así que el re-skin se hace cambiando **valores** sin tocar la estructura. Estos fragmentos dan
 * una sola fuente de verdad para mapear las claves legacy (title/card/error/joinButton…) a tokens.
 */
function noticeStyle(bg: string, border: string, fg: string): TextStyle {
  return {
    backgroundColor: bg,
    borderWidth: 1,
    borderColor: border,
    borderRadius: radius.md,
    padding: spacing.md,
    color: fg,
    fontFamily: fonts.semibold,
    fontSize: 14,
  };
}

export const cs = {
  safeArea: { flex: 1, backgroundColor: colors.bg } as ViewStyle,
  /** contentContainer de ScrollView (sin flex) o View superior. */
  container: { padding: spacing.xl, gap: spacing.lg } as ViewStyle,
  title: typography.display as TextStyle,
  description: { ...typography.body, color: colors.muted } as TextStyle,

  card: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    borderRadius: radius.lg,
    padding: spacing.lg,
    gap: spacing.xs,
  } as ViewStyle,
  cardTitle: typography.title as TextStyle,
  cardLine: { ...typography.body, color: colors.inkSoft } as TextStyle,
  empty: { ...typography.body, color: colors.muted } as TextStyle,

  error: noticeStyle(colors.dangerWash, colors.danger, colors.danger),
  success: noticeStyle(colors.successWash, colors.success, '#136530'),

  filters: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm } as ViewStyle,
  filterButton: {
    minHeight: 44,
    justifyContent: 'center',
    borderRadius: radius.pill,
    borderWidth: 1,
    borderColor: colors.lineStrong,
    backgroundColor: colors.surface,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
  } as ViewStyle,
  filterButtonActive: { backgroundColor: colors.primaryWash, borderColor: colors.primaryBright } as ViewStyle,
  filterText: { ...typography.label, color: colors.inkSoft } as TextStyle,
  filterTextActive: { color: colors.primaryStrong } as TextStyle,

  primaryButton: {
    minHeight: 48,
    borderRadius: radius.md,
    backgroundColor: colors.primaryFill,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
  } as ViewStyle,
  primaryButtonDisabledFill: {
    minHeight: 48,
    borderRadius: radius.md,
    backgroundColor: colors.primaryDisabled,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
  } as ViewStyle,
  primaryButtonDisabled: { backgroundColor: colors.primaryDisabled } as ViewStyle,
  primaryButtonText: { color: colors.white, fontFamily: fonts.semibold, fontSize: 15 } as TextStyle,

  secondaryButton: {
    minHeight: 48,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.lineStrong,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
  } as ViewStyle,
  secondaryButtonText: { color: colors.ink, fontFamily: fonts.semibold, fontSize: 15 } as TextStyle,
};
