import { uploadBdtTreasure } from "./bdtTreasureUploadApi.js";

export const allowedTreasureImageTypes = ["image/jpeg", "image/png"];
export const maxTreasureImageSizeBytes = 5 * 1024 * 1024;

export function validateTreasureImage(image) {
  if (!image?.uri) {
    return { ok: false, message: "Selecciona o toma una foto del tesoro QR." };
  }

  if (!allowedTreasureImageTypes.includes(image.type)) {
    return { ok: false, message: "Solo se aceptan imagenes JPEG o PNG." };
  }

  if (typeof image.size === "number" && image.size > maxTreasureImageSizeBytes) {
    return { ok: false, message: "La imagen no puede superar 5 MB." };
  }

  return { ok: true };
}

export async function submitTreasureUpload({ apiBaseUrl, token, partidaId, etapaId, image, fetchImpl, formDataFactory }) {
  if (!partidaId || !etapaId) {
    return { ok: false, type: "validation", message: "La partida o etapa BDT no es valida." };
  }

  const validation = validateTreasureImage(image);
  if (!validation.ok) {
    return { ok: false, type: "validation", message: validation.message };
  }

  return await uploadBdtTreasure(apiBaseUrl, token, partidaId, etapaId, image, fetchImpl, formDataFactory);
}
