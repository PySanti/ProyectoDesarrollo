export function buildBdtTreasureUploadParams(stageData) {
  return {
    partidaId: stageData.partidaId,
    etapaId: stageData.etapaActiva.etapaId,
  };
}
