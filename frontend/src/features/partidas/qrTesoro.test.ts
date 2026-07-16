import { describe, expect, it } from "vitest";
import { generarCodigoTesoro, nombreArchivoQr, renderizarQrDataUrl } from "./qrTesoro";

const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

describe("generarCodigoTesoro", () => {
  it("genera un UUID", () => {
    expect(generarCodigoTesoro()).toMatch(UUID_RE);
  });

  it("genera un codigo distinto cada vez", () => {
    expect(generarCodigoTesoro()).not.toBe(generarCodigoTesoro());
  });
});

describe("renderizarQrDataUrl", () => {
  it("devuelve un data-URL PNG", async () => {
    const dataUrl = await renderizarQrDataUrl(generarCodigoTesoro());
    expect(dataUrl.startsWith("data:image/png;base64,")).toBe(true);
  });
});

describe("nombreArchivoQr", () => {
  it("nombra el archivo por el orden de la etapa", () => {
    expect(nombreArchivoQr(3)).toBe("tesoro-etapa-3.png");
  });
});
