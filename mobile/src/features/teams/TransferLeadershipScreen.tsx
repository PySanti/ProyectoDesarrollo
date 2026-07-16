import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, Modal, Pressable, SafeAreaView, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, radius, spacing } from "../../shared/theme";
import { fetchMyTeamStatus } from "./teamPanelFlow.js";
import { getEligibleLeaderMembers } from "./transferLeadershipFlow.js";
import { submitTransferLeadershipFromScreen } from "./transferLeadershipScreenModel.js";
import { FetchTeamStatusResult, Participante } from "./teamTypes";

type TransferLeadershipScreenProps = {
  apiBaseUrl: string;
  token: string;
  currentUserId: string;
  onTransferred?: (result: unknown) => void;
};

export function TransferLeadershipScreen({
  apiBaseUrl,
  token,
  currentUserId,
  onTransferred,
}: TransferLeadershipScreenProps) {
  const [loadingTeam, setLoadingTeam] = useState(true);
  const [teamError, setTeamError] = useState<string | null>(null);
  const [participantes, setParticipantes] = useState<Participante[]>([]);
  const [selectedMember, setSelectedMember] = useState<Participante | null>(null);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const loadTeam = useCallback(async () => {
    setLoadingTeam(true);
    setTeamError(null);
    const result = (await fetchMyTeamStatus({
      apiBaseUrl,
      token,
      currentUserId,
      fetchImpl: undefined,
    })) as FetchTeamStatusResult;
    setLoadingTeam(false);
    if (!result.ok) {
      setTeamError(result.message ?? "No se pudo cargar tu equipo.");
      return;
    }
    if (result.status === "sinEquipo") {
      setTeamError("No pertenecés a ningún equipo activo.");
      return;
    }
    setParticipantes(result.participantes);
  }, [apiBaseUrl, token, currentUserId]);

  useEffect(() => {
    loadTeam();
  }, [loadTeam]);

  const eligibleMembers = getEligibleLeaderMembers(participantes, currentUserId) as Participante[];

  function selectMember(member: Participante) {
    setErrorMessage(null);
    setSelectedMember(member);
  }

  async function handleConfirm() {
    if (!selectedMember) {
      return;
    }
    const result = await submitTransferLeadershipFromScreen({
      apiBaseUrl,
      token,
      nuevoLiderUserId: selectedMember.usuarioId,
      onTransferred,
      setLoading,
      setErrorMessage,
      setSuccessMessage,
    });
    if (result?.ok) {
      setSelectedMember(null);
      loadTeam();
    }
  }

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <ScreenHeader title="Transferir liderazgo" subtitle="Elegí quién será el nuevo líder del equipo." />
        {successMessage ? <Notice variant="success">{successMessage}</Notice> : null}

        {loadingTeam ? (
          <ActivityIndicator color={colors.primaryBright} size="large" />
        ) : teamError ? (
          <View style={styles.group}>
            <Notice variant="error">{teamError}</Notice>
            <Button label="Reintentar" variant="secondary" onPress={loadTeam} />
          </View>
        ) : eligibleMembers.length === 0 ? (
          <Card>
            <AppText variant="body" color={colors.muted}>
              No hay integrantes en el equipo
            </AppText>
          </Card>
        ) : (
          <Card>
            <View style={styles.memberList}>
              {eligibleMembers.map((member) => (
                <Pressable
                  key={member.usuarioId}
                  accessibilityRole="button"
                  onPress={() => selectMember(member)}
                  style={({ pressed }) => [styles.memberRow, pressed && styles.memberRowPressed]}
                >
                  <AppText variant="body">{member.nombre || "Sin nombre"}</AppText>
                </Pressable>
              ))}
            </View>
          </Card>
        )}
      </ScrollView>

      <Modal
        visible={!!selectedMember}
        transparent
        animationType="fade"
        onRequestClose={() => setSelectedMember(null)}
      >
        <View style={styles.backdrop}>
          <Card style={styles.modalCard}>
            <AppText variant="bodyStrong">
              ¿Confirmás transferir el liderazgo a {selectedMember?.nombre || "Sin nombre"}?
            </AppText>
            {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
            <Button label="Transferir liderazgo" onPress={handleConfirm} loading={loading} disabled={loading} />
            <Button
              label="Cancelar"
              variant="secondary"
              onPress={() => {
                setErrorMessage(null);
                setSelectedMember(null);
              }}
              disabled={loading}
            />
          </Card>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, gap: spacing.lg },
  group: { gap: spacing.sm },
  memberList: { gap: spacing.xs },
  memberRow: {
    minHeight: 48,
    justifyContent: "center",
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
  },
  memberRowPressed: {
    borderColor: colors.primaryBright,
    backgroundColor: colors.primaryWash,
  },
  backdrop: {
    flex: 1,
    justifyContent: "center",
    padding: spacing.xl,
    backgroundColor: "rgba(0,0,0,0.55)",
  },
  modalCard: {
    gap: spacing.md,
  },
});
