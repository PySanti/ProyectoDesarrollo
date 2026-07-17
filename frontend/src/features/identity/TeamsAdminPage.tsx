import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  AdminTeam,
  createAdminTeam,
  deleteAdminTeam,
  DeleteAdminTeamResult,
  IdentityApiError,
  listAdminTeams,
  reassignAdminTeamLeader,
  renameAdminTeam,
  setAdminTeamEstado
} from "../../api/adminTeamsApi";
import { getIdentityUsers, IdentityUserSummary } from "../../api/identityApi";
import { Flag } from "../../shell/icons";
import { Field } from "../../shared/Field";
import { nombreEquipo } from "../../shared/validation";

interface TeamsAdminPageProps {
  accessToken: string;
}

export function TeamsAdminPage({ accessToken }: TeamsAdminPageProps) {
  const [teams, setTeams] = useState<AdminTeam[]>([]);
  const [users, setUsers] = useState<IdentityUserSummary[]>([]);
  const [listError, setListError] = useState<string | null>(null);
  const [isLoadingList, setIsLoadingList] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const [createName, setCreateName] = useState("");
  const [createNameTouched, setCreateNameTouched] = useState(false);
  const [createLiderUserId, setCreateLiderUserId] = useState("");
  const [createError, setCreateError] = useState<string | null>(null);
  const [createSuccessMessage, setCreateSuccessMessage] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  const [renameTeam, setRenameTeam] = useState<AdminTeam | null>(null);
  const [renameValue, setRenameValue] = useState("");
  const [renameTouched, setRenameTouched] = useState(false);
  const [renameError, setRenameError] = useState<string | null>(null);
  const [renameSaving, setRenameSaving] = useState(false);

  const [reassignTeam, setReassignTeam] = useState<AdminTeam | null>(null);
  const [reassignValue, setReassignValue] = useState("");
  const [reassignError, setReassignError] = useState<string | null>(null);
  const [reassignSaving, setReassignSaving] = useState(false);

  const [estadoSavingId, setEstadoSavingId] = useState<string | null>(null);
  const [estadoError, setEstadoError] = useState<string | null>(null);

  const [deleteTeam, setDeleteTeam] = useState<AdminTeam | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteSaving, setDeleteSaving] = useState(false);

  // El id de líder/integrante viene como KeycloakId (join equipos↔usuarios por
  // KeycloakId) o, en algunos payloads, como UsuarioId local. Indexar por ambos
  // para resolver el nombre sin importar cuál llegue.
  const nombrePorId = useMemo(() => {
    const mapa = new Map<string, string>();
    for (const user of users) {
      mapa.set(user.keycloakId, user.name);
      mapa.set(user.userId, user.name);
    }
    return mapa;
  }, [users]);

  useEffect(() => {
    void loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadAll() {
    setIsLoadingList(true);
    setListError(null);
    try {
      const [teamsResponse, usersResponse] = await Promise.all([
        listAdminTeams(accessToken),
        getIdentityUsers(accessToken)
      ]);
      setTeams(teamsResponse);
      setUsers(usersResponse);
    } catch (caught) {
      setListError(mapAdminTeamErrorMessage(caught, "list"));
    } finally {
      setIsLoadingList(false);
    }
  }

  async function onCreateTeam(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setCreateError(null);
    setCreateSuccessMessage(null);

    setCreateNameTouched(true);
    if (nombreEquipo(createName)) {
      return;
    }

    if (!createLiderUserId) {
      setCreateError("Selecciona un líder para el equipo.");
      return;
    }

    setIsCreating(true);
    try {
      await createAdminTeam(
        { nombreEquipo: createName.trim(), liderUserId: createLiderUserId },
        accessToken
      );
      setCreateName("");
      setCreateNameTouched(false);
      setCreateLiderUserId("");
      setCreateSuccessMessage("Equipo creado correctamente.");
      await loadAll();
    } catch (caught) {
      setCreateError(mapAdminTeamErrorMessage(caught, "create"));
    } finally {
      setIsCreating(false);
    }
  }

  function openRenameModal(team: AdminTeam) {
    setRenameTeam(team);
    setRenameValue(team.nombreEquipo);
    setRenameTouched(false);
    setRenameError(null);
  }

  function closeRenameModal() {
    if (renameSaving) {
      return;
    }
    setRenameTeam(null);
  }

  async function onConfirmRename() {
    if (!renameTeam) {
      return;
    }

    setRenameTouched(true);
    if (nombreEquipo(renameValue)) {
      return;
    }

    setRenameSaving(true);
    setRenameError(null);
    try {
      const updated = await renameAdminTeam(
        renameTeam.equipoId,
        { nombreEquipo: renameValue.trim() },
        accessToken
      );
      setTeams((current) =>
        current.map((team) => (team.equipoId === updated.equipoId ? updated : team))
      );
      setSuccessMessage(`Equipo renombrado a "${updated.nombreEquipo}".`);
      setRenameTeam(null);
    } catch (caught) {
      setRenameError(mapAdminTeamErrorMessage(caught, "rename"));
    } finally {
      setRenameSaving(false);
    }
  }

  function openReassignModal(team: AdminTeam) {
    setReassignTeam(team);
    setReassignValue("");
    setReassignError(null);
  }

  function closeReassignModal() {
    if (reassignSaving) {
      return;
    }
    setReassignTeam(null);
  }

  async function onConfirmReassign() {
    if (!reassignTeam || !reassignValue) {
      return;
    }

    setReassignSaving(true);
    setReassignError(null);
    try {
      const updated = await reassignAdminTeamLeader(
        reassignTeam.equipoId,
        { nuevoLiderUserId: reassignValue },
        accessToken
      );
      setTeams((current) =>
        current.map((team) => (team.equipoId === updated.equipoId ? updated : team))
      );
      setSuccessMessage("Liderazgo reasignado correctamente.");
      setReassignTeam(null);
    } catch (caught) {
      setReassignError(mapAdminTeamErrorMessage(caught, "reassign"));
    } finally {
      setReassignSaving(false);
    }
  }

  async function onToggleEstado(team: AdminTeam) {
    if (team.estado === "Eliminado") {
      return;
    }

    const nuevoEstado = team.estado === "Activo" ? "Desactivado" : "Activo";
    setEstadoSavingId(team.equipoId);
    setEstadoError(null);
    setSuccessMessage(null);
    try {
      const updated = await setAdminTeamEstado(team.equipoId, { estado: nuevoEstado }, accessToken);
      setTeams((current) =>
        current.map((current_) => (current_.equipoId === updated.equipoId ? updated : current_))
      );
      setSuccessMessage(`Equipo ${updated.nombreEquipo} ahora está ${updated.estado.toLowerCase()}.`);
    } catch (caught) {
      setEstadoError(mapAdminTeamErrorMessage(caught, "estado"));
    } finally {
      setEstadoSavingId(null);
    }
  }

  function openDeleteModal(team: AdminTeam) {
    setDeleteTeam(team);
    setDeleteError(null);
  }

  function closeDeleteModal() {
    if (deleteSaving) {
      return;
    }
    setDeleteTeam(null);
  }

  async function onConfirmDelete() {
    if (!deleteTeam) {
      return;
    }

    setDeleteSaving(true);
    setDeleteError(null);
    try {
      const resultado = await deleteAdminTeam(deleteTeam.equipoId, accessToken);
      setSuccessMessage(mensajeEliminacion(resultado));
      setDeleteTeam(null);
      await loadAll();
    } catch (caught) {
      setDeleteError(mapAdminTeamErrorMessage(caught, "delete"));
    } finally {
      setDeleteSaving(false);
    }
  }

  return (
    <div className="page">
      <div className="stack">
        <div>
          <h1>Equipos</h1>
          <p className="muted">
            Panel de administración de equipos.
          </p>
        </div>

        {listError ? (
          <div className="notice error" role="alert">
            {listError}
          </div>
        ) : null}

        {successMessage ? (
          <div className="notice success" role="status" data-testid="teams-action-success">
            {successMessage}
          </div>
        ) : null}

        <div className="card stack">
          <div className="card-head">
            <h2 className="q-title">Crear equipo</h2>
          </div>

          {createError ? (
            <div className="notice error" role="alert" data-testid="create-team-error">
              {createError}
            </div>
          ) : null}

          {createSuccessMessage ? (
            <div className="notice success" data-testid="create-team-success">
              {createSuccessMessage}
            </div>
          ) : null}

          <form onSubmit={onCreateTeam} noValidate>
            <div className="row">
              <Field
                id="create-team-name"
                label="Nombre del equipo"
                value={createName}
                error={
                  createName.trim() !== "" || createNameTouched
                    ? nombreEquipo(createName)
                    : null
                }
                onChange={(event) => setCreateName(event.target.value)}
                onBlur={() => setCreateNameTouched(true)}
                autoComplete="off"
              />

              <label htmlFor="create-team-leader">
                Líder inicial
                <select
                  id="create-team-leader"
                  data-testid="create-team-leader-select"
                  value={createLiderUserId}
                  onChange={(event) => setCreateLiderUserId(event.target.value)}
                >
                  <option value="">Selecciona un usuario…</option>
                  {users.map((user) => (
                    <option key={user.userId} value={user.userId}>
                      {user.name} ({user.email})
                    </option>
                  ))}
                </select>
              </label>
            </div>

            <button type="submit" data-testid="create-team-submit" disabled={isCreating}>
              {isCreating ? "Creando…" : "Crear equipo"}
            </button>
          </form>
        </div>

        <div className="card stack">
          <div className="card-head">
            <h2 className="q-title">
              Equipos
              {teams.length > 0 ? <span className="badge">{teams.length}</span> : null}
            </h2>
          </div>

          {teams.length === 0 && !isLoadingList ? (
            <div className="empty-panel">
              <Flag />
              <p>No hay equipos registrados todavía.</p>
              <p className="muted">
                Crea el primero en <strong>Crear equipo</strong>.
              </p>
            </div>
          ) : (
            <div className="table-wrap">
              <table aria-label="Equipos">
                <thead>
                  <tr>
                    <th scope="col">Nombre</th>
                    <th scope="col">Estado</th>
                    <th scope="col">Integrantes</th>
                    <th scope="col">Líder</th>
                    <th scope="col">Acciones</th>
                  </tr>
                </thead>
                <tbody>
                  {teams.map((team) => {
                    const lider = team.integrantes.find((integrante) => integrante.esLider);
                    const liderId = lider?.usuarioId ?? team.liderUserId;
                    return (
                      <tr key={team.equipoId}>
                        <td>{team.nombreEquipo}</td>
                        <td>
                          <TeamEstadoPill estado={team.estado} />
                        </td>
                        <td>{team.integrantes.length}</td>
                        <td>{(liderId && nombrePorId.get(liderId)) ?? liderId ?? "-"}</td>
                        <td>
                          <div className="compact-actions">
                            <button
                              type="button"
                              className="secondary-button"
                              data-testid={`team-rename-open-${team.equipoId}`}
                              disabled={team.estado === "Eliminado"}
                              onClick={() => openRenameModal(team)}
                            >
                              Renombrar
                            </button>
                            <button
                              type="button"
                              className="secondary-button"
                              data-testid={`team-reassign-open-${team.equipoId}`}
                              disabled={team.estado === "Eliminado" || team.integrantes.length < 2}
                              onClick={() => openReassignModal(team)}
                            >
                              Reasignar líder
                            </button>
                            {team.estado !== "Eliminado" ? (
                              <button
                                type="button"
                                className="secondary-button"
                                data-testid={`team-estado-toggle-${team.equipoId}`}
                                disabled={estadoSavingId === team.equipoId}
                                onClick={() => onToggleEstado(team)}
                              >
                                {estadoSavingId === team.equipoId
                                  ? "Guardando…"
                                  : team.estado === "Activo"
                                    ? "Desactivar"
                                    : "Activar"}
                              </button>
                            ) : null}
                            <button
                              type="button"
                              className="secondary-button"
                              data-testid={`team-delete-open-${team.equipoId}`}
                              disabled={team.estado === "Eliminado"}
                              onClick={() => openDeleteModal(team)}
                            >
                              Eliminar
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}

          {estadoError ? (
            <div className="notice error" role="alert">
              {estadoError}
            </div>
          ) : null}
        </div>
      </div>

      {renameTeam ? (
        <div className="modal-backdrop" role="presentation">
          <section
            className="modal-card"
            role="dialog"
            aria-modal="true"
            aria-labelledby="rename-team-title"
            data-testid="rename-team-modal"
          >
            <div className="modal-header">
              <div>
                <span className="badge">Renombrar equipo</span>
                <h2 id="rename-team-title">{renameTeam.nombreEquipo}</h2>
              </div>
              <button type="button" className="secondary-button" onClick={closeRenameModal}>
                Cerrar
              </button>
            </div>

            <Field
              id="rename-team-input"
              data-testid="rename-team-input"
              label="Nuevo nombre"
              value={renameValue}
              disabled={renameSaving}
              error={
                renameValue.trim() !== "" || renameTouched ? nombreEquipo(renameValue) : null
              }
              onChange={(event) => setRenameValue(event.target.value)}
              onBlur={() => setRenameTouched(true)}
            />

            {renameError ? (
              <div className="notice error" role="alert" data-testid="rename-team-error">
                {renameError}
              </div>
            ) : null}

            <div className="row">
              <button
                type="button"
                data-testid="rename-team-confirm"
                disabled={renameSaving}
                onClick={onConfirmRename}
              >
                {renameSaving ? "Guardando…" : "Guardar nombre"}
              </button>
              <button
                type="button"
                className="secondary-button"
                disabled={renameSaving}
                onClick={closeRenameModal}
              >
                Cancelar
              </button>
            </div>
          </section>
        </div>
      ) : null}

      {reassignTeam ? (
        <div className="modal-backdrop" role="presentation">
          <section
            className="modal-card"
            role="dialog"
            aria-modal="true"
            aria-labelledby="reassign-team-title"
            data-testid="reassign-team-modal"
          >
            <div className="modal-header">
              <div>
                <span className="badge">Reasignar líder</span>
                <h2 id="reassign-team-title">{reassignTeam.nombreEquipo}</h2>
              </div>
              <button type="button" className="secondary-button" onClick={closeReassignModal}>
                Cerrar
              </button>
            </div>

            <label htmlFor="reassign-team-select">
              Nuevo líder
              <select
                id="reassign-team-select"
                data-testid="reassign-team-select"
                value={reassignValue}
                disabled={reassignSaving}
                onChange={(event) => setReassignValue(event.target.value)}
              >
                <option value="">Selecciona un integrante…</option>
                {reassignTeam.integrantes
                  .filter((integrante) => !integrante.esLider)
                  .map((integrante) => (
                    <option key={integrante.usuarioId} value={integrante.usuarioId}>
                      {nombrePorId.get(integrante.usuarioId) ?? integrante.usuarioId}
                    </option>
                  ))}
              </select>
            </label>

            {reassignError ? (
              <div className="notice error" role="alert" data-testid="reassign-team-error">
                {reassignError}
              </div>
            ) : null}

            <div className="row">
              <button
                type="button"
                data-testid="reassign-team-confirm"
                disabled={reassignSaving || !reassignValue}
                onClick={onConfirmReassign}
              >
                {reassignSaving ? "Reasignando…" : "Reasignar líder"}
              </button>
              <button
                type="button"
                className="secondary-button"
                disabled={reassignSaving}
                onClick={closeReassignModal}
              >
                Cancelar
              </button>
            </div>
          </section>
        </div>
      ) : null}

      {deleteTeam ? (
        <div className="modal-backdrop" role="presentation">
          <section
            className="modal-card"
            role="dialog"
            aria-modal="true"
            aria-labelledby="delete-team-title"
            data-testid="delete-team-modal"
          >
            <div className="modal-header">
              <div>
                <span className="badge">Eliminar equipo</span>
                <h2 id="delete-team-title">{deleteTeam.nombreEquipo}</h2>
              </div>
              <button type="button" className="secondary-button" onClick={closeDeleteModal}>
                Cerrar
              </button>
            </div>

            <p className="muted">
              Esta acción es irreversible: el equipo pasará a estado Eliminado y sus integrantes
              quedarán libres para unirse a otro equipo.
            </p>

            {deleteError ? (
              <div className="notice error" role="alert" data-testid="delete-team-error">
                {deleteError}
              </div>
            ) : null}

            {deleteSaving ? (
              <p className="muted" role="status" data-testid="delete-team-status">
                Eliminando el equipo y notificando por correo a los integrantes…
              </p>
            ) : null}

            <div className="row">
              <button
                type="button"
                data-testid="delete-team-confirm"
                disabled={deleteSaving}
                onClick={onConfirmDelete}
              >
                {deleteSaving ? "Eliminando y notificando…" : "Confirmar eliminación"}
              </button>
              <button
                type="button"
                className="secondary-button"
                data-testid="delete-team-cancel"
                disabled={deleteSaving}
                onClick={closeDeleteModal}
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

function mensajeEliminacion(resultado: DeleteAdminTeamResult): string {
  return `Equipo ${resultado.nombreEquipo} eliminado correctamente. Se notificará a los integrantes por correo.`;
}

function TeamEstadoPill({ estado }: { estado: AdminTeam["estado"] }) {
  const variant = estado === "Activo" ? "pill--ok" : estado === "Desactivado" ? "pill--done" : "pill--cancel";
  return (
    <span className={`pill ${variant}`}>
      <span className="pill__dot" />
      {estado}
    </span>
  );
}

function mapAdminTeamErrorMessage(
  caught: unknown,
  context: "list" | "create" | "rename" | "reassign" | "estado" | "delete"
): string {
  if (!(caught instanceof IdentityApiError)) {
    return "Error inesperado en Identity Service.";
  }

  switch (caught.statusCode) {
    case 401:
      return "No autenticado. Inicia sesión nuevamente.";
    case 403:
      return "No autorizado. Debes tener rol Administrador.";
    case 404:
      return "Equipo no encontrado.";
    case 409:
      if (context === "delete") {
        return "El equipo participa en una partida activa y no puede eliminarse.";
      }
      return caught.message || "Ya existe un equipo con ese nombre.";
    case 400:
      return caught.message || "Solicitud inválida. Verifica los datos enviados.";
    case 502:
      return "Error de integración con Keycloak. Inténtalo nuevamente.";
    default:
      return caught.message || "Error inesperado en Identity Service.";
  }
}
