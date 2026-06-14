import React, { useMemo, useState } from "react";
import { SafeAreaView, ScrollView, StyleSheet } from "react-native";
import { Button, Card, Field, Notice, ScreenHeader } from "../../shared/ui";
import { colors, fonts, spacing } from "../../shared/theme";
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
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <ScreenHeader
          title="Unirse a equipo"
          subtitle="Ingresa el codigo de acceso que te compartio el lider del equipo."
        />
        <Card>
          <Field
            label="Codigo de acceso"
            value={accessCode}
            onChangeText={setAccessCode}
            placeholder="Ej. ABCD1234"
            autoCapitalize="characters"
            autoCorrect={false}
            editable={!loading}
            style={styles.codeInput}
          />
          {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
          {successMessage ? <Notice variant="success">{successMessage}</Notice> : null}
          <Button label="Unirme al equipo" onPress={handleSubmit} disabled={!canSubmit} loading={loading} />
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
  codeInput: {
    fontFamily: fonts.mono,
    letterSpacing: 2,
  },
});
