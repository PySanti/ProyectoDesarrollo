import React from 'react';
import { StyleSheet, View, ViewStyle } from 'react-native';
import { colors, radius, spacing } from '../theme';

interface Props {
  children: React.ReactNode;
  style?: ViewStyle | ViewStyle[];
  /** Variante hundida para agrupaciones internas (surface-sunk). */
  sunk?: boolean;
}

/**
 * Contenedor de marca. Plano por reposo (regla Flat-By-Default): superficie + borde 1px,
 * sin sombras decorativas. `sunk` para zonas agrupadas dentro de otra card.
 */
export function Card({ children, style, sunk }: Props) {
  return <View style={[styles.card, sunk && styles.sunk, style]}>{children}</View>;
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    borderRadius: radius.lg,
    padding: spacing.lg,
    gap: spacing.sm,
  },
  sunk: {
    backgroundColor: colors.surfaceSunk,
    borderColor: colors.lineStrong,
  },
});
