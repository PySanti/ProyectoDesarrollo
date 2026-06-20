import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, typography } from '../theme';
import { AppText } from './AppText';

interface Props {
  value: string | number;
  /** Etiqueta bajo el número (sentence case). */
  label?: string;
  color?: string;
  /** `mega` (64) por defecto, `hero` (40) para contextos más contenidos. */
  size?: 'mega' | 'hero';
  /** Color de la etiqueta (útil sobre stage). */
  labelColor?: string;
  align?: 'center' | 'flex-start';
}

/**
 * Número protagonista (puntaje, cuenta regresiva, posición). Tipografía display 700, gigante.
 * El número es el foco; la etiqueta lo contextualiza sin competir.
 */
export function BigNumber({ value, label, color = colors.ink, size = 'mega', labelColor = colors.muted, align = 'center' }: Props) {
  return (
    <View style={[styles.wrap, { alignItems: align }]}>
      <Text style={[typography[size], { color }]} allowFontScaling={false}>
        {value}
      </Text>
      {label ? (
        <AppText variant="label" color={labelColor}>
          {label}
        </AppText>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    gap: 2,
  },
});
