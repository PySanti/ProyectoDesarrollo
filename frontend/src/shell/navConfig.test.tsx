import { describe, expect, it } from "vitest";
import { areasForRoles, landingPath, titleForPath } from "./navConfig";

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
    expect(areasForRoles(["Administrador"], ["GestionarPartidas", "GestionarEquipos"]).map((area) => area.id)).toEqual([
      "identidad",
      "partidas",
      "equipos"
    ]);
  });

  it("shows Partidas and Equipos, but not Identidad, to an operator", () => {
    expect(areasForRoles(["Operador"], ["GestionarPartidas", "GestionarEquipos"]).map((area) => area.id)).toEqual(["partidas", "equipos"]);
  });

  it("shows the three team entries to an admin, but hides 'Creación de equipos' from an operator", () => {
    const equiposAdmin = areasForRoles(["Administrador"], ["GestionarEquipos"]).find((a) => a.id === "equipos");
    expect(equiposAdmin?.items.map((i) => i.label)).toEqual([
      "Creación de equipos",
      "Gestión de equipos",
      "Rendimiento de equipos"
    ]);
    const equiposOperador = areasForRoles(["Operador"], ["GestionarEquipos"]).find((a) => a.id === "equipos");
    expect(equiposOperador?.items.map((i) => i.label)).toEqual([
      "Gestión de equipos",
      "Rendimiento de equipos"
    ]);
  });

  it("hides 'Nueva partida' from an admin but keeps it for an operator", () => {
    const partidasAdmin = areasForRoles(["Administrador"], ["GestionarPartidas"]).find((a) => a.id === "partidas");
    expect(partidasAdmin?.items.map((i) => i.label)).toEqual(["Partidas"]);
    const partidasOperador = areasForRoles(["Operador"], ["GestionarPartidas"]).find((a) => a.id === "partidas");
    expect(partidasOperador?.items.map((i) => i.label)).toEqual(["Partidas", "Nueva partida"]);
  });

  /* El privilegio abre el área entera, consulta incluida: sin él no aparece nada de esa área. */
  it("oculta el área Partidas a quien no tiene GestionarPartidas, aunque sea Operador", () => {
    const areas = areasForRoles(["Operador"], []);

    expect(areas.map((area) => area.id)).not.toContain("partidas");
  });

  it("muestra el área Partidas a un Administrador con GestionarPartidas", () => {
    const areas = areasForRoles(["Administrador"], ["GestionarPartidas"]);

    expect(areas.map((area) => area.id)).toContain("partidas");
  });

  it("oculta el área Equipos a quien no tiene GestionarEquipos", () => {
    const areas = areasForRoles(["Administrador"], ["GestionarPartidas"]);

    expect(areas.map((area) => area.id)).not.toContain("equipos");
  });

  /* Identidad no es un privilegio: viene con el rol y está protegida, o un admin podría
     quitarse a sí mismo el acceso a la gobernanza y dejar el sistema cerrado sin llave. */
  it("muestra Identidad a un Administrador sin ningún privilegio", () => {
    const areas = areasForRoles(["Administrador"], []);

    expect(areas.map((area) => area.id)).toEqual(["identidad"]);
  });

  it("no da landing a un Operador sin privilegios: no tiene ninguna área", () => {
    expect(landingPath(["Operador"], [])).toBeNull();
  });

  it("lleva al Administrador a Identidad, que siempre tiene", () => {
    // Al listado, no al formulario de alta: el área declara su landing porque su primer item
    // («Crear usuario») soltaría al admin en un formulario vacío.
    expect(landingPath(["Administrador"], [])).toBe("/identidad/usuarios");
  });

  it("lleva a Partidas a quien puede gestionarlas", () => {
    expect(landingPath(["Operador"], ["GestionarPartidas"])).toBe("/partidas");
  });
});
