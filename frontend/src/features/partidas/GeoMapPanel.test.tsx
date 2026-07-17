import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { GeoMapPanel, calcularCentro, type UbicacionParticipante } from "./GeoMapPanel";

// leaflet no corre en jsdom: mockeamos react-leaflet con stubs que renderizan hijos.
vi.mock("react-leaflet", () => ({
  MapContainer: ({ children }: { children: React.ReactNode }) => <div data-testid="map">{children}</div>,
  TileLayer: () => <div data-testid="tile" />,
  CircleMarker: ({ children }: { children: React.ReactNode }) => <div data-testid="geo-marker">{children}</div>,
  Popup: ({ children }: { children: React.ReactNode }) => <div>{children}</div>
}));

const u = (id: string, lat: number, lng: number): UbicacionParticipante => ({
  participanteId: id, latitud: lat, longitud: lng, timestampUtc: new Date().toISOString()
});

describe("GeoMapPanel", () => {
  it("calcularCentro promedia lat/long; vacio -> [0,0]", () => {
    expect(calcularCentro([])).toEqual([0, 0]);
    expect(calcularCentro([u("a", 10, 20), u("b", 20, 40)])).toEqual([15, 30]);
  });

  it("renderiza un marcador por ubicacion", () => {
    render(<GeoMapPanel ubicaciones={[u("aaaaaaaa-1", 10, 20), u("bbbbbbbb-2", 11, 21)]} nombreDe={(id) => id} />);
    expect(screen.getByTestId("geo-map")).toBeInTheDocument();
    expect(screen.getAllByTestId("geo-marker")).toHaveLength(2);
  });

  it("etiqueta cada marcador con el nombre resuelto, no con el id", () => {
    const nombres: Record<string, string> = { "aaaaaaaa-1": "María González" };
    render(<GeoMapPanel ubicaciones={[u("aaaaaaaa-1", 10, 20)]} nombreDe={(id) => nombres[id] ?? id} />);
    expect(screen.getByText(/María González/)).toBeInTheDocument();
  });

  it("sin ubicaciones muestra leyenda de espera y ningun marcador", () => {
    render(<GeoMapPanel ubicaciones={[]} nombreDe={(id) => id} />);
    expect(screen.getByText(/esperando ubicaciones/i)).toBeInTheDocument();
    expect(screen.queryAllByTestId("geo-marker")).toHaveLength(0);
  });
});
