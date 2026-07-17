import React, { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, Pressable, SafeAreaView, ScrollView, StyleSheet, Text, View } from "react-native";
import { Card, Icon, Notice, ScreenHeader } from "../../shared/ui";
import { colors, fonts, radius, spacing, typography } from "../../shared/theme";
import { fetchEligibleParticipants, submitInviteMember } from "./inviteMemberFlow.js";

type EligibleParticipant = {
  userId: string;
  nombre?: string;
  email?: string;
  yaInvitado?: boolean;
};

type InviteMemberScreenProps = {
  apiBaseUrl: string;
  token: string;
  onInvited?: (result: unknown) => void;
};

export function InviteMemberScreen({ apiBaseUrl, token, onInvited }: InviteMemberScreenProps) {
  const [participants, setParticipants] = useState<EligibleParticipant[]>([]);
  const [selected, setSelected] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setErrorMessage(null);
    const result = await fetchEligibleParticipants({ apiBaseUrl, token, fetchImpl: undefined });
    setLoading(false);
    if (!result.ok) {
      setErrorMessage(result.message ?? "No se pudieron cargar los participantes.");
      return;
    }
    setParticipants(result.data as EligibleParticipant[]);
  }, [apiBaseUrl, token]);

  useEffect(() => {
    load();
  }, [load]);

  async function handleInvite() {
    if (!selected) return;
    setSubmitting(true);
    setErrorMessage(null);
    setSuccessMessage(null);
    const result = await submitInviteMember({ apiBaseUrl, token, invitadoUserId: selected, fetchImpl: undefined });
    setSubmitting(false);
    if (!result.ok) {
      setErrorMessage(result.message ?? "No se pudo enviar la invitacion.");
      return;
    }
    setSuccessMessage("Invitacion enviada con exito.");
    // Refleja el estado "Ya invitado" al instante, sin recargar el panel.
    setParticipants((prev) =>
      prev.map((p) => (p.userId === selected ? { ...p, yaInvitado: true } : p))
    );
    setSelected(null);
    onInvited?.(result.data);
  }

  const canSubmit = selected !== null && !submitting;

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <ScreenHeader
          title="Invitar miembro"
          subtitle="Selecciona un participante para invitar a tu equipo."
        />
        {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
        {successMessage ? <Notice variant="success">{successMessage}</Notice> : null}
        {loading ? (
          <ActivityIndicator color={colors.primaryBright} size="large" />
        ) : participants.length === 0 ? (
          <Card>
            <Text style={styles.empty}>No hay participantes elegibles para invitar.</Text>
          </Card>
        ) : (
          <Card>
            {participants.map((p) => {
              const invited = p.yaInvitado === true;
              return (
                <Pressable
                  key={p.userId}
                  style={[
                    styles.item,
                    selected === p.userId && styles.itemSelected,
                    invited && styles.itemInvited,
                  ]}
                  onPress={() => setSelected(p.userId)}
                  disabled={submitting || invited}
                >
                  <Icon
                    name={invited ? "check-circle" : "plus-circle"}
                    size={22}
                    color={invited ? colors.success : colors.primaryBright}
                  />
                  <View style={styles.itemBody}>
                    <Text style={styles.itemName}>{p.nombre ?? p.email ?? p.userId}</Text>
                    {p.email && p.nombre ? <Text style={styles.itemEmail}>{p.email}</Text> : null}
                  </View>
                  {invited ? <Text style={styles.invitedTag}>Ya invitado</Text> : null}
                </Pressable>
              );
            })}
            <Pressable
              style={[styles.button, !canSubmit && styles.buttonDisabled]}
              onPress={handleInvite}
              disabled={!canSubmit}
            >
              <Text style={styles.buttonText}>{submitting ? "Enviando..." : "Enviar invitacion"}</Text>
            </Pressable>
          </Card>
        )}
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
  empty: {
    ...typography.body,
    color: colors.muted,
    textAlign: "center",
  },
  item: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.line,
    borderRadius: radius.md,
    padding: spacing.md,
    marginBottom: spacing.sm,
    minHeight: 48,
  },
  itemSelected: {
    borderColor: colors.primaryBright,
    backgroundColor: colors.primaryWash,
  },
  itemInvited: {
    backgroundColor: colors.surfaceSunk,
    borderColor: colors.line,
    opacity: 0.7,
  },
  itemBody: {
    flex: 1,
  },
  invitedTag: {
    fontFamily: fonts.semibold,
    fontSize: 12,
    color: colors.success,
  },
  itemName: {
    fontFamily: fonts.semibold,
    fontSize: 15,
    color: colors.ink,
  },
  itemEmail: {
    fontFamily: fonts.body,
    fontSize: 13,
    color: colors.muted,
    marginTop: 2,
  },
  button: {
    minHeight: 48,
    borderRadius: radius.md,
    backgroundColor: colors.primaryFill,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    marginTop: spacing.sm,
  },
  buttonDisabled: {
    backgroundColor: colors.primaryDisabled,
  },
  buttonText: {
    color: colors.white,
    fontFamily: fonts.semibold,
    fontSize: 15,
  },
});
