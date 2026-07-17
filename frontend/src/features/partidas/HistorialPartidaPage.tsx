// Historial cronológico de la partida (HU-43): eventos proyectados por Puntuaciones,
// paginado limit/offset con filtro por tipo. Solo Operador/Administrador (403 backend).
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  getHistorialPartida,
  PuntuacionesApiError,
  type HistorialPartidaDto
} from "../../api/puntuacionesApi";
import { useNombres } from "../shared/useNombres";
import { useNombresPartida } from "../shared/useNombresPartida";
import { etiquetaJuego } from "./juegoLabels";

export const TIPOS_EVENTO = [
  "PartidaPublicadaEnLobby",
  "PartidaIniciada",
  "PartidaCancelada",
  "PartidaFinalizada",
  "JuegoActivado",
  "PreguntaTriviaActivada",
  "RespuestaTriviaValidada",
  "PuntajeTriviaIncrementado",
  "PreguntaTriviaCerrada",
  "EtapaBDTActivada",
  "TesoroQRValidado",
  "EtapaBDTGanada",
  "EtapaBDTCerrada",
  "PistaEnviada",
  "ConvocatoriaCreada",
  "ConvocatoriaRespondida",
  "UbicacionActualizada"
];

const LIMIT = 100;

type Estado =
  | { status: "cargando" }
  | { status: "ok"; historial: HistorialPartidaDto }
  | { status: "error"; message: string };

// Etiqueta legible para el <select>: distinta del texto crudo de la tabla
// (evita colisión de getByText entre <option> y <td> para el mismo tipo de evento).
const etiquetaTipoEvento = (t: string) => t.replace(/([a-z0-9])([A-Z])/g, "$1 $2");

export function HistorialPartidaPage({ accessToken }: { accessToken: string }) {
  const { partidaId } = useParams<{ partidaId: string }>();
  const [estado, setEstado] = useState<Estado>({ status: "cargando" });
  const [tipo, setTipo] = useState("");
  const [offset, setOffset] = useState(0);
  const entradas = estado.status === "ok" ? estado.historial.entradas : [];
  const nombrePartidaDe = useNombresPartida(accessToken);
  const nombreDe = useNombres(
    {
      participanteIds: entradas.map((e) => e.participanteId).filter((id): id is string => !!id),
      equipoIds: entradas.map((e) => e.equipoId).filter((id): id is string => !!id)
    },
    accessToken
  );

  useEffect(() => {
    if (!partidaId) return;
    let active = true;
    setEstado({ status: "cargando" });
    getHistorialPartida(partidaId, accessToken, {
      limit: LIMIT,
      offset,
      ...(tipo ? { tipo } : {})
    })
      .then((historial) => {
        if (active) setEstado({ status: "ok", historial });
      })
      .catch((caught) => {
        if (!active) return;
        const message =
          caught instanceof PuntuacionesApiError && caught.statusCode === 404
            ? "La partida no existe en la proyección de Puntuaciones."
            : caught instanceof Error
              ? caught.message
              : "Error inesperado al consultar el historial.";
        setEstado({ status: "error", message });
      });
    return () => {
      active = false;
    };
  }, [partidaId, accessToken, tipo, offset]);

  const total = estado.status === "ok" ? estado.historial.total : 0;
  const desde = total === 0 ? 0 : offset + 1;
  const hasta = estado.status === "ok" ? offset + estado.historial.entradas.length : 0;

  return (
    <div className="page" data-testid="historial-partida">
      <div className="card stack">
        <h1>Historial de la partida{partidaId ? ` — ${nombrePartidaDe(partidaId)}` : ""}</h1>
        <div className="compact-actions">
          <label>
            Tipo de evento{" "}
            <select
              value={tipo}
              aria-label="Filtrar por tipo de evento"
              onChange={(e) => {
                setTipo(e.target.value);
                setOffset(0);
              }}
            >
              <option value="">Todos</option>
              {TIPOS_EVENTO.map((t) => (
                <option key={t} value={t}>
                  {etiquetaTipoEvento(t)}
                </option>
              ))}
            </select>
          </label>
          <Link to={`/partidas/${partidaId}`} className="row-link">
            Volver a la partida
          </Link>
        </div>

        {estado.status === "cargando" ? <p className="muted">Cargando historial…</p> : null}
        {estado.status === "error" ? (
          <div className="notice error" role="alert">
            {estado.message}
          </div>
        ) : null}

        {estado.status === "ok" ? (
          estado.historial.entradas.length === 0 ? (
            <p className="muted">Sin eventos registrados.</p>
          ) : (
            <>
              <div className="table-wrap">
                <table aria-label="Historial de eventos" data-testid="tabla-historial">
                  <thead>
                    <tr>
                      <th scope="col">Momento</th>
                      <th scope="col">Evento</th>
                      <th scope="col">Juego</th>
                      <th scope="col">Participante</th>
                      <th scope="col">Equipo</th>
                      <th scope="col">Detalle</th>
                    </tr>
                  </thead>
                  <tbody>
                    {estado.historial.entradas.map((e, i) => (
                      <tr key={`${e.occurredAt}-${i}`}>
                        <td>{new Date(e.occurredAt).toLocaleString()}</td>
                        <td>{e.tipoEvento}</td>
                        <td>{etiquetaJuego(e.juegoOrden, e.tipoJuego, e.juegoId)}</td>
                        <td>{e.participanteId ? nombreDe(e.participanteId) : "—"}</td>
                        <td>{e.equipoId ? nombreDe(e.equipoId) : "—"}</td>
                        <td className="muted">{JSON.stringify(e.detalle)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="compact-actions">
                <button type="button" disabled={offset === 0} onClick={() => setOffset(Math.max(0, offset - LIMIT))}>
                  Anterior
                </button>
                <span className="muted">
                  {desde}–{hasta} de {total}
                </span>
                <button
                  type="button"
                  disabled={offset + LIMIT >= total}
                  onClick={() => setOffset(offset + LIMIT)}
                >
                  Siguiente
                </button>
              </div>
            </>
          )
        ) : null}
      </div>
    </div>
  );
}
