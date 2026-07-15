import { describe, expect, it } from "vitest";
import { extractPermisos, extractRoles } from "./keycloak";

/* Los privilegios viajan en el mismo realm_access.roles que los roles base: ADR-0013 los modela
   como realm roles composite y Keycloak los expande solo. Se extraen aparte para no mezclarlos:
   el shell muestra `roles` al usuario. */
describe("extracción de credenciales del token", () => {
  const tokenCon = (roles: string[]) => ({ realm_access: { roles } }) as never;

  it("extrae los privilegios gobernables, no sólo los roles base", () => {
    const parsed = tokenCon(["Administrador", "GestionarPartidas", "GestionarEquipos"]);

    expect(extractPermisos(parsed)).toEqual(["GestionarPartidas", "GestionarEquipos"]);
  });

  it("no mezcla los privilegios con los roles base", () => {
    const parsed = tokenCon(["Administrador", "GestionarPartidas"]);

    expect(extractRoles(parsed)).toEqual(["Administrador"]);
  });

  it("descarta los roles técnicos de Keycloak", () => {
    const parsed = tokenCon(["Operador", "offline_access", "uma_authorization", "default-roles-umbral"]);

    expect(extractRoles(parsed)).toEqual(["Operador"]);
    expect(extractPermisos(parsed)).toEqual([]);
  });

  it("devuelve listas vacías si el token no trae realm_access", () => {
    expect(extractRoles(undefined)).toEqual([]);
    expect(extractPermisos(undefined)).toEqual([]);
  });
});
