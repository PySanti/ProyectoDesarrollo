import React from 'react';
import { StyleSheet, View, ViewStyle } from 'react-native';
import { colors, fonts, radius, spacing } from '../theme';
import { AppText } from './AppText';

type Variant = 'error' | 'success' | 'info';

interface Props {
  variant: Variant;
  children: React.ReactNode;
  style?: ViewStyle;
  testID?: string;
}

/**
 * Aviso en bloque (error / éxito / info): lavado de fondo + borde 1px + texto del color
 * correspondiente. Si `children` es texto, se renderiza con énfasis; si es nodo, tal cual.
 */
export function Notice({ variant, children, style, testID }: Props) {
  const m = MAP[variant];
  return (
    <View testID={testID} style={[styles.notice, { backgroundColor: m.bg, borderColor: m.border }, style]}>
      {typeof children === 'string' ? (
        <AppText variant="body" color={m.fg} style={styles.text}>
          {children}
        </AppText>
      ) : (
        children
      )}
    </View>
  );
}

const MAP: Record<Variant, { bg: string; fg: string; border: string }> = {
  error: { bg: colors.dangerWash, fg: colors.danger, border: colors.danger },
  success: { bg: colors.successWash, fg: '#136530', border: colors.success },
  info: { bg: colors.accentWash, fg: '#2c4790', border: colors.accent },
};

const styles = StyleSheet.create({
  notice: {
    borderWidth: 1,
    borderRadius: radius.md,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
  },
  text: {
    fontFamily: fonts.semibold,
  },
});
