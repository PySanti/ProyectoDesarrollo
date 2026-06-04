import React, { useMemo, useState } from "react";
import { ActivityIndicator, Pressable, SafeAreaView, StyleSheet, Text, TextInput, View } from "react-native";
import { screenStyles } from "../../shared/styles";
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
  safeArea: screenStyles.safeArea,
  container: screenStyles.container,
  title: screenStyles.title,
  description: screenStyles.description,
  label: screenStyles.label,
  input: screenStyles.input,
  error: screenStyles.error,
  success: screenStyles.success,
  button: screenStyles.primaryButton,
  buttonDisabled: screenStyles.primaryButtonDisabled,
  buttonText: screenStyles.primaryButtonText,
});
