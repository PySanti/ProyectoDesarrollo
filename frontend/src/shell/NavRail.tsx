import { NavLink } from "react-router-dom";
import { BrandMark, PanelLeft } from "./icons";
import { areasForRoles } from "./navConfig";

interface NavRailProps {
  roles: string[];
  permisos: string[];
  collapsed: boolean;
  onToggleCollapse: () => void;
  onNavigate: () => void;
}

export function NavRail({ roles, permisos, collapsed, onToggleCollapse, onNavigate }: NavRailProps) {
  const areas = areasForRoles(roles, permisos);

  return (
    <nav className="sh-rail" aria-label="Navegación principal">
      <div className="sh-rail__brand">
        <BrandMark className="sh-rail__brand-mark" />
        <span>UMBRAL</span>
      </div>

      <div className="sh-rail__nav">
        {areas.map((area) => (
          <div className="sh-rail__group" key={area.id}>
            <div className="sh-rail__group-label">{area.label}</div>
            {area.items.map((item) => {
              const Icon = item.icon;
              return (
                <NavLink
                  key={item.path}
                  to={item.path}
                  end
                  title={collapsed ? item.label : undefined}
                  onClick={onNavigate}
                  className={({ isActive }) => `sh-nav-item${isActive ? " is-active" : ""}`}
                >
                  <Icon className="sh-nav-item__icon" />
                  <span className="sh-nav-item__label">{item.label}</span>
                </NavLink>
              );
            })}
          </div>
        ))}
      </div>

      <div className="sh-rail__footer">
        <button
          type="button"
          className="secondary-button sh-collapse-btn"
          onClick={onToggleCollapse}
          aria-pressed={collapsed}
          title={collapsed ? "Expandir navegación" : "Colapsar navegación"}
        >
          <PanelLeft className="sh-nav-item__icon" />
          <span>{collapsed ? "Expandir" : "Colapsar"}</span>
        </button>
      </div>
    </nav>
  );
}
