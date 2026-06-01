import React, { createContext, useContext, useEffect, useMemo, useState } from "react";
import { AuthSessionState } from "./authTypes";
import { loginWithKeycloakAsync, logoutAsync, restoreSessionAsync } from "./keycloakMobileAuth";

type AuthContextValue = {
  loading: boolean;
  session: AuthSessionState | null;
  login: () => Promise<void>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [loading, setLoading] = useState(true);
  const [session, setSession] = useState<AuthSessionState | null>(null);

  useEffect(() => {
    restoreSessionAsync()
      .then((value) => {
        setSession(value);
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  async function login() {
    setLoading(true);
    try {
      const nextSession = await loginWithKeycloakAsync();
      setSession(nextSession);
    } finally {
      setLoading(false);
    }
  }

  async function logout() {
    setLoading(true);
    try {
      await logoutAsync();
      setSession(null);
    } finally {
      setLoading(false);
    }
  }

  const value = useMemo<AuthContextValue>(
    () => ({
      loading,
      session,
      login,
      logout,
    }),
    [loading, session],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }

  return context;
}
