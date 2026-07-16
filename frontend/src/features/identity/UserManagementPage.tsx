import { FormEvent, useEffect, useState } from "react";
import {
  changeUserRole,
  deactivateIdentityUser,
  getIdentityUserById,
  getIdentityUsers,
  IdentityApiError,
  IdentityUserDetail,
  IdentityUserSummary,
  updateIdentityUserGeneralData
} from "../../api/identityApi";
import { RefreshCw, Users } from "../../shell/icons";
import { Field } from "../../shared/Field";
import { correo, nombrePersona } from "../../shared/validation";

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
  const [nameTouched, setNameTouched] = useState(false);
  const [emailTouched, setEmailTouched] = useState(false);
  const [serverFieldErrors, setServerFieldErrors] = useState<Record<string, string>>({});
  const [page, setPage] = useState(1);
  const [roleModalUser, setRoleModalUser] = useState<IdentityUserSummary | null>(null);
  const [roleTarget, setRoleTarget] = useState("");
  const [roleArmed, setRoleArmed] = useState(false);
  const [roleError, setRoleError] = useState<string | null>(null);
  const [roleSaving, setRoleSaving] = useState(false);
  const [roleSuccess, setRoleSuccess] = useState<string | null>(null);

  // El front no sabe si el usuario aún tiene contraseña temporal (eso lo decide el backend
  // contra Keycloak), pero sí sabe si el correo cambió, que es la condición para que se reenvíe.
  const emailChanged =
    selectedUser != null && email.trim().toLowerCase() !== selectedUser.email.toLowerCase();

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
      setNameTouched(false);
      setEmailTouched(false);
      setServerFieldErrors({});
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
    setServerFieldErrors({});

    setNameTouched(true);
    setEmailTouched(true);
    if (nombrePersona(name) || correo(email)) {
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
        if (caught.fieldErrors) {
          setServerFieldErrors(caught.fieldErrors);
        }
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

  function openRoleModal(user: IdentityUserSummary) {
    setRoleModalUser(user);
    setRoleTarget("");
    setRoleArmed(false);
    setRoleError(null);
    setRoleSuccess(null);
  }

  function closeRoleModal() {
    if (roleSaving) {
      return;
    }
    setRoleModalUser(null);
  }

  async function onChangeRole() {
    if (!roleModalUser || !roleTarget) {
      return;
    }

    // Promoción a admin es irreversible: primer click arma, segundo ejecuta.
    if (roleTarget === "Administrador" && !roleArmed) {
      setRoleArmed(true);
      return;
    }

    setRoleSaving(true);
    setRoleError(null);
    try {
      const response = await changeUserRole(roleModalUser.userId, roleTarget, accessToken);
      setUsers((current) =>
        current.map((user) =>
          user.userId === response.usuarioId ? { ...user, role: response.rol } : user
        )
      );
      setRoleSuccess(`Rol de ${roleModalUser.name} actualizado a ${response.rol}.`);
      setRoleModalUser(null);
    } catch (caught) {
      setRoleError(mapRoleChangeError(caught));
    } finally {
      setRoleSaving(false);
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
            Panel de gestión de usuarios.
          </p>
        </div>

        {listError ? (
          <div className="notice error" role="alert">
            {listError}
          </div>
        ) : null}

        {roleSuccess ? (
          <div className="notice success" role="status" data-testid="role-change-success">
            {roleSuccess}
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
                      <th scope="col">Rol</th>
                      <th scope="col">Acciones</th>
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
                        <td>{user.role}</td>
                        <td>
                          <button
                            type="button"
                            className="secondary-button"
                            data-testid={`role-change-open-${user.userId}`}
                            disabled={user.role === "Administrador"}
                            title={
                              user.role === "Administrador"
                                ? "El rol de un Administrador es inmutable."
                                : undefined
                            }
                            onClick={() => openRoleModal(user)}
                          >
                            Cambiar rol
                          </button>
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
              <Field
                id="edit-name"
                label="Nombre"
                value={name}
                error={
                  serverFieldErrors.name ||
                  (name.trim() !== "" || nameTouched ? nombrePersona(name) : null)
                }
                onChange={(event) => {
                  setName(event.target.value);
                  setServerFieldErrors((previous) => ({ ...previous, name: "" }));
                }}
                onBlur={() => setNameTouched(true)}
                autoComplete="name"
              />

              <Field
                id="edit-email"
                label="Correo"
                value={email}
                error={
                  serverFieldErrors.email ||
                  (email.trim() !== "" || emailTouched ? correo(email) : null)
                }
                onChange={(event) => {
                  setEmail(event.target.value);
                  setServerFieldErrors((previous) => ({ ...previous, email: "" }));
                }}
                onBlur={() => setEmailTouched(true)}
                autoComplete="email"
              />

              <p className="muted" data-testid="hu02-email-hint">
                Si cambias el correo de un usuario que aún no ha iniciado sesión, se le enviará un{" "}
                <strong>correo</strong> con su nueva contraseña temporal al nuevo correo.
              </p>

              {isSubmitting && emailChanged ? (
                <p className="muted" role="status" data-testid="hu02-update-status">
                  Guardando los cambios y, si el usuario aún no ha iniciado sesión, enviando el correo
                  con su nueva contraseña temporal…
                </p>
              ) : null}

              <div className="row">
                <button type="submit" disabled={isSubmitting}>
                  {isSubmitting
                    ? emailChanged
                      ? "Guardando y enviando correo…"
                      : "Guardando…"
                    : "Guardar datos"}
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

      {roleModalUser ? (
        <div className="modal-backdrop" role="presentation">
          <section
            className="modal-card"
            role="dialog"
            aria-modal="true"
            aria-labelledby="role-change-title"
            data-testid="role-change-modal"
          >
            <div className="modal-header">
              <div>
                <span className="badge">Cambiar rol</span>
                <h2 id="role-change-title">{roleModalUser.name}</h2>
              </div>
              <button type="button" className="secondary-button" onClick={closeRoleModal}>
                Cerrar
              </button>
            </div>

            <p className="muted">
              {roleModalUser.email} · Rol actual: <strong>{roleModalUser.role}</strong>
            </p>

            <label htmlFor="role-change-select">
              Nuevo rol
              <select
                id="role-change-select"
                data-testid="role-change-select"
                value={roleTarget}
                disabled={roleSaving}
                onChange={(event) => {
                  setRoleTarget(event.target.value);
                  setRoleArmed(false);
                  setRoleError(null);
                }}
              >
                <option value="">Selecciona un rol…</option>
                {(["Administrador", "Operador", "Participante"] as const)
                  .filter((rol) => rol !== roleModalUser.role)
                  .map((rol) => (
                    <option key={rol} value={rol}>
                      {rol}
                    </option>
                  ))}
              </select>
            </label>

            {roleTarget === "Administrador" ? (
              <div className="notice info" role="alert" data-testid="role-change-warning">
                Promover a Administrador es irreversible: el rol de un administrador no puede
                volver a cambiarse.
              </div>
            ) : null}

            {roleError ? (
              <div className="notice error" role="alert" data-testid="role-change-error">
                {roleError}
              </div>
            ) : null}

            <div className="row">
              <button
                type="button"
                data-testid="role-change-confirm"
                disabled={roleSaving || !roleTarget}
                onClick={onChangeRole}
              >
                {roleSaving
                  ? "Cambiando…"
                  : roleTarget === "Administrador" && roleArmed
                    ? "Entiendo, promover"
                    : "Cambiar rol"}
              </button>
              <button
                type="button"
                className="secondary-button"
                disabled={roleSaving}
                onClick={closeRoleModal}
              >
                Cancelar
              </button>
            </div>
          </section>
        </div>
      ) : null}
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
    case 502:
      // Al cambiar el correo de un usuario con contraseña temporal pendiente, el backend
      // reenvía credenciales; si falla (correo o Keycloak) revierte el cambio.
      if (/smtp|email|correo/i.test(fallbackMessage)) {
        return "No se pudo enviar el correo con la nueva contraseña temporal. El cambio se revirtió; revisa la configuración de correo (SMTP) e inténtalo nuevamente.";
      }
      return "Error de integración con Keycloak al actualizar el usuario. El cambio se revirtió; inténtalo nuevamente.";
    default:
      return fallbackMessage || "Error inesperado en Identity Service.";
  }
}

function mapRoleChangeError(caught: unknown): string {
  if (!(caught instanceof IdentityApiError)) {
    return "Error inesperado al cambiar el rol.";
  }
  switch (caught.statusCode) {
    case 502:
      return "Keycloak no disponible. Inténtalo de nuevo.";
    case 409:
    case 400:
    case 404:
      return caught.message || "No fue posible cambiar el rol.";
    default:
      return caught.message || "Error inesperado en Identity Service.";
  }
}
