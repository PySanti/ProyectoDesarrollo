// Panel de monitoreo operador (HU-38): intentos de TesoroQR por etapa del juego BDT activo.
// Solo lectura, sin gating por puedeOperar (visible tambien al admin observador).
// Refetch por el patron GET-en-senial existente: se dispara con cada cambio de refetchSignal.
import { useEffect, useState } from "react";
import { getEnviosTesoro, type EnviosTesoroDto, type IntentoTesoroDto } from "../../api/operacionesApi";

export function EnviosTesoroPanel({
  partidaId,
  accessToken,
  refetchSignal
}: {
  partidaId: string;
  accessToken: string;
  refetchSignal: number;
}) {
  const [envios, setEnvios] = useState<EnviosTesoroDto | null>(null);

  useEffect(() => {
    let active = true;
    getEnviosTesoro(partidaId, accessToken)
      .then((e) => {
        if (active) setEnvios(e);
      })
      .catch(() => {
        if (active) setEnvios(null);
      });
    return () => {
      active = false;
    };
  }, [partidaId, accessToken, refetchSignal]);

  const filas = (envios?.etapas ?? []).flatMap((etapa) =>
    etapa.intentos.map((intento) => ({ orden: etapa.orden, ...intento }))
  );

  return (
    <div className="stack" data-testid="envios-tesoro-panel">
      <h3 className="q-title">Envíos de tesoro</h3>
      {filas.length === 0 ? (
        <p className="muted">Sin envíos registrados todavía.</p>
      ) : (
        <div className="table-wrap">
          <table aria-label="Envíos de tesoro por etapa">
            <thead>
              <tr>
                <th scope="col">Etapa</th>
                <th scope="col">Participante/Equipo</th>
                <th scope="col">Resultado</th>
                <th scope="col">Hora</th>
              </tr>
            </thead>
            <tbody>
              {filas.map((f, idx) => (
                <FilaIntento key={idx} orden={f.orden} intento={f} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function FilaIntento({ orden, intento }: { orden: number; intento: IntentoTesoroDto }) {
  return (
    <tr>
      <td>{orden}</td>
      <td>{intento.equipoId ?? intento.participanteId}</td>
      <td>{intento.resultado}</td>
      <td>{new Date(intento.instante).toLocaleTimeString()}</td>
    </tr>
  );
}
