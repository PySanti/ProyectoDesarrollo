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
   bucle. `null` = ninguna área; App.tsx muestra la pantalla de sin accesos. */
export function landingPath(roles: string[], permisos: string[] = []): string | null {
  const areas = areasForRoles(roles, permisos);
  if (areas.length === 0) {
    return null;
  }

  const primerItem = areas.flatMap((area) => area.items)[0];
  return primerItem?.path ?? null;
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
