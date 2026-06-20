import React, { useState } from "react";
import { SafeAreaView, StyleSheet, View } from "react-native";
import { useAuth } from "../auth/AuthProvider";
import { AuthError } from "../auth/keycloakMobileAuth";
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
      // Nunca mostramos el mensaje crudo de la librería/Keycloak (puede venir en inglés):
      // AuthError ya trae el texto en español; cualquier otro caso usa un genérico en español.
      if (authError instanceof AuthError) {
        // Cancelación voluntaria del usuario: no es un error que deba mostrarse.
        setError(authError.cancelled ? null : authError.message);
      } else {
        setError("No se pudo iniciar sesión. Intenta de nuevo.");
      }
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
    // En Android, letterSpacing negativo sub-mide el ancho de línea y recorta el último glifo
    // ("UMBRAL" → "UMBRA"). El padding extiende el frame del texto y deja sitio a la "L".
    paddingRight: spacing.sm,
  },
});
