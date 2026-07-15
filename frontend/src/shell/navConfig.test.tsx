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
  it("shows Identidad, Partidas and Equipos to an admin", () => {
    expect(areasForRoles(["Administrador"]).map((area) => area.id)).toEqual([
      "identidad",
      "partidas",
      "equipos"
    ]);
  });

  it("shows Partidas and Equipos, but not Identidad, to an operator", () => {
    expect(areasForRoles(["Operador"]).map((area) => area.id)).toEqual(["partidas", "equipos"]);
  });

  it("shows the three team entries to an admin, but hides 'Creación de equipos' from an operator", () => {
    const equiposAdmin = areasForRoles(["Administrador"]).find((a) => a.id === "equipos");
    expect(equiposAdmin?.items.map((i) => i.label)).toEqual([
      "Creación de equipos",
      "Gestión de equipos",
      "Rendimiento de equipos"
    ]);
    const equiposOperador = areasForRoles(["Operador"]).find((a) => a.id === "equipos");
    expect(equiposOperador?.items.map((i) => i.label)).toEqual([
      "Gestión de equipos",
      "Rendimiento de equipos"
    ]);
  });

  /* BR-R02: 'Nueva partida' la abre el permiso GestionarPartidas, no el rol base
     — la gobernanza (HU-04) puede darselo al admin o quitarselo al operador. */
  it("hides 'Nueva partida' from anyone without GestionarPartidas", () => {
    const partidasAdmin = areasForRoles(["Administrador"]).find((a) => a.id === "partidas");
    expect(partidasAdmin?.items.map((i) => i.label)).toEqual(["Partidas"]);
    const partidasOperador = areasForRoles(["Operador"], []).find((a) => a.id === "partidas");
    expect(partidasOperador?.items.map((i) => i.label)).toEqual(["Partidas"]);
  });

  it("shows 'Nueva partida' to anyone with GestionarPartidas, admin included", () => {
    const partidasOperador = areasForRoles(["Operador"], ["GestionarPartidas"]).find(
      (a) => a.id === "partidas"
    );
    expect(partidasOperador?.items.map((i) => i.label)).toEqual(["Partidas", "Nueva partida"]);
    const partidasAdmin = areasForRoles(["Administrador"], ["GestionarPartidas"]).find(
      (a) => a.id === "partidas"
    );
    expect(partidasAdmin?.items.map((i) => i.label)).toEqual(["Partidas", "Nueva partida"]);
  });
});
