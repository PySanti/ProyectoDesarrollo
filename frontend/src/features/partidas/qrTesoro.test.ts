import jsQR from "jsqr";
import { PNG } from "pngjs";
import { describe, expect, it } from "vitest";
import { generarCodigoTesoro, nombreArchivoQr, renderizarQrDataUrl } from "./qrTesoro";

const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// Decodifica el PNG generado y lee el QR con jsQR para probar, de punta a punta, que el texto
// que sale escaneando el codigo es EXACTAMENTE el que se le paso a renderizarQrDataUrl. Esto es
// lo que realmente importa: Operaciones de Sesion compara el texto decodificado contra el
// codigo guardado de forma literal, asi que un re-casing o una mutacion silenciosa (p.ej. un
// .toUpperCase() colado en la funcion) rompe la validacion del tesoro en produccion aunque el
// data-URL siga pareciendo un PNG valido.
function decodificarQr(dataUrl: string): string {
  const base64 = dataUrl.replace(/^data:image\/png;base64,/, "");
  const png = PNG.sync.read(Buffer.from(base64, "base64"));
  const resultado = jsQR(new Uint8ClampedArray(png.data), png.width, png.height);
  if (!resultado) {
    throw new Error("jsQR no pudo decodificar el PNG generado por renderizarQrDataUrl");
  }
  return resultado.data;
}

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

  it("el QR decodificado contiene el codigo exacto, sin normalizar mayusculas/minusculas", async () => {
    // Codigo con casing mixto a proposito: si renderizarQrDataUrl alguna vez normalizara
    // (toUpperCase/toLowerCase) o mutara el texto de cualquier otra forma, este test lo detecta;
    // un UUID generado por randomUUID() nunca tiene mayusculas, asi que no bastaria para probarlo.
    const codigo = "AbCd1234-EeFF-4a1b-9c2d-0123456789Ab";
    const dataUrl = await renderizarQrDataUrl(codigo);
    expect(decodificarQr(dataUrl)).toBe(codigo);
  });
});

describe("nombreArchivoQr", () => {
  it("nombra el archivo por el juego y el orden de la etapa", () => {
    expect(nombreArchivoQr(2, 3)).toBe("tesoro-juego-2-etapa-3.png");
  });

  it("distingue etapas con el mismo orden en juegos BDT distintos", () => {
    // Orden es unico por juego, no por partida: una partida con dos juegos BDT tiene dos
    // "etapa 1" (una por juego). El nombre debe incluir el juego para no colisionar.
    expect(nombreArchivoQr(2, 1)).not.toBe(nombreArchivoQr(3, 1));
  });
});
