import React from 'react';
import { StyleSheet, View } from 'react-native';
import { colors, radius, spacing } from '../theme';
import { AppText } from './AppText';

interface Props {
  title: string;
  message: string;
  /** Acción opcional (p. ej. un Button) que enseña a dónde ir. */
  action?: React.ReactNode;
  /** Glifo/decoración opcional sobre el título. */
  icon?: React.ReactNode;
}

/**
 * Empty state que **enseña**: panel punteado con título + frase concreta + acción opcional,
 * nunca solo "no hay nada". Espejo del `.empty-panel` de la web.
 */
export function EmptyPanel({ title, message, action, icon }: Props) {
  return (
    <View style={styles.panel}>
      {icon}
      <AppText variant="title" color={colors.inkSoft}>
        {title}
      </AppText>
      <AppText variant="body" color={colors.muted} style={styles.message}>
        {message}
      </AppText>
      {action}
    </View>
  );
}

const styles = StyleSheet.create({
  panel: {
    borderWidth: 1,
    borderStyle: 'dashed',
    borderColor: colors.lineStrong,
    borderRadius: radius.lg,
    backgroundColor: colors.surface,
    paddingVertical: spacing.xxl,
    paddingHorizontal: spacing.xl,
    alignItems: 'center',
    gap: spacing.sm,
  },
  message: {
    textAlign: 'center',
  },
});
