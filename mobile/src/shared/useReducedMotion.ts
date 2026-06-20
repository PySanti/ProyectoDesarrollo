import { useEffect, useState } from 'react';
import { AccessibilityInfo } from 'react-native';

/**
 * `true` si el usuario pidió reducir movimiento. Base para dar alternativa estática a toda
 * animación del registro de juego. Usa `AccessibilityInfo` (sin dependencias nativas extra).
 */
export function useReducedMotion(): boolean {
  const [reduced, setReduced] = useState(false);

  useEffect(() => {
    let mounted = true;
    AccessibilityInfo.isReduceMotionEnabled().then((r) => {
      if (mounted) setReduced(r);
    });
    const sub = AccessibilityInfo.addEventListener('reduceMotionChanged', setReduced);
    return () => {
      mounted = false;
      sub.remove();
    };
  }, []);

  return reduced;
}
