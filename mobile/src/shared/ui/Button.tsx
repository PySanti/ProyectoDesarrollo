import React, { useRef, useState } from 'react';
import { ActivityIndicator, Animated, GestureResponderEvent, Pressable, StyleSheet, Text, View, ViewStyle } from 'react-native';
import { colors, fonts, game, radius, spacing } from '../theme';
import { useReducedMotion } from '../useReducedMotion';
import { AppText } from './AppText';
import { Icon, IconName } from './Icon';

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger';

interface Props {
  label: string;
  onPress: () => void;
  variant?: Variant;
  disabled?: boolean;
  loading?: boolean;
  /** Ícono de línea a la izquierda del label. */
  icon?: IconName;
  /** Conteo opcional anclado a la derecha del botón (p. ej. invitaciones pendientes). Se oculta si es 0. */
  badgeCount?: number;
  /** Tratamiento para usarse sobre un `Stage` de color (relleno blanco / contornos claros). */
  onStage?: boolean;
  style?: ViewStyle;
  testID?: string;
  accessibilityLabel?: string;
}

/**
 * Botón de marca con micro-interacción de press (escala con spring, reduce-motion aware), ícono
 * opcional y variante `onStage` para fondos de color. Altura táctil ≥48px.
 */
export function Button({
  label,
  onPress,
  variant = 'primary',
  disabled,
  loading,
  icon,
  badgeCount,
  onStage,
  style,
  testID,
  accessibilityLabel,
}: Props) {
  const isDisabled = !!disabled || !!loading;
  const showBadge = typeof badgeCount === 'number' && badgeCount > 0;
  const badgeText = showBadge ? (badgeCount > 99 ? '99+' : String(badgeCount)) : '';
  const [pressed, setPressed] = useState(false);
  const scale = useRef(new Animated.Value(1)).current;
  const reduced = useReducedMotion();

  const animateTo = (toValue: number) =>
    Animated.spring(scale, { toValue, useNativeDriver: true, ...game.motion.spring }).start();

  const handleIn = (e: GestureResponderEvent) => {
    setPressed(true);
    if (!reduced && !isDisabled) animateTo(game.motion.pressScale);
  };
  const handleOut = (e: GestureResponderEvent) => {
    setPressed(false);
    animateTo(1);
  };

  const fg = textColor(variant, isDisabled, onStage);

  return (
    <Pressable
      testID={testID}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel ?? (showBadge ? `${label}, ${badgeCount} pendientes` : label)}
      accessibilityState={{ disabled: isDisabled, busy: !!loading }}
      onPress={onPress}
      onPressIn={handleIn}
      onPressOut={handleOut}
      disabled={isDisabled}
    >
      <Animated.View style={[styles.base, fillStyle(variant, pressed, isDisabled, onStage), { transform: [{ scale }] }, style]}>
        {loading ? (
          <ActivityIndicator color={fg} />
        ) : (
          <>
            {icon ? <Icon name={icon} size={18} color={fg} /> : null}
            <AppText variant="label" color={fg}>
              {label}
            </AppText>
            {showBadge ? (
              <View style={styles.badgeWrap} pointerEvents="none">
                <View style={styles.badge}>
                  <Text style={styles.badgeText}>{badgeText}</Text>
                </View>
              </View>
            ) : null}
          </>
        )}
      </Animated.View>
    </Pressable>
  );
}

function fillStyle(variant: Variant, pressed: boolean, disabled: boolean, onStage?: boolean): ViewStyle {
  if (disabled) {
    if (variant === 'secondary' || variant === 'ghost') {
      return onStage
        ? { backgroundColor: 'transparent', borderWidth: 1, borderColor: game.onStageLine }
        : { backgroundColor: 'transparent', borderWidth: 1, borderColor: colors.line };
    }
    return { backgroundColor: onStage ? game.onStageSunk : colors.primaryDisabled };
  }

  if (onStage) {
    switch (variant) {
      case 'primary':
        return { backgroundColor: pressed ? colors.primaryWash : game.onStage };
      case 'danger':
        return { backgroundColor: pressed ? '#a81f25' : colors.danger };
      case 'secondary':
        return { backgroundColor: pressed ? game.onStageSunk : 'transparent', borderWidth: 1, borderColor: game.onStageLine };
      case 'ghost':
        return { backgroundColor: pressed ? game.onStageSunk : 'transparent' };
    }
  }

  switch (variant) {
    case 'primary':
      return { backgroundColor: pressed ? colors.primaryStrong : colors.primaryFill };
    case 'danger':
      return { backgroundColor: pressed ? '#a81f25' : colors.danger };
    case 'secondary':
      return { backgroundColor: pressed ? colors.surfaceSunk : colors.surface, borderWidth: 1, borderColor: colors.lineStrong };
    case 'ghost':
      return { backgroundColor: pressed ? colors.surface : 'transparent' };
  }
}

function textColor(variant: Variant, disabled: boolean, onStage?: boolean): string {
  if (disabled && (variant === 'secondary' || variant === 'ghost')) {
    return onStage ? game.onStageMuted : colors.muted;
  }
  if (onStage) {
    if (variant === 'primary') return colors.primaryStrong; // texto magenta sobre botón blanco
    return colors.white; // secondary/ghost/danger sobre color → texto blanco
  }
  switch (variant) {
    case 'secondary':
      return colors.ink;
    case 'ghost':
      return colors.accent;
    default:
      return colors.white;
  }
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
  // Envoltura de alto completo para centrar verticalmente el pill sin cálculos de offset.
  badgeWrap: {
    position: 'absolute',
    right: spacing.md,
    top: 0,
    bottom: 0,
    justifyContent: 'center',
  },
  // Pill blanco con número magenta: contrasta sobre el botón primario magenta y sobre el secondary claro.
  badge: {
    minWidth: 22,
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: radius.pill,
    backgroundColor: colors.white,
    borderWidth: 1,
    borderColor: colors.lineStrong,
    alignItems: 'center',
    justifyContent: 'center',
  },
  badgeText: {
    fontFamily: fonts.bold,
    fontSize: 12,
    lineHeight: 16,
    color: colors.primaryStrong,
  },
});
