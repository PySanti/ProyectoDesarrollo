import React from 'react';
import { StyleSheet, View } from 'react-native';
import { colors, spacing } from '../theme';
import { AppText } from './AppText';

interface Props {
  title: string;
  subtitle?: string;
  /** Elemento a la derecha del título (p. ej. un chip de rol o estado). */
  right?: React.ReactNode;
}

/**
 * Cabecera de pantalla: título en display + subtítulo opcional. Sin kicker en mayúsculas
 * (regla No-Eyebrow): la jerarquía la dan escala y peso.
 */
export function ScreenHeader({ title, subtitle, right }: Props) {
  return (
    <View style={styles.head}>
      <View style={styles.text}>
        <AppText variant="display">{title}</AppText>
        {subtitle ? (
          <AppText variant="body" color={colors.muted}>
            {subtitle}
          </AppText>
        ) : null}
      </View>
      {right}
    </View>
  );
}

const styles = StyleSheet.create({
  head: {
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
