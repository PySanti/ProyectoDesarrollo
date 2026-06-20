import React, { useEffect, useRef } from 'react';
import { Animated, Easing, StyleSheet, View } from 'react-native';
import { colors, game, typography } from '../theme';
import { useReducedMotion } from '../useReducedMotion';
import { AppText } from './AppText';

interface Props {
  /** Segundos restantes. El componente NO posee el temporizador: lo recibe de quien tiene el dato real. */
  seconds: number;
  /** Umbral ámbar (urgencia). Por defecto 10s. */
  warnAt?: number;
  /** Umbral rojo (crítico). Por defecto 5s. */
  dangerAt?: number;
  /** Etiqueta bajo el número (sentence case). */
  label?: string;
  /** El timer vive sobre un `Stage` de color (el estado normal usa blanco en vez de tinta). */
  onStage?: boolean;
}

function format(total: number): string {
  const s = Math.max(0, Math.floor(total));
  if (s < 60) return String(s);
  const m = Math.floor(s / 60);
  const rem = s % 60;
  return `${m}:${String(rem).padStart(2, '0')}`;
}

/**
 * Cuenta regresiva dramática: número gigante (Space Grotesk 700) que pasa de normal → ámbar →
 * rojo según se agota el tiempo, con un pulso de urgencia bajo el umbral ámbar. Es **presentacional**:
 * recibe los segundos restantes; no corre ningún reloj propio. Respeta `prefers-reduced-motion`
 * (queda estático, el color sigue comunicando la urgencia). Estado = color + número + (pulso).
 */
export function Countdown({ seconds, warnAt = 10, dangerAt = 5, label, onStage = true }: Props) {
  const reduced = useReducedMotion();
  const pulse = useRef(new Animated.Value(1)).current;
  const urgent = seconds <= warnAt;

  const color =
    seconds <= dangerAt ? colors.danger : seconds <= warnAt ? colors.warning : onStage ? game.onStage : colors.ink;

  useEffect(() => {
    if (reduced || !urgent) {
      pulse.setValue(1);
      return;
    }
    const loop = Animated.loop(
      Animated.sequence([
        Animated.timing(pulse, { toValue: 1.12, duration: 320, easing: Easing.out(Easing.ease), useNativeDriver: true }),
        Animated.timing(pulse, { toValue: 1, duration: 320, easing: Easing.in(Easing.ease), useNativeDriver: true }),
      ]),
    );
    loop.start();
    return () => loop.stop();
  }, [reduced, urgent, pulse, seconds]);

  return (
    <View style={styles.wrap}>
      <Animated.Text
        allowFontScaling={false}
        style={[typography.mega, { color }, { transform: [{ scale: pulse }] }]}
      >
        {format(seconds)}
      </Animated.Text>
      {label ? (
        <AppText variant="label" color={onStage ? game.onStageMuted : colors.muted}>
          {label}
        </AppText>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    alignItems: 'center',
    gap: 2,
  },
});
