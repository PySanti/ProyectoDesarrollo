import React from 'react';
import { StyleProp, StyleSheet, View, ViewStyle } from 'react-native';
import { game, radius, spacing } from '../theme';

interface Props {
  children: React.ReactNode;
  style?: StyleProp<ViewStyle>;
}

/**
 * Tarjeta translúcida ("glass") para contenido sobre un `Stage` de color: relleno y borde claros
 * (`onStageSunk`/`onStageLine`) que dejan ver el fondo inmersivo. Equivalente a `Card`, pero para
 * superficies de color en vez del blanco. El texto encima va en `game.onStage*`.
 */
export function Panel({ children, style }: Props) {
  return <View style={[styles.panel, style]}>{children}</View>;
}

const styles = StyleSheet.create({
  panel: {
    backgroundColor: game.onStageSunk,
    borderWidth: 1,
    borderColor: game.onStageLine,
    borderRadius: radius.lg,
    padding: spacing.lg,
    gap: spacing.md,
  },
});
