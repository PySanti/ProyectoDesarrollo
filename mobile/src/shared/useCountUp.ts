import { useEffect, useRef, useState } from 'react';
import { Animated, Easing } from 'react-native';
import { useReducedMotion } from './useReducedMotion';

/**
 * Anima un entero de 0 → `target` (efecto "count-up" para puntajes y conteos que se "llenan").
 * Presentacional: no posee ningún dato, sólo anima el valor real que recibe. Respeta
 * `prefers-reduced-motion` saltando directo al valor final. `useNativeDriver: false` porque se
 * lee el valor en JS para mostrar el entero.
 */
export function useCountUp(target: number, duration = 700): number {
  const reduced = useReducedMotion();
  const [display, setDisplay] = useState(target);
  const anim = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    if (reduced || !Number.isFinite(target)) {
      setDisplay(target);
      return;
    }
    anim.setValue(0);
    const id = anim.addListener(({ value }) => setDisplay(Math.round(value)));
    Animated.timing(anim, {
      toValue: target,
      duration,
      easing: Easing.out(Easing.cubic),
      useNativeDriver: false,
    }).start();
    return () => anim.removeListener(id);
  }, [target, duration, reduced, anim]);

  return display;
}
