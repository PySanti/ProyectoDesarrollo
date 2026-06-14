import React from 'react';
import { StyleSheet, View } from 'react-native';
import { colors, spacing } from '../theme';
import { AppText } from './AppText';

interface Props {
  label: string;
  value: string;
}

/** Fila etiqueta → valor para paneles de detalle (espejo del `.detail-grid` de la web). */
export function DetailRow({ label, value }: Props) {
  return (
    <View style={styles.row}>
      <AppText variant="label" color={colors.muted}>
        {label}
      </AppText>
      <AppText variant="bodyStrong" style={styles.value}>
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
