// Piezas compartidas del runtime del operador (Trivia + BDT + página de sesión):
// cuenta regresiva, tabla de ranking del juego, formato de tiempo mm:ss.
import { useEffect, useState } from "react";
import type { RankingJuegoDto } from "../../api/puntuacionesApi";

export function formatTiempo(ms: number): string {
  const mm = String(Math.floor(ms / 60000)).padStart(2, "0");
  const ss = String(Math.floor(ms / 1000) % 60).padStart(2, "0");
  return `${mm}:${ss}`;
}

export function Countdown({
  target,
  testid,
  expiredLabel = "Tiempo agotado",
  muted = true
}: {
  target: string;
  testid: string;
  expiredLabel?: string;
  muted?: boolean;
}) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);
  const remaining = Math.max(0, Math.floor((new Date(target).getTime() - now) / 1000));
  const mm = String(Math.floor(remaining / 60)).padStart(2, "0");
  const ss = String(remaining % 60).padStart(2, "0");
  return (
    <span className={muted ? "muted" : undefined} data-testid={testid}>
      {remaining > 0 ? `${mm}:${ss}` : expiredLabel}
    </span>
  );
}

export function RankingView({ ranking }: { ranking: RankingJuegoDto | null }) {
  if (!ranking?.entradas?.length) {
    return <p className="muted">Sin datos de ranking todavía.</p>;
  }
  return (
    <div className="table-wrap">
      <table aria-label="Ranking del juego" data-testid="ranking-juego">
        <thead>
          <tr>
            <th scope="col">Posición</th>
            <th scope="col">Competidor</th>
            <th scope="col">Puntos</th>
            <th scope="col">Tiempo</th>
            <th scope="col">Ganadas</th>
          </tr>
        </thead>
        <tbody>
          {ranking.entradas.map((entrada) => (
            <tr key={entrada.competidorId}>
              <td>{entrada.posicion}</td>
              <td>{entrada.competidorId.slice(0, 8)}</td>
              <td>{entrada.puntos}</td>
              <td>{formatTiempo(entrada.tiempoAcumuladoMs)}</td>
              <td>{entrada.unidadesGanadas}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
