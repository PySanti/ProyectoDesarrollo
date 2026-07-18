// Historial cronológico de la partida (HU-43): eventos proyectados por Puntuaciones,
// paginado limit/offset con filtro por tipo. Solo Operador/Administrador (403 backend).
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  getHistorialPartida,
  PuntuacionesApiError,
  type HistorialPartidaDto
} from "../../api/puntuacionesApi";
import { useNombres } from "../shared/useNombres";
import { useNombresPartida } from "../shared/useNombresPartida";
import { etiquetaJuego } from "./juegoLabels";
import { describirDetalle } from "./detalleEvento";
import { etiquetaTipoEvento, TIPOS_EVENTO } from "./eventoLabels";
import { ClipboardList } from "../../shell/icons";

const LIMIT = 100;

type Estado =
  | { status: "cargando" }
  | { status: "ok"; historial: HistorialPartidaDto }
  | { status: "error"; message: string };


// El detalle es un objeto abierto (el payload del evento menos los ids ya extraidos), asi que
// se pinta como pares etiqueta→valor en vez de como JSON: el operador lee "Puntaje 50", no
// {"puntaje":50}. Los ids sueltos van en mono (regla Mono For Machine Strings).
function DetalleEvento({ detalle }: { detalle: unknown }) {
  const campos = describirDetalle(detalle);
  if (campos.length === 0) return <span className="muted">—</span>;
  return (
    <div>
      {campos.map((campo) => (
        <div key={campo.label}>
          <span className="muted">{campo.label}</span>{" "}
          <span className={campo.mono ? "mono" : undefined}>{campo.value}</span>
        </div>
      ))}
    </div>
  );
}

export function HistorialPartidaPage({ accessToken }: { accessToken: string }) {
  const { partidaId } = useParams<{ partidaId: string }>();
  const navigate = useNavigate();
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
          <button
            type="button"
            className="secondary-button"
            onClick={() => navigate(`/partidas/${partidaId}`)}
          >
            Volver a la partida
          </button>
        </div>

        {estado.status === "cargando" ? <p className="muted">Cargando historial…</p> : null}
        {estado.status === "error" ? (
          <div className="notice error" role="alert">
            {estado.message}
          </div>
        ) : null}

        {estado.status === "ok" ? (
          estado.historial.entradas.length === 0 ? (
            <div className="empty-panel" data-testid="historial-vacio">
              <ClipboardList />
              <p>Sin eventos registrados.</p>
              <p className="muted">
                {tipo ? (
                  <>
                    Ningún evento del tipo <strong>{etiquetaTipoEvento(tipo)}</strong> en esta partida.
                    Cambia el filtro a <strong>Todos</strong> para ver el resto.
                  </>
                ) : (
                  <>El historial se llena solo, a medida que la partida se publica, se juega y termina.</>
                )}
              </p>
            </div>
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
                        <td>{etiquetaTipoEvento(e.tipoEvento)}</td>
                        <td>{etiquetaJuego(e.juegoOrden, e.tipoJuego, e.juegoId)}</td>
                        <td>{e.participanteId ? nombreDe(e.participanteId) : "—"}</td>
                        <td>{e.equipoId ? nombreDe(e.equipoId) : "—"}</td>
                        <td data-testid="detalle-evento">
                          <DetalleEvento detalle={e.detalle} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="compact-actions">
                <button
                  type="button"
                  className="secondary-button"
                  disabled={offset === 0}
                  onClick={() => setOffset(Math.max(0, offset - LIMIT))}
                >
                  Anterior
                </button>
                <span className="muted">
                  {desde}–{hasta} de {total}
                </span>
                <button
                  type="button"
                  className="secondary-button"
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
