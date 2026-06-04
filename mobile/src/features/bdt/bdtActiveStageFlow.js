import { getActiveBdtStage } from "./bdtActiveStageApi.js";

export async function loadActiveBdtStage({ apiBaseUrl, token, partidaId, fetchImpl }) {
  if (!partidaId) {
    return { ok: false, type: "validation", message: "Selecciona una BDT valida." };
  }

  try {
    return await getActiveBdtStage(apiBaseUrl, token, partidaId, fetchImpl);
  } catch {
    return {
      ok: false,
      type: "error",
      message: "Ocurrio un error inesperado al cargar la etapa activa.",
    };
  }
}

export function calculateRemainingSeconds(cierraEnUtc, now = new Date()) {
  const closeTime = new Date(cierraEnUtc).getTime();
  if (!Number.isFinite(closeTime)) {
    return 0;
  }

  return Math.max(0, Math.ceil((closeTime - now.getTime()) / 1000));
}
