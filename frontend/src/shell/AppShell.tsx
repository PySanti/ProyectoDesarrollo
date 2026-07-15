import { useEffect, useMemo, useState } from "react";
import { Outlet, useLocation } from "react-router-dom";
import { NavRail } from "./NavRail";
import { TopBar } from "./TopBar";
import { titleForPath } from "./navConfig";

const COLLAPSE_KEY = "umbral.rail.collapsed";

interface AppShellProps {
  roles: string[];
  userName: string;
  onLogout: () => void;
}

export function AppShell({ roles, userName, onLogout }: AppShellProps) {
  const [collapsed, setCollapsed] = useState<boolean>(() => {
    try {
      return localStorage.getItem(COLLAPSE_KEY) === "1";
    } catch {
      return false;
    }
  });
  const [drawerOpen, setDrawerOpen] = useState(false);
  const location = useLocation();
  const title = useMemo(() => titleForPath(location.pathname), [location.pathname]);

  // Close the mobile drawer on navigation.
  useEffect(() => {
    setDrawerOpen(false);
  }, [location.pathname]);

  function toggleCollapse() {
    setCollapsed((current) => {
      const next = !current;
      try {
        localStorage.setItem(COLLAPSE_KEY, next ? "1" : "0");
      } catch {
        /* storage unavailable; keep in-memory only */
      }
      return next;
    });
  }

  const roleLabel = roles.join(" · ");
  const shellClass = [
    "sh-shell",
    collapsed ? "sh-shell--collapsed" : "",
    drawerOpen ? "sh-shell--drawer-open" : ""
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className={shellClass}>
      <a className="skip-link" href="#sh-content">
        Saltar al contenido
      </a>
      <div className="sh-backdrop" aria-hidden="true" onClick={() => setDrawerOpen(false)} />

      <NavRail
        roles={roles}
        collapsed={collapsed}
        onToggleCollapse={toggleCollapse}
        onNavigate={() => setDrawerOpen(false)}
      />

      <div className="sh-main">
        <TopBar
          title={title}
          userName={userName}
          roleLabel={roleLabel}
          onLogout={onLogout}
          onOpenDrawer={() => setDrawerOpen(true)}
        />
        <main id="sh-content" className="sh-content" tabIndex={-1}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
