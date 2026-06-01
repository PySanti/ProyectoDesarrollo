import React, { useMemo, useState } from "react";
import { ActivityIndicator, Pressable, SafeAreaView, StyleSheet, Text, TextInput, View } from "react-native";
import { submitJoinTeamFromScreen } from "./joinTeamScreenModel.js";

type JoinTeamScreenProps = {
  apiBaseUrl: string;
  token: string;
  onJoined?: (result: unknown) => void;
};

export function JoinTeamScreen({ apiBaseUrl, token, onJoined }: JoinTeamScreenProps) {
  const [accessCode, setAccessCode] = useState("");
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const canSubmit = useMemo(() => accessCode.trim().length > 0 && !loading, [accessCode, loading]);

  async function handleSubmit() {
    await submitJoinTeamFromScreen({
      apiBaseUrl,
      token,
      accessCode,
      onJoined,
      setLoading,
      setErrorMessage,
      setSuccessMessage,
      setAccessCode,
    });
  }

  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.container}>
        <Text style={styles.title}>Unirse a equipo</Text>
        <Text style={styles.description}>Ingresa el codigo de acceso que te compartio el lider del equipo.</Text>
        <Text style={styles.label}>Codigo de acceso</Text>
        <TextInput
          value={accessCode}
          onChangeText={setAccessCode}
          placeholder="Ej. ABCD1234"
          autoCapitalize="characters"
          style={styles.input}
          editable={!loading}
        />

        {errorMessage ? <Text style={styles.error}>{errorMessage}</Text> : null}
        {successMessage ? <Text style={styles.success}>{successMessage}</Text> : null}

        <Pressable
          accessibilityRole="button"
          onPress={handleSubmit}
          disabled={!canSubmit}
          style={[styles.button, !canSubmit && styles.buttonDisabled]}
        >
          {loading ? <ActivityIndicator color="#ffffff" /> : <Text style={styles.buttonText}>Unirme al equipo</Text>}
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
    padding: 20,
    gap: 12,
  },
  title: {
    fontSize: 24,
    fontWeight: "700",
    color: "#0f172a",
  },
  description: {
    color: "#475569",
    fontSize: 14,
    lineHeight: 20,
  },
  label: {
    fontSize: 14,
    color: "#334155",
  },
  input: {
    borderWidth: 1,
    borderColor: "#cbd5e1",
    borderRadius: 10,
    backgroundColor: "#ffffff",
    fontSize: 16,
    color: "#0f172a",
    paddingHorizontal: 12,
    paddingVertical: 10,
  },
  error: {
    color: "#b91c1c",
    fontSize: 13,
  },
  success: {
    color: "#166534",
    fontSize: 13,
  },
  button: {
    marginTop: 8,
    borderRadius: 10,
    backgroundColor: "#0b5fff",
    paddingVertical: 12,
    alignItems: "center",
    justifyContent: "center",
  },
  buttonDisabled: {
    backgroundColor: "#93c5fd",
  },
  buttonText: {
    color: "#ffffff",
    fontWeight: "700",
    fontSize: 15,
  },
});
