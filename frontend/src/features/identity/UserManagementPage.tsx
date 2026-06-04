import { FormEvent, useEffect, useState } from "react";
import {
  deactivateIdentityUser,
  getIdentityUserById,
  getIdentityUsers,
  IdentityApiError,
  IdentityUserDetail,
  IdentityUserSummary,
  updateIdentityUserGeneralData
} from "../../api/identityApi";

interface UserManagementPageProps {
  accessToken: string;
}

export function UserManagementPage({ accessToken }: UserManagementPageProps) {
  const [users, setUsers] = useState<IdentityUserSummary[]>([]);
  const [selectedUser, setSelectedUser] = useState<IdentityUserDetail | null>(null);
  const [listError, setListError] = useState<string | null>(null);
  const [detailError, setDetailError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isLoadingList, setIsLoadingList] = useState(false);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDeactivating, setIsDeactivating] = useState(false);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");

  useEffect(() => {
    void loadUsers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadUsers() {
    setIsLoadingList(true);
    setListError(null);
    try {
      const response = await getIdentityUsers(accessToken);
      setUsers(response);
    } catch (caught) {
      if (caught instanceof IdentityApiError) {
        setListError(mapHu02ErrorMessage(caught.statusCode, caught.message));
      } else {
        setListError("Error inesperado al consultar usuarios.");
      }
    } finally {
      setIsLoadingList(false);
    }
  }

  async function onSelectUser(userId: string) {
    setIsLoadingDetail(true);
    setDetailError(null);
    setActionError(null);
    setSuccessMessage(null);

    try {
      const detail = await getIdentityUserById(userId, accessToken);
      setSelectedUser(detail);
      setName(detail.name);
      setEmail(detail.email);
    } catch (caught) {
      if (caught instanceof IdentityApiError) {
        setDetailError(mapHu02ErrorMessage(caught.statusCode, caught.message));
      } else {
        setDetailError("Error inesperado al consultar detalle de usuario.");
      }
      setSelectedUser(null);
    } finally {
      setIsLoadingDetail(false);
    }
  }

  async function onUpdateUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!selectedUser) {
      return;
    }

    setActionError(null);
    setSuccessMessage(null);

    if (!name.trim()) {
      setActionError("El nombre es obligatorio.");
      return;
    }

    if (!email.trim() || !email.includes("@")) {
      setActionError("El correo es invalido.");
      return;
    }

    setIsSubmitting(true);
    try {
      const updated = await updateIdentityUserGeneralData(
        selectedUser.userId,
        { name: name.trim(), email: email.trim() },
        accessToken
      );

      setSelectedUser(updated);
      setSuccessMessage("Datos generales actualizados correctamente.");
      setUsers((current) =>
        current.map((user) => (user.userId === updated.userId ? updated : user))
      );
    } catch (caught) {
      if (caught instanceof IdentityApiError) {
        setActionError(mapHu02ErrorMessage(caught.statusCode, caught.message));
      } else {
        setActionError("Error inesperado al actualizar usuario.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  async function onDeactivateUser() {
    if (!selectedUser || selectedUser.status === "Desactivado") {
      return;
    }

    setActionError(null);
    setSuccessMessage(null);
    setIsDeactivating(true);

    try {
      const response = await deactivateIdentityUser(selectedUser.userId, accessToken);
      const updatedUser: IdentityUserDetail = { ...selectedUser, status: response.status };
      setSelectedUser(updatedUser);
      setUsers((current) =>
        current.map((user) =>
          user.userId === updatedUser.userId ? { ...user, status: "Desactivado" } : user
        )
      );
      setSuccessMessage("Usuario desactivado correctamente.");
    } catch (caught) {
      if (caught instanceof IdentityApiError) {
        setActionError(mapHu02ErrorMessage(caught.statusCode, caught.message));
      } else {
        setActionError("Error inesperado al desactivar usuario.");
      }
    } finally {
      setIsDeactivating(false);
    }
  }

  return (
    <div className="card">
      <h1>Gestion de usuarios</h1>
      <p>HU-02: consultar detalle, editar datos generales y desactivar usuarios.</p>

      {listError ? (
        <div className="notice error" role="alert">
          {listError}
        </div>
      ) : null}

      <button type="button" onClick={loadUsers} disabled={isLoadingList}>
        {isLoadingList ? "Cargando..." : "Recargar lista"}
      </button>

      <h2>Usuarios</h2>
      <ul className="clean-list">
        {users.map((user) => (
          <li key={user.userId}>
            <button type="button" onClick={() => onSelectUser(user.userId)}>
              {user.name} ({user.email}) - {user.status}
            </button>
          </li>
        ))}
      </ul>

      {detailError ? (
        <div className="notice error" role="alert">
          {detailError}
        </div>
      ) : null}

      {isLoadingDetail ? <p>Cargando detalle...</p> : null}

      {selectedUser ? (
        <>
          <h2>Detalle de usuario</h2>

          <p>
            <strong>UserId:</strong> {selectedUser.userId}
          </p>
          <p>
            <strong>Rol:</strong> {selectedUser.role}
          </p>
          <p>
            <strong>Estado:</strong> {selectedUser.status}
          </p>

          {actionError ? (
            <div className="notice error" role="alert">
              {actionError}
            </div>
          ) : null}

          {successMessage ? (
            <div className="notice success" data-testid="hu02-success">
              {successMessage}
            </div>
          ) : null}

          <form onSubmit={onUpdateUser} noValidate>
            <label htmlFor="edit-name">
              Nombre
              <input
                id="edit-name"
                value={name}
                onChange={(event) => setName(event.target.value)}
                autoComplete="name"
              />
            </label>

            <label htmlFor="edit-email">
              Correo
              <input
                id="edit-email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                autoComplete="email"
              />
            </label>

            <label htmlFor="edit-role-readonly">
              Rol (solo lectura)
              <input id="edit-role-readonly" value={selectedUser.role} readOnly />
            </label>

            <div className="row">
              <button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Guardando..." : "Guardar datos"}
              </button>

              <button
                type="button"
                disabled={isDeactivating || selectedUser.status === "Desactivado"}
                onClick={onDeactivateUser}
              >
                {isDeactivating ? "Desactivando..." : "Desactivar usuario"}
              </button>
            </div>
          </form>
        </>
      ) : null}
    </div>
  );
}

function mapHu02ErrorMessage(statusCode: number, fallbackMessage: string): string {
  switch (statusCode) {
    case 401:
      return "No autenticado. Inicia sesion nuevamente.";
    case 403:
      return "No autorizado. Debes tener rol Administrador.";
    case 404:
      return "Usuario no encontrado.";
    case 409:
      return "El correo ya existe.";
    case 400:
      return "Solicitud invalida. Verifica los datos enviados.";
    default:
      return fallbackMessage || "Error inesperado en Identity Service.";
  }
}
