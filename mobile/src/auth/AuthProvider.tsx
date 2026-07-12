import React, { createContext, useContext, useEffect, useMemo, useRef, useState } from "react";
import { Alert, View } from "react-native";
import { AuthSessionState } from "./authTypes";
import {
  loginWithKeycloakAsync,
  logoutAsync,
  refreshSessionAsync,
  restoreSessionAsync,
} from "./keycloakMobileAuth";
import { crearSessionRefreshCore } from "./sessionRefreshCore.js";
import { SessionExpiryModal } from "./SessionExpiryModal";

const REFRESH_INTERVAL_MS = 270_000;

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
  const [modalVisible, setModalVisible] = useState(false);
  const coreRef = useRef<ReturnType<typeof crearSessionRefreshCore> | null>(null);
  // logout se recrea en cada render: el core lo alcanza por ref.
  const logoutRef = useRef<() => Promise<void>>(async () => {});

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
  logoutRef.current = logout;

  const haySesion = session != null;
  useEffect(() => {
    if (!haySesion) return;

    const core = crearSessionRefreshCore({
      refrescar: async () => {
        const nueva = await refreshSessionAsync();
        if (nueva) {
          setSession(nueva);
          return true;
        }
        return false;
      },
      onModal: setModalVisible,
      onExpirada: () => {
        Alert.alert("Sesión expirada", "Tu sesión expiró. Inicia sesión de nuevo.");
        void logoutRef.current();
      },
    });
    coreRef.current = core;
    const interval = setInterval(() => void core.tick(), REFRESH_INTERVAL_MS);

    return () => {
      clearInterval(interval);
      coreRef.current = null;
      setModalVisible(false);
    };
  }, [haySesion]);

  const value = useMemo<AuthContextValue>(
    () => ({
      loading,
      session,
      login,
      logout,
    }),
    [loading, session],
  );

  return (
    <AuthContext.Provider value={value}>
      <View
        style={{ flex: 1 }}
        onStartShouldSetResponderCapture={() => {
          coreRef.current?.marcarActividad();
          return false;
        }}
      >
        {children}
        <SessionExpiryModal
          visible={modalVisible}
          onContinuar={() => void coreRef.current?.continuar()}
          onSalir={() => void logout()}
        />
      </View>
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }

  return context;
}
