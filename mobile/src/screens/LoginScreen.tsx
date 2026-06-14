import React, { useState } from "react";
import { SafeAreaView, StyleSheet, View } from "react-native";
import { useAuth } from "../auth/AuthProvider";
import { AppText, Button, Card, Notice } from "../shared/ui";
import { colors, spacing } from "../shared/theme";

export function LoginScreen() {
  const { login } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onLogin() {
    setError(null);
    setBusy(true);
    try {
      await login();
    } catch (authError) {
      setError(authError instanceof Error ? authError.message : "No se pudo autenticar.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <SafeAreaView style={styles.safe}>
      <View style={styles.container}>
        <View style={styles.brand}>
          <AppText variant="display" color={colors.primaryStrong} style={styles.wordmark}>
            UMBRAL
          </AppText>
          <AppText variant="body" color={colors.muted}>
            Trivia y Búsqueda del Tesoro en tiempo real, desde tu teléfono.
          </AppText>
        </View>

        <Card>
          <AppText variant="title">Te damos la bienvenida</AppText>
          <AppText variant="body">
            Inicia sesión para unirte a tu equipo y entrar a las partidas publicadas.
          </AppText>
          {error ? <Notice variant="error">{error}</Notice> : null}
          <Button label="Iniciar sesión" onPress={onLogin} loading={busy} />
        </Card>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: colors.bg,
  },
  container: {
    flex: 1,
    justifyContent: "center",
    padding: spacing.xl,
    gap: spacing.xxl,
  },
  brand: {
    gap: spacing.sm,
  },
  wordmark: {
    fontSize: 44,
    lineHeight: 48,
    letterSpacing: -1,
  },
});
