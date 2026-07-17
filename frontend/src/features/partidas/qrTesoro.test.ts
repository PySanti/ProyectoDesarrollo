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
  it("nombra el archivo con el juego, la etapa y el prefijo del codigo de la etapa", () => {
    expect(nombreArchivoQr(2, 3, "abcdef12-3456-7890-abcd-ef1234567890")).toBe(
      "tesoro-juego-2-etapa-3-abcdef12.png"
    );
  });

  it("no transforma el codigo: solo toma su primer segmento (hasta el primer guion) como prefijo", () => {
    // El codigo se guarda literal en todos lados (draft, backend, QR). Aqui solo se usa una
    // rebanada para el nombre de archivo; el propio codigo (fuera de esta funcion) nunca se
    // toca. Un casing mixto no debe normalizarse tampoco en el prefijo.
    expect(nombreArchivoQr(1, 1, "AbCd1234-EeFF-4a1b-9c2d-0123456789Ab")).toBe(
      "tesoro-juego-1-etapa-1-AbCd1234.png"
    );
  });

  it("dos etapas con el mismo orden de juego y de etapa, pero codigos distintos, producen nombres de archivo completos distintos", () => {
    // Reproduce la regresion real: la posicion de un juego (y por tanto su "orden") es
    // mutable via subir/bajar en el wizard, y un archivo ya descargado/impreso tiene su
    // nombre congelado en disco. Si dos etapas terminan compartiendo juego+etapa (por
    // reordenamiento tras una descarga previa), solo el codigo -unico por etapa por regla
    // de dominio- garantiza que el nombre no colisione. Se asume la cadena completa, no un
    // simple "no son iguales", para que un cambio que ignore el codigo (p.ej. solo lo
    // concatena vacio) tambien falle.
    const nombreA = nombreArchivoQr(2, 1, "11111111-1111-1111-1111-111111111111");
    const nombreB = nombreArchivoQr(2, 1, "22222222-2222-2222-2222-222222222222");

    expect(nombreA).toBe("tesoro-juego-2-etapa-1-11111111.png");
    expect(nombreB).toBe("tesoro-juego-2-etapa-1-22222222.png");
    expect(nombreA).not.toBe(nombreB);
  });
});
