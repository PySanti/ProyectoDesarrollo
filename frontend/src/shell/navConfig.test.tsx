import { describe, expect, it } from "vitest";
import { areasForRoles, titleForPath } from "./navConfig";

describe("titleForPath", () => {
  it("devuelve 'Sesión en vivo' para la ruta de sesion", () => {
    expect(titleForPath("/partidas/p1/sesion")).toBe("Sesión en vivo");
  });
  it("devuelve 'Detalle de partida' para el detalle", () => {
    expect(titleForPath("/partidas/p1")).toBe("Detalle de partida");
  });
});

describe("areasForRoles", () => {
  it("shows Identidad, Partidas, Puntuaciones and Equipos to an admin", () => {
    expect(areasForRoles(["Administrador"]).map((area) => area.id)).toEqual([
      "identidad",
      "partidas",
      "puntuaciones",
      "equipos"
    ]);
  });

  it("shows Partidas, Puntuaciones and Equipos, but not Identidad, to an operator", () => {
    expect(areasForRoles(["Operador"]).map((area) => area.id)).toEqual([
      "partidas",
      "puntuaciones",
      "equipos"
    ]);
  });

  it("shows Equipos to both admin and operator", () => {
    expect(areasForRoles(["Administrador"]).map((a) => a.id)).toContain("equipos");
    expect(areasForRoles(["Operador"]).map((a) => a.id)).toContain("equipos");
  });

  it("hides 'Nueva partida' from an admin but keeps it for an operator", () => {
    const partidasAdmin = areasForRoles(["Administrador"]).find((a) => a.id === "partidas");
    expect(partidasAdmin?.items.map((i) => i.label)).toEqual(["Partidas"]);
    const partidasOperador = areasForRoles(["Operador"]).find((a) => a.id === "partidas");
    expect(partidasOperador?.items.map((i) => i.label)).toEqual(["Partidas", "Nueva partida"]);
  });
});
