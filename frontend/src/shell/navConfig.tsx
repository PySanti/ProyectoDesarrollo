import { Flag, IconComponent, ListChecks, Lock, Plus, UserPlus, Users } from "./icons";

export type Role = "Administrador" | "Operador";

export interface NavItemDef {
  label: string;
  path: string;
  icon: IconComponent;
  /** Sin `roles`: el item hereda la visibilidad del área. */
  roles?: readonly Role[];
  /** Privilegio funcional opcional para este item específico. */
  permisos?: readonly string[];
}

export interface NavAreaDef {
  id: string;
  label: string;
  role: Role | readonly Role[];
  icon: IconComponent;
  /** Privilegio que gobierna el área entera. Sin él, el área no existe para el usuario:
      ni en el menú ni por URL directa (ver `Require` en App.tsx). */
  permisos?: readonly string[];
  /** Dónde aterrizar si ésta es la primera área del usuario. Por defecto, su primer item.
      Se declara sólo cuando ese primero no es buen sitio para caer — un formulario vacío
      en vez de un listado, por ejemplo. */
  landing?: string;
  items: NavItemDef[];
}

/* Nav organized by domain area, filtered by role. Each area maps to one of the
   four approved services. Web is only for Administrador / Operador flows. */
export const NAV_AREAS: NavAreaDef[] = [
  {
    id: "identidad",
    label: "Identidad",
    role: "Administrador",
    icon: Users,
    // Su primer item es «Crear usuario»: aterrizar ahí soltaría al admin en un formulario vacío.
    landing: "/identidad/usuarios",
    items: [
      { label: "Crear usuario", path: "/identidad/usuarios/nuevo", icon: UserPlus },
      { label: "Gestión de usuarios", path: "/identidad/usuarios", icon: Users },
      { label: "Gobernanza", path: "/identidad/gobernanza", icon: Lock }
    ]
  },
  // GestionarPartidas gobierna el CRUD de partidas completo, consulta incluida.
  {
    id: "partidas",
    label: "Partidas",
    role: ["Operador", "Administrador"],
    icon: Flag,
    permisos: ["GestionarPartidas"],
    items: [
      { label: "Partidas", path: "/partidas", icon: ListChecks },
      { label: "Nueva partida", path: "/partidas/crear", icon: Plus, roles: ["Operador"] }
    ]
  },
  {
    id: "equipos",
    label: "Equipos",
    role: ["Operador", "Administrador"],
    icon: Users,
    permisos: ["GestionarEquipos"],
    items: [
      { label: "Creación de equipos", path: "/identidad/equipos", icon: Flag, roles: ["Administrador"] },
      { label: "Gestión de equipos", path: "/equipos", icon: Users },
      { label: "Rendimiento de equipos", path: "/puntuaciones/equipos", icon: ListChecks }
    ]
  }
];

/* El rol base delimita el ámbito del área y el privilegio funcional la habilita: sin el privilegio
   de gestión, nada de esa área aparece. Dentro del área, un item puede además exigir su propio rol. */
export function areasForRoles(roles: string[], permisos: string[] = []): NavAreaDef[] {
  return NAV_AREAS.filter((area) => {
    const allowedRoles = typeof area.role === "string" ? [area.role] : area.role;
    return (
      allowedRoles.some((role) => roles.includes(role)) &&
      (!area.permisos || area.permisos.some((permiso) => permisos.includes(permiso)))
    );
  }).map((area) => ({
    ...area,
    items: area.items.filter(
      (item) =>
        (!item.roles || item.roles.some((role) => roles.includes(role))) &&
        (!item.permisos || item.permisos.some((permiso) => permisos.includes(permiso)))
    )
  }));
}

/* Primera área disponible, en orden de prioridad. Depende de los privilegios porque un Operador sin
   GestionarPartidas ya no tiene /partidas: aterrizar ahí lo rebotaría contra su propio landing en
   bucle. `null` = ninguna área; App.tsx muestra la pantalla de sin accesos.

   Se deriva de `areasForRoles` a propósito: si el landing pudiera apuntar a un área que el nav
   oculta, la ruta rebotaría al landing y el landing volvería a rebotar. Derivarlo hace imposible esa
   discrepancia — y por lo mismo, un `landing` declarado sólo se respeta si su item sigue visible. */
export function landingPath(roles: string[], permisos: string[] = []): string | null {
  for (const area of areasForRoles(roles, permisos)) {
    const paths = area.items.map((item) => item.path);
    if (paths.length === 0) {
      continue;
    }
    return area.landing && paths.includes(area.landing) ? area.landing : paths[0];
  }

  return null;
}

export function titleForPath(pathname: string): string {
  for (const area of NAV_AREAS) {
    for (const item of area.items) {
      if (item.path === pathname) {
        return item.label;
      }
    }
  }
  if (pathname.endsWith("/sesion")) {
    return "Sesión en vivo";
  }
  if (pathname.endsWith("/historial")) {
    return "Historial de la partida";
  }
  if (pathname.startsWith("/partidas/")) {
    return "Detalle de partida";
  }
  return "UMBRAL";
}
