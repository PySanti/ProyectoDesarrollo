import React from 'react';
import { StyleSheet, Text, TextProps, View } from 'react-native';
import { colors, radius, spacing, typography } from '../theme';

interface Props extends TextProps {
  children: React.ReactNode;
  /** Renderiza como chip (fondo hundido) para resaltar un código/identificador. */
  chip?: boolean;
}

/**
 * Texto monoespaciado para identificadores, códigos de acceso y QR (la regla Mono For
 * Machine Strings). `chip` lo envuelve en una pastilla de superficie hundida.
 */
export function Mono({ children, chip, style, ...rest }: Props) {
  const text = (
    <Text {...rest} style={[typography.mono, style]}>
      {children}
    </Text>
  );
  if (!chip) return text;
  return <View style={styles.chip}>{text}</View>;
}

const styles = StyleSheet.create({
  chip: {
    alignSelf: 'flex-start',
    backgroundColor: colors.surfaceSunk,
    borderRadius: radius.sm,
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.xs / 2,
  },
});
