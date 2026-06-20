import { Menu } from "./icons";

interface TopBarProps {
  title: string;
  userName: string;
  roleLabel: string;
  onLogout: () => void;
  onOpenDrawer: () => void;
}

export function TopBar({ title, userName, roleLabel, onLogout, onOpenDrawer }: TopBarProps) {
  return (
    <header className="sh-topbar">
      <div className="sh-topbar__left">
        <button
          type="button"
          className="sh-iconbtn"
          aria-label="Abrir navegación"
          onClick={onOpenDrawer}
        >
          <Menu className="sh-nav-item__icon" />
        </button>
        <span className="sh-topbar__title">{title}</span>
      </div>

      <div className="sh-topbar__right">
        <span className="sh-identity">
          <span className="sh-identity__name">{userName}</span>
          <span className="sh-role-pill">{roleLabel}</span>
        </span>
        <button type="button" className="secondary-button" onClick={onLogout}>
          Cerrar sesión
        </button>
      </div>
    </header>
  );
}
