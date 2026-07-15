import { Flag, IconComponent, ListChecks, Lock, Plus, UserPlus, Users } from "./icons";

export type Role = "Administrador" | "Operador";

export interface NavItemDef {
  label: string;
  path: string;
  icon: IconComponent;
  /** Sin `roles`: el item hereda la visibilidad del área. */
  roles?: readonly Role[];
}

export interface NavAreaDef {
  id: string;
  label: string;
  role: Role | readonly Role[];
  icon: IconComponent;
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
  {
    id: "partidas",
    label: "Partidas",
    role: ["Operador", "Administrador"],
    icon: Flag,
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
    items: [
      { label: "Creación de equipos", path: "/identidad/equipos", icon: Flag, roles: ["Administrador"] },
      { label: "Gestión de equipos", path: "/equipos", icon: Users },
      { label: "Rendimiento de equipos", path: "/puntuaciones/equipos", icon: ListChecks }
    ]
  }
];

export function areasForRoles(roles: string[]): NavAreaDef[] {
  return NAV_AREAS.filter((area) => {
    const allowedRoles = typeof area.role === "string" ? [area.role] : area.role;
    return allowedRoles.some((role) => roles.includes(role));
  }).map((area) => ({
    ...area,
    items: area.items.filter(
      (item) => !item.roles || item.roles.some((role) => roles.includes(role))
    )
  }));
}

/* Landing per role: Operador -> Partidas; Administrador -> Gestión de usuarios.
   A user with both roles lands on Partidas (Operador is checked first). */
export function landingPath(roles: string[]): string {
  if (roles.includes("Operador")) {
    return "/partidas";
  }
  if (roles.includes("Administrador")) {
    return "/identidad/usuarios";
  }
  return "/";
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
