import { ClipboardList, Flag, IconComponent, ListChecks, Lock, MapPin, Play, Plus, UserPlus, Users } from "./icons";

export type Role = "Administrador" | "Operador";

export interface NavItemDef {
  label: string;
  path: string;
  icon: IconComponent;
}

export interface NavAreaDef {
  id: string;
  label: string;
  role: Role;
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
    id: "trivia",
    label: "Trivia",
    role: "Operador",
    icon: ListChecks,
    items: [
      { label: "Crear formulario", path: "/trivia/formularios/nuevo", icon: ClipboardList },
      { label: "Crear Trivia", path: "/trivia/crear", icon: Plus },
      { label: "Operar Trivia", path: "/trivia/operar", icon: Play }
    ]
  },
  {
    id: "bdt",
    label: "Búsqueda del Tesoro",
    role: "Operador",
    icon: MapPin,
    items: [
      { label: "Crear BDT", path: "/bdt/crear", icon: Plus },
      { label: "Partidas BDT", path: "/bdt/partidas", icon: Flag }
    ]
  }
];

export function areasForRoles(roles: string[]): NavAreaDef[] {
  return NAV_AREAS.filter((area) => roles.includes(area.role));
}

/* Landing per role: Operador -> Operar Trivia; Administrador -> Gestión de usuarios.
   A user with both roles lands on Trivia (Operador is checked first). */
export function landingPath(roles: string[]): string {
  if (roles.includes("Operador")) {
    return "/trivia/operar";
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
  return "UMBRAL";
}
