import React, { useMemo, useState } from "react";
import { SafeAreaView, ScrollView, StyleSheet } from "react-native";
import { Button, Card, Field, Notice, ScreenHeader } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
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
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <ScreenHeader title="Crear equipo" subtitle="Dale un nombre a tu equipo para empezar a jugar." />
        <Card>
          <Field
            label="Nombre del equipo"
            value={teamName}
            onChangeText={setTeamName}
            placeholder="Ej. Exploradores"
            autoCapitalize="words"
            editable={!loading}
          />
          {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
          {successMessage ? <Notice variant="success">{successMessage}</Notice> : null}
          <Button label="Crear equipo" onPress={handleSubmit} disabled={!canSubmit} loading={loading} />
        </Card>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: colors.bg,
  },
  content: {
    padding: spacing.xl,
    gap: spacing.lg,
  },
});
