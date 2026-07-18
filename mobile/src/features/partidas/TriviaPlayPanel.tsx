// Gameplay Trivia del participante: pregunta activa + responder una vez + ranking en vivo.
import React, { useCallback, useEffect, useRef, useState } from "react";
import { StyleSheet, View } from "react-native";
import { AppText, Button, Card, Notice } from "../../shared/ui";
import { spacing } from "../../shared/theme";
import {
  getPreguntaActual, responderPregunta, getRankingJuego, formatRespuestaCorrecta, seleccionarRespuestaCorrecta,
  aplicaRespuestaEquipo,
} from "./gameplayApi.js";
import { enviarRespuestaTrivia } from "./partidaLiveFlow.js";
import { Countdown, RankingTable, type RankingEntrada } from "./liveShared";
import { idsDeCompetidores } from "./liveLabels.js";
import { useNombres } from "../shared/useNombres.js";

// El reveal "La respuesta correcta era X" se muestra este tiempo y se auto-borra, con reloj propio
// para que la siguiente pregunta no lo pise (HU-28: todos deben alcanzar a leerlo).
const REVELADO_MS = 5000;

type Pregunta = {
  preguntaId: string;
  orden: number;
  texto: string;
  opciones: { opcionId: string; texto: string }[];
  fechaActivacion: string;
  tiempoLimiteSegundos: number;
};

type Resultado = { esCorrecta: boolean; puntaje?: number } | null;

type PreguntaResult =
  | { ok: true; pregunta: Pregunta }
  | { ok: false; type: string; message?: string };
type ResponderResult =
  | { ok: true; data: { esCorrecta: boolean; cerroPregunta: boolean; puntaje?: number } }
  | { ok: false; type: string; message?: string };
type RankingResult =
  | { ok: true; ranking: { entradas: RankingEntrada[] } }
  | { ok: false; type: string; message?: string };

type Props = {
  apiBaseUrl: string;
  token: string;
  partidaId: string;
  juegoId: string;
  yaRespondioInicial: boolean;
  refetchSignal: number; // bump = PreguntaActivada/Cerrada del hub
  resetSignal: number; // bump = PreguntaActivada (nueva pregunta → limpiar respondido)
  miSub: string;
  rankingPush: { juegoId: string; entradas: RankingEntrada[] } | null;
  // HU-24/BR-T06: payload de PreguntaCerrada (nueva referencia en cada cierre, aunque el
  // texto se repita, para que el efecto siempre dispare). texto null = backend no lo mandó.
  // juegoId (7d review fix): filtra cierres de un juego que ya no es el activo — este estado
  // vive a nivel de partida en el padre y no se limpia al cambiar de juego.
  preguntaCerrada?: { texto: string | null; juegoId: string } | null;
  // Equipo: la respuesta de cualquier miembro sella al equipo entero, así que el resto ve el mismo
  // resultado. Se filtra por juego+pregunta para no sellar la pregunta siguiente con un evento tardío.
  respuestaEquipo?: { juegoId: string; preguntaId: string; esCorrecta: boolean } | null;
};

export function TriviaPlayPanel({
  apiBaseUrl, token, partidaId, juegoId, yaRespondioInicial, refetchSignal, resetSignal, miSub, rankingPush,
  preguntaCerrada, respuestaEquipo,
}: Props) {
  const [pregunta, setPregunta] = useState<Pregunta | null>(null);
  const [sinPregunta, setSinPregunta] = useState(false);
  const [respondida, setRespondida] = useState(yaRespondioInicial);
  const [resultado, setResultado] = useState<Resultado>(null);
  const [entradas, setEntradas] = useState<RankingEntrada[]>([]);
  const nombreDe = useNombres(idsDeCompetidores(entradas), apiBaseUrl, token);
  const [posting, setPosting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [textoCorrecta, setTextoCorrecta] = useState<string | null>(null);

  // Valor VIVO de resetSignal para leerlo dentro de onResponder tras el await (el prop en la
  // clausura queda stale). Se bumpea al llegar PreguntaActivada, señal de que la pregunta avanzó.
  const resetSignalRef = useRef(resetSignal);
  useEffect(() => {
    resetSignalRef.current = resetSignal;
  }, [resetSignal]);

  // Nueva pregunta activada → limpiar estado de respuesta local. NO se toca textoCorrecta: el
  // reveal de la pregunta que se cerró tiene vida propia (efecto de abajo) para que la siguiente
  // pregunta, que llega casi pegada, no lo borre antes de que a todos les dé tiempo de leerlo.
  useEffect(() => {
    if (resetSignal > 0) {
      setRespondida(false);
      setResultado(null);
      setError(null);
    }
  }, [resetSignal]);

  // Cierre de pregunta: guardar la respuesta correcta si el backend la mandó (aditivo) y si
  // pertenece a este juego (7d review fix: preguntaCerrada puede ser del juego anterior).
  useEffect(() => {
    const texto = seleccionarRespuestaCorrecta(preguntaCerrada, juegoId);
    if (texto) setTextoCorrecta(texto);
  }, [preguntaCerrada, juegoId]);

  // Reloj propio del reveal: se auto-borra a los REVELADO_MS. Un cierre nuevo lo reemplaza y
  // reinicia el temporizador (siempre un solo cartel, el más reciente).
  useEffect(() => {
    if (!textoCorrecta) return;
    const id = setTimeout(() => setTextoCorrecta(null), REVELADO_MS);
    return () => clearTimeout(id);
  }, [textoCorrecta]);

  // Equipo: un compañero respondió MAL y eso bloquea a todo el equipo. Se pinta "Incorrecta." en
  // el resto de los teléfonos aunque no hayan tocado nada. El acierto no llega hasta acá (cierra y
  // avanza para todos); aplicaRespuestaEquipo solo deja pasar el fallo, filtrado por juego+pregunta.
  useEffect(() => {
    if (!pregunta || !aplicaRespuestaEquipo(respuestaEquipo, juegoId, pregunta.preguntaId)) return;
    setRespondida(true);
    setResultado({ esCorrecta: false });
  }, [respuestaEquipo, juegoId, pregunta]);

  // Push SP-4c aditivo: ranking en vivo sin esperar señal de cierre.
  useEffect(() => {
    if (rankingPush && rankingPush.juegoId === juegoId) {
      setEntradas(rankingPush.entradas);
    }
  }, [rankingPush, juegoId]);

  const cargar = useCallback(async () => {
    const r = (await getPreguntaActual(apiBaseUrl, token, partidaId, undefined)) as PreguntaResult;
    if (r.ok) {
      setPregunta(r.pregunta);
      setSinPregunta(false);
    } else if (r.type === "sin_pregunta") {
      setPregunta(null);
      setSinPregunta(true);
    } else {
      setError(r.message ?? "No se pudo cargar la pregunta.");
    }
    const rk = (await getRankingJuego(apiBaseUrl, token, partidaId, juegoId, undefined)) as RankingResult;
    if (rk.ok) setEntradas(rk.ranking.entradas ?? []);
  }, [apiBaseUrl, token, partidaId, juegoId]);

  useEffect(() => {
    void cargar();
  }, [cargar, refetchSignal]);

  async function onResponder(opcionId: string) {
    setPosting(true);
    setError(null);
    // Si PreguntaActivada llegó mientras el POST estaba en vuelo, la pregunta ya avanzó: no
    // fijamos estado "respondida" (dejaría clavado "¡Correcta! / Esperando…" sobre la nueva
    // pregunta). El avance del hub (resetSignal/refetchSignal) ya maneja la UI.
    const { avanzo, r: raw } = await enviarRespuestaTrivia(
      () => responderPregunta(apiBaseUrl, token, partidaId, opcionId, undefined),
      () => resetSignalRef.current,
    );
    const r = raw as ResponderResult;
    setPosting(false);
    if (avanzo) return;
    if (r.ok) {
      setRespondida(true);
      setResultado({ esCorrecta: r.data.esCorrecta, puntaje: r.data.puntaje });
      return;
    }
    if (r.type === "conflict") {
      // Duplicada (yo o mi equipo) o fuera de tiempo: queda como respondida, sin resultado propio.
      setRespondida(true);
      setError(r.message ?? "La pregunta ya no acepta tu respuesta.");
      return;
    }
    setError(r.message ?? "No se pudo enviar la respuesta.");
  }

  const target = pregunta
    ? new Date(new Date(pregunta.fechaActivacion).getTime() + pregunta.tiempoLimiteSegundos * 1000).toISOString()
    : null;
  const avisoRespuestaCorrecta = formatRespuestaCorrecta(textoCorrecta);

  return (
    <View style={styles.stack}>
      {error ? <Notice variant="error">{error}</Notice> : null}
      {avisoRespuestaCorrecta ? <Notice variant="info">{avisoRespuestaCorrecta}</Notice> : null}
      {pregunta ? (
        <Card style={styles.card}>
          <AppText variant="bodyStrong">
            Pregunta {pregunta.orden} — {pregunta.texto}
          </AppText>
          {target ? <Countdown target={target} /> : null}
          {!respondida
            ? pregunta.opciones.map((o) => (
                <Button key={o.opcionId} label={o.texto} disabled={posting} onPress={() => void onResponder(o.opcionId)} />
              ))
            : null}
          {respondida ? (
            <View style={styles.resultado}>
              {resultado ? (
                <Notice variant={resultado.esCorrecta ? "success" : "error"}>
                  {resultado.esCorrecta
                    ? `¡Correcta!${resultado.puntaje != null ? ` +${resultado.puntaje} pts` : ""}`
                    : "Incorrecta."}
                </Notice>
              ) : (
                <Notice variant="info">Tu respuesta ya está registrada.</Notice>
              )}
              <AppText>Esperando el cierre de la pregunta…</AppText>
            </View>
          ) : null}
        </Card>
      ) : null}
      {sinPregunta ? (
        <Card style={styles.card}>
          <AppText>Esperando la siguiente pregunta…</AppText>
        </Card>
      ) : null}
      <Card style={styles.card}>
        <AppText variant="bodyStrong">Ranking del juego</AppText>
        <RankingTable entradas={entradas} resaltarId={miSub} nombreDe={nombreDe} />
      </Card>
    </View>
  );
}

const styles = StyleSheet.create({
  stack: { gap: spacing.lg },
  card: { gap: spacing.sm },
  resultado: { gap: spacing.sm },
});
