import React from 'react';
import { ScrollView, StatusBar, StyleSheet, View, ViewStyle } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { LinearGradient } from 'expo-linear-gradient';
import { colors, game, spacing } from '../theme';

type Variant = 'magenta' | 'indigo' | 'ink' | 'plain';

interface Props {
  children: React.ReactNode;
  /** Color de fondo a sangre completa. `plain` = blanco (pantallas no inmersivas). */
  variant?: Variant;
  /** Usa degradado del mismo hue en vez de color plano. */
  gradient?: boolean;
  /** Envuelve el contenido en ScrollView. */
  scroll?: boolean;
  contentStyle?: ViewStyle | ViewStyle[];
}

/**
 * Lienzo inmersivo del registro de juego: fondo de color (o gradiente del mismo hue) a sangre
 * completa con safe area. El contenido sobre `stage` usa texto blanco (ver `game.onStage*`).
 * `plain` lo deja blanco para pantallas que no deban "encenderse".
 */
export function Stage({ children, variant = 'plain', gradient = false, scroll = false, contentStyle }: Props) {
  const inner = scroll ? (
    <ScrollView
      contentContainerStyle={[styles.content, contentStyle]}
      showsVerticalScrollIndicator={false}
      keyboardShouldPersistTaps="handled"
    >
      {children}
    </ScrollView>
  ) : (
    <View style={[styles.content, styles.fill, contentStyle]}>{children}</View>
  );

  const body = (
    <SafeAreaView style={styles.fill}>
      <StatusBar barStyle={variant === 'plain' ? 'dark-content' : 'light-content'} translucent backgroundColor="transparent" />
      {inner}
    </SafeAreaView>
  );

  if (variant === 'plain') {
    return <View style={[styles.fill, { backgroundColor: colors.bg }]}>{body}</View>;
  }

  if (gradient) {
    return (
      <LinearGradient colors={game.gradient[variant]} start={{ x: 0, y: 0 }} end={{ x: 0, y: 1 }} style={styles.fill}>
        {body}
      </LinearGradient>
    );
  }

  return <View style={[styles.fill, { backgroundColor: game.stage[variant] }]}>{body}</View>;
}

const styles = StyleSheet.create({
  fill: {
    flex: 1,
  },
  content: {
    padding: spacing.xl,
    gap: spacing.lg,
  },
});
