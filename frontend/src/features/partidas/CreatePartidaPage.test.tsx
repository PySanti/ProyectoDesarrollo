import { describe, expect, it, vi, beforeEach } from "vitest";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CreatePartidaPage } from "./CreatePartidaPage";
import { enviarPartida } from "./enviarPartida";
import type { ResultadoEnvio } from "./enviarPartida";

const { navigateMock } = vi.hoisted(() => ({ navigateMock: vi.fn() }));

vi.mock("react-router-dom", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router-dom")>();
  return { ...actual, useNavigate: () => navigateMock };
});

vi.mock("./enviarPartida", () => ({ enviarPartida: vi.fn() }));

const enviarPartidaMock = vi.mocked(enviarPartida);

async function fillValidHeader(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText(/nombre de la partida/i), "Trivia de prueba");
  await user.clear(screen.getByLabelText(/m[ií]nimo de participaci[oó]n/i));
  await user.type(screen.getByLabelText(/m[ií]nimo de participaci[oó]n/i), "1");
  await user.clear(screen.getByLabelText(/m[aá]ximo de participaci[oó]n/i));
  await user.type(screen.getByLabelText(/m[aá]ximo de participaci[oó]n/i), "10");
  await user.click(screen.getByTestId("btn-siguiente"));
}

// Juego 1 = Trivia con una pregunta valida (2 opciones con texto, la primera
// ya es "correcta" por defecto en newPregunta(), asi que no hace falta tocar el radio).
async function addValidTriviaGame(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByTestId("btn-agregar-trivia"));
  const region = screen.getByRole("region", { name: "Juego 1" });
  await user.click(within(region).getByRole("button", { name: /agregar pregunta/i }));
  await user.type(within(region).getByLabelText(/texto de la pregunta 1/i), "2+2?");
  const opciones = within(region).getAllByLabelText(/^opci[oó]n \d pregunta 1$/i);
  await user.type(opciones[0], "4");
  await user.type(opciones[1], "5");
  await user.type(within(region).getByLabelText(/^puntaje$/i), "100");
  await user.type(within(region).getByLabelText(/tiempo l[ií]mite/i), "30");
}

describe("CreatePartidaPage", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    enviarPartidaMock.mockReset();
  });

  it("no avanza el paso 1 con nombre vacio y avanza con datos validos en modo Manual", async () => {
    const user = userEvent.setup();
    render(<CreatePartidaPage accessToken="token" />);

    expect(screen.getByTestId("paso-1")).toBeInTheDocument();

    await user.clear(screen.getByLabelText(/m[ií]nimo de participaci[oó]n/i));
    await user.type(screen.getByLabelText(/m[ií]nimo de participaci[oó]n/i), "1");
    await user.clear(screen.getByLabelText(/m[aá]ximo de participaci[oó]n/i));
    await user.type(screen.getByLabelText(/m[aá]ximo de participaci[oó]n/i), "10");
    await user.click(screen.getByTestId("btn-siguiente"));

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByTestId("paso-1")).toBeInTheDocument();

    await user.type(screen.getByLabelText(/nombre de la partida/i), "Trivia de prueba");
    await user.click(screen.getByTestId("btn-siguiente"));

    expect(screen.getByTestId("paso-2")).toBeInTheDocument();
  });

  it("muestra tiempoInicio en modo Automatico y lo exige para avanzar", async () => {
    const user = userEvent.setup();
    render(<CreatePartidaPage accessToken="token" />);

    expect(screen.queryByLabelText(/tiempo de inicio/i)).not.toBeInTheDocument();

    await user.selectOptions(screen.getByLabelText(/modo de inicio/i), "Automatico");
    expect(screen.getByLabelText(/tiempo de inicio/i)).toBeInTheDocument();

    await user.type(screen.getByLabelText(/nombre de la partida/i), "Trivia automatica");
    await user.clear(screen.getByLabelText(/m[ií]nimo de participaci[oó]n/i));
    await user.type(screen.getByLabelText(/m[ií]nimo de participaci[oó]n/i), "1");
    await user.clear(screen.getByLabelText(/m[aá]ximo de participaci[oó]n/i));
    await user.type(screen.getByLabelText(/m[aá]ximo de participaci[oó]n/i), "10");
    await user.click(screen.getByTestId("btn-siguiente"));

    expect(screen.getByRole("alert")).toHaveTextContent(/tiempo de inicio es obligatorio/i);
    expect(screen.getByTestId("paso-1")).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText(/tiempo de inicio/i), {
      target: { value: "2026-08-01T10:00" }
    });
    await user.click(screen.getByTestId("btn-siguiente"));

    expect(screen.getByTestId("paso-2")).toBeInTheDocument();
  });

  it("agrega una Trivia y un BDT en orden, y subir el segundo lo vuelve Juego 1", async () => {
    const user = userEvent.setup();
    render(<CreatePartidaPage accessToken="token" />);
    await fillValidHeader(user);

    await user.click(screen.getByTestId("btn-agregar-trivia"));
    await user.click(screen.getByTestId("btn-agregar-bdt"));

    expect(screen.getByRole("heading", { name: "Juego 1 — Trivia" })).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "Juego 2 — Búsqueda del Tesoro" })
    ).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /subir juego 2/i }));

    expect(
      screen.getByRole("heading", { name: "Juego 1 — Búsqueda del Tesoro" })
    ).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Juego 2 — Trivia" })).toBeInTheDocument();
  });

  it("no avanza el paso 2 con una pregunta invalida (1 opcion) y avanza con juegos validos", async () => {
    const user = userEvent.setup();
    render(<CreatePartidaPage accessToken="token" />);
    await fillValidHeader(user);

    await user.click(screen.getByTestId("btn-agregar-trivia"));
    const region = screen.getByRole("region", { name: "Juego 1" });
    await user.click(within(region).getByRole("button", { name: /agregar pregunta/i }));
    await user.type(within(region).getByLabelText(/texto de la pregunta 1/i), "2+2?");
    await user.click(within(region).getByRole("button", { name: /eliminar opci[oó]n 2/i }));
    expect(within(region).getAllByLabelText(/^opci[oó]n \d pregunta 1$/i)).toHaveLength(1);

    await user.click(screen.getByTestId("btn-siguiente"));

    expect(screen.getByRole("alert")).toHaveTextContent(/al menos 2 opciones/i);
    expect(screen.getByTestId("paso-2")).toBeInTheDocument();

    await user.click(within(region).getByRole("button", { name: /agregar opci[oó]n/i }));
    const opciones = within(region).getAllByLabelText(/^opci[oó]n \d pregunta 1$/i);
    await user.type(opciones[0], "4");
    await user.type(opciones[1], "5");
    await user.type(within(region).getByLabelText(/^puntaje$/i), "100");
    await user.type(within(region).getByLabelText(/tiempo l[ií]mite/i), "30");

    await user.click(screen.getByTestId("btn-siguiente"));

    expect(screen.getByTestId("paso-3")).toBeInTheDocument();
  });

  it("genera el QR de la etapa y ofrece descargarlo", async () => {
    const user = userEvent.setup();

    render(<CreatePartidaPage accessToken="token-xyz" />);
    await fillValidHeader(user);
    await addValidTriviaGame(user);

    await user.click(screen.getByTestId("btn-agregar-bdt"));
    const bdtRegion = screen.getByRole("region", { name: "Juego 2" });
    await user.type(within(bdtRegion).getByLabelText(/[aá]rea de b[uú]squeda/i), "Patio");
    await user.click(within(bdtRegion).getByRole("button", { name: /agregar etapa/i }));

    await user.click(within(bdtRegion).getByRole("button", { name: /^generar qr/i }));

    expect(await within(bdtRegion).findByRole("img", { name: /qr del tesoro/i })).toBeInTheDocument();
    expect(within(bdtRegion).getByRole("link", { name: /descargar qr/i })).toHaveAttribute(
      "download",
      "tesoro-etapa-1.png"
    );
    expect(within(bdtRegion).getByRole("button", { name: /regenerar qr/i })).toBeInTheDocument();
  });

  it("en el paso 3 llama a enviarPartida con el draft y navega al detalle en exito", async () => {
    const user = userEvent.setup();
    enviarPartidaMock.mockResolvedValue({
      partidaId: "partida-1",
      estados: [{ estado: "ok" }],
      completo: true
    } satisfies ResultadoEnvio);

    render(<CreatePartidaPage accessToken="token-abc" />);
    await fillValidHeader(user);
    await addValidTriviaGame(user);
    await user.click(screen.getByTestId("btn-siguiente"));

    expect(screen.getByTestId("paso-3")).toBeInTheDocument();
    await user.click(screen.getByTestId("btn-crear-partida"));

    expect(enviarPartidaMock).toHaveBeenCalledWith(
      expect.objectContaining({
        header: expect.objectContaining({ nombrePartida: "Trivia de prueba" }),
        juegos: expect.any(Array)
      }),
      "token-abc",
      null,
      expect.any(Function)
    );
    expect(navigateMock).toHaveBeenCalledWith("/partidas/partida-1");
  });

  it("en fallo parcial muestra btn-reintentar y lo llama con el previo correcto", async () => {
    const user = userEvent.setup();
    enviarPartidaMock.mockResolvedValue({
      partidaId: "partida-2",
      estados: [{ estado: "ok" }, { estado: "error", mensaje: "boom" }],
      completo: false
    } satisfies ResultadoEnvio);

    render(<CreatePartidaPage accessToken="token-xyz" />);
    await fillValidHeader(user);
    await addValidTriviaGame(user);

    await user.click(screen.getByTestId("btn-agregar-bdt"));
    const bdtRegion = screen.getByRole("region", { name: "Juego 2" });
    await user.type(within(bdtRegion).getByLabelText(/[aá]rea de b[uú]squeda/i), "Patio");
    await user.click(within(bdtRegion).getByRole("button", { name: /agregar etapa/i }));
    await user.click(within(bdtRegion).getByRole("button", { name: /^generar qr/i }));
    await user.type(within(bdtRegion).getByLabelText(/^puntaje$/i), "50");
    await user.type(within(bdtRegion).getByLabelText(/tiempo l[ií]mite/i), "60");

    await user.click(screen.getByTestId("btn-siguiente"));
    expect(screen.getByTestId("paso-3")).toBeInTheDocument();

    await user.click(screen.getByTestId("btn-crear-partida"));

    expect(await screen.findByTestId("btn-reintentar")).toBeInTheDocument();
    expect(screen.getByTestId("envio-juego-0")).toHaveTextContent(/ok/i);
    expect(screen.getByTestId("envio-juego-1")).toHaveTextContent(/error/i);

    await user.click(screen.getByTestId("btn-reintentar"));

    expect(enviarPartidaMock).toHaveBeenLastCalledWith(
      expect.anything(),
      "token-xyz",
      { partidaId: "partida-2", estados: [{ estado: "ok" }, { estado: "error", mensaje: "boom" }] },
      expect.any(Function)
    );
  });

  it("editar los juegos tras un fallo parcial descarta el progreso previo (no reintenta con indices stale)", async () => {
    const user = userEvent.setup();
    enviarPartidaMock.mockResolvedValue({
      partidaId: "partida-2",
      estados: [{ estado: "ok" }, { estado: "error", mensaje: "boom" }],
      completo: false
    } satisfies ResultadoEnvio);

    render(<CreatePartidaPage accessToken="token-xyz" />);
    await fillValidHeader(user);
    await addValidTriviaGame(user);

    await user.click(screen.getByTestId("btn-agregar-bdt"));
    const bdtRegion = screen.getByRole("region", { name: "Juego 2" });
    await user.type(within(bdtRegion).getByLabelText(/[aá]rea de b[uú]squeda/i), "Patio");
    await user.click(within(bdtRegion).getByRole("button", { name: /agregar etapa/i }));
    await user.click(within(bdtRegion).getByRole("button", { name: /^generar qr/i }));
    await user.type(within(bdtRegion).getByLabelText(/^puntaje$/i), "50");
    await user.type(within(bdtRegion).getByLabelText(/tiempo l[ií]mite/i), "60");

    await user.click(screen.getByTestId("btn-siguiente"));
    await user.click(screen.getByTestId("btn-crear-partida"));

    expect(await screen.findByTestId("btn-reintentar")).toBeInTheDocument();

    // El usuario vuelve al paso 2 y reordena los juegos: el progreso de envio
    // (mapeado por indice) queda desincronizado de la identidad real de cada
    // juego, asi que debe descartarse en vez de reutilizarse en el reintento.
    await user.click(screen.getByTestId("btn-atras"));
    await user.click(screen.getByRole("button", { name: /subir juego 2/i }));
    await user.click(screen.getByTestId("btn-siguiente"));

    expect(screen.getByTestId("paso-3")).toBeInTheDocument();
    expect(screen.queryByTestId("btn-reintentar")).not.toBeInTheDocument();
    const crearBtn = screen.getByTestId("btn-crear-partida");
    expect(crearBtn).toHaveTextContent(/crear partida/i);

    enviarPartidaMock.mockClear();
    enviarPartidaMock.mockResolvedValue({
      partidaId: "partida-3",
      estados: [{ estado: "ok" }, { estado: "ok" }],
      completo: true
    } satisfies ResultadoEnvio);

    await user.click(crearBtn);

    expect(enviarPartidaMock).toHaveBeenCalledWith(
      expect.anything(),
      "token-xyz",
      null,
      expect.any(Function)
    );
  });
});
