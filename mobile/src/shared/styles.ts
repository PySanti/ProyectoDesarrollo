import { TextStyle, ViewStyle } from 'react-native';
import { colors, radius, spacing } from './theme';

type NamedStyles = Record<string, ViewStyle | TextStyle>;

export const screenStyles: NamedStyles = {
  safeArea: {
    flex: 1,
    backgroundColor: colors.background,
  },
  container: {
    flex: 1,
    padding: spacing.xl,
    gap: spacing.md,
  },
  scrollContainer: {
    padding: spacing.xl,
    gap: spacing.md,
  },
  centered: {
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xxl,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
    color: colors.text,
  },
  subtitle: {
    color: colors.textMuted,
    fontSize: 14,
  },
  description: {
    color: colors.textSoft,
    fontSize: 14,
    lineHeight: 20,
  },
  label: {
    fontSize: 14,
    color: colors.textMuted,
  },
  input: {
    borderWidth: 1,
    borderColor: colors.inputBorder,
    borderRadius: radius.md,
    backgroundColor: colors.card,
    fontSize: 16,
    color: colors.text,
    paddingHorizontal: spacing.md,
    paddingVertical: 10,
  },
  error: {
    color: colors.danger,
    fontSize: 13,
  },
  errorText: {
    color: colors.danger,
    fontSize: 14,
    textAlign: 'center',
    marginBottom: spacing.lg,
  },
  success: {
    color: colors.success,
    fontSize: 13,
  },
  empty: {
    color: colors.textSoft,
    fontSize: 14,
  },
  emptyText: {
    fontSize: 15,
    color: colors.textSoft,
    textAlign: 'center',
    marginBottom: spacing.lg,
  },
  loadingText: {
    marginTop: spacing.md,
    fontSize: 14,
    color: colors.textSoft,
  },
  card: {
    borderRadius: radius.lg,
    backgroundColor: colors.card,
    borderWidth: 1,
    borderColor: colors.border,
    padding: 14,
    gap: spacing.xs,
  },
  cardTitle: {
    color: colors.text,
    fontSize: 17,
    fontWeight: '700',
  },
  cardLine: {
    color: colors.textMuted,
    fontSize: 13,
  },
  primaryButton: {
    marginTop: spacing.sm,
    borderRadius: radius.md,
    backgroundColor: colors.primary,
    paddingVertical: spacing.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  primaryButtonDisabled: {
    backgroundColor: colors.primaryDisabled,
  },
  primaryButtonText: {
    color: colors.card,
    fontWeight: '700',
    fontSize: 15,
  },
  secondaryButton: {
    borderRadius: radius.md,
    backgroundColor: colors.primaryMuted,
    paddingHorizontal: spacing.md,
    paddingVertical: 10,
    alignItems: 'center',
    justifyContent: 'center',
  },
  secondaryButtonText: {
    color: colors.primary,
    fontWeight: '700',
  },
  dangerButton: {
    marginTop: spacing.sm,
    borderRadius: radius.md,
    backgroundColor: colors.danger,
    paddingVertical: spacing.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  filters: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.sm,
  },
  filterButton: {
    borderRadius: radius.pill,
    borderWidth: 1,
    borderColor: colors.disabled,
    paddingHorizontal: 14,
    paddingVertical: spacing.sm,
    backgroundColor: colors.card,
  },
  filterButtonActive: {
    backgroundColor: colors.primary,
    borderColor: colors.primary,
  },
  filterText: {
    color: colors.textMuted,
    fontWeight: '700',
  },
  filterTextActive: {
    color: colors.card,
  },
  joinButton: {
    marginTop: spacing.sm,
    borderRadius: radius.md,
    backgroundColor: colors.primary,
    paddingHorizontal: spacing.md,
    paddingVertical: 10,
    alignItems: 'center',
    justifyContent: 'center',
  },
  joinButtonDisabled: {
    backgroundColor: colors.disabled,
  },
  disabledButton: {
    marginTop: spacing.sm,
    borderRadius: radius.md,
    backgroundColor: colors.disabled,
    paddingHorizontal: spacing.md,
    paddingVertical: 10,
    alignItems: 'center',
  },
  joinButtonText: {
    color: colors.card,
    fontWeight: '700',
  },
};
