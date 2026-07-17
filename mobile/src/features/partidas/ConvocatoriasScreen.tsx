import React, { useCallback, useEffect, useRef, useState } from "react";
import { ActivityIndicator, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice, ScreenHeader } from "../../shared/ui";
import { colors, spacing } from "../../shared/theme";
import { fetchConvocatorias, responderConvocatoria } from "./convocatoriasFlow.js";
import { crearSesionHub } from "./sesionHub.js";
import { useNombres } from "../shared/useNombres.js";

type Convocatoria = {
  convocatoriaId: string;
  partidaId: string;
  equipoId: string;
  fechaEnvio: string;
  nombrePartida: string;
};

type Props = { apiBaseUrl: string; token: string };

// fetchConvocatorias/responderConvocatoria viven en un .js sin checkJs: TS pierde el discriminante
// literal de "ok" al inferir su tipo exportado (mismo patrón que PartidaLobbyScreen).
type ConvocatoriasResult =
  | { ok: true; data: Convocatoria[] }
  | { ok: false; type: string; message?: string };

type ResponderResult = { ok: true; data?: unknown } | { ok: false; type: string; message?: string };

export function ConvocatoriasScreen({ apiBaseUrl, token }: Props) {
  const [convocatorias, setConvocatorias] = useState<Convocatoria[]>([]);
  const nombreDe = useNombres(
    { participanteIds: [], equipoIds: convocatorias.map((c) => c.equipoId) },
    apiBaseUrl,
    token
  );
  const [loading, setLoading] = useState(true);
  const [actionId, setActionId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const load = useCallback(async () => {
    setErrorMessage(null);
    const r = (await fetchConvocatorias({ apiBaseUrl, token, fetchImpl: undefined })) as ConvocatoriasResult;
    if (!r.ok) {
      setErrorMessage(r.message ?? "No se pudieron cargar las convocatorias.");
      return;
    }
    setConvocatorias(r.data);
  }, [apiBaseUrl, token]);

  useEffect(() => {
    (async () => {
      setLoading(true);
      await load();
      setLoading(false);
    })();
  }, [load]);

  // El token va por ref: un refresh de sesion (RNF-24) no debe derribar la conexion viva.
  const tokenRef = useRef(token);
  tokenRef.current = token;
  const loadRef = useRef(load);
  loadRef.current = load;

  useEffect(() => {
    const hub = crearSesionHub(apiBaseUrl, () => tokenRef.current);
    // Sin SuscribirAPartida: no hay partida que mirar. El canal personal se activa al conectar,
    // y con el llega tanto el push en vivo como el re-push de pendientes de OnConnectedAsync.
    hub.on("ConvocatoriaCreada", () => void loadRef.current());
    hub.start().catch(() => {
      // Degradacion deliberada: la pantalla sigue siendo operativa con su carga inicial.
    });
    return () => {
      void hub.stop().catch(() => {});
    };
  }, [apiBaseUrl]);

  async function onResponder(convocatoriaId: string, aceptar: boolean) {
    setActionId(convocatoriaId);
    setErrorMessage(null);
    setFeedback(null);
    const r = (await responderConvocatoria({
      apiBaseUrl, token, convocatoriaId, aceptar, fetchImpl: undefined,
    })) as ResponderResult;
    setActionId(null);
    if (!r.ok) {
      setErrorMessage(r.message ?? "No se pudo responder la convocatoria.");
      return;
    }
    setFeedback(aceptar ? "Convocatoria aceptada. ¡Nos vemos en el lobby!" : "Convocatoria rechazada.");
    setConvocatorias((prev) => prev.filter((c) => c.convocatoriaId !== convocatoriaId));
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <ScreenHeader title="Convocatorias" subtitle="Tu equipo te espera" />
      {errorMessage ? <Notice variant="error">{errorMessage}</Notice> : null}
      {feedback ? <Notice variant="success">{feedback}</Notice> : null}
      {loading ? <ActivityIndicator color={colors.primaryBright} style={styles.spinner} /> : null}
      {!loading && convocatorias.length === 0 ? (
        <Card>
          <AppText style={styles.empty}>No tienes convocatorias pendientes.</AppText>
        </Card>
      ) : null}
      {convocatorias.map((c) => (
        <Card key={c.convocatoriaId}>
          <AppText variant="bodyStrong">{c.nombrePartida}</AppText>
          <AppText>Equipo {nombreDe(c.equipoId)}</AppText>
          <View style={styles.acciones}>
            <Button
              label="Aceptar"
              onPress={() => void onResponder(c.convocatoriaId, true)}
              disabled={actionId === c.convocatoriaId}
            />
            <Button
              label="Rechazar"
              variant="secondary"
              onPress={() => void onResponder(c.convocatoriaId, false)}
              disabled={actionId === c.convocatoriaId}
            />
          </View>
        </Card>
      ))}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  content: { padding: spacing.xl, gap: spacing.lg },
  spinner: { marginTop: spacing.lg },
  empty: { color: colors.muted, textAlign: "center" },
  acciones: { flexDirection: "row", gap: spacing.sm, marginTop: spacing.sm },
});
