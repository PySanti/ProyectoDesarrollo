import React from 'react';
import { StyleSheet, View } from 'react-native';
import { colors, game, spacing } from '../theme';
import { AppText } from './AppText';

interface Props {
  label: string;
  value: string;
  /** Sobre un `Stage`/`Panel` de color: usa texto claro (`onStage*`) en vez de tinta. */
  onStage?: boolean;
}

/** Fila etiqueta → valor para paneles de detalle (espejo del `.detail-grid` de la web). */
export function DetailRow({ label, value, onStage }: Props) {
  return (
    <View style={styles.row}>
      <AppText variant="label" color={onStage ? game.onStageMuted : colors.muted}>
        {label}
      </AppText>
      <AppText variant="bodyStrong" color={onStage ? game.onStage : undefined} style={styles.value}>
        {value}
      </AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: spacing.md,
  },
  value: {
    flexShrink: 1,
    textAlign: 'right',
  },
});
