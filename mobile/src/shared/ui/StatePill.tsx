import React, { useEffect, useRef, useState } from 'react';
import { AccessibilityInfo, Animated, Easing, StyleSheet, View } from 'react-native';
import { colors, radius, spacing } from '../theme';
import { AppText } from './AppText';

export type PillState = 'live' | 'lobby' | 'done' | 'ok' | 'warn' | 'cancel';

interface Props {
  state: PillState;
  label: string;
}

/**
 * Chip de estado: lavado de fondo + punto + etiqueta. Codifica estado con **color + texto
 * + forma** (regla State Is Never Color Alone): el punto varía de relleno (live/ok) a anillo
 * hueco (lobby) y la etiqueta siempre acompaña. El estado "En vivo" **late** (halo magenta),
 * con alternativa estática cuando hay `prefers-reduced-motion`.
 */
export function StatePill({ state, label }: Props) {
  const s = MAP[state];
  return (
    <View style={[styles.pill, { backgroundColor: s.wash }]}>
      {state === 'live' ? (
        <LiveDot color={s.dot} />
      ) : (
        <View style={[styles.dot, s.hollow ? { borderColor: s.dot, borderWidth: 2 } : { backgroundColor: s.dot }]} />
      )}
      <AppText variant="label" color={s.fg}>
        {label}
      </AppText>
    </View>
  );
}

/** Punto "en vivo" con halo que late; respeta `prefers-reduced-motion` (queda estático). */
function LiveDot({ color }: { color: string }) {
  const progress = useRef(new Animated.Value(0)).current;
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

  useEffect(() => {
    if (reduced) return;
    const loop = Animated.loop(
      Animated.timing(progress, {
        toValue: 1,
        duration: 1500,
        easing: Easing.out(Easing.ease),
        useNativeDriver: true,
      }),
    );
    loop.start();
    return () => loop.stop();
  }, [reduced, progress]);

  return (
    <View style={styles.dotWrap}>
      {reduced ? null : (
        <Animated.View
          style={[
            styles.halo,
            {
              backgroundColor: color,
              opacity: progress.interpolate({ inputRange: [0, 1], outputRange: [0.45, 0] }),
              transform: [{ scale: progress.interpolate({ inputRange: [0, 1], outputRange: [1, 2.6] }) }],
            },
          ]}
        />
      )}
      <View style={[styles.dot, { backgroundColor: color }]} />
    </View>
  );
}

const MAP: Record<PillState, { wash: string; fg: string; dot: string; hollow?: boolean }> = {
  live: { wash: colors.stateLiveWash, fg: colors.primaryStrong, dot: colors.stateLive },
  lobby: { wash: colors.stateLobbyWash, fg: '#2c4790', dot: colors.stateLobby, hollow: true },
  done: { wash: colors.stateDoneWash, fg: colors.muted, dot: colors.stateDone },
  ok: { wash: colors.successWash, fg: '#136530', dot: colors.success },
  warn: { wash: colors.warningWash, fg: colors.warningInk, dot: colors.warning },
  cancel: { wash: colors.dangerWash, fg: colors.danger, dot: colors.danger },
};

const styles = StyleSheet.create({
  pill: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: spacing.xs + 2,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs + 1,
    borderRadius: radius.pill,
  },
  dotWrap: {
    width: 8,
    height: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  halo: {
    position: 'absolute',
    width: 8,
    height: 8,
    borderRadius: radius.pill,
  },
  dot: {
    width: 8,
    height: 8,
    borderRadius: radius.pill,
  },
});
