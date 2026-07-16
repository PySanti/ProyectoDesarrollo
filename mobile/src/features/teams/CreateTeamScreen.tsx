import React, { useMemo, useState } from "react";
import { SafeAreaView, ScrollView, StyleSheet } from "react-native";
import { Button, Card, Field, Notice, ScreenHeader } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
import { submitCreateTeam } from "./createTeamFlow.js";
import { nombreEquipo } from "../../shared/validation.js";

type CreateTeamScreenProps = {
  apiBaseUrl: string;
  token: string;
  onCreated?: (result: unknown) => void;
};

export function CreateTeamScreen({ apiBaseUrl, token, onCreated }: CreateTeamScreenProps) {
  const [teamName, setTeamName] = useState("");
  const [touched, setTouched] = useState(false);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const validationError = nombreEquipo(teamName) as string | null;
  // Formato valida en vivo (valor no vacio); "obligatorio" recien al salir del campo.
  const fieldError = teamName.trim() !== "" || touched ? validationError : null;
  const canSubmit = useMemo(
    () => validationError === null && !loading,
    [validationError, loading]
  );

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
    setTouched(false);
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
            onBlur={() => setTouched(true)}
            error={fieldError}
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
