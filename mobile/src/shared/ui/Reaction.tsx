import React, { useEffect, useRef } from 'react';
import { Animated, StyleSheet, View } from 'react-native';
import { colors, game, radius, spacing } from '../theme';
import { useReducedMotion } from '../useReducedMotion';
import { AppText } from './AppText';
import { Icon } from './Icon';

interface Props {
  /** Veredicto. `true` → acierto (verde); `false` → fallo (rojo). */
  correct: boolean;
  title: string;
  subtitle?: string;
  /** Sobre un `Stage` de color (texto blanco). Por defecto `true`. */
  onStage?: boolean;
}

/**
 * Reacción de resultado (momento estelar, sin confeti): disco con ícono check/✕ que entra con un
 * pop sobrio (spring), en verde/rojo semánticos existentes. Estado = color + ícono + texto. Respeta
 * `prefers-reduced-motion` (aparece estático). El color del veredicto lo da el dato real, no adorna.
 */
export function Reaction({ correct, title, subtitle, onStage = true }: Props) {
  const reduced = useReducedMotion();
  const scale = useRef(new Animated.Value(reduced ? 1 : 0.6)).current;
  const tint = correct ? colors.success : colors.danger;
  const wash = correct ? colors.successWash : colors.dangerWash;

  useEffect(() => {
    if (reduced) {
      scale.setValue(1);
      return;
    }
    scale.setValue(0.6);
    Animated.spring(scale, { toValue: 1, friction: 5, tension: 160, useNativeDriver: true }).start();
  }, [correct, reduced, scale]);

  return (
    <View style={styles.wrap}>
      <Animated.View style={[styles.disc, { backgroundColor: wash }, { transform: [{ scale }] }]}>
        <Icon name={correct ? 'check' : 'x'} size={44} color={tint} />
      </Animated.View>
      <View style={styles.text}>
        <AppText variant="headline" color={onStage ? game.onStage : colors.ink}>
          {title}
        </AppText>
        {subtitle ? (
          <AppText variant="body" color={onStage ? game.onStageMuted : colors.muted}>
            {subtitle}
          </AppText>
        ) : null}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    alignItems: 'center',
    gap: spacing.md,
  },
  disc: {
    width: 88,
    height: 88,
    borderRadius: radius.pill,
    alignItems: 'center',
    justifyContent: 'center',
  },
  text: {
    alignItems: 'center',
    gap: spacing.xs,
  },
});
