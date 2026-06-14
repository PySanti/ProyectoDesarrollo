import React, { useRef } from 'react';
import { Animated, GestureResponderEvent, Pressable, PressableProps, StyleProp, ViewStyle } from 'react-native';
import { game } from '../theme';
import { useReducedMotion } from '../useReducedMotion';

interface Props extends PressableProps {
  children: React.ReactNode;
  style?: StyleProp<ViewStyle>;
  /** Escala al presionar (por defecto 0.96). */
  pressedScale?: number;
}

/**
 * Envoltorio táctil con micro-interacción de press (escala con spring vía `Animated` nativo).
 * Respeta `prefers-reduced-motion` (sin animación). Base de los accionables del registro de juego.
 */
export function PressableScale({ children, style, pressedScale = game.motion.pressScale, onPressIn, onPressOut, ...rest }: Props) {
  const scale = useRef(new Animated.Value(1)).current;
  const reduced = useReducedMotion();

  const animateTo = (toValue: number) =>
    Animated.spring(scale, { toValue, useNativeDriver: true, ...game.motion.spring }).start();

  const handleIn = (e: GestureResponderEvent) => {
    if (!reduced) animateTo(pressedScale);
    onPressIn?.(e);
  };
  const handleOut = (e: GestureResponderEvent) => {
    animateTo(1);
    onPressOut?.(e);
  };

  return (
    <Pressable onPressIn={handleIn} onPressOut={handleOut} {...rest}>
      <Animated.View style={[{ transform: [{ scale }] }, style]}>{children}</Animated.View>
    </Pressable>
  );
}
