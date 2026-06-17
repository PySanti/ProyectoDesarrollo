import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, game, spacing, typography } from '../theme';
import { AppText } from './AppText';

interface Props {
  title: string;
  subtitle?: string;
  /** Texto en blanco/translúcido para usarse sobre un `Stage` de color. */
  onStage?: boolean;
  /** Elemento a la derecha (p. ej. un chip de estado). */
  right?: React.ReactNode;
  /**
   * Tamaño del título. `hero` (40) por defecto para títulos cortos y fijos. Usa `display` (30)
   * o `headline` (22) en cabeceras con texto dinámico/largo (p. ej. "Hola, {usuario}"), donde
   * el tamaño hero se ve desproporcionado.
   */
  titleVariant?: 'hero' | 'display' | 'headline';
}

/**
 * Cabecera dramática del registro de juego: título en `hero` (Space Grotesk 700). Sin kicker en
 * mayúsculas (regla No-Eyebrow): la jerarquía la dan escala y peso.
 */
export function Hero({ title, subtitle, onStage, right, titleVariant = 'hero' }: Props) {
  const titleColor = onStage ? game.onStage : colors.ink;
  const subColor = onStage ? game.onStageMuted : colors.muted;
  return (
    <View style={styles.row}>
      <View style={styles.text}>
        <Text style={[typography[titleVariant], { color: titleColor }]}>{title}</Text>
        {subtitle ? (
          <AppText variant="body" color={subColor}>
            {subtitle}
          </AppText>
        ) : null}
      </View>
      {right}
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: spacing.md,
  },
  text: {
    flex: 1,
    gap: spacing.xs,
  },
});
