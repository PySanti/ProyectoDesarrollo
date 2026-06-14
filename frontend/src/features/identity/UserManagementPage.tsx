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
import { RefreshCw, Users } from "../../shell/icons";

interface UserManagementPageProps {
  accessToken: string;
}

const PAGE_SIZE = 8;

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
  const [page, setPage] = useState(1);

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
      setPage(1);
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
      setActionError("El correo es inválido.");
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

  const totalPages = Math.max(1, Math.ceil(users.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageUsers = users.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  return (
    <div className="page">
      <div className="stack">
        <div>
          <h1>Gestión de usuarios</h1>
          <p className="muted">
            Consulta, actualiza datos generales y desactiva usuarios. El rol inicial se asigna al
            crear y no se gestiona desde aquí.
          </p>
        </div>

        {listError ? (
          <div className="notice error" role="alert">
            {listError}
          </div>
        ) : null}

        <div className="card stack">
          <div className="card-head">
            <h2 className="q-title">
              Usuarios
              {users.length > 0 ? <span className="badge">{users.length}</span> : null}
            </h2>
            <button
              type="button"
              className="secondary-button btn-icon"
              onClick={loadUsers}
              disabled={isLoadingList}
            >
              <RefreshCw className={isLoadingList ? "ops-spin" : undefined} />
              {isLoadingList ? "Cargando…" : "Recargar lista"}
            </button>
          </div>

          {users.length === 0 && !isLoadingList ? (
            <div className="empty-panel">
              <Users />
              <p>No hay usuarios registrados todavía.</p>
              <p className="muted">
                Crea el primero en <strong>Crear usuario</strong>.
              </p>
            </div>
          ) : (
            <>
              <div className="table-wrap">
                <table aria-label="Usuarios">
                  <thead>
                    <tr>
                      <th scope="col">Nombre</th>
                      <th scope="col">Correo</th>
                      <th scope="col">Estado</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pageUsers.map((user) => (
                      <tr key={user.userId}>
                        <td>
                          <button
                            type="button"
                            className="row-link"
                            onClick={() => onSelectUser(user.userId)}
                          >
                            {user.name}
                          </button>
                        </td>
                        <td>{user.email}</td>
                        <td>
                          <StatusPill status={user.status} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {users.length > PAGE_SIZE ? (
                <div className="card-head">
                  <span className="muted">
                    Página {currentPage} de {totalPages} · {users.length} usuarios
                  </span>
                  <div className="compact-actions">
                    <button
                      type="button"
                      className="secondary-button"
                      disabled={currentPage === 1}
                      onClick={() => setPage((current) => Math.max(1, current - 1))}
                    >
                      Anterior
                    </button>
                    <button
                      type="button"
                      className="secondary-button"
                      disabled={currentPage === totalPages}
                      onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
                    >
                      Siguiente
                    </button>
                  </div>
                </div>
              ) : null}
            </>
          )}
        </div>

        {detailError ? (
          <div className="notice error" role="alert">
            {detailError}
          </div>
        ) : null}

        {isLoadingDetail ? <p className="muted">Cargando detalle…</p> : null}

        {selectedUser ? (
          <div className="card stack">
            <div className="card-head">
              <h2>Detalle de usuario</h2>
              <StatusPill status={selectedUser.status} />
            </div>
            <p className="muted">
              ID: <span className="mono">{selectedUser.userId}</span>
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

              <div className="row">
                <button type="submit" disabled={isSubmitting}>
                  {isSubmitting ? "Guardando…" : "Guardar datos"}
                </button>

                <button
                  type="button"
                  className="secondary-button"
                  disabled={isDeactivating || selectedUser.status === "Desactivado"}
                  onClick={onDeactivateUser}
                >
                  {isDeactivating ? "Desactivando…" : "Desactivar usuario"}
                </button>
              </div>
            </form>
          </div>
        ) : null}
      </div>
    </div>
  );
}

function StatusPill({ status }: { status: string }) {
  const variant = status === "Activo" ? "pill--ok" : "pill--done";
  return (
    <span className={`pill ${variant}`}>
      <span className="pill__dot" />
      {status}
    </span>
  );
}

function mapHu02ErrorMessage(statusCode: number, fallbackMessage: string): string {
  switch (statusCode) {
    case 401:
      return "No autenticado. Inicia sesión nuevamente.";
    case 403:
      return "No autorizado. Debes tener rol Administrador.";
    case 404:
      return "Usuario no encontrado.";
    case 409:
      return "El correo ya existe en UMBRAL o Keycloak.";
    case 400:
      return "Solicitud inválida. Verifica los datos enviados.";
    default:
      return fallbackMessage || "Error inesperado en Identity Service.";
  }
}
