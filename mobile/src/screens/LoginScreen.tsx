import React, { useState } from "react";
import { Pressable, SafeAreaView, StyleSheet, Text, View } from "react-native";
import { useAuth } from "../auth/AuthProvider";
import { screenStyles } from "../shared/styles";

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
        <Text style={styles.title}>UMBRAL</Text>
        <Text style={styles.subtitle}>Ingresa con tu cuenta de Keycloak para continuar.</Text>
        {error ? <Text style={styles.error}>{error}</Text> : null}
        <Pressable style={styles.button} onPress={onLogin}>
          <Text style={styles.buttonText}>Iniciar sesion</Text>
        </Pressable>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: screenStyles.safeArea,
  container: {
    ...screenStyles.container,
    justifyContent: "center",
    gap: 14,
  },
  title: {
    ...screenStyles.title,
    fontSize: 32,
  },
  subtitle: screenStyles.subtitle,
  error: screenStyles.error,
  button: screenStyles.primaryButton,
  buttonText: screenStyles.primaryButtonText,
});
