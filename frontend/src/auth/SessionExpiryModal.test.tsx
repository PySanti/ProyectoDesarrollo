import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SessionExpiryModal } from "./SessionExpiryModal";

describe("SessionExpiryModal", () => {
  it("no renderiza nada cuando visible=false", () => {
    render(<SessionExpiryModal visible={false} onContinuar={() => {}} onSalir={() => {}} />);
    expect(screen.queryByTestId("session-expiry-modal")).not.toBeInTheDocument();
  });

  it("visible: muestra textos y los botones llaman sus callbacks", async () => {
    const onContinuar = vi.fn();
    const onSalir = vi.fn();
    render(<SessionExpiryModal visible onContinuar={onContinuar} onSalir={onSalir} />);
    expect(screen.getByText("¿Sigues ahí?")).toBeInTheDocument();
    expect(screen.getByText("Tu sesión está por expirar.")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Continuar sesión" }));
    expect(onContinuar).toHaveBeenCalledTimes(1);
    await userEvent.click(screen.getByRole("button", { name: "Salir" }));
    expect(onSalir).toHaveBeenCalledTimes(1);
  });
});
