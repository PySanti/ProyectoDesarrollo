import React, { useMemo, useState } from "react";
import { ActivityIndicator, Pressable, SafeAreaView, StyleSheet, Text, TextInput, View } from "react-native";
import { screenStyles } from "../../shared/styles";
import { submitCreateTeam } from "./createTeamFlow.js";

type CreateTeamScreenProps = {
  apiBaseUrl: string;
  token: string;
  onCreated?: (result: unknown) => void;
};

export function CreateTeamScreen({ apiBaseUrl, token, onCreated }: CreateTeamScreenProps) {
  const [teamName, setTeamName] = useState("");
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const canSubmit = useMemo(() => teamName.trim().length > 0 && !loading, [teamName, loading]);

  async function handleSubmit() {
    setLoading(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    let result;
    try {
      result = await submitCreateTeam({
        apiBaseUrl,
        token,
        teamName,
      });
    } catch {
      setLoading(false);
      setErrorMessage("Ocurrio un error inesperado. Intenta nuevamente.");
      return;
    }

    setLoading(false);

    if (!result.ok) {
      setErrorMessage(result.message ?? "No se pudo crear el equipo.");
      return;
    }

    setTeamName("");
    setSuccessMessage("Equipo creado con exito.");
    onCreated?.(result.data);
  }

  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.container}>
        <Text style={styles.title}>Crear equipo</Text>
        <Text style={styles.label}>Nombre del equipo</Text>
        <TextInput
          value={teamName}
          onChangeText={setTeamName}
          placeholder="Ej. Exploradores"
          autoCapitalize="words"
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
          {loading ? <ActivityIndicator color="#ffffff" /> : <Text style={styles.buttonText}>Crear equipo</Text>}
        </Pressable>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: screenStyles.safeArea,
  container: screenStyles.container,
  title: screenStyles.title,
  label: screenStyles.label,
  input: screenStyles.input,
  error: screenStyles.error,
  success: screenStyles.success,
  button: screenStyles.primaryButton,
  buttonDisabled: screenStyles.primaryButtonDisabled,
  buttonText: screenStyles.primaryButtonText,
});
