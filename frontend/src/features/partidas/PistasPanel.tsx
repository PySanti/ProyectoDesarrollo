// Envio de pistas del operador a un participante o equipo (BDT).
import { useEffect, useState } from "react";
import { enviarPista, getLobby, OperacionesApiError, type LobbyDto } from "../../api/operacionesApi";
import { useNombres } from "../shared/useNombres";

export function PistasPanel({ partidaId, accessToken }: { partidaId: string; accessToken: string }) {
  const [lobby, setLobby] = useState<LobbyDto | null>(null);
  const [destino, setDestino] = useState("");
  const [texto, setTexto] = useState("");
  const [posteando, setPosteando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [enviadaEn, setEnviadaEn] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    getLobby(partidaId, accessToken)
      .then((l) => { if (active) setLobby(l); })
      .catch(() => { if (active) setLobby(null); });
    return () => { active = false; };
  }, [partidaId, accessToken]);

  const esEquipo = lobby?.modalidad === "Equipo";
  const opciones = esEquipo ? (lobby?.equipos.map((e) => e.equipoId) ?? []) : (lobby?.participantes ?? []);
  const nombreDe = useNombres(
    esEquipo ? { participanteIds: [], equipoIds: opciones } : { participanteIds: opciones, equipoIds: [] },
    accessToken
  );

  async function onEnviar() {
    if (!destino || !texto.trim()) return;
    setPosteando(true);
    setError(null);
    setEnviadaEn(null);
    try {
      const body = esEquipo
        ? { texto, equipoDestinoId: destino }
        : { texto, participanteDestinoId: destino };
      const r = await enviarPista(partidaId, body, accessToken);
      setEnviadaEn(r.timestampUtc);
      setTexto("");
    } catch (caught) {
      setError(
        caught instanceof OperacionesApiError
          ? mapPistaError(caught.statusCode)
          : "No se pudo enviar la pista."
      );
    } finally {
      setPosteando(false);
    }
  }

  return (
    <div className="stack" data-testid="pistas-panel">
      <h3 className="q-title">Enviar pista</h3>
      {opciones.length === 0 ? (
        <p className="muted">Sin inscritos para enviar pistas.</p>
      ) : (
        <>
          <select data-testid="pista-destino" value={destino} onChange={(e) => setDestino(e.target.value)}>
            <option value="">— elige {esEquipo ? "equipo" : "participante"} —</option>
            {opciones.map((id) => (
              <option key={id} value={id}>{nombreDe(id)}</option>
            ))}
          </select>
          <textarea data-testid="pista-texto" value={texto} onChange={(e) => setTexto(e.target.value)} placeholder="Texto de la pista" />
          <button type="button" data-testid="btn-enviar-pista" disabled={posteando || !destino || !texto.trim()} onClick={() => void onEnviar()}>
            Enviar pista
          </button>
        </>
      )}
      {error ? <div className="notice error" role="alert">{error}</div> : null}
      {enviadaEn ? <p className="muted" data-testid="pista-enviada">Pista enviada ({enviadaEn}).</p> : null}
    </div>
  );
}

function mapPistaError(statusCode: number): string {
  switch (statusCode) {
    case 400: return "Indica exactamente un destino.";
    case 404: return "El destino no tiene una inscripción activa.";
    case 409: return "No se puede enviar la pista ahora (juego no BDT o sin etapa activa).";
    default: return "No se pudo enviar la pista.";
  }
}
