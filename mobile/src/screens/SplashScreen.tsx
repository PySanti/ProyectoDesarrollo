import React, { useEffect, useRef } from "react";
import { ActivityIndicator, Animated, Easing, StyleSheet, View } from "react-native";
import { AppText, Stage } from "../shared/ui";
import { game, spacing } from "../shared/theme";
import { useReducedMotion } from "../shared/useReducedMotion";

/**
 * Pantalla de carga inmersiva (registro de juego). Stage magenta con entrada animada del wordmark
 * (API `Animated` nativa, sin dependencias nativas extra). Se muestra mientras cargan las fuentes y
 * al restaurar sesión; legible aun sin las fuentes de marca. Respeta `prefers-reduced-motion`.
 */
export function SplashScreen() {
  const progress = useRef(new Animated.Value(0)).current;
  const reduced = useReducedMotion();

  useEffect(() => {
    if (reduced) {
      progress.setValue(1);
      return;
    }
    Animated.timing(progress, {
      toValue: 1,
      duration: 440,
      easing: Easing.out(Easing.cubic),
      useNativeDriver: true,
    }).start();
  }, [reduced, progress]);

  const brandStyle = {
    opacity: progress,
    transform: [
      {
        translateY: progress.interpolate({ inputRange: [0, 1], outputRange: [14, 0] }),
      },
    ],
  };

  return (
    <Stage variant="magenta" gradient>
      <View style={styles.center}>
        <Animated.View style={brandStyle}>
          <AppText variant="mega" color={game.onStage} style={styles.wordmark}>
            UMBRAL
          </AppText>
        </Animated.View>
        <ActivityIndicator color={game.onStage} />
      </View>
    </Stage>
  );
}

const styles = StyleSheet.create({
  center: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    gap: spacing.xl,
  },
  wordmark: {
    fontSize: 52,
    lineHeight: 56,
    letterSpacing: -1.5,
    textAlign: "center",
    // En Android, letterSpacing negativo sub-mide el ancho de línea y recorta el último glifo
    // ("UMBRAL" → "UMBRA"). El padding extiende el frame y deja sitio a la "L". Aquí va `md`
    // (no `sm`) porque la "mega" usa el tracking más agresivo (-1.5) y el glifo más grande.
    paddingHorizontal: spacing.md,
  },
});
