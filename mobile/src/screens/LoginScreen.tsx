import React, { useState } from "react";
import { Pressable, SafeAreaView, StyleSheet, Text, View } from "react-native";
import { useAuth } from "../auth/AuthProvider";
import { screenStyles } from "../shared/styles";
import { colors, radius, spacing } from "../shared/theme";

export function LoginScreen() {
  const { login } = useAuth();
  const [error, setError] = useState<string | null>(null);

  async function onLogin() {
    setError(null);
    try {
      await login();
    } catch (authError) {
      setError(authError instanceof Error ? authError.message : "No se pudo autenticar.");
    }
  }

  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.container}>
        <View style={styles.heroCard}>
          <Text style={styles.kicker}>Participantes</Text>
          <Text style={styles.title}>UMBRAL</Text>
          <Text style={styles.subtitle}>Trivia y Busqueda del Tesoro en tiempo real desde tu telefono.</Text>
          {error ? <Text style={styles.error}>{error}</Text> : null}
          <Pressable style={styles.button} onPress={onLogin}>
            <Text style={styles.buttonText}>Iniciar sesion</Text>
          </Pressable>
        </View>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: screenStyles.safeArea,
  container: {
    ...screenStyles.container,
    justifyContent: "center",
    backgroundColor: colors.background,
  },
  heroCard: {
    ...screenStyles.card,
    padding: spacing.xxxl,
    borderRadius: radius.xl,
  },
  kicker: {
    color: colors.accent,
    fontSize: 12,
    fontWeight: "800",
    letterSpacing: 1.4,
    textTransform: "uppercase",
  },
  title: {
    ...screenStyles.title,
    fontSize: 38,
    color: colors.primaryDark,
  },
  subtitle: screenStyles.subtitle,
  error: screenStyles.error,
  button: screenStyles.primaryButton,
  buttonText: screenStyles.primaryButtonText,
});
