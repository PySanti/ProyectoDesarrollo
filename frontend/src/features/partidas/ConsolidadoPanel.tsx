// Ranking consolidado de la partida terminada (RF-45). GET puntual a Puntuaciones con
// reintento corto ante 404/409 (lag de proyección tras PartidaFinalizada).
import { useEffect, useState } from "react";
import {
  getRankingConsolidado,
  PuntuacionesApiError,
  type RankingConsolidadoDto
} from "../../api/puntuacionesApi";
import { formatTiempo } from "./runtimeShared";
import { idsDeCompetidores, useNombres } from "../shared/useNombres";

const MAX_INTENTOS = 3;
const ESPERA_MS = 1500;

type Estado =
  | { status: "cargando" }
  | { status: "ok"; ranking: RankingConsolidadoDto }
  | { status: "no-disponible" };

export function ConsolidadoPanel({
  partidaId,
  accessToken,
  consolidadoPush
}: {
  partidaId: string;
  accessToken: string;
  consolidadoPush?: RankingConsolidadoDto | null;
}) {
  const [estado, setEstado] = useState<Estado>({ status: "cargando" });
  const [intentoManual, setIntentoManual] = useState(0);
  const nombreDe = useNombres(
    idsDeCompetidores(estado.status === "ok" ? estado.ranking.entradas : []),
    accessToken
  );

  useEffect(() => {
    let active = true;
    let timer: ReturnType<typeof setTimeout> | undefined;
    let intentos = 0;

    const cargar = () => {
      getRankingConsolidado(partidaId, accessToken)
        .then((ranking) => {
          if (active) setEstado({ status: "ok", ranking });
        })
        .catch((caught) => {
          if (!active) return;
          const code = caught instanceof PuntuacionesApiError ? caught.statusCode : 0;
          intentos += 1;
          if ((code === 404 || code === 409) && intentos < MAX_INTENTOS) {
            timer = setTimeout(cargar, ESPERA_MS);
            return;
          }
          setEstado({ status: "no-disponible" });
        });
    };

    setEstado({ status: "cargando" });
    cargar();
    return () => {
      active = false;
      if (timer) clearTimeout(timer);
    };
  }, [partidaId, accessToken, intentoManual]);

  // Push SP-4c: el consolidado difundido al finalizar pinta sin esperar el retry del GET.
  useEffect(() => {
    if (consolidadoPush) {
      setEstado({ status: "ok", ranking: consolidadoPush });
    }
  }, [consolidadoPush]);

  return (
    <div className="card stack" data-testid="consolidado-panel">
      <h1>Partida finalizada</h1>
      {estado.status === "cargando" ? <p className="muted">Cargando consolidado…</p> : null}
      {estado.status === "no-disponible" ? (
        <div className="stack">
          <p className="muted">Consolidado no disponible aún.</p>
          <button type="button" className="secondary-button" onClick={() => setIntentoManual((n) => n + 1)}>
            Reintentar
          </button>
        </div>
      ) : null}
      {estado.status === "ok" ? <ConsolidadoTabla ranking={estado.ranking} nombreDe={nombreDe} /> : null}
    </div>
  );
}

function ConsolidadoTabla({
  ranking,
  nombreDe
}: {
  ranking: RankingConsolidadoDto;
  nombreDe: (id: string) => string;
}) {
  if (!ranking.entradas.length) {
    return <p className="muted">Sin resultados.</p>;
  }
  return (
    <div className="table-wrap">
      <table aria-label="Ranking consolidado" data-testid="ranking-consolidado">
        <thead>
          <tr>
            <th scope="col">Posición</th>
            <th scope="col">Competidor</th>
            <th scope="col">Juegos ganados</th>
            <th scope="col">Puntos totales</th>
            <th scope="col">Tiempo total</th>
          </tr>
        </thead>
        <tbody>
          {ranking.entradas.map((entrada) => (
            <tr key={entrada.competidorId}>
              <td>{entrada.posicion}</td>
              <td>{nombreDe(entrada.competidorId)}</td>
              <td>{entrada.juegosGanados}</td>
              <td>{entrada.puntosTotales}</td>
              <td>{formatTiempo(entrada.tiempoTotalMs)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
