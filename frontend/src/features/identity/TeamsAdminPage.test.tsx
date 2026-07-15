import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TeamsAdminPage } from "./TeamsAdminPage";
import * as adminTeamsApi from "../../api/adminTeamsApi";
import * as identityApi from "../../api/identityApi";

describe("TeamsAdminPage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  const LEADER: adminTeamsApi.AdminTeamMember = { usuarioId: "u1", esLider: true };
  const MEMBER: adminTeamsApi.AdminTeamMember = { usuarioId: "u2", esLider: false };

  const TEAM: adminTeamsApi.AdminTeam = {
    equipoId: "t1",
    nombreEquipo: "Los Halcones",
    estado: "Activo",
    liderUserId: "u1",
    integrantes: [LEADER, MEMBER]
  };

  const ANA: identityApi.IdentityUserSummary = {
    userId: "u1",
    keycloakId: "k1",
    name: "Ana",
    email: "ana@demo.com",
    role: "Participante",
    status: "Activo"
  };

  const BETO: identityApi.IdentityUserSummary = {
    userId: "u2",
    keycloakId: "k2",
    name: "Beto",
    email: "beto@demo.com",
    role: "Participante",
    status: "Activo"
  };

  function mockLists(teams: adminTeamsApi.AdminTeam[] = [TEAM], users: identityApi.IdentityUserSummary[] = [ANA, BETO]) {
    vi.spyOn(adminTeamsApi, "listAdminTeams").mockResolvedValue(teams);
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue(users);
  }

  it("loads teams and renders a row with name, estado, members and leader", async () => {
    mockLists();

    render(<TeamsAdminPage accessToken="token" />);

    expect(await screen.findByText("Los Halcones")).toBeInTheDocument();
    expect(screen.getByRole("cell", { name: "Activo" })).toBeInTheDocument();
    expect(screen.getByRole("cell", { name: "2" })).toBeInTheDocument();
    // La celda de líder muestra el nombre (u1 -> Ana), no el id.
    expect(screen.getByRole("cell", { name: "Ana" })).toBeInTheDocument();
    expect(screen.queryByText("u1")).not.toBeInTheDocument();
  });

  it("creates a team sending the selected user's userId as liderUserId", async () => {
    mockLists([]);
    const createSpy = vi.spyOn(adminTeamsApi, "createAdminTeam").mockResolvedValue(TEAM);

    render(<TeamsAdminPage accessToken="token" />);

    await screen.findByTestId("create-team-leader-select");
    await userEvent.type(screen.getByLabelText(/nombre del equipo/i), "Los Halcones");
    await userEvent.selectOptions(screen.getByTestId("create-team-leader-select"), "u1");
    await userEvent.click(screen.getByTestId("create-team-submit"));

    expect(createSpy).toHaveBeenCalledWith(
      { nombreEquipo: "Los Halcones", liderUserId: "u1" },
      "token"
    );
    expect(await screen.findByTestId("create-team-success")).toBeInTheDocument();
  });

  it("renames a team", async () => {
    mockLists();
    const renameSpy = vi.spyOn(adminTeamsApi, "renameAdminTeam").mockResolvedValue({
      ...TEAM,
      nombreEquipo: "Los Cóndores"
    });

    render(<TeamsAdminPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("team-rename-open-t1"));
    const input = screen.getByTestId("rename-team-input");
    await userEvent.clear(input);
    await userEvent.type(input, "Los Cóndores");
    await userEvent.click(screen.getByTestId("rename-team-confirm"));

    expect(renameSpy).toHaveBeenCalledWith("t1", { nombreEquipo: "Los Cóndores" }, "token");
    expect(await screen.findByText("Los Cóndores")).toBeInTheDocument();
  });

  it("reassigns leadership to a current member", async () => {
    mockLists();
    const reassignSpy = vi.spyOn(adminTeamsApi, "reassignAdminTeamLeader").mockResolvedValue({
      ...TEAM,
      liderUserId: "u2",
      integrantes: [
        { usuarioId: "u1", esLider: false },
        { usuarioId: "u2", esLider: true }
      ]
    });

    render(<TeamsAdminPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("team-reassign-open-t1"));
    await userEvent.selectOptions(screen.getByTestId("reassign-team-select"), "u2");
    await userEvent.click(screen.getByTestId("reassign-team-confirm"));

    expect(reassignSpy).toHaveBeenCalledWith("t1", { nuevoLiderUserId: "u2" }, "token");
  });

  it("toggles estado from Activo to Desactivado", async () => {
    mockLists();
    const estadoSpy = vi.spyOn(adminTeamsApi, "setAdminTeamEstado").mockResolvedValue({
      ...TEAM,
      estado: "Desactivado"
    });

    render(<TeamsAdminPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("team-estado-toggle-t1"));

    expect(estadoSpy).toHaveBeenCalledWith("t1", { estado: "Desactivado" }, "token");
  });

  it("deletes a team after confirmation and reports the notification outcome", async () => {
    mockLists();
    const deleteSpy = vi.spyOn(adminTeamsApi, "deleteAdminTeam").mockResolvedValue({
      equipoId: "t1",
      nombreEquipo: "Los Halcones",
      integrantesTotal: 2,
      integrantesNotificados: 2,
      servidorCorreoRespondio: true
    });
    vi.spyOn(adminTeamsApi, "listAdminTeams").mockResolvedValueOnce([TEAM]).mockResolvedValueOnce([]);

    render(<TeamsAdminPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("team-delete-open-t1"));
    expect(screen.getByTestId("delete-team-modal")).toBeInTheDocument();
    await userEvent.click(screen.getByTestId("delete-team-confirm"));

    expect(deleteSpy).toHaveBeenCalledWith("t1", "token");
    await waitFor(() => expect(screen.queryByTestId("delete-team-modal")).toBeNull());
    expect(await screen.findByText(/Se notificó a 2 de 2 integrante/)).toBeInTheDocument();
  });

  it("informa cuando el servidor de correo no respondió al eliminar", async () => {
    mockLists();
    vi.spyOn(adminTeamsApi, "deleteAdminTeam").mockResolvedValue({
      equipoId: "t1",
      nombreEquipo: "Los Halcones",
      integrantesTotal: 2,
      integrantesNotificados: 0,
      servidorCorreoRespondio: false
    });
    vi.spyOn(adminTeamsApi, "listAdminTeams").mockResolvedValueOnce([TEAM]).mockResolvedValueOnce([]);

    render(<TeamsAdminPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("team-delete-open-t1"));
    await userEvent.click(screen.getByTestId("delete-team-confirm"));

    expect(
      await screen.findByText(/el servidor de correo no respondió/)
    ).toBeInTheDocument();
  });

  it("maps a 409 delete rejection to the 'partida activa' message", async () => {
    mockLists();
    vi.spyOn(adminTeamsApi, "deleteAdminTeam").mockRejectedValue(
      new adminTeamsApi.IdentityApiError("conflict", 409)
    );

    render(<TeamsAdminPage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("team-delete-open-t1"));
    await userEvent.click(screen.getByTestId("delete-team-confirm"));

    expect(await screen.findByTestId("delete-team-error")).toHaveTextContent(
      "El equipo participa en una partida activa y no puede eliminarse."
    );
  });

  it("maps 403 list error to admin authorization message", async () => {
    vi.spyOn(adminTeamsApi, "listAdminTeams").mockRejectedValue(
      new adminTeamsApi.IdentityApiError("forbidden", 403)
    );
    vi.spyOn(identityApi, "getIdentityUsers").mockResolvedValue([]);

    render(<TeamsAdminPage accessToken="token" />);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "No autorizado. Debes tener rol Administrador."
    );
  });

  it("shows an empty state when there are no teams", async () => {
    mockLists([]);

    render(<TeamsAdminPage accessToken="token" />);

    expect(await screen.findByText(/no hay equipos registrados/i)).toBeInTheDocument();
  });
});
