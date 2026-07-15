import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { GovernancePage } from "./GovernancePage";
import * as identityApi from "../../api/identityApi";

const MATRIZ: identityApi.GovernanceRolesResponse = {
  roles: [
    { rol: "Administrador", permisos: [], privilegiosGobernanza: true },
    { rol: "Operador", permisos: ["GestionarPartidas"], privilegiosGobernanza: false },
    {
      rol: "Participante",
      permisos: ["GestionarEquipos"],
      privilegiosGobernanza: false
    }
  ]
};

describe("GovernancePage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renderiza la matriz: 3 cards, badge de gobernanza solo en Administrador", async () => {
    vi.spyOn(identityApi, "getGovernanceRoles").mockResolvedValue(MATRIZ);

    render(<GovernancePage accessToken="token" />);

    expect(await screen.findByTestId("gov-card-Administrador")).toBeInTheDocument();
    expect(screen.getByTestId("gov-card-Operador")).toBeInTheDocument();
    expect(screen.getByTestId("gov-card-Participante")).toBeInTheDocument();
    expect(screen.getAllByTestId("gov-badge-admin")).toHaveLength(1);
    expect(screen.getByTestId("gov-check-Operador-GestionarPartidas")).toBeChecked();
    expect(screen.getByTestId("gov-check-Operador-GestionarEquipos")).not.toBeChecked();
  });

  it("guardar deshabilitado sin cambios; toggle habilita y el PUT manda el set completo", async () => {
    vi.spyOn(identityApi, "getGovernanceRoles").mockResolvedValue(MATRIZ);
    const putSpy = vi.spyOn(identityApi, "updateRolePermissions").mockResolvedValue({
      rol: "Operador",
      permisos: ["GestionarPartidas", "GestionarEquipos"],
      privilegiosGobernanza: false
    });

    render(<GovernancePage accessToken="token" />);

    const save = await screen.findByTestId("gov-save-Operador");
    expect(save).toBeDisabled();

    await userEvent.click(screen.getByTestId("gov-check-Operador-GestionarEquipos"));
    expect(save).toBeEnabled();

    await userEvent.click(save);

    expect(putSpy).toHaveBeenCalledWith(
      "Operador",
      ["GestionarPartidas", "GestionarEquipos"],
      "token"
    );
    // Tras el éxito el estado confirmado se actualiza: Guardar vuelve a deshabilitarse.
    expect(await screen.findByTestId("gov-save-Operador")).toBeDisabled();
  });

  it("502 al guardar muestra el mensaje de Keycloak en la card correcta y conserva lo marcado", async () => {
    vi.spyOn(identityApi, "getGovernanceRoles").mockResolvedValue(MATRIZ);
    vi.spyOn(identityApi, "updateRolePermissions").mockRejectedValue(
      new identityApi.IdentityApiError("bad gateway", 502)
    );

    render(<GovernancePage accessToken="token" />);

    await userEvent.click(await screen.findByTestId("gov-check-Participante-GestionarEquipos"));
    await userEvent.click(screen.getByTestId("gov-save-Participante"));

    expect(await screen.findByTestId("gov-error-Participante")).toHaveTextContent(/keycloak no disponible/i);
    expect(screen.getByTestId("gov-check-Participante-GestionarEquipos")).not.toBeChecked();
    expect(screen.getByTestId("gov-save-Participante")).toBeEnabled();
  });

  it("error de carga muestra gov-load-error con reintento", async () => {
    const getSpy = vi
      .spyOn(identityApi, "getGovernanceRoles")
      .mockRejectedValueOnce(new identityApi.IdentityApiError("boom", 500))
      .mockResolvedValueOnce(MATRIZ);

    render(<GovernancePage accessToken="token" />);

    expect(await screen.findByTestId("gov-load-error")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /reintentar/i }));

    expect(await screen.findByTestId("gov-card-Operador")).toBeInTheDocument();
    expect(getSpy).toHaveBeenCalledTimes(2);
  });

  /* El panel gobierna dos privilegios. ParticiparEnPartidas esta fijo al rol Participante
     (composite del realm) y no es asignable: mostrarlo prometeria algo que el backend rechaza. */
  it("no ofrece ParticiparEnPartidas como privilegio asignable", async () => {
    vi.spyOn(identityApi, "getGovernanceRoles").mockResolvedValue(MATRIZ);

    render(<GovernancePage accessToken="token" />);

    // "Gestionar partidas"/"Gestionar equipos" aparecen una vez por card (3 roles).
    await screen.findAllByText("Gestionar partidas");
    expect(screen.getAllByText("Gestionar equipos").length).toBeGreaterThan(0);
    expect(screen.queryByText("Participar en partidas")).not.toBeInTheDocument();
  });
});
