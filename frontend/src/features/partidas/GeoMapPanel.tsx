// Mapa de geolocalizacion en vivo (operador-only): un CircleMarker por participante.
import { CircleMarker, MapContainer, Popup, TileLayer } from "react-leaflet";
import "leaflet/dist/leaflet.css";

export interface UbicacionParticipante {
  participanteId: string;
  latitud: number;
  longitud: number;
  timestampUtc: string;
}

export function calcularCentro(ubicaciones: UbicacionParticipante[]): [number, number] {
  if (ubicaciones.length === 0) return [0, 0];
  const lat = ubicaciones.reduce((s, u) => s + u.latitud, 0) / ubicaciones.length;
  const lng = ubicaciones.reduce((s, u) => s + u.longitud, 0) / ubicaciones.length;
  return [lat, lng];
}

function hace(timestampUtc: string): string {
  const seg = Math.max(0, Math.round((Date.now() - new Date(timestampUtc).getTime()) / 1000));
  return `${seg}s`;
}

// nombreDe llega por prop (y no se resuelve aquí) para que el mapa siga siendo
// presentacional: no necesita token ni conocer el directorio.
export function GeoMapPanel({
  ubicaciones,
  nombreDe
}: {
  ubicaciones: UbicacionParticipante[];
  nombreDe: (id: string) => string;
}) {
  const centro = calcularCentro(ubicaciones);
  const tieneUbicaciones = ubicaciones.length > 0;
  return (
    <div className="stack" data-testid="geo-map">
      <h3 className="q-title">Ubicaciones en vivo</h3>
      {tieneUbicaciones ? null : <p className="muted">Esperando ubicaciones…</p>}
      {/* key re-monta el mapa una sola vez al pasar de vacio a con-datos (salto a la zona). */}
      <MapContainer
        key={tieneUbicaciones ? "live" : "empty"}
        center={centro}
        zoom={tieneUbicaciones ? 15 : 2}
        style={{ height: "360px", width: "100%" }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        {ubicaciones.map((u) => (
          <CircleMarker key={u.participanteId} center={[u.latitud, u.longitud]} radius={8}>
            <Popup>
              {nombreDe(u.participanteId)} · visto hace {hace(u.timestampUtc)}
            </Popup>
          </CircleMarker>
        ))}
      </MapContainer>
    </div>
  );
}
