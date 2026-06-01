import React, { useState } from "react";
import { Pressable, SafeAreaView, StyleSheet, Text, View } from "react-native";
import { useAuth } from "../auth/AuthProvider";

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
  safeArea: {
    flex: 1,
    backgroundColor: "#f4f7fb",
  },
  container: {
    flex: 1,
    justifyContent: "center",
    padding: 24,
    gap: 14,
  },
  title: {
    fontSize: 32,
    fontWeight: "700",
    color: "#0f172a",
  },
  subtitle: {
    color: "#334155",
    fontSize: 15,
  },
  error: {
    color: "#b91c1c",
  },
  button: {
    marginTop: 12,
    borderRadius: 10,
    backgroundColor: "#0b5fff",
    paddingVertical: 12,
    alignItems: "center",
  },
  buttonText: {
    color: "#fff",
    fontWeight: "700",
    fontSize: 15,
  },
});
