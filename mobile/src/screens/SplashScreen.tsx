import React from "react";
import { ActivityIndicator, SafeAreaView, StyleSheet, View } from "react-native";
import { AppText } from "../shared/ui";
import { colors, spacing } from "../shared/theme";

/**
 * Pantalla de carga. Se muestra en dos momentos: mientras cargan las fuentes (gate en
 * `App.tsx`, aún con fuente del sistema) y mientras se restaura la sesión. Por eso se mantiene
 * legible sin depender de las fuentes de marca.
 */
export function SplashScreen() {
  return (
    <SafeAreaView style={styles.safe}>
      <View style={styles.center}>
        <AppText variant="display" color={colors.primaryStrong}>
          UMBRAL
        </AppText>
        <ActivityIndicator size="large" color={colors.primaryFill} />
        <AppText variant="body" color={colors.muted}>
          Cargando…
        </AppText>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: colors.bg,
  },
  center: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    gap: spacing.lg,
  },
});
