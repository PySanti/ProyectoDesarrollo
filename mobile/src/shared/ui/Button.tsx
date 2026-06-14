import React from 'react';
import { ActivityIndicator, Pressable, StyleSheet, ViewStyle } from 'react-native';
import { colors, radius, spacing } from '../theme';
import { AppText } from './AppText';

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger';

interface Props {
  label: string;
  onPress: () => void;
  variant?: Variant;
  disabled?: boolean;
  loading?: boolean;
  style?: ViewStyle;
  testID?: string;
  accessibilityLabel?: string;
}

/**
 * Botón de marca. Esquinas contenidas (8px), altura táctil ≥48px. El primario (magenta
 * relleno) es la acción que cambia el estado del juego; secundario/ghost para apoyo.
 * Pressed oscurece (sin `translateY` exagerado).
 */
export function Button({
  label,
  onPress,
  variant = 'primary',
  disabled,
  loading,
  style,
  testID,
  accessibilityLabel,
}: Props) {
  const isDisabled = !!disabled || !!loading;

  return (
    <Pressable
      testID={testID}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel ?? label}
      accessibilityState={{ disabled: isDisabled, busy: !!loading }}
      onPress={onPress}
      disabled={isDisabled}
      style={({ pressed }) => [styles.base, fillStyle(variant, pressed, isDisabled), style]}
    >
      {loading ? (
        <ActivityIndicator color={spinnerColor(variant)} />
      ) : (
        <AppText variant="label" color={textColor(variant, isDisabled)}>
          {label}
        </AppText>
      )}
    </Pressable>
  );
}

function fillStyle(variant: Variant, pressed: boolean, disabled: boolean): ViewStyle {
  if (disabled) {
    if (variant === 'secondary' || variant === 'ghost') {
      return { backgroundColor: 'transparent', borderWidth: 1, borderColor: colors.line };
    }
    return { backgroundColor: colors.primaryDisabled };
  }
  switch (variant) {
    case 'primary':
      return { backgroundColor: pressed ? colors.primaryStrong : colors.primaryFill };
    case 'danger':
      return { backgroundColor: pressed ? '#a81f25' : colors.danger };
    case 'secondary':
      return {
        backgroundColor: pressed ? colors.surfaceSunk : colors.surface,
        borderWidth: 1,
        borderColor: colors.lineStrong,
      };
    case 'ghost':
      return { backgroundColor: pressed ? colors.surface : 'transparent' };
  }
}

function textColor(variant: Variant, disabled: boolean): string {
  if (disabled && (variant === 'secondary' || variant === 'ghost')) return colors.muted;
  switch (variant) {
    case 'secondary':
      return colors.ink;
    case 'ghost':
      return colors.accent;
    default:
      return colors.white;
  }
}

function spinnerColor(variant: Variant): string {
  return variant === 'secondary' || variant === 'ghost' ? colors.primaryFill : colors.white;
}

const styles = StyleSheet.create({
  base: {
    minHeight: 48,
    borderRadius: radius.md,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
  },
});
